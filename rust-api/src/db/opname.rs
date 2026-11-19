//! Stock opname per barang dari mobile/web. Tabel dan nomor SO dibagi dengan desktop
//! (`BackOffice/DataLayer/StockOpnameRepository.cs`): hasilnya langsung terbaca rumus stok dan laporan desktop.
use chrono::{Datelike, NaiveDate};
use oracle::Connection;
use rust_decimal::Decimal;
use serde::Serialize;
use sha2::{Digest, Sha256};

use super::{dec, katalog, num, opt, tm};
use crate::error::{AppError, AppResult};

const KODE: &str = "STOCK_OPNAME";
const OPERASI: &str = "OPNAME";
const MAKS_QTY: i64 = 1_000_000_000;

#[derive(Debug, Serialize)]
pub struct HasilOpname {
    pub nomor_so: String,
    pub jumlah_sistem: Decimal,
    pub qty_fisik: Decimal,
    pub selisih: Decimal,
    /// True bila permintaan ini pengulangan (Idempotency-Key sama): tidak ada dokumen SO baru.
    pub ulang: bool,
}

/// Nomor SO seperti desktop: `SO-YY-NNNNNN`.
pub fn format_nomor(tahun: i32, urut: i64) -> String {
    format!("SO-{:02}-{urut:06}", tahun % 100)
}

pub fn validasi_qty(qty: Decimal) -> AppResult<()> {
    if qty.is_sign_negative() {
        return Err(AppError::validation("Qty fisik tidak boleh negatif"));
    }
    if qty > Decimal::from(MAKS_QTY) || qty.scale() > 4 {
        return Err(AppError::validation("Qty fisik tidak valid"));
    }
    Ok(())
}

/// Sidik payload: kunci idempotensi yang dipakai ulang dengan produk/qty lain ditolak.
fn hash_payload(produk_id: i64, qty: Decimal) -> String {
    let d = Sha256::digest(format!("{produk_id}|{}", qty.normalize()).as_bytes());
    d.iter().map(|b| format!("{b:02x}")).collect()
}

/// Alokasi nomor berikutnya (UPDATE mengunci baris counter sampai commit, jadi paralel aman).
/// Fallback INSERT + ORA-00001 meniru `AllocateTransactionNumber` desktop.
fn alokasi_nomor(conn: &Connection, tahun: i32) -> AppResult<String> {
    let upd = "UPDATE POS_TRANSACTION_COUNTER SET LAST_NUMBER = LAST_NUMBER + 1 WHERE TRANSACTION_CODE = :c AND TRANSACTION_YEAR = :y";
    let mut n = conn.execute_named(upd, &[("c", &KODE), ("y", &tahun)])?.row_count()?;
    if n == 0 {
        match conn.execute_named(
            "INSERT INTO POS_TRANSACTION_COUNTER (TRANSACTION_CODE, TRANSACTION_YEAR, LAST_NUMBER) VALUES (:c, :y, 1)",
            &[("c", &KODE), ("y", &tahun)],
        ) {
            Ok(_) => n = 1,
            Err(e) if e.oci_code() == Some(1) => {
                n = conn.execute_named(upd, &[("c", &KODE), ("y", &tahun)])?.row_count()?;
            }
            Err(e) => return Err(e.into()),
        }
    }
    if n != 1 {
        return Err(AppError::Internal("counter nomor SO gagal".into()));
    }
    let urut: i64 = conn.query_row_as_named(
        "SELECT LAST_NUMBER FROM POS_TRANSACTION_COUNTER WHERE TRANSACTION_CODE = :c AND TRANSACTION_YEAR = :y",
        &[("c", &KODE), ("y", &tahun)],
    )?;
    Ok(format_nomor(tahun, urut))
}

