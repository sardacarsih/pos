use jsonwebtoken::{Algorithm, DecodingKey, EncodingKey, Header, Validation, decode, encode};
use serde::{Deserialize, Serialize};
use sha2::{Digest, Sha256};

use super::Role;
use crate::error::{AppError, AppResult};

pub const ACCESS_TTL_SECS: i64 = 15 * 60;

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct Claims {
    pub sub: i64,
    pub usr: String,
    pub nama: String,
    pub role: Role,
    pub nik: Option<String>,
    /// True bila user wajib ganti password dulu (password sementara).
    pub ganti: bool,
    pub iat: i64,
    pub exp: i64,
}

pub fn issue(secret: &str, mut c: Claims, now: i64) -> AppResult<String> {
    c.iat = now;
    c.exp = now + ACCESS_TTL_SECS;
    encode(
        &Header::new(Algorithm::HS256),
        &c,
        &EncodingKey::from_secret(secret.as_bytes()),
    )
    .map_err(|e| AppError::Internal(format!("jwt: {e}")))
}

pub fn verify(secret: &str, token: &str) -> AppResult<Claims> {
    let mut v = Validation::new(Algorithm::HS256);
    v.leeway = 5;
    decode::<Claims>(token, &DecodingKey::from_secret(secret.as_bytes()), &v)
        .map(|d| d.claims)
        .map_err(|_| AppError::Unauthorized)
}

/// Refresh token acak (~244 bit dari dua UUID v4 CSPRNG). Hanya hash-nya yang disimpan.
pub fn new_refresh() -> (String, String) {
    let plain = format!(
        "{}{}",
        uuid::Uuid::new_v4().simple(),
        uuid::Uuid::new_v4().simple()
    );
    let h = hash_refresh(&plain);
    (plain, h)
}

pub fn hash_refresh(plain: &str) -> String {
    Sha256::digest(plain.as_bytes())
        .iter()
        .map(|b| format!("{b:02x}"))
        .collect()
}

#[cfg(test)]
mod tests {
    use super::*;

    fn claims() -> Claims {
        Claims {
            sub: 7,
            usr: "budi".into(),
            nama: "Budi".into(),
            role: Role::Anggota,
            nik: Some("12.34".into()),
            ganti: false,
            iat: 0,
            exp: 0,
        }
    }

    #[test]
    fn jwt_roundtrip_and_tamper() {
        let secret = "x".repeat(32);
        let now = chrono::Utc::now().timestamp();
        let t = issue(&secret, claims(), now).unwrap();
        let c = verify(&secret, &t).unwrap();
        assert_eq!(
            (c.sub, c.role, c.nik.as_deref()),
            (7, Role::Anggota, Some("12.34"))
        );
        assert!(verify(&"y".repeat(32), &t).is_err());
        let expired = issue(&secret, claims(), now - 3600).unwrap();
        assert!(verify(&secret, &expired).is_err());
    }

    #[test]
    fn refresh_tokens() {
        let (p1, h1) = new_refresh();
        let (p2, _) = new_refresh();
        assert_ne!(p1, p2);
        assert_eq!(h1, hash_refresh(&p1));
        assert_eq!(h1.len(), 64);
    }
}
