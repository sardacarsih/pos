//! Pinjaman tunai anggota: pengajuan (anggota) -> verifikasi -> persetujuan -> antrian dana -> pencairan.
//! Pencairan menulis FIN_PINJAMAN + FIN_PINJAMAN_DETAIL persis seperti BackOffice (ucPinjaman), sehingga
//! tagihan potong gaji, laporan desktop, dan tarik KAS tetap membaca dari tabel lama.
use chrono::{Datelike, NaiveDate, NaiveDateTime};
use oracle::Connection;
use rust_decimal::{Decimal, prelude::ToPrimitive};
use serde::{Deserialize, Serialize};
use serde_json::{Value, json};

use super::{faktur, katalog, num, opt, pembelian::{Operator, transaksi, tulis}};
use crate::{
    domain::{nomor, pinjaman as hitung},
    error::{AppError, AppResult},
};

pub const BERJALAN: &str = "('DIAJUKAN','DIVERIFIKASI','DISETUJUI')";
/// Pinjaman aktif (alias `p` = FIN_PINJAMAN) belum lunas dan memiliki jadwal yang masih berjalan.
const AKTIF: &str = "NVL(p.ISLUNAS,'T')='T'
  AND (SELECT MAX(d.TANGGALJATUHTEMPO) FROM FIN_PINJAMAN_DETAIL d WHERE d.NO_TRANSAKSI=p.NO_TRANSAKSI) >= TRUNC(SYSDATE)";
const MAKS_RUPIAH: i64 = 9_000_000_000_000;

pub fn migrate(c: &Connection) -> AppResult<usize> {
    let mut n = 0;
    for ddl in include_str!("pinjaman.sql").replace("\r\n", "\n").split("\n/\n") {
        let ddl: String = ddl.lines().filter(|l| !l.trim_start().starts_with("--")).collect::<Vec<_>>().join("\n");
        match c.execute(ddl.trim(), &[]) {
            Ok(_) => n += 1,
            Err(e) if matches!(e.oci_code(), Some(955 | 1408 | 1430)) => {}
            Err(e) => return Err(e.into()),
        }
    }
    Ok(n)
}

// ---------- Aturan (FIN_SETTINGS, default sama dengan BackOffice FinSetting) ----------

#[derive(Serialize, Clone, Copy)]
pub struct Aturan {
    pub bunga: f64,
    pub maks_tenor: i32,
    /// 0 = tanpa batas.
    pub maks_pinjaman: i64,
}

pub fn aturan(c: &Connection) -> AppResult<Aturan> {
    let ambil = |k: &str| katalog::setting(c, k);
    Ok(Aturan {
        bunga: ambil("BUNGA_EFEKTIF")?.and_then(|d| d.to_f64()).unwrap_or(1.6),
        maks_tenor: ambil("MAXANGSURAN")?.and_then(|d| d.to_i32()).unwrap_or(12),
        maks_pinjaman: ambil("MAX_PINJAMAN")?.and_then(|d| d.to_i64()).unwrap_or(0),
    })
}

pub fn simpan_maks_pinjaman(c: &Connection, nilai: i64) -> AppResult<Value> {
    if !(0..=MAKS_RUPIAH).contains(&nilai) {
        return Err(AppError::validation("Plafon maksimal pinjaman tidak valid"));
    }
    transaksi(c, || {
        c.execute_named(
            "MERGE INTO FIN_SETTINGS t USING (SELECT 'MAX_PINJAMAN' CONFIG FROM DUAL) s ON (t.CONFIG=s.CONFIG)
             WHEN MATCHED THEN UPDATE SET JUMLAH=:n WHEN NOT MATCHED THEN INSERT (CONFIG,JUMLAH) VALUES ('MAX_PINJAMAN',:n)",
            &[("n", &nilai)],
        )?;
        Ok(json!({ "maks_pinjaman": nilai }))
    })
}

// ---------- Kelayakan ----------

pub struct Anggota {
    pub id: i64,
    pub nama: String,
    pub status: Option<String>,
    pub unit_kerja: Option<String>,
}

