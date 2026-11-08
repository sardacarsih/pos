//! Ringkasan harian kasir. Selalu dibatasi ke username kasir dari token.
use oracle::Connection;
use serde::Serialize;

use super::{dec, faktur, tampil, tm};
use crate::error::{AppError, AppResult};

#[derive(Debug, Serialize)]
pub struct Ringkasan {
    pub jumlah: i64,
    pub total: i64,
    pub tunai: i64,
    pub kredit: i64,
}

#[derive(Debug, Serialize)]
pub struct Terakhir {
    pub no_transaksi: String,
    pub jam: String,
    pub nama_pelanggan: String,
    pub jenis_bayar: String,
    pub total: i64,
}

#[derive(Debug, Serialize)]
pub struct Dashboard {
    pub tanggal: String,
    pub periode_terbuka: bool,
    pub periode_pesan: Option<String>,
    /// PERIODE_TUTUP | PERIODE_TIDAK_ADA | PERIODE_TIDAK_VALID; null bila terbuka.
    pub periode_kode: Option<String>,
    /// Periode hari ini dan rentang bulanannya (YYYY-MM-DD); null bila tanggal di luar semua periode.
    pub periode: Option<i32>,
    pub periode_dari: Option<String>,
    pub periode_sampai: Option<String>,
    pub ringkasan: Ringkasan,
    pub terakhir: Vec<Terakhir>,
}

/// Pecah InfoPeriode menjadi field JSON dashboard: (periode, dari, sampai) dengan tanggal YYYY-MM-DD.
fn ringkas_periode(i: Option<faktur::InfoPeriode>) -> (Option<i32>, Option<String>, Option<String>) {
    (
        i.map(|x| x.periode),
        i.map(|x| x.dari.format("%Y-%m-%d").to_string()),
        i.map(|x| x.sampai.format("%Y-%m-%d").to_string()),
    )
}

pub fn dashboard_kasir(conn: &Connection, username: &str) -> AppResult<Dashboard> {
    let (hari, _) = faktur::sekarang(conn)?;
    let (periode_terbuka, periode_pesan, periode_kode) = match faktur::cek_periode(conn, hari) {
        Ok(()) => (true, None, None),
        Err(AppError::Rule { kode, pesan }) => (false, Some(pesan), Some(kode.to_string())),
        Err(e) => return Err(e),
    };
    let (periode, periode_dari, periode_sampai) = ringkas_periode(faktur::info_periode(conn, hari)?);

    let sql = format!(
        "SELECT COUNT(*), {}, {}, {} FROM POS_PENJUALAN WHERE KASIR = :u AND TRUNC(TANGGAL) = :hari",
        tm("NVL(SUM(TOTAL), 0)"),
        tm("NVL(SUM(CASE WHEN JENIS_BAYAR = 'TUNAI' THEN TOTAL END), 0)"),
        tm("NVL(SUM(CASE WHEN JENIS_BAYAR = 'KREDIT' THEN TOTAL END), 0)"),
    );
    let r = conn.query_row_named(&sql, &[("u", &username), ("hari", &hari)])?;
    let ringkasan = Ringkasan {
        jumlah: r.get(0)?,
        total: tampil(dec(&r.get::<_, String>(1)?)?),
        tunai: tampil(dec(&r.get::<_, String>(2)?)?),
        kredit: tampil(dec(&r.get::<_, String>(3)?)?),
    };

    let sql = format!(
        "SELECT NO_TRANSAKSI, JAM, NAMA_PELANGGAN, JENIS_BAYAR, {} FROM POS_PENJUALAN \
         WHERE KASIR = :u AND TRUNC(TANGGAL) = :hari \
         ORDER BY JAM DESC, ID_PENJUALAN DESC FETCH FIRST 10 ROWS ONLY",
        tm("TOTAL")
    );
    let mut terakhir = Vec::new();
    for r in conn.query_named(&sql, &[("u", &username), ("hari", &hari)])? {
        let r = r?;
        terakhir.push(Terakhir {
            no_transaksi: r.get(0)?,
            jam: r.get::<_, Option<String>>(1)?.unwrap_or_default(),
            nama_pelanggan: r.get::<_, Option<String>>(2)?.unwrap_or_default(),
            jenis_bayar: r.get::<_, Option<String>>(3)?.unwrap_or_default(),
            total: tampil(dec(&r.get::<_, String>(4)?)?),
        });
    }

    Ok(Dashboard {
        tanggal: hari.format("%Y-%m-%d").to_string(),
        periode_terbuka,
        periode_pesan,
        periode_kode,
        periode,
        periode_dari,
        periode_sampai,
        ringkasan,
        terakhir,
    })
}

