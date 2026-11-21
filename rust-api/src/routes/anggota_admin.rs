use axum::{
    Json,
    extract::{Path, Query, State},
};
use chrono::Datelike;
use oracle::Connection;
use serde::{Deserialize, Serialize};

use super::{AppState, Auth};
use crate::{
    auth::Role,
    db,
    error::{AppError, AppResult},
};

const UKURAN: usize = 25;
const KOLOM: &str =
    "ID_PELANGGAN AS ID, NIK, NAMA_PELANGGAN AS NAMA, UNIT_KERJA AS UNIT, AKTIF, ANGGOTA, STATUS,
     NVL(LIMIT_HUTANG, 0) AS LIMIT_HUTANG, TO_CHAR(TMK, 'YYYY-MM-DD') AS TMK";

#[derive(Deserialize)]
pub struct Filter {
    q: Option<String>,
    unit: Option<String>,
    aktif: Option<String>,
    anggota: Option<String>,
    status: Option<String>,
    halaman: Option<u32>,
}

#[derive(Serialize, oracle::RowValue)]
pub struct Ringkas {
    id: i64,
    nik: Option<String>,
    nama: Option<String>,
    unit: Option<String>,
    aktif: Option<String>,
    anggota: Option<String>,
    status: Option<String>,
    limit_hutang: i64,
    dapat_gabung: i64,
    tmk: Option<String>,
}

#[derive(Serialize)]
pub struct Halaman {
    data: Vec<Ringkas>,
    lanjut: bool,
    registrasi: bool,
    riwayat: bool,
    audit_tmk: bool,
}

#[derive(Debug, Deserialize, Serialize, PartialEq, oracle::RowValue)]
#[serde(deny_unknown_fields)]
pub struct Profil {
    nama: Option<String>,
    alamat: Option<String>,
    kota: Option<String>,
    kode_pos: Option<String>,
    telepon: Option<String>,
    #[serde(default, deserialize_with = "baca_limit")]
    limit_hutang: Option<i64>,
}

fn baca_limit<'de, D: serde::Deserializer<'de>>(d: D) -> Result<Option<i64>, D::Error> {
    i64::deserialize(d).map(Some)
}

const LIMIT_HUTANG_MAX: i64 = 9_007_199_254_740_991;

#[derive(Serialize)]
pub struct Detail {
    anggota: Ringkas,
    profil: Profil,
}

#[derive(Deserialize)]
#[serde(deny_unknown_fields)]
pub struct Edit {
    awal: Profil,
    profil: Profil,
    mutasi: Option<MutasiProfil>,
}

#[derive(Deserialize)]
#[serde(deny_unknown_fields)]
struct MutasiProfil {
    awal: super::riwayat_anggota::Salinan,
    unit: String,
    status: String,
    alasan: String,
}

