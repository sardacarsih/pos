use axum::{
    Json,
    extract::{Path, Query, State},
    http::HeaderMap,
};
use serde::{Deserialize, Serialize};
use serde_json::{Value, json};

use super::{AppState, Auth, Paging, halaman, idempotency_key};
use crate::{
    auth::Role,
    db::{self, faktur, katalog, opt, tampil, user},
    error::{AppError, AppResult},
};

const KASIR: &[Role] = &[Role::Kasir];
const KASIR_ADMIN: &[Role] = &[Role::Kasir, Role::Admin];
/// Katalog barang read-only: juga dipakai anggota untuk belanja mandiri.
const KATALOG: &[Role] = &[Role::Kasir, Role::Anggota];

pub async fn periode_status(State(s): State<AppState>, a: Auth) -> AppResult<Json<Value>> {
    a.peran(KASIR)?;
    db::run(&s.pool, |c| {
        let (hari, _) = faktur::sekarang(c)?;
        let tanggal = hari.format("%Y-%m-%d").to_string();
        match faktur::cek_periode(c, hari) {
            Ok(()) => Ok(Json(json!({ "tanggal": tanggal, "terbuka": true }))),
            Err(AppError::Rule { kode, pesan }) => Ok(Json(
                json!({ "tanggal": tanggal, "terbuka": false, "pesan": pesan, "kode": kode }),
            )),
            Err(e) => Err(e),
        }
    })
    .await
}

pub async fn dashboard(State(s): State<AppState>, a: Auth) -> AppResult<Json<Value>> {
    let usr = a.peran(KASIR)?.usr.clone();
    let d = db::run(&s.pool, move |c| db::dashboard::dashboard_kasir(c, &usr)).await?;
    Ok(Json(json!(d)))
}

#[derive(Deserialize)]
pub struct RiwayatQuery {
    tanggal: Option<String>,
}

/// Riwayat transaksi kasir per tanggal (default hari ini; tidak boleh di masa depan).
pub async fn riwayat(
    State(s): State<AppState>,
    a: Auth,
    Query(q): Query<RiwayatQuery>,
) -> AppResult<Json<Value>> {
    let (id, usr) = {
        let c = a.peran(KASIR)?;
        (c.sub, c.usr.clone())
    };
    let minta = match q.tanggal.as_deref().filter(|t| !t.is_empty()) {
        None => None,
        Some(t) => Some(
            chrono::NaiveDate::parse_from_str(t, "%Y-%m-%d")
                .map_err(|_| AppError::validation("Tanggal harus berformat YYYY-MM-DD"))?,
        ),
    };
    let (tgl, items) = db::run(&s.pool, move |c| {
        let (hari, _) = faktur::sekarang(c)?;
        let tgl = minta.unwrap_or(hari);
        if tgl > hari {
            return Err(AppError::validation("Tanggal tidak boleh di masa depan"));
        }
        Ok((tgl, db::dashboard::riwayat_kasir(c, id, &usr, tgl)?))
    })
    .await?;
    Ok(Json(json!({ "tanggal": tgl.format("%Y-%m-%d").to_string(), "items": items })))
}

pub async fn nomor(
    State(s): State<AppState>,
    a: Auth,
    headers: HeaderMap,
) -> AppResult<Json<Value>> {
    let id = a.peran(KASIR)?.sub;
    let key = idempotency_key(&headers)?;
    let no =db::run(&s.pool, move |c| faktur::terbitkan_nomor(c, id, &key)).await?;
    Ok(Json(json!({ "no_transaksi": no.0, "duplikat": no.1 })))
}

#[derive(Serialize)]
pub struct BarangResp {
    id: i64,
    kode: String,
    barcode: String,
    nama: String,
    satuan: String,
    harga: i64,
    /// null bila stok tidak dihitung.
    stok: Option<i64>,
    kategori_id: String,
    kategori: String,
    /// Versi foto unggahan (epoch detik) atau null; ambil di GET /produk/{id}/foto.
    foto: Option<i64>,
}

impl BarangResp {
    fn dari(p: katalog::Produk, stok: Option<i64>) -> Self {
        Self {
            id: p.id,
            kode: p.kode,
            barcode: p.barcode,
            nama: p.nama,
            satuan: p.satuan,
            harga: tampil(p.harga),
            stok,
            kategori_id: p.kategori_id,
            kategori: p.kategori,
            foto: p.foto_versi,
        }
    }
}

#[derive(Deserialize)]
pub struct BarangQuery {
    // Tidak memakai `#[serde(flatten)] Paging`: query string berisi string, dan flatten membuat angka gagal di-parse.
    q: Option<String>,
    page: Option<u32>,
    size: Option<u32>,
    kategori: Option<String>,
}


