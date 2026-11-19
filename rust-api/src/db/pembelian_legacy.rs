use chrono::NaiveDate;
use oracle::Connection;
use rust_decimal::{Decimal, prelude::ToPrimitive};
use serde::{Deserialize, Serialize};
use serde_json::{Value, json};

use super::{
    Alasan, MAKS_RUPIAH, Metode, Operator, dec, faktur, opt, referensi_bayar, tm, tulis, wajib,
};
use crate::{
    auth::Role,
    error::{AppError, AppResult},
};

const GUARDS: [&str; 2] = ["MOB_BELI_LAMA_HEADER_GUARD", "MOB_BELI_LAMA_DETAIL_GUARD"];

pub fn cek_guard(c: &Connection) -> AppResult<()> {
    let n: i64 = c.query_row_as(
        "SELECT COUNT(*) FROM USER_TRIGGERS t JOIN USER_OBJECTS o
         ON o.OBJECT_NAME=t.TRIGGER_NAME AND o.OBJECT_TYPE='TRIGGER'
         WHERE t.TRIGGER_NAME IN ('MOB_BELI_LAMA_HEADER_GUARD','MOB_BELI_LAMA_DETAIL_GUARD')
           AND t.STATUS='ENABLED' AND o.STATUS='VALID'",
        &[],
    )?;
    if n != 2 {
        return Err(AppError::Internal(
            "Proteksi faktur Backoffice belum siap".into(),
        ));
    }
    Ok(())
}

pub(super) fn migrate(c: &Connection) -> AppResult<usize> {
    let mut n = 0;
    for ddl in include_str!("pembelian_legacy.sql")
        .replace("\r\n", "\n")
        .split("\n/\n")
    {
        match c.execute(ddl.trim(), &[]) {
            Ok(_) => n += 1,
            Err(e) if matches!(e.oci_code(), Some(955 | 1408)) => {}
            Err(e) => return Err(e.into()),
        }
    }
    for (ddl, nama) in include_str!("pembelian_legacy_guard.sql")
        .replace("\r\n", "\n")
        .split("\n/\n")
        .zip(GUARDS)
    {
        let valid: i64 = c.query_row_as_named(
            "SELECT COUNT(*) FROM USER_TRIGGERS t JOIN USER_OBJECTS o
             ON o.OBJECT_NAME=t.TRIGGER_NAME AND o.OBJECT_TYPE='TRIGGER'
             WHERE t.TRIGGER_NAME=:nama AND t.STATUS='ENABLED' AND o.STATUS='VALID'",
            &[("nama", &nama)],
        )?;
        if valid == 0 {
            let dibuat = c.execute(ddl.trim(), &[]);
            let gagal: i64 = c.query_row_as_named(
                "SELECT COUNT(*) FROM USER_OBJECTS WHERE OBJECT_NAME=:nama AND OBJECT_TYPE='TRIGGER' AND STATUS='INVALID'",
                &[("nama", &nama)],
            )?;
            if gagal != 0 {
                let dilindungi: i64 = c.query_row_as("SELECT COUNT(*) FROM MOB_BELI_LAMA", &[])?;
                if dilindungi == 0 {
                    c.execute(&format!("ALTER TRIGGER {nama} DISABLE"), &[])?;
                }
                return Err(AppError::Internal(format!(
                    "Trigger {nama} tidak valid; periksa USER_ERRORS"
                )));
            }
            dibuat?;
            n += 1;
        }
    }
    cek_guard(c)?;
    Ok(n)
}

#[derive(Serialize, Deserialize)]
#[serde(deny_unknown_fields)]
pub struct Awal {
    pub purchase_id: String,
    pub saldo_awal: String,
    pub tanggal_rekonsiliasi: NaiveDate,
    pub jatuh_tempo: Option<NaiveDate>,
    pub referensi: String,
}

#[derive(Serialize, Deserialize)]
#[serde(deny_unknown_fields)]
pub struct Koreksi {
    pub revisi: i64,
    pub saldo_awal: String,
    pub jatuh_tempo: Option<NaiveDate>,
    pub referensi: String,
    pub alasan: String,
}

#[derive(Serialize, Deserialize)]
pub struct BayarLama {
    pub tanggal: NaiveDate,
    pub metode: Metode,
    pub referensi: Option<String>,
    pub nominal: String,
}

