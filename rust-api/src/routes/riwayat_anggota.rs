use axum::{
    Json,
    extract::{Path, Query, State},
    http::StatusCode,
};
use oracle::Connection;
use serde::{Deserialize, Serialize};

use super::{AppState, Auth};
use crate::{
    auth::{Role, token::Claims},
    db,
    error::{AppError, AppResult},
};

pub fn aktif() -> bool {
    std::env::var("POS_WEB_MEMBER_HISTORY").as_deref() == Ok("true")
}

pub fn audit_tmk_tersedia(c: &Connection) -> AppResult<bool> {
    Ok(c.query_row_as::<i64>(
        "SELECT COUNT(*) FROM USER_TAB_COLUMNS WHERE TABLE_NAME = 'MOB_ANGGOTA_RIWAYAT'
         AND COLUMN_NAME IN ('TMK_DARI', 'TMK_KE') AND DATA_TYPE = 'DATE'",
        &[],
    )? == 2)
}

pub fn transaksi<T>(c: &Connection, kerja: impl FnOnce() -> AppResult<T>) -> AppResult<T> {
    let hasil = kerja().and_then(|nilai| {
        c.commit()?;
        Ok(nilai)
    });
    if hasil.is_err() {
        c.rollback()?;
    }
    hasil
}

const JENIS: &[&str] = &[
    "MASUK",
    "MUTASI",
    "NONAKTIF",
    "AKTIF",
    "BERHENTI",
    "GABUNG_KEMBALI",
];

#[derive(Clone, Serialize, oracle::RowValue)]
pub struct Kondisi {
    id: i64,
    nik: Option<String>,
    nama: Option<String>,
    unit: Option<String>,
    status: Option<String>,
    aktif: Option<String>,
    anggota: Option<String>,
    tmk: Option<String>,
}

#[derive(Deserialize, PartialEq)]
#[serde(deny_unknown_fields)]
pub struct Salinan {
    unit: Option<String>,
    status: Option<String>,
    aktif: Option<String>,
    anggota: Option<String>,
}

impl Kondisi {
    fn salinan(&self) -> Salinan {
        Salinan {
            unit: self.unit.clone(),
            status: self.status.clone(),
            aktif: self.aktif.clone(),
            anggota: self.anggota.clone(),
        }
    }
}

pub fn anggota(c: &Connection, id: i64, umum: &str) -> AppResult<Kondisi> {
    Ok(c.query_as::<Kondisi>(
        "SELECT ID_PELANGGAN AS ID, NIK, NAMA_PELANGGAN AS NAMA, UNIT_KERJA AS UNIT, STATUS, AKTIF, ANGGOTA,
                TO_CHAR(TMK, 'YYYY-MM-DD') AS TMK
         FROM FIN_ANGGOTA WHERE ID_PELANGGAN = :1 AND (NIK IS NULL OR NIK <> :2) FOR UPDATE",
        &[&id, &umum],
    )?.next().transpose()?.ok_or_else(|| AppError::NotFound("Data anggota tidak ditemukan".into()))?)
}

pub fn alasan_valid(alasan: &str) -> AppResult<&str> {
    let alasan = alasan.trim();
    if alasan.is_empty() || alasan.len() > 500 || alasan.chars().any(char::is_control) {
        return Err(AppError::validation(
            "Alasan wajib diisi dan maksimal 500 byte",
        ));
    }
    Ok(alasan)
}

