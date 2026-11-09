//! Perbandingan tagihan Waserda BackOffice dan transaksi kredit pada rentang terpilih.
use super::{dec, opt, tm};
use crate::error::{AppError, AppResult};
use oracle::{Connection, sql_type::RefCursor};
use rust_decimal::prelude::ToPrimitive;
use serde::Serialize;
use std::collections::BTreeMap;

#[derive(Debug, Clone, Serialize, PartialEq, Eq)]
pub struct Item {
    pub nik: String,
    pub nama: String,
    pub unit: String,
    pub tagihan: i64,
    pub rekap: i64,
    pub selisih: i64,
}
#[derive(Serialize)]
pub struct Report {
    pub periode: i32,
    pub remise: u8,
    pub dari: String,
    pub sampai: String,
    pub tagihan: i64,
    pub rekap: i64,
    pub selisih: i64,
    pub items: Vec<Item>,
}

pub fn range_columns(remise: u8) -> AppResult<(&'static str, &'static str)> {
    match remise {
        1 => Ok(("R1DARI", "R1SAMPAI")),
        2 => Ok(("R2DARI", "R2SAMPAI")),
        3 => Ok(("BDARI", "BSAMPAI")),
        _ => Err(AppError::validation("Remise harus 1, 2, atau 3")),
    }
}
fn money(s: &str) -> AppResult<i64> {
    dec(s)?
        .round_dp(0)
        .to_i64()
        .ok_or_else(|| AppError::Internal("Nilai rupiah di luar jangkauan".into()))
}
fn add(a: i64, b: i64) -> AppResult<i64> {
    a.checked_add(b)
        .ok_or_else(|| AppError::Internal("Total perbandingan di luar jangkauan".into()))
}
fn combine(tagihan: Vec<Item>, rekap: Vec<Item>) -> AppResult<Vec<Item>> {
    let mut map: BTreeMap<String, Item> = BTreeMap::new();
    for row in tagihan.into_iter().chain(rekap) {
        if row.nik.trim().is_empty() {
            return Err(AppError::Internal(
                "NIK kosong dalam data perbandingan".into(),
            ));
        }
        let v = map.entry(row.nik.clone()).or_insert_with(|| Item {
            nik: row.nik.clone(),
            nama: String::new(),
            unit: String::new(),
            tagihan: 0,
            rekap: 0,
            selisih: 0,
        });
        if v.nama.is_empty() {
            v.nama = row.nama;
        }
        if v.unit.is_empty() {
            v.unit = row.unit;
        }
        v.tagihan = add(v.tagihan, row.tagihan)?;
        v.rekap = add(v.rekap, row.rekap)?;
        v.selisih = v
            .tagihan
            .checked_sub(v.rekap)
            .ok_or_else(|| AppError::Internal("Selisih di luar jangkauan".into()))?;
    }
    Ok(map.into_values().collect())
}
pub fn report(conn: &Connection, periode: i32, remise: u8) -> AppResult<Report> {
    let (from, to) = range_columns(remise)?;
    let p = i64::from(periode);
    let fmt = "'DD-MON-YYYY', 'NLS_DATE_LANGUAGE = ENGLISH'";
    let sql = format!(
        "SELECT TO_CHAR(TO_DATE({from}, {fmt}), 'YYYY-MM-DD'), TO_CHAR(TO_DATE({to}, {fmt}), 'YYYY-MM-DD') FROM POS_PERIODE WHERE PERIODE = :p"
    );
    let result = conn.query_row_named(&sql, &[("p", &p)]);
    if let Err(e) = &result {
        if matches!(e.oci_code(), Some(1840..=1861)) {
            return Err(AppError::validation("Format tanggal periode tidak valid"));
        }
    }
    let row = opt(result)?
        .ok_or_else(|| AppError::NotFound(format!("Periode {periode} tidak ditemukan")))?;
    let dari: Option<String> = row.get(0)?;
    let sampai: Option<String> = row.get(1)?;
    let (dari, sampai) = match (dari, sampai) {
        (Some(a), Some(b)) if a <= b => (a, b),
        _ => {
            return Err(AppError::validation(
                "Rentang tanggal periode belum diatur atau tidak valid",
            ));
        }
    };
    // Sama dengan BackOffice: dua OUT REF CURSOR lalu periode dan remise.
    let mut stmt = conn
        .statement("BEGIN FINANCE.TAGIHAN_ALL(:uk, :nik, :periode, :remise); END;")
        .build()?;
    stmt.execute(&[
        &None::<RefCursor>,
        &None::<RefCursor>,
        &p,
        &i64::from(remise),
    ])
    .map_err(|e| {
        tracing::error!(error = %e, "FINANCE.TAGIHAN_ALL gagal");
        AppError::rule(
            "TAGIHAN_TIDAK_TERSEDIA",
            "Prosedur tagihan tidak tersedia atau gagal dijalankan",
        )
    })?;
    let mut cursor: RefCursor = stmt.bind_value(2)?;
    let rows = cursor.query()?;
    let required = ["NIK", "NAMA_PELANGGAN", "UNIT_KERJA", "WASERDA"];
    if required.iter().any(|name| {
        !rows
            .column_info()
            .iter()
            .any(|c| c.name().eq_ignore_ascii_case(name))
    }) {
        return Err(AppError::rule(
            "TAGIHAN_TIDAK_TERSEDIA",
            "Kolom prosedur tagihan tidak sesuai kontrak",
        ));
    }
    let mut bills = Vec::new();
    for row in rows {
        let row = row?;
        let amount: Option<String> = row.get("WASERDA")?;
        bills.push(Item {
            nik: row.get::<_, Option<String>>("NIK")?.unwrap_or_default(),
            nama: row
                .get::<_, Option<String>>("NAMA_PELANGGAN")?
                .unwrap_or_default(),
            unit: row
                .get::<_, Option<String>>("UNIT_KERJA")?
                .unwrap_or_default(),
            tagihan: match amount {
                Some(x) => money(&x)?,
                None => 0,
            },
            rekap: 0,
            selisih: 0,
        });
    }
    let sql = format!(
        "SELECT P.NIK, MAX(A.NAMA_PELANGGAN), MAX(K.NAMA), {} FROM POS_PENJUALAN P LEFT JOIN FIN_ANGGOTA A ON A.NIK = P.NIK LEFT JOIN FIN_UNITKERJA K ON K.KODE = A.UNIT_KERJA WHERE P.JENIS_BAYAR = 'KREDIT' AND P.TANGGAL >= TO_DATE(:dari, 'YYYY-MM-DD') AND P.TANGGAL < TO_DATE(:sampai, 'YYYY-MM-DD') + 1 GROUP BY P.NIK",
        tm("NVL(SUM(P.TOTAL), 0)")
    );
    let mut sales = Vec::new();
    for row in conn.query_named(&sql, &[("dari", &dari), ("sampai", &sampai)])? {
        let row = row?;
        sales.push(Item {
            nik: row.get::<_, Option<String>>(0)?.unwrap_or_default(),
            nama: row.get::<_, Option<String>>(1)?.unwrap_or_default(),
            unit: row.get::<_, Option<String>>(2)?.unwrap_or_default(),
            tagihan: 0,
            rekap: money(&row.get::<_, String>(3)?)?,
            selisih: 0,
        });
    }
    let items = combine(bills, sales)?;
    let mut tagihan = 0;
    let mut rekap = 0;
    for item in &items {
        tagihan = add(tagihan, item.tagihan)?;
        rekap = add(rekap, item.rekap)?;
    }
    let selisih = tagihan
        .checked_sub(rekap)
        .ok_or_else(|| AppError::Internal("Selisih di luar jangkauan".into()))?;
    Ok(Report {
        periode,
        remise,
        dari,
        sampai,
        tagihan,
        rekap,
        selisih,
        items,
    })
}
#[cfg(test)]
mod tests {
    use super::*;
    fn item(n: &str, t: i64, r: i64) -> Item {
        Item {
            nik: n.into(),
            nama: n.into(),
            unit: String::new(),
            tagihan: t,
            rekap: r,
            selisih: 0,
        }
    }
    #[test]
    fn ranges() {
        assert_eq!(range_columns(1).unwrap(), ("R1DARI", "R1SAMPAI"));
        assert_eq!(range_columns(2).unwrap(), ("R2DARI", "R2SAMPAI"));
        assert_eq!(range_columns(3).unwrap(), ("BDARI", "BSAMPAI"));
        assert!(range_columns(0).is_err());
    }
    #[test]
    fn merge_both_and_single_sources() {
        let rows = combine(
            vec![item("a", 100, 0), item("b", 50, 0)],
            vec![item("a", 0, 80), item("c", 0, 30)],
        )
        .unwrap();
        assert_eq!(
            rows.iter().map(|x| x.selisih).collect::<Vec<_>>(),
            vec![20, 50, -30]
        );
        assert!(combine(vec![], vec![]).unwrap().is_empty());
    }
}
