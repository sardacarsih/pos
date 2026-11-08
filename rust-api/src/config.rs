use std::{env, fs};

#[derive(Clone)]
pub struct Config {
    pub bind: String,
    pub db_user: String,
    pub db_password: String,
    pub db_connect: String,
    pub pool_max: u32,
    pub jwt_secret: String,
    pub default_nik: String,
    pub trust_proxy: bool,
    pub default_max_tenor: i32,
}

/// Muat `.env` sederhana (KEY=VALUE) tanpa menimpa environment yang sudah ada.
fn load_dotenv() {
    let Ok(text) = fs::read_to_string(".env") else {
        return;
    };
    for line in text.lines() {
        let line = line.trim();
        if line.is_empty() || line.starts_with('#') {
            continue;
        }
        if let Some((k, v)) = line.split_once('=') {
            let k = k.trim();
            if env::var_os(k).is_none() {
                // SAFETY: dipanggil sekali saat start, sebelum ada thread lain.
                unsafe { env::set_var(k, v.trim()) };
            }
        }
    }
}

fn req(k: &str) -> Result<String, String> {
    env::var(k).map_err(|_| format!("environment {k} belum diset"))
}

impl Config {
    pub fn from_env() -> Result<Self, String> {
        load_dotenv();
        let jwt_secret = req("POS_JWT_SECRET")?;
        if jwt_secret.len() < 32 {
            return Err("POS_JWT_SECRET minimal 32 karakter".into());
        }
        Ok(Self {
            bind: env::var("POS_BIND").unwrap_or_else(|_| "127.0.0.1:8080".into()),
            db_user: req("POS_DB_USER")?,
            db_password: req("POS_DB_PASSWORD")?,
            db_connect: req("POS_DB_CONNECT")?,
            pool_max: env::var("POS_DB_POOL_MAX")
                .ok()
                .and_then(|v| v.parse().ok())
                .unwrap_or(10),
            jwt_secret,
            default_nik: env::var("POS_DEFAULT_CUSTOMER_NIK").unwrap_or_else(|_| "00.00004".into()),
            trust_proxy: env::var("POS_TRUST_PROXY")
                .map(|v| v == "true")
                .unwrap_or(false),
            default_max_tenor: env::var("POS_DEFAULT_MAX_TENOR")
                .ok()
                .and_then(|v| v.parse().ok())
                .unwrap_or(12),
        })
    }
}
