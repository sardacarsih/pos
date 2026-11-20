//! Endpoint admin modul Persediaan (kartu stok, penyesuaian + balik, valuasi, ambang stok minimum).
//! Semua ADMIN-only; tulis memakai Idempotency-Key UUIDv4 seperti opname/pembelian.
use axum::{
    Json,
    extract::{Path, Query, State},
    http::HeaderMap,
};
use chrono::{Datelike, NaiveDate};
use serde::Deserialize;
use serde_json::{Value, json};

use super::{AppState, Auth, Paging, halaman, idempotency_key};
use crate::{
    auth::Role,
    db::{self, persediaan},
    error::{AppError, AppResult},
};

const ADMIN: &[Role] = &[Role::Admin];

fn tanggal(s: &str) -> AppResult<NaiveDate> {
    NaiveDate::parse_from_str(s.trim(), "%Y-%m-%d")
        .map_err(|_| AppError::validation("Tanggal tidak valid (YYYY-MM-DD)"))
}

#[derive(Deserialize)]
pub struct KartuQuery {
    produk_id: i64,
    dari: Option<String>,
    sampai: Option<String>,
    page: Option<u32>,
    size: Option<u32>,
}

pub async fn kartu(State(s): State<AppState>, a: Auth, Query(q): Query<KartuQuery>) -> AppResult<Json<persediaan::KartuStok>> {
    a.peran(ADMIN)?;
    let pg = Paging { q: None, page: q.page, size: q.size };
    let (page, size) = (pg.page(), pg.size());
    let dari = q.dari.as_deref().map(tanggal).transpose()?;
    let sampai = q.sampai.as_deref().map(tanggal).transpose()?;
    let h = db::run(&s.pool, move |c| {
        let (hari, _) = db::faktur::sekarang(c)?;
        persediaan::kartu_stok(c, q.produk_id, dari, sampai, hari, page, size)
    })
    .await?;
    Ok(Json(h))
}

pub async fn daftar(State(s): State<AppState>, a: Auth, Query(pg): Query<Paging>) -> AppResult<Json<Value>> {
    a.peran(ADMIN)?;
    let (page, size) = (pg.page(), pg.size());
    let h = db::run(&s.pool, move |c| persediaan::daftar_sesuai(c, pg.q.as_deref(), page, size)).await?;
    Ok(super::halaman(h.items, h.ada_lagi, page))
}

pub async fn buat(
    State(s): State<AppState>,
    a: Auth,
    headers: HeaderMap,
    Json(req): Json<persediaan::SesuaiReq>,
) -> AppResult<Json<persediaan::HasilSesuai>> {
    let (uid, nama) = {
        let c = a.peran(ADMIN)?;
        let nama = if c.nama.trim().is_empty() { c.usr.clone() } else { c.nama.clone() };
        (c.sub, nama)
    };
    let key = idempotency_key(&headers)?;
    let produk_id = req.produk_id;
    let h = db::run(&s.pool, move |c| {
        let (hari, _) = db::faktur::sekarang(c)?;
        persediaan::sesuaikan(c, uid, &nama, &key, &req, hari)
    })
    .await?;
    tracing::info!(user = uid, produk = produk_id, id = h.id, qty = %h.qty, ulang = h.ulang, "penyesuaian stok");
    Ok(Json(h))
}

#[derive(Deserialize)]
#[serde(deny_unknown_fields)]
pub struct AlasanReq {
    alasan: String,
}

pub async fn balik(
    State(s): State<AppState>,
    a: Auth,
    Path(id): Path<i64>,
    headers: HeaderMap,
    Json(req): Json<AlasanReq>,
) -> AppResult<Json<persediaan::HasilSesuai>> {
    let (uid, nama) = {
        let c = a.peran(ADMIN)?;
        let nama = if c.nama.trim().is_empty() { c.usr.clone() } else { c.nama.clone() };
        (c.sub, nama)
    };
    let key = idempotency_key(&headers)?;
    let h = db::run(&s.pool, move |c| {
        let (hari, _) = db::faktur::sekarang(c)?;
        persediaan::balik(c, uid, &nama, &key, id, &req.alasan, hari)
    })
    .await?;
    tracing::info!(user = uid, asal = id, id = h.id, qty = %h.qty, ulang = h.ulang, "balik penyesuaian stok");
    Ok(Json(h))
}

#[derive(Deserialize)]
pub struct ValuasiQuery {
    kategori: Option<String>,
}

pub async fn valuasi(State(s): State<AppState>, a: Auth, Query(q): Query<ValuasiQuery>) -> AppResult<Json<persediaan::Valuasi>> {
    a.peran(ADMIN)?;
    let h = db::run(&s.pool, move |c| {
        let (hari, _) = db::faktur::sekarang(c)?;
        let awal = NaiveDate::from_ymd_opt(hari.year(), 1, 1).expect("1 Jan");
        persediaan::valuasi(c, q.kategori.as_deref(), awal, hari)
    })
    .await?;
    Ok(Json(h))
}

pub async fn valuasi_item(State(s): State<AppState>, a: Auth, Query(pg): Query<Paging>) -> AppResult<Json<Value>> {
    a.peran(ADMIN)?;
    let (page, size) = (pg.page(), pg.size());
    let h = db::run(&s.pool, move |c| {
        let (hari, _) = db::faktur::sekarang(c)?;
        let awal = NaiveDate::from_ymd_opt(hari.year(), 1, 1).expect("1 Jan");
        persediaan::valuasi_item(c, pg.q.as_deref(), awal, hari, page, size)
    })
    .await?;
    Ok(halaman(h.items, h.ada_lagi, page))
}

#[derive(Deserialize)]
pub struct StokMinQuery {
    q: Option<String>,
    kategori: Option<String>,
    page: Option<u32>,
    size: Option<u32>,
}

pub async fn stok_minimum(State(s): State<AppState>, a: Auth, Query(q): Query<StokMinQuery>) -> AppResult<Json<Value>> {
    a.peran(ADMIN)?;
    let pg = Paging { q: q.q, page: q.page, size: q.size };
    let (page, size) = (pg.page(), pg.size());
    let (h, jumlah) = db::run(&s.pool, move |c| {
        let (hari, _) = db::faktur::sekarang(c)?;
        let awal = NaiveDate::from_ymd_opt(hari.year(), 1, 1).expect("1 Jan");
        persediaan::stok_minimum(c, pg.q.as_deref(), q.kategori.as_deref(), awal, hari, page, size)
    })
    .await?;
    Ok(Json(json!({ "items": h.items, "ada_lagi": h.ada_lagi, "page": page, "jumlah": jumlah })))
}

#[derive(Deserialize)]
#[serde(deny_unknown_fields)]
pub struct StokMinReq {
    stok_min: Option<rust_decimal::Decimal>,
}

pub async fn set_stok_min(
    State(s): State<AppState>,
    a: Auth,
    Path(produk_id): Path<i64>,
    Json(req): Json<StokMinReq>,
) -> AppResult<Json<Value>> {
    let uid = a.peran(ADMIN)?.sub;
    db::run(&s.pool, move |c| persediaan::set_stok_min(c, produk_id, req.stok_min)).await?;
    tracing::info!(user = uid, produk = produk_id, kosong = req.stok_min.is_none(), "ambang stok minimum");
    Ok(Json(json!({ "ok": true })))
}
