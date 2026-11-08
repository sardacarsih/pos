use chrono::{Datelike, NaiveDate, NaiveDateTime};
use oracle::Connection;
use rust_decimal::Decimal;
use serde::{Deserialize, Serialize};

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

/// Periode harus terbuka: remise untuk tanggal itu belum 'Y' dan periode <= MAX(PERIODE). Fail closed.
pub fn cek_periode(conn: &Connection, tanggal: NaiveDate) -> AppResult<()> {
    let periode = tanggal.year() * 100 + tanggal.month() as i32;
    let kolom = if calc::remise_untuk_hari(tanggal.day()) == 1 {
        "REMISE1"
    } else {
        "REMISE2"
    };
    let row = opt(conn.query_row_named(
        &format!("SELECT {kolom} FROM POS_PERIODE WHERE PERIODE = :p"),
        &[("p", &(periode as i64))],
    ))?;
    let Some(row) = row else {
        return Err(AppError::rule(
            "PERIODE_TIDAK_ADA",
            format!("Periode {periode} belum dikonfigurasi"),
        ));
    };
    let tutup: Option<String> = row.get(0)?;
    if tutup.is_some_and(|t| t.trim().eq_ignore_ascii_case("Y")) {
        return Err(AppError::rule(
            "PERIODE_TUTUP",
            "Periode telah ditutup, transaksi tidak dapat dilakukan",
        ));
    }
    let max: Option<i64> = conn.query_row_as("SELECT MAX(PERIODE) FROM POS_PERIODE", &[])?;
    if max.is_some_and(|m| (periode as i64) > m) {
        return Err(AppError::rule(
            "PERIODE_TIDAK_VALID",
            "Input transaksi dibatasi pada periode yang valid",
        ));
    }
    Ok(())
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
    let periode = tanggal.year() * 100 + tanggal.month() as i32;
    let Some((dari, sampai)) = jendela_periode(conn, periode, &status)? else {
        return Err(AppError::rule(
            "PERIODE_TIDAK_ADA",
            format!(
                "Periode {periode} belum dikonfigurasi di POS_PERIODE; limit hutang tidak dapat diperiksa"
            ),
        ));
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

pub fn terbitkan_nomor(conn: &Connection, user_id: i64) -> AppResult<String> {
    let (hari, _) = sekarang(conn)?;
    let terakhir: Option<String> = opt(conn.query_row_as_named::<Option<String>>(
        "SELECT NOMOR FROM NOMOR_TRANSAKSI WHERE KODE = 'PENJUALAN' FOR UPDATE",
        &[],
    ))?
    .flatten();
    let baru = nomor::berikutnya(terakhir.as_deref(), hari.year());
    conn.execute_named(
        "MERGE INTO NOMOR_TRANSAKSI t USING (SELECT 'PENJUALAN' AS KODE FROM DUAL) s ON (t.KODE = s.KODE) \
         WHEN MATCHED THEN UPDATE SET NOMOR = :n WHEN NOT MATCHED THEN INSERT (KODE, NOMOR) VALUES ('PENJUALAN', :n)",
        &[("n", &baru)],
    )?;
    conn.execute_named(
        "INSERT INTO MOB_NOMOR (NO_TRANSAKSI, USER_ID) VALUES (:n, :u)",
        &[("n", &baru), ("u", &user_id)],
    )?;
    conn.commit()?;
    Ok(baru)
}

// ------------------------------------------------------- DTO faktur

#[derive(Debug, Deserialize)]
pub struct BarisReq {
    pub product_id: i64,
    pub qty: i64,
}

#[derive(Debug, Deserialize)]
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
    pub qty: i64,
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
) -> AppResult<FakturResp> {
    let r = simpan_inner(conn, kasir, req, opsi);
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
) -> AppResult<FakturResp> {
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
    if claim.get::<_, i64>(0)? == 1 {
        conn.rollback()?;
        let mut f = muat(conn, &req.no_transaksi)?.ok_or_else(|| {
            AppError::Internal("nomor terpakai tetapi faktur tidak ditemukan".into())
        })?;
        f.duplikat = true;
        return Ok(f);
    }

    if req.baris.is_empty() {
        return Err(AppError::validation("Keranjang kosong"));
    }
    let (hari, jam) = sekarang(conn)?;
    cek_periode(conn, hari)?;

    let pel = katalog::pelanggan_by_nik(conn, &req.nik)?.ok_or_else(|| {
        AppError::rule(
            "PELANGGAN_TIDAK_ADA",
            "Pelanggan tidak ditemukan atau tidak aktif",
        )
    })?;
    let tunai = pel.nik == opsi.default_nik;

    // Gabungkan baris dengan produk sama (seperti keranjang desktop).
    let mut gabung: Vec<(i64, i64)> = Vec::new();
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
        let qty = Decimal::from(q);
        if !calc::qty_valid(qty) {
            return Err(AppError::validation(
                "Qty harus lebih dari 0 dan maksimal 1000",
            ));
        }
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
            qty: m(6)?,
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