pub async fn daftar(
    State(s): State<AppState>,
    a: Auth,
    Query(f): Query<Filter>,
) -> AppResult<Json<Halaman>> {
    a.peran(&[Role::Admin])?;
    let halaman = f.halaman.unwrap_or(1);
    let q = f.q.as_deref().unwrap_or_default().trim();
    if !(1..=100_000).contains(&halaman)
        || q.len() > 100
        || f.unit.as_ref().is_some_and(|v| v.len() > 20)
        || f.aktif.as_deref().is_some_and(|v| !matches!(v, "Y" | "T"))
        || f.anggota
            .as_deref()
            .is_some_and(|v| !matches!(v, "Y" | "T"))
        || f.status
            .as_deref()
            .is_some_and(|v| !matches!(v, "BULANAN" | "KHT"))
    {
        return Err(AppError::validation("Filter anggota tidak valid"));
    }
    let q = q.to_owned();
    let umum = s.cfg.default_nik.clone();
    let registrasi = s.cfg.registrasi_anggota_web;
    let riwayat = super::riwayat_anggota::aktif();
    let dapat_gabung = if riwayat {
        "CASE WHEN ANGGOTA = 'T' AND AKTIF = 'T' AND NIK IS NOT NULL AND EXISTS (
           SELECT 1 FROM MOB_ANGGOTA_RIWAYAT R WHERE R.ID_PELANGGAN = FIN_ANGGOTA.ID_PELANGGAN
             AND R.JENIS = 'BERHENTI' AND NOT EXISTS (
               SELECT 1 FROM MOB_ANGGOTA_RIWAYAT G WHERE G.ID_PELANGGAN = R.ID_PELANGGAN
                 AND G.JENIS = 'GABUNG_KEMBALI' AND G.ID > R.ID
             )
        ) THEN 1 ELSE 0 END"
    } else {
        "0"
    };
    let offset = i64::from(halaman - 1) * UKURAN as i64;
    Ok(Json(
        db::run(&s.pool, move |c| {
            let mut data = c
                .query_as_named::<Ringkas>(
                    &format!(
                        "SELECT {KOLOM}, {dapat_gabung} AS DAPAT_GABUNG FROM FIN_ANGGOTA
                WHERE (NIK IS NULL OR NIK <> :umum) AND (:q IS NULL OR INSTR(UPPER(NAMA_PELANGGAN), UPPER(:q)) > 0
                    OR INSTR(UPPER(NIK), UPPER(:q)) > 0)
                AND (:unit IS NULL OR UNIT_KERJA = :unit)
                AND (:aktif IS NULL OR AKTIF = :aktif)
                AND (:anggota IS NULL OR ANGGOTA = :anggota)
                AND (:status IS NULL OR STATUS = :status)
                ORDER BY NAMA_PELANGGAN, ID_PELANGGAN
                OFFSET :offset ROWS FETCH NEXT 26 ROWS ONLY"
                    ),
                    &[
                        ("umum", &umum), ("q", &q),
                        ("unit", &f.unit),
                        ("aktif", &f.aktif),
                        ("anggota", &f.anggota),
                        ("status", &f.status),
                        ("offset", &offset),
                    ],
                )?
                .collect::<Result<Vec<_>, _>>()?;
            let lanjut = data.len() > UKURAN;
            data.truncate(UKURAN);
            let audit_tmk = riwayat && super::riwayat_anggota::audit_tmk_tersedia(c)?;
            Ok(Halaman { data, lanjut, registrasi, riwayat, audit_tmk })
        })
        .await?,
    ))
}

fn profil(c: &Connection, id: i64, umum: &str, kunci: bool) -> AppResult<Profil> {
    let sql = if kunci {
        "SELECT NAMA_PELANGGAN AS NAMA, ALAMAT, KOTA, KODE_POS, NO_TELP AS TELEPON,
         NVL(LIMIT_HUTANG, 0) AS LIMIT_HUTANG FROM FIN_ANGGOTA
         WHERE ID_PELANGGAN = :1 AND (NIK IS NULL OR NIK <> :2) FOR UPDATE"
    } else {
        "SELECT NAMA_PELANGGAN AS NAMA, ALAMAT, KOTA, KODE_POS, NO_TELP AS TELEPON,
         NVL(LIMIT_HUTANG, 0) AS LIMIT_HUTANG FROM FIN_ANGGOTA
         WHERE ID_PELANGGAN = :1 AND (NIK IS NULL OR NIK <> :2)"
    };
    Ok(c.query_as::<Profil>(sql, &[&id, &umum])?
        .next()
        .transpose()?
        .ok_or_else(|| AppError::NotFound("Data anggota tidak ditemukan".into()))?)
}

pub async fn lihat(
    State(s): State<AppState>,
    a: Auth,
    Path(id): Path<i64>,
) -> AppResult<Json<Detail>> {
    a.peran(&[Role::Admin])?;
    let umum = s.cfg.default_nik.clone();
    Ok(Json(
        db::run(&s.pool, move |c| {
            let anggota = c
                .query_as::<Ringkas>(
                    &format!("SELECT {KOLOM}, 0 AS DAPAT_GABUNG FROM FIN_ANGGOTA WHERE ID_PELANGGAN = :1 AND (NIK IS NULL OR NIK <> :2)"),
                    &[&id, &umum],
                )?
                .next()
                .transpose()?
                .ok_or_else(|| AppError::NotFound("Data anggota tidak ditemukan".into()))?;
            Ok(Detail {
                anggota,
                profil: profil(c, id, &umum, false)?,
            })
        })
        .await?,
    ))
}