/// Mengunci baris anggota (menyerialkan pengajuan/pencairan per anggota) lalu cek semua aturan.
fn kelayakan(c: &Connection, nik: &str, jumlah: i64, tenor: i32, kecuali: Option<i64>) -> AppResult<(Anggota, Aturan)> {
    let r = opt(c.query_row_named(
        "SELECT ID_PELANGGAN,NAMA_PELANGGAN,STATUS,UNIT_KERJA FROM FIN_ANGGOTA
         WHERE NIK=:nik AND AKTIF='Y' AND NVL(ANGGOTA,'Y')='Y' FOR UPDATE",
        &[("nik", &nik)],
    ))?
    .ok_or_else(|| AppError::rule("ANGGOTA_TIDAK_AKTIF", "Hanya anggota aktif yang dapat mengajukan pinjaman"))?;
    let a = Anggota { id: r.get(0)?, nama: r.get(1)?, status: r.get(2)?, unit_kerja: r.get(3)? };
    let at = aturan(c)?;
    if !(1..=at.maks_tenor).contains(&tenor) {
        return Err(AppError::rule("TENOR", format!("Tenor harus 1 sampai {} bulan", at.maks_tenor)));
    }
    if jumlah <= 0 || jumlah > MAKS_RUPIAH {
        return Err(AppError::validation("Jumlah pinjaman harus lebih dari nol"));
    }
    if at.maks_pinjaman > 0 && jumlah > at.maks_pinjaman {
        return Err(AppError::rule(
            "MELEBIHI_PLAFON_ANGGOTA",
            format!("Jumlah pinjaman maksimal Rp {}", at.maks_pinjaman),
        ));
    }
    let aktif: i64 = c.query_row_as_named(
        &format!("SELECT COUNT(*) FROM FIN_PINJAMAN p WHERE p.NIK=:nik AND {AKTIF}"),
        &[("nik", &nik)],
    )?;
    if aktif > 0 {
        return Err(AppError::rule("PINJAMAN_AKTIF", "Masih ada pinjaman yang belum lunas"));
    }
    let jalan: i64 = c.query_row_as_named(
        &format!("SELECT COUNT(*) FROM MOB_PINJAMAN_AJU WHERE NIK=:nik AND STATUS IN {BERJALAN} AND ID<>NVL(:id,-1)"),
        &[("nik", &nik), ("id", &kecuali)],
    )?;
    if jalan > 0 {
        return Err(AppError::rule("PENGAJUAN_BERJALAN", "Masih ada pengajuan pinjaman yang sedang diproses"));
    }
    Ok((a, at))
}

fn teks(v: Option<&str>, panjang: usize, label: &str, wajib: bool) -> AppResult<Option<String>> {
    let v = v.map(str::trim).filter(|v| !v.is_empty());
    match v {
        None if wajib => Err(AppError::validation(format!("{label} wajib diisi"))),
        Some(v) if v.chars().count() > panjang || v.chars().any(char::is_control) => {
            Err(AppError::validation(format!("{label} maksimal {panjang} karakter")))
        }
        v => Ok(v.map(str::to_owned)),
    }
}

// ---------- Anggota ----------

#[derive(Serialize, Deserialize)]
pub struct AjuReq {
    pub jumlah: i64,
    pub tenor: i32,
    pub keperluan: String,
}

pub fn simulasi(c: &Connection, jumlah: i64, tenor: i32) -> AppResult<Value> {
    let at = aturan(c)?;
    if !(1..=at.maks_tenor).contains(&tenor) {
        return Err(AppError::rule("TENOR", format!("Tenor harus 1 sampai {} bulan", at.maks_tenor)));
    }
    let hari = faktur::sekarang(c)?.0;
    let jadwal = hitung::jadwal(hari, jumlah, tenor, at.bunga).map_err(AppError::validation)?;
    Ok(json!({ "aturan": at, "angsuran": jadwal[0].angsuran.to_f64(), "jadwal": jadwal }))
}

pub fn ajukan(c: &Connection, u: &Operator, nik: &str, key: &str, p: &AjuReq) -> AppResult<String> {
    tulis(c, u, key, "PINJAMAN_AJU", p, || {
        let keperluan = teks(Some(&p.keperluan), 200, "Keperluan", true)?.unwrap_or_default();
        let (a, at) = kelayakan(c, nik, p.jumlah, p.tenor, None)?;
        let hari = faktur::sekarang(c)?.0;
        let jadwal = hitung::jadwal(hari, p.jumlah, p.tenor, at.bunga).map_err(AppError::validation)?;
        let s = c.execute_named(
            &format!(
                "INSERT INTO MOB_PINJAMAN_AJU (NIK,NAMA,JUMLAH,TENOR,BUNGA,ANGSURAN,KEPERLUAN,AJU_USER)
                 VALUES (:nik,:nama,:j,:t,{},{},:k,:u) RETURNING ID INTO :id",
                num("b"), num("ang")
            ),
            &[("nik", &nik), ("nama", &a.nama), ("j", &p.jumlah), ("t", &p.tenor), ("b", &at.bunga.to_string()),
              ("ang", &jadwal[0].angsuran.to_string()), ("k", &keperluan), ("u", &u.id), ("id", &None::<i64>)],
        );
        let s = match s {
            Err(e) if e.oci_code() == Some(1) => {
                return Err(AppError::rule("PENGAJUAN_BERJALAN", "Masih ada pengajuan pinjaman yang sedang diproses"));
            }
            s => s?,
        };
        Ok(s.returned_values::<_, i64>("id")?.into_iter().next().unwrap_or_default().to_string())
    })
}

pub fn batal(c: &Connection, u: &Operator, nik: &str, id: i64, key: &str) -> AppResult<String> {
    tulis(c, u, key, "PINJAMAN_BATAL", &id, || {
        let n = c
            .execute_named(
                "UPDATE MOB_PINJAMAN_AJU SET STATUS='DIBATALKAN' WHERE ID=:id AND NIK=:nik AND STATUS='DIAJUKAN'",
                &[("id", &id), ("nik", &nik)],
            )?
            .row_count()?;
        if n == 0 {
            return Err(AppError::rule("STATUS", "Pengajuan hanya dapat dibatalkan sebelum diverifikasi"));
        }
        Ok(id.to_string())
    })
}

