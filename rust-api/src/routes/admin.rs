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

/// Username akun anggota = NIK ternormalisasi. Harus identik dengan `usernameDariNik` (mobile/lib/core/validasi.dart).
pub fn username_dari_nik(nik: &str) -> String {
    let mut out = String::new();
    let mut gap = false;
    for c in nik.trim().to_lowercase().chars() {
        if c.is_ascii_lowercase() || c.is_ascii_digit() || matches!(c, '.' | '_' | '-') {
            if gap {
                out.push('.');
                gap = false;
            }
            out.push(c);
        } else {
            gap = true;
        }
    }
    if gap {
        out.push('.');
    }
    out.chars().take(50).collect()
}

pub async fn list(State(s): State<AppState>, a: Auth) -> AppResult<Json<Vec<user::UserInfo>>> {
    let Json(mut users) = list_web(State(s), a).await?;
    users.retain(|u| u.role != Role::Pembelian);
    Ok(Json(users))
}

pub async fn list_web(State(s): State<AppState>, a: Auth) -> AppResult<Json<Vec<user::UserInfo>>> {
    a.peran(ADMIN)?;
    Ok(Json(db::run(&s.pool, user::list_users).await?))
}

/// Foto profil akun lain untuk daftar admin. Hanya ADMIN; hanya foto (tanpa kontak).
pub async fn foto_user(
    State(s): State<AppState>,
    a: Auth,
    Path(id): Path<i64>,
) -> AppResult<axum::response::Response> {
    use axum::{http::header, response::IntoResponse};
    a.peran(ADMIN)?;
    let f = db::run(&s.pool, move |c| db::profil::ambil_foto(c, id))
        .await?
        .ok_or_else(|| AppError::NotFound("Foto tidak ada".into()))?;
    Ok((
        [
            (header::CONTENT_TYPE, f.content_type),
            (header::CACHE_CONTROL, "private, no-cache".to_string()),
        ],
        f.data,
    )
        .into_response())
}

#[derive(Deserialize)]
pub struct BuatReq {
    username: String,
    nama: String,
    role: Role,
    nik: Option<String>,
}

/// Permintaan buat akun yang sudah divalidasi dan dinormalkan.
pub struct ValidBuat {
    pub username: String,
    pub nama: String,
    pub role: Role,
    pub nik: Option<String>,
}

/// Validasi murni (tanpa DB): username 3-50 karakter pola `username_valid`, nama 1-100 karakter,
/// NIK hanya untuk ANGGOTA (wajib, maks 30 karakter, bukan NIK pelanggan umum).
pub fn validasi_buat(req: &BuatReq, default_nik: &str) -> AppResult<ValidBuat> {
    let username = req.username.trim().to_lowercase();
    if !username_valid(&username) {
        return Err(AppError::validation(
            "Username 3-50 karakter: huruf kecil, angka, titik, garis bawah, atau strip",
        ));
    }
    let nama = req.nama.trim().to_string();
    if nama.is_empty() || nama.chars().count() > 100 {
        return Err(AppError::validation("Nama wajib diisi (maks 100 karakter)"));
    }
    let nik = req
        .nik
        .as_ref()
        .map(|n| n.trim().to_string())
        .filter(|n| !n.is_empty());
    match (req.role, &nik) {
        (Role::Anggota, None) => {
            return Err(AppError::validation("Akun anggota wajib memiliki NIK"));
        }
        (Role::Kasir | Role::Admin | Role::Pembelian, Some(_)) => {
            return Err(AppError::validation("NIK hanya untuk akun anggota"));
        }
        _ => {}
    }
    if let Some(n) = &nik {
        if n.chars().count() > 30 {
            return Err(AppError::validation("NIK maksimal 30 karakter"));
        }
        if n == default_nik {
            return Err(AppError::validation(
                "NIK pelanggan umum (tunai) tidak dapat dibuatkan akun",
            ));
        }
        let wajib = username_dari_nik(n);
        if req.role == Role::Anggota && username != wajib {
            return Err(AppError::validation(format!(
                "Username akun anggota harus sama dengan NIK ({wajib})"
            )));
        }
    }
    Ok(ValidBuat {
        username,
        nama,
        role: req.role,
        nik,
    })
}

