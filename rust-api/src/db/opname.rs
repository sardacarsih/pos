//! Stock opname per barang dari mobile/web. Tabel dan nomor SO dibagi dengan desktop
//! (`BackOffice/DataLayer/StockOpnameRepository.cs`): hasilnya langsung terbaca rumus stok dan laporan desktop.
use chrono::{Datelike, NaiveDate};
use oracle::Connection;
use rust_decimal::Decimal;
use serde::Serialize;

use super::{katalog, num, opt, tm};
use crate::error::{AppError, AppResult};

const KODE: &str = "STOCK_OPNAME";
const MAKS_QTY: i64 = 1_000_000_000;

#[derive(Debug, Serialize)]
pub struct HasilOpname {
    pub nomor_so: String,
    pub jumlah_sistem: Decimal,
    pub qty_fisik: Decimal,
    pub selisih: Decimal,
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

/// Simpan satu baris opname. `hari` = tanggal server. Commit di sini; rollback bila gagal.
pub fn simpan(
    conn: &Connection,
    produk_id: i64,
    qty_fisik: Decimal,
    hari: NaiveDate,
) -> AppResult<HasilOpname> {
    validasi_qty(qty_fisik)?;
    let r = (|| -> AppResult<HasilOpname> {
        let sql = format!(
            "SELECT KODE_ITEM, {}, AKTIF FROM POS_PRODUCT WHERE PRODUCTID = :id",
            tm("NVL(BELI, 0)")
        );
        let p = opt(conn.query_row_named(&sql, &[("id", &produk_id)]))?
            .ok_or_else(|| AppError::NotFound("Produk tidak ditemukan".into()))?;
        let kode: String = p.get(0)?;
        let hpp = super::dec(&p.get::<_, String>(1)?)?;
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
        conn.commit()?;
        Ok(HasilOpname {
            nomor_so: nomor,
            jumlah_sistem: sistem,
            qty_fisik,
            selisih,
        })
    })();
    if r.is_err() {
        let _ = conn.rollback();
    }
    r
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
}
