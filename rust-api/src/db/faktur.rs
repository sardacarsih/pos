use chrono::{Datelike, NaiveDate, NaiveDateTime};
use oracle::Connection;
use rust_decimal::Decimal;
use serde::{Deserialize, Serialize};
use sha2::{Digest, Sha256};

use super::{Binds, dec, katalog, num, opt, rupiah, tampil, tm};
use crate::{
    domain::{angsuran, credit_limit, faktur as calc, nomor, potongan},
    error::{AppError, AppResult},
};

// ------------------------------------------------------------ periode

/// Tanggal/jam server (zona waktu database). Mobile hanya menjual untuk hari ini.
pub fn sekarang(conn: &Connection) -> AppResult<(NaiveDate, String)> {
    let (d, jam): (NaiveDateTime, String) = conn.query_row_as(
        "SELECT TRUNC(SYSDATE), TO_CHAR(SYSDATE, 'HH24:MI:SS') FROM DUAL",
        &[],
    )?;
    Ok((d.date(), jam))
}

const TGL: &str = "'DD-MON-YYYY', 'NLS_DATE_LANGUAGE = ENGLISH'";

/// Remise untuk tanggal: dalam R1DARI..R1SAMPAI -> 1, dalam R2DARI..R2SAMPAI -> 2,
/// selain itu aturan lama berbasis hari (<= 15 -> 1).
fn pilih_remise(
    tanggal: NaiveDate,
    r1: Option<(NaiveDate, NaiveDate)>,
    r2: Option<(NaiveDate, NaiveDate)>,
) -> u8 {
    let dalam =
        |r: Option<(NaiveDate, NaiveDate)>| r.is_some_and(|(a, b)| (a..=b).contains(&tanggal));
    if dalam(r1) {
        1
    } else if dalam(r2) {
        2
    } else {
        calc::remise_untuk_hari(tanggal.day())
    }
}

/// Aturan tutup/valid yang tidak butuh DB: flag 'Y' = tutup; periode > MAX(PERIODE) = belum valid.
fn validasi_periode(periode: i64, tutup: Option<&str>, max: Option<i64>) -> AppResult<()> {
    if tutup.is_some_and(|t| t.trim().eq_ignore_ascii_case("Y")) {
        return Err(AppError::rule(
            "PERIODE_TUTUP",
            format!("Periode {periode} telah ditutup, transaksi tidak dapat dilakukan"),
        ));
    }
    if max.is_some_and(|m| periode > m) {
        return Err(AppError::rule(
            "PERIODE_TIDAK_VALID",
            "Input transaksi dibatasi pada periode yang valid",
        ));
    }
    Ok(())
}

/// Periode harus terbuka: tanggal berada dalam rentang bulanan (BDARI..BSAMPAI) sebuah baris POS_PERIODE,
/// remise untuk tanggal itu belum 'Y', dan periode <= MAX(PERIODE). Fail closed.
pub fn cek_periode(conn: &Connection, tanggal: NaiveDate) -> AppResult<()> {
    let sql = format!(
        "SELECT PERIODE, REMISE1, REMISE2, \
                TO_DATE(R1DARI, {TGL}), TO_DATE(R1SAMPAI, {TGL}), \
                TO_DATE(R2DARI, {TGL}), TO_DATE(R2SAMPAI, {TGL}) \
         FROM POS_PERIODE \
         WHERE :tgl BETWEEN TO_DATE(BDARI, {TGL}) AND TO_DATE(BSAMPAI, {TGL}) \
         ORDER BY PERIODE DESC FETCH FIRST 1 ROWS ONLY"
    );
    let Some(r) = opt(conn.query_row_named(&sql, &[("tgl", &tanggal)]))? else {
        return Err(AppError::rule(
            "PERIODE_TIDAK_ADA",
            format!(
                "Tanggal {} di luar rentang periode yang dikonfigurasi",
                tanggal.format("%Y-%m-%d")
            ),
        ));
    };
    let periode: i64 = r.get(0)?;
    let tgl = |i: usize| -> AppResult<Option<NaiveDate>> {
        Ok(r.get::<_, Option<NaiveDateTime>>(i)?.map(|d| d.date()))
    };
    let rentang = |a: Option<NaiveDate>, b: Option<NaiveDate>| a.zip(b);
    let remise = pilih_remise(
        tanggal,
        rentang(tgl(3)?, tgl(4)?),
        rentang(tgl(5)?, tgl(6)?),
    );
    let tutup: Option<String> = r.get(if remise == 1 { 1 } else { 2 })?;
    let max: Option<i64> = conn.query_row_as("SELECT MAX(PERIODE) FROM POS_PERIODE", &[])?;
    validasi_periode(periode, tutup.as_deref(), max)
}

#[cfg(test)]
mod periode_tests {
    use super::*;

    fn d(y: i32, m: u32, dd: u32) -> NaiveDate {
        NaiveDate::from_ymd_opt(y, m, dd).unwrap()
    }