pub async fn buat(
    State(s): State<AppState>,
    a: Auth,
    Json(req): Json<BuatReq>,
) -> AppResult<Json<Value>> {
    a.peran(ADMIN)?;
    if req.role == Role::Pembelian {
        return Err(AppError::validation("Akun petugas pembelian dibuat melalui POS Web"));
    }
    buat_web(State(s), a, Json(req)).await
}

pub async fn buat_web(State(s): State<AppState>, a: Auth, Json(req): Json<BuatReq>) -> AppResult<Json<Value>> {
    a.peran(ADMIN)?;
    let v = validasi_buat(&req, &s.cfg.default_nik)?;
    let tmp = password::temp_password();
    let t = tmp.clone();
    let id = db::run(&s.pool, move |c| {
        // Pesan jelas untuk duplikat (konflik unik Oracle hanya akan menjadi "Data sudah ada").
        if user::find_by_username(c, &v.username)?.is_some() {
            return Err(AppError::Conflict("Username sudah dipakai".into()));
        }
        if let Some(n) = &v.nik {
            if katalog::pelanggan_by_nik(c, n)?.is_none() {
                return Err(AppError::validation(
                    "NIK tidak ditemukan di data anggota aktif",
                ));
            }
            if user::nik_sudah_punya_akun(c, n)? {
                return Err(AppError::Conflict(
                    "Anggota ini sudah punya akun mobile".into(),
                ));
            }
        }
        user::create_user(
            c,
            &v.username,
            &v.nama,
            &password::hash(&t)?,
            v.role,
            v.nik.as_deref(),
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

    use super::{BuatReq, validasi_buat};
    use crate::auth::Role;

    fn req(username: &str, nama: &str, role: Role, nik: Option<&str>) -> BuatReq {
        BuatReq {
            username: username.into(),
            nama: nama.into(),
            role,
            nik: nik.map(Into::into),
        }
    }

    fn galat(r: BuatReq) -> String {
        match validasi_buat(&r, "00.00004") {
            Err(e) => format!("{e:?}"),
            Ok(_) => "OK".into(),
        }
    }

    #[test]
    fn validasi_buat_menormalkan_dan_menerima_data_benar() {
        let v = validasi_buat(&req("  Budi.Santoso ", " Budi Santoso ", Role::Kasir, None), "00.00004").unwrap();
        assert_eq!((v.username.as_str(), v.nama.as_str(), v.nik), ("budi.santoso", "Budi Santoso", None));
        let v = validasi_buat(&req("01.00062", "Aan", Role::Anggota, Some(" 01.00062 ")), "00.00004").unwrap();
        assert_eq!(v.nik.as_deref(), Some("01.00062"));
    }

    #[test]
    fn validasi_buat_menolak_username_salah() {
        assert!(galat(req("ab", "Budi", Role::Kasir, None)).contains("Username"));
        assert!(galat(req("budi santoso", "Budi", Role::Kasir, None)).contains("Username"));
        assert!(galat(req(&"a".repeat(51), "Budi", Role::Kasir, None)).contains("Username"));
    }

    #[test]
    fn validasi_buat_menolak_nama_salah() {
        assert!(galat(req("budi", "  ", Role::Kasir, None)).contains("Nama"));
        assert!(galat(req("budi", &"x".repeat(101), Role::Kasir, None)).contains("Nama"));
        // 100 karakter multibyte tetap valid (dihitung karakter, bukan byte)
        assert_eq!(galat(req("budi", &"é".repeat(100), Role::Kasir, None)), "OK");
    }

    #[test]
    fn validasi_buat_aturan_role_dan_nik() {
        assert!(galat(req("budi", "Budi", Role::Anggota, None)).contains("wajib memiliki NIK"));
        assert!(galat(req("budi", "Budi", Role::Kasir, Some("01.00062"))).contains("NIK hanya untuk akun anggota"));
        assert!(galat(req("budi", "Budi", Role::Anggota, Some(&"1".repeat(31)))).contains("maksimal 30"));
        assert!(galat(req("budi", "Budi", Role::Anggota, Some("00.00004"))).contains("pelanggan umum"));
        assert_eq!(galat(req("01.00062", "Budi", Role::Anggota, Some("01.00062"))), "OK");
    }

    #[test]
    fn username_anggota_harus_nik() {
        assert!(galat(req("budi", "Budi", Role::Anggota, Some("01.00062"))).contains("harus sama dengan NIK"));
        assert_eq!(galat(req("kb.23.001", "Budi", Role::Anggota, Some("KB 23/001"))), "OK");
        assert_eq!(super::username_dari_nik("A-24-061"), "a-24-061");
    }
}

// ------------------------------------------------------------------ dashboard & produk

pub async fn dashboard(State(s): State<AppState>, a: Auth) -> AppResult<Json<Value>> {
    a.peran(ADMIN)?;
    let d = db::run(&s.pool, db::dashboard::dashboard_admin).await?;
    Ok(Json(json!(d)))
}

#[derive(Deserialize)]
pub struct RincianQuery {
    dari: String,
    sampai: String,
}

/// Rincian penjualan semua kasir pada rentang tanggal: tunai vs kredit, per kasir, dan per hari.
pub async fn penjualan_rincian(
    State(s): State<AppState>,
    a: Auth,
    axum::extract::Query(q): axum::extract::Query<RincianQuery>,
) -> AppResult<Json<db::dashboard::Rincian>> {
    a.peran(ADMIN)?;
    let r = db::run(&s.pool, move |c| {
        let (hari, _) = db::faktur::sekarang(c)?;
        let (dari, sampai) = db::dashboard::validasi_rentang(&q.dari, &q.sampai, hari)?;
        db::dashboard::rincian_penjualan(c, dari, sampai)
    })
    .await?;
    Ok(Json(r))
}

/// Cache jumlah stok habis untuk Beranda admin: agregasinya berat, cukup diperbarui tiap 60 detik per proses.
static STOK_HABIS_CACHE: std::sync::Mutex<Option<(std::time::Instant, i64)>> = std::sync::Mutex::new(None);
const STOK_HABIS_TTL: std::time::Duration = std::time::Duration::from_secs(60);

/// Jumlah produk aktif stok habis dengan cache 60 detik (dipakai Beranda dan lencana menu).
async fn stok_habis_cached(pool: &db::DbPool) -> AppResult<i64> {
    let segar = STOK_HABIS_CACHE
        .lock()
        .unwrap_or_else(|e| e.into_inner())
        .filter(|(t, _)| t.elapsed() < STOK_HABIS_TTL)
        .map(|(_, n)| n);
    if let Some(n) = segar {
        return Ok(n);
    }
    let n = db::run(pool, |c| {
        let (hari, _) = db::faktur::sekarang(c)?;
        let awal = chrono::NaiveDate::from_ymd_opt(chrono::Datelike::year(&hari), 1, 1).expect("1 Jan");
        katalog::jumlah_stok_habis(c, awal, hari)
    })
    .await?;
    *STOK_HABIS_CACHE.lock().unwrap_or_else(|e| e.into_inner()) = Some((std::time::Instant::now(), n));
    Ok(n)
}

/// Jumlah produk aktif dengan stok sistem <= 0, dimuat terpisah agar Beranda tidak menunggu agregasi berat.
pub async fn dashboard_stok_habis(State(s): State<AppState>, a: Auth) -> AppResult<Json<Value>> {
    a.peran(ADMIN)?;
    Ok(Json(json!({ "jumlah": stok_habis_cached(&s.pool).await? })))
}

/// Lencana sidebar admin: stok habis (cache 60 detik) dan produk berharga jual <= HPP. Masing-masing `null`
/// bila gagal dihitung (menu tetap tampil tanpa lencana, kegagalan dicatat di log).
pub async fn menu_badge(State(s): State<AppState>, a: Auth) -> AppResult<Json<Value>> {
    a.peran(ADMIN)?;
    let (stok, harga) = tokio::join!(
        stok_habis_cached(&s.pool),
        db::run(&s.pool, |c| katalog::jumlah_harga_rendah(c))
    );
    if let Err(e) = &stok {
        tracing::warn!(error = %e, "lencana stok habis gagal");
    }
    if let Err(e) = &harga {
        tracing::warn!(error = %e, "lencana harga rendah gagal");
    }
    let harga = harga.ok();
    Ok(Json(json!({
        "stok_habis": stok.ok(),
        "harga_rendah": harga.map(|h| h.rugi),
        "tanpa_hpp": harga.map(|h| h.tanpa_hpp),
    })))
}

/// Field eksplisit (bukan `#[serde(flatten)]`): query string selalu string, flatten membuat angka gagal di-parse.
#[derive(Deserialize)]
pub struct ProdukQuery {
    q: Option<String>,
    kategori: Option<String>,
    aktif: Option<String>,
    /// "Y" = hanya produk aktif dengan harga jual <= harga beli (monitoring margin).
    harga_rendah: Option<String>,
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
    let harga_rendah = match q.harga_rendah.as_deref() {
        None | Some("") => false,
        Some("Y") => true,
        Some(_) => return Err(AppError::validation("Filter harga_rendah harus Y")),
    };
    let (page, size) = (pg.page(), pg.size());
    let teks = pg.q;
    let kategori = q.kategori;
    let (h, jumlah) = db::run(&s.pool, move |c| {
        let h = katalog::daftar_produk_admin(
            c,
            teks.as_deref(),
            kategori.as_deref(),
            aktif.as_deref(),
            harga_rendah,
            page,
            size,
        )?;
        let jumlah = if harga_rendah && page <= 1 {
            Some(katalog::jumlah_harga_rendah(c)?)
        } else {
            None
        };
        Ok((h, jumlah))
    })
    .await?;
    Ok(Json(json!({
        "items": h.items, "ada_lagi": h.ada_lagi, "page": page,
        // Halaman 1 dengan harga_rendah=Y: panjang daftar dan rinciannya (rugi vs belum ada harga beli).
        "jumlah": jumlah.map(|h| h.total()), "rugi": jumlah.map(|h| h.rugi), "tanpa_hpp": jumlah.map(|h| h.tanpa_hpp),
    })))
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

/// Riwayat pemakaian produk (menentukan boleh tidaknya hapus permanen); dipanggil saat dialog hapus dibuka.
pub async fn produk_jejak(State(s): State<AppState>, a: Auth, Path(id): Path<i64>) -> AppResult<Json<Value>> {
    a.peran(ADMIN)?;
    let (p, j) = db::run(&s.pool, move |c| katalog::jejak_produk(c, id)).await?;
    Ok(Json(json!({
        "id": p.id, "kode": p.kode, "nama": p.nama,
        "bisa_hapus": j.bisa_hapus(), "rincian": j.rincian(), "jejak": j,
    })))
}

#[derive(serde::Deserialize)]
pub struct SatuanBody {
    pub satuan: String,
    #[serde(with = "rust_decimal::serde::arbitrary_precision")]
    pub isi: rust_decimal::Decimal,
    #[serde(with = "rust_decimal::serde::arbitrary_precision")]
    pub harga: rust_decimal::Decimal,
    #[serde(default)]
    pub barcode: Option<String>,
}

#[derive(serde::Deserialize)]
pub struct SatuanReq {
    pub satuan: Vec<SatuanBody>,
}

fn satuan_json(v: &[katalog::SatuanJual]) -> Value {
    json!({ "satuan": v.iter().map(|s| json!({
        "satuan": s.satuan, "isi": s.isi.normalize().to_string(),
        "harga": s.harga.normalize().to_string(), "barcode": s.barcode,
    })).collect::<Vec<_>>() })
}

/// Satuan jual tambahan produk.
pub async fn produk_satuan(State(s): State<AppState>, a: Auth, Path(id): Path<i64>) -> AppResult<Json<Value>> {
    a.peran(ADMIN)?;
    let v = db::run(&s.pool, move |c| katalog::satuan_produk(c, id)).await?;
    Ok(Json(satuan_json(&v)))
}

/// Ganti seluruh satuan jual tambahan produk (daftar kosong = hanya satuan dasar).
pub async fn produk_satuan_ganti(
    State(s): State<AppState>,
    a: Auth,
    Path(id): Path<i64>,
    Json(req): Json<SatuanReq>,
) -> AppResult<Json<Value>> {
    let admin = a.peran(ADMIN)?.usr.clone();
    let daftar: Vec<katalog::SatuanJual> = req
        .satuan
        .into_iter()
        .map(|b| katalog::SatuanJual {
            satuan: b.satuan,
            isi: b.isi,
            harga: b.harga,
            barcode: b.barcode.unwrap_or_default(),
        })
        .collect();
    let v = db::run(&s.pool, move |c| katalog::ganti_satuan(c, id, &daftar)).await?;
    tracing::info!(admin = %admin, produk = id, jumlah = v.len(), "satuan jual produk diubah");
    Ok(Json(satuan_json(&v)))
}

/// Buat produk baru; kode item otomatis. Header `Idempotency-Key` wajib (permintaan ulang aman).
pub async fn produk_buat(
    State(s): State<AppState>,
    a: Auth,
    headers: axum::http::HeaderMap,
    Json(req): Json<UbahProdukReq>,
) -> AppResult<(StatusCode, Json<katalog::ProdukAdmin>)> {
    let (admin, uid) = {
        let c = a.peran(ADMIN)?;
        (c.usr.clone(), c.sub)
    };
    let key = super::idempotency_key(&headers)?;
    let p = katalog::UbahProduk {
        nama: req.nama,
        barcode: req.barcode.unwrap_or_default(),
        satuan: req.satuan,
        kategori_id: req.kategori_id,
        harga: req.harga,
        beli: req.beli,
        aktif: true,
    };
    let baru = db::run(&s.pool, move |c| katalog::buat_produk(c, uid, &key, &p)).await?;
    tracing::info!(admin = %admin, produk = baru.id, kode = %baru.kode, nama = %baru.nama, "produk dibuat");
    Ok((StatusCode::CREATED, Json(baru)))
}

/// Hapus permanen produk tanpa riwayat (422 PRODUK_BERTRANSAKSI bila sudah dipakai).
pub async fn produk_hapus(State(s): State<AppState>, a: Auth, Path(id): Path<i64>) -> AppResult<StatusCode> {
    let admin = a.peran(ADMIN)?.usr.clone();
    let p = db::run(&s.pool, move |c| katalog::hapus_produk(c, id)).await?;
    tracing::info!(admin = %admin, produk = p.id, kode = %p.kode, nama = %p.nama, "produk dihapus permanen");
    Ok(StatusCode::NO_CONTENT)
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

    #[test]
    fn query_produk_menerima_harga_rendah() {
        let uri: axum::http::Uri = "/api/v1/admin/produk?harga_rendah=Y&page=1".parse().unwrap();
        let q = axum::extract::Query::<ProdukQuery>::try_from_uri(&uri).expect("query valid");
        assert_eq!(q.0.harga_rendah.as_deref(), Some("Y"));
    }
}

// ------------------------------------------------------------------ stok habis & opname

#[derive(Deserialize)]
pub struct StokQuery {
    q: Option<String>,
    kategori: Option<String>,
    page: Option<u32>,
    size: Option<u32>,
}

/// Produk aktif dengan stok sistem <= 0. `jumlah` (total seluruh halaman) hanya ada di halaman 1.
pub async fn stok_habis(
    State(s): State<AppState>,
    a: Auth,
    axum::extract::Query(q): axum::extract::Query<StokQuery>,
) -> AppResult<Json<Value>> {
    a.peran(ADMIN)?;
    let pg = super::Paging {
        q: q.q,
        page: q.page,
        size: q.size,
    };
    let (page, size) = (pg.page(), pg.size());
    let teks = pg.q;
    let kategori = q.kategori;
    let (h, jumlah, kontrol) = db::run(&s.pool, move |c| {
        let (hari, _) = db::faktur::sekarang(c)?;
        let awal = chrono::NaiveDate::from_ymd_opt(chrono::Datelike::year(&hari), 1, 1).expect("1 Jan");
        let (h, jumlah) = katalog::stok_habis(c, teks.as_deref(), kategori.as_deref(), awal, hari, page, size)?;
        // Kontrol stok kasir mati = kasir menjual tanpa cek stok; daftar ini lalu bisa memuat barang yang tidak dilacak.
        let kontrol = katalog::setting(c, "KONTROL_SALDO_QTY")?
            .is_some_and(|v| v.trunc() > rust_decimal::Decimal::ZERO);
        Ok((h, jumlah, kontrol))
    })
    .await?;
    Ok(Json(json!({ "items": h.items, "ada_lagi": h.ada_lagi, "page": page, "jumlah": jumlah, "kontrol_stok": kontrol })))
}

#[derive(Deserialize)]
pub struct OpnameReq {
    produk_id: i64,
    qty_fisik: rust_decimal::Decimal,
}

/// Koreksi stok satu barang lewat dokumen SO (berbagi tabel/nomor dengan desktop).
pub async fn stok_opname(
    State(s): State<AppState>,
    a: Auth,
    headers: axum::http::HeaderMap,
    Json(req): Json<OpnameReq>,
) -> AppResult<Json<db::opname::HasilOpname>> {
    let (admin, uid) = {
        let c = a.peran(ADMIN)?;
        (c.usr.clone(), c.sub)
    };
    let key = super::idempotency_key(&headers)?;
    let h = db::run(&s.pool, move |c| {
        let (hari, _) = db::faktur::sekarang(c)?;
        db::opname::simpan(c, uid, &key, req.produk_id, req.qty_fisik, hari)
    })
    .await?;
    tracing::info!(admin = %admin, produk = req.produk_id, nomor_so = %h.nomor_so, selisih = %h.selisih, ulang = h.ulang, "stok opname");
    Ok(Json(h))
}

#[derive(Deserialize)]
pub struct DokumenReq {
    items: Vec<db::opname::BarisReq>,
}

/// Satu dokumen SO berisi beberapa barang (satu nomor SO). Semua-atau-tidak-sama-sekali.
pub async fn stok_opname_dokumen(
    State(s): State<AppState>,
    a: Auth,
    headers: axum::http::HeaderMap,
    Json(req): Json<DokumenReq>,
) -> AppResult<Json<db::opname::DokumenSo>> {
    let (admin, uid) = {
        let c = a.peran(ADMIN)?;
        (c.usr.clone(), c.sub)
    };
    let key = super::idempotency_key(&headers)?;
    db::opname::validasi_dokumen(&req.items)?;
    let h = db::run(&s.pool, move |c| {
        let (hari, _) = db::faktur::sekarang(c)?;
        db::opname::simpan_dokumen(c, uid, &key, &req.items, hari)
    })
    .await?;
    tracing::info!(admin = %admin, nomor_so = %h.nomor_so, baris = h.items.len(), ulang = h.ulang, "stok opname dokumen");
    Ok(Json(h))
}

#[derive(Deserialize)]
pub struct RiwayatQuery {
    tahun: Option<i32>,
    page: Option<u32>,
    size: Option<u32>,
}

/// Riwayat dokumen SO (dari desktop, mobile, dan web) per tahun, terbaru dulu.
pub async fn stok_opname_riwayat(
    State(s): State<AppState>,
    a: Auth,
    axum::extract::Query(q): axum::extract::Query<RiwayatQuery>,
) -> AppResult<Json<Value>> {
    a.peran(ADMIN)?;
    let pg = super::Paging {
        q: None,
        page: q.page,
        size: q.size,
    };
    let (page, size) = (pg.page(), pg.size());
    let tahun = q.tahun;
    if tahun.is_some_and(|t| !(2000..=2100).contains(&t)) {
        return Err(AppError::validation("Tahun tidak valid"));
    }
    let (items, lagi, tahun) = db::run(&s.pool, move |c| {
        let tahun = match tahun {
            Some(t) => t,
            None => chrono::Datelike::year(&db::faktur::sekarang(c)?.0),
        };
        let (items, lagi) = db::opname::daftar_so(c, tahun, page, size)?;
        Ok((items, lagi, tahun))
    })
    .await?;
    Ok(Json(json!({ "items": items, "ada_lagi": lagi, "page": page, "tahun": tahun })))
}

pub async fn stok_opname_detail(
    State(s): State<AppState>,
    a: Auth,
    Path(nomor): Path<String>,
) -> AppResult<Json<db::opname::DokumenSo>> {
    a.peran(ADMIN)?;
    if !db::opname::nomor_valid(&nomor) {
        return Err(AppError::validation("Nomor SO tidak valid"));
    }
    Ok(Json(db::run(&s.pool, move |c| db::opname::detail_so(c, &nomor)).await?))
}

#[derive(serde::Serialize)]
struct StokCari {
    id: i64,
    kode: String,
    barcode: String,
    nama: String,
    satuan: String,
    kategori: String,
    stok: rust_decimal::Decimal,
}

/// Cari barang aktif untuk dihitung (nama/kode/barcode), lengkap dengan stok sistem hari ini.
pub async fn stok_cari(
    State(s): State<AppState>,
    a: Auth,
    axum::extract::Query(q): axum::extract::Query<StokQuery>,
) -> AppResult<Json<Value>> {
    a.peran(ADMIN)?;
    let pg = super::Paging {
        q: q.q,
        page: q.page,
        size: q.size,
    };
    let (page, size) = (pg.page(), pg.size());
    let teks = pg.q;
    let kategori = q.kategori;
    let (items, lagi) = db::run(&s.pool, move |c| {
        let h = katalog::daftar_produk_admin(c, teks.as_deref(), kategori.as_deref(), Some("Y"), false, page, size)?;
        let (hari, _) = db::faktur::sekarang(c)?;
        let awal = chrono::NaiveDate::from_ymd_opt(chrono::Datelike::year(&hari), 1, 1).expect("1 Jan");
        let kodes: Vec<String> = h.items.iter().map(|p| p.kode.clone()).collect();
        let stok = katalog::stok_map(c, &kodes, awal, hari)?;
        let items: Vec<StokCari> = h
            .items
            .into_iter()
            .map(|p| StokCari {
                stok: stok.get(&p.kode).copied().unwrap_or_default(),
                id: p.id,
                kode: p.kode,
                barcode: p.barcode,
                nama: p.nama,
                satuan: p.satuan,
                kategori: p.kategori,
            })
            .collect();
        Ok((items, h.ada_lagi))
    })
    .await?;
    Ok(super::halaman(items, lagi, page))
}

#[cfg(test)]
mod opname_tests {
    use super::*;

    #[test]
    fn opname_req_menerima_angka_json() {
        let r: OpnameReq = serde_json::from_str(r#"{"produk_id":7,"qty_fisik":12.5}"#).unwrap();
        assert_eq!((r.produk_id, r.qty_fisik.to_string()), (7, "12.5".into()));
        let r: OpnameReq = serde_json::from_str(r#"{"produk_id":7,"qty_fisik":0}"#).unwrap();
        assert!(r.qty_fisik.is_zero());
    }

    #[test]
    fn dokumen_req_dan_riwayat_query_menerima_angka() {
        let r: DokumenReq =
            serde_json::from_str(r#"{"items":[{"produk_id":7,"qty_fisik":12.5},{"produk_id":8,"qty_fisik":0}]}"#).unwrap();
        assert_eq!(r.items.len(), 2);
        let uri: axum::http::Uri = "/api/v1/admin/stok-opname?tahun=2026&page=2&size=20".parse().unwrap();
        let q = axum::extract::Query::<RiwayatQuery>::try_from_uri(&uri).expect("query valid");
        assert_eq!((q.0.tahun, q.0.page, q.0.size), (Some(2026), Some(2), Some(20)));
    }

    #[test]
    fn stok_query_menerima_angka() {
        let uri: axum::http::Uri = "/api/v1/admin/stok-habis?q=gula&page=2&size=30&kategori=14".parse().unwrap();
        let q = axum::extract::Query::<StokQuery>::try_from_uri(&uri).expect("query valid");
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

// ------------------------------------------------------------------ rekap transaksi per anggota

/// Daftar periode yang sudah diatur (terbaru dulu) + periode hari ini (null bila di luar semua periode).
pub async fn periode_list(State(s): State<AppState>, a: Auth) -> AppResult<Json<Value>> {
    a.peran(ADMIN)?;
    let (items, sekarang) = db::run(&s.pool, |c| {
        let (hari, _) = db::faktur::sekarang(c)?;
        Ok((
            db::rekap::daftar_periode(c)?,
            db::faktur::periode_untuk_tanggal(c, hari)?,
        ))
    })
    .await?;
    Ok(Json(json!({ "items": items, "sekarang": sekarang })))
}

#[derive(Deserialize)]
pub struct RekapQuery {
    periode: Option<i32>,
    // Tanpa `#[serde(flatten)]`: angka dari query string (string) gagal di-parse bila di-flatten.
    page: Option<u32>,
    size: Option<u32>,
}

/// Rekap belanja per anggota pada satu periode (default: periode hari ini).
pub async fn rekap_anggota(
    State(s): State<AppState>,
    a: Auth,
    axum::extract::Query(q): axum::extract::Query<RekapQuery>,
) -> AppResult<Json<db::rekap::Rekap>> {
    a.peran(ADMIN)?;
    let periode = q.periode.map(super::anggota::periode_valid).transpose()?;
    let default_nik = s.cfg.default_nik.clone();
    let r = db::run(&s.pool, move |c| {
        let p = match periode {
            Some(p) => p,
            None => db::anggota::periode_sekarang(c)?,
        };
        db::rekap::rekap_anggota(c, p, &default_nik)
    })
    .await?;
    Ok(Json(r))
}

/// Transaksi satu anggota pada periode + ringkasan limit (sama dengan yang dilihat anggota di dashboardnya).
pub async fn rekap_transaksi(
    State(s): State<AppState>,
    a: Auth,
    Path(nik): Path<String>,
    axum::extract::Query(q): axum::extract::Query<RekapQuery>,
) -> AppResult<Json<Value>> {
    a.peran(ADMIN)?;
    if nik.is_empty() || nik.len() > 30 {
        return Err(AppError::validation("NIK tidak valid"));
    }
    let periode = q.periode.map(super::anggota::periode_valid).transpose()?;
    let page = q.page.unwrap_or(1).max(1);
    let size = q.size.unwrap_or(30).clamp(1, 100);
    let (ringkasan, items, lagi) = db::run(&s.pool, move |c| {
        let p = match periode {
            Some(p) => p,
            None => db::anggota::periode_sekarang(c)?,
        };
        let ringkasan = db::anggota::ringkasan(c, &nik, p)?;
        let (items, lagi) = db::anggota::transaksi(c, &nik, p, page, size)?;
        Ok((ringkasan, items, lagi))
    })
    .await?;
    Ok(Json(json!({ "ringkasan": ringkasan, "items": items, "ada_lagi": lagi, "page": page })))
}

/// Detail faktur mana pun untuk admin (rekap per anggota).
pub async fn faktur_admin(
    State(s): State<AppState>,
    a: Auth,
    Path(no): Path<String>,
) -> AppResult<Json<db::faktur::FakturResp>> {
    a.peran(ADMIN)?;
    let f = db::run(&s.pool, move |c| db::faktur::muat(c, &no)).await?;
    f.map(Json)
        .ok_or_else(|| AppError::NotFound("Faktur tidak ditemukan".into()))
}

#[derive(Deserialize)]
pub struct TagihanVsRekapQuery {
    periode: i32,
    remise: u8,
}

pub async fn tagihan_vs_rekap(
    State(s): State<AppState>,
    a: Auth,
    axum::extract::Query(q): axum::extract::Query<TagihanVsRekapQuery>,
) -> AppResult<Json<db::tagihan_rekap::Report>> {
    a.peran(ADMIN)?;
    super::anggota::periode_valid(q.periode)?;
    db::tagihan_rekap::range_columns(q.remise)?;
    Ok(Json(
        db::run(&s.pool, move |c| {
            db::tagihan_rekap::report(c, q.periode, q.remise)
        })
        .await?,
    ))
}

#[cfg(test)]
mod tagihan_rekap_access_tests {
    use super::*;
    use crate::auth::token::Claims;
    fn auth(role: Role) -> Auth {
        Auth(Claims {
            sub: 1, usr: String::new(), nama: String::new(), role,
            nik: None, ganti: false, iat: 0, exp: 0,
        })
    }
    #[test]
    fn hanya_admin_yang_bisa_melihat_laporan() {
        assert!(auth(Role::Admin).peran(ADMIN).is_ok());
        assert!(auth(Role::Kasir).peran(ADMIN).is_err());
        assert!(auth(Role::Anggota).peran(ADMIN).is_err());
    }
}