pub async fn barang(
    State(s): State<AppState>,
    a: Auth,
    Query(q): Query<BarangQuery>,
) -> AppResult<Json<Value>> {
    a.peran(KATALOG)?;
    // Pengaturan kategori tampil (MOB_KATEGORI_TAMPIL) hanya untuk kasir mobile; anggota melihat semua produk.
    let semua = a.0.role == Role::Anggota;
    let pg = Paging {
        q: q.q,
        page: q.page,
        size: q.size,
    };
    let (page, size) = (pg.page(), pg.size());
    let teks = pg.q;
    let chip = q.kategori;
    let (items, lagi) = db::run(&s.pool, move |c| {
        let allowed = if semua { Vec::new() } else { user::allowed_kategori(c)? };
        // Chip di luar kategori yang diizinkan -> hasil kosong (bukan bocor).
        if let Some(ch) = &chip
            && !allowed.is_empty()
            && !allowed.contains(ch)
        {
            return Ok((Vec::new(), false));
        }
        let h = katalog::cari_barang(c, teks.as_deref(), chip.as_deref(), &allowed, page, size)?;
        let (hari, _) = faktur::sekarang(c)?;
        let awal =
            chrono::NaiveDate::from_ymd_opt(chrono::Datelike::year(&hari), 1, 1).expect("1 Jan");
        let kodes: Vec<String> = h.items.iter().map(|p| p.kode.clone()).collect();
        let stok = katalog::stok_map(c, &kodes, awal, hari)?;
        let items: Vec<BarangResp> = h
            .items
            .into_iter()
            .map(|p| {
                let st = stok.get(&p.kode).map(|d| tampil(*d));
                BarangResp::dari(p, st)
            })
            .collect();
        Ok((items, h.ada_lagi))
    })
    .await?;
    Ok(halaman(items, lagi, page))
}

/// Hasil scan barcode: jalur sama dengan daftar (filter kategori, harga, stok).
pub async fn barang_barcode(
    State(s): State<AppState>,
    a: Auth,
    Path(kode): Path<String>,
) -> AppResult<Json<BarangResp>> {
    a.peran(KATALOG)?;
    // Pengaturan kategori tampil (MOB_KATEGORI_TAMPIL) hanya untuk kasir mobile; anggota melihat semua produk.
    let semua = a.0.role == Role::Anggota;
    if kode.is_empty() || kode.len() > 64 {
        return Err(AppError::validation("Barcode tidak valid"));
    }
    let r = db::run(&s.pool, move |c| {
        let allowed = if semua { Vec::new() } else { user::allowed_kategori(c)? };
        let p = katalog::barang_by_barcode(c, &kode, &allowed)?
            .ok_or_else(|| AppError::NotFound("Barang tidak ditemukan".into()))?;
        let (hari, _) = faktur::sekarang(c)?;
        let awal =
            chrono::NaiveDate::from_ymd_opt(chrono::Datelike::year(&hari), 1, 1).expect("1 Jan");
        let stok = katalog::stok_map(c, std::slice::from_ref(&p.kode), awal, hari)?;
        let st = stok.get(&p.kode).map(|d| tampil(*d));
        Ok(BarangResp::dari(p, st))
    })
    .await?;
    Ok(Json(r))
}

pub async fn kategori(
    State(s): State<AppState>,
    a: Auth,
) -> AppResult<Json<Vec<katalog::Kategori>>> {
    a.peran(KATALOG)?;
    // Pengaturan kategori tampil (MOB_KATEGORI_TAMPIL) hanya untuk kasir mobile; anggota melihat semua produk.
    let semua = a.0.role == Role::Anggota;
    let r = db::run(&s.pool, move |c| {
        let allowed = if semua { Vec::new() } else { user::allowed_kategori(c)? };
        katalog::kategori_diizinkan(c, &allowed)
    })
    .await?;
    Ok(Json(r))
}

#[derive(Serialize)]
pub struct PelangganResp {
    id: i64,
    nik: String,
    nama: String,
    unit_kerja: String,
    unit_nama: String,
    status: String,
    limit: i64,
    tunai: bool,
}

fn pel_resp(p: katalog::Pelanggan, default_nik: &str) -> PelangganResp {
    PelangganResp {
        tunai: p.nik == default_nik,
        id: p.id,
        nik: p.nik,
        nama: p.nama,
        unit_kerja: p.unit_kerja,
        unit_nama: p.unit_nama,
        status: p.status,
        limit: tampil(p.limit),
    }
}

pub async fn pelanggan(
    State(s): State<AppState>,
    a: Auth,
    Query(q): Query<Paging>,
) -> AppResult<Json<Value>> {
    a.peran(KASIR_ADMIN)?;
    let (page, size, teks) = (q.page(), q.size(), q.q.clone());
    let h = db::run(&s.pool, move |c| {
        katalog::cari_pelanggan(c, teks.as_deref(), page, size)
    })
    .await?;
    let nik = s.cfg.default_nik.clone();
    let items: Vec<PelangganResp> = h.items.into_iter().map(|p| pel_resp(p, &nik)).collect();
    Ok(halaman(items, h.ada_lagi, page))
}