/// Baca ulang hasil dokumen SO yang sudah tersimpan (untuk balasan permintaan ulang).
fn baca_hasil(conn: &Connection, nomor: &str) -> AppResult<HasilOpname> {
    let sql = format!(
        "SELECT {}, {}, {} FROM POS_STOCKOPNAME WHERE NOMOR_SO = :n FETCH FIRST 1 ROW ONLY",
        tm("JUMLAHSISTEM"),
        tm("JUMLAHFISIK"),
        tm("SELISIH")
    );
    let r = opt(conn.query_row_named(&sql, &[("n", &nomor)]))?
        .ok_or_else(|| AppError::Internal("dokumen SO untuk kunci idempotensi hilang".into()))?;
    Ok(HasilOpname {
        nomor_so: nomor.to_string(),
        jumlah_sistem: dec(&r.get::<_, String>(0)?)?,
        qty_fisik: dec(&r.get::<_, String>(1)?)?,
        selisih: dec(&r.get::<_, String>(2)?)?,
        ulang: true,
    })
}

/// Simpan satu baris opname. `hari` = tanggal server. Commit di sini; rollback bila gagal.
/// Baris produk dikunci (FOR UPDATE) sebelum stok dibaca, jadi opname paralel untuk produk yang sama antre
/// dan masing-masing menghitung selisih dari stok terbaru. Idempotensi lewat MOB_IDEMPOTENCY (operasi OPNAME).
pub fn simpan(
    conn: &Connection,
    user_id: i64,
    idempotency_key: &str,
    produk_id: i64,
    qty_fisik: Decimal,
    hari: NaiveDate,
) -> AppResult<HasilOpname> {
    validasi_qty(qty_fisik)?;
    let r = simpan_inner(conn, user_id, idempotency_key, produk_id, qty_fisik, hari);
    if r.is_err() {
        let _ = conn.rollback();
    }
    r
}

