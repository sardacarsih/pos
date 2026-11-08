use axum::{
    Json,
    extract::{Path, Query, State},
};
use serde::{Deserialize, Serialize};
use serde_json::{Value, json};

use super::{AppState, Auth, Paging, halaman};
use crate::{
    auth::Role,
    db::{self, faktur, katalog, opt, tampil, user},
    error::{AppError, AppResult},
};

const KASIR: &[Role] = &[Role::Kasir];
const KASIR_ADMIN: &[Role] = &[Role::Kasir, Role::Admin];

pub async fn periode_status(State(s): State<AppState>, a: Auth) -> AppResult<Json<Value>> {
    a.peran(KASIR)?;
    db::run(&s.pool, |c| {
        let (hari, _) = faktur::sekarang(c)?;
        let tanggal = hari.format("%Y-%m-%d").to_string();
        match faktur::cek_periode(c, hari) {
            Ok(()) => Ok(Json(json!({ "tanggal": tanggal, "terbuka": true }))),
            Err(AppError::Rule { pesan, .. }) => Ok(Json(
                json!({ "tanggal": tanggal, "terbuka": false, "pesan": pesan }),
            )),
            Err(e) => Err(e),
        }
    })
    .await
}

pub async fn nomor(State(s): State<AppState>, a: Auth) -> AppResult<Json<Value>> {
    let id = a.peran(KASIR)?.sub;
    let no = db::run(&s.pool, move |c| {
        let (hari, _) = faktur::sekarang(c)?;
        faktur::cek_periode(c, hari)?;
        faktur::terbitkan_nomor(c, id)
    })
    .await?;
    Ok(Json(json!({ "no_transaksi": no })))
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
        }
    }
}

#[derive(Deserialize)]
pub struct BarangQuery {
    #[serde(flatten)]
    pg: Paging,
    kategori: Option<String>,
}

async fn kategori_user(s: &AppState, id: i64) -> AppResult<Vec<String>> {
    db::run(&s.pool, move |c| user::allowed_kategori(c, id)).await
}

pub async fn barang(
    State(s): State<AppState>,
    a: Auth,
    Query(q): Query<BarangQuery>,
) -> AppResult<Json<Value>> {
    let id = a.peran(KASIR)?.sub;
    let (page, size) = (q.pg.page(), q.pg.size());
    let teks = q.pg.q.clone();
    let chip = q.kategori.clone();
    let (items, lagi) = db::run(&s.pool, move |c| {
        let allowed = user::allowed_kategori(c, id)?;
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
    let id = a.peran(KASIR)?.sub;
    if kode.is_empty() || kode.len() > 64 {
        return Err(AppError::validation("Barcode tidak valid"));
    }
    let r = db::run(&s.pool, move |c| {
        let allowed = user::allowed_kategori(c, id)?;
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
    let id = a.peran(KASIR)?.sub;
    let r = db::run(&s.pool, move |c| {
        let allowed = user::allowed_kategori(c, id)?;
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

pub async fn simpan(
    State(s): State<AppState>,
    a: Auth,
    Json(req): Json<faktur::SimpanReq>,
) -> AppResult<Json<faktur::FakturResp>> {
    let cl = a.peran(KASIR)?.clone();
    if req.baris.len() > 200 {
        return Err(AppError::validation("Terlalu banyak baris"));
    }
    if req.baris.iter().any(|b| b.qty <= 0) {
        return Err(AppError::validation("Qty harus lebih dari 0"));
    }
    let kategori = kategori_user(&s, cl.sub).await?;
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
            },
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