pub fn catat(
    c: &Connection,
    admin: &Claims,
    jenis: &str,
    alasan: &str,
    awal: Option<&Kondisi>,
    akhir: &Kondisi,
) -> AppResult<()> {
    let awal_unit = awal.and_then(|a| a.unit.as_deref());
    let awal_status = awal.and_then(|a| a.status.as_deref());
    let awal_aktif = awal.and_then(|a| a.aktif.as_deref());
    let awal_anggota = awal.and_then(|a| a.anggota.as_deref());
    let mut binds: Vec<(&str, &dyn oracle::sql_type::ToSql)> = vec![
        ("id", &akhir.id),
        ("nik", &akhir.nik),
        ("nama", &akhir.nama),
        ("jenis", &jenis),
        ("admin_id", &admin.sub),
        ("admin_nama", &admin.usr),
        ("alasan", &alasan),
        ("unit_dari", &awal_unit),
        ("unit_ke", &akhir.unit),
        ("status_dari", &awal_status),
        ("status_ke", &akhir.status),
        ("aktif_dari", &awal_aktif),
        ("aktif_ke", &akhir.aktif),
        ("anggota_dari", &awal_anggota),
        ("anggota_ke", &akhir.anggota),
    ];
    let awal_tmk = awal.and_then(|a| a.tmk.as_deref());
    let (kolom_tmk, nilai_tmk) = if jenis == "GABUNG_KEMBALI" {
        binds.extend([
            ("tmk_dari", &awal_tmk as &dyn oracle::sql_type::ToSql),
            ("tmk_ke", &akhir.tmk),
        ]);
        (
            ",TMK_DARI,TMK_KE",
            ",TO_DATE(:tmk_dari,'YYYY-MM-DD'),TO_DATE(:tmk_ke,'YYYY-MM-DD')",
        )
    } else {
        ("", "")
    };
    c.execute_named(
        &format!("INSERT INTO MOB_ANGGOTA_RIWAYAT (ID_PELANGGAN,NIK,NAMA,JENIS,ADMIN_ID,ADMIN_NAMA,ALASAN,WAKTU,
          UNIT_DARI,UNIT_KE,STATUS_DARI,STATUS_KE,AKTIF_DARI,AKTIF_KE,ANGGOTA_DARI,ANGGOTA_KE{kolom_tmk})
         VALUES (:id,:nik,:nama,:jenis,:admin_id,:admin_nama,:alasan,
          CAST(SYSTIMESTAMP AT TIME ZONE 'Asia/Jakarta' AS TIMESTAMP),
          :unit_dari,:unit_ke,:status_dari,:status_ke,:aktif_dari,:aktif_ke,:anggota_dari,:anggota_ke{nilai_tmk})"),
        &binds,
    )?;
    Ok(())
}

#[derive(Deserialize)]
#[serde(rename_all = "snake_case", tag = "aksi", deny_unknown_fields)]
pub enum Perubahan {
    Mutasi {
        awal: Salinan,
        unit: String,
        status: String,
        alasan: String,
    },
    Berhenti {
        awal: Salinan,
        alasan: String,
    },
    GabungKembali {
        awal: Salinan,
        alasan: String,
        tanggal_efektif: Option<String>,
        tmk_lama: Option<String>,
    },
}

pub async fn ubah(
    State(s): State<AppState>,
    a: Auth,
    Path(id): Path<i64>,
    Json(data): Json<Perubahan>,
) -> AppResult<StatusCode> {
    let admin = a.peran(&[Role::Admin])?.clone();
    if !aktif() {
        return Err(AppError::Forbidden(
            "Riwayat anggota belum diaktifkan".into(),
        ));
    }
    let umum = s.cfg.default_nik.clone();
    db::run(&s.pool, move |c| {
        transaksi(c, || terapkan(c, id, &umum, &admin, data))
    })
    .await?;
    Ok(StatusCode::NO_CONTENT)
}