    #[test]
    fn remise_mengikuti_rentang_bukan_hari_kalender() {
        // R1 digeser: 20 Okt - 4 Nov; R2: 5 Nov - 19 Nov.
        let r1 = Some((d(2026, 10, 20), d(2026, 11, 4)));
        let r2 = Some((d(2026, 11, 5), d(2026, 11, 19)));
        assert_eq!(pilih_remise(d(2026, 11, 3), r1, r2), 1); // hari<=15 dan di R1
        assert_eq!(pilih_remise(d(2026, 11, 10), r1, r2), 2); // hari<=15 tapi di R2 (aturan lama salah)
        assert_eq!(pilih_remise(d(2026, 10, 25), r1, r2), 1);
    }

    #[test]
    fn remise_fallback_aturan_hari_bila_rentang_kosong() {
        assert_eq!(pilih_remise(d(2026, 11, 10), None, None), 1);
        assert_eq!(pilih_remise(d(2026, 11, 20), None, None), 2);
    }

    #[test]
    fn validasi_tutup_dan_max() {
        assert!(validasi_periode(202611, None, Some(202611)).is_ok());
        assert!(validasi_periode(202611, Some(" n "), Some(202611)).is_ok());
        assert!(matches!(
            validasi_periode(202611, Some("y"), Some(202611)),
            Err(AppError::Rule {
                kode: "PERIODE_TUTUP",
                ..
            })
        ));
        assert!(matches!(
            validasi_periode(202612, None, Some(202611)),
            Err(AppError::Rule {
                kode: "PERIODE_TIDAK_VALID",
                ..
            })
        ));
    }
}

/// Periode yang rentang bulanannya (BDARI..BSAMPAI) memuat tanggal; terbesar bila tumpang tindih. None bila tidak ada.
/// Dasar semua pencarian periode (status buka/tutup maupun window limit hutang) — bukan bulan kalender.
pub fn periode_untuk_tanggal(conn: &Connection, tanggal: NaiveDate) -> AppResult<Option<i32>> {
    let sql = "SELECT PERIODE FROM POS_PERIODE          WHERE :tgl BETWEEN TO_DATE(BDARI, 'DD-MON-YYYY', 'NLS_DATE_LANGUAGE = ENGLISH')                         AND TO_DATE(BSAMPAI, 'DD-MON-YYYY', 'NLS_DATE_LANGUAGE = ENGLISH')          ORDER BY PERIODE DESC FETCH FIRST 1 ROWS ONLY";
    match opt(conn.query_row_named(sql, &[("tgl", &tanggal)]))? {
        Some(r) => Ok(Some(r.get::<_, i64>(0)? as i32)),
        None => Ok(None),
    }
}

/// Jendela periode anggota: BULANAN -> BDARI..BSAMPAI, selain itu R1DARI..R2SAMPAI.
pub fn jendela_periode(
    conn: &Connection,
    periode: i32,
    status: &str,
) -> AppResult<Option<(NaiveDateTime, NaiveDateTime)>> {
    let sql = if status.eq_ignore_ascii_case("BULANAN") {
        "SELECT TO_DATE(BDARI, 'DD-MON-YYYY', 'NLS_DATE_LANGUAGE = ENGLISH'), TO_DATE(BSAMPAI, 'DD-MON-YYYY', 'NLS_DATE_LANGUAGE = ENGLISH') FROM POS_PERIODE WHERE PERIODE = :p"
    } else {
        "SELECT TO_DATE(R1DARI, 'DD-MON-YYYY', 'NLS_DATE_LANGUAGE = ENGLISH'), TO_DATE(R2SAMPAI, 'DD-MON-YYYY', 'NLS_DATE_LANGUAGE = ENGLISH') FROM POS_PERIODE WHERE PERIODE = :p"
    };
    match opt(conn.query_row_named(sql, &[("p", &(periode as i64))]))? {
        Some(r) => {
            let a: Option<NaiveDateTime> = r.get(0)?;
            let b: Option<NaiveDateTime> = r.get(1)?;
            Ok(a.zip(b))
        }
        None => Ok(None),
    }
}

/// Total belanja (POS_PENJUALAN.TOTAL) anggota dalam jendela periode.
pub fn belanja_periode(
    conn: &Connection,
    nik: &str,
    dari: NaiveDateTime,
    sampai: NaiveDateTime,
) -> AppResult<Decimal> {
    let sql = format!(
        "SELECT {} FROM POS_PENJUALAN WHERE NIK = :nik AND TANGGAL BETWEEN :dari AND :sampai",
        tm("NVL(SUM(TOTAL), 0)")
    );
    dec(&conn.query_row_as_named::<String>(
        &sql,
        &[("nik", &nik), ("dari", &dari), ("sampai", &sampai)],
    )?)
}

