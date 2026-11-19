use axum::{
    Json,
    extract::{Path, Query, State},
    http::HeaderMap,
};
use oracle::Connection;
use serde_json::{Value, json};

use super::{AppState, Auth, idempotency_key};
use crate::{
    auth::Role,
    db::{self, kas, pembelian as beli},
    error::AppResult,
};

async fn jalankan(
    s: AppState,
    a: Auth,
    f: impl FnOnce(&Connection, &beli::Operator) -> AppResult<Value> + Send + 'static,
) -> AppResult<Json<Value>> {
    let id = a.peran(&[Role::Admin])?.sub;
    let hasil = db::run(&s.pool, move |c| {
        let u = beli::operator(c, id, true)?;
        f(c, &u)
    })
    .await?;
    Ok(Json(hasil))
}

pub async fn akun(State(s): State<AppState>, a: Auth) -> AppResult<Json<Value>> {
    jalankan(s, a, |c, _| kas::daftar_akun(c)).await
}

pub async fn akun_buat(State(s): State<AppState>, a: Auth, Json(req): Json<kas::AkunReq>) -> AppResult<Json<Value>> {
    jalankan(s, a, move |c, _| kas::simpan_akun(c, None, &req)).await
}

pub async fn akun_ubah(
    State(s): State<AppState>,
    a: Auth,
    Path(id): Path<i64>,
    Json(req): Json<kas::AkunReq>,
) -> AppResult<Json<Value>> {
    jalankan(s, a, move |c, _| kas::simpan_akun(c, Some(id), &req)).await
}

pub async fn buku(State(s): State<AppState>, a: Auth, Query(q): Query<kas::FilterBuku>) -> AppResult<Json<Value>> {
    jalankan(s, a, move |c, _| kas::buku(c, &q)).await
}

pub async fn manual(
    State(s): State<AppState>,
    a: Auth,
    headers: HeaderMap,
    Json(req): Json<kas::ManualReq>,
) -> AppResult<Json<Value>> {
    let key = idempotency_key(&headers)?;
    jalankan(s, a, move |c, u| Ok(json!({ "id": kas::manual(c, u, &key, &req)? }))).await
}

pub async fn transfer(
    State(s): State<AppState>,
    a: Auth,
    headers: HeaderMap,
    Json(req): Json<kas::TransferReq>,
) -> AppResult<Json<Value>> {
    let key = idempotency_key(&headers)?;
    jalankan(s, a, move |c, u| Ok(json!({ "ref": kas::transfer(c, u, &key, &req)? }))).await
}

pub async fn batal(
    State(s): State<AppState>,
    a: Auth,
    Path(id): Path<i64>,
    headers: HeaderMap,
    Json(req): Json<kas::Alasan>,
) -> AppResult<Json<Value>> {
    let key = idempotency_key(&headers)?;
    jalankan(s, a, move |c, u| Ok(json!({ "id": kas::batal(c, u, id, &key, &req)? }))).await
}

pub async fn tarik(State(s): State<AppState>, a: Auth, Json(req): Json<kas::TarikReq>) -> AppResult<Json<Value>> {
    jalankan(s, a, move |c, u| kas::tarik(c, u, &req)).await
}