pub async fn ubah(
    State(s): State<AppState>,
    a: Auth,
    Path(id): Path<i64>,
    Json(mut edit): Json<Edit>,
) -> AppResult<Json<Profil>> {
    let admin = a.peran(&[Role::Admin])?.clone();
    if edit.mutasi.is_some() && !super::riwayat_anggota::aktif() {
        return Err(AppError::Forbidden(
            "Riwayat anggota belum diaktifkan".into(),
        ));
    }
    for (nilai, batas, label) in [
        (&mut edit.profil.nama, 50, "Nama"),
        (&mut edit.profil.alamat, 100, "Alamat"),
        (&mut edit.profil.kota, 50, "Kota"),
        (&mut edit.profil.kode_pos, 10, "Kode pos"),
        (&mut edit.profil.telepon, 20, "Telepon"),
    ] {
        *nilai = nilai
            .take()
            .map(|v| v.trim().to_owned())
            .filter(|v| !v.is_empty());
        if nilai
            .as_ref()
            .is_some_and(|v| v.len() > batas || v.chars().any(char::is_control))
        {
            return Err(AppError::validation(format!(
                "{label} tidak valid atau melebihi {batas} byte"
            )));
        }
    }
    if edit.profil.nama.is_none() {
        return Err(AppError::validation("Nama wajib diisi"));
    }
    let limit = edit.profil.limit_hutang;
    if limit.is_some_and(|nilai| !(0..=LIMIT_HUTANG_MAX).contains(&nilai)) {
        return Err(AppError::validation(
            "Limit hutang harus berupa rupiah bulat dan tidak negatif",
        ));
    }
    if limit.is_some() && edit.awal.limit_hutang.is_none() {
        return Err(AppError::validation(
            "Muat ulang detail sebelum mengubah limit hutang",
        ));
    }
    let umum = s.cfg.default_nik.clone();
    Ok(Json(
        db::run(&s.pool, move |c| {
            super::riwayat_anggota::transaksi(c, || {
                let saat_ini = profil(c, id, &umum, true)?;
                if edit.awal.limit_hutang.is_none() {
                    edit.awal.limit_hutang = saat_ini.limit_hutang;
                }
                if saat_ini != edit.awal {
                    return Err(AppError::Conflict(
                        "Profil telah berubah. Buka kembali detail sebelum mengedit.".into(),
                    ));
                }
                let mut p = edit.profil;
                c.execute_named(
                    "UPDATE FIN_ANGGOTA SET NAMA_PELANGGAN = :nama, ALAMAT = :alamat,
             KOTA = :kota, KODE_POS = :kode, NO_TELP = :tel,
             LIMIT_HUTANG = COALESCE(:limit_hutang, LIMIT_HUTANG)
             WHERE ID_PELANGGAN = :id",
                    &[
                        ("nama", &p.nama),
                        ("alamat", &p.alamat),
                        ("kota", &p.kota),
                        ("kode", &p.kode_pos),
                        ("tel", &p.telepon),
                        ("limit_hutang", &limit),
                        ("id", &id),
                    ],
                )?;
                if let Some(m) = edit.mutasi {
                    super::riwayat_anggota::terapkan(
                        c,
                        id,
                        &umum,
                        &admin,
                        super::riwayat_anggota::Perubahan::Mutasi {
                            awal: m.awal,
                            unit: m.unit,
                            status: m.status,
                            alasan: m.alasan,
                        },
                    )?;
                }
                p.limit_hutang = limit.or(saat_ini.limit_hutang);
                Ok(p)
            })
        })
        .await?,
    ))
}

#[derive(Deserialize)]
#[serde(deny_unknown_fields)]
pub struct PerubahanAktif {
    aktif: bool,
    alasan: Option<String>,
}

