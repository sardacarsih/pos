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
    kategori: Option<Vec<String>>,
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
        let kat = req.kategori.unwrap_or_default();
        if !kat.is_empty() {
            let semua = katalog::semua_kategori(c)?;
            if kat.iter().any(|k| !semua.iter().any(|x| &x.id == k)) {
                return Err(AppError::validation("Ada kategori yang tidak dikenal"));
            }
        }
        let id = user::create_user(
            c,
            &username,
            req.nama.trim(),
            &password::hash(&t)?,
            req.role,
            nik.as_deref(),
        )?;
        if req.role == Role::Kasir {
            user::set_kategori(c, id, &kat)?;
        }
        Ok(id)
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

/// Kosong = tidak dibatasi. Hanya untuk akun kasir.
pub async fn kategori(
    State(s): State<AppState>,
    a: Auth,
    Path(id): Path<i64>,
    Json(req): Json<KategoriReq>,
) -> AppResult<StatusCode> {
    a.peran(ADMIN)?;
    db::run(&s.pool, move |c| {
        let u = user::find_by_id(c, id)?
            .ok_or_else(|| AppError::NotFound("User tidak ditemukan".into()))?;
        if u.role != Role::Kasir {
            return Err(AppError::validation(
                "Pembatasan kategori hanya untuk akun kasir",
            ));
        }
        let semua = katalog::semua_kategori(c)?;
        if req
            .kategori_ids
            .iter()
            .any(|k| !semua.iter().any(|x| &x.id == k))
        {
            return Err(AppError::validation("Ada kategori yang tidak dikenal"));
        }
        user::set_kategori(c, id, &req.kategori_ids)
    })
    .await?;
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
