//! Profil akun sendiri (semua role): nama, kontak, foto. ID selalu dari token, tidak dari klien.
use axum::{
    Json,
    extract::State,
    http::{StatusCode, header},
    response::{IntoResponse, Response},
};
use serde::Deserialize;
use serde_json::{Value, json};

use super::{AppState, Auth};
use crate::{
    auth::Role,
    db::{self, profil},
    error::{AppError, AppResult},
};

const SEMUA: &[Role] = &[Role::Kasir, Role::Anggota, Role::Admin];

pub async fn lihat(State(s): State<AppState>, a: Auth) -> AppResult<Json<profil::Profil>> {
    let id = a.peran(SEMUA)?.sub;
    Ok(Json(db::run(&s.pool, move |c| profil::muat(c, id)).await?))
}

#[derive(Deserialize)]
pub struct UbahReq {
    nama: String,
    #[serde(default)]
    telepon: String,
    #[serde(default)]
    email: String,
}

pub async fn ubah(
    State(s): State<AppState>,
    a: Auth,
    Json(req): Json<UbahReq>,
) -> AppResult<Json<profil::Profil>> {
    let id = a.peran(SEMUA)?.sub;
    let p = db::run(&s.pool, move |c| {
        profil::ubah(c, id, &req.nama, &req.telepon, &req.email)
    })
    .await?;
    Ok(Json(p))
}

/// Unggah foto profil: body = bytes gambar mentah (PNG/JPEG persegi, maks 1 MB), divalidasi seperti foto produk.
pub async fn foto_ubah(
    State(s): State<AppState>,
    a: Auth,
    body: axum::body::Bytes,
) -> AppResult<Json<Value>> {
    let id = a.peran(SEMUA)?.sub;
    let data = body.to_vec();
    let versi = db::run(&s.pool, move |c| profil::simpan_foto(c, id, &data)).await?;
    Ok(Json(json!({ "versi": versi })))
}

pub async fn foto_ambil(State(s): State<AppState>, a: Auth) -> AppResult<Response> {
    let id = a.peran(SEMUA)?.sub;
    let f = db::run(&s.pool, move |c| profil::ambil_foto(c, id))
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

pub async fn foto_hapus(State(s): State<AppState>, a: Auth) -> AppResult<StatusCode> {
    let id = a.peran(SEMUA)?.sub;
    if !db::run(&s.pool, move |c| profil::hapus_foto(c, id)).await? {
        return Err(AppError::NotFound("Tidak ada foto profil".into()));
    }
    Ok(StatusCode::NO_CONTENT)
}