// ---------- Daftar / detail ----------

const KOLOM: &str = "a.ID,a.NIK,a.NAMA,a.JUMLAH,a.TENOR,TO_CHAR(a.BUNGA,'TM9','NLS_NUMERIC_CHARACTERS=''.,'''),
  TO_CHAR(a.ANGSURAN,'TM9','NLS_NUMERIC_CHARACTERS=''.,'''),a.KEPERLUAN,a.STATUS,a.TGL_AJU,
  a.VERIF_NAMA,a.VERIF_TGL,a.VERIF_CATATAN,a.SETUJU_NAMA,a.SETUJU_TGL,a.SETUJU_CATATAN,
  a.TOLAK_NAMA,a.TOLAK_TGL,a.ALASAN,a.CAIR_NAMA,a.CAIR_TGL,a.NO_TRANSAKSI,
  CASE WHEN a.STATUS='DISETUJUI' THEN (SELECT COUNT(*) FROM MOB_PINJAMAN_AJU q WHERE q.STATUS='DISETUJUI'
    AND (q.SETUJU_TGL < a.SETUJU_TGL OR (q.SETUJU_TGL=a.SETUJU_TGL AND q.ID<=a.ID))) END,
  a.VERIF_USER,a.SETUJU_USER";

fn baris(r: &oracle::Row) -> AppResult<Value> {
    let t = |i: usize| -> AppResult<Option<NaiveDateTime>> { Ok(r.get(i)?) };
    let s = |i: usize| -> AppResult<Option<String>> { Ok(r.get(i)?) };
    Ok(json!({
        "id": r.get::<_, i64>(0)?, "nik": r.get::<_, String>(1)?, "nama": r.get::<_, String>(2)?,
        "jumlah": r.get::<_, i64>(3)?, "tenor": r.get::<_, i64>(4)?,
        "bunga": r.get::<_, String>(5)?.parse::<f64>().unwrap_or_default(),
        "angsuran": r.get::<_, String>(6)?.parse::<f64>().unwrap_or_default(),
        "keperluan": r.get::<_, String>(7)?, "status": r.get::<_, String>(8)?, "tgl_aju": t(9)?,
        "verif_nama": s(10)?, "verif_tgl": t(11)?, "verif_catatan": s(12)?,
        "setuju_nama": s(13)?, "setuju_tgl": t(14)?, "setuju_catatan": s(15)?,
        "tolak_nama": s(16)?, "tolak_tgl": t(17)?, "alasan": s(18)?,
        "cair_nama": s(19)?, "cair_tgl": t(20)?, "no_transaksi": s(21)?,
        "antrian": r.get::<_, Option<i64>>(22)?,
        "verif_user": r.get::<_, Option<i64>>(23)?, "setuju_user": r.get::<_, Option<i64>>(24)?,
    }))
}

fn kumpul(c: &Connection, sql: &str, binds: &[(&str, &dyn oracle::sql_type::ToSql)]) -> AppResult<Vec<Value>> {
    c.query_named(sql, binds)?.map(|r| baris(&r?)).collect()
}

/// Riwayat milik anggota + aturan untuk form.
pub fn saya(c: &Connection, nik: &str) -> AppResult<Value> {
    let items = kumpul(
        c,
        &format!("SELECT {KOLOM} FROM MOB_PINJAMAN_AJU a WHERE a.NIK=:nik ORDER BY a.ID DESC FETCH FIRST 50 ROWS ONLY"),
        &[("nik", &nik)],
    )?;
    let aktif = c
        .query_named(
            &format!(
                "SELECT p.NO_TRANSAKSI,p.TANGGAL,p.PINJAMAN,p.TENOR,TO_CHAR(p.ANGSURAN,'TM9','NLS_NUMERIC_CHARACTERS=''.,'''),
                        (SELECT COUNT(*) FROM FIN_PINJAMAN_DETAIL d WHERE d.NO_TRANSAKSI=p.NO_TRANSAKSI AND d.TANGGALJATUHTEMPO < TRUNC(SYSDATE))
                 FROM FIN_PINJAMAN p WHERE p.NIK=:nik AND {AKTIF} ORDER BY p.TANGGAL DESC"
            ),
            &[("nik", &nik)],
        )?
        .map(|r| -> AppResult<Value> {
            let r = r?;
            Ok(json!({
                "no_transaksi": r.get::<_, String>(0)?, "tanggal": r.get::<_, NaiveDateTime>(1)?.date(),
                "pinjaman": r.get::<_, i64>(2)?, "tenor": r.get::<_, i64>(3)?,
                "angsuran": r.get::<_, String>(4)?.parse::<f64>().unwrap_or_default(),
                "angsuran_lewat": r.get::<_, i64>(5)?,
            }))
        })
        .collect::<AppResult<Vec<_>>>()?;
    Ok(json!({ "aturan": aturan(c)?, "pengajuan": items, "pinjaman_aktif": aktif }))
}

