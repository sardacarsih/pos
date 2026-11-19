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
    db::{self, pembelian as beli},
    error::AppResult,
};

async fn jalankan(
    s: AppState,
    a: Auth,
    admin: bool,
    f: impl FnOnce(&Connection, &beli::Operator) -> AppResult<Value> + Send + 'static,
) -> AppResult<Json<Value>> {
    let roles: &[Role] = if admin {
        &[Role::Admin]
    } else {
        &[Role::Admin, Role::Pembelian]
    };
    let id = a.peran(roles)?.sub;
    let hasil = db::run(&s.pool, move |c| {
        let u = beli::operator(c, id, admin)?;
        f(c, &u)
    })
    .await?;
    Ok(Json(hasil))
}

pub async fn referensi(State(s): State<AppState>, a: Auth) -> AppResult<Json<Value>> {
    jalankan(s, a, false, |c, _| beli::referensi(c)).await
}

pub async fn riwayat(
    State(s): State<AppState>,
    a: Auth,
    Query(q): Query<beli::Filter>,
) -> AppResult<Json<Value>> {
    jalankan(s, a, false, move |c, u| beli::riwayat(c, u, &q)).await
}

pub async fn detail(
    State(s): State<AppState>,
    a: Auth,
    Path(no): Path<String>,
) -> AppResult<Json<Value>> {
    jalankan(s, a, false, move |c, u| beli::detail(c, u, &no)).await
}

pub async fn posting(
    State(s): State<AppState>,
    a: Auth,
    headers: HeaderMap,
    Json(req): Json<beli::Posting>,
) -> AppResult<Json<Value>> {
    let key = idempotency_key(&headers)?;
    jalankan(s, a, false, move |c, u| {
        Ok(json!({"no_transaksi":beli::posting(c, u, &key, &req)?}))
    })
    .await
}

pub async fn batal(
    State(s): State<AppState>,
    a: Auth,
    Path(no): Path<String>,
    headers: HeaderMap,
    Json(req): Json<beli::Alasan>,
) -> AppResult<Json<Value>> {
    let key = idempotency_key(&headers)?;
    jalankan(s, a, true, move |c, u| {
        Ok(json!({"no_transaksi":beli::batal(c, u, &no, &key, &req)?}))
    })
    .await
}

pub async fn hutang(
    State(s): State<AppState>,
    a: Auth,
    Query(q): Query<beli::Filter>,
) -> AppResult<Json<Value>> {
    jalankan(s, a, true, move |c, u| beli::hutang(c, u, &q)).await
}

pub async fn bayar(
    State(s): State<AppState>,
    a: Auth,
    Path(no): Path<String>,
    headers: HeaderMap,
    Json(req): Json<beli::Bayar>,
) -> AppResult<Json<Value>> {
    let key = idempotency_key(&headers)?;
    jalankan(s, a, true, move |c, u| {
        Ok(json!({"id":beli::bayar(c, u, &no, &key, &req)?}))
    })
    .await
}

pub async fn balik(
    State(s): State<AppState>,
    a: Auth,
    Path((no, id)): Path<(String, i64)>,
    headers: HeaderMap,
    Json(req): Json<beli::Alasan>,
) -> AppResult<Json<Value>> {
    let key = idempotency_key(&headers)?;
    jalankan(s, a, true, move |c, u| {
        Ok(json!({"id":beli::balik(c, u, &no, id, &key, &req)?}))
    })
    .await
}
