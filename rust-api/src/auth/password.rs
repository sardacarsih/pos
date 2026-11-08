use std::sync::LazyLock;

use argon2::{
    Argon2,
    password_hash::{PasswordHasher, PasswordVerifier, phc::PasswordHash},
};

use crate::error::{AppError, AppResult};

/// Parameter default crate (Argon2id v19, m=19 MiB, t=2, p=1 = minimum OWASP).
/// Hash dengan prefix lain dianggap perlu di-rehash.
const CURRENT_PREFIX: &str = "$argon2id$v=19$m=19456,t=2,p=1$";
pub const MIN_LENGTH: usize = 8;

pub fn hash(password: &str) -> AppResult<String> {
    Argon2::default()
        .hash_password(password.as_bytes())
        .map(|h| h.to_string())
        .map_err(|e| AppError::Internal(format!("hash gagal: {e}")))
}

pub fn verify(password: &str, stored: &str) -> bool {
    match PasswordHash::new(stored) {
        Ok(parsed) => Argon2::default()
            .verify_password(password.as_bytes(), &parsed)
            .is_ok(),
        Err(_) => false,
    }
}

pub fn needs_rehash(stored: &str) -> bool {
    !stored.starts_with(CURRENT_PREFIX)
}

static DUMMY: LazyLock<String> =
    LazyLock::new(|| hash("dummy-password-for-timing").expect("hash dummy"));

/// Dipanggil saat user tidak ada agar waktu respons sama dengan verifikasi sungguhan.
pub fn dummy_verify(password: &str) {
    let _ = verify(password, &DUMMY);
}

pub fn validate_new(password: &str) -> AppResult<()> {
    if password.chars().count() < MIN_LENGTH {
        return Err(AppError::validation(format!(
            "Password minimal {MIN_LENGTH} karakter"
        )));
    }
    Ok(())
}

/// Password sementara acak (12 karakter hex) untuk akun baru / reset.
pub fn temp_password() -> String {
    uuid::Uuid::new_v4().simple().to_string()[..12].to_string()
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn roundtrip_and_rehash() {
        let h = hash("rahasia-123").unwrap();
        assert!(h.starts_with(CURRENT_PREFIX));
        assert!(verify("rahasia-123", &h));
        assert!(!verify("salah", &h));
        assert!(!needs_rehash(&h));
        assert!(needs_rehash("$argon2id$v=19$m=8,t=1,p=1$abc$def"));
        assert!(!verify("x", "bukan-hash"));
    }

    #[test]
    fn policy() {
        assert!(validate_new("1234567").is_err());
        assert!(validate_new("12345678").is_ok());
        assert_ne!(temp_password(), temp_password());
    }
}
