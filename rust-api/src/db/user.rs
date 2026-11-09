use oracle::Connection;
use serde::Serialize;

use super::opt;
use crate::{
    auth::{Role, throttle},
    error::{AppError, AppResult},
};

#[derive(Debug, Clone)]
pub struct UserRow {
    pub id: i64,
    pub username: String,
    pub full_name: String,
    pub hash: String,
    pub role: Role,
    pub nik: Option<String>,
    pub active: bool,
    pub must_change: bool,
    pub locked: bool,
}

const USER_COLS: &str = "USER_ID, USERNAME, NVL(FULL_NAME, USERNAME), PASSWORD_HASH, ROLE, NIK, IS_ACTIVE, MUST_CHANGE_PASSWORD, \
    CASE WHEN LOCKED_UNTIL IS NOT NULL AND LOCKED_UNTIL > SYSTIMESTAMP THEN 1 ELSE 0 END";

fn map(row: &oracle::Row) -> AppResult<UserRow> {
    let role: String = row.get(4)?;
    Ok(UserRow {
        id: row.get(0)?,
        username: row.get(1)?,
        full_name: row.get(2)?,
        hash: row.get(3)?,
        role: role.parse().map_err(AppError::Internal)?,
        nik: row.get(5)?,
        active: row.get::<_, i64>(6)? == 1,
        must_change: row.get::<_, i64>(7)? == 1,
        locked: row.get::<_, i64>(8)? == 1,
    })
}

pub fn find_by_username(conn: &Connection, username: &str) -> AppResult<Option<UserRow>> {
    let sql = format!("SELECT {USER_COLS} FROM MOB_USER WHERE USERNAME = :u");
    match opt(conn.query_row_named(&sql, &[("u", &username.to_lowercase())]))? {
        Some(r) => Ok(Some(map(&r)?)),
        None => Ok(None),
    }
}

pub fn find_by_id(conn: &Connection, id: i64) -> AppResult<Option<UserRow>> {
    let sql = format!("SELECT {USER_COLS} FROM MOB_USER WHERE USER_ID = :id");
    match opt(conn.query_row_named(&sql, &[("id", &id)]))? {
        Some(r) => Ok(Some(map(&r)?)),
        None => Ok(None),
    }
}

/// Catat login gagal; kunci sementara sesuai kebijakan. Tidak menyentuh akun desktop.
pub fn register_failure(conn: &Connection, id: i64) -> AppResult<()> {
    conn.execute_named(
        "UPDATE MOB_USER SET FAILED_ATTEMPTS = FAILED_ATTEMPTS + 1 WHERE USER_ID = :id",
        &[("id", &id)],
    )?;
    let failed: i64 = conn.query_row_as_named(
        "SELECT FAILED_ATTEMPTS FROM MOB_USER WHERE USER_ID = :id",
        &[("id", &id)],
    )?;
    if let Some(min) = throttle::lock_minutes(failed as i32) {
        conn.execute_named(
            "UPDATE MOB_USER SET LOCKED_UNTIL = SYSTIMESTAMP + NUMTODSINTERVAL(:m, 'MINUTE') WHERE USER_ID = :id",
            &[("m", &min), ("id", &id)],
        )?;
    }
    conn.commit()?;
    Ok(())
}

pub fn reset_failures(conn: &Connection, id: i64) -> AppResult<()> {
    conn.execute_named(
        "UPDATE MOB_USER SET FAILED_ATTEMPTS = 0, LOCKED_UNTIL = NULL WHERE USER_ID = :id",
        &[("id", &id)],
    )?;
    conn.commit()?;
    Ok(())
}

pub fn set_password(conn: &Connection, id: i64, hash: &str, must_change: bool) -> AppResult<()> {
    conn.execute_named(
        "UPDATE MOB_USER SET PASSWORD_HASH = :h, MUST_CHANGE_PASSWORD = :m, FAILED_ATTEMPTS = 0, LOCKED_UNTIL = NULL WHERE USER_ID = :id",
        &[("h", &hash), ("m", &(must_change as i64)), ("id", &id)],
    )?;
    conn.commit()?;
    Ok(())
}