fn admin(u: &Operator) -> AppResult<()> {
    if u.role != Role::Admin {
        return Err(AppError::Forbidden(
            "Rekonsiliasi hanya dapat dilakukan Admin".into(),
        ));
    }
    Ok(())
}

fn dalam_sen(teks: &str) -> AppResult<i64> {
    let (bulat, pecahan) = teks.split_once('.').unwrap_or((teks, ""));
    if bulat.is_empty()
        || bulat.len() > 16
        || pecahan.len() > 2
        || (teks.contains('.') && pecahan.is_empty())
        || !bulat
            .bytes()
            .chain(pecahan.bytes())
            .all(|b| b.is_ascii_digit())
    {
        return Err(AppError::validation(
            "Nominal harus berupa desimal nonnegatif dengan maksimal dua angka pecahan",
        ));
    }
    let nilai = dec(teks)?;
    if nilai > Decimal::from(MAKS_RUPIAH) {
        return Err(AppError::validation("Nominal melampaui batas"));
    }
    (nilai * Decimal::from(100))
        .to_i64()
        .ok_or_else(|| AppError::validation("Nominal melampaui batas"))
}

fn nominal(sen: i64) -> String {
    Decimal::new(sen, 2).normalize().to_string()
}

struct Saldo {
    revisi: i64,
    tanggal: NaiveDate,
    tanggal_faktur: NaiveDate,
    total: Decimal,
    awal: i64,
    dibayar: i64,
}

fn kunci_saldo(c: &Connection, no: &str) -> AppResult<Saldo> {
    opt(c.query_row_named(
        "SELECT NO_TRANSAKSI FROM MOB_BELI_LAMA WHERE NO_TRANSAKSI=:no FOR UPDATE",
        &[("no", &no)],
    ))?
    .ok_or_else(|| AppError::NotFound("Faktur Backoffice belum direkonsiliasi".into()))?;
    let baris = c.query_row_named(&format!(
        "SELECT l.REVISI,l.TANGGAL_REKONSILIASI,TRUNC(b.TANGGAL),{},s.SEN,
            (SELECT NVL(SUM(p.SEN),0) FROM MOB_BELI_LAMA_BAYAR p WHERE p.NO_TRANSAKSI=l.NO_TRANSAKSI)
         FROM MOB_BELI_LAMA l JOIN POS_PEMBELIAN b ON b.PURCHASE_ID=l.PURCHASE_ID
         JOIN MOB_BELI_LAMA_SALDO s ON s.NO_TRANSAKSI=l.NO_TRANSAKSI AND s.REVISI=l.REVISI
         WHERE l.NO_TRANSAKSI=:no", tm("b.TOTAL")),
        &[("no", &no)],
    )?;
    Ok(Saldo {
        revisi: baris.get(0)?,
        tanggal: baris.get(1)?,
        tanggal_faktur: baris.get(2)?,
        total: dec(&baris.get::<_, String>(3)?)?,
        awal: baris.get(4)?,
        dibayar: baris.get(5)?,
    })
}

fn validasi_saldo(sen: i64, total: Decimal, dibayar: i64) -> AppResult<()> {
    if Decimal::new(sen, 2) > total {
        return Err(AppError::validation(
            "Saldo awal tidak boleh melebihi total faktur",
        ));
    }
    if sen < dibayar {
        return Err(AppError::rule(
            "SALDO_BERUBAH",
            "Saldo awal tidak boleh kurang dari pembayaran bersih",
        ));
    }
    Ok(())
}

fn validasi_tempo(tanggal_faktur: NaiveDate, tempo: Option<NaiveDate>) -> AppResult<()> {
    if tempo.is_some_and(|t| t < tanggal_faktur) {
        return Err(AppError::validation(
            "Jatuh tempo tidak boleh sebelum tanggal faktur",
        ));
    }
    Ok(())
}

struct Revisi<'a> {
    no: &'a str,
    versi: i64,
    sen: i64,
    jatuh_tempo: Option<NaiveDate>,
    referensi: &'a str,
    alasan: Option<&'a str>,
}