/// Port ValidateCreditLimit: kunci baris anggota (FOR UPDATE), baca limit dari DB, hitung ulang
/// belanja periode. Anggota/periode tidak ada => DITOLAK (fail closed).
fn validasi_limit(
    conn: &Connection,
    nik: &str,
    tanggal: NaiveDate,
    invoice: Decimal,
) -> AppResult<String> {
    let sql = format!(
        "SELECT {}, STATUS FROM FIN_ANGGOTA WHERE NIK = :nik AND AKTIF = 'Y' FOR UPDATE",
        tm("NVL(LIMIT_HUTANG, 0)")
    );
    let Some(r) = opt(conn.query_row_named(&sql, &[("nik", &nik)]))? else {
        return Err(AppError::rule(
            "PELANGGAN_TIDAK_ADA",
            format!(
                "Anggota dengan NIK '{nik}' tidak ditemukan; limit hutang tidak dapat diperiksa"
            ),
        ));
    };
    let limit = dec(&r.get::<_, String>(0)?)?;
    let status: String = r.get::<_, Option<String>>(1)?.unwrap_or_default();
    if limit.is_zero() {
        return Ok(status);
    }
    let tidak_ada = || {
        AppError::rule(
            "PERIODE_TIDAK_ADA",
            format!(
                "Tanggal {} di luar rentang periode di POS_PERIODE; limit hutang tidak dapat diperiksa",
                tanggal.format("%Y-%m-%d")
            ),
        )
    };
    let periode = periode_untuk_tanggal(conn, tanggal)?.ok_or_else(tidak_ada)?;
    let Some((dari, sampai)) = jendela_periode(conn, periode, &status)? else {
        return Err(tidak_ada());
    };
    let spend = belanja_periode(conn, nik, dari, sampai)?;
    if credit_limit::is_exceeded(spend, invoice, limit) {
        return Err(AppError::rule(
            "LIMIT_TERLAMPAUI",
            format!(
                "Limit hutang terlampaui. Belanja periode ini Rp {spend:.0}, faktur baru Rp {invoice:.0}, batas Rp {limit:.0}"
            ),
        ));
    }
    Ok(status)
}

// ------------------------------------------------------------- nomor

pub fn terbitkan_nomor(
    conn: &Connection,
    user_id: i64,
    idempotency_key: &str,
) -> AppResult<(String, bool)> {
    let result = terbitkan_nomor_inner(conn, user_id, idempotency_key);
    if result.is_err() {
        let _ = conn.rollback();
    }
    result
}

fn terbitkan_nomor_inner(
    conn: &Connection,
    user_id: i64,
    idempotency_key: &str,
) -> AppResult<(String, bool)> {
    match conn.execute_named(
        "INSERT INTO MOB_IDEMPOTENCY (USER_ID, OPERASI, IDEMPOTENCY_KEY) VALUES (:u, 'NOMOR', :k)",
        &[("u", &user_id), ("k", &idempotency_key)],
    ) {
        Ok(_) => {}
        Err(e) if e.oci_code() == Some(1) => {
            // INSERT menunggu transaksi pertama jika kuncinya sedang diproses.
            // Setelah unique violation, rollback lalu baca hasil yang sudah commit.
            conn.rollback()?;
            let row = conn.query_row_named(
                "SELECT RESOURCE_ID FROM MOB_IDEMPOTENCY WHERE USER_ID = :u AND OPERASI = 'NOMOR' AND IDEMPOTENCY_KEY = :k",
                &[("u", &user_id), ("k", &idempotency_key)],
            )?;
            let no: Option<String> = row.get(0)?;
            return no
                .map(|no| (no, true))
                .ok_or_else(|| AppError::Internal("kunci nomor belum memiliki hasil".into()));
        }
        Err(e) => return Err(e.into()),
    }

    let (hari, _) = sekarang(conn)?;
    cek_periode(conn, hari)?;
    let no = terbitkan_nomor_baru(conn, user_id)?;
    conn.execute_named(
        "UPDATE MOB_IDEMPOTENCY SET RESOURCE_ID = :n WHERE USER_ID = :u AND OPERASI = 'NOMOR' AND IDEMPOTENCY_KEY = :k",
        &[("n", &no), ("u", &user_id), ("k", &idempotency_key)],
    )?;
    conn.commit()?;
    Ok((no, false))
}

fn terbitkan_nomor_baru(conn: &Connection, user_id: i64) -> AppResult<String> {
    let (hari, _) = sekarang(conn)?;
    let terakhir: Option<String> = opt(conn.query_row_as_named::<Option<String>>(
        "SELECT NOMOR FROM NOMOR_TRANSAKSI WHERE KODE = 'BBM' FOR UPDATE",
        &[],
    ))?
    .flatten();
    let baru = nomor::berikutnya(terakhir.as_deref(), hari.year());
    conn.execute_named(
        "MERGE INTO NOMOR_TRANSAKSI t USING (SELECT 'BBM' AS KODE FROM DUAL) s ON (t.KODE = s.KODE) \
         WHEN MATCHED THEN UPDATE SET NOMOR = :n WHEN NOT MATCHED THEN INSERT (KODE, NOMOR) VALUES ('BBM', :n)",
        &[("n", &baru)],
    )?;
    conn.execute_named(
        "INSERT INTO MOB_NOMOR (NO_TRANSAKSI, USER_ID) VALUES (:n, :u)",
        &[("n", &baru), ("u", &user_id)],
    )?;
    Ok(baru)
}