/// Ganti hash tanpa mengubah flag ganti-password (rehash transparan setelah login).
pub fn upgrade_hash(conn: &Connection, id: i64, hash: &str) -> AppResult<()> {
    conn.execute_named(
        "UPDATE MOB_USER SET PASSWORD_HASH = :h WHERE USER_ID = :id",
        &[("h", &hash), ("id", &id)],
    )?;
    conn.commit()?;
    Ok(())
}

// ---------------------------------------------------------------- sesi

pub struct SessionRow {
    pub id: i64,
    pub family: String,
    pub user_id: i64,
    pub used: bool,
    pub revoked: bool,
    pub expired: bool,
    pub device: Option<String>,
}

pub fn create_session(
    conn: &Connection,
    user_id: i64,
    family: &str,
    token_hash: &str,
    device: &str,
    ip: &str,
    ttl_days: i64,
) -> AppResult<()> {
    conn.execute_named(
        "INSERT INTO MOB_SESSION (FAMILY_ID, USER_ID, TOKEN_HASH, DEVICE_ID, IP, EXPIRES_AT) \
         VALUES (:f, :u, :h, :d, :ip, SYSTIMESTAMP + NUMTODSINTERVAL(:days, 'DAY'))",
        &[
            ("f", &family),
            ("u", &user_id),
            ("h", &token_hash),
            ("d", &device),
            ("ip", &ip),
            ("days", &ttl_days),
        ],
    )?;
    conn.commit()?;
    Ok(())
}

pub fn find_session(conn: &Connection, token_hash: &str) -> AppResult<Option<SessionRow>> {
    let r = opt(conn.query_row_named(
        "SELECT SESSION_ID, FAMILY_ID, USER_ID, \
         CASE WHEN USED_AT IS NULL THEN 0 ELSE 1 END, CASE WHEN REVOKED_AT IS NULL THEN 0 ELSE 1 END, \
         CASE WHEN EXPIRES_AT < SYSTIMESTAMP THEN 1 ELSE 0 END, DEVICE_ID FROM MOB_SESSION WHERE TOKEN_HASH = :h",
        &[("h", &token_hash)],
    ))?;
    match r {
        Some(r) => Ok(Some(SessionRow {
            id: r.get(0)?,
            family: r.get(1)?,
            user_id: r.get(2)?,
            used: r.get::<_, i64>(3)? == 1,
            revoked: r.get::<_, i64>(4)? == 1,
            expired: r.get::<_, i64>(5)? == 1,
            device: r.get(6)?,
        })),
        None => Ok(None),
    }
}

/// Tandai token terpakai secara atomik; false bila sudah dipakai/dicabut (balapan atau pemakaian ulang).
pub fn consume_session(conn: &Connection, id: i64) -> AppResult<bool> {
    let st = conn.execute_named(
        "UPDATE MOB_SESSION SET USED_AT = SYSTIMESTAMP WHERE SESSION_ID = :id AND USED_AT IS NULL AND REVOKED_AT IS NULL",
        &[("id", &id)],
    )?;
    let n = st.row_count()?;
    conn.commit()?;
    Ok(n == 1)
}

pub fn revoke_family(conn: &Connection, family: &str) -> AppResult<()> {
    conn.execute_named("UPDATE MOB_SESSION SET REVOKED_AT = SYSTIMESTAMP WHERE FAMILY_ID = :f AND REVOKED_AT IS NULL", &[("f", &family)])?;
    conn.commit()?;
    Ok(())
}

pub fn revoke_user(conn: &Connection, user_id: i64) -> AppResult<()> {
    conn.execute_named("DELETE FROM MOB_PUSH_DEVICE WHERE USER_ID = :u", &[("u", &user_id)])?;
    conn.execute_named("UPDATE MOB_SESSION SET REVOKED_AT = SYSTIMESTAMP WHERE USER_ID = :u AND REVOKED_AT IS NULL", &[("u", &user_id)])?;
    conn.commit()?;
    Ok(())
}

