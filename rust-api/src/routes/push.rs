use axum::{Json, extract::State, http::StatusCode};
use serde::Deserialize;

use super::{AppState, Auth};
use crate::{
    auth::Role,
    db,
    error::{AppError, AppResult},
};

#[derive(Deserialize)]
pub struct DeviceReq {
    device_id: String,
    token: String,
}

#[derive(Deserialize)]
pub struct RemoveReq {
    device_id: String,
}

fn valid_device(device: &str) -> bool {
    device.len() == 32 && device.bytes().all(|c| c.is_ascii_hexdigit())
}

pub async fn register(
    State(s): State<AppState>,
    a: Auth,
    Json(req): Json<DeviceReq>,
) -> AppResult<StatusCode> {
    let user = a.peran(&[Role::Anggota])?.sub;
    if !valid_device(&req.device_id)
        || req.token.is_empty()
        || req.token.len() > 2048
        || !req.token.is_ascii()
    {
        return Err(AppError::validation(
            "Identitas perangkat atau token tidak valid",
        ));
    }
    db::run(&s.pool, move |c| {
        db::push::register(c, user, &req.device_id, &req.token)
    })
    .await?;
    Ok(StatusCode::NO_CONTENT)
}

pub async fn remove(
    State(s): State<AppState>,
    a: Auth,
    Json(req): Json<RemoveReq>,
) -> AppResult<StatusCode> {
    let user = a.peran(&[Role::Anggota])?.sub;
    if !valid_device(&req.device_id) {
        return Err(AppError::validation("Identitas perangkat tidak valid"));
    }
    db::run(&s.pool, move |c| {
        db::push::unregister(c, user, &req.device_id)
    })
    .await?;
    Ok(StatusCode::NO_CONTENT)
}

#[cfg(test)]
mod tests {
    use super::*;
    #[test]
    fn validasi_device() {
        assert!(valid_device("0123456789abcdef0123456789abcdef"));
        assert!(!valid_device("not-a-device"));
    }
}
