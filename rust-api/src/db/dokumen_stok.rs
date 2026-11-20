//! Pengaman dan log bersama untuk ubah/hapus dokumen stok (Stock Opname `POS_STOCKOPNAME`, Barang Rusak `POS_BARANGRUSAK`).
//! Dua tabel itu berbagi nomor `SO-YY-NNNNNN` dan sama-sama menggeser stok sistem (`STOK_EXPR`), jadi aturannya satu.
use chrono::NaiveDate;
use oracle::Connection;
use rust_decimal::Decimal;
use sha2::{Digest, Sha256};

use super::{Binds, faktur};
use crate::error::{AppError, AppResult};

pub const MAKS_ALASAN: usize = 250;

#[derive(Clone, Copy, PartialEq, Eq, Debug)]
pub enum Jenis {
    So,
    Rusak,
}

impl Jenis {
    pub fn kode(self) -> &'static str {
        match self {
            Jenis::So => "SO",
            Jenis::Rusak => "RUSAK",
        }
    }
}

pub fn hex(d: impl AsRef<[u8]>) -> String {
    d.as_ref().iter().map(|b| format!("{b:02x}")).collect()
}

/// Sidik isi dokumen (baris sudah diurutkan pemanggil); dibaca klien lalu dikirim balik sebagai `versi`
/// agar ubah/hapus atas data yang sudah berubah ditolak (optimistic lock).
pub fn sidik_versi(baris: &[String]) -> String {
    hex(Sha256::digest(baris.join(";").as_bytes()))
}

/// Alasan wajib diisi (jejak audit), tanpa spasi pinggir, maksimal 250 karakter.
pub fn alasan_valid(alasan: Option<&str>) -> AppResult<String> {
    let a = alasan.map(str::trim).unwrap_or_default();
    if a.is_empty() {
        return Err(AppError::validation("Alasan wajib diisi"));
    }
    if a.chars().count() > MAKS_ALASAN {
        return Err(AppError::validation(format!("Alasan maksimal {MAKS_ALASAN} karakter")));
    }
    Ok(a.to_string())
}

/// Dokumen boleh diubah/dihapus hanya bila: periode tanggalnya masih terbuka dan tidak ada dokumen SO/rusak yang
/// lebih baru untuk barang yang sama. Fisik SO berikutnya bersifat absolut, jadi mengubah dokumen lama
/// menggeser stok akhir tanpa hitungan baru yang mengoreksinya.
pub fn cek_boleh_ubah(conn: &Connection, nomor: &str, tanggal: NaiveDate, kodes: &[String]) -> AppResult<()> {
    faktur::cek_periode(conn, tanggal)?;
    if kodes.is_empty() {
        return Ok(());
    }
    let mut b = Binds::default();
    b.add("t", tanggal.format("%Y-%m-%d"));
    b.add("n", nomor);
    let names: Vec<String> = kodes
        .iter()
        .enumerate()
        .map(|(i, k)| {
            b.add(format!("k{i}"), k);
            format!(":k{i}")
        })
        .collect();
    let in_list = names.join(",");
    let sql = format!(
        "SELECT KODE_BARANG, NOMOR_SO FROM ( \
           SELECT KODE_BARANG, NOMOR_SO, TANGGAL FROM POS_STOCKOPNAME \
           UNION ALL SELECT KODE_BARANG, NOMOR_SO, TANGGAL FROM POS_BARANGRUSAK) \
         WHERE KODE_BARANG IN ({in_list}) AND NOMOR_SO <> :n \
           AND (TANGGAL > TO_DATE(:t,'YYYY-MM-DD') OR (TANGGAL = TO_DATE(:t,'YYYY-MM-DD') AND NOMOR_SO > :n)) \
         FETCH FIRST 1 ROWS ONLY"
    );
    let lebih_baru: Option<(String, String)> = b.with(|p| -> AppResult<_> {
        match super::opt(conn.query_row_named(&sql, p))? {
            Some(r) => Ok(Some((r.get(0)?, r.get(1)?))),
            None => Ok(None),
        }
    })?;
    if let Some((kode, no)) = lebih_baru {
        return Err(AppError::rule(
            "BENTROK_DOKUMEN_BARU",
            format!("Barang {kode} sudah punya dokumen yang lebih baru ({no}); ubah atau hapus dokumen itu lebih dulu"),
        ));
    }
    Ok(())
}

/// Catat satu baris log ringkas (siapa, kapan, nomor, jumlah baris, total nilai). Dalam transaksi pemanggil.
#[allow(clippy::too_many_arguments)]
pub fn catat_log(
    conn: &Connection,
    jenis: Jenis,
    nomor: &str,
    aksi: &str,
    user_id: i64,
    user_nama: &str,
    alasan: Option<&str>,
    baris: usize,
    total_nilai: Decimal,
) -> AppResult<()> {
    conn.execute_named(
        "INSERT INTO POS_DOKUMEN_STOK_LOG (JENIS, NOMOR, AKSI, USER_ID, USER_NAMA, ALASAN, JUMLAH_BARIS, TOTAL_NILAI) \
         VALUES (:j, :n, :a, :u, :un, :al, :b, TO_NUMBER(:t, '9999999999999990.99', 'NLS_NUMERIC_CHARACTERS=''.,'''))",
        &[
            ("j", &jenis.kode()),
            ("n", &nomor),
            ("a", &aksi),
            ("u", &user_id),
            ("un", &user_nama),
            ("al", &alasan),
            ("b", &(baris as i64)),
            ("t", &total_nilai.round_dp(2).to_string()),
        ],
    )?;
    Ok(())
}

/// Tandai kunci idempotensi selesai (tanpa menyentuh cermin nomor terakhir seperti `opname::selesai`).
pub fn tandai_selesai(conn: &Connection, user_id: i64, operasi: &str, key: &str, nomor: &str) -> AppResult<()> {
    conn.execute_named(
        "UPDATE MOB_IDEMPOTENCY SET RESOURCE_ID = :n WHERE USER_ID = :u AND OPERASI = :o AND IDEMPOTENCY_KEY = :k",
        &[("n", &nomor), ("u", &user_id), ("o", &operasi), ("k", &key)],
    )?;
    Ok(())
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn alasan_wajib_dan_dibatasi() {
        assert!(alasan_valid(None).is_err());
        assert!(alasan_valid(Some("   ")).is_err());
        assert_eq!(alasan_valid(Some("  salah hitung ")).unwrap(), "salah hitung");
        assert!(alasan_valid(Some(&"x".repeat(251))).is_err());
        assert!(alasan_valid(Some(&"x".repeat(250))).is_ok());
    }

    #[test]
    fn versi_berubah_bila_isi_berubah() {
        let a = sidik_versi(&["K1|1|2|1".into()]);
        assert_eq!(a, sidik_versi(&["K1|1|2|1".into()]));
        assert_ne!(a, sidik_versi(&["K1|1|3|2".into()]));
    }
}