// ------------------------------------------------------- DTO faktur

#[derive(Debug, Deserialize, Serialize)]
pub struct BarisReq {
    pub product_id: i64,
    #[serde(with = "rust_decimal::serde::arbitrary_precision")]
    pub qty: Decimal,
}

#[derive(Debug, Deserialize, Serialize)]
pub struct SimpanReq {
    pub no_transaksi: String,
    pub nik: String,
    pub tenor: Option<i32>,
    pub uang_bayar: Option<i64>,
    pub baris: Vec<BarisReq>,
}

#[derive(Debug, Serialize)]
pub struct BarisResp {
    pub baris: i32,
    pub product_id: i64,
    pub kode: String,
    pub barcode: String,
    pub nama: String,
    pub satuan: String,
    #[serde(with = "rust_decimal::serde::arbitrary_precision")]
    pub qty: Decimal,
    pub harga: i64,
    pub potongan: i64,
    pub total: i64,
}

#[derive(Debug, Serialize)]
pub struct JadwalResp {
    pub ke: i32,
    pub jatuh_tempo: String,
    pub periode: i32,
    pub saldo_awal: i64,
    pub angsuran: i64,
    pub saldo_akhir: i64,
}

#[derive(Debug, Serialize)]
pub struct FakturResp {
    pub no_transaksi: String,
    pub tanggal: String,
    pub jam: String,
    pub kasir: String,
    pub nama_kasir: String,
    pub nik: String,
    pub nama_pelanggan: String,
    pub unit_kerja: String,
    pub status: String,
    pub jenis_bayar: String,
    pub ket_bayar: String,
    pub tenor: i32,
    pub angsuran: i64,
    pub bruto: i64,
    pub potongan: i64,
    pub total: i64,
    pub uang_bayar: Option<i64>,
    pub kembalian: Option<i64>,
    pub baris: Vec<BarisResp>,
    pub jadwal: Vec<JadwalResp>,
    /// True bila permintaan ini pengulangan (idempotensi): faktur sudah tersimpan sebelumnya.
    pub duplikat: bool,
}

pub struct Kasir {
    pub id: i64,
    pub username: String,
    pub nama: String,
    pub kategori: Vec<String>,
}

pub struct Opsi<'a> {
    pub default_nik: &'a str,
    pub default_max_tenor: i32,
}

/// Simpan faktur dalam SATU transaksi. Harga, potongan, total, limit, stok, kategori dihitung ulang server.
pub fn simpan(
    conn: &Connection,
    kasir: &Kasir,
    req: &SimpanReq,
    opsi: &Opsi,
    idempotency_key: &str,
) -> AppResult<FakturResp> {
    let r = simpan_inner(conn, kasir, req, opsi, idempotency_key);
    if r.is_err() {
        let _ = conn.rollback();
    }
    r
}

