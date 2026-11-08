//! Data milik anggota sendiri. NIK SELALU dari token, tidak pernah dari parameter klien.
use chrono::NaiveDateTime;
use oracle::Connection;
use serde::Serialize;

use super::{dec, faktur, katalog, tampil, tm};
use crate::error::{AppError, AppResult};

#[derive(Debug, Serialize)]
pub struct Ringkasan {
    pub nama: String,
    pub nik: String,
    pub periode: i32,
    pub dari: String,
    pub sampai: String,
    /// 0 = tidak dibatasi.
    pub limit: i64,
    pub belanja_periode: i64,
    /// null bila limit tidak dibatasi.
    pub sisa_limit: Option<i64>,
}

#[derive(Debug, Serialize)]
pub struct TransaksiRingkas {
    pub no_transaksi: String,
    pub tanggal: String,
    pub jam: String,
    pub jenis_bayar: String,
    pub tenor: i32,
    pub total: i64,
}

/// Periode hari ini menurut rentang tanggal POS_PERIODE (bukan bulan kalender).
pub fn periode_sekarang(conn: &Connection) -> AppResult<i32> {
    let (d, _) = faktur::sekarang(conn)?;
    faktur::periode_untuk_tanggal(conn, d)?.ok_or_else(|| {
        AppError::rule(
            "PERIODE_TIDAK_ADA",
            format!(
                "Tanggal {} di luar rentang periode yang dikonfigurasi",
                d.format("%Y-%m-%d")
            ),
        )
    })
}

pub fn ringkasan(conn: &Connection, nik: &str, periode: i32) -> AppResult<Ringkasan> {
    let p = katalog::pelanggan_by_nik(conn, nik)?
        .ok_or_else(|| AppError::NotFound("Data anggota tidak ditemukan".into()))?;
    let (dari, sampai) = faktur::jendela_periode(conn, periode, &p.status)?
        .ok_or_else(|| AppError::NotFound(format!("Periode {periode} tidak ditemukan")))?;
    let belanja = faktur::belanja_periode(conn, nik, dari, sampai)?;
    let limit = tampil(p.limit);
    let belanja_i = tampil(belanja);
    Ok(Ringkasan {
        nama: p.nama,
        nik: p.nik,
        periode,
        dari: dari.format("%Y-%m-%d").to_string(),
        sampai: sampai.format("%Y-%m-%d").to_string(),
        limit,
        belanja_periode: belanja_i,
        sisa_limit: if limit == 0 {
            None
        } else {
            Some(limit - belanja_i)
        },
    })
}

pub fn transaksi(
    conn: &Connection,
    nik: &str,
    periode: i32,
    page: u32,
    size: u32,
) -> AppResult<(Vec<TransaksiRingkas>, bool)> {
    let p = katalog::pelanggan_by_nik(conn, nik)?
        .ok_or_else(|| AppError::NotFound("Data anggota tidak ditemukan".into()))?;
    let (dari, sampai) = faktur::jendela_periode(conn, periode, &p.status)?
        .ok_or_else(|| AppError::NotFound(format!("Periode {periode} tidak ditemukan")))?;
    let sql = format!(
        "SELECT NO_TRANSAKSI, TANGGAL, JAM, JENIS_BAYAR, {}, {} FROM POS_PENJUALAN \
         WHERE NIK = :nik AND TANGGAL BETWEEN :dari AND :sampai \
         ORDER BY TANGGAL DESC, JAM DESC, ID_PENJUALAN DESC OFFSET :off ROWS FETCH NEXT :lim ROWS ONLY",
        tm("TENOR"),
        tm("TOTAL")
    );
    let off = (page.saturating_sub(1) * size) as i64;
    let lim = (size + 1) as i64;
    let mut out = Vec::new();
    for r in conn.query_named(
        &sql,
        &[
            ("nik", &nik),
            ("dari", &dari),
            ("sampai", &sampai),
            ("off", &off),
            ("lim", &lim),
        ],
    )? {
        let r = r?;
        let tgl: NaiveDateTime = r.get(1)?;
        out.push(TransaksiRingkas {
            no_transaksi: r.get(0)?,
            tanggal: tgl.format("%Y-%m-%d").to_string(),
            jam: r.get::<_, Option<String>>(2)?.unwrap_or_default(),
            jenis_bayar: r.get::<_, Option<String>>(3)?.unwrap_or_default(),
            tenor: tampil(dec(&r.get::<_, String>(4)?)?) as i32,
            total: tampil(dec(&r.get::<_, String>(5)?)?),
        });
    }
    let ada_lagi = out.len() > size as usize;
    out.truncate(size as usize);
    Ok((out, ada_lagi))
}

/// Detail faktur hanya bila milik NIK pemanggil; selain itu 404 (tidak membocorkan keberadaan).
pub fn detail(conn: &Connection, nik: &str, no: &str) -> AppResult<faktur::FakturResp> {
    match faktur::muat(conn, no)? {
        Some(f) if f.nik == nik => Ok(f),
        _ => Err(AppError::NotFound("Transaksi tidak ditemukan".into())),
    }
}
