//! Ringkasan harian kasir. Selalu dibatasi ke username kasir dari token.
use chrono::{Duration, NaiveDate};
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

/// Penjualan satu hari (semua kasir): untuk pembanding kemarin dan grafik tren.
#[derive(Debug, Serialize, Clone, PartialEq)]
pub struct HariJual {
    pub tanggal: String,
    pub jumlah: i64,
    pub total: i64,
}

/// Hal yang perlu ditindak admin dan murah dihitung (stok habis dimuat terpisah karena berat).
#[derive(Debug, Serialize)]
pub struct Perhatian {
    /// Produk aktif rugi/impas (harga jual <= harga beli); butuh tindakan.
    pub harga_rendah: i64,
    /// Produk aktif belum punya harga beli (HPP); informasi kelengkapan data.
    pub tanpa_hpp: i64,
}

/// Susun `n` hari berakhir di `hari` (lama -> baru); hari tanpa transaksi diisi 0.
pub fn isi_tren(rows: &[(NaiveDate, i64, i64)], hari: NaiveDate, n: i64) -> Vec<HariJual> {
    (0..n)
        .map(|i| {
            let d = hari - Duration::days(n - 1 - i);
            let (jumlah, total) = rows
                .iter()
                .find(|(t, _, _)| *t == d)
                .map_or((0, 0), |(_, j, t)| (*j, *t));
            HariJual {
                tanggal: d.format("%Y-%m-%d").to_string(),
                jumlah,
                total,
            }
        })
        .collect()
}

/// Penjualan `n` hari terakhir (termasuk `hari`), satu query GROUP BY tanggal.
pub fn tren_penjualan(conn: &Connection, hari: NaiveDate, n: i64) -> AppResult<Vec<HariJual>> {
    let dari = hari - Duration::days(n - 1);
    let besok = hari + Duration::days(1);
    let sql = format!(
        "SELECT TO_CHAR(TRUNC(TANGGAL), 'YYYY-MM-DD'), COUNT(*), {} FROM POS_PENJUALAN          WHERE TANGGAL >= :dari AND TANGGAL < :besok GROUP BY TRUNC(TANGGAL)",
        tm("NVL(SUM(TOTAL), 0)")
    );
    let mut rows = Vec::new();
    for r in conn.query_named(&sql, &[("dari", &dari), ("besok", &besok)])? {
        let r = r?;
        let tgl = NaiveDate::parse_from_str(&r.get::<_, String>(0)?, "%Y-%m-%d")
            .map_err(|e| AppError::Internal(format!("tanggal tren: {e}")))?;
        rows.push((tgl, r.get(1)?, tampil(dec(&r.get::<_, String>(2)?)?)));
    }
    Ok(isi_tren(&rows, hari, n))
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
    /// Penjualan kemarin (pembanding KPI hari ini).
    pub kemarin: HariJual,
    /// 7 hari terakhir termasuk hari ini, lama -> baru.
    pub tren: Vec<HariJual>,
    pub perhatian: Perhatian,
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
            // Oracle menganggap '' sebagai NULL: penjualan lama/desktop bisa tanpa NAMA_KASIR.
            nama: r.get::<_, Option<String>>(1)?.unwrap_or_default(),
            jumlah: r.get(2)?,
            total: tampil(dec(&r.get::<_, String>(3)?)?),
        });
    }

    let sql = format!(
        "SELECT d.KODE_BARANG, NVL(MAX(d.NAMA_BARANG), ''), {}, {} \
         FROM POS_PENJUALAN_DETAIL d JOIN POS_PENJUALAN m ON m.NO_TRANSAKSI = d.NO_TRANSAKSI \
         WHERE TRUNC(m.TANGGAL) = :hari GROUP BY d.KODE_BARANG \
         ORDER BY SUM(d.TOTAL_HARGA) DESC FETCH FIRST 5 ROWS ONLY",
        tm("NVL(SUM(d.JUMLAH_BARANG * d.ISI), 0)"),
        tm("NVL(SUM(d.TOTAL_HARGA), 0)")
    );
    let mut terlaris = Vec::new();
    for r in conn.query_named(&sql, &[("hari", &hari)])? {
        let r = r?;
        terlaris.push(Terlaris {
            kode: r.get(0)?,
            nama: r.get::<_, Option<String>>(1)?.unwrap_or_default(),
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

    let tren = tren_penjualan(conn, hari, 7)?;
    let kemarin = tren[tren.len() - 2].clone();
    let hr = super::katalog::jumlah_harga_rendah(conn)?;
    let perhatian = Perhatian {
        harga_rendah: hr.rugi,
        tanpa_hpp: hr.tanpa_hpp,
    };

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
        kemarin,
        tren,
        perhatian,
    })
}