/// Laporan pinjaman aktif semua anggota. Estimasi sisa pokok memakai saldo awal angsuran
/// pertama dengan tanggal jatuh tempo hari ini atau sesudahnya.
pub fn laporan(c: &Connection, q: Option<&str>) -> AppResult<Value> {
    let q = q.map(str::trim).filter(|s| !s.is_empty()).map(super::like);
    let tm = |kol: &str| format!("TO_CHAR({kol},'TM9','NLS_NUMERIC_CHARACTERS=''.,''')");
    let sql = format!(
        "SELECT p.NO_TRANSAKSI,p.TANGGAL,p.NIK,p.NAMA_PELANGGAN,p.UNIT_KERJA,p.PINJAMAN,p.TENOR,{},
                (SELECT COUNT(*) FROM FIN_PINJAMAN_DETAIL d WHERE d.NO_TRANSAKSI=p.NO_TRANSAKSI AND d.TANGGALJATUHTEMPO < TRUNC(SYSDATE)),
                (SELECT {} FROM FIN_PINJAMAN_DETAIL d WHERE d.NO_TRANSAKSI=p.NO_TRANSAKSI AND d.TANGGALJATUHTEMPO >= TRUNC(SYSDATE)),
                (SELECT MAX(d.TANGGALJATUHTEMPO) FROM FIN_PINJAMAN_DETAIL d WHERE d.NO_TRANSAKSI=p.NO_TRANSAKSI)
         FROM FIN_PINJAMAN p
         WHERE {AKTIF} AND (:q IS NULL OR UPPER(p.NAMA_PELANGGAN) LIKE :q ESCAPE '\\' OR UPPER(p.NIK) LIKE :q ESCAPE '\\')
         ORDER BY p.NAMA_PELANGGAN, p.TANGGAL, p.NO_TRANSAKSI",
        tm("p.ANGSURAN"),
        tm("MIN(d.SALDOAWAL) KEEP (DENSE_RANK FIRST ORDER BY d.TANGGALJATUHTEMPO, d.ANGSURANKE)"),
    );
    let angka = |s: Option<String>| s.and_then(|v| v.parse::<f64>().ok()).unwrap_or_default();
    let (mut total_pinjaman, mut total_sisa) = (0i64, 0f64);
    let mut items = Vec::new();
    for r in c.query_named(&sql, &[("q", &q)])? {
        let r = r?;
        let pinjaman: i64 = r.get(5)?;
        let sisa = angka(r.get(9)?);
        total_pinjaman += pinjaman;
        total_sisa += sisa;
        items.push(json!({
            "no_transaksi": r.get::<_, String>(0)?, "tanggal": r.get::<_, NaiveDateTime>(1)?.date(),
            "nik": r.get::<_, Option<String>>(2)?, "nama": r.get::<_, Option<String>>(3)?,
            "unit_kerja": r.get::<_, Option<String>>(4)?, "pinjaman": pinjaman, "tenor": r.get::<_, i64>(6)?,
            "angsuran": angka(r.get(7)?), "angsuran_lewat": r.get::<_, i64>(8)?, "sisa_pokok": sisa,
            "jatuh_tempo_akhir": r.get::<_, Option<NaiveDateTime>>(10)?.map(|d| d.date()),
        }));
    }
    Ok(json!({ "jumlah": items.len(), "items": items, "total_pinjaman": total_pinjaman, "total_sisa_pokok": total_sisa }))
}

#[derive(Deserialize)]
pub struct Filter {
    pub status: Option<String>,
    pub q: Option<String>,
}

pub fn daftar(c: &Connection, f: &Filter) -> AppResult<Value> {
    let status = f.status.as_deref().filter(|s| !s.is_empty());
    let q = f.q.as_deref().map(str::trim).filter(|s| !s.is_empty()).map(super::like);
    let items = kumpul(
        c,
        &format!(
            "SELECT {KOLOM} FROM MOB_PINJAMAN_AJU a
             WHERE (:st IS NULL OR a.STATUS=:st) AND (:q IS NULL OR UPPER(a.NAMA) LIKE :q ESCAPE '\\' OR a.NIK LIKE :q ESCAPE '\\')
             ORDER BY CASE a.STATUS WHEN 'DIAJUKAN' THEN 1 WHEN 'DIVERIFIKASI' THEN 2 WHEN 'DISETUJUI' THEN 3 ELSE 4 END, a.ID DESC
             FETCH FIRST 200 ROWS ONLY"
        ),
        &[("st", &status), ("q", &q)],
    )?;
    Ok(json!({ "items": items }))
}

pub fn detail(c: &Connection, id: i64) -> AppResult<Value> {
    let mut v = kumpul(c, &format!("SELECT {KOLOM} FROM MOB_PINJAMAN_AJU a WHERE a.ID=:id"), &[("id", &id)])?
        .pop()
        .ok_or_else(|| AppError::NotFound("Pengajuan tidak ditemukan".into()))?;
    let nik = v["nik"].as_str().unwrap_or_default().to_owned();
    let riwayat: i64 = c.query_row_as_named("SELECT COUNT(*) FROM FIN_PINJAMAN WHERE NIK=:nik", &[("nik", &nik)])?;
    let jumlah = v["jumlah"].as_i64().unwrap_or_default();
    let tenor = v["tenor"].as_i64().unwrap_or(1) as i32;
    let bunga = v["bunga"].as_f64().unwrap_or_default();
    let hari = faktur::sekarang(c)?.0;
    v["jadwal"] = json!(hitung::jadwal(hari, jumlah, tenor, bunga).unwrap_or_default());
    v["jumlah_pinjaman_lalu"] = json!(riwayat);
    Ok(v)
}