pub async fn ubah_aktif(
    State(s): State<AppState>,
    a: Auth,
    Path(id): Path<i64>,
    Json(perubahan): Json<PerubahanAktif>,
) -> AppResult<Json<serde_json::Value>> {
    let admin = a.peran(&[Role::Admin])?.clone();
    let audit = super::riwayat_anggota::aktif();
    let alasan = if audit {
        Some(
            super::riwayat_anggota::alasan_valid(perubahan.alasan.as_deref().unwrap_or_default())?
                .to_owned(),
        )
    } else {
        None
    };
    let umum = s.cfg.default_nik.clone();
    db::run(&s.pool, move |c| {
        super::riwayat_anggota::transaksi(c, || {
            let awal = alasan
                .as_ref()
                .map(|_| super::riwayat_anggota::anggota(c, id, &umum))
                .transpose()?;
            let (tujuan, sebelumnya) = if perubahan.aktif {
                ("Y", "T")
            } else {
                ("T", "Y")
            };
            let stmt = c.execute_named(
                "UPDATE FIN_ANGGOTA SET AKTIF = :tujuan,
             TGLNONAKTIF = CASE WHEN :tujuan = 'T'
               THEN TRUNC(CAST(SYSTIMESTAMP AT TIME ZONE 'Asia/Jakarta' AS DATE))
               ELSE NULL END
             WHERE ID_PELANGGAN = :id AND ANGGOTA = 'Y'
               AND AKTIF = :sebelumnya AND (NIK IS NULL OR NIK <> :umum)",
                &[
                    ("tujuan", &tujuan),
                    ("id", &id),
                    ("sebelumnya", &sebelumnya),
                    ("umum", &umum),
                ],
            )?;
            if stmt.row_count()? != 1 {
                return Err(AppError::Conflict(
                    "Status anggota sudah berubah atau tidak dapat diubah. Muat ulang detail."
                        .into(),
                ));
            }
            if let (Some(awal), Some(alasan)) = (&awal, &alasan) {
                let akhir = super::riwayat_anggota::anggota(c, id, &umum)?;
                let jenis = if perubahan.aktif { "AKTIF" } else { "NONAKTIF" };
                super::riwayat_anggota::catat(c, &admin, jenis, alasan, Some(awal), &akhir)?;
            }
            Ok(())
        })
    })
    .await?;
    Ok(Json(serde_json::json!({ "aktif": perubahan.aktif })))
}

#[derive(Default, Deserialize)]
#[serde(default, deny_unknown_fields)]
pub struct Registrasi {
    nama: String,
    unit: String,
    status: String,
    tmk: String,
    limit_hutang: Option<serde_json::Value>,
}

impl Registrasi {
    fn validasi(&self, hari: chrono::NaiveDate) -> AppResult<(&str, &str, chrono::NaiveDate, i64)> {
        let nama = self.nama.trim();
        let unit = self.unit.trim();
        if nama.is_empty() || nama.len() > 50 || self.nama.chars().any(char::is_control) {
            return Err(AppError::rule(
                "ANGGOTA_NAMA",
                "Nama wajib diisi, maksimal 50 byte, dan tidak boleh mengandung karakter kontrol.",
            ));
        }
        if unit.is_empty() || unit.len() > 20 {
            return Err(AppError::rule(
                "ANGGOTA_UNIT",
                "Pilih unit kerja yang tersedia.",
            ));
        }
        if !matches!(self.status.as_str(), "BULANAN" | "KHT") {
            return Err(AppError::rule(
                "ANGGOTA_STATUS",
                "Pilih kategori tagihan BULANAN atau KHT.",
            ));
        }
        let tmk = chrono::NaiveDate::parse_from_str(&self.tmk, "%Y-%m-%d")
            .ok()
            .filter(|tgl| tgl.year() >= 1 && *tgl <= hari && tgl.to_string() == self.tmk)
            .ok_or_else(|| {
                AppError::rule(
                    "ANGGOTA_TMK",
                    "Tanggal masuk wajib valid dan paling lambat hari ini (WIB).",
                )
            })?;
        let limit = self
            .limit_hutang
            .as_ref()
            .and_then(serde_json::Value::as_i64)
            .filter(|nilai| (0..=LIMIT_HUTANG_MAX).contains(nilai))
            .ok_or_else(|| {
                AppError::rule(
                    "ANGGOTA_LIMIT",
                    "Isi limit dengan rupiah bulat dari 0 hingga 9.007.199.254.740.991.",
                )
            })?;
        Ok((nama, unit, tmk, limit))
    }
}

#[derive(Serialize)]
pub struct Terdaftar {
    nik: String,
}

fn nomor_berikutnya(awalan: &str, counter: Option<&str>, terakhir: i64) -> AppResult<String> {
    let urut = counter
        .and_then(|nomor| nomor.strip_prefix(awalan))
        .map(str::parse::<i64>)
        .transpose()
        .map_err(|_| AppError::Conflict("Counter NIK tahun berjalan tidak valid".into()))?
        .unwrap_or(0);
    let dasar = urut.max(terakhir);
    if !(0..999).contains(&dasar) {
        return Err(AppError::Conflict(
            "Nomor anggota tahun ini sudah mencapai batas".into(),
        ));
    }
    Ok(format!("{awalan}{:03}", dasar + 1))
}