// ------------------------------------------------------------------ rincian penjualan (tunai/kredit, per kasir)

/// Rentang maksimum rincian (hari): menjaga query dan grafik tetap ringan.
pub const MAKS_HARI_RINCIAN: i64 = 92;

/// Jumlah transaksi dan nilai rupiahnya.
#[derive(Debug, Serialize, Clone, Copy, Default, PartialEq)]
pub struct Bagian {
    pub jumlah: i64,
    pub total: i64,
}

/// Penjualan dipecah menurut jenis bayar. `lain` = selain TUNAI/KREDIT; `total` selalu = tunai + kredit + lain.
#[derive(Debug, Serialize, Clone, Copy, Default, PartialEq)]
pub struct Pecahan {
    pub tunai: Bagian,
    pub kredit: Bagian,
    pub lain: Bagian,
    pub total: Bagian,
}

impl Pecahan {
    fn dari(total: Bagian, tunai: Bagian, kredit: Bagian) -> Self {
        Self {
            lain: Bagian {
                jumlah: total.jumlah - tunai.jumlah - kredit.jumlah,
                total: total.total - tunai.total - kredit.total,
            },
            tunai,
            kredit,
            total,
        }
    }

    fn tambah(&mut self, o: &Pecahan) {
        for (a, b) in [
            (&mut self.tunai, o.tunai),
            (&mut self.kredit, o.kredit),
            (&mut self.lain, o.lain),
            (&mut self.total, o.total),
        ] {
            a.jumlah += b.jumlah;
            a.total += b.total;
        }
    }
}

#[derive(Debug, Serialize, Clone, PartialEq)]
pub struct HarianRincian {
    pub tanggal: String,
    pub tunai: i64,
    pub kredit: i64,
    pub lain: i64,
    pub jumlah: i64,
}

#[derive(Debug, Serialize)]
pub struct KasirRincian {
    pub kasir: String,
    pub nama: String,
    #[serde(flatten)]
    pub pecahan: Pecahan,
}

#[derive(Debug, Serialize)]
pub struct Rincian {
    pub dari: String,
    pub sampai: String,
    pub hari: i64,
    pub ringkasan: Pecahan,
    pub per_kasir: Vec<KasirRincian>,
    pub harian: Vec<HarianRincian>,
}

/// Validasi rentang tanggal rincian: format YYYY-MM-DD, dari <= sampai, tidak melewati hari ini, maks 92 hari.
pub fn validasi_rentang(dari: &str, sampai: &str, hari_ini: NaiveDate) -> AppResult<(NaiveDate, NaiveDate)> {
    let parse = |s: &str, nama: &str| {
        NaiveDate::parse_from_str(s, "%Y-%m-%d")
            .map_err(|_| AppError::validation(format!("Tanggal {nama} harus berformat YYYY-MM-DD")))
    };
    let (d, s) = (parse(dari, "awal")?, parse(sampai, "akhir")?);
    if d > s {
        return Err(AppError::validation("Tanggal awal tidak boleh setelah tanggal akhir"));
    }
    if s > hari_ini {
        return Err(AppError::validation("Tanggal akhir tidak boleh melewati hari ini"));
    }
    if (s - d).num_days() + 1 > MAKS_HARI_RINCIAN {
        return Err(AppError::validation(format!("Rentang maksimal {MAKS_HARI_RINCIAN} hari")));
    }
    Ok((d, s))
}

/// Susun satu baris per hari dari `dari` s/d `sampai` (hari tanpa transaksi = 0).
pub fn isi_harian(rows: &[(NaiveDate, Pecahan)], dari: NaiveDate, sampai: NaiveDate) -> Vec<HarianRincian> {
    let n = (sampai - dari).num_days() + 1;
    (0..n)
        .map(|i| {
            let d = dari + Duration::days(i);
            let p = rows.iter().find(|(t, _)| *t == d).map(|(_, p)| *p).unwrap_or_default();
            HarianRincian {
                tanggal: d.format("%Y-%m-%d").to_string(),
                tunai: p.tunai.total,
                kredit: p.kredit.total,
                lain: p.lain.total,
                jumlah: p.total.jumlah,
            }
        })
        .collect()
}