// ---------- Hak tahap ----------

#[derive(Debug, Clone, Copy, Serialize, Deserialize, PartialEq, Eq)]
#[serde(rename_all = "UPPERCASE")]
pub enum Tahap {
    Verifikasi,
    Setuju,
    Cair,
}

impl Tahap {
    fn kode(self) -> &'static str {
        match self {
            Tahap::Verifikasi => "VERIFIKASI",
            Tahap::Setuju => "SETUJU",
            Tahap::Cair => "CAIR",
        }
    }
}

fn hak(c: &Connection, u: &Operator, t: Tahap) -> AppResult<()> {
    let n: i64 = c.query_row_as_named(
        "SELECT COUNT(*) FROM MOB_PINJAMAN_HAK WHERE USER_ID=:u AND TAHAP=:t",
        &[("u", &u.id), ("t", &t.kode())],
    )?;
    if n == 0 {
        return Err(AppError::rule("HAK_TIDAK_ADA", format!("Akun ini tidak punya hak tahap {}", t.kode())));
    }
    Ok(())
}

pub fn hak_saya(c: &Connection, u: &Operator) -> AppResult<Vec<String>> {
    Ok(c.query_as_named::<String>("SELECT TAHAP FROM MOB_PINJAMAN_HAK WHERE USER_ID=:u", &[("u", &u.id)])?
        .collect::<Result<_, _>>()?)
}

/// Semua admin aktif beserta hak tahapnya.
pub fn daftar_hak(c: &Connection) -> AppResult<Value> {
    let mut out = Vec::new();
    for r in c.query(
        "SELECT u.USER_ID,u.USERNAME,NVL(u.FULL_NAME,u.USERNAME),
                (SELECT LISTAGG(h.TAHAP,',') WITHIN GROUP (ORDER BY h.TAHAP) FROM MOB_PINJAMAN_HAK h WHERE h.USER_ID=u.USER_ID)
         FROM MOB_USER u WHERE u.ROLE='ADMIN' AND u.IS_ACTIVE=1 ORDER BY u.USERNAME",
        &[],
    )? {
        let r = r?;
        let tahap: Vec<String> =
            r.get::<_, Option<String>>(3)?.map(|s| s.split(',').map(str::to_owned).collect()).unwrap_or_default();
        out.push(json!({ "id": r.get::<_, i64>(0)?, "username": r.get::<_, String>(1)?, "nama": r.get::<_, String>(2)?, "tahap": tahap }));
    }
    Ok(json!({ "users": out }))
}

#[derive(Deserialize)]
pub struct HakReq {
    pub user_id: i64,
    pub tahap: Vec<Tahap>,
}

pub fn simpan_hak(c: &Connection, p: &HakReq) -> AppResult<Value> {
    transaksi(c, || {
        let admin: i64 = c.query_row_as_named(
            "SELECT COUNT(*) FROM MOB_USER WHERE USER_ID=:u AND ROLE='ADMIN'",
            &[("u", &p.user_id)],
        )?;
        if admin == 0 {
            return Err(AppError::validation("Hak tahap hanya untuk akun ADMIN"));
        }
        c.execute_named("DELETE FROM MOB_PINJAMAN_HAK WHERE USER_ID=:u", &[("u", &p.user_id)])?;
        for t in &p.tahap {
            c.execute_named(
                "INSERT INTO MOB_PINJAMAN_HAK (USER_ID,TAHAP) VALUES (:u,:t)",
                &[("u", &p.user_id), ("t", &t.kode())],
            )?;
        }
        Ok(json!({ "ok": true }))
    })
}

// ---------- Plafon dana & antrian ----------

/// Periode berjalan dari rentang POS_PERIODE (bukan bulan kalender).
fn periode_kini(c: &Connection) -> AppResult<faktur::InfoPeriode> {
    let hari = faktur::sekarang(c)?.0;
    faktur::info_periode(c, hari)?
        .ok_or_else(|| AppError::rule("PERIODE_TIDAK_ADA", "Periode untuk hari ini belum diatur"))
}

fn terpakai(c: &Connection, p: &faktur::InfoPeriode) -> AppResult<i64> {
    let d: Option<String> = c.query_row_as_named(
        "SELECT TO_CHAR(SUM(PINJAMAN),'TM9','NLS_NUMERIC_CHARACTERS=''.,''') FROM FIN_PINJAMAN WHERE TANGGAL >= :dari AND TANGGAL < :sampai + 1",
        &[("dari", &p.dari), ("sampai", &p.sampai)],
    )?;
    Ok(d.map(|s| super::dec(&s)).transpose()?.unwrap_or_default().ceil().to_i64().unwrap_or(i64::MAX))
}

