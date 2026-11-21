use axum::{
    Json,
    extract::{Path, State},
    http::StatusCode,
};
use oracle::Connection;
use serde::{Deserialize, Serialize};

use super::{AppState, Auth};
use crate::{
    auth::Role,
    db,
    error::{AppError, AppResult},
};

#[derive(Serialize, oracle::RowValue)]
pub struct Unit {
    kode: String,
    nama: Option<String>,
    potong_shu: Option<String>,
}

#[derive(Deserialize)]
#[serde(deny_unknown_fields)]
pub struct FormUnit {
    nama: String,
    potong_shu: bool,
}

impl FormUnit {
    fn nilai(&self) -> AppResult<(String, &'static str)> {
        let nama = self.nama.trim().to_uppercase();
        if nama.is_empty() || nama.len() > 50 || nama.chars().any(char::is_control) {
            return Err(AppError::validation(
                "Nama unit wajib diisi dan maksimal 50 byte",
            ));
        }
        Ok((nama, if self.potong_shu { "Y" } else { "T" }))
    }
}

fn kode_valid(kode: &str) -> AppResult<()> {
    if kode.is_empty() || kode.len() > 2 || kode.chars().any(char::is_control) {
        return Err(AppError::validation("Kode unit tidak valid"));
    }
    Ok(())
}

fn kode_berikutnya(terakhir: i64) -> AppResult<String> {
    if !(0..99).contains(&terakhir) {
        return Err(AppError::Conflict(
            "Kode unit numerik sudah mencapai batas 99".into(),
        ));
    }
    Ok(format!("{:02}", terakhir + 1))
}

fn konflik_db(e: oracle::Error) -> AppError {
    match e.oci_code() {
        Some(54 | 30006) => AppError::Conflict("Data unit sedang dipakai. Coba lagi.".into()),
        Some(2292) => {
            AppError::Conflict("Unit kerja masih digunakan dan tidak dapat dihapus".into())
        }
        _ => e.into(),
    }
}

fn transaksi<T>(c: &Connection, kerja: impl FnOnce() -> AppResult<T>) -> AppResult<T> {
    let hasil = kerja().and_then(|nilai| {
        c.commit()?;
        Ok(nilai)
    });
    if hasil.is_err() {
        c.rollback()?;
    }
    hasil
}

fn tidak_ada() -> AppError {
    AppError::NotFound("Unit kerja tidak ditemukan".into())
}

pub async fn daftar(State(s): State<AppState>, a: Auth) -> AppResult<Json<Vec<Unit>>> {
    a.peran(&[Role::Admin])?;
    Ok(Json(
        db::run(&s.pool, |c| {
            let unit = c
                .query_as::<Unit>(
                    "SELECT KODE, NAMA, SW_POT_SHU AS POTONG_SHU FROM FIN_UNITKERJA ORDER BY NAMA, KODE",
                    &[],
                )?
                .collect::<Result<Vec<_>, _>>()?;
            Ok(unit)
        })
        .await?,
    ))
}

pub async fn buat(
    State(s): State<AppState>,
    a: Auth,
    Json(form): Json<FormUnit>,
) -> AppResult<(StatusCode, Json<Unit>)> {
    a.peran(&[Role::Admin])?;
    let (nama, potong) = form.nilai()?;
    let unit = db::run(&s.pool, move |c| transaksi(c, || {
        c.execute("LOCK TABLE FIN_UNITKERJA IN SHARE ROW EXCLUSIVE MODE NOWAIT", &[])
            .map_err(konflik_db)?;
        let terakhir: i64 = c.query_row_as(
            "SELECT NVL(MAX(CASE WHEN REGEXP_LIKE(KODE, '^[0-9]{1,2}$') THEN TO_NUMBER(KODE) END), 0)
             FROM FIN_UNITKERJA", &[],
        )?;
        let kode = kode_berikutnya(terakhir)?;
        c.execute(
            "INSERT INTO FIN_UNITKERJA (KODE, NAMA, SW_POT_SHU) VALUES (:1, :2, :3)",
            &[&kode, &nama, &potong],
        ).map_err(konflik_db)?;
        Ok(Unit { kode, nama: Some(nama), potong_shu: Some(potong.into()) })
    })).await?;
    Ok((StatusCode::CREATED, Json(unit)))
}

pub async fn ubah(
    State(s): State<AppState>,
    a: Auth,
    Path(kode): Path<String>,
    Json(form): Json<FormUnit>,
) -> AppResult<Json<Unit>> {
    a.peran(&[Role::Admin])?;
    kode_valid(&kode)?;
    let (nama, potong) = form.nilai()?;
    Ok(Json(
        db::run(&s.pool, move |c| {
            transaksi(c, || {
                let stmt = c
                    .execute(
                        "UPDATE FIN_UNITKERJA SET NAMA = :1, SW_POT_SHU = :2 WHERE KODE = :3",
                        &[&nama, &potong, &kode],
                    )
                    .map_err(konflik_db)?;
                if stmt.row_count()? == 0 {
                    return Err(tidak_ada());
                }
                Ok(Unit {
                    kode,
                    nama: Some(nama),
                    potong_shu: Some(potong.into()),
                })
            })
        })
        .await?,
    ))
}

