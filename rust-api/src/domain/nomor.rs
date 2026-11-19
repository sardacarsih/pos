/// Nomor berikutnya format `BBM-yy-nnnnnn`. `terakhir` = isi NOMOR_TRANSAKSI (baris kode BBM, terpisah dari PENJUALAN milik desktop).
/// Tahun baru atau kosong mulai dari 1 (port GenerateTransactionNumber).
pub fn berikutnya(terakhir: Option<&str>, tahun: i32) -> String {
    let prefix = format!("BBM-{:02}", tahun % 100);
    match terakhir {
        Some(t) if t.starts_with(&prefix) && t.len() >= 6 => {
            let seq: u32 = t[t.len() - 6..].parse().unwrap_or(0);
            format!("{prefix}-{:06}", seq + 1)
        }
        _ => format!("{prefix}-000001"),
    }
}

/// Kode item produk berikutnya format `KB-yy-nnnnn` (sama dengan frmMasterBarang desktop).
/// `terakhir` = isi NOMOR_TRANSAKSI baris BARANG; tahun baru atau kosong mulai dari 1.
pub fn kode_produk_berikutnya(terakhir: Option<&str>, tahun: i32) -> String {
    let prefix = format!("KB-{:02}", tahun % 100);
    match terakhir {
        Some(t) if t.starts_with(&prefix) && t.len() >= 5 => {
            let seq: u32 = t[t.len() - 5..].parse().unwrap_or(0);
            format!("{prefix}-{:05}", seq + 1)
        }
        _ => format!("{prefix}-00001"),
    }
}

/// Nomor pinjaman `P-yy-nnnnnn` (ucPinjaman.GenerateTransactionNumber). `terakhir` = kandidat nomor terakhir
/// (NOMOR_TRANSAKSI baris PINJAMAN dan MAX FIN_PINJAMAN); urutan terbesar tahun berjalan + 1, agar tidak bentrok dengan BackOffice.
pub fn pinjaman_berikutnya(terakhir: &[Option<&str>], tahun: i32) -> String {
    let prefix = format!("P-{:02}-", tahun % 100);
    let seq = terakhir
        .iter()
        .flatten()
        .filter_map(|t| t.strip_prefix(&prefix)?.parse::<u32>().ok())
        .max()
        .unwrap_or(0);
    format!("{prefix}{:06}", seq + 1)
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn nomor_pinjaman_ambil_terbesar() {
        assert_eq!(pinjaman_berikutnya(&[None, None], 2026), "P-26-000001");
        assert_eq!(pinjaman_berikutnya(&[Some("P-26-000007"), Some("P-26-000012")], 2026), "P-26-000013");
        assert_eq!(pinjaman_berikutnya(&[Some("P-25-000099"), None], 2026), "P-26-000001");
    }

    #[test]
    fn kode_produk_naik_dan_reset_tahun() {
        assert_eq!(kode_produk_berikutnya(None, 2026), "KB-26-00001");
        assert_eq!(kode_produk_berikutnya(Some("KB-26-00009"), 2026), "KB-26-00010");
        assert_eq!(kode_produk_berikutnya(Some("KB-25-00999"), 2026), "KB-26-00001");
        assert_eq!(kode_produk_berikutnya(Some("sampah"), 2026), "KB-26-00001");
    }

    #[test]
    fn increments_and_resets() {
        assert_eq!(berikutnya(None, 2026), "BBM-26-000001");
        assert_eq!(berikutnya(Some("BBM-26-000123"), 2026), "BBM-26-000124");
        assert_eq!(berikutnya(Some("BBM-25-000999"), 2026), "BBM-26-000001");
    }
}
