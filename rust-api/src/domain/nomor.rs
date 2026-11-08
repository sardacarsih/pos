/// Nomor berikutnya format `W-yy-nnnnnn`. `terakhir` = isi NOMOR_TRANSAKSI (kode PENJUALAN).
/// Tahun baru atau kosong mulai dari 1 (port GenerateTransactionNumber).
pub fn berikutnya(terakhir: Option<&str>, tahun: i32) -> String {
    let prefix = format!("W-{:02}", tahun % 100);
    match terakhir {
        Some(t) if t.starts_with(&prefix) && t.len() >= 6 => {
            let seq: u32 = t[t.len() - 6..].parse().unwrap_or(0);
            format!("{prefix}-{:06}", seq + 1)
        }
        _ => format!("{prefix}-000001"),
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn increments_and_resets() {
        assert_eq!(berikutnya(None, 2026), "W-26-000001");
        assert_eq!(berikutnya(Some("W-26-000123"), 2026), "W-26-000124");
        assert_eq!(berikutnya(Some("W-25-000999"), 2026), "W-26-000001");
    }
}