fn ident(nama: &str) -> String {
    format!("\"{}\"", nama.replace('"', "\"\""))
}

fn cek_penggunaan(c: &Connection, kode: &str, nama: &Option<String>) -> AppResult<()> {
    let hapus_berantai: i64 = c.query_row_as(
        "SELECT COUNT(*) FROM ALL_CONSTRAINTS ANAK
         JOIN USER_CONSTRAINTS INDUK ON INDUK.CONSTRAINT_NAME = ANAK.R_CONSTRAINT_NAME
         WHERE ANAK.R_OWNER = USER AND INDUK.TABLE_NAME = 'FIN_UNITKERJA'
           AND ANAK.CONSTRAINT_TYPE = 'R' AND ANAK.DELETE_RULE <> 'NO ACTION'",
        &[],
    )?;
    if hapus_berantai > 0 {
        return Err(AppError::Conflict(
            "Unit kerja memiliki relasi dengan penghapusan berantai".into(),
        ));
    }
    // Legacy rows can store either the work-unit code or its name.
    // Include the deployed reference columns and POS customer tables when present.
    let kolom = c
        .query_as::<(String, String)>(
            "SELECT C.TABLE_NAME, C.COLUMN_NAME FROM USER_TAB_COLUMNS C
         JOIN USER_TABLES T ON T.TABLE_NAME = C.TABLE_NAME
         WHERE C.TABLE_NAME <> 'FIN_UNITKERJA'
           AND C.COLUMN_NAME IN ('UNIT_KERJA', 'UNITKERJA', 'UNITLAMA', 'UNITBARU',
                                 'KODE_UNIT', 'KODE_UNITKERJA', 'KODEUK', 'KODE_UK')
         ORDER BY C.TABLE_NAME, C.COLUMN_NAME",
            &[],
        )?
        .collect::<Result<Vec<_>, _>>()?;
    if !kolom
        .iter()
        .any(|(t, k)| t == "FIN_ANGGOTA" && k == "UNIT_KERJA")
    {
        return Err(AppError::Conflict(
            "Referensi unit kerja belum dapat diverifikasi".into(),
        ));
    }
    let mut terkunci = None;
    for (tabel, kolom) in &kolom {
        let nama_tabel = ident(tabel);
        if terkunci != Some(tabel) {
            c.execute(
                &format!("LOCK TABLE {nama_tabel} IN SHARE MODE NOWAIT"),
                &[],
            )
            .map_err(konflik_db)?;
            terkunci = Some(tabel);
        }
        let kolom = ident(kolom);
        let dipakai: i64 = c.query_row_as(
            &format!(
                "SELECT COUNT(*) FROM {nama_tabel}
                WHERE (UPPER(TRIM({kolom})) IN (UPPER(TRIM(:1)), UPPER(TRIM(:2))))
                  AND ROWNUM = 1"
            ),
            &[&kode, nama],
        )?;
        if dipakai > 0 {
            return Err(AppError::Conflict(
                "Unit kerja masih digunakan dan tidak dapat dihapus".into(),
            ));
        }
    }
    Ok(())
}

pub async fn hapus(
    State(s): State<AppState>,
    a: Auth,
    Path(kode): Path<String>,
) -> AppResult<StatusCode> {
    a.peran(&[Role::Admin])?;
    kode_valid(&kode)?;
    db::run(&s.pool, move |c| {
        transaksi(c, || {
            let (nama, akun) = c
                .query_as::<(Option<String>, Option<String>)>(
                    "SELECT NAMA, ACKODE FROM FIN_UNITKERJA WHERE KODE = :1 FOR UPDATE NOWAIT",
                    &[&kode],
                )
                .map_err(konflik_db)?
                .next()
                .transpose()
                .map_err(konflik_db)?
                .ok_or_else(tidak_ada)?;
            if akun.as_deref().is_some_and(|v| !v.trim().is_empty()) {
                return Err(AppError::Conflict(
                    "Unit kerja terkait akun jurnal dan tidak dapat dihapus".into(),
                ));
            }
            cek_penggunaan(c, &kode, &nama)?;
            c.execute("DELETE FROM FIN_UNITKERJA WHERE KODE = :1", &[&kode])
                .map_err(konflik_db)?;
            Ok(())
        })
    })
    .await?;
    Ok(StatusCode::NO_CONTENT)
}

#[cfg(test)]
mod tests {
    use super::{FormUnit, kode_berikutnya};

    #[test]
    fn kode_mengikuti_batas_kolom_oracle() {
        assert_eq!(kode_berikutnya(0).unwrap(), "01");
        assert_eq!(kode_berikutnya(9).unwrap(), "10");
        assert_eq!(kode_berikutnya(98).unwrap(), "99");
        assert!(kode_berikutnya(99).is_err());
    }

    #[test]
    fn validasi_nama_menggunakan_byte_setelah_huruf_besar() {
        assert_eq!(
            FormUnit {
                nama: "  kebun  ".into(),
                potong_shu: false
            }
            .nilai()
            .unwrap(),
            ("KEBUN".into(), "T")
        );
        assert!(
            FormUnit {
                nama: "é".repeat(26),
                potong_shu: true
            }
            .nilai()
            .is_err()
        );
    }
}