fn plafon(c: &Connection, periode: i32, kunci: bool) -> AppResult<Option<i64>> {
    let sql = format!("SELECT PLAFON FROM MOB_PINJAMAN_PLAFON WHERE PERIODE=:p{}", if kunci { " FOR UPDATE" } else { "" });
    opt(c.query_row_as_named::<i64>(&sql, &[("p", &periode)]))
}

pub fn antrian(c: &Connection) -> AppResult<Value> {
    let p = periode_kini(c)?;
    let pl = plafon(c, p.periode, false)?;
    let pakai = terpakai(c, &p)?;
    let items = kumpul(
        c,
        &format!("SELECT {KOLOM} FROM MOB_PINJAMAN_AJU a WHERE a.STATUS='DISETUJUI' ORDER BY a.SETUJU_TGL,a.ID"),
        &[],
    )?;
    Ok(json!({
        "periode": p.periode, "dari": p.dari, "sampai": p.sampai,
        "plafon": pl, "terpakai": pakai, "sisa": pl.map(|v| v - pakai), "items": items,
    }))
}

#[derive(Deserialize)]
pub struct PlafonReq {
    pub plafon: i64,
}

pub fn simpan_plafon(c: &Connection, u: &Operator, periode: i32, p: &PlafonReq) -> AppResult<Value> {
    if !(0..=MAKS_RUPIAH).contains(&p.plafon) {
        return Err(AppError::validation("Plafon dana tidak valid"));
    }
    transaksi(c, || {
        c.execute_named(
            "MERGE INTO MOB_PINJAMAN_PLAFON t USING (SELECT :p PERIODE FROM DUAL) s ON (t.PERIODE=s.PERIODE)
             WHEN MATCHED THEN UPDATE SET PLAFON=:n, DIUBAH_OLEH=:u, DIUBAH_TGL=SYSTIMESTAMP
             WHEN NOT MATCHED THEN INSERT (PERIODE,PLAFON,DIUBAH_OLEH) VALUES (:p,:n,:u)",
            &[("p", &periode), ("n", &p.plafon), ("u", &u.nama)],
        )?;
        Ok(json!({ "periode": periode, "plafon": p.plafon }))
    })
}

// ---------- Proses tahap ----------

#[derive(Debug, Serialize, Deserialize, Clone, Copy, PartialEq, Eq)]
#[serde(rename_all = "lowercase")]
pub enum Aksi {
    Verifikasi,
    Setujui,
    Tolak,
    Cairkan,
}

#[derive(Serialize, Deserialize)]
pub struct ProsesReq {
    pub catatan: Option<String>,
}

pub fn proses(c: &Connection, u: &Operator, id: i64, aksi: Aksi, key: &str, p: &ProsesReq) -> AppResult<String> {
    tulis(c, u, key, "PINJAMAN_PROSES", &(id, aksi, p), || {
        let r = opt(c.query_row_named(
            "SELECT STATUS,NIK,JUMLAH,TENOR,VERIF_USER,SETUJU_USER FROM MOB_PINJAMAN_AJU WHERE ID=:id FOR UPDATE",
            &[("id", &id)],
        ))?
        .ok_or_else(|| AppError::NotFound("Pengajuan tidak ditemukan".into()))?;
        let status: String = r.get(0)?;
        let nik: String = r.get(1)?;
        let (jumlah, tenor): (i64, i32) = (r.get(2)?, r.get(3)?);
        let pelaku: [Option<i64>; 2] = [r.get(4)?, r.get(5)?];
        let salah_status = || AppError::rule("STATUS", format!("Pengajuan berstatus {status}, aksi tidak dapat dilakukan"));
        let sudah_pegang = || pelaku.contains(&Some(u.id));
        let catatan = teks(p.catatan.as_deref(), 200, "Catatan", aksi == Aksi::Tolak)?;
        match aksi {
            Aksi::Verifikasi => {
                if status != "DIAJUKAN" { return Err(salah_status()); }
                hak(c, u, Tahap::Verifikasi)?;
                kelayakan(c, &nik, jumlah, tenor, Some(id))?;
                c.execute_named(
                    "UPDATE MOB_PINJAMAN_AJU SET STATUS='DIVERIFIKASI',VERIF_USER=:u,VERIF_NAMA=:n,VERIF_TGL=SYSTIMESTAMP,VERIF_CATATAN=:k WHERE ID=:id",
                    &[("u", &u.id), ("n", &u.nama), ("k", &catatan), ("id", &id)],
                )?;
            }
            Aksi::Setujui => {
                if status != "DIVERIFIKASI" { return Err(salah_status()); }
                hak(c, u, Tahap::Setuju)?;
                if sudah_pegang() { return Err(tahap_sama()); }
                c.execute_named(
                    "UPDATE MOB_PINJAMAN_AJU SET STATUS='DISETUJUI',SETUJU_USER=:u,SETUJU_NAMA=:n,SETUJU_TGL=SYSTIMESTAMP,SETUJU_CATATAN=:k WHERE ID=:id",
                    &[("u", &u.id), ("n", &u.nama), ("k", &catatan), ("id", &id)],
                )?;
            }
            Aksi::Tolak => {
                let t = match status.as_str() {
                    "DIAJUKAN" => Tahap::Verifikasi,
                    "DIVERIFIKASI" | "DISETUJUI" => Tahap::Setuju,
                    _ => return Err(salah_status()),
                };
                hak(c, u, t)?;
                c.execute_named(
                    "UPDATE MOB_PINJAMAN_AJU SET STATUS='DITOLAK',TOLAK_USER=:u,TOLAK_NAMA=:n,TOLAK_TGL=SYSTIMESTAMP,ALASAN=:k WHERE ID=:id",
                    &[("u", &u.id), ("n", &u.nama), ("k", &catatan), ("id", &id)],
                )?;
            }
            Aksi::Cairkan => {
                if status != "DISETUJUI" { return Err(salah_status()); }
                hak(c, u, Tahap::Cair)?;
                if sudah_pegang() { return Err(tahap_sama()); }
                return cairkan(c, u, id, &nik, jumlah, tenor);
            }
        }
        let rp = format!("Rp {}", ribuan(jumlah));
        match aksi {
            Aksi::Verifikasi => kabari(c, id, 1, &nik, &format!("Pengajuan pinjaman {rp} sudah diverifikasi dan menunggu persetujuan."))?,
            Aksi::Setujui => {
                let urut: i64 = c.query_row_as_named(
                    "SELECT COUNT(*) FROM MOB_PINJAMAN_AJU q, (SELECT SETUJU_TGL t FROM MOB_PINJAMAN_AJU WHERE ID=:id) m
                     WHERE q.STATUS='DISETUJUI' AND (q.SETUJU_TGL < m.t OR (q.SETUJU_TGL = m.t AND q.ID <= :id))",
                    &[("id", &id)],
                )?;
                kabari(c, id, 2, &nik, &format!("Pengajuan pinjaman {rp} disetujui. Nomor antrian pencairan Anda: {urut}."))?
            }
            Aksi::Tolak => kabari(c, id, 3, &nik, &format!("Pengajuan pinjaman {rp} ditolak: {}", catatan.as_deref().unwrap_or("-")))?,
            Aksi::Cairkan => {}
        }
        Ok(id.to_string())
    })
}