fn simpan_inner(
    conn: &Connection,
    user_id: i64,
    key: &str,
    produk_id: i64,
    qty_fisik: Decimal,
    hari: NaiveDate,
) -> AppResult<HasilOpname> {
    let hash = hash_payload(produk_id, qty_fisik);
    // Klaim kunci lebih dulu: permintaan paralel dengan kunci sama menunggu di INSERT ini lalu jatuh ke ORA-00001.
    match conn.execute_named(
        "INSERT INTO MOB_IDEMPOTENCY (USER_ID, OPERASI, IDEMPOTENCY_KEY, PAYLOAD_HASH) VALUES (:u, :o, :k, :h)",
        &[("u", &user_id), ("o", &OPERASI), ("k", &key), ("h", &hash)],
    ) {
        Ok(_) => {}
        Err(e) if e.oci_code() == Some(1) => {
            let r = opt(conn.query_row_named(
                "SELECT RESOURCE_ID, PAYLOAD_HASH FROM MOB_IDEMPOTENCY WHERE USER_ID = :u AND OPERASI = :o AND IDEMPOTENCY_KEY = :k",
                &[("u", &user_id), ("o", &OPERASI), ("k", &key)],
            ))?
            .ok_or_else(|| AppError::Conflict("Permintaan sedang diproses, coba lagi".into()))?;
            let (nomor, h): (Option<String>, Option<String>) = (r.get(0)?, r.get(1)?);
            if h.as_deref() != Some(hash.as_str()) {
                return Err(AppError::Conflict(
                    "Kunci idempotensi sudah dipakai dengan data berbeda".into(),
                ));
            }
            let nomor = nomor.ok_or_else(|| AppError::Conflict("Permintaan sedang diproses, coba lagi".into()))?;
            return baca_hasil(conn, &nomor);
        }
        Err(e) => return Err(e.into()),
    }

    // Kunci baris produk: opname lain atas produk ini menunggu sampai commit/rollback kita.
    let sql = format!(
        "SELECT KODE_ITEM, {}, AKTIF FROM POS_PRODUCT WHERE PRODUCTID = :id FOR UPDATE",
        tm("NVL(BELI, 0)")
    );
    let p = opt(conn.query_row_named(&sql, &[("id", &produk_id)]))?
        .ok_or_else(|| AppError::NotFound("Produk tidak ditemukan".into()))?;
    let kode: String = p.get(0)?;
    let hpp = dec(&p.get::<_, String>(1)?)?;
    if !p
        .get::<_, Option<String>>(2)?
        .is_some_and(|a| a.trim().eq_ignore_ascii_case("Y"))
    {
        return Err(AppError::validation("Produk tidak aktif"));
    }
    let awal = NaiveDate::from_ymd_opt(hari.year(), 1, 1).expect("1 Jan");
    let sistem = katalog::stok_map(conn, std::slice::from_ref(&kode), awal, hari)?
        .get(&kode)
        .copied()
        .unwrap_or_default();
    let selisih = qty_fisik - sistem;
    if selisih.is_zero() {
        return Err(AppError::validation("Stok fisik sama dengan stok sistem"));
    }
    // Menambah stok tanpa harga beli membuat nilai persediaan (TOTAL = selisih x HPP) tidak berubah.
    if selisih > Decimal::ZERO && hpp.is_zero() {
        return Err(AppError::validation(
            "Harga beli produk belum diisi; isi di Katalog produk sebelum menambah stok lewat opname",
        ));
    }
    let nomor = alokasi_nomor(conn, hari.year())?;
    let tgl = hari.format("%Y-%m-%d").to_string();
    conn.execute_named(
        &format!(
            "INSERT INTO POS_STOCKOPNAME (NOMOR_SO, TANGGAL, KODE_BARANG, JUMLAHSISTEM, JUMLAHFISIK, SELISIH, HPP, TOTAL) \
             VALUES (:no, TO_DATE(:tgl,'YYYY-MM-DD'), :kode, {}, {}, {}, {}, {})",
            num("sis"), num("fis"), num("sel"), num("hpp"), num("tot")
        ),
        &[
            ("no", &nomor),
            ("tgl", &tgl),
            ("kode", &kode),
            ("sis", &sistem.to_string()),
            ("fis", &qty_fisik.to_string()),
            ("sel", &selisih.to_string()),
            ("hpp", &hpp.to_string()),
            ("tot", &(selisih * hpp).to_string()),
        ],
    )?;
    // Cermin nomor terakhir untuk pembaca lama (sama seperti MergeLegacyNumberSql desktop).
    conn.execute_named(
        "MERGE INTO nomor_transaksi t USING (SELECT 'STOCK_OPNAME' kode, :no nomor FROM dual) s ON (t.kode = s.kode) \
         WHEN MATCHED THEN UPDATE SET t.nomor = s.nomor \
         WHEN NOT MATCHED THEN INSERT (kode, nomor) VALUES (s.kode, s.nomor)",
        &[("no", &nomor)],
    )?;
    conn.execute_named(
        "UPDATE MOB_IDEMPOTENCY SET RESOURCE_ID = :n WHERE USER_ID = :u AND OPERASI = :o AND IDEMPOTENCY_KEY = :k",
        &[("n", &nomor), ("u", &user_id), ("o", &OPERASI), ("k", &key)],
    )?;
    conn.commit()?;
    Ok(HasilOpname {
        nomor_so: nomor,
        jumlah_sistem: sistem,
        qty_fisik,
        selisih,
        ulang: false,
    })
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn nomor_so_format() {
        assert_eq!(format_nomor(2026, 7), "SO-26-000007");
        assert_eq!(format_nomor(2031, 123456), "SO-31-123456");
    }

    #[test]
    fn qty_divalidasi() {
        assert!(validasi_qty(Decimal::from(0)).is_ok());
        assert!(validasi_qty("12.5".parse().unwrap()).is_ok());
        assert!(validasi_qty(Decimal::from(-1)).is_err());
        assert!(validasi_qty("1.00001".parse().unwrap()).is_err());
        assert!(validasi_qty(Decimal::from(MAKS_QTY + 1)).is_err());
    }

    #[test]
    fn hash_payload_stabil_dan_membedakan() {
        let a = hash_payload(7, "10".parse().unwrap());
        assert_eq!(a, hash_payload(7, "10.00".parse().unwrap()));
        assert_ne!(a, hash_payload(7, "11".parse().unwrap()));
        assert_ne!(a, hash_payload(8, "10".parse().unwrap()));
        assert_eq!(a.len(), 64);
    }
}