// -------------------------------------------------------------- admin

#[derive(Debug, Serialize)]
pub struct UserInfo {
    pub id: i64,
    pub username: String,
    pub nama: String,
    pub role: Role,
    pub nik: Option<String>,
    pub aktif: bool,
    pub terkunci: bool,
    pub kategori: Vec<String>,
    /// Versi foto profil (epoch detik); null = tanpa foto. Kunci cache klien untuk daftar admin.
    pub foto_versi: Option<i64>,
}

/// True bila NIK ini sudah dipakai akun mobile (satu NIK satu akun).
pub fn nik_sudah_punya_akun(conn: &Connection, nik: &str) -> AppResult<bool> {
    let n: i64 = conn.query_row_as_named("SELECT COUNT(*) FROM MOB_USER WHERE NIK = :n", &[("n", &nik)])?;
    Ok(n > 0)
}

pub fn create_user(
    conn: &Connection,
    username: &str,
    full_name: &str,
    hash: &str,
    role: Role,
    nik: Option<&str>,
) -> AppResult<i64> {
    conn.execute_named(
        "INSERT INTO MOB_USER (USERNAME, FULL_NAME, PASSWORD_HASH, ROLE, NIK) VALUES (:u, :n, :h, :r, :nik)",
        &[("u", &username.to_lowercase()), ("n", &full_name), ("h", &hash), ("r", &role.as_str()), ("nik", &nik)],
    )?;
    let id: i64 = conn.query_row_as_named(
        "SELECT USER_ID FROM MOB_USER WHERE USERNAME = :u",
        &[("u", &username.to_lowercase())],
    )?;
    conn.commit()?;
    Ok(id)
}

pub fn list_users(conn: &Connection) -> AppResult<Vec<UserInfo>> {
    let sql = format!(
        "SELECT {USER_COLS}, (SELECT P.FOTO_VERSI FROM MOB_USER_PROFIL P WHERE P.USER_ID = MOB_USER.USER_ID) \
         FROM MOB_USER ORDER BY USERNAME"
    );
    let mut out = Vec::new();
    for r in conn.query(&sql, &[])? {
        let r = r?;
        let u = map(&r)?;
        let foto_versi: Option<i64> = r.get(9)?;
        out.push(UserInfo {
            foto_versi,
            id: u.id,
            username: u.username,
            nama: u.full_name,
            role: u.role,
            nik: u.nik,
            aktif: u.active,
            terkunci: u.locked,
            kategori: Vec::new(),
        });
    }
    Ok(out)
}

pub fn set_active(conn: &Connection, id: i64, active: bool) -> AppResult<()> {
    conn.execute_named(
        "UPDATE MOB_USER SET IS_ACTIVE = :a WHERE USER_ID = :id",
        &[("a", &(active as i64)), ("id", &id)],
    )?;
    conn.commit()?;
    if !active {
        revoke_user(conn, id)?;
    }
    Ok(())
}

/// Kategori yang tampil di kasir (pengaturan global admin). Kosong = tidak dibatasi.
pub fn allowed_kategori(conn: &Connection) -> AppResult<Vec<String>> {
    let mut out = Vec::new();
    for r in conn.query_as::<String>(
        "SELECT KATEGORI_ID FROM MOB_KATEGORI_TAMPIL ORDER BY KATEGORI_ID",
        &[],
    )? {
        out.push(r?);
    }
    Ok(out)
}

pub fn set_kategori(conn: &Connection, ids: &[String]) -> AppResult<()> {
    conn.execute("DELETE FROM MOB_KATEGORI_TAMPIL", &[])?;
    for id in ids {
        conn.execute(
            "INSERT INTO MOB_KATEGORI_TAMPIL (KATEGORI_ID) VALUES (:1)",
            &[id],
        )?;
    }
    conn.commit()?;
    Ok(())
}