/// Riwayat transaksi mobile milik kasir pada satu tanggal. Hanya nomor yang diterbitkan ke kasir ini
/// (sama dengan batas `GET /faktur/{no}`), agar setiap baris pasti bisa dibuka detailnya.
pub fn riwayat_kasir(
    conn: &Connection,
    user_id: i64,
    username: &str,
    tanggal: chrono::NaiveDate,
) -> AppResult<Vec<Terakhir>> {
    let sql = format!(
        "SELECT NO_TRANSAKSI, JAM, NAMA_PELANGGAN, JENIS_BAYAR, {} FROM POS_PENJUALAN          WHERE KASIR = :u AND TRUNC(TANGGAL) = :tgl          AND NO_TRANSAKSI IN (SELECT NO_TRANSAKSI FROM MOB_NOMOR WHERE USER_ID = :usrid AND USED_AT IS NOT NULL)          ORDER BY JAM DESC, ID_PENJUALAN DESC FETCH FIRST 200 ROWS ONLY",
        tm("TOTAL")
    );
    let mut out = Vec::new();
    for r in conn.query_named(&sql, &[("u", &username), ("tgl", &tanggal), ("usrid", &user_id)])? {
        let r = r?;
        out.push(Terakhir {
            no_transaksi: r.get(0)?,
            jam: r.get::<_, Option<String>>(1)?.unwrap_or_default(),
            nama_pelanggan: r.get::<_, Option<String>>(2)?.unwrap_or_default(),
            jenis_bayar: r.get::<_, Option<String>>(3)?.unwrap_or_default(),
            total: tampil(dec(&r.get::<_, String>(4)?)?),
        });
    }
    Ok(out)
}

// ------------------------------------------------------------------ admin

#[derive(Debug, Serialize)]
pub struct PerKasir {
    pub kasir: String,
    pub nama: String,
    pub jumlah: i64,
    pub total: i64,
}

#[derive(Debug, Serialize)]
pub struct Terlaris {
    pub kode: String,
    pub nama: String,
    pub qty: f64,
    pub total: i64,
}

#[derive(Debug, Serialize, Default)]
pub struct RingkasanUser {
    pub admin: i64,
    pub kasir: i64,
    pub anggota: i64,
    pub nonaktif: i64,
    pub terkunci: i64,
}

#[derive(Debug, Serialize)]
pub struct DashboardAdmin {
    pub tanggal: String,
    pub periode_terbuka: bool,
    pub periode_pesan: Option<String>,
    pub periode_kode: Option<String>,
    pub periode: Option<i32>,
    pub periode_dari: Option<String>,
    pub periode_sampai: Option<String>,
    pub ringkasan: Ringkasan,
    pub per_kasir: Vec<PerKasir>,
    pub terlaris: Vec<Terlaris>,
    pub user: RingkasanUser,
}