fn galat_nomor(e: oracle::Error) -> AppError {
    match e.oci_code() {
        Some(1 | 54 | 30006) => AppError::rule(
            "ANGGOTA_NOMOR",
            "Nomor anggota sedang digunakan. Coba daftarkan kembali.",
        ),
        _ => e.into(),
    }
}

pub async fn daftar_baru(
    State(s): State<AppState>,
    a: Auth,
    Json(data): Json<Registrasi>,
) -> AppResult<(axum::http::StatusCode, Json<Terdaftar>)> {
    let admin = a.peran(&[Role::Admin])?.clone();
    if !s.cfg.registrasi_anggota_web {
        return Err(AppError::Forbidden(
            "Registrasi anggota melalui web belum diaktifkan".into(),
        ));
    }

    let hari = (chrono::Utc::now() + chrono::Duration::hours(7)).date_naive();
    let (nama, unit, tmk, limit) = data.validasi(hari)?;
    let nama = nama.to_owned();
    let unit = unit.to_owned();
    let awalan = format!("A-{}-", hari.format("%y"));
    let umum = s.cfg.default_nik.clone();
    let nik = db::run(&s.pool, move |c| super::riwayat_anggota::transaksi(c, || {
        let kunci = c.query_as::<String>(
            "SELECT KODE FROM NOMOR_TRANSAKSI WHERE KODE = 'ANGGOTA' FOR UPDATE WAIT 5", &[],
        ).map_err(galat_nomor)?.collect::<Result<Vec<_>, _>>().map_err(galat_nomor)?;
        if kunci.len() != 1 {
            return Err(AppError::rule("ANGGOTA_NOMOR", "Counter NIK belum siap. Registrasi belum disimpan."));
        }
        let unit_valid = c.query_as::<String>(
            "SELECT KODE FROM FIN_UNITKERJA WHERE KODE = :1 FOR UPDATE WAIT 5", &[&unit],
        )?.next().transpose()?;
        if unit_valid.is_none() {
            return Err(AppError::rule("ANGGOTA_UNIT", "Unit kerja tidak lagi tersedia. Pilih unit lain."));
        }
        let duplikat: i64 = c.query_row_as(
            "SELECT COUNT(*) FROM FIN_ANGGOTA
             WHERE NLS_UPPER(TRIM(NAMA_PELANGGAN)) = NLS_UPPER(:1) AND UNIT_KERJA = :2",
            &[&nama, &unit],
        )?;
        if duplikat > 0 {
            return Err(AppError::rule("ANGGOTA_DUPLIKAT", "Nama dan unit kerja sudah terdaftar. Periksa daftar anggota."));
        }
        let counter: Option<String> = c.query_row_as(
            "SELECT NOMOR FROM NOMOR_TRANSAKSI WHERE KODE = 'ANGGOTA'", &[],
        )?;
        let pola = format!("^{awalan}[0-9]+$");
        let terakhir: i64 = c.query_row_as(
            "SELECT NVL(MAX(TO_NUMBER(SUBSTR(NIK, 6))), 0) FROM FIN_ANGGOTA
             WHERE REGEXP_LIKE(NIK, :1, 'c')", &[&pola],
        )?;
        let nik = nomor_berikutnya(&awalan, counter.as_deref(), terakhir)
            .map_err(|e| AppError::rule("ANGGOTA_NOMOR", e.to_string()))?;
        let tmk = tmk.to_string();
        let updateat = hari.format("%d-%m-%Y").to_string();
        c.execute_named(
            "INSERT INTO FIN_ANGGOTA
               (NIK, NAMA_PELANGGAN, UNIT_KERJA, STATUS, TMK, LIMIT_HUTANG, AKTIF, ANGGOTA, UPDATEAT)
             VALUES (:nik, :nama, :unit, :status, TO_DATE(:tmk, 'YYYY-MM-DD'), :limit, 'Y', 'Y', :updateat)",
            &[("nik", &nik), ("nama", &nama), ("unit", &unit), ("status", &data.status),
              ("tmk", &tmk), ("limit", &limit), ("updateat", &updateat)],
        ).map_err(galat_nomor)?;
        c.execute("UPDATE NOMOR_TRANSAKSI SET NOMOR = :1 WHERE KODE = 'ANGGOTA'", &[&nik])?;
        if super::riwayat_anggota::aktif() {
            let id: i64 = c.query_row_as("SELECT ID_PELANGGAN FROM FIN_ANGGOTA WHERE NIK = :1", &[&nik])?;
            let akhir = super::riwayat_anggota::anggota(c, id, &umum)?;
            super::riwayat_anggota::catat(c, &admin, "MASUK", "Registrasi anggota melalui web", None, &akhir)?;
        }
        Ok(nik)
    })).await?;
    Ok((axum::http::StatusCode::CREATED, Json(Terdaftar { nik })))
}