fn simpan_inner(
    conn: &Connection,
    kasir: &Kasir,
    req: &SimpanReq,
    opsi: &Opsi,
    idempotency_key: &str,
) -> AppResult<FakturResp> {
    let nomor_key = opt(conn.query_row_named(
        "SELECT RESOURCE_ID FROM MOB_IDEMPOTENCY WHERE USER_ID = :u AND OPERASI = 'NOMOR' AND IDEMPOTENCY_KEY = :k",
        &[("u", &kasir.id), ("k", &idempotency_key)],
    ))?;
    let Some(nomor_key) = nomor_key else {
        return Err(AppError::validation(
            "Kunci idempotensi belum menerbitkan nomor transaksi",
        ));
    };
    let nomor_terkait: Option<String> = nomor_key.get(0)?;
    if nomor_terkait.as_deref() != Some(req.no_transaksi.as_str()) {
        return Err(AppError::Conflict(
            "Kunci idempotensi terikat ke nomor transaksi lain".into(),
        ));
    }

    // Idempotensi: nomor harus milik kasir ini; baris dikunci sehingga retry paralel antre.
    let claim = opt(conn.query_row_named(
        "SELECT CASE WHEN USED_AT IS NULL THEN 0 ELSE 1 END FROM MOB_NOMOR WHERE NO_TRANSAKSI = :n AND USER_ID = :u FOR UPDATE",
        &[("n", &req.no_transaksi), ("u", &kasir.id)],
    ))?;
    let Some(claim) = claim else {
        return Err(AppError::validation(
            "Nomor transaksi tidak dikenal, ambil nomor baru",
        ));
    };
    let sudah_dipakai = claim.get::<_, i64>(0)? == 1;
    let payload_hash = hash_payload(req)?;
    if let Some(row) = opt(conn.query_row_named(
        "SELECT RESOURCE_ID, PAYLOAD_HASH FROM MOB_IDEMPOTENCY WHERE USER_ID = :u AND OPERASI = 'FAKTUR' AND IDEMPOTENCY_KEY = :k FOR UPDATE",
        &[("u", &kasir.id), ("k", &idempotency_key)],
    ))? {
        let resource_id: Option<String> = row.get(0)?;
        let stored_hash: Option<String> = row.get(1)?;
        cek_payload_idempoten(
            resource_id.as_deref(),
            stored_hash.as_deref(),
            &req.no_transaksi,
            &payload_hash,
        )?;
        if !sudah_dipakai {
            return Err(AppError::Internal(
                "kunci faktur tersimpan tetapi nomor belum digunakan".into(),
            ));
        }
        conn.rollback()?;
        let mut f = muat(conn, &req.no_transaksi)?.ok_or_else(|| {
            AppError::Internal("nomor terpakai tetapi faktur tidak ditemukan".into())
        })?;
        f.duplikat = true;
        return Ok(f);
    }
    if sudah_dipakai {
        return Err(AppError::Conflict(
            "Nomor transaksi sudah digunakan oleh request lain".into(),
        ));
    }

    conn.execute_named(
        "INSERT INTO MOB_IDEMPOTENCY (USER_ID, OPERASI, IDEMPOTENCY_KEY, RESOURCE_ID, PAYLOAD_HASH) VALUES (:u, 'FAKTUR', :k, :n, :h)",
        &[("u", &kasir.id), ("k", &idempotency_key), ("n", &req.no_transaksi), ("h", &payload_hash)],
    )?;

    let (hari, jam) = sekarang(conn)?;
    cek_periode(conn, hari)?;

    if req.baris.is_empty() {
        return Err(AppError::validation("Keranjang kosong"));
    }
    if req.baris.len() > 200 {
        return Err(AppError::validation("Terlalu banyak baris"));
    }
    if req
        .baris
        .iter()
        .any(|b| b.qty <= Decimal::ZERO || b.qty.normalize().scale() > 2)
    {
        return Err(AppError::validation(
            "Qty harus lebih dari 0 dan maksimal 2 angka desimal",
        ));
    }

    let pel = katalog::pelanggan_by_nik(conn, &req.nik)?.ok_or_else(|| {
        AppError::rule(
            "PELANGGAN_TIDAK_ADA",
            "Pelanggan tidak ditemukan atau tidak aktif",
        )
    })?;
    let tunai = pel.nik == opsi.default_nik;

    // Gabungkan baris dengan produk sama (seperti keranjang desktop).
    let mut gabung: Vec<(i64, Decimal)> = Vec::new();
    for b in &req.baris {
        match gabung.iter_mut().find(|(id, _)| *id == b.product_id) {
            Some((_, q)) => *q += b.qty,
            None => gabung.push((b.product_id, b.qty)),
        }
    }

    struct Item {
        p: katalog::Produk,
        qty: Decimal,
        pot: Decimal,
    }
    let mut items: Vec<Item> = Vec::new();
    for (pid, q) in gabung {
        if !calc::qty_valid(q.normalize()) || q.normalize().scale() > 2 {
            return Err(AppError::validation(
                "Qty harus lebih dari 0, maksimal 1000, dan paling banyak 2 angka desimal",
            ));
        }
        let qty = q;
        let p = katalog::barang_by_id(conn, pid, &kasir.kategori)?.ok_or_else(|| {
            AppError::rule(
                "BARANG_TIDAK_ADA",
                format!("Barang {pid} tidak ditemukan atau tidak aktif"),
            )
        })?;
        calc::harga_boleh_dijual(p.harga, p.hpp).map_err(|m| {
            AppError::rule("HARGA_TIDAK_VALID", format!("{m}: {} {}", p.kode, p.nama))
        })?;
        let pot = match katalog::potongan_qty(conn, &p.kode)? {
            Some((min, per_unit)) => potongan::diskon_qty(qty, min, per_unit),
            None => Decimal::ZERO,
        };
        items.push(Item { p, qty, pot });
    }

    // Kontrol stok bila FIN_SETTINGS KONTROL_SALDO_QTY > 0.
    if katalog::setting(conn, "KONTROL_SALDO_QTY")?.is_some_and(|v| v.trunc() > Decimal::ZERO) {
        let kodes: Vec<String> = items.iter().map(|i| i.p.kode.clone()).collect();
        let awal = NaiveDate::from_ymd_opt(hari.year(), 1, 1).expect("1 Jan valid");
        let stok = katalog::stok_map(conn, &kodes, awal, hari)?;
        for i in &items {
            let ada = stok.get(&i.p.kode).copied().unwrap_or_default();
            if i.qty > ada {
                return Err(AppError::rule(
                    "STOK_KURANG",
                    format!(
                        "Qty melebihi stok sistem: {} {} (stok {ada:.0})",
                        i.p.kode, i.p.nama
                    ),
                ));
            }
        }
    }

    let baris: Vec<calc::Baris> = items
        .iter()
        .map(|i| calc::Baris {
            qty: i.qty,
            harga: i.p.harga,
            potongan: i.pot,
        })
        .collect();
    let tot = calc::total_faktur(&baris);
    if tot.total < Decimal::ZERO {
        return Err(AppError::validation("Total transaksi minus"));
    }

    // Tunai: uang bayar >= total. Kredit: tenor 1..max, cek limit di transaksi yang sama.
    let (jenis, ket, tenor, bayar, kembali) = if tunai {
        let bayar = Decimal::from(req.uang_bayar.unwrap_or(0));
        if bayar.is_zero() || bayar < tot.total {
            return Err(AppError::rule(
                "BAYAR_KURANG",
                "Pembayaran harus lebih besar atau sama dengan total",
            ));
        }
        ("TUNAI", "KAS", 1, Some(bayar), Some(bayar - tot.total))
    } else {
        let max = katalog::setting(conn, "MAXANGSURAN")?
            .map(|d| {
                d.trunc()
                    .to_string()
                    .parse::<i32>()
                    .unwrap_or(opsi.default_max_tenor)
            })
            .filter(|m| *m >= 1)
            .unwrap_or(opsi.default_max_tenor);
        let tenor = req.tenor.unwrap_or(1);
        if !(1..=max).contains(&tenor) {
            return Err(AppError::validation(format!("Tenor harus 1 sampai {max}")));
        }
        ("KREDIT", "TAGIHAN", tenor, None, None)
    };
    let status = if tunai {
        pel.status.clone()
    } else {
        validasi_limit(conn, &pel.nik, hari, tot.total)?
    };

    // --- tulis ---
    let ang_header = calc::angsuran_header(tot.total, tenor);
    let mut b = Binds::default();
    b.add("no", &req.no_transaksi);
    b.add("tgl", hari.format("%Y-%m-%d"));
    b.add("jam", &jam);
    b.add("kasir", &kasir.username);
    b.add("nkasir", &kasir.nama);
    b.add("idp", pel.id);
    b.add("nik", &pel.nik);
    b.add("npel", &pel.nama);
    b.add("unit", &pel.unit_kerja);
    b.add("status", &status);
    b.add("bruto", tot.bruto);
    b.add("pot", tot.potongan);
    b.add("total", tot.total);
    b.add("jb", jenis);
    b.add("kb", ket);
    b.add("tenor", tenor);
    b.add("ang", ang_header);
    // Desktop mengisi PENDING='T' hanya pada jalur tenor 1.
    let (pcol, pval) = if tenor == 1 {
        (", PENDING", ", 'T'")
    } else {
        ("", "")
    };
    let sql = format!(
        "INSERT INTO POS_PENJUALAN (NO_TRANSAKSI, TANGGAL, JAM, KASIR, NAMA_KASIR, ID_PELANGGAN, NIK, NAMA_PELANGGAN, UNIT_KERJA, STATUS, BRUTO, POTONGAN, TOTAL, JENIS_BAYAR, KET_BAYAR, TENOR, ANGSURAN{pcol}) \
         VALUES (:no, TO_DATE(:tgl, 'YYYY-MM-DD'), :jam, :kasir, :nkasir, TO_NUMBER(:idp), :nik, :npel, :unit, :status, {}, {}, {}, :jb, :kb, TO_NUMBER(:tenor), {}{pval})",
        num("bruto"),
        num("pot"),
        num("total"),
        num("ang")
    );
    b.with(|p| conn.execute_named(&sql, p))?;
    let id_penjualan: i64 = conn.query_row_as_named(
        "SELECT MAX(ID_PENJUALAN) FROM POS_PENJUALAN WHERE NO_TRANSAKSI = :n",
        &[("n", &req.no_transaksi)],
    )?;

    for (i, it) in items.iter().enumerate() {
        let br = &baris[i];
        let mut d = Binds::default();
        d.add("id", id_penjualan);
        d.add("no", &req.no_transaksi);
        d.add("baris", i + 1);
        d.add("pid", it.p.id);
        d.add("kode", &it.p.kode);
        d.add("barcode", &it.p.barcode);
        d.add("nama", &it.p.nama);
        d.add("satuan", &it.p.satuan);
        d.add("hpp", it.p.hpp);
        d.add("harga", it.p.harga);
        d.add("qty", it.qty);
        d.add("bruto", br.bruto());
        d.add("pot", it.pot);
        d.add("total", br.total());
        let sql = format!(
            "INSERT INTO POS_PENJUALAN_DETAIL (ID_PENJUALAN, NO_TRANSAKSI, BARIS, PRODUCT_ID, KODE_BARANG, BARCODE, NAMA_BARANG, SATUAN, HPP, HARGA_BARANG, JUMLAH_BARANG, BRUTO, POTONGAN, TOTAL_HARGA) \
             VALUES (TO_NUMBER(:id), :no, TO_NUMBER(:baris), TO_NUMBER(:pid), :kode, :barcode, :nama, :satuan, {}, {}, {}, {}, {}, {})",
            num("hpp"),
            num("harga"),
            num("qty"),
            num("bruto"),
            num("pot"),
            num("total")
        );
        d.with(|p| conn.execute_named(&sql, p))?;
    }

    if tenor > 1 {
        let jadwal = angsuran::hitung(&req.no_transaksi, hari, tot.total, tenor)
            .map_err(AppError::validation)?;
        for a in &jadwal {
            let mut j = Binds::default();
            j.add("per", a.periode);
            j.add("no", &a.no_transaksi);
            j.add("jt", a.jatuh_tempo.format("%Y-%m-%d"));
            j.add("ke", a.ke);
            j.add("sa", a.saldo_awal);
            j.add("an", a.angsuran);
            j.add("sk", a.saldo_akhir);
            let sql = format!(
                "INSERT INTO POS_KREDIT_ANGSURAN (PERIODE, NO_TRANSAKSI, TANGGALJATUHTEMPO, ANGSURANKE, SALDOAWAL, ANGSURAN, SALDOAKHIR) \
                 VALUES (TO_NUMBER(:per), :no, TO_DATE(:jt, 'YYYY-MM-DD'), TO_NUMBER(:ke), {}, {}, {})",
                num("sa"),
                num("an"),
                num("sk")
            );
            j.with(|p| conn.execute_named(&sql, p))?;
        }
    }

    conn.execute_named(
        "UPDATE MOB_NOMOR SET USED_AT = SYSTIMESTAMP WHERE NO_TRANSAKSI = :n",
        &[("n", &req.no_transaksi)],
    )?;
    conn.commit()?;

    let mut f = muat(conn, &req.no_transaksi)?
        .ok_or_else(|| AppError::Internal("faktur tidak terbaca setelah simpan".into()))?;
    f.uang_bayar = bayar.map(rupiah).transpose()?;
    f.kembalian = kembali.map(rupiah).transpose()?;
    Ok(f)
}