/// 12345678 -> "12.345.678".
fn ribuan(n: i64) -> String {
    let s = n.unsigned_abs().to_string();
    let mut out = String::new();
    for (i, ch) in s.chars().enumerate() {
        if i > 0 && (s.len() - i).is_multiple_of(3) {
            out.push('.');
        }
        out.push(ch);
    }
    if n < 0 { format!("-{out}") } else { out }
}

/// Antre notifikasi push ke anggota dalam transaksi yang sama. ID negatif deterministik per (pengajuan, kejadian)
/// agar tidak bentrok dengan SALE_ID penjualan; tag "PJ-<id>" membuat status terbaru mengganti notifikasi sebelumnya.
fn kabari(c: &Connection, id: i64, kejadian: i64, nik: &str, pesan: &str) -> AppResult<()> {
    let pesan: String = pesan.chars().take(250).collect();
    match c.execute_named(
        "INSERT INTO MOB_PUSH_QUEUE (SALE_ID,NO_TRANSAKSI,NIK,PESAN) VALUES (:id,:no,:nik,:p)",
        &[("id", &-(id * 10 + kejadian)), ("no", &format!("PJ-{id}")), ("nik", &nik), ("p", &pesan)],
    ) {
        Err(e) if e.oci_code() == Some(1) => Ok(()),
        r => r.map(|_| ()).map_err(Into::into),
    }
}

fn tahap_sama() -> AppError {
    AppError::rule("TAHAP_SAMA_USER", "Tahap ini harus dikerjakan oleh pengurus yang berbeda dari tahap sebelumnya")
}