fn simpan_revisi(c: &Connection, u: &Operator, r: Revisi<'_>) -> AppResult<String> {
    c.execute_named(
        "INSERT INTO MOB_BELI_LAMA_SALDO
         (NO_TRANSAKSI,REVISI,SEN,JATUH_TEMPO,REFERENSI,ALASAN,USER_ID,USER_NAMA)
         VALUES (:no,:rev,:sen,:tempo,:ref_bukti,:alasan,:operator_id,:nama)",
        &[
            ("no", &r.no),
            ("rev", &r.versi),
            ("sen", &r.sen),
            ("tempo", &r.jatuh_tempo),
            ("ref_bukti", &r.referensi),
            ("alasan", &r.alasan),
            ("operator_id", &u.id),
            ("nama", &u.nama),
        ],
    )?;
    Ok(r.versi.to_string())
}

pub fn tetapkan(
    c: &Connection,
    u: &Operator,
    no: &str,
    key: &str,
    req: &Awal,
) -> AppResult<String> {
    admin(u)?;
    tulis(c, u, key, "BL_AWAL", &(no, req), || {
        cek_guard(c)?;
        let sen = dalam_sen(&req.saldo_awal)?;
        let referensi = wajib(&req.referensi, 250, "Referensi bukti")?;
        // DML Backoffice dapat mulai dari detail sebelum menyentuh header.
        // NOWAIT menjaga kedua tabel tetap konsisten saat identitas faktur didaftarkan.
        for sql in [
            "LOCK TABLE POS_PEMBELIAN IN SHARE ROW EXCLUSIVE MODE NOWAIT",
            "LOCK TABLE POS_PEMBELIANDETAIL IN SHARE ROW EXCLUSIVE MODE NOWAIT",
        ] {
            c.execute(sql, &[]).map_err(|e| {
                if e.oci_code() == Some(54) {
                    AppError::Conflict(
                        "Pembelian sedang diperbarui. Coba lagi setelah perubahan selesai.".into(),
                    )
                } else {
                    e.into()
                }
            })?;
        }
        let r = opt(c.query_row_named(&format!(
            "SELECT {},TRUNC(b.TANGGAL),{},b.SUPPLIER_ID
             FROM POS_PEMBELIAN b WHERE b.NO_TRANSAKSI=:no AND b.TANGGAL IS NOT NULL
             AND NOT EXISTS (SELECT 1 FROM MOB_BELI w WHERE w.PURCHASE_ID=b.PURCHASE_ID OR w.NO_TRANSAKSI=b.NO_TRANSAKSI)",
            tm("b.PURCHASE_ID"), tm("b.TOTAL")), &[("no", &no)]))?
            .ok_or_else(|| AppError::NotFound("Faktur Backoffice tidak ditemukan".into()))?;
        if r.get::<_, String>(0)? != req.purchase_id {
            return Err(AppError::Conflict(
                "Faktur Backoffice telah berubah. Buka ulang detail faktur.".into(),
            ));
        }
        let total = r
            .get::<_, Option<String>>(2)?
            .ok_or_else(|| AppError::validation("Total faktur belum tersedia"))?;
        if r.get::<_, Option<String>>(3)?.is_none() {
            return Err(AppError::validation("Faktur belum memiliki supplier"));
        }
        let tanggal_faktur = r.get(1)?;
        if !(tanggal_faktur..=faktur::sekarang(c)?.0).contains(&req.tanggal_rekonsiliasi) {
            return Err(AppError::validation(
                "Tanggal rekonsiliasi harus di antara tanggal faktur dan hari ini",
            ));
        }
        validasi_tempo(tanggal_faktur, req.jatuh_tempo)?;
        validasi_saldo(sen, dec(&total)?, 0)?;
        c.execute_named(
            "INSERT INTO MOB_BELI_LAMA (NO_TRANSAKSI,PURCHASE_ID,TANGGAL_REKONSILIASI)
             SELECT NO_TRANSAKSI,PURCHASE_ID,:tgl FROM POS_PEMBELIAN WHERE NO_TRANSAKSI=:no",
            &[("no", &no), ("tgl", &req.tanggal_rekonsiliasi)],
        )
        .map_err(|e| {
            if e.oci_code() == Some(1) {
                AppError::Conflict("Saldo faktur sudah terverifikasi".into())
            } else {
                e.into()
            }
        })?;
        simpan_revisi(
            c,
            u,
            Revisi {
                no,
                versi: 1,
                sen,
                jatuh_tempo: req.jatuh_tempo,
                referensi: &referensi,
                alasan: None,
            },
        )
    })
}

