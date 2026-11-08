use axum::{
    Json,
    extract::{Path, State},
    http::StatusCode,
};
use serde::Deserialize;
use serde_json::{Value, json};

use super::{AppState, Auth};
use crate::{
    auth::{Role, password},
    db::{self, katalog, user},
    error::{AppError, AppResult},
};

const ADMIN: &[Role] = &[Role::Admin];

pub fn username_valid(u: &str) -> bool {
    (3..=50).contains(&u.len())
        && u.chars()
            .all(|c| c.is_ascii_lowercase() || c.is_ascii_digit() || matches!(c, '.' | '_' | '-'))
}

pub async fn list(State(s): State<AppState>, a: Auth) -> AppResult<Json<Vec<user::UserInfo>>> {
    a.peran(ADMIN)?;
    Ok(Json(db::run(&s.pool, user::list_users).await?))
}

#[derive(Deserialize)]
pub struct BuatReq {
    username: String,
    nama: String,
    role: Role,
    nik: Option<String>,
}

pub async fn buat(
    State(s): State<AppState>,
    a: Auth,
    Json(req): Json<BuatReq>,
) -> AppResult<Json<Value>> {
    a.peran(ADMIN)?;
    let username = req.username.trim().to_lowercase();
    if !username_valid(&username) {
        return Err(AppError::validation(
            "Username 3-50 karakter: huruf kecil, angka, titik, garis bawah, atau strip",
        ));
    }
    if req.nama.trim().is_empty() || req.nama.len() > 100 {
        return Err(AppError::validation("Nama wajib diisi (maks 100 karakter)"));
    }
    let nik = req
        .nik
        .map(|n| n.trim().to_string())
        .filter(|n| !n.is_empty());
    match (req.role, &nik) {
        (Role::Anggota, None) => {
            return Err(AppError::validation("Akun anggota wajib memiliki NIK"));
        }
        (Role::Kasir | Role::Admin, Some(_)) => {
            return Err(AppError::validation("NIK hanya untuk akun anggota"));
        }
        _ => {}
    }
    let tmp = password::temp_password();
    let t = tmp.clone();
    let id = db::run(&s.pool, move |c| {
        if let Some(n) = &nik
            && katalog::pelanggan_by_nik(c, n)?.is_none()
        {
            return Err(AppError::validation(
                "NIK tidak ditemukan di data anggota aktif",
            ));
        }
        user::create_user(
            c,
            &username,
            req.nama.trim(),
            &password::hash(&t)?,
            req.role,
            nik.as_deref(),
        )
    })
    .await?;
    // Password sementara hanya tampil sekali di respons ini; tidak disimpan/di-log.
    Ok(Json(json!({ "id": id, "password_sementara": tmp })))
}

pub async fn reset_password(
    State(s): State<AppState>,
    a: Auth,
    Path(id): Path<i64>,
) -> AppResult<Json<Value>> {
    a.peran(ADMIN)?;
    let tmp = password::temp_password();
    let t = tmp.clone();
    db::run(&s.pool, move |c| {
        user::find_by_id(c, id)?
            .ok_or_else(|| AppError::NotFound("User tidak ditemukan".into()))?;
        user::set_password(c, id, &password::hash(&t)?, true)?;
        user::revoke_user(c, id)
    })
    .await?;
    Ok(Json(json!({ "password_sementara": tmp })))
}

#[derive(Deserialize)]
pub struct AktifReq {
    aktif: bool,
}

pub async fn aktif(
    State(s): State<AppState>,
    a: Auth,
    Path(id): Path<i64>,
    Json(req): Json<AktifReq>,
) -> AppResult<StatusCode> {
    let me = a.peran(ADMIN)?.sub;
    if me == id && !req.aktif {
        return Err(AppError::validation(
            "Tidak bisa menonaktifkan akun sendiri",
        ));
    }
    db::run(&s.pool, move |c| {
        user::find_by_id(c, id)?
            .ok_or_else(|| AppError::NotFound("User tidak ditemukan".into()))?;
        user::set_active(c, id, req.aktif)
    })
    .await?;
    Ok(StatusCode::NO_CONTENT)
}

#[derive(Deserialize)]
pub struct KategoriReq {
    kategori_ids: Vec<String>,
}

pub async fn kategori_tampil(State(s): State<AppState>, a: Auth) -> AppResult<Json<Vec<String>>> {
    a.peran(ADMIN)?;
    Ok(Json(db::run(&s.pool, user::allowed_kategori).await?))
}

/// Kategori yang tampil di kasir untuk semua akun. Kosong = tidak dibatasi.
pub async fn set_kategori_tampil(
    State(s): State<AppState>,
    a: Auth,
    Json(req): Json<KategoriReq>,
) -> AppResult<StatusCode> {
    a.peran(ADMIN)?;
    db::run(&s.pool, move |c| {
        let semua = katalog::semua_kategori(c)?;
        if req
            .kategori_ids
            .iter()
            .any(|k| !semua.iter().any(|x| &x.id == k))
        {
            return Err(AppError::validation("Ada kategori yang tidak dikenal"));
        }
        user::set_kategori(c, &req.kategori_ids)
    })
    .await?;
    Ok(StatusCode::NO_CONTENT)
}

#[derive(Deserialize)]
pub struct UrutReq {
    produk_ids: Vec<i64>,
}