fn cairkan(c: &Connection, u: &Operator, id: i64, nik: &str, jumlah: i64, tenor: i32) -> AppResult<String> {
    let hari = faktur::sekarang(c)?.0;
    faktur::cek_periode(c, hari)?;
    let p = periode_kini(c)?;
    // Kunci plafon periode: menyerialkan semua pencairan sehingga sisa dana tidak terpakai ganda.
    let pl = plafon(c, p.periode, true)?
        .ok_or_else(|| AppError::rule("PLAFON_BELUM_DIATUR", format!("Plafon dana periode {} belum diatur", p.periode)))?;
    let kepala: Option<i64> = opt(c.query_row_as::<i64>(
        "SELECT ID FROM MOB_PINJAMAN_AJU WHERE STATUS='DISETUJUI' ORDER BY SETUJU_TGL,ID FETCH FIRST 1 ROWS ONLY",
        &[],
    ))?;
    if kepala != Some(id) {
        return Err(AppError::rule("BUKAN_KEPALA_ANTRIAN", "Cairkan pengajuan sesuai urutan antrian"));
    }
    let sisa = pl - terpakai(c, &p)?;
    if jumlah > sisa {
        return Err(AppError::rule(
            "DANA_TIDAK_CUKUP",
            format!("Sisa plafon dana periode ini Rp {sisa}, pinjaman Rp {jumlah}"),
        ));
    }
    // Aturan dicek ulang saat cair: bunga terkini dipakai seperti BackOffice saat menyimpan.
    let (a, at) = kelayakan(c, nik, jumlah, tenor, Some(id))?;
    let no = nomor_baru(c, hari)?;
    let jadwal = hitung::jadwal(hari, jumlah, tenor, at.bunga).map_err(AppError::validation)?;
    let angsuran = jadwal[0].angsuran;
    let n = |x: Decimal| x.to_string();
    c.execute_named(
        &format!(
            "INSERT INTO FIN_PINJAMAN (NO_TRANSAKSI,TANGGAL,KASIR,ID_PELANGGAN,NIK,NAMA_PELANGGAN,STATUS,UNIT_KERJA,PINJAMAN,TENOR,BUNGA,ANGSURAN,KETERANGAN)
             VALUES (:no,:tgl,:kasir,:idp,:nik,:nama,:st,:uk,:j,:t,{},{},:ket)",
            num("b"), num("ang")
        ),
        &[("no", &no), ("tgl", &hari), ("kasir", &u.username), ("idp", &a.id), ("nik", &nik), ("nama", &a.nama),
          ("st", &a.status), ("uk", &a.unit_kerja), ("j", &jumlah), ("t", &tenor), ("b", &at.bunga.to_string()),
          ("ang", &n(angsuran)), ("ket", &format!("Pengajuan web #{id}"))],
    )?;
    for b in &jadwal {
        c.execute_named(
            &format!(
                "INSERT INTO FIN_PINJAMAN_DETAIL (PERIODE,NO_TRANSAKSI,TANGGALJATUHTEMPO,ANGSURANKE,SALDOAWAL,POKOK,BUNGA,ANGSURAN,SALDOAKHIR)
                 VALUES (:p,:no,:jt,:ke,{},{},{},{},{})",
                num("sa"), num("po"), num("bu"), num("an"), num("sk")
            ),
            &[("p", &b.periode), ("no", &no), ("jt", &b.jatuh_tempo), ("ke", &b.ke), ("sa", &n(b.saldo_awal)),
              ("po", &n(b.pokok)), ("bu", &n(b.bunga)), ("an", &n(b.angsuran)), ("sk", &n(b.saldo_akhir))],
        )?;
    }
    c.execute_named(
        &format!(
            "UPDATE MOB_PINJAMAN_AJU SET STATUS='DICAIRKAN',CAIR_USER=:u,CAIR_NAMA=:n,CAIR_TGL=SYSTIMESTAMP,NO_TRANSAKSI=:no,
             BUNGA={},ANGSURAN={} WHERE ID=:id",
            num("b"), num("ang")
        ),
        &[("u", &u.id), ("n", &u.nama), ("no", &no), ("b", &at.bunga.to_string()), ("ang", &n(angsuran)), ("id", &id)],
    )?;
    kabari(c, id, 4, nik, &format!(
        "Pinjaman Rp {} sudah dicairkan ({no}). Angsuran Rp {}/bulan selama {tenor} bulan.",
        ribuan(jumlah), ribuan(angsuran.round().to_i64().unwrap_or_default())
    ))?;
    Ok(no)
}

/// NOMOR_TRANSAKSI baris PINJAMAN dikunci; MAX FIN_PINJAMAN ikut dibaca karena BackOffice menulis nomornya di luar transaksi.
fn nomor_baru(c: &Connection, hari: NaiveDate) -> AppResult<String> {
    let lama: Option<String> = opt(c.query_row_as::<Option<String>>(
        "SELECT NOMOR FROM NOMOR_TRANSAKSI WHERE KODE='PINJAMAN' FOR UPDATE",
        &[],
    ))?
    .flatten();
    let prefix = format!("P-{:02}-%", hari.year() % 100);
    let maks: Option<String> = c.query_row_as_named(
        "SELECT MAX(NO_TRANSAKSI) FROM FIN_PINJAMAN WHERE NO_TRANSAKSI LIKE :p",
        &[("p", &prefix)],
    )?;
    let baru = nomor::pinjaman_berikutnya(&[lama.as_deref(), maks.as_deref()], hari.year());
    c.execute_named(
        "MERGE INTO NOMOR_TRANSAKSI t USING (SELECT 'PINJAMAN' KODE FROM DUAL) s ON (t.KODE=s.KODE)
         WHEN MATCHED THEN UPDATE SET NOMOR=:n WHEN NOT MATCHED THEN INSERT (KODE,NOMOR) VALUES ('PINJAMAN',:n)",
        &[("n", &baru)],
    )?;
    Ok(baru)
}

#[cfg(test)]
mod tests {
    #[test]
    fn ribuan_pakai_titik() {
        assert_eq!(super::ribuan(0), "0");
        assert_eq!(super::ribuan(999), "999");
        assert_eq!(super::ribuan(1000), "1.000");
        assert_eq!(super::ribuan(3_000_000), "3.000.000");
        assert_eq!(super::ribuan(-12_345), "-12.345");
    }
}
