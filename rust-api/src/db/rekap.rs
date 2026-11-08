//! Rekap transaksi per anggota untuk admin: daftar periode dan total belanja anggota pada satu periode.
use oracle::Connection;
use serde::Serialize;

use super::{dec, opt, tampil, tm};
use crate::error::{AppError, AppResult};

const TGL: &str = "'DD-MON-YYYY', 'NLS_DATE_LANGUAGE = ENGLISH'";

#[derive(Debug, Serialize)]
pub struct PeriodeInfo {
    pub periode: i32,
    /// Rentang bulanan periode (YYYY-MM-DD).
    pub dari: String,
    pub sampai: String,
}

/// Semua periode yang rentang bulanannya sudah diatur, terbaru dulu.
pub fn daftar_periode(conn: &Connection) -> AppResult<Vec<PeriodeInfo>> {
    let sql = format!(
        "SELECT PERIODE, TO_CHAR(TO_DATE(BDARI, {TGL}), 'YYYY-MM-DD'), TO_CHAR(TO_DATE(BSAMPAI, {TGL}), 'YYYY-MM-DD') \
         FROM POS_PERIODE WHERE BDARI IS NOT NULL AND BSAMPAI IS NOT NULL ORDER BY PERIODE DESC"
    );
    let mut out = Vec::new();
    for r in conn.query(&sql, &[])? {
        let r = r?;
        out.push(PeriodeInfo {
            periode: r.get::<_, i64>(0)? as i32,
            dari: r.get(1)?,
            sampai: r.get(2)?,
        });
    }
    Ok(out)
}

#[derive(Debug, Serialize)]
pub struct RekapItem {
    pub nik: String,
    pub nama: String,
    pub unit: String,
    pub jumlah: i64,
    pub total: i64,
    pub tunai: i64,
    pub kredit: i64,
    /// 0 = tidak dibatasi.
    pub limit: i64,
    /// null bila limit tidak dibatasi; bisa negatif bila melewati limit.
    pub sisa_limit: Option<i64>,
}

#[derive(Debug, Serialize)]
pub struct Rekap {
    pub periode: i32,
    pub dari: String,
    pub sampai: String,
    pub jumlah_anggota: i64,
    pub jumlah_transaksi: i64,
    pub total: i64,
    pub tunai: i64,
    pub kredit: i64,
    pub items: Vec<RekapItem>,
}

/// Rekap per anggota pada [periode]. Jendela tiap anggota sama dengan pembatas limit hutang:
/// BULANAN -> BDARI..BSAMPAI, selain itu R1DARI..R2SAMPAI. Hanya anggota aktif yang bertransaksi;
/// pelanggan tunai default ([default_nik]) tidak ikut. Urut total belanja menurun.
pub fn rekap_anggota(conn: &Connection, periode: i32, default_nik: &str) -> AppResult<Rekap> {
    let p = periode as i64;
    let sql_periode = format!(
        "SELECT TO_CHAR(TO_DATE(BDARI, {TGL}), 'YYYY-MM-DD'), TO_CHAR(TO_DATE(BSAMPAI, {TGL}), 'YYYY-MM-DD') \
         FROM POS_PERIODE WHERE PERIODE = :p AND BDARI IS NOT NULL AND BSAMPAI IS NOT NULL"
    );
    let r = opt(conn.query_row_named(&sql_periode, &[("p", &p)]))?
        .ok_or_else(|| AppError::NotFound(format!("Periode {periode} tidak ditemukan")))?;
    let (dari, sampai): (String, String) = (r.get(0)?, r.get(1)?);

    let sql = format!(
        "SELECT A.NIK, A.NAMA_PELANGGAN, K.NAMA, {limit}, COUNT(*), {total}, {tunai}, {kredit} \
         FROM FIN_ANGGOTA A \
         JOIN FIN_UNITKERJA K ON K.KODE = A.UNIT_KERJA \
         JOIN POS_PERIODE R ON R.PERIODE = :p \
         JOIN POS_PENJUALAN P ON P.NIK = A.NIK \
          AND P.TANGGAL >= CASE WHEN UPPER(A.STATUS) = 'BULANAN' THEN TO_DATE(R.BDARI, {TGL}) ELSE TO_DATE(R.R1DARI, {TGL}) END \
          AND P.TANGGAL <= CASE WHEN UPPER(A.STATUS) = 'BULANAN' THEN TO_DATE(R.BSAMPAI, {TGL}) ELSE TO_DATE(R.R2SAMPAI, {TGL}) END \
         WHERE A.AKTIF = 'Y' AND A.NIK <> :dn \
         GROUP BY A.NIK, A.NAMA_PELANGGAN, K.NAMA, A.LIMIT_HUTANG \
         ORDER BY SUM(P.TOTAL) DESC, A.NAMA_PELANGGAN",
        limit = tm("NVL(A.LIMIT_HUTANG, 0)"),
        total = tm("NVL(SUM(P.TOTAL), 0)"),
        tunai = tm("NVL(SUM(CASE WHEN P.JENIS_BAYAR = 'TUNAI' THEN P.TOTAL END), 0)"),
        kredit = tm("NVL(SUM(CASE WHEN P.JENIS_BAYAR = 'KREDIT' THEN P.TOTAL END), 0)"),
    );
    let mut items = Vec::new();
    let (mut jt, mut t, mut tu, mut kr) = (0i64, 0i64, 0i64, 0i64);
    for r in conn.query_named(&sql, &[("p", &p), ("dn", &default_nik)])? {
        let r = r?;
        let limit = tampil(dec(&r.get::<_, String>(3)?)?);
        let jumlah: i64 = r.get(4)?;
        let total = tampil(dec(&r.get::<_, String>(5)?)?);
        let tunai = tampil(dec(&r.get::<_, String>(6)?)?);
        let kredit = tampil(dec(&r.get::<_, String>(7)?)?);
        jt += jumlah;
        t += total;
        tu += tunai;
        kr += kredit;
        items.push(RekapItem {
            nik: r.get(0)?,
            nama: r.get::<_, Option<String>>(1)?.unwrap_or_default(),
            unit: r.get::<_, Option<String>>(2)?.unwrap_or_default(),
            jumlah,
            total,
            tunai,
            kredit,
            limit,
            sisa_limit: if limit == 0 { None } else { Some(limit - total) },
        });
    }
    Ok(Rekap {
        periode,
        dari,
        sampai,
        jumlah_anggota: items.len() as i64,
        jumlah_transaksi: jt,
        total: t,
        tunai: tu,
        kredit: kr,
        items,
    })
}
