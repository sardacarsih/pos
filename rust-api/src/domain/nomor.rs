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

#[cfg(test)]
mod tests {
    use super::*;

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