#[cfg(test)]
mod tes_registrasi {
    use super::{AppError, Profil, Registrasi, nomor_berikutnya};
    use serde_json::json;

    #[test]
    fn registrasi_menerima_nol_dan_menolak_isian_tidak_valid() {
        let hari = chrono::NaiveDate::from_ymd_opt(2026, 11, 21).unwrap();
        let benar = json!({
            "nama": " Anggota Uji ", "unit": "01", "status": "KHT",
            "tmk": "2026-11-21", "limit_hutang": 0
        });
        let data: Registrasi = serde_json::from_value(benar.clone()).unwrap();
        assert_eq!(data.validasi(hari).unwrap(), ("Anggota Uji", "01", hari, 0));
        for (field, nilai, kode) in [
            ("nama", json!("  "), "ANGGOTA_NAMA"),
            ("nama", json!("é".repeat(26)), "ANGGOTA_NAMA"),
            ("nama", json!("Uji\n"), "ANGGOTA_NAMA"),
            ("unit", json!(""), "ANGGOTA_UNIT"),
            ("status", json!("LAIN"), "ANGGOTA_STATUS"),
            ("tmk", json!("2026-11-22"), "ANGGOTA_TMK"),
            ("tmk", json!("2026-02-29"), "ANGGOTA_TMK"),
            ("limit_hutang", json!(null), "ANGGOTA_LIMIT"),
            ("limit_hutang", json!(-1), "ANGGOTA_LIMIT"),
            ("limit_hutang", json!(1.5), "ANGGOTA_LIMIT"),
            (
                "limit_hutang",
                json!(9_007_199_254_740_992_i64),
                "ANGGOTA_LIMIT",
            ),
        ] {
            let mut salah = benar.clone();
            salah[field] = nilai;
            let data: Registrasi = serde_json::from_value(salah).unwrap();
            assert!(
                matches!(data.validasi(hari), Err(AppError::Rule { kode: aktual, .. }) if aktual == kode),
                "{field}"
            );
        }
        let mut kosong = benar;
        kosong.as_object_mut().unwrap().remove("limit_hutang");
        let data: Registrasi = serde_json::from_value(kosong).unwrap();
        assert!(matches!(
            data.validasi(hari),
            Err(AppError::Rule {
                kode: "ANGGOTA_LIMIT",
                ..
            })
        ));
    }

    #[test]
    fn limit_profil_membedakan_nol_dan_field_yang_tidak_dikirim() {
        let lama: Profil = serde_json::from_str(r#"{"nama":"Anggota"}"#).unwrap();
        assert_eq!(lama.limit_hutang, None);
        let nol: Profil = serde_json::from_str(r#"{"nama":"Anggota","limit_hutang":0}"#).unwrap();
        assert_eq!(nol.limit_hutang, Some(0));
        for salah in ["null", "1.5", "\"\""] {
            let json = format!(r#"{{"nama":"Anggota","limit_hutang":{salah}}}"#);
            assert!(serde_json::from_str::<Profil>(&json).is_err());
        }
    }

    #[test]
    fn mempertahankan_format_dan_mengikuti_nomor_terbesar() {
        assert_eq!(
            nomor_berikutnya("A-26-", Some("A-25-990"), 0).unwrap(),
            "A-26-001"
        );
        assert_eq!(
            nomor_berikutnya("A-26-", Some("A-26-003"), 7).unwrap(),
            "A-26-008"
        );
        assert_eq!(
            nomor_berikutnya("A-26-", Some("A-26-008"), 7).unwrap(),
            "A-26-009"
        );
        assert!(nomor_berikutnya("A-26-", None, 999).is_err());
        assert!(nomor_berikutnya("A-26-", Some("A-26-rusak"), 0).is_err());
    }
}
