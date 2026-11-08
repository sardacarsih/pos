//! Akses Oracle. Driver `oracle` bersifat blocking: semua kerja DB lewat `run` (spawn_blocking).
pub mod anggota;
pub mod dashboard;
pub mod faktur;
pub mod foto;
pub mod katalog;
pub mod profil;
pub mod user;

use std::sync::Arc;

use oracle::{
    Connection,
    pool::{Pool, PoolBuilder},
};
use rust_decimal::{Decimal, prelude::ToPrimitive};

use crate::{
    config::Config,
    error::{AppError, AppResult},
};

pub type DbPool = Arc<Pool>;

pub fn connect(cfg: &Config) -> Result<DbPool, String> {
    let pool = PoolBuilder::new(&cfg.db_user, &cfg.db_password, &cfg.db_connect)
        .max_connections(cfg.pool_max)
        .build()
        .map_err(|e| format!("gagal membuat pool Oracle (Instant Client terpasang?): {e}"))?;
    Ok(Arc::new(pool))
}

/// Jalankan blok blocking pada thread khusus dengan koneksi dari pool.
pub async fn run<T, F>(pool: &DbPool, f: F) -> AppResult<T>
where
    T: Send + 'static,
    F: FnOnce(&Connection) -> AppResult<T> + Send + 'static,
{
    let pool = pool.clone();
    tokio::task::spawn_blocking(move || {
        let conn = pool.get()?;
        f(&conn)
    })
    .await
    .map_err(|e| AppError::Internal(format!("task db: {e}")))?
}

/// Buat tabel MOB_*; ORA-00955 (sudah ada) diabaikan sehingga aman diulang.
pub fn migrate(conn: &Connection) -> AppResult<usize> {
    let sql = include_str!("schema.sql");
    let mut done = 0;
    for stmt in sql.split("\n/\n") {
        let stmt: String = stmt
            .lines()
            .filter(|l| !l.trim_start().starts_with("--"))
            .collect::<Vec<_>>()
            .join("\n");
        let stmt = stmt.trim().trim_end_matches('/').trim();
        if stmt.is_empty() {
            continue;
        }
        match conn.execute(stmt, &[]) {
            Ok(_) => done += 1,
            Err(e) if matches!(e.oci_code(), Some(955) | Some(1408) | Some(2261)) => {}
            Err(e) => return Err(e.into()),
        }
    }
    conn.commit()?;
    Ok(done)
}

// ---- Helper angka: hindari konversi implisit bergantung NLS (koma/titik) ----

/// Ekspresi SQL membaca kolom NUMBER sebagai teks berformat titik, tanpa float.
pub fn tm(col: &str) -> String {
    format!("TO_CHAR({col}, 'TM9', 'NLS_NUMERIC_CHARACTERS=''.,''')")
}

/// Ekspresi SQL mengubah bind teks (format titik) menjadi NUMBER.
pub fn num(bind: &str) -> String {
    format!("TO_NUMBER(:{bind}, '9999999999999990.9999999999', 'NLS_NUMERIC_CHARACTERS=''.,''')")
}

/// Pembulatan HANYA untuk tampilan data historis (bukan untuk menulis).
pub fn tampil(d: Decimal) -> i64 {
    d.round_dp(0).to_i64().unwrap_or(0)
}

/// Ubah "tidak ada baris" menjadi None.
pub fn opt<T>(r: oracle::Result<T>) -> AppResult<Option<T>> {
    match r {
        Ok(v) => Ok(Some(v)),
        Err(e) if e.kind() == oracle::ErrorKind::NoDataFound => Ok(None),
        Err(e) => Err(e.into()),
    }
}

pub fn dec(s: &str) -> AppResult<Decimal> {
    s.trim()
        .parse::<Decimal>()
        .map_err(|_| AppError::Internal(format!("angka tidak valid dari DB: {s}")))
}

/// Rupiah bulat untuk API; nilai pecahan ditolak agar tidak membulatkan diam-diam.
pub fn rupiah(d: Decimal) -> AppResult<i64> {
    if d.fract().is_zero() {
        d.to_i64()
            .ok_or_else(|| AppError::Internal("nilai di luar jangkauan".into()))
    } else {
        Err(AppError::Internal(format!("nilai bukan rupiah bulat: {d}")))
    }
}

/// Bind parameter bernama dinamis (daftar IN, filter opsional).
#[derive(Default)]
pub struct Binds(Vec<(String, String)>);

impl Binds {
    pub fn add(&mut self, name: impl Into<String>, v: impl ToString) {
        self.0.push((name.into(), v.to_string()));
    }
    pub fn with<T>(&self, f: impl FnOnce(&[(&str, &dyn oracle::sql_type::ToSql)]) -> T) -> T {
        let v: Vec<(&str, &dyn oracle::sql_type::ToSql)> = self
            .0
            .iter()
            .map(|(k, v)| (k.as_str(), v as &dyn oracle::sql_type::ToSql))
            .collect();
        f(&v)
    }
}

/// Escape LIKE sederhana: `%`/`_` dari input pengguna diperlakukan literal (ESCAPE '\').
pub fn like(q: &str) -> String {
    let esc: String = q
        .trim()
        .to_uppercase()
        .chars()
        .flat_map(|c| {
            if matches!(c, '%' | '_' | '\\') {
                vec!['\\', c]
            } else {
                vec![c]
            }
        })
        .collect();
    format!("%{esc}%")
}

#[cfg(test)]
mod tests {
    use super::*;
    use rust_decimal::dec;

    #[test]
    fn helpers() {
        assert_eq!(rupiah(dec!(1500)).unwrap(), 1500);
        assert_eq!(rupiah(dec!(1500.00)).unwrap(), 1500);
        assert!(rupiah(dec!(1500.5)).is_err());
        assert_eq!(like("a%b_"), "%A\\%B\\_%");
        assert_eq!(dec("12.50").unwrap(), dec!(12.5));
    }
}
