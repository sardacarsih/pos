//! Data milik anggota sendiri. NIK selalu dari token (claims.nik), tidak pernah dari klien.
use axum::{
    Json,
    extract::{Path, Query, State},
};
use serde::Deserialize;
use serde_json::Value;

use super::{AppState, Auth, Paging, halaman};
use crate::{
    auth::Role,
    db::{self, anggota},
    error::{AppError, AppResult},
};

const ANGGOTA: &[Role] = &[Role::Anggota];

fn nik_dari_token(a: &Auth) -> AppResult<String> {
    a.peran(ANGGOTA)?;
    a.0.nik
        .clone()
        .ok_or_else(|| AppError::Forbidden("Akun belum terhubung ke data anggota".into()))
}

#[derive(Deserialize)]
pub struct PeriodeQuery {
    periode: Option<i32>,
    #[serde(flatten)]
    pg: Paging,
}

fn periode_valid(p: i32) -> AppResult<i32> {
    if (200001..=299912).contains(&p) && (1..=12).contains(&(p % 100)) {
        Ok(p)
    } else {
        Err(AppError::validation("Periode harus berformat yyyyMM"))
    }
}

pub async fn ringkasan(
    State(s): State<AppState>,
    a: Auth,
    Query(q): Query<PeriodeQuery>,
) -> AppResult<Json<anggota::Ringkasan>> {
    let nik = nik_dari_token(&a)?;
    let periode = q.periode.map(periode_valid).transpose()?;
    let r = db::run(&s.pool, move |c| {
        let p = match periode {
            Some(p) => p,
            None => anggota::periode_sekarang(c)?,
        };
        anggota::ringkasan(c, &nik, p)
    })
    .await?;
    Ok(Json(r))
}

pub async fn transaksi(
    State(s): State<AppState>,
    a: Auth,
    Query(q): Query<PeriodeQuery>,
) -> AppResult<Json<Value>> {
    let nik = nik_dari_token(&a)?;
    let periode = q.periode.map(periode_valid).transpose()?;
    let (page, size) = (q.pg.page(), q.pg.size());
    let (items, lagi) = db::run(&s.pool, move |c| {
        let p = match periode {
            Some(p) => p,
            None => anggota::periode_sekarang(c)?,
        };
        anggota::transaksi(c, &nik, p, page, size)
    })
    .await?;
    Ok(halaman(items, lagi, page))
}

pub async fn detail(
    State(s): State<AppState>,
    a: Auth,
    Path(no): Path<String>,
) -> AppResult<Json<db::faktur::FakturResp>> {
    let nik = nik_dari_token(&a)?;
    let f = db::run(&s.pool, move |c| anggota::detail(c, &nik, &no)).await?;
    Ok(Json(f))
}