pub fn terapkan(
    c: &Connection,
    id: i64,
    umum: &str,
    admin: &Claims,
    data: Perubahan,
) -> AppResult<()> {
    let awal = anggota(c, id, umum)?;
    let mut akhir = awal.clone();
    let (salinan, alasan) = match &data {
        Perubahan::Mutasi { awal, alasan, .. }
        | Perubahan::Berhenti { awal, alasan }
        | Perubahan::GabungKembali { awal, alasan, .. } => (awal, alasan_valid(alasan)?),
    };
    if &awal.salinan() != salinan {
        return Err(AppError::Conflict(
            "Data anggota telah berubah. Muat ulang daftar.".into(),
        ));
    }
    let jenis = match data {
        Perubahan::Mutasi {
            ref unit,
            ref status,
            ..
        } => {
            if awal.anggota.as_deref() != Some("Y") {
                return Err(AppError::Conflict(
                    "Mutasi hanya tersedia untuk anggota koperasi".into(),
                ));
            }
            let unit = unit.trim();
            if unit.is_empty() || unit.len() > 20 || !matches!(status.as_str(), "BULANAN" | "KHT") {
                return Err(AppError::validation(
                    "Pilih unit dan kategori tagihan yang valid",
                ));
            }
            let ada = c
                .query_as::<String>(
                    "SELECT KODE FROM FIN_UNITKERJA WHERE KODE = :1 FOR UPDATE",
                    &[&unit],
                )?
                .next()
                .transpose()?;
            if ada.is_none() {
                return Err(AppError::validation("Unit kerja tidak ditemukan"));
            }
            akhir.unit = Some(unit.into());
            akhir.status = Some(status.clone());
            if akhir.salinan() == awal.salinan() {
                return Err(AppError::validation(
                    "Pilih unit atau kategori tagihan yang berbeda",
                ));
            }
            c.execute(
                "UPDATE FIN_ANGGOTA SET UNIT_KERJA = :1, STATUS = :2 WHERE ID_PELANGGAN = :3",
                &[&akhir.unit, &akhir.status, &id],
            )?;
            "MUTASI"
        }
        Perubahan::Berhenti { .. } => {
            if awal.anggota.as_deref() != Some("Y") {
                return Err(AppError::Conflict("Keanggotaan sudah berakhir".into()));
            }
            akhir.anggota = Some("T".into());
            akhir.aktif = Some("T".into());
            c.execute(
                "UPDATE FIN_ANGGOTA SET ANGGOTA = 'T', AKTIF = 'T',
                 TGLNONAKTIF = TRUNC(CAST(SYSTIMESTAMP AT TIME ZONE 'Asia/Jakarta' AS DATE))
                 WHERE ID_PELANGGAN = :1",
                &[&id],
            )?;
            if let Some(nik) = &awal.nik {
                let pengguna = c.query_as::<i64>(
                    "SELECT USER_ID FROM MOB_USER WHERE NIK = :1 AND ROLE = 'ANGGOTA' FOR UPDATE", &[nik],
                )?.next().transpose()?;
                if let Some(uid) = pengguna {
                    c.execute(
                        "UPDATE MOB_USER SET IS_ACTIVE = 0 WHERE USER_ID = :1",
                        &[&uid],
                    )?;
                    c.execute("UPDATE MOB_SESSION SET REVOKED_AT = SYSTIMESTAMP WHERE USER_ID = :1 AND REVOKED_AT IS NULL", &[&uid])?;
                    c.execute("DELETE FROM MOB_PUSH_DEVICE WHERE USER_ID = :1", &[&uid])?;
                }
            }
            "BERHENTI"
        }
        Perubahan::GabungKembali {
            ref tanggal_efektif,
            ref tmk_lama,
            ..
        } => {
            let kejadian = c.query_as::<String>(
                "SELECT JENIS FROM MOB_ANGGOTA_RIWAYAT WHERE ID_PELANGGAN = :1
                 AND JENIS IN ('BERHENTI', 'GABUNG_KEMBALI') ORDER BY ID DESC FETCH FIRST 1 ROW ONLY", &[&id],
            )?.next().transpose()?;
            if awal.anggota.as_deref() != Some("T")
                || awal.aktif.as_deref() != Some("T")
                || awal.nik.is_none()
                || kejadian.as_deref() != Some("BERHENTI")
            {
                return Err(AppError::Conflict(
                    "Gabung kembali hanya untuk anggota yang tercatat berhenti".into(),
                ));
            }
            if !audit_tmk_tersedia(c)? {
                return Err(AppError::Conflict(
                    "Pencatatan tanggal gabung belum tersedia. Hubungi pengelola sistem.".into(),
                ));
            }
            let hari: String = c.query_row_as(
                "SELECT TO_CHAR(CAST(SYSTIMESTAMP AT TIME ZONE 'Asia/Jakarta' AS DATE), 'YYYY-MM-DD') FROM DUAL",
                &[],
            )?;
            if tanggal_efektif
                .as_ref()
                .is_some_and(|tanggal| *tanggal != hari || *tmk_lama != awal.tmk)
            {
                return Err(AppError::Conflict("Tanggal masuk atau tanggal efektif sudah berubah. Muat ulang dan periksa kembali.".into()));
            }
            akhir.anggota = Some("Y".into());
            akhir.aktif = Some("Y".into());
            akhir.tmk = Some(hari);
            c.execute(
                "UPDATE FIN_ANGGOTA SET ANGGOTA = 'Y', AKTIF = 'Y', TGLNONAKTIF = NULL,
                 TMK = TO_DATE(:1, 'YYYY-MM-DD') WHERE ID_PELANGGAN = :2",
                &[&akhir.tmk, &id],
            )?;
            "GABUNG_KEMBALI"
        }
    };
    catat(c, admin, jenis, alasan, Some(&awal), &akhir)?;
    Ok(())
}

#[derive(Deserialize)]
pub struct Filter {
    dari: Option<String>,
    sampai: Option<String>,
    jenis: Option<String>,
    q: Option<String>,
    id: Option<i64>,
    halaman: Option<u32>,
}

#[derive(Serialize, oracle::RowValue)]
pub struct Kejadian {
    id: i64,
    nik: Option<String>,
    nama: Option<String>,
    jenis: String,
    admin_nama: String,
    alasan: String,
    waktu: String,
    unit_dari: Option<String>,
    unit_ke: Option<String>,
    status_dari: Option<String>,
    status_ke: Option<String>,
    aktif_dari: Option<String>,
    aktif_ke: Option<String>,
    anggota_dari: Option<String>,
    anggota_ke: Option<String>,
    tmk_dari: Option<String>,
    tmk_ke: Option<String>,
}

