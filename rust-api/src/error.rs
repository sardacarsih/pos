use axum::{
    Json,
    http::StatusCode,
    response::{IntoResponse, Response},
};
use serde_json::json;

#[derive(Debug, thiserror::Error)]
pub enum AppError {
    #[error("{0}")]
    Validation(String),
    #[error("tidak berwenang")]
    Unauthorized,
    /// Login gagal: pesan sengaja generik (tidak membedakan user salah / password salah).
    #[error("login gagal")]
    LoginGagal,
    #[error("{0}")]
    Forbidden(String),
    #[error("{0}")]
    NotFound(String),
    #[error("{0}")]
    Conflict(String),
    #[error("{0}")]
    TooMany(String),
    /// Penolakan aturan bisnis (limit, kategori, stok, periode) dengan kode yang bisa dikenali klien.
    #[error("{pesan}")]
    Rule { kode: &'static str, pesan: String },
    #[error("{0}")]
    Internal(String),
}

pub type AppResult<T> = Result<T, AppError>;

impl AppError {
    pub fn rule(kode: &'static str, pesan: impl Into<String>) -> Self {
        Self::Rule {
            kode,
            pesan: pesan.into(),
        }
    }
    pub fn validation(p: impl Into<String>) -> Self {
        Self::Validation(p.into())
    }
}

impl From<oracle::Error> for AppError {
    fn from(e: oracle::Error) -> Self {
        if e.oci_code() == Some(1) {
            return AppError::Conflict("Data sudah ada".into());
        }
        tracing::error!(error = %e, "kesalahan database");
        AppError::Internal("Kesalahan database".into())
    }
}

impl IntoResponse for AppError {
    fn into_response(self) -> Response {
        let (status, kode, pesan) = match &self {
            AppError::Validation(m) => (StatusCode::BAD_REQUEST, "VALIDASI", m.clone()),
            AppError::Unauthorized => (
                StatusCode::UNAUTHORIZED,
                "TIDAK_BERWENANG",
                "Sesi tidak valid, silakan login ulang".into(),
            ),
            AppError::LoginGagal => (
                StatusCode::UNAUTHORIZED,
                "LOGIN_GAGAL",
                "Username atau password salah".into(),
            ),
            AppError::Forbidden(m) => (StatusCode::FORBIDDEN, "DILARANG", m.clone()),
            AppError::NotFound(m) => (StatusCode::NOT_FOUND, "TIDAK_DITEMUKAN", m.clone()),
            AppError::Conflict(m) => (StatusCode::CONFLICT, "KONFLIK", m.clone()),
            AppError::TooMany(m) => (StatusCode::TOO_MANY_REQUESTS, "TERLALU_BANYAK", m.clone()),
            AppError::Rule { kode, pesan } => {
                (StatusCode::UNPROCESSABLE_ENTITY, *kode, pesan.clone())
            }
            AppError::Internal(m) => {
                tracing::error!(%m, "kesalahan internal");
                (
                    StatusCode::INTERNAL_SERVER_ERROR,
                    "KESALAHAN_SERVER",
                    "Terjadi kesalahan pada server".into(),
                )
            }
        };
        (status, Json(json!({ "kode": kode, "pesan": pesan }))).into_response()
    }
}