fn hex_digest(bytes: &[u8]) -> String {
    const HEX: &[u8; 16] = b"0123456789abcdef";
    let mut out = String::with_capacity(bytes.len() * 2);
    for &byte in bytes {
        out.push(HEX[(byte >> 4) as usize] as char);
        out.push(HEX[(byte & 0x0f) as usize] as char);
    }
    out
}

fn hash_payload(req: &SimpanReq) -> AppResult<String> {
    let payload = serde_json::to_vec(req)
        .map_err(|e| AppError::Internal(format!("gagal menghitung hash faktur: {e}")))?;
    Ok(hex_digest(&Sha256::digest(payload)))
}

fn cek_payload_idempoten(
    stored_resource: Option<&str>,
    stored_hash: Option<&str>,
    resource: &str,
    payload_hash: &str,
) -> AppResult<()> {
    if stored_resource != Some(resource) || stored_hash != Some(payload_hash) {
        return Err(AppError::Conflict(
            "Kunci idempotensi sudah dipakai dengan payload berbeda".into(),
        ));
    }
    Ok(())
}

/// Baca faktur lengkap dari DB (untuk idempotensi, cetak ulang, dan riwayat anggota).
pub fn muat(conn: &Connection, no: &str) -> AppResult<Option<FakturResp>> {
    let sql = format!(
        "SELECT NO_TRANSAKSI, TANGGAL, JAM, KASIR, NAMA_KASIR, NIK, NAMA_PELANGGAN, UNIT_KERJA, STATUS, JENIS_BAYAR, KET_BAYAR, {}, {}, {}, {}, {} \
         FROM POS_PENJUALAN WHERE NO_TRANSAKSI = :n ORDER BY ID_PENJUALAN DESC FETCH FIRST 1 ROW ONLY",
        tm("TENOR"),
        tm("ANGSURAN"),
        tm("BRUTO"),
        tm("POTONGAN"),
        tm("TOTAL")
    );
    let Some(h) = opt(conn.query_row_named(&sql, &[("n", &no)]))? else {
        return Ok(None);
    };
    let s =
        |i: usize| -> AppResult<String> { Ok(h.get::<_, Option<String>>(i)?.unwrap_or_default()) };
    let tgl: NaiveDateTime = h.get(1)?;
    let n = |i: usize| -> AppResult<i64> { Ok(tampil(dec(&h.get::<_, String>(i)?)?)) };
    let mut f = FakturResp {
        no_transaksi: s(0)?,
        tanggal: tgl.format("%Y-%m-%d").to_string(),
        jam: s(2)?,
        kasir: s(3)?,
        nama_kasir: s(4)?,
        nik: s(5)?,
        nama_pelanggan: s(6)?,
        unit_kerja: s(7)?,
        status: s(8)?,
        jenis_bayar: s(9)?,
        ket_bayar: s(10)?,
        tenor: n(11)? as i32,
        angsuran: n(12)?,
        bruto: n(13)?,
        potongan: n(14)?,
        total: n(15)?,
        uang_bayar: None,
        kembalian: None,
        baris: Vec::new(),
        jadwal: Vec::new(),
        duplikat: false,
    };

    let sql = format!(
        "SELECT BARIS, PRODUCT_ID, KODE_BARANG, BARCODE, NAMA_BARANG, SATUAN, {}, {}, {}, {} FROM POS_PENJUALAN_DETAIL WHERE NO_TRANSAKSI = :n ORDER BY BARIS",
        tm("JUMLAH_BARANG"),
        tm("HARGA_BARANG"),
        tm("POTONGAN"),
        tm("TOTAL_HARGA")
    );
    for r in conn.query_named(&sql, &[("n", &no)])? {
        let r = r?;
        let t = |i: usize| -> AppResult<String> {
            Ok(r.get::<_, Option<String>>(i)?.unwrap_or_default())
        };
        let m = |i: usize| -> AppResult<i64> { Ok(tampil(dec(&r.get::<_, String>(i)?)?)) };
        f.baris.push(BarisResp {
            baris: r.get::<_, i64>(0)? as i32,
            product_id: r.get(1)?,
            kode: t(2)?,
            barcode: t(3)?,
            nama: t(4)?,
            satuan: t(5)?,
            qty: dec(&r.get::<_, String>(6)?)?,
            harga: m(7)?,
            potongan: m(8)?,
            total: m(9)?,
        });
    }

    if f.tenor > 1 {
        let sql = format!(
            "SELECT ANGSURANKE, TANGGALJATUHTEMPO, PERIODE, {}, {}, {} FROM POS_KREDIT_ANGSURAN WHERE NO_TRANSAKSI = :n ORDER BY ANGSURANKE",
            tm("SALDOAWAL"),
            tm("ANGSURAN"),
            tm("SALDOAKHIR")
        );
        for r in conn.query_named(&sql, &[("n", &no)])? {
            let r = r?;
            let jt: NaiveDateTime = r.get(1)?;
            let m = |i: usize| -> AppResult<i64> { Ok(tampil(dec(&r.get::<_, String>(i)?)?)) };
            f.jadwal.push(JadwalResp {
                ke: r.get::<_, i64>(0)? as i32,
                jatuh_tempo: jt.format("%Y-%m-%d").to_string(),
                periode: r.get::<_, i64>(2)? as i32,
                saldo_awal: m(3)?,
                angsuran: m(4)?,
                saldo_akhir: m(5)?,
            });
        }
    }
    Ok(Some(f))
}

