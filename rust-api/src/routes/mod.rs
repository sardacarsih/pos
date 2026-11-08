pub mod admin;
pub mod anggota;
pub mod auth;
pub mod kasir;
pub mod profil;

use std::{net::SocketAddr, sync::Arc};

use axum::{
    Json, Router,
    extract::{ConnectInfo, DefaultBodyLimit, FromRequestParts},
    http::{HeaderMap, Request, request::Parts},
    routing::{get, post, put},
};
use serde::Deserialize;
use serde_json::{Value, json};
use tower_http::trace::TraceLayer;

use crate::{
    auth::{
        Role,
        throttle::IpLimiter,
        token::{self, Claims},
    },
    config::Config,
    db::DbPool,
    error::{AppError, AppResult},
};

#[derive(Clone)]
pub struct AppState {
    pub pool: DbPool,
    pub cfg: Arc<Config>,
    pub limiter: Arc<IpLimiter>,
}

/// Token akses yang valid (tanda tangan + kedaluwarsa). Cek peran lewat `peran`.
pub struct Auth(pub Claims);

impl Auth {
    /// Peran harus salah satu dari `roles` dan password sementara sudah diganti.
    pub fn peran(&self, roles: &[Role]) -> AppResult<&Claims> {
        if !roles.contains(&self.0.role) {
            return Err(AppError::Forbidden(
                "Akun ini tidak berhak mengakses fitur tersebut".into(),
            ));
        }
        if self.0.ganti {
            return Err(AppError::rule(
                "GANTI_PASSWORD",
                "Ganti password sementara terlebih dahulu",
            ));
        }
        Ok(&self.0)
    }
}

impl FromRequestParts<AppState> for Auth {
    type Rejection = AppError;
    async fn from_request_parts(parts: &mut Parts, state: &AppState) -> Result<Self, AppError> {
        let h = parts
            .headers
            .get("authorization")
            .and_then(|v| v.to_str().ok())
            .ok_or(AppError::Unauthorized)?;
        let t = h.strip_prefix("Bearer ").ok_or(AppError::Unauthorized)?;
        Ok(Auth(token::verify(&state.cfg.jwt_secret, t)?))
    }
}

/// IP klien: X-Forwarded-For hanya dipercaya bila POS_TRUST_PROXY=true (di belakang reverse proxy).
pub struct ClientIp(pub String);

impl FromRequestParts<AppState> for ClientIp {
    type Rejection = AppError;
    async fn from_request_parts(parts: &mut Parts, state: &AppState) -> Result<Self, AppError> {
        if state.cfg.trust_proxy
            && let Some(ip) = parts
                .headers
                .get("x-forwarded-for")
                .and_then(|v| v.to_str().ok())
                .and_then(|v| v.split(',').next())
        {
            return Ok(ClientIp(ip.trim().to_string()));
        }
        let ip = parts
            .extensions
            .get::<ConnectInfo<SocketAddr>>()
            .map(|c| c.0.ip().to_string())
            .unwrap_or_else(|| "unknown".into());
        Ok(ClientIp(ip))
    }
}

#[derive(Deserialize)]
pub struct Paging {
    pub q: Option<String>,
    pub page: Option<u32>,
    pub size: Option<u32>,
}

impl Paging {
    pub fn page(&self) -> u32 {
        self.page.unwrap_or(1).max(1)
    }
    pub fn size(&self) -> u32 {
        self.size.unwrap_or(30).clamp(1, 100)
    }
}

pub fn halaman<T: serde::Serialize>(items: Vec<T>, ada_lagi: bool, page: u32) -> Json<Value> {
    Json(json!({ "items": items, "ada_lagi": ada_lagi, "page": page }))
}

/// Checkout uses a canonical UUID v4 so a caller can safely persist and replay it.
pub fn idempotency_key(headers: &HeaderMap) -> AppResult<String> {
    let raw = headers
        .get("Idempotency-Key")
        .and_then(|value| value.to_str().ok())
        .ok_or_else(|| AppError::validation("Header Idempotency-Key wajib diisi"))?;
    let key = raw.to_ascii_lowercase();
    let bytes = key.as_bytes();
    let valid = bytes.len() == 36
        && [8, 13, 18, 23].iter().all(|&i| bytes[i] == b'-')
        && bytes
            .iter()
            .enumerate()
            .all(|(i, b)| [8, 13, 18, 23].contains(&i) || b.is_ascii_hexdigit())
        && bytes[14] == b'4'
        && matches!(bytes[19], b'8' | b'9' | b'a' | b'b');
    if !valid {
        return Err(AppError::validation(
            "Idempotency-Key harus berupa UUID versi 4",
        ));
    }
    Ok(key)
}

