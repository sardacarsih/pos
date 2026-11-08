//! Data milik anggota sendiri. NIK selalu dari token (claims.nik), tidak pernah dari klien.
use axum::{
    Json,
    extract::{Path, Query, State},
};
use serde::Deserialize;
use serde_json::Value;

use super::{AppState, Auth, Paging, halaman};
use crate::{
    auth::Role,
    db::{self, anggota},
    error::{AppError, AppResult},
};

const ANGGOTA: &[Role] = &[Role::Anggota];

fn nik_dari_token(a: &Auth) -> AppResult<String> {
    a.peran(ANGGOTA)?;
    a.0.nik
        .clone()
        .ok_or_else(|| AppError::Forbidden("Akun belum terhubung ke data anggota".into()))
}

#[derive(Deserialize)]
pub struct PeriodeQuery {
    periode: Option<i32>,
    // Tanpa `#[serde(flatten)]`: angka dari query string (string) gagal di-parse bila di-flatten.
    q: Option<String>,
    page: Option<u32>,
    size: Option<u32>,
}

fn periode_valid(p: i32) -> AppResult<i32> {
    if (200001..=299912).contains(&p) && (1..=12).contains(&(p % 100)) {
        Ok(p)
    } else {
        Err(AppError::validation("Periode harus berformat yyyyMM"))
    }
}

pub async fn ringkasan(
    State(s): State<AppState>,
    a: Auth,
    Query(q): Query<PeriodeQuery>,
) -> AppResult<Json<anggota::Ringkasan>> {
    let nik = nik_dari_token(&a)?;
    let periode = q.periode.map(periode_valid).transpose()?;
    let r = db::run(&s.pool, move |c| {
        let p = match periode {
            Some(p) => p,
            None => anggota::periode_sekarang(c)?,
        };
        anggota::ringkasan(c, &nik, p)
    })
    .await?;
    Ok(Json(r))
}

pub async fn transaksi(
    State(s): State<AppState>,
    a: Auth,
    Query(q): Query<PeriodeQuery>,
) -> AppResult<Json<Value>> {
    let nik = nik_dari_token(&a)?;
    let periode = q.periode.map(periode_valid).transpose()?;
    let pg = Paging {
        q: q.q,
        page: q.page,
        size: q.size,
    };
    let (page, size) = (pg.page(), pg.size());
    let (items, lagi) = db::run(&s.pool, move |c| {
        let p = match periode {
            Some(p) => p,
            None => anggota::periode_sekarang(c)?,
        };
        anggota::transaksi(c, &nik, p, page, size)
    })
    .await?;
    Ok(halaman(items, lagi, page))
}

pub async fn detail(
    State(s): State<AppState>,
    a: Auth,
    Path(no): Path<String>,
) -> AppResult<Json<db::faktur::FakturResp>> {
    let nik = nik_dari_token(&a)?;
    let f = db::run(&s.pool, move |c| anggota::detail(c, &nik, &no)).await?;
    Ok(Json(f))
}

/// Dashboard anggota: ringkasan limit periode berjalan (dengan rentang tanggal) + 5 transaksi terakhir.
/// Periode di luar rentang / tutup tidak menggagalkan dashboard; `periode_terbuka=false` + `pesan`.
pub async fn dashboard(State(s): State<AppState>, a: Auth) -> AppResult<Json<Value>> {
    let nik = nik_dari_token(&a)?;
    let v = db::run(&s.pool, move |c| {
        let (hari, _) = db::faktur::sekarang(c)?;
        let (terbuka, pesan) = match db::faktur::cek_periode(c, hari) {
            Ok(()) => (true, None),
            Err(AppError::Rule { pesan, .. }) => (false, Some(pesan)),
            Err(e) => return Err(e),
        };
        let (ringkasan, terakhir) = match anggota::periode_sekarang(c) {
            Ok(p) => (
                Some(anggota::ringkasan(c, &nik, p)?),
                anggota::transaksi(c, &nik, p, 1, 5)?.0,
            ),
            Err(AppError::Rule { .. }) => (None, Vec::new()),
            Err(e) => return Err(e),
        };
        Ok(serde_json::json!({
            "tanggal": hari.format("%Y-%m-%d").to_string(),
            "periode_terbuka": terbuka,
            "pesan": pesan,
            "ringkasan": ringkasan,
            "terakhir": terakhir,
        }))
    })
    .await?;
    Ok(Json(v))
}

/// Nomor faktur untuk belanja mandiri (sama dengan kasir, terikat ke akun anggota).
pub async fn nomor(
    State(s): State<AppState>,
    a: Auth,
    headers: axum::http::HeaderMap,
) -> AppResult<Json<Value>> {
    nik_dari_token(&a)?;
    let id = a.0.sub;
    let key = super::idempotency_key(&headers)?;
    let no = db::run(&s.pool, move |c| db::faktur::terbitkan_nomor(c, id, &key)).await?;
    Ok(Json(serde_json::json!({ "no_transaksi": no.0, "duplikat": no.1 })))
}

/// Belanja mandiri: selalu kredit atas NIK sendiri. NIK dari token; jalur tunai (NIK default) ditolak.
pub async fn simpan(
    State(s): State<AppState>,
    a: Auth,
    headers: axum::http::HeaderMap,
    Json(mut req): Json<db::faktur::SimpanReq>,
) -> AppResult<Json<db::faktur::FakturResp>> {
    let nik = nik_dari_token(&a)?;
    if nik == s.cfg.default_nik {
        return Err(AppError::Forbidden("Akun ini tidak dapat belanja mandiri".into()));
    }
    req.nik = nik;
    req.uang_bayar = None;
    if req.tenor.is_none() {
        return Err(AppError::validation("Tenor wajib diisi"));
    }
    let cl = a.0.clone();
    let key = super::idempotency_key(&headers)?;
    let kategori = db::run(&s.pool, db::user::allowed_kategori).await?;
    let nik_default = s.cfg.default_nik.clone();
    let max_tenor = s.cfg.default_max_tenor;
    let f = db::run(&s.pool, move |c| {
        let kasir = db::faktur::Kasir {
            id: cl.sub,
            username: cl.usr,
            nama: cl.nama,
            kategori,
        };
        db::faktur::simpan(
            c,
            &kasir,
            &req,
            &db::faktur::Opsi {
                default_nik: &nik_default,
                default_max_tenor: max_tenor,
            },
            &key,
        )
    })
    .await?;
    Ok(Json(f))
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn query_periode_menerima_angka() {
        let uri: axum::http::Uri = "/api/v1/saya/transaksi?periode=202611&page=2&size=30"
            .parse()
            .unwrap();
        let q = Query::<PeriodeQuery>::try_from_uri(&uri).expect("query periode valid");
        assert_eq!(
            (q.0.periode, q.0.page, q.0.size),
            (Some(202611), Some(2), Some(30))
        );
    }
}