#[cfg(test)]
mod quantity_api_tests {
    use super::{AppError, BarisReq, SimpanReq, cek_payload_idempoten, hash_payload};
    use rust_decimal::Decimal;

    #[test]
    fn accepts_legacy_integer_and_fractional_json_quantities() {
        let integer: BarisReq = serde_json::from_str(r#"{"product_id":7,"qty":3}"#).unwrap();
        assert_eq!(integer.qty, Decimal::from(3));

        let fractional: BarisReq = serde_json::from_str(r#"{"product_id":9,"qty":4.10}"#).unwrap();
        assert_eq!(fractional.qty, Decimal::new(410, 2));
        assert!(fractional.qty.normalize().scale() <= 2);
        #[derive(serde::Serialize)]
        struct Echo {
            #[serde(with = "rust_decimal::serde::arbitrary_precision")]
            qty: Decimal,
        }
        let echoed = serde_json::to_string(&Echo {
            qty: fractional.qty,
        })
        .unwrap();
        assert_eq!(echoed, r#"{"qty":4.10}"#);
    }

    #[test]
    fn hashes_the_complete_invoice_request() {
        let a: SimpanReq = serde_json::from_str(
            r#"{"no_transaksi":"BBM-26-1","nik":"123","tenor":1,"uang_bayar":50000,"baris":[{"product_id":7,"qty":4.10}]}"#,
        )
        .unwrap();
        let same: SimpanReq = serde_json::from_str(
            r#"{"no_transaksi":"BBM-26-1","nik":"123","tenor":1,"uang_bayar":50000,"baris":[{"product_id":7,"qty":4.10}]}"#,
        )
        .unwrap();
        let changed: SimpanReq = serde_json::from_str(
            r#"{"no_transaksi":"BBM-26-1","nik":"123","tenor":1,"uang_bayar":50001,"baris":[{"product_id":7,"qty":4.10}]}"#,
        )
        .unwrap();
        assert_eq!(hash_payload(&a).unwrap(), hash_payload(&same).unwrap());
        assert_ne!(hash_payload(&a).unwrap(), hash_payload(&changed).unwrap());
        assert!(
            cek_payload_idempoten(
                Some("BBM-26-1"),
                Some(&hash_payload(&a).unwrap()),
                "BBM-26-1",
                &hash_payload(&a).unwrap(),
            )
            .is_ok()
        );
        assert!(matches!(
            cek_payload_idempoten(
                Some("BBM-26-1"),
                Some(&hash_payload(&a).unwrap()),
                "BBM-26-1",
                &hash_payload(&changed).unwrap(),
            ),
            Err(AppError::Conflict(_))
        ));
    }
}