pub fn koreksi(
    c: &Connection,
    u: &Operator,
    no: &str,
    key: &str,
    req: &Koreksi,
) -> AppResult<String> {
    admin(u)?;
    tulis(c, u, key, "BL_KOREK", &(no, req), || {
        let f = kunci_saldo(c, no)?;
        if f.revisi != req.revisi {
            return Err(AppError::Conflict(
                "Revisi saldo telah berubah. Buka ulang detail dan periksa saldo terbaru.".into(),
            ));
        }
        let referensi = wajib(&req.referensi, 250, "Referensi bukti")?;
        let alasan = wajib(&req.alasan, 250, "Alasan koreksi")?;
        let sen = dalam_sen(&req.saldo_awal)?;
        validasi_saldo(sen, f.total, f.dibayar)?;
        validasi_tempo(f.tanggal_faktur, req.jatuh_tempo)?;
        let versi = f.revisi + 1;
        let hasil = simpan_revisi(
            c,
            u,
            Revisi {
                no,
                versi,
                sen,
                jatuh_tempo: req.jatuh_tempo,
                referensi: &referensi,
                alasan: Some(&alasan),
            },
        )?;
        c.execute_named(
            "UPDATE MOB_BELI_LAMA SET REVISI=:rev WHERE NO_TRANSAKSI=:no",
            &[("rev", &versi), ("no", &no)],
        )?;
        Ok(hasil)
    })
}

fn entri(
    c: &Connection,
    u: &Operator,
    no: &str,
    p: &BayarLama,
    sen: i64,
    balik: Option<(i64, &str)>,
) -> AppResult<String> {
    let referensi = referensi_bayar(p.referensi.as_deref())?;
    let metode = p.metode.kode();
    let balik_id = balik.map(|b| b.0);
    let alasan = balik.map(|b| b.1);
    let stmt = c.execute_named(
        "INSERT INTO MOB_BELI_LAMA_BAYAR
         (NO_TRANSAKSI,TANGGAL,METODE,REFERENSI,SEN,BALIK_ID,ALASAN,USER_ID,USER_NAMA)
         VALUES (:no,:tgl,:metode,:ref_bukti,:sen,:balik,:alasan,:operator_id,:nama) RETURNING ID INTO :id",
        &[("no", &no), ("tgl", &p.tanggal), ("metode", &metode), ("ref_bukti", &referensi),
          ("sen", &sen), ("balik", &balik_id), ("alasan", &alasan), ("operator_id", &u.id),
          ("nama", &u.nama), ("id", &None::<i64>)],
    )?;
    let id: i64 = stmt
        .returned_values("id")?
        .into_iter()
        .next()
        .ok_or_else(|| AppError::Internal("Oracle tidak mengembalikan ID pembayaran".into()))?;
    Ok(id.to_string())
}

pub fn bayar(
    c: &Connection,
    u: &Operator,
    no: &str,
    key: &str,
    p: &BayarLama,
) -> AppResult<String> {
    admin(u)?;
    tulis(c, u, key, "BL_BAYAR", &(no, p), || {
        let f = kunci_saldo(c, no)?;
        let sen = dalam_sen(&p.nominal)?;
        if !(f.tanggal..=faktur::sekarang(c)?.0).contains(&p.tanggal) {
            return Err(AppError::validation(
                "Tanggal pembayaran harus di antara tanggal rekonsiliasi dan hari ini",
            ));
        }
        if sen <= 0 || sen > f.awal - f.dibayar {
            return Err(AppError::rule(
                "SALDO_BERUBAH",
                "Nominal pembayaran harus positif dan tidak boleh melebihi sisa hutang",
            ));
        }
        entri(c, u, no, p, sen, None)
    })
}

pub fn dilacak(c: &Connection, no: &str) -> AppResult<bool> {
    let n: i64 = c.query_row_as_named(
        "SELECT COUNT(*) FROM MOB_BELI_LAMA WHERE NO_TRANSAKSI=:no",
        &[("no", &no)],
    )?;
    Ok(n != 0)
}

