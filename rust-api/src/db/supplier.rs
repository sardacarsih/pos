use oracle::Connection;
use serde::{Deserialize, Serialize};

use crate::error::{AppError, AppResult};

use super::{like, opt};

const UKURAN_HALAMAN: usize = 30;

#[derive(Serialize)]
pub struct Supplier {
    pub kode: String,
    pub nama: Option<String>,
    pub aktif: bool,
}

impl Supplier {
    fn dari_baris(row: oracle::Row) -> oracle::Result<Self> {
        Ok(Self {
            kode: row.get(0)?,
            nama: row.get(1)?,
            aktif: row.get::<_, Option<String>>(2)?.as_deref() == Some("Y"),
        })
    }
}

#[derive(Default, Deserialize)]
pub struct Filter {
    pub q: Option<String>,
    pub aktif: Option<bool>,
    pub page: Option<u32>,
}

#[derive(Serialize)]
pub struct Halaman {
    pub items: Vec<Supplier>,
    pub ada_lagi: bool,
    pub page: u32,
}

#[derive(Deserialize)]
#[serde(deny_unknown_fields)]
pub struct Baru {
    pub kode: String,
    pub nama: String,
}

#[derive(Deserialize)]
#[serde(deny_unknown_fields)]
pub struct Ubah {
    pub nama: Option<String>,
    pub aktif: Option<bool>,
}

fn normalisasi(teks: &str, label: &str, batas: usize) -> AppResult<String> {
    let teks = teks.trim().to_uppercase();
    if teks.is_empty() {
        return Err(AppError::validation(format!("{label} wajib diisi")));
    }
    if teks.len() > batas {
        return Err(AppError::validation(format!(
            "{label} maksimal {batas} byte"
        )));
    }
    if teks.chars().any(char::is_control) {
        return Err(AppError::validation(format!(
            "{label} mengandung karakter kontrol"
        )));
    }
    Ok(teks)
}

pub fn daftar(c: &Connection, filter: &Filter) -> AppResult<Halaman> {
    let page = filter.page.unwrap_or(1).clamp(1, 100_000);
    let offset = i64::from(page - 1) * UKURAN_HALAMAN as i64;
    let q = filter.q.as_deref().map(str::trim).unwrap_or("");
    if q.chars().count() > 100 {
        return Err(AppError::validation("Pencarian maksimal 100 karakter"));
    }
    let pola = (!q.is_empty()).then(|| like(q));
    let status = filter.aktif.map(|aktif| if aktif { "Y" } else { "N" });
    let mut items = c
        .query_named(
            "SELECT KODE,NAMA,AKTIF FROM POS_SUPPLIER
             WHERE (:pola IS NULL OR UPPER(KODE) LIKE :pola ESCAPE '\\' OR UPPER(NAMA) LIKE :pola ESCAPE '\\')
               AND (:status_supplier IS NULL OR CASE WHEN AKTIF='Y' THEN 'Y' ELSE 'N' END=:status_supplier)
             ORDER BY NAMA NULLS LAST,KODE OFFSET :offset_baris ROWS FETCH NEXT 31 ROWS ONLY",
            &[("pola", &pola), ("status_supplier", &status), ("offset_baris", &offset)],
        )?
        .map(|row| row.and_then(Supplier::dari_baris))
        .collect::<oracle::Result<Vec<_>>>()?;
    let ada_lagi = items.len() > UKURAN_HALAMAN;
    items.truncate(UKURAN_HALAMAN);
    Ok(Halaman {
        items,
        ada_lagi,
        page,
    })
}

pub fn buat(c: &Connection, req: &Baru) -> AppResult<Supplier> {
    let kode = normalisasi(&req.kode, "Kode supplier", 20)?;
    // Segmen URL "." dan ".." dinormalisasi oleh browser sebelum permintaan dikirim.
    if matches!(kode.as_str(), "." | "..") || kode.contains(['/', '\\']) {
        return Err(AppError::validation(
            "Kode supplier tidak boleh berupa titik, dua titik, atau mengandung garis miring",
        ));
    }
    let nama = normalisasi(&req.nama, "Nama supplier", 50)?;
    c.execute_named(
        "INSERT INTO POS_SUPPLIER (KODE,NAMA,AKTIF) VALUES (:kode,:nama,'Y')",
        &[("kode", &kode), ("nama", &nama)],
    )
    .map_err(|e| {
        if e.oci_code() == Some(1) {
            AppError::Conflict("Kode supplier sudah digunakan".into())
        } else {
            e.into()
        }
    })?;
    c.commit()?;
    Ok(Supplier {
        kode,
        nama: Some(nama),
        aktif: true,
    })
}

pub fn ubah(c: &Connection, kode: &str, req: &Ubah) -> AppResult<Supplier> {
    let (sql, nilai) = match (&req.nama, req.aktif) {
        (Some(nama), None) => (
            "UPDATE POS_SUPPLIER SET NAMA=:nilai WHERE KODE=:kode",
            normalisasi(nama, "Nama supplier", 50)?,
        ),
        (None, Some(aktif)) => (
            "UPDATE POS_SUPPLIER SET AKTIF=:nilai WHERE KODE=:kode",
            if aktif { "Y".into() } else { "N".into() },
        ),
        _ => {
            return Err(AppError::validation(
                "Ubah nama atau status supplier dalam satu permintaan",
            ));
        }
    };
    let hasil = (|| -> AppResult<Supplier> {
        let stmt = c.execute_named(sql, &[("nilai", &nilai), ("kode", &kode)])?;
        if stmt.row_count()? == 0 {
            return Err(AppError::NotFound("Supplier tidak ditemukan".into()));
        }
        let row = opt(c.query_row_named(
            "SELECT KODE,NAMA,AKTIF FROM POS_SUPPLIER WHERE KODE=:kode",
            &[("kode", &kode)],
        ))?
        .ok_or_else(|| AppError::NotFound("Supplier tidak ditemukan".into()))?;
        let supplier = Supplier::dari_baris(row)?;
        c.commit()?;
        Ok(supplier)
    })();
    if hasil.is_err() {
        let _ = c.rollback();
    }
    hasil
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn panjang_dihitung_dalam_byte_setelah_normalisasi() {
        assert_eq!(normalisasi("  s-01  ", "Kode", 20).unwrap(), "S-01");
        assert!(normalisasi("   ", "Kode", 20).is_err());
        assert!(normalisasi(&"é".repeat(10), "Kode", 20).is_ok());
        assert!(normalisasi(&"é".repeat(11), "Kode", 20).is_err());
        assert!(normalisasi("ß", "Nama", 1).is_err());
        assert!(normalisasi(&"é".repeat(25), "Nama", 50).is_ok());
        assert!(normalisasi(&"é".repeat(26), "Nama", 50).is_err());
    }
}
