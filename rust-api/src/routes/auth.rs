use axum::{Json, extract::State, http::StatusCode};
use serde::{Deserialize, Serialize};
use serde_json::{Value, json};

use super::{AppState, Auth, ClientIp};
use crate::{
    auth::{Role, password, token},
    db::{
        self,
        user::{self, UserRow},
    },
    error::{AppError, AppResult},
};

const REFRESH_TTL_DAYS: i64 = 30;

#[derive(Deserialize)]
pub struct LoginReq {
    pub username: String,
    pub password: String,
    pub device_id: Option<String>,
}

#[derive(Serialize)]
pub struct TokenResp {
    pub access_token: String,
    pub refresh_token: String,
    pub expires_in: i64,
    pub role: Role,
    pub nama: String,
    pub nik: Option<String>,
    pub ganti_password: bool,
}

fn claims(u: &UserRow) -> token::Claims {
    token::Claims {
        sub: u.id,
        usr: u.username.clone(),
        nama: u.full_name.clone(),
        role: u.role,
        nik: u.nik.clone(),
        ganti: u.must_change,
        iat: 0,
        exp: 0,
    }
}

fn respons(secret: &str, u: &UserRow, refresh: String) -> AppResult<TokenResp> {
    Ok(TokenResp {
        access_token: token::issue(secret, claims(u), chrono::Utc::now().timestamp())?,
        refresh_token: refresh,
        expires_in: token::ACCESS_TTL_SECS,
        role: u.role,
        nama: u.full_name.clone(),
        nik: u.nik.clone(),
        ganti_password: u.must_change,
    })
}

pub async fn login(
    State(s): State<AppState>,
    ClientIp(ip): ClientIp,
    Json(req): Json<LoginReq>,
) -> AppResult<Json<TokenResp>> {
    if !s.limiter.allow(&ip) {
        return Err(AppError::TooMany(
            "Terlalu banyak percobaan. Coba lagi sebentar lagi.".into(),
        ));
    }
    if req.username.trim().is_empty()
        || req.username.len() > 50
        || req.password.is_empty()
        || req.password.len() > 200
    {
        return Err(AppError::LoginGagal);
    }
    let secret = s.cfg.jwt_secret.clone();
    let (u, refresh) = db::run(&s.pool, move |c| {
        // Argon2 mahal: dijalankan di thread blocking yang sama dengan DB.
        let Some(u) = user::find_by_username(c, &req.username)? else {
            password::dummy_verify(&req.password);
            return Err(AppError::LoginGagal);
        };
        if u.locked {
            password::dummy_verify(&req.password);
            return Err(AppError::TooMany(
                "Terlalu banyak percobaan gagal. Coba lagi nanti.".into(),
            ));
        }
        if !password::verify(&req.password, &u.hash) || !u.active {
            user::register_failure(c, u.id)?;
            return Err(AppError::LoginGagal);
        }
        user::reset_failures(c, u.id)?;
        if password::needs_rehash(&u.hash) {
            user::upgrade_hash(c, u.id, &password::hash(&req.password)?)?;
        }
        let (plain, hash) = token::new_refresh();
        let family = uuid::Uuid::new_v4().to_string();
        let device = req.device_id.unwrap_or_default();
        user::create_session(
            c,
            u.id,
            &family,
            &hash,
            &device.chars().take(100).collect::<String>(),
            &ip,
            REFRESH_TTL_DAYS,
        )?;
        Ok((u, plain))
    })
    .await?;
    Ok(Json(respons(&secret, &u, refresh)?))
}

#[derive(Deserialize)]
pub struct RefreshReq {
    pub refresh_token: String,
    pub device_id: Option<String>,
}

pub async fn refresh(
    State(s): State<AppState>,
    ClientIp(ip): ClientIp,
    Json(req): Json<RefreshReq>,
) -> AppResult<Json<TokenResp>> {
    if !s.limiter.allow(&ip) {
        return Err(AppError::TooMany(
            "Terlalu banyak percobaan. Coba lagi sebentar lagi.".into(),
        ));
    }
    let secret = s.cfg.jwt_secret.clone();
    let (u, plain) = db::run(&s.pool, move |c| {
        let hash = token::hash_refresh(&req.refresh_token);
        let ses = user::find_session(c, &hash)?.ok_or(AppError::Unauthorized)?;
        // Token yang sudah dipakai/dicabut dipakai lagi = indikasi pencurian: cabut seluruh rantai.
        if ses.used || ses.revoked {
            user::revoke_family(c, &ses.family)?;
            return Err(AppError::Unauthorized);
        }
        if ses.expired {
            return Err(AppError::Unauthorized);
        }
        if !user::consume_session(c, ses.id)? {
            user::revoke_family(c, &ses.family)?;
            return Err(AppError::Unauthorized);
        }
        let u = user::find_by_id(c, ses.user_id)?
            .filter(|u| u.active)
            .ok_or(AppError::Unauthorized)?;
        let (plain, new_hash) = token::new_refresh();
        let device = req.device_id.unwrap_or_default();
        user::create_session(
            c,
            u.id,
            &ses.family,
            &new_hash,
            &device.chars().take(100).collect::<String>(),
            &ip,
            REFRESH_TTL_DAYS,
        )?;
        Ok((u, plain))
    })
    .await?;
    Ok(Json(respons(&secret, &u, plain)?))
}

#[derive(Deserialize)]
pub struct LogoutReq {
    pub refresh_token: String,
}

pub async fn logout(
    State(s): State<AppState>,
    Json(req): Json<LogoutReq>,
) -> AppResult<StatusCode> {
    db::run(&s.pool, move |c| {
        if let Some(ses) = user::find_session(c, &token::hash_refresh(&req.refresh_token))? {
            user::revoke_family(c, &ses.family)?;
        }
        Ok(())
    })
    .await?;
    Ok(StatusCode::NO_CONTENT)
}

#[derive(Deserialize)]
pub struct GantiReq {
    pub password_lama: String,
    pub password_baru: String,
}

/// Boleh dipanggil saat password sementara (tidak memakai `peran`). Semua sesi dicabut setelahnya.
pub async fn ganti_password(
    State(s): State<AppState>,
    a: Auth,
    Json(req): Json<GantiReq>,
) -> AppResult<StatusCode> {
    password::validate_new(&req.password_baru)?;
    if req.password_baru == req.password_lama {
        return Err(AppError::validation("Password baru harus berbeda"));
    }
    let id = a.0.sub;
    db::run(&s.pool, move |c| {
        let u = user::find_by_id(c, id)?
            .filter(|u| u.active)
            .ok_or(AppError::Unauthorized)?;
        if !password::verify(&req.password_lama, &u.hash) {
            return Err(AppError::validation("Password lama salah"));
        }
        user::set_password(c, id, &password::hash(&req.password_baru)?, false)?;
        user::revoke_user(c, id)
    })
    .await?;
    Ok(StatusCode::NO_CONTENT)
}

pub async fn me(State(s): State<AppState>, a: Auth) -> AppResult<Json<Value>> {
    let kat = db::run(&s.pool, user::allowed_kategori).await?;
    Ok(Json(json!({
        "id": a.0.sub, "username": a.0.usr, "nama": a.0.nama, "role": a.0.role,
        "nik": a.0.nik, "ganti_password": a.0.ganti, "kategori_diizinkan": kat,
        "default_nik": s.cfg.default_nik,
    })))
}