/// Urutan produk di kasir. Urutan array = urutan tampil; maks 1000 produk per permintaan.
pub async fn set_urut_produk(
    State(s): State<AppState>,
    a: Auth,
    Json(req): Json<UrutReq>,
) -> AppResult<StatusCode> {
    a.peran(ADMIN)?;
    if req.produk_ids.is_empty() || req.produk_ids.len() > 1000 {
        return Err(AppError::validation("Daftar produk harus 1-1000 item"));
    }
    db::run(&s.pool, move |c| katalog::urut_set(c, &req.produk_ids)).await?;
    Ok(StatusCode::NO_CONTENT)
}

pub async fn semua_kategori(
    State(s): State<AppState>,
    a: Auth,
) -> AppResult<Json<Vec<katalog::Kategori>>> {
    a.peran(ADMIN)?;
    Ok(Json(db::run(&s.pool, katalog::semua_kategori).await?))
}

#[cfg(test)]
mod tests {
    use super::username_valid;

    #[test]
    fn username_rules() {
        assert!(username_valid("budi.santoso"));
        assert!(!username_valid("Budi"));
        assert!(!username_valid("ab"));
        assert!(!username_valid("a b c"));
    }
}

// ------------------------------------------------------------------ dashboard & produk

pub async fn dashboard(State(s): State<AppState>, a: Auth) -> AppResult<Json<Value>> {
    a.peran(ADMIN)?;
    let d = db::run(&s.pool, db::dashboard::dashboard_admin).await?;
    Ok(Json(json!(d)))
}

/// Field eksplisit (bukan `#[serde(flatten)]`): query string selalu string, flatten membuat angka gagal di-parse.
#[derive(Deserialize)]
pub struct ProdukQuery {
    q: Option<String>,
    kategori: Option<String>,
    aktif: Option<String>,
    page: Option<u32>,
    size: Option<u32>,
}

pub async fn produk_list(
    State(s): State<AppState>,
    a: Auth,
    axum::extract::Query(q): axum::extract::Query<ProdukQuery>,
) -> AppResult<Json<Value>> {
    a.peran(ADMIN)?;
    let aktif = match q.aktif.as_deref() {
        None | Some("") => None,
        Some("Y") => Some("Y".to_string()),
        Some("N") => Some("N".to_string()),
        Some(_) => return Err(AppError::validation("Filter aktif harus Y atau N")),
    };
    let pg = super::Paging {
        q: q.q,
        page: q.page,
        size: q.size,
    };
    let (page, size) = (pg.page(), pg.size());
    let teks = pg.q;
    let kategori = q.kategori;
    let h = db::run(&s.pool, move |c| {
        katalog::daftar_produk_admin(
            c,
            teks.as_deref(),
            kategori.as_deref(),
            aktif.as_deref(),
            page,
            size,
        )
    })
    .await?;
    Ok(super::halaman(h.items, h.ada_lagi, page))
}

#[derive(Deserialize)]
pub struct UbahProdukReq {
    nama: String,
    barcode: Option<String>,
    satuan: String,
    kategori_id: String,
    harga: i64,
    beli: i64,
    aktif: bool,
}

pub async fn produk_ubah(
    State(s): State<AppState>,
    a: Auth,
    Path(id): Path<i64>,
    Json(req): Json<UbahProdukReq>,
) -> AppResult<Json<katalog::ProdukAdmin>> {
    let admin = a.peran(ADMIN)?.usr.clone();
    let p = katalog::UbahProduk {
        nama: req.nama,
        barcode: req.barcode.unwrap_or_default(),
        satuan: req.satuan,
        kategori_id: req.kategori_id,
        harga: req.harga,
        beli: req.beli,
        aktif: req.aktif,
    };
    let (lama, baru) = db::run(&s.pool, move |c| katalog::ubah_produk(c, id, &p)).await?;
    // Audit perubahan master produk (harga memengaruhi penjualan): siapa, produk mana, harga lama -> baru.
    tracing::info!(admin = %admin, produk = baru.id, kode = %baru.kode, harga_lama = lama, harga_baru = baru.harga, aktif = baru.aktif, "produk diubah");
    Ok(Json(baru))
}

#[cfg(test)]
mod produk_query_tests {
    use super::*;

    #[test]
    fn query_produk_menerima_angka() {
        let uri: axum::http::Uri =
            "/api/v1/admin/produk?q=pertalite&aktif=Y&page=2&size=30&kategori=14"
                .parse()
                .unwrap();
        let q = axum::extract::Query::<ProdukQuery>::try_from_uri(&uri).expect("query valid");
        assert_eq!((q.0.page, q.0.size), (Some(2), Some(30)));
    }
}

// ------------------------------------------------------------------ foto produk

/// Unggah foto produk: body = bytes gambar mentah (PNG/JPEG, maks 1 MB). Jenis dicek dari isi file.
pub async fn foto_ubah(
    State(s): State<AppState>,
    a: Auth,
    Path(id): Path<i64>,
    body: axum::body::Bytes,
) -> AppResult<Json<Value>> {
    let admin = a.peran(ADMIN)?.usr.clone();
    let data = body.to_vec();
    let versi = db::run(&s.pool, move |c| db::foto::simpan(c, id, &data)).await?;
    tracing::info!(admin = %admin, produk = id, versi, "foto produk diunggah");
    Ok(Json(json!({ "versi": versi })))
}

pub async fn foto_hapus(
    State(s): State<AppState>,
    a: Auth,
    Path(id): Path<i64>,
) -> AppResult<StatusCode> {
    let admin = a.peran(ADMIN)?.usr.clone();
    let ada = db::run(&s.pool, move |c| db::foto::hapus(c, id)).await?;
    if !ada {
        return Err(AppError::NotFound(
            "Produk tidak punya foto unggahan".into(),
        ));
    }
    tracing::info!(admin = %admin, produk = id, "foto produk dihapus");
    Ok(StatusCode::NO_CONTENT)
}