#[derive(Serialize)]
pub struct Halaman {
    data: Vec<Kejadian>,
    lanjut: bool,
}

pub async fn daftar(
    State(s): State<AppState>,
    a: Auth,
    Query(f): Query<Filter>,
) -> AppResult<Json<Halaman>> {
    a.peran(&[Role::Admin])?;
    if !aktif() {
        return Err(AppError::Forbidden(
            "Riwayat anggota belum diaktifkan".into(),
        ));
    }
    let halaman = f.halaman.unwrap_or(1);
    let q = f.q.unwrap_or_default();
    let tanggal_valid = |v: &str| {
        chrono::NaiveDate::parse_from_str(v, "%Y-%m-%d").is_ok_and(|tgl| tgl.to_string() == v)
    };
    if !(1..=100_000).contains(&halaman)
        || q.len() > 100
        || f.jenis.as_deref().is_some_and(|v| !JENIS.contains(&v))
        || f.dari.as_deref().is_some_and(|v| !tanggal_valid(v))
        || f.sampai.as_deref().is_some_and(|v| !tanggal_valid(v))
        || matches!((&f.dari, &f.sampai), (Some(dari), Some(sampai)) if dari > sampai)
    {
        return Err(AppError::validation("Filter riwayat tidak valid"));
    }
    let offset = i64::from(halaman - 1) * 25;
    Ok(Json(db::run(&s.pool, move |c| {
        let kolom_tmk = if audit_tmk_tersedia(c)? {
            "TO_CHAR(TMK_DARI,'YYYY-MM-DD') AS TMK_DARI, TO_CHAR(TMK_KE,'YYYY-MM-DD') AS TMK_KE"
        } else {
            "CAST(NULL AS VARCHAR2(10)) AS TMK_DARI, CAST(NULL AS VARCHAR2(10)) AS TMK_KE"
        };
        let mut data = c.query_as_named::<Kejadian>(
            &format!("SELECT ID,NIK,NAMA,JENIS,ADMIN_NAMA,ALASAN,TO_CHAR(WAKTU,'YYYY-MM-DD HH24:MI') AS WAKTU,
              UNIT_DARI,UNIT_KE,STATUS_DARI,STATUS_KE,AKTIF_DARI,AKTIF_KE,ANGGOTA_DARI,ANGGOTA_KE,{kolom_tmk}
             FROM MOB_ANGGOTA_RIWAYAT
             WHERE (:id IS NULL OR ID_PELANGGAN = :id)
               AND (:jenis IS NULL OR JENIS = :jenis)
               AND (:dari IS NULL OR WAKTU >= TO_DATE(:dari,'YYYY-MM-DD'))
               AND (:sampai IS NULL OR WAKTU < TO_DATE(:sampai,'YYYY-MM-DD') + 1)
               AND (:q IS NULL OR INSTR(UPPER(NIK),UPPER(:q)) > 0 OR INSTR(UPPER(NAMA),UPPER(:q)) > 0)
             ORDER BY WAKTU DESC, ID DESC OFFSET :offset ROWS FETCH NEXT 26 ROWS ONLY"),
            &[("id", &f.id), ("jenis", &f.jenis), ("dari", &f.dari), ("sampai", &f.sampai), ("q", &q), ("offset", &offset)],
        )?.collect::<Result<Vec<_>, _>>()?;
        let lanjut = data.len() > 25;
        data.truncate(25);
        Ok(Halaman { data, lanjut })
    }).await?))
}

#[cfg(test)]
mod tests {
    use super::{Perubahan, alasan_valid};
    use serde_json::json;

    #[test]
    fn perubahan_membawa_snapshot_dan_alasan() {
        let data = json!({
            "aksi": "berhenti",
            "awal": { "unit": "02", "status": "KHT", "aktif": "Y", "anggota": "Y" },
            "alasan": "  Permintaan anggota  "
        });
        let Ok(Perubahan::Berhenti { alasan, .. }) = serde_json::from_value(data.clone()) else {
            panic!("aksi berhenti gagal dibaca");
        };
        assert_eq!(alasan_valid(&alasan).unwrap(), "Permintaan anggota");
        for field in ["awal", "alasan"] {
            let mut tidak_lengkap = data.clone();
            tidak_lengkap.as_object_mut().unwrap().remove(field);
            assert!(serde_json::from_value::<Perubahan>(tidak_lengkap).is_err());
        }
        assert!(alasan_valid("   ").is_err());
        assert!(alasan_valid(&"é".repeat(251)).is_err());
    }
}