pub fn balik(
    c: &Connection,
    u: &Operator,
    no: &str,
    id: i64,
    key: &str,
    req: &Alasan,
) -> AppResult<String> {
    admin(u)?;
    tulis(c, u, key, "BL_BALIK", &(no, id, req), || {
        kunci_saldo(c, no)?;
        let alasan = wajib(&req.alasan, 250, "Alasan pembalikan")?;
        let r = opt(c.query_row_named(
            "SELECT p.SEN,p.METODE,p.REFERENSI,(SELECT COUNT(*) FROM MOB_BELI_LAMA_BAYAR x WHERE x.BALIK_ID=p.ID)
             FROM MOB_BELI_LAMA_BAYAR p WHERE p.ID=:id AND p.NO_TRANSAKSI=:no AND p.BALIK_ID IS NULL",
            &[("no", &no), ("id", &id)],
        ))?.ok_or_else(|| AppError::NotFound("Pembayaran tidak ditemukan".into()))?;
        if r.get::<_, i64>(3)? != 0 {
            return Err(AppError::Conflict("Pembayaran sudah dibalik".into()));
        }
        let metode = match r.get::<_, String>(1)?.as_str() {
            "TUNAI" => Metode::Tunai,
            "TRANSFER" => Metode::Transfer,
            _ => Metode::Lainnya,
        };
        let p = BayarLama {
            tanggal: faktur::sekarang(c)?.0,
            metode,
            referensi: r.get(2)?,
            nominal: String::new(),
        };
        entri(c, u, no, &p, -r.get::<_, i64>(0)?, Some((id, &alasan)))
    })
}

pub(super) fn revisi(c: &Connection, no: &str, hingga: i64) -> AppResult<Vec<Value>> {
    c.query_named(
        "SELECT REVISI,SEN,JATUH_TEMPO,REFERENSI,ALASAN,USER_NAMA,TO_CHAR(CREATED_AT,'YYYY-MM-DD HH24:MI:SS')
         FROM MOB_BELI_LAMA_SALDO WHERE NO_TRANSAKSI=:no AND REVISI<=:rev ORDER BY REVISI DESC",
        &[("no", &no), ("rev", &hingga)],
    )?.map(|r| {
        let r = r?;
        Ok(json!({
            "revisi":r.get::<_, i64>(0)?, "saldo_awal":nominal(r.get(1)?),
            "jatuh_tempo":r.get::<_, Option<NaiveDate>>(2)?, "referensi":r.get::<_, String>(3)?,
            "alasan":r.get::<_, Option<String>>(4)?, "operator":r.get::<_, String>(5)?,
            "created_at":r.get::<_, String>(6)?,
        }))
    }).collect()
}

pub(super) fn pembayaran(c: &Connection, no: &str, hingga: i64) -> AppResult<Vec<Value>> {
    c.query_named(
        "SELECT p.ID,p.TANGGAL,p.METODE,p.REFERENSI,p.SEN,p.BALIK_ID,p.ALASAN,p.USER_NAMA,
                TO_CHAR(p.CREATED_AT,'YYYY-MM-DD HH24:MI:SS'),CASE WHEN r.ID IS NULL THEN 0 ELSE 1 END
         FROM MOB_BELI_LAMA_BAYAR p LEFT JOIN MOB_BELI_LAMA_BAYAR r ON r.BALIK_ID=p.ID AND r.ID<=:batas
         WHERE p.NO_TRANSAKSI=:no AND p.ID<=:batas ORDER BY p.ID", &[("no", &no), ("batas", &hingga)],
    )?.map(|r| {
        let r = r?;
        Ok(json!({
            "id":r.get::<_, i64>(0)?, "tanggal":r.get::<_, NaiveDate>(1)?, "metode":r.get::<_, String>(2)?,
            "referensi":r.get::<_, Option<String>>(3)?, "nominal":nominal(r.get(4)?),
            "balik_id":r.get::<_, Option<i64>>(5)?, "alasan":r.get::<_, Option<String>>(6)?,
            "operator":r.get::<_, String>(7)?, "created_at":r.get::<_, String>(8)?, "dibalik":r.get::<_, i64>(9)? == 1,
        }))
    }).collect()
}