/// Scan kartu anggota / barcode NIK: cocok dengan pelanggan aktif atau 404 (klien tidak mengubah pilihan).
pub async fn pelanggan_nik(
    State(s): State<AppState>,
    a: Auth,
    Path(nik): Path<String>,
) -> AppResult<Json<PelangganResp>> {
    a.peran(KASIR_ADMIN)?;
    if nik.is_empty() || nik.len() > 30 {
        return Err(AppError::validation("NIK tidak valid"));
    }
    let p = db::run(&s.pool, move |c| katalog::pelanggan_by_nik(c, nik.trim())).await?;
    let p = p.ok_or_else(|| AppError::NotFound("Pelanggan tidak ditemukan".into()))?;
    Ok(Json(pel_resp(p, &s.cfg.default_nik)))
}

/// Foto profil anggota untuk verifikasi identitas di kasir. Hanya KASIR; hanya foto (tanpa kontak).
pub async fn foto_anggota(
    State(s): State<AppState>,
    a: Auth,
    Path(nik): Path<String>,
) -> AppResult<axum::response::Response> {
    use axum::{http::header, response::IntoResponse};
    a.peran(KASIR)?;
    if nik.is_empty() || nik.len() > 30 {
        return Err(AppError::validation("NIK tidak valid"));
    }
    let f = db::run(&s.pool, move |c| db::profil::ambil_foto_by_nik(c, nik.trim()))
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

pub async fn simpan(
    State(s): State<AppState>,
    a: Auth,
    headers: HeaderMap,
    Json(req): Json<faktur::SimpanReq>,
) -> AppResult<Json<faktur::FakturResp>> {
    let cl = a.peran(KASIR)?.clone();
    let key = idempotency_key(&headers)?;
    let kategori = db::run(&s.pool, user::allowed_kategori).await?;
    let nik_default = s.cfg.default_nik.clone();
    let max_tenor = s.cfg.default_max_tenor;
    let f = db::run(&s.pool, move |c| {
        let kasir = faktur::Kasir {
            id: cl.sub,
            username: cl.usr,
            nama: cl.nama,
            kategori,
        };
        faktur::simpan(
            c,
            &kasir,
            &req,
            &faktur::Opsi {
                default_nik: &nik_default,
                default_max_tenor: max_tenor,
                demo: false,
            },
            &key,
        )
    })
    .await?;
    Ok(Json(f))
}

/// Cetak ulang struk: hanya faktur yang nomornya diterbitkan ke kasir ini.
pub async fn faktur(
    State(s): State<AppState>,
    a: Auth,
    Path(no): Path<String>,
) -> AppResult<Json<faktur::FakturResp>> {
    let id = a.peran(KASIR)?.sub;
    let f = db::run(&s.pool, move |c| {
        let milik = opt(c.query_row_named("SELECT 1 FROM MOB_NOMOR WHERE NO_TRANSAKSI = :n AND USER_ID = :u AND USED_AT IS NOT NULL", &[("n", &no), ("u", &id)]))?;
        if milik.is_none() {
            return Ok(None);
        }
        faktur::muat(c, &no)
    })
    .await?;
    f.map(Json)
        .ok_or_else(|| AppError::NotFound("Faktur tidak ditemukan".into()))
}

#[cfg(test)]
mod tests {
    use super::*;

    // Query string selalu string; `flatten` membuat angka gagal di-parse (400 teks biasa).
    #[test]
    fn query_barang_menerima_angka() {
        let uri: axum::http::Uri = "/api/v1/barang?page=2&size=30&kategori=A&q=x"
            .parse()
            .unwrap();
        let q = Query::<BarangQuery>::try_from_uri(&uri).expect("query barang valid");
        assert_eq!((q.0.page, q.0.size), (Some(2), Some(30)));
    }
}

/// Foto produk unggahan admin; kasir dan admin boleh mengambil. 404 bila tidak ada.
pub async fn foto_ambil(
    State(s): State<AppState>,
    a: Auth,
    Path(id): Path<i64>,
) -> AppResult<axum::response::Response> {
    use axum::{http::header, response::IntoResponse};
    a.peran(&[Role::Kasir, Role::Admin, Role::Anggota])?;
    let f = db::run(&s.pool, move |c| db::foto::ambil(c, id))
        .await?
        .ok_or_else(|| AppError::NotFound("Foto tidak ada".into()))?;
    Ok((
        [
            (header::CONTENT_TYPE, f.content_type),
            (header::CACHE_CONTROL, "private, max-age=86400".to_string()),
        ],
        f.data,
    )
        .into_response())
}
