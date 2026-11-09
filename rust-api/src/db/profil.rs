//! Profil akun mobile: nama tampilan (MOB_USER.FULL_NAME) + kontak dan foto (MOB_USER_PROFIL).
use oracle::Connection;
use serde::Serialize;

use super::{foto, opt};
use crate::error::{AppError, AppResult};

#[derive(Debug, Serialize)]
pub struct Profil {
    pub username: String,
    pub nama: String,
    pub role: String,
    pub nik: Option<String>,
    pub telepon: String,
    pub email: String,
    /// null = belum ada foto.
    pub foto_versi: Option<i64>,
}

pub fn muat(conn: &Connection, id: i64) -> AppResult<Profil> {
    let r = opt(conn.query_row_named(
        "SELECT U.USERNAME, U.FULL_NAME, U.ROLE, U.NIK, P.TELEPON, P.EMAIL, P.FOTO_VERSI \
         FROM MOB_USER U LEFT JOIN MOB_USER_PROFIL P ON P.USER_ID = U.USER_ID WHERE U.USER_ID = :id",
        &[("id", &id)],
    ))?
    .ok_or_else(|| AppError::NotFound("Akun tidak ditemukan".into()))?;
    Ok(Profil {
        username: r.get(0)?,
        nama: r.get::<_, Option<String>>(1)?.unwrap_or_default(),
        role: r.get(2)?,
        nik: r.get(3)?,
        telepon: r.get::<_, Option<String>>(4)?.unwrap_or_default(),
        email: r.get::<_, Option<String>>(5)?.unwrap_or_default(),
        foto_versi: r.get(6)?,
    })
}

/// Validasi + normalisasi input profil. Telepon/email boleh kosong.
pub fn validasi(nama: &str, telepon: &str, email: &str) -> AppResult<(String, String, String)> {
    let (nama, telepon, email) = (nama.trim(), telepon.trim(), email.trim());
    if nama.is_empty() || nama.chars().count() > 100 {
        return Err(AppError::validation("Nama wajib diisi (maks 100 karakter)"));
    }
    if !telepon.is_empty() {
        let digit = telepon.chars().filter(char::is_ascii_digit).count();
        let ok = telepon.len() <= 20
            && (8..=15).contains(&digit)
            && telepon.chars().all(|c| c.is_ascii_digit() || " +-()".contains(c));
        if !ok {
            return Err(AppError::validation("Nomor telepon tidak valid"));
        }
    }
    if !email.is_empty() {
        let ok = email.len() <= 100
            && !email.contains(char::is_whitespace)
            && email.split_once('@').is_some_and(|(u, d)| {
                !u.is_empty() && d.contains('.') && !d.starts_with('.') && !d.ends_with('.')
            });
        if !ok {
            return Err(AppError::validation("Format email tidak valid"));
        }
    }
    Ok((nama.into(), telepon.into(), email.into()))
}

pub fn ubah(conn: &Connection, id: i64, nama: &str, telepon: &str, email: &str) -> AppResult<Profil> {
    let (nama, telepon, email) = validasi(nama, telepon, email)?;
    conn.execute_named(
        "UPDATE MOB_USER SET FULL_NAME = :n WHERE USER_ID = :id",
        &[("n", &nama), ("id", &id)],
    )?;
    let (t, e) = (
        (!telepon.is_empty()).then_some(telepon),
        (!email.is_empty()).then_some(email),
    );
    let n = conn.execute_named(
        "UPDATE MOB_USER_PROFIL SET TELEPON = :t, EMAIL = :e WHERE USER_ID = :id",
        &[("t", &t), ("e", &e), ("id", &id)],
    )?;
    if n.row_count()? == 0 {
        conn.execute_named(
            "INSERT INTO MOB_USER_PROFIL (USER_ID, TELEPON, EMAIL) VALUES (:id, :t, :e)",
            &[("id", &id), ("t", &t), ("e", &e)],
        )?;
    }
    conn.commit()?;
    muat(conn, id)
}

pub fn simpan_foto(conn: &Connection, id: i64, data: &[u8]) -> AppResult<i64> {
    let jenis = foto::validasi_foto(data)?;
    let versi = chrono::Utc::now().timestamp();
    let n = conn.execute_named(
        "UPDATE MOB_USER_PROFIL SET FOTO_TYPE = :ct, FOTO = :d, FOTO_VERSI = :v WHERE USER_ID = :id",
        &[("ct", &jenis), ("d", &data), ("v", &versi), ("id", &id)],
    )?;
    if n.row_count()? == 0 {
        conn.execute_named(
            "INSERT INTO MOB_USER_PROFIL (USER_ID, FOTO_TYPE, FOTO, FOTO_VERSI) VALUES (:id, :ct, :d, :v)",
            &[("id", &id), ("ct", &jenis), ("d", &data), ("v", &versi)],
        )?;
    }
    conn.commit()?;
    Ok(versi)
}

pub fn ambil_foto(conn: &Connection, id: i64) -> AppResult<Option<foto::Foto>> {
    let r = opt(conn.query_row_named(
        "SELECT FOTO_TYPE, FOTO FROM MOB_USER_PROFIL WHERE USER_ID = :id AND FOTO IS NOT NULL",
        &[("id", &id)],
    ))?;
    Ok(match r {
        Some(r) => Some(foto::Foto {
            content_type: r.get(0)?,
            data: r.get(1)?,
        }),
        None => None,
    })
}

/// Foto akun ANGGOTA aktif dengan NIK ini (untuk kasir memverifikasi identitas). Hanya foto, tanpa data lain.
pub fn ambil_foto_by_nik(conn: &Connection, nik: &str) -> AppResult<Option<foto::Foto>> {
    let r = opt(conn.query_row_named(
        "SELECT P.FOTO_TYPE, P.FOTO FROM MOB_USER_PROFIL P JOIN MOB_USER U ON U.USER_ID = P.USER_ID \
         WHERE U.NIK = :nik AND U.ROLE = 'ANGGOTA' AND U.IS_ACTIVE = 1 AND P.FOTO IS NOT NULL",
        &[("nik", &nik)],
    ))?;
    Ok(match r {
        Some(r) => Some(foto::Foto {
            content_type: r.get(0)?,
            data: r.get(1)?,
        }),
        None => None,
    })
}

pub fn hapus_foto(conn: &Connection, id: i64) -> AppResult<bool> {
    let n = conn.execute_named(
        "UPDATE MOB_USER_PROFIL SET FOTO_TYPE = NULL, FOTO = NULL, FOTO_VERSI = NULL WHERE USER_ID = :id AND FOTO IS NOT NULL",
        &[("id", &id)],
    )?;
    conn.commit()?;
    Ok(n.row_count()? > 0)
}

#[cfg(test)]
mod tests {
    use super::validasi;

    #[test]
    fn validasi_profil() {
        assert!(validasi("Aan", "", "").is_ok());
        assert!(validasi("  ", "", "").is_err());
        assert!(validasi("Aan", "0812-3456-7890", "a@b.co").is_ok());
        assert!(validasi("Aan", "123", "").is_err());
        assert!(validasi("Aan", "08123abc4567", "").is_err());
        assert!(validasi("Aan", "", "tanpa-at").is_err());
        assert!(validasi("Aan", "", "a@b").is_err());
        assert_eq!(validasi(" Aan ", "", "").unwrap().0, "Aan");
    }
}
