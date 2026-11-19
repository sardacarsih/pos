//! Pinjaman: endpoint anggota (/saya/pinjaman, NIK dari token) dan pengurus (/pinjaman, ADMIN + hak tahap).
use axum::{
    Json,
    extract::{Path, Query, State},
    http::HeaderMap,
};
use oracle::Connection;
use serde::Deserialize;
use serde_json::{Value, json};

use super::{AppState, Auth, anggota::periode_valid, idempotency_key};
use crate::{
    auth::Role,
    db::{self, pembelian as beli, pinjaman as p},
    error::{AppError, AppResult},
};

async fn pengurus(
    s: AppState,
    a: Auth,
    f: impl FnOnce(&Connection, &beli::Operator) -> AppResult<Value> + Send + 'static,
) -> AppResult<Json<Value>> {
    let id = a.peran(&[Role::Admin])?.sub;
    Ok(Json(db::run(&s.pool, move |c| f(c, &beli::operator(c, id, true)?)).await?))
}

async fn anggota(
    s: AppState,
    a: Auth,
    f: impl FnOnce(&Connection, &beli::Operator, &str) -> AppResult<Value> + Send + 'static,
) -> AppResult<Json<Value>> {
    let cl = a.peran(&[Role::Anggota])?;
    let nik = cl.nik.clone().ok_or_else(|| AppError::Forbidden("Akun belum terhubung ke data anggota".into()))?;
    let u = beli::Operator { id: cl.sub, username: cl.usr.clone(), nama: cl.nama.clone(), role: Role::Anggota };
    Ok(Json(db::run(&s.pool, move |c| f(c, &u, &nik)).await?))
}

// ---------- Anggota ----------

pub async fn saya(State(s): State<AppState>, a: Auth) -> AppResult<Json<Value>> {
    anggota(s, a, |c, _, nik| p::saya(c, nik)).await
}

#[derive(Deserialize)]
pub struct SimulasiReq {
    jumlah: i64,
    tenor: i32,
}

pub async fn simulasi(State(s): State<AppState>, a: Auth, Json(r): Json<SimulasiReq>) -> AppResult<Json<Value>> {
    anggota(s, a, move |c, _, _| p::simulasi(c, r.jumlah, r.tenor)).await
}

pub async fn ajukan(State(s): State<AppState>, a: Auth, h: HeaderMap, Json(r): Json<p::AjuReq>) -> AppResult<Json<Value>> {
    let key = idempotency_key(&h)?;
    anggota(s, a, move |c, u, nik| Ok(json!({ "id": p::ajukan(c, u, nik, &key, &r)? }))).await
}

pub async fn batal(State(s): State<AppState>, a: Auth, h: HeaderMap, Path(id): Path<i64>) -> AppResult<Json<Value>> {
    let key = idempotency_key(&h)?;
    anggota(s, a, move |c, u, nik| Ok(json!({ "id": p::batal(c, u, nik, id, &key)? }))).await
}

// ---------- Pengurus ----------

pub async fn daftar(State(s): State<AppState>, a: Auth, Query(f): Query<p::Filter>) -> AppResult<Json<Value>> {
    pengurus(s, a, move |c, u| {
        let mut v = p::daftar(c, &f)?;
        v["hak"] = json!(p::hak_saya(c, u)?);
        v["user_id"] = json!(u.id);
        Ok(v)
    })
    .await
}

pub async fn detail(State(s): State<AppState>, a: Auth, Path(id): Path<i64>) -> AppResult<Json<Value>> {
    pengurus(s, a, move |c, _| p::detail(c, id)).await
}

pub async fn proses(
    State(s): State<AppState>,
    a: Auth,
    h: HeaderMap,
    Path((id, aksi)): Path<(i64, p::Aksi)>,
    Json(r): Json<p::ProsesReq>,
) -> AppResult<Json<Value>> {
    let key = idempotency_key(&h)?;
    pengurus(s, a, move |c, u| Ok(json!({ "hasil": p::proses(c, u, id, aksi, &key, &r)? }))).await
}

pub async fn antrian(State(s): State<AppState>, a: Auth) -> AppResult<Json<Value>> {
    pengurus(s, a, |c, _| p::antrian(c)).await
}

pub async fn plafon(
    State(s): State<AppState>,
    a: Auth,
    Path(periode): Path<i32>,
    Json(r): Json<p::PlafonReq>,
) -> AppResult<Json<Value>> {
    let periode = periode_valid(periode)?;
    pengurus(s, a, move |c, u| p::simpan_plafon(c, u, periode, &r)).await
}

pub async fn pengaturan(State(s): State<AppState>, a: Auth) -> AppResult<Json<Value>> {
    pengurus(s, a, |c, _| {
        let mut v = p::daftar_hak(c)?;
        v["aturan"] = json!(p::aturan(c)?);
        Ok(v)
    })
    .await
}

pub async fn hak(State(s): State<AppState>, a: Auth, Json(r): Json<p::HakReq>) -> AppResult<Json<Value>> {
    pengurus(s, a, move |c, _| p::simpan_hak(c, &r)).await
}

#[derive(Deserialize)]
pub struct MaksReq {
    maks_pinjaman: i64,
}

pub async fn maks(State(s): State<AppState>, a: Auth, Json(r): Json<MaksReq>) -> AppResult<Json<Value>> {
    pengurus(s, a, move |c, _| p::simpan_maks_pinjaman(c, r.maks_pinjaman)).await
}