/// Ringkasan hari ini semua kasir (mobile dan desktop), per kasir, barang terlaris, dan jumlah user mobile.
pub fn dashboard_admin(conn: &Connection) -> AppResult<DashboardAdmin> {
    use rust_decimal::prelude::ToPrimitive;

    let (hari, _) = faktur::sekarang(conn)?;
    let (periode_terbuka, periode_pesan, periode_kode) = match faktur::cek_periode(conn, hari) {
        Ok(()) => (true, None, None),
        Err(AppError::Rule { kode, pesan }) => (false, Some(pesan), Some(kode.to_string())),
        Err(e) => return Err(e),
    };
    let (periode, periode_dari, periode_sampai) = ringkas_periode(faktur::info_periode(conn, hari)?);

    let sql = format!(
        "SELECT COUNT(*), {}, {}, {} FROM POS_PENJUALAN WHERE TRUNC(TANGGAL) = :hari",
        tm("NVL(SUM(TOTAL), 0)"),
        tm("NVL(SUM(CASE WHEN JENIS_BAYAR = 'TUNAI' THEN TOTAL END), 0)"),
        tm("NVL(SUM(CASE WHEN JENIS_BAYAR = 'KREDIT' THEN TOTAL END), 0)"),
    );
    let r = conn.query_row_named(&sql, &[("hari", &hari)])?;
    let ringkasan = Ringkasan {
        jumlah: r.get(0)?,
        total: tampil(dec(&r.get::<_, String>(1)?)?),
        tunai: tampil(dec(&r.get::<_, String>(2)?)?),
        kredit: tampil(dec(&r.get::<_, String>(3)?)?),
    };

    let sql = format!(
        "SELECT NVL(KASIR, '-'), NVL(MAX(NAMA_KASIR), ''), COUNT(*), {} FROM POS_PENJUALAN \
         WHERE TRUNC(TANGGAL) = :hari GROUP BY KASIR ORDER BY SUM(TOTAL) DESC FETCH FIRST 10 ROWS ONLY",
        tm("NVL(SUM(TOTAL), 0)")
    );
    let mut per_kasir = Vec::new();
    for r in conn.query_named(&sql, &[("hari", &hari)])? {
        let r = r?;
        per_kasir.push(PerKasir {
            kasir: r.get(0)?,
            nama: r.get(1)?,
            jumlah: r.get(2)?,
            total: tampil(dec(&r.get::<_, String>(3)?)?),
        });
    }

    let sql = format!(
        "SELECT d.KODE_BARANG, NVL(MAX(d.NAMA_BARANG), ''), {}, {} \
         FROM POS_PENJUALAN_DETAIL d JOIN POS_PENJUALAN m ON m.NO_TRANSAKSI = d.NO_TRANSAKSI \
         WHERE TRUNC(m.TANGGAL) = :hari GROUP BY d.KODE_BARANG \
         ORDER BY SUM(d.TOTAL_HARGA) DESC FETCH FIRST 5 ROWS ONLY",
        tm("NVL(SUM(d.JUMLAH_BARANG), 0)"),
        tm("NVL(SUM(d.TOTAL_HARGA), 0)")
    );
    let mut terlaris = Vec::new();
    for r in conn.query_named(&sql, &[("hari", &hari)])? {
        let r = r?;
        terlaris.push(Terlaris {
            kode: r.get(0)?,
            nama: r.get(1)?,
            qty: dec(&r.get::<_, String>(2)?)?.to_f64().unwrap_or(0.0),
            total: tampil(dec(&r.get::<_, String>(3)?)?),
        });
    }

    let mut user = RingkasanUser::default();
    for u in super::user::list_users(conn)? {
        match u.role {
            crate::auth::Role::Admin => user.admin += 1,
            crate::auth::Role::Kasir => user.kasir += 1,
            crate::auth::Role::Anggota => user.anggota += 1,
        }
        if !u.aktif {
            user.nonaktif += 1;
        }
        if u.terkunci {
            user.terkunci += 1;
        }
    }

    Ok(DashboardAdmin {
        tanggal: hari.format("%Y-%m-%d").to_string(),
        periode_terbuka,
        periode_pesan,
        periode_kode,
        periode,
        periode_dari,
        periode_sampai,
        ringkasan,
        per_kasir,
        terlaris,
        user,
    })
}