/// Ukuran yang sama untuk kelompok hari dan kasir: jumlah+total keseluruhan, TUNAI, dan KREDIT.
fn ukuran_rincian() -> String {
    let nilai = |cond: &str| tm(&format!("NVL(SUM(CASE WHEN {cond} THEN TOTAL END), 0)"));
    format!(
        "COUNT(*), {}, COUNT(CASE WHEN JENIS_BAYAR = 'TUNAI' THEN 1 END), {}, COUNT(CASE WHEN JENIS_BAYAR = 'KREDIT' THEN 1 END), {}",
        tm("NVL(SUM(TOTAL), 0)"),
        nilai("JENIS_BAYAR = 'TUNAI'"),
        nilai("JENIS_BAYAR = 'KREDIT'"),
    )
}

/// Baca enam kolom `ukuran_rincian` mulai indeks `o`.
fn baca_pecahan(r: &oracle::Row, o: usize) -> AppResult<Pecahan> {
    let rp = |i: usize| -> AppResult<i64> { Ok(tampil(dec(&r.get::<_, String>(i)?)?)) };
    Ok(Pecahan::dari(
        Bagian { jumlah: r.get(o)?, total: rp(o + 1)? },
        Bagian { jumlah: r.get(o + 2)?, total: rp(o + 3)? },
        Bagian { jumlah: r.get(o + 4)?, total: rp(o + 5)? },
    ))
}

/// Rincian penjualan semua kasir (mobile dan desktop) pada rentang: per hari dan per kasir, tunai vs kredit.
/// Ringkasan dijumlahkan dari baris harian yang sama sehingga selalu cocok dengan jumlah tabel kasir.
pub fn rincian_penjualan(conn: &Connection, dari: NaiveDate, sampai: NaiveDate) -> AppResult<Rincian> {
    let besok = sampai + Duration::days(1);
    let sql = format!(
        "SELECT TO_CHAR(TRUNC(TANGGAL), 'YYYY-MM-DD'), {} FROM POS_PENJUALAN \
         WHERE TANGGAL >= :dari AND TANGGAL < :besok GROUP BY TRUNC(TANGGAL)",
        ukuran_rincian()
    );
    let mut rows = Vec::new();
    for r in conn.query_named(&sql, &[("dari", &dari), ("besok", &besok)])? {
        let r = r?;
        let tgl = NaiveDate::parse_from_str(&r.get::<_, String>(0)?, "%Y-%m-%d")
            .map_err(|e| AppError::Internal(format!("tanggal rincian: {e}")))?;
        rows.push((tgl, baca_pecahan(&r, 1)?));
    }
    let mut ringkasan = Pecahan::default();
    for (_, p) in &rows {
        ringkasan.tambah(p);
    }

    let sql = format!(
        "SELECT NVL(KASIR, '-'), NVL(MAX(NAMA_KASIR), ''), {} FROM POS_PENJUALAN \
         WHERE TANGGAL >= :dari AND TANGGAL < :besok GROUP BY KASIR ORDER BY SUM(TOTAL) DESC FETCH FIRST 50 ROWS ONLY",
        ukuran_rincian()
    );
    let mut per_kasir = Vec::new();
    for r in conn.query_named(&sql, &[("dari", &dari), ("besok", &besok)])? {
        let r = r?;
        per_kasir.push(KasirRincian {
            kasir: r.get(0)?,
            // Oracle menganggap '' sebagai NULL: penjualan lama/desktop bisa tanpa NAMA_KASIR.
            nama: r.get::<_, Option<String>>(1)?.unwrap_or_default(),
            pecahan: baca_pecahan(&r, 2)?,
        });
    }

    Ok(Rincian {
        dari: dari.format("%Y-%m-%d").to_string(),
        sampai: sampai.format("%Y-%m-%d").to_string(),
        hari: (sampai - dari).num_days() + 1,
        ringkasan,
        per_kasir,
        harian: isi_harian(&rows, dari, sampai),
    })
}

#[cfg(test)]
mod rincian_tests {
    use super::*;

