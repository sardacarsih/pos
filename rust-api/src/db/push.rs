use oracle::Connection;
use sha2::{Digest, Sha256};

use crate::error::AppResult;

pub fn register(conn: &Connection, user: i64, device: &str, token: &str) -> AppResult<()> {
    let hash: String = Sha256::digest(token.as_bytes())
        .iter()
        .map(|b| format!("{b:02x}"))
        .collect();
    conn.execute_named(
        "DELETE FROM MOB_PUSH_DEVICE WHERE TOKEN_HASH = :h AND DEVICE_ID <> :d",
        &[("h", &hash), ("d", &device)],
    )?;
    conn.execute_named(
        "MERGE INTO MOB_PUSH_DEVICE t USING (SELECT :d DEVICE_ID, :u USER_ID, :h TOKEN_HASH, :t TOKEN FROM DUAL) s \
         ON (t.DEVICE_ID = s.DEVICE_ID) WHEN MATCHED THEN UPDATE SET t.USER_ID=s.USER_ID, t.TOKEN_HASH=s.TOKEN_HASH, t.TOKEN=s.TOKEN, t.UPDATED_AT=SYSTIMESTAMP \
         WHEN NOT MATCHED THEN INSERT (DEVICE_ID, USER_ID, TOKEN_HASH, TOKEN) VALUES (s.DEVICE_ID,s.USER_ID,s.TOKEN_HASH,s.TOKEN)",
        &[("d", &device), ("u", &user), ("h", &hash), ("t", &token)],
    )?;
    conn.commit()?;
    Ok(())
}

pub fn unregister(conn: &Connection, user: i64, device: &str) -> AppResult<()> {
    conn.execute_named(
        "DELETE FROM MOB_PUSH_DEVICE WHERE USER_ID=:u AND DEVICE_ID=:d",
        &[("u", &user), ("d", &device)],
    )?;
    conn.commit()?;
    Ok(())
}

pub struct Event {
    pub id: i64,
    pub number: String,
    pub nik: String,
    pub lease: String,
}

pub fn claim(conn: &Connection) -> AppResult<Option<Event>> {
    let candidate = super::opt(conn.query_row("SELECT SALE_ID, NO_TRANSAKSI, NIK FROM MOB_PUSH_QUEUE WHERE SENT_AT IS NULL AND NEXT_ATTEMPT <= SYSTIMESTAMP AND (LEASE_UNTIL IS NULL OR LEASE_UNTIL < SYSTIMESTAMP) ORDER BY CREATED_AT FETCH FIRST 1 ROW ONLY", &[]))?;
    let Some(row) = candidate else {
        return Ok(None);
    };
    let id: i64 = row.get(0)?;
    let lease = uuid::Uuid::new_v4().to_string();
    let updated = conn.execute_named("UPDATE MOB_PUSH_QUEUE SET LEASE_ID=:l, LEASE_UNTIL=SYSTIMESTAMP + NUMTODSINTERVAL(30, 'MINUTE') WHERE SALE_ID=:id AND SENT_AT IS NULL AND NEXT_ATTEMPT <= SYSTIMESTAMP AND (LEASE_UNTIL IS NULL OR LEASE_UNTIL < SYSTIMESTAMP)", &[("l", &lease), ("id", &id)])?.row_count()?;
    conn.commit()?;
    if updated != 1 {
        return Ok(None);
    }
    Ok(Some(Event {
        id,
        number: row.get(1)?,
        nik: row.get(2)?,
        lease,
    }))
}

pub fn devices(conn: &Connection, nik: &str) -> AppResult<Vec<String>> {
    let mut result = Vec::new();
    for row in conn.query_named("SELECT D.TOKEN FROM MOB_PUSH_DEVICE D JOIN MOB_USER U ON U.USER_ID=D.USER_ID WHERE U.NIK=:n AND U.ROLE='ANGGOTA' AND U.IS_ACTIVE=1 AND U.MUST_CHANGE_PASSWORD=0", &[("n", &nik)])? {
        result.push(row?.get(0)?);
    }
    Ok(result)
}

pub fn invalid_token(conn: &Connection, token: &str) -> AppResult<()> {
    let hash: String = Sha256::digest(token.as_bytes())
        .iter()
        .map(|b| format!("{b:02x}"))
        .collect();
    conn.execute_named(
        "DELETE FROM MOB_PUSH_DEVICE WHERE TOKEN_HASH=:h",
        &[("h", &hash)],
    )?;
    conn.commit()?;
    Ok(())
}

pub fn finish(conn: &Connection, id: i64, lease: &str, retry: bool) -> AppResult<()> {
    let sql = if retry {
        "UPDATE MOB_PUSH_QUEUE SET SENT_AT=CASE WHEN ATTEMPTS+1 >= 10 THEN SYSTIMESTAMP END, ATTEMPTS=ATTEMPTS+1, NEXT_ATTEMPT=SYSTIMESTAMP + NUMTODSINTERVAL(LEAST(3600, 30 * POWER(2, LEAST(ATTEMPTS, 7))), 'SECOND'), LEASE_ID=NULL, LEASE_UNTIL=NULL WHERE SALE_ID=:id AND LEASE_ID=:l"
    } else {
        "UPDATE MOB_PUSH_QUEUE SET SENT_AT=SYSTIMESTAMP, LEASE_ID=NULL, LEASE_UNTIL=NULL WHERE SALE_ID=:id AND LEASE_ID=:l"
    };
    conn.execute_named(sql, &[("id", &id), ("l", &lease)])?;
    conn.commit()?;
    Ok(())
}

/// Hapus antrean yang sudah selesai/menyerah lebih dari 30 hari (tabel tidak tumbuh tanpa batas).
pub fn purge(conn: &Connection) -> AppResult<()> {
    conn.execute(
        "DELETE FROM MOB_PUSH_QUEUE WHERE SENT_AT IS NOT NULL AND SENT_AT < SYSTIMESTAMP - INTERVAL '30' DAY",
        &[],
    )?;
    conn.commit()?;
    Ok(())
}