pub fn router(state: AppState) -> Router {
    Router::new()
        .route("/health", get(|| async { Json(json!({ "status": "ok" })) }))
        .route("/api/v1/auth/login", post(auth::login))
        .route("/api/v1/auth/refresh", post(auth::refresh))
        .route("/api/v1/auth/logout", post(auth::logout))
        .route("/api/v1/auth/ganti-password", post(auth::ganti_password))
        .route("/api/v1/me", get(auth::me))
        // profil sendiri (semua role)
        .route("/api/v1/profil", get(profil::lihat).put(profil::ubah))
        .route(
            "/api/v1/profil/foto",
            get(profil::foto_ambil)
                .put(profil::foto_ubah)
                .delete(profil::foto_hapus)
                // Foto maks 1 MB + sedikit ruang (default body 256 KB tidak cukup).
                .layer(DefaultBodyLimit::max(1_100_000)),
        )
        // kasir
        .route("/api/v1/periode/status", get(kasir::periode_status))
        .route("/api/v1/kasir/dashboard", get(kasir::dashboard))
        .route("/api/v1/kasir/riwayat", get(kasir::riwayat))
        .route("/api/v1/transaksi/nomor", post(kasir::nomor))
        .route("/api/v1/barang", get(kasir::barang))
        .route(
            "/api/v1/barang/by-barcode/{kode}",
            get(kasir::barang_barcode),
        )
        .route("/api/v1/kategori", get(kasir::kategori))
        .route("/api/v1/pelanggan", get(kasir::pelanggan))
        .route("/api/v1/pelanggan/by-nik/{nik}", get(kasir::pelanggan_nik))
        .route("/api/v1/pelanggan/{nik}/foto", get(kasir::foto_anggota))
        .route("/api/v1/faktur", post(kasir::simpan))
        .route("/api/v1/faktur/{no}", get(kasir::faktur))
        // anggota
        .route("/api/v1/saya/ringkasan", get(anggota::ringkasan))
        .route("/api/v1/saya/dashboard", get(anggota::dashboard))
        .route("/api/v1/saya/belanja/nomor", post(anggota::nomor))
        .route("/api/v1/saya/belanja", post(anggota::simpan))
        .route("/api/v1/saya/transaksi", get(anggota::transaksi))
        .route("/api/v1/saya/transaksi/{no}", get(anggota::detail))
        // admin
        .route("/api/v1/admin/users", get(admin::list).post(admin::buat))
        .route(
            "/api/v1/admin/users/{id}/reset-password",
            post(admin::reset_password),
        )
        .route("/api/v1/admin/users/{id}/aktif", put(admin::aktif))
        .route("/api/v1/admin/users/{id}/foto", get(admin::foto_user))
        .route("/api/v1/admin/produk-urut", put(admin::set_urut_produk))
        .route(
            "/api/v1/admin/kategori-tampil",
            get(admin::kategori_tampil).put(admin::set_kategori_tampil),
        )
        .route("/api/v1/admin/kategori", get(admin::semua_kategori))
        .route("/api/v1/admin/dashboard", get(admin::dashboard))
        .route("/api/v1/admin/periode", get(admin::periode_list))
        .route("/api/v1/admin/rekap-anggota", get(admin::rekap_anggota))
        .route(
            "/api/v1/admin/rekap-anggota/{nik}/transaksi",
            get(admin::rekap_transaksi),
        )
        .route("/api/v1/admin/faktur/{no}", get(admin::faktur_admin))
        .route("/api/v1/admin/produk", get(admin::produk_list))
        .route("/api/v1/admin/produk/{id}", put(admin::produk_ubah))
        .route(
            "/api/v1/admin/produk/{id}/foto",
            put(admin::foto_ubah)
                .delete(admin::foto_hapus)
                // Unggah foto: batas body lebih besar dari default 256 KB (foto maks 1 MB + sedikit ruang).
                .layer(DefaultBodyLimit::max(1_100_000)),
        )
        .route("/api/v1/produk/{id}/foto", get(kasir::foto_ambil))
        .layer(DefaultBodyLimit::max(256 * 1024))
        .layer(TraceLayer::new_for_http().make_span_with(|r: &Request<_>| {
            // Hanya method + path: query (nama/NIK) dan header tidak ikut log.
            tracing::info_span!("req", method = %r.method(), path = %r.uri().path())
        }))
        .with_state(state)
}

#[cfg(test)]
mod idempotency_key_tests {
    use axum::http::{HeaderMap, HeaderValue};

    use super::idempotency_key;

    #[test]
    fn requires_canonical_uuid_v4_header() {
        let mut headers = HeaderMap::new();
        assert!(idempotency_key(&headers).is_err());

        headers.insert("Idempotency-Key", HeaderValue::from_static("not-a-uuid"));
        assert!(idempotency_key(&headers).is_err());

        headers.insert(
            "Idempotency-Key",
            HeaderValue::from_static("d9428888-122b-4f8a-8e3c-20d3c7eab5f1"),
        );
        assert_eq!(
            idempotency_key(&headers).unwrap(),
            "d9428888-122b-4f8a-8e3c-20d3c7eab5f1"
        );
    }
}
