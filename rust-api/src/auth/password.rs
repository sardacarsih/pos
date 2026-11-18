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

/// Hash format desktop (`Shared/Auth/PasswordCryptographyPbkdf2.cs`): `PBKDF2-SHA256$600000$<salt b64>$<hash b64>`.
pub fn hash_desktop(password: &str) -> String {
    use base64::{Engine, engine::general_purpose::STANDARD};
    // Dua UUID v4 = 32 byte acak; 16 byte pertama dipakai sebagai salt (bit versi/varian tidak masalah untuk salt).
    let salt = uuid::Uuid::new_v4().into_bytes();
    hash_desktop_dengan(password, &salt, 600_000, STANDARD.encode(salt))
}

fn hash_desktop_dengan(password: &str, salt: &[u8], iterasi: u32, salt_b64: String) -> String {
    use base64::{Engine, engine::general_purpose::STANDARD};
    let mut kunci = [0u8; 32];
    pbkdf2::pbkdf2_hmac::<sha2::Sha256>(password.as_bytes(), salt, iterasi, &mut kunci);
    format!("PBKDF2-SHA256${iterasi}${salt_b64}${}", STANDARD.encode(kunci))
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

/// A–Z tanpa I/O + 2–9: 32 simbol, tanpa karakter yang mudah tertukar (0/O, 1/I).
const TEMP_ALPHABET: &[u8; 32] = b"ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

/// Password sementara acak (8 karakter dari `TEMP_ALPHABET`) untuk akun baru / reset.
pub fn temp_password() -> String {
    // Byte 6 dan 8 UUID v4 memuat bit versi/varian (tidak acak penuh): dilewati.
    // 32 simbol -> `& 31` tanpa bias.
    uuid::Uuid::new_v4()
        .into_bytes()
        .into_iter()
        .enumerate()
        .filter(|(i, _)| *i != 6 && *i != 8)
        .take(8)
        .map(|(_, b)| TEMP_ALPHABET[(b & 31) as usize] as char)
        .collect()
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

    #[test]
    fn temp_password_format() {
        for _ in 0..500 {
            let t = temp_password();
            assert_eq!(t.len(), 8);
            assert!(t.bytes().all(|b| TEMP_ALPHABET.contains(&b)), "{t}");
            assert!(!t.contains(['I', 'O', '0', '1']), "{t}");
        }
    }
}

#[cfg(test)]
mod tests_desktop {
    use super::*;

    #[test]
    fn hash_desktop_berformat_dan_deterministik_per_salt() {
        let salt = [7u8; 16];
        let a = hash_desktop_dengan("rahasia", &salt, 1000, "B64".into());
        let b = hash_desktop_dengan("rahasia", &salt, 1000, "B64".into());
        assert_eq!(a, b);
        let bagian: Vec<_> = a.split('$').collect();
        assert_eq!((bagian.len(), bagian[0], bagian[1]), (4, "PBKDF2-SHA256", "1000"));
        assert!(hash_desktop("x").starts_with("PBKDF2-SHA256$600000$"));
    }

    #[test]
    fn hash_desktop_cocok_dengan_vektor_rfc7914() {
        // RFC 7914 6: PBKDF2-HMAC-SHA256("passwd", "salt", c=1, dkLen=64) diawali 55ac046e56e3089f.
        let mut k = [0u8; 8];
        pbkdf2::pbkdf2_hmac::<sha2::Sha256>(b"passwd", b"salt", 1, &mut k);
        assert_eq!(k, [0x55, 0xac, 0x04, 0x6e, 0x56, 0xe3, 0x08, 0x9f]);
    }
}