    fn d(s: &str) -> NaiveDate {
        NaiveDate::parse_from_str(s, "%Y-%m-%d").unwrap()
    }

    fn bagian(jumlah: i64, total: i64) -> Bagian {
        Bagian { jumlah, total }
    }

    #[test]
    fn pecahan_menghitung_jenis_lain_dan_menjaga_total() {
        let p = Pecahan::dari(bagian(10, 1000), bagian(6, 600), bagian(3, 300));
        assert_eq!(p.lain, bagian(1, 100));
        assert_eq!(p.tunai.total + p.kredit.total + p.lain.total, p.total.total);
        let nol = Pecahan::dari(bagian(0, 0), bagian(0, 0), bagian(0, 0));
        assert_eq!(nol, Pecahan::default());
    }

    #[test]
    fn rentang_divalidasi() {
        let hari = d("2026-11-18");
        assert_eq!(validasi_rentang("2026-11-18", "2026-11-18", hari).unwrap(), (hari, hari));
        assert!(validasi_rentang("2026-11-12", "2026-11-18", hari).is_ok());
        for (a, b) in [
            ("2026-11-19", "2026-11-18"), // terbalik
            ("2026-11-18", "2026-11-19"), // masa depan
            ("2026-01-01", "2026-11-18"), // > 92 hari
            ("18-11-2026", "2026-11-18"), // format salah
            ("2026-11-18", ""),
        ] {
            assert!(validasi_rentang(a, b, hari).is_err(), "{a} {b}");
        }
        // Tepat 92 hari diterima, 93 ditolak.
        assert!(validasi_rentang("2026-08-18", "2026-11-17", hari).is_ok());
        assert!(validasi_rentang("2026-08-17", "2026-11-17", hari).is_err());
    }

    #[test]
    fn harian_mengisi_hari_kosong_dan_lintas_tahun() {
        let p = Pecahan::dari(bagian(3, 300), bagian(2, 200), bagian(1, 100));
        let h = isi_harian(&[(d("2026-12-31"), p)], d("2026-12-30"), d("2027-01-01"));
        assert_eq!(h.len(), 3);
        assert_eq!((h[0].tanggal.as_str(), h[0].jumlah), ("2026-12-30", 0));
        assert_eq!((h[1].tunai, h[1].kredit, h[1].lain, h[1].jumlah), (200, 100, 0, 3));
        assert_eq!(h[2].tanggal, "2027-01-01");
        assert_eq!(isi_harian(&[], d("2026-11-18"), d("2026-11-18")).len(), 1);
    }

    #[test]
    fn ringkasan_menjumlahkan_pecahan() {
        let a = Pecahan::dari(bagian(3, 300), bagian(2, 200), bagian(1, 100));
        let b = Pecahan::dari(bagian(2, 500), bagian(0, 0), bagian(2, 500));
        let mut r = Pecahan::default();
        r.tambah(&a);
        r.tambah(&b);
        assert_eq!(r.total, bagian(5, 800));
        assert_eq!((r.tunai, r.kredit), (bagian(2, 200), bagian(3, 600)));
    }
}

#[cfg(test)]
mod tren_tests {
    use super::*;

    fn d(s: &str) -> NaiveDate {
        NaiveDate::parse_from_str(s, "%Y-%m-%d").unwrap()
    }

    #[test]
    fn tren_mengisi_hari_kosong_dan_berurutan() {
        let rows = [(d("2026-11-17"), 3, 300), (d("2026-11-13"), 1, 50)];
        let t = isi_tren(&rows, d("2026-11-18"), 7);
        assert_eq!(t.len(), 7);
        assert_eq!(t[0].tanggal, "2026-11-12");
        assert_eq!(t[6].tanggal, "2026-11-18");
        assert_eq!((t[6].jumlah, t[6].total), (0, 0)); // hari ini belum ada transaksi
        assert_eq!((t[5].jumlah, t[5].total), (3, 300)); // kemarin
        assert_eq!((t[1].jumlah, t[1].total), (1, 50));
        assert_eq!(t.iter().filter(|h| h.total == 0).count(), 5);
    }

    #[test]
    fn tren_melewati_pergantian_bulan_dan_tahun() {
        let t = isi_tren(&[], d("2027-01-02"), 7);
        assert_eq!(t[0].tanggal, "2026-12-27");
        assert_eq!(t[6].tanggal, "2027-01-02");
    }
}
