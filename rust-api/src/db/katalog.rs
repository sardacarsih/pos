use std::collections::HashMap;

use chrono::NaiveDate;
use oracle::Connection;
use rust_decimal::Decimal;
use serde::Serialize;

use super::{Binds, dec, like, opt, tm};
use crate::error::{AppError, AppResult};

#[derive(Debug, Clone)]
pub struct Produk {
    /// Versi foto produk (epoch detik) bila ada foto unggahan; null = pakai ikon/foto bawaan.
    pub foto_versi: Option<i64>,
    pub id: i64,
    pub kode: String,
    pub barcode: String,
    pub nama: String,
    pub satuan: String,
    pub harga: Decimal,
    pub hpp: Decimal,
    pub kategori_id: String,
    pub kategori: String,
}

const FROM: &str = "FROM POS_PRODUCT p LEFT JOIN POS_KATEGORI k ON TO_CHAR(k.KODE) = TO_CHAR(p.KATEGORI_ID)      LEFT JOIN MOB_PRODUK_FOTO f ON f.PRODUCT_ID = p.PRODUCTID LEFT JOIN MOB_PRODUK_URUT u ON u.PRODUCT_ID = p.PRODUCTID";

fn cols() -> String {
    format!(
        "p.PRODUCTID, p.KODE_ITEM, p.BARCODE, p.PRODUCTNAME, p.SATUAN, {}, {}, TO_CHAR(p.KATEGORI_ID), k.KATEGORI, TO_CHAR(f.VERSI)",
        tm("p.PRICE"),
        tm("NVL(p.BELI, 0)")
    )
}

fn map(r: &oracle::Row) -> AppResult<Produk> {
    Ok(Produk {
        id: r.get(0)?,
        kode: r.get(1)?,
        barcode: r.get::<_, Option<String>>(2)?.unwrap_or_default(),
        nama: r.get::<_, Option<String>>(3)?.unwrap_or_default(),
        satuan: r.get::<_, Option<String>>(4)?.unwrap_or_default(),
        harga: dec(&r.get::<_, String>(5)?)?,
        hpp: dec(&r.get::<_, String>(6)?)?,
        kategori_id: r.get::<_, Option<String>>(7)?.unwrap_or_default(),
        kategori: r.get::<_, Option<String>>(8)?.unwrap_or_default(),
        foto_versi: r
            .get::<_, Option<String>>(9)?
            .and_then(|v| v.trim().parse().ok()),
    })
}

/// Klausa pembatas kategori. `allowed` kosong = tidak dibatasi. Server yang memfilter, bukan UI.
fn kategori_clause(allowed: &[String], chip: Option<&str>, b: &mut Binds) -> String {
    let mut sql = String::new();
    if !allowed.is_empty() {
        let names: Vec<String> = allowed
            .iter()
            .enumerate()
            .map(|(i, k)| {
                b.add(format!("ak{i}"), k);
                format!(":ak{i}")
            })
            .collect();
        sql.push_str(&format!(
            " AND TO_CHAR(p.KATEGORI_ID) IN ({})",
            names.join(",")
        ));
    }
    if let Some(c) = chip {
        b.add("chip", c);
        sql.push_str(" AND TO_CHAR(p.KATEGORI_ID) = :chip");
    }
    sql
}

pub struct Halaman<T> {
    pub items: Vec<T>,
    pub ada_lagi: bool,
}

pub fn cari_barang(
    conn: &Connection,
    q: Option<&str>,
    chip: Option<&str>,
    allowed: &[String],
    page: u32,
    size: u32,
) -> AppResult<Halaman<Produk>> {
    let mut b = Binds::default();
    let mut sql = format!("SELECT {} {FROM} WHERE p.AKTIF = 'Y'", cols());
    if let Some(q) = q.filter(|q| !q.trim().is_empty()) {
        b.add("q", like(q));
        b.add("qraw", q.trim());
        sql.push_str(" AND (UPPER(p.PRODUCTNAME) LIKE :q ESCAPE '\\' OR UPPER(p.KODE_ITEM) LIKE :q ESCAPE '\\' OR p.BARCODE = :qraw)");
    }
    sql.push_str(&kategori_clause(allowed, chip, &mut b));
    b.add("off", (page.saturating_sub(1) * size).to_string());
    b.add("lim", (size + 1).to_string());
    sql.push_str(" ORDER BY NVL(u.URUT, 999999999), p.PRODUCTNAME, p.PRODUCTID OFFSET TO_NUMBER(:off) ROWS FETCH NEXT TO_NUMBER(:lim) ROWS ONLY");
    let mut items = Vec::new();
    b.with(|p| -> AppResult<()> {
        for r in conn.query_named(&sql, p)? {
            items.push(map(&r?)?);
        }
        Ok(())
    })?;
    let ada_lagi = items.len() > size as usize;
    items.truncate(size as usize);
    Ok(Halaman { items, ada_lagi })
}

/// Cari satu barang untuk simpan faktur / scan. `None` bila tidak aktif; ditolak bila di luar kategori.
pub fn barang_by(
    conn: &Connection,
    where_sql: &str,
    key: &str,
    allowed: &[String],
) -> AppResult<Option<Produk>> {
    let sql = format!(
        "SELECT {} {FROM} WHERE p.AKTIF = 'Y' AND {where_sql}",
        cols()
    );
    let r = opt(conn.query_row_named(&sql, &[("key", &key)]))?;
    let Some(r) = r else { return Ok(None) };
    let p = map(&r)?;
    if !allowed.is_empty() && !allowed.contains(&p.kategori_id) {
        return Err(AppError::rule(
            "KATEGORI_TIDAK_DIIZINKAN",
            format!(
                "Barang \"{}\" tidak termasuk kategori yang diizinkan untuk akun ini",
                p.nama
            ),
        ));
    }
    Ok(Some(p))
}

pub fn barang_by_barcode(
    conn: &Connection,
    barcode: &str,
    allowed: &[String],
) -> AppResult<Option<Produk>> {
    barang_by(conn, "p.BARCODE = :key", barcode, allowed)
}

pub fn barang_by_id(conn: &Connection, id: i64, allowed: &[String]) -> AppResult<Option<Produk>> {
    barang_by(
        conn,
        "p.PRODUCTID = TO_NUMBER(:key)",
        &id.to_string(),
        allowed,
    )
}

/// Satuan jual tambahan sebuah produk (satuan dasar tetap di `POS_PRODUCT`; ISI = jumlah satuan dasar per satuan ini).
#[derive(Debug, Clone)]
pub struct SatuanJual {
    pub satuan: String,
    pub isi: Decimal,
    pub harga: Decimal,
    pub barcode: String,
}

fn kol_satuan() -> String {
    format!("SATUAN, {}, {}, BARCODE", tm("ISI"), tm("HARGA"))
}

/// Satuan tambahan untuk banyak produk sekaligus (satu halaman).
pub fn satuan_map(conn: &Connection, ids: &[i64]) -> AppResult<HashMap<i64, Vec<SatuanJual>>> {
    let mut out: HashMap<i64, Vec<SatuanJual>> = HashMap::new();
    if ids.is_empty() {
        return Ok(out);
    }
    let mut b = Binds::default();
    let names: Vec<String> = ids
        .iter()
        .enumerate()
        .map(|(i, id)| {
            b.add(format!("s{i}"), id);
            format!("TO_NUMBER(:s{i})")
        })
        .collect();
    let sql = format!(
        "SELECT PRODUCT_ID, {} FROM POS_PRODUCT_SATUAN WHERE PRODUCT_ID IN ({}) ORDER BY PRODUCT_ID, ISI",
        kol_satuan(),
        names.join(",")
    );
    b.with(|p| -> AppResult<()> {
        for r in conn.query_named(&sql, p)? {
            let r = r?;
            let pid: i64 = r.get(0)?;
            let sj = SatuanJual {
                satuan: r.get(1)?,
                isi: dec(&r.get::<_, String>(2)?)?,
                harga: dec(&r.get::<_, String>(3)?)?,
                barcode: r.get::<_, Option<String>>(4)?.unwrap_or_default(),
            };
            out.entry(pid).or_default().push(sj);
        }
        Ok(())
    })?;
    Ok(out)
}

/// Satuan tambahan tertentu (nama dicocokkan tanpa beda huruf besar/kecil).
pub fn satuan_jual(conn: &Connection, pid: i64, satuan: &str) -> AppResult<Option<SatuanJual>> {
    let sql = format!(
        "SELECT {} FROM POS_PRODUCT_SATUAN WHERE PRODUCT_ID = TO_NUMBER(:p) AND UPPER(SATUAN) = UPPER(:s)",
        kol_satuan()
    );
    let r = opt(conn.query_row_named(&sql, &[("p", &pid.to_string()), ("s", &satuan.trim())]))?;
    let Some(r) = r else { return Ok(None) };
    Ok(Some(SatuanJual {
        satuan: r.get(0)?,
        isi: dec(&r.get::<_, String>(1)?)?,
        harga: dec(&r.get::<_, String>(2)?)?,
        barcode: r.get::<_, Option<String>>(3)?.unwrap_or_default(),
    }))
}

/// Barcode milik satuan tambahan -> (product_id, satuan).
pub fn produk_by_barcode_satuan(conn: &Connection, barcode: &str) -> AppResult<Option<(i64, String)>> {
    let r = opt(conn.query_row_named(
        "SELECT PRODUCT_ID, SATUAN FROM POS_PRODUCT_SATUAN WHERE BARCODE = :b",
        &[("b", &barcode)],
    ))?;
    let Some(r) = r else { return Ok(None) };
    Ok(Some((r.get(0)?, r.get(1)?)))
}

/// Satuan tambahan untuk opname multi-satuan: nama, faktor konversi, barcode (tanpa harga jual).
#[derive(Debug, Serialize)]
pub struct SatuanTambahan {
    pub satuan: String,
    pub isi: Decimal,
    pub barcode: String,
}

/// Batch per halaman untuk `multi_satuan=1` di /admin/stok dan /admin/stok-habis.
pub fn satuan_tambahan(conn: &Connection, ids: &[i64]) -> AppResult<HashMap<i64, Vec<SatuanTambahan>>> {
    Ok(satuan_map(conn, ids)?
        .into_iter()
        .map(|(id, v)| {
            (
                id,
                v.into_iter()
                    .map(|s| SatuanTambahan { satuan: s.satuan, isi: s.isi, barcode: s.barcode })
                    .collect(),
            )
        })
        .collect())
}

/// Kondisi pencarian nama/kode/barcode; `barcode_satuan` ikut mencocokkan barcode satuan tambahan
/// sebelum paginasi (opt-in opname multi-satuan).
fn kondisi_cari(b: &mut Binds, q: &str, barcode_satuan: bool) -> String {
    b.add("q", like(q));
    b.add("qraw", q.trim());
    let mut s = String::from(
        "(UPPER(p.PRODUCTNAME) LIKE :q ESCAPE '\\' OR UPPER(p.KODE_ITEM) LIKE :q ESCAPE '\\' OR p.BARCODE = :qraw",
    );
    if barcode_satuan {
        s.push_str(
            " OR EXISTS (SELECT 1 FROM POS_PRODUCT_SATUAN s WHERE s.PRODUCT_ID = p.PRODUCTID AND s.BARCODE = :qraw)",
        );
    }
    s.push(')');
    s
}

/// Ekspresi stok sistem (alias join dari `stok_join`): stok awal tahun + pembelian + selisih opname - penjualan - rusak.
pub(crate) const STOK_EXPR: &str = "NVL(st.q,0) + NVL(pb.q,0) + NVL(so.q,0) - (NVL(pj.q,0) + NVL(rs.q,0))";

/// Join agregat stok per KODE_ITEM untuk `POS_PRODUCT p` (bind `:dari`/`:sampai`).
/// `inl` = daftar bind IN untuk membatasi kode (satu halaman); None = semua produk.
pub(crate) fn stok_join(inl: Option<&str>) -> String {
    let rng = "TO_DATE(:dari,'YYYY-MM-DD') AND TO_DATE(:sampai,'YYYY-MM-DD')";
    let batas = |kcol: &str| inl.map(|i| format!(" AND {kcol} IN ({i})")).unwrap_or_default();
    let sub = |t: &str, kcol: &str, qcol: &str, tgl: &str| {
        format!(
            "(SELECT {kcol} k, SUM({qcol}) q FROM {t} WHERE {tgl} BETWEEN {rng}{} GROUP BY {kcol})",
            batas(kcol)
        )
    };
    format!(
        "LEFT JOIN (SELECT KODE_BARANG k, SUM(QUANTITY) q FROM POS_STOCK WHERE TANGGAL = TO_DATE(:dari,'YYYY-MM-DD'){} GROUP BY KODE_BARANG) st ON st.k = p.KODE_ITEM          LEFT JOIN {} pb ON pb.k = p.KODE_ITEM          LEFT JOIN {} pj ON pj.k = p.KODE_ITEM          LEFT JOIN {} rs ON rs.k = p.KODE_ITEM          LEFT JOIN {} so ON so.k = p.KODE_ITEM",
        batas("KODE_BARANG"),
        sub(
            "POS_PEMBELIANDETAIL d JOIN POS_PEMBELIAN m ON m.PURCHASE_ID = d.PURCHASE_ID",
            "d.KODE_BARANG",
            "d.QUANTITY",
            "m.TANGGAL"
        ),
        sub(
            "POS_PENJUALAN_DETAIL d JOIN POS_PENJUALAN m ON m.NO_TRANSAKSI = d.NO_TRANSAKSI",
            "d.KODE_BARANG",
            "d.JUMLAH_BARANG * d.ISI", // ISI = faktor satuan jual -> satuan dasar
            "m.TANGGAL"
        ),
        sub("POS_BARANGRUSAK", "KODE_BARANG", "JUMLAHFISIK", "TANGGAL"),
        sub("POS_STOCKOPNAME", "KODE_BARANG", "SELISIH", "TANGGAL"),
    )
}

/// Stok sistem per kode (formula sama dengan GetStocItem desktop, set-based untuk satu halaman).
pub fn stok_map(
    conn: &Connection,
    kodes: &[String],
    dari: NaiveDate,
    sampai: NaiveDate,
) -> AppResult<HashMap<String, Decimal>> {
    let mut out = HashMap::new();
    if kodes.is_empty() {
        return Ok(out);
    }
    let mut b = Binds::default();
    let names: Vec<String> = kodes
        .iter()
        .enumerate()
        .map(|(i, k)| {
            b.add(format!("k{i}"), k);
            format!(":k{i}")
        })
        .collect();
    let inl = names.join(",");
    b.add("dari", dari.format("%Y-%m-%d"));
    b.add("sampai", sampai.format("%Y-%m-%d"));
    let sql = format!(
        "SELECT p.KODE_ITEM, {} FROM POS_PRODUCT p {} WHERE p.KODE_ITEM IN ({inl})",
        tm(STOK_EXPR),
        stok_join(Some(&inl)),
    );
    b.with(|p| -> AppResult<()> {
        for r in conn.query_named(&sql, p)? {
            let r = r?;
            out.insert(r.get::<_, String>(0)?, dec(&r.get::<_, String>(1)?)?);
        }
        Ok(())
    })?;
    Ok(out)
}

#[derive(Debug, Serialize)]
pub struct StokHabis {
    pub id: i64,
    pub kode: String,
    pub nama: String,
    pub satuan: String,
    pub kategori: String,
    pub stok: Decimal,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub satuan_tambahan: Option<Vec<SatuanTambahan>>,
}

#[derive(Clone, Copy)]
pub enum UrutanStokHabis {
    Nama,
    Qty,
}

/// Produk aktif dengan stok sistem <= 0 (1 Jan tahun berjalan s/d `sampai`).
/// `jumlah` (total seluruh halaman) hanya dihitung pada halaman pertama.
#[allow(clippy::too_many_arguments)]
pub fn stok_habis(
    conn: &Connection,
    q: Option<&str>,
    kategori: Option<&str>,
    dari: NaiveDate,
    sampai: NaiveDate,
    page: u32,
    size: u32,
    barcode_satuan: bool,
    urutan: UrutanStokHabis,
) -> AppResult<(Halaman<StokHabis>, Option<i64>)> {
    let mut b = Binds::default();
    b.add("dari", dari.format("%Y-%m-%d"));
    b.add("sampai", sampai.format("%Y-%m-%d"));
    let mut where_sql = format!("WHERE p.AKTIF = 'Y' AND {STOK_EXPR} <= 0");
    if let Some(q) = q.filter(|q| !q.trim().is_empty()) {
        where_sql.push_str(&format!(" AND {}", kondisi_cari(&mut b, q, barcode_satuan)));
    }
    if let Some(k) = kategori.filter(|k| !k.trim().is_empty()) {
        b.add("kat", k.trim());
        where_sql.push_str(" AND TO_CHAR(p.KATEGORI_ID) = :kat");
    }
    b.add("off", (u64::from(page.saturating_sub(1)) * u64::from(size)).to_string());
    b.add("lim", (size + 1).to_string());
    let urutan_qty = match urutan {
        UrutanStokHabis::Nama => String::new(),
        UrutanStokHabis::Qty => format!("({STOK_EXPR}) ASC, "),
    };
    // COUNT(*) OVER () memberi total seluruh hasil dari agregasi yang sama (satu pemindaian, bukan dua).
    let sql = format!(
        "SELECT p.PRODUCTID, p.KODE_ITEM, p.PRODUCTNAME, p.SATUAN, k.KATEGORI, {}, COUNT(*) OVER () \
         FROM POS_PRODUCT p LEFT JOIN POS_KATEGORI k ON TO_CHAR(k.KODE) = TO_CHAR(p.KATEGORI_ID) {} {where_sql} \
         ORDER BY {urutan_qty}p.PRODUCTNAME, p.PRODUCTID OFFSET TO_NUMBER(:off) ROWS FETCH NEXT TO_NUMBER(:lim) ROWS ONLY",
        tm(STOK_EXPR),
        stok_join(None)
    );
    let mut items = Vec::new();
    let mut total: Option<i64> = None;
    b.with(|p| -> AppResult<()> {
        for r in conn.query_named(&sql, p)? {
            let r = r?;
            total.get_or_insert(r.get::<_, i64>(6)?);
            items.push(StokHabis {
                id: r.get(0)?,
                kode: r.get::<_, Option<String>>(1)?.unwrap_or_default(),
                nama: r.get::<_, Option<String>>(2)?.unwrap_or_default(),
                satuan: r.get::<_, Option<String>>(3)?.unwrap_or_default(),
                kategori: r.get::<_, Option<String>>(4)?.unwrap_or_default(),
                stok: dec(&r.get::<_, String>(5)?)?,
                satuan_tambahan: None,
            });
        }
        Ok(())
    })?;
    let ada_lagi = items.len() > size as usize;
    items.truncate(size as usize);
    // Total hanya dikirim di halaman 1 (halaman kosong di sana berarti memang 0).
    let jumlah = (page <= 1).then(|| total.unwrap_or(0));
    Ok((Halaman { items, ada_lagi }, jumlah))
}

#[cfg(test)]
mod stok_sql_tests {
    use super::*;

    #[test]
    fn stok_join_tanpa_batas_kode_tidak_memakai_in() {
        let s = stok_join(None);
        assert!(!s.contains(" IN ("));
        assert!(s.contains(":dari") && s.contains(":sampai"));
        for t in ["POS_STOCK ", "POS_PEMBELIANDETAIL", "POS_PENJUALAN_DETAIL", "POS_BARANGRUSAK", "POS_STOCKOPNAME"] {
            assert!(s.contains(t), "{t}");
        }
    }

    #[test]
    fn stok_join_dengan_batas_kode_membatasi_semua_subquery() {
        let s = stok_join(Some(":k0,:k1"));
        assert_eq!(s.matches("IN (:k0,:k1)").count(), 5);
    }
}

#[derive(Debug, Serialize)]
pub struct Kategori {
    pub id: String,
    pub nama: String,
}

/// Simpan urutan manual produk (indeks = urutan). Produk di luar daftar kembali ke urutan abjad.
pub fn urut_set(conn: &Connection, ids: &[i64]) -> AppResult<()> {
    for (i, id) in ids.iter().enumerate() {
        let n = i as i64;
        conn.execute_named(
            "MERGE INTO MOB_PRODUK_URUT t USING (SELECT :id PRODUCT_ID FROM DUAL) s ON (t.PRODUCT_ID = s.PRODUCT_ID)              WHEN MATCHED THEN UPDATE SET t.URUT = :n WHEN NOT MATCHED THEN INSERT (PRODUCT_ID, URUT) VALUES (s.PRODUCT_ID, :n)",
            &[("id", id), ("n", &n)],
        )?;
    }
    conn.commit()?;
    Ok(())
}

pub fn semua_kategori(conn: &Connection) -> AppResult<Vec<Kategori>> {
    let mut out = Vec::new();
    for r in conn.query(
        "SELECT TO_CHAR(KODE), KATEGORI FROM POS_KATEGORI ORDER BY KATEGORI",
        &[],
    )? {
        let r = r?;
        out.push(Kategori {
            id: r.get(0)?,
            nama: r.get::<_, Option<String>>(1)?.unwrap_or_default(),
        });
    }
    Ok(out)
}

pub fn kategori_diizinkan(conn: &Connection, allowed: &[String]) -> AppResult<Vec<Kategori>> {
    let semua = semua_kategori(conn)?;
    Ok(if allowed.is_empty() {
        semua
    } else {
        semua
            .into_iter()
            .filter(|k| allowed.contains(&k.id))
            .collect()
    })
}

fn nama_kategori_valid(nama: &str) -> AppResult<String> {
    let nama = nama.trim();
    if nama.is_empty() {
        return Err(AppError::validation("Nama kategori wajib diisi"));
    }
    if nama.len() > 50 {
        return Err(AppError::validation("Nama kategori maksimal 50 byte"));
    }
    if nama.chars().any(char::is_control) {
        return Err(AppError::validation(
            "Nama kategori mengandung karakter kontrol",
        ));
    }
    Ok(nama.to_string())
}

fn kategori_ada(c: &Connection, id: &str) -> AppResult<Kategori> {
    opt(c.query_row_named(
        "SELECT TO_CHAR(KODE), KATEGORI FROM POS_KATEGORI WHERE TO_CHAR(KODE) = :id",
        &[("id", &id)],
    ))?
    .map(|r| -> AppResult<Kategori> {
        Ok(Kategori {
            id: r.get(0)?,
            nama: r.get::<_, Option<String>>(1)?.unwrap_or_default(),
        })
    })
    .transpose()?
    .ok_or_else(|| AppError::NotFound("Kategori tidak ditemukan".into()))
}

fn kategori_jumlah_produk(c: &Connection, id: &str) -> AppResult<i64> {
    Ok(c.query_row_named(
        "SELECT COUNT(*) FROM POS_PRODUCT WHERE TO_CHAR(KATEGORI_ID) = :id",
        &[("id", &id)],
    )?
    .get(0)?)
}

fn kategori_nama_dipakai(c: &Connection, nama: &str, kecuali: Option<&str>) -> AppResult<bool> {
    let n: i64 = match kecuali {
        Some(id) => c.query_row_named(
            "SELECT COUNT(*) FROM POS_KATEGORI WHERE UPPER(KATEGORI) = UPPER(:nama) AND TO_CHAR(KODE) <> :id",
            &[("nama", &nama), ("id", &id)],
        )?.get(0)?,
        None => c.query_row_named(
            "SELECT COUNT(*) FROM POS_KATEGORI WHERE UPPER(KATEGORI) = UPPER(:nama)",
            &[("nama", &nama)],
        )?.get(0)?,
    };
    Ok(n > 0)
}

pub fn kategori_buat(c: &Connection, nama: &str) -> AppResult<Kategori> {
    let nama = nama_kategori_valid(nama)?;
    if kategori_nama_dipakai(c, &nama, None)? {
        return Err(AppError::Conflict("Nama kategori sudah dipakai".into()));
    }
    let kode: i64 = c.query_row_as("SELECT NVL(MAX(KODE), 0) + 1 FROM POS_KATEGORI", &[])?;
    c.execute_named(
        "INSERT INTO POS_KATEGORI (KODE, KATEGORI) VALUES (:kode, :nama)",
        &[("kode", &kode), ("nama", &nama)],
    )
    .map_err(|e| {
        if e.oci_code() == Some(1) {
            AppError::Conflict("Kode kategori bentrok, coba lagi".into())
        } else {
            e.into()
        }
    })?;
    c.commit()?;
    Ok(Kategori { id: kode.to_string(), nama })
}

pub fn kategori_ubah(c: &Connection, id: &str, nama: &str) -> AppResult<()> {
    let nama = nama_kategori_valid(nama)?;
    kategori_ada(c, id)?;
    if kategori_nama_dipakai(c, &nama, Some(id))? {
        return Err(AppError::Conflict("Nama kategori sudah dipakai".into()));
    }
    let hasil = (|| -> AppResult<()> {
        let stmt = c.execute_named(
            "UPDATE POS_KATEGORI SET KATEGORI = :nama WHERE TO_CHAR(KODE) = :id",
            &[("nama", &nama), ("id", &id)],
        )?;
        if stmt.row_count()? == 0 {
            return Err(AppError::NotFound("Kategori tidak ditemukan".into()));
        }
        c.commit()?;
        Ok(())
    })();
    if hasil.is_err() {
        let _ = c.rollback();
    }
    hasil
}

pub fn kategori_jejak(c: &Connection, id: &str) -> AppResult<(Kategori, i64)> {
    let k = kategori_ada(c, id)?;
    let n = kategori_jumlah_produk(c, id)?;
    Ok((k, n))
}

pub fn kategori_hapus(c: &Connection, id: &str) -> AppResult<()> {
    kategori_ada(c, id)?;
    let n = kategori_jumlah_produk(c, id)?;
    if n > 0 {
        return Err(AppError::Conflict(format!(
            "Kategori masih dipakai {n} produk. Pindahkan produk ke kategori lain dulu."
        )));
    }
    let hasil = (|| -> AppResult<()> {
        c.execute_named(
            "DELETE FROM MOB_KATEGORI_TAMPIL WHERE KATEGORI_ID = :id",
            &[("id", &id)],
        )?;
        let stmt = c.execute_named(
            "DELETE FROM POS_KATEGORI WHERE TO_CHAR(KODE) = :id",
            &[("id", &id)],
        )?;
        if stmt.row_count()? == 0 {
            return Err(AppError::NotFound("Kategori tidak ditemukan".into()));
        }
        c.commit()?;
        Ok(())
    })();
    if hasil.is_err() {
        let _ = c.rollback();
    }
    hasil
}

#[derive(Debug, Serialize)]
pub struct Satuan {
    pub id: String,
    pub nama: String,
}

pub fn semua_satuan(c: &Connection) -> AppResult<Vec<Satuan>> {
    let mut out = Vec::new();
    for r in c.query("SELECT TO_CHAR(ID), SATUAN FROM POS_SATUAN ORDER BY SATUAN", &[])? {
        let r = r?;
        out.push(Satuan { id: r.get(0)?, nama: r.get::<_, Option<String>>(1)?.unwrap_or_default() });
    }
    Ok(out)
}

fn nama_satuan_valid(nama: &str) -> AppResult<String> {
    let nama = nama.trim();
    if nama.is_empty() || nama.chars().count() > 20 {
        return Err(AppError::validation("Nama satuan 1-20 karakter"));
    }
    if nama.chars().any(char::is_control) {
        return Err(AppError::validation("Nama satuan mengandung karakter kontrol"));
    }
    Ok(nama.to_uppercase())
}

fn satuan_ada(c: &Connection, id: &str) -> AppResult<Satuan> {
    opt(c.query_row_named(
        "SELECT TO_CHAR(ID), SATUAN FROM POS_SATUAN WHERE TO_CHAR(ID) = :id",
        &[("id", &id)],
    ))?
    .map(|r| -> AppResult<Satuan> {
        Ok(Satuan { id: r.get(0)?, nama: r.get::<_, Option<String>>(1)?.unwrap_or_default() })
    })
    .transpose()?
    .ok_or_else(|| AppError::NotFound("Satuan tidak ditemukan".into()))
}

fn satuan_jumlah_produk(c: &Connection, nama: &str) -> AppResult<i64> {
    Ok(c.query_row_named(
        "SELECT (SELECT COUNT(*) FROM POS_PRODUCT WHERE UPPER(TRIM(SATUAN)) = UPPER(:n)) \
              + (SELECT COUNT(*) FROM POS_PRODUCT_SATUAN WHERE UPPER(TRIM(SATUAN)) = UPPER(:n)) FROM DUAL",
        &[("n", &nama)],
    )?
    .get(0)?)
}

fn satuan_nama_dipakai(c: &Connection, nama: &str, kecuali: Option<&str>) -> AppResult<bool> {
    let id = kecuali.unwrap_or("");
    let n: i64 = c
        .query_row_named(
            "SELECT COUNT(*) FROM POS_SATUAN WHERE UPPER(TRIM(SATUAN)) = UPPER(:nama) AND TO_CHAR(ID) <> :id",
            &[("nama", &nama), ("id", &id)],
        )?
        .get(0)?;
    Ok(n > 0)
}

/// Satuan harus ada di master `POS_SATUAN` (tanpa beda huruf); mengembalikan nama persis seperti di master.
pub fn pastikan_satuan_master(c: &Connection, nama: &str) -> AppResult<String> {
    let nama = nama.trim();
    opt(c.query_row_as_named::<String>(
        "SELECT TRIM(SATUAN) FROM POS_SATUAN WHERE UPPER(TRIM(SATUAN)) = UPPER(:n) FETCH FIRST 1 ROWS ONLY",
        &[("n", &nama)],
    ))?
    .ok_or_else(|| AppError::validation(format!("Satuan \"{nama}\" tidak ada di master Satuan")))
}

/// Samakan nama setiap satuan tambahan dengan penulisan di master (sekaligus memastikan ada di master).
fn satuan_kanonik(conn: &Connection, daftar: &[SatuanJual]) -> AppResult<Vec<SatuanJual>> {
    daftar
        .iter()
        .map(|s| Ok(SatuanJual { satuan: pastikan_satuan_master(conn, &s.satuan)?, ..s.clone() }))
        .collect()
}

pub fn satuan_buat(c: &Connection, nama: &str) -> AppResult<Satuan> {
    let nama = nama_satuan_valid(nama)?;
    if satuan_nama_dipakai(c, &nama, None)? {
        return Err(AppError::Conflict("Satuan sudah ada".into()));
    }
    let id: i64 = c.query_row_as("SELECT NVL(MAX(ID), 0) + 1 FROM POS_SATUAN", &[])?;
    c.execute_named("INSERT INTO POS_SATUAN (ID, SATUAN) VALUES (:id, :nama)", &[("id", &id), ("nama", &nama)])
        .map_err(|e| {
            if e.oci_code() == Some(1) {
                AppError::Conflict("ID satuan bentrok, coba lagi".into())
            } else {
                e.into()
            }
        })?;
    c.commit()?;
    Ok(Satuan { id: id.to_string(), nama })
}

/// Ubah nama satuan; produk yang memakainya ikut berganti nama. Riwayat transaksi tetap memakai nama lama.
pub fn satuan_ubah(c: &Connection, id: &str, nama: &str) -> AppResult<()> {
    let nama = nama_satuan_valid(nama)?;
    let lama = satuan_ada(c, id)?;
    if satuan_nama_dipakai(c, &nama, Some(id))? {
        return Err(AppError::Conflict("Satuan sudah ada".into()));
    }
    let hasil = (|| -> AppResult<()> {
        let lama_nama = lama.nama.trim().to_string();
        c.execute_named("UPDATE POS_SATUAN SET SATUAN = :nama WHERE TO_CHAR(ID) = :id", &[("nama", &nama), ("id", &id)])?;
        c.execute_named(
            "UPDATE POS_PRODUCT SET SATUAN = :nama WHERE UPPER(TRIM(SATUAN)) = UPPER(:lama)",
            &[("nama", &nama), ("lama", &lama_nama)],
        )?;
        c.execute_named(
            "UPDATE POS_PRODUCT_SATUAN SET SATUAN = :nama WHERE UPPER(TRIM(SATUAN)) = UPPER(:lama)",
            &[("nama", &nama), ("lama", &lama_nama)],
        )?;
        c.commit()?;
        Ok(())
    })();
    if hasil.is_err() {
        let _ = c.rollback();
    }
    hasil
}

pub fn satuan_jejak(c: &Connection, id: &str) -> AppResult<(Satuan, i64)> {
    let s = satuan_ada(c, id)?;
    let n = satuan_jumlah_produk(c, s.nama.trim())?;
    Ok((s, n))
}

pub fn satuan_hapus(c: &Connection, id: &str) -> AppResult<()> {
    let s = satuan_ada(c, id)?;
    let n = satuan_jumlah_produk(c, s.nama.trim())?;
    if n > 0 {
        return Err(AppError::Conflict(format!(
            "Satuan masih dipakai {n} produk. Ganti satuan produk itu dulu."
        )));
    }
    let hasil = (|| -> AppResult<()> {
        c.execute_named("DELETE FROM POS_SATUAN WHERE TO_CHAR(ID) = :id", &[("id", &id)])?;
        c.commit()?;
        Ok(())
    })();
    if hasil.is_err() {
        let _ = c.rollback();
    }
    hasil
}

#[cfg(test)]
mod tests_kategori {
    use super::nama_kategori_valid;

    #[test]
    fn nama_kategori_trim_dan_batas_byte() {
        assert_eq!(nama_kategori_valid("  Minuman ").unwrap(), "Minuman");
        assert!(nama_kategori_valid("   ").is_err());
        assert!(nama_kategori_valid(&"é".repeat(25)).is_ok());
        assert!(nama_kategori_valid(&"é".repeat(26)).is_err());
        assert!(nama_kategori_valid("a\nb").is_err());
        // Huruf besar/kecil dipertahankan (data lama mixed-case).
        assert_eq!(nama_kategori_valid("Alat Tulis").unwrap(), "Alat Tulis");
    }
}

/// Aturan diskon qty: (MINQTY, POTONGAN per unit) untuk satu KODE_ITEM.
pub fn potongan_qty(conn: &Connection, kode: &str) -> AppResult<Option<(i64, Decimal)>> {
    let sql = format!(
        "SELECT {}, {} FROM POS_POTONGANBERDASARKANQTY WHERE KODE_ITEM = :k FETCH FIRST 1 ROW ONLY",
        tm("MINQTY"),
        tm("POTONGAN")
    );
    match opt(conn.query_row_named(&sql, &[("k", &kode)]))? {
        Some(r) => {
            let min = dec(&r.get::<_, String>(0)?)?;
            let pot = dec(&r.get::<_, String>(1)?)?;
            Ok(Some((
                min.trunc().to_string().parse::<i64>().unwrap_or(0),
                pot,
            )))
        }
        None => Ok(None),
    }
}

pub fn setting(conn: &Connection, config: &str) -> AppResult<Option<Decimal>> {
    let sql = format!(
        "SELECT {} FROM FIN_SETTINGS WHERE CONFIG = :c FETCH FIRST 1 ROW ONLY",
        tm("JUMLAH")
    );
    match opt(conn.query_row_named(&sql, &[("c", &config)]))? {
        Some(r) => Ok(r
            .get::<_, Option<String>>(0)?
            .map(|s| dec(&s))
            .transpose()?),
        None => Ok(None),
    }
}

// ------------------------------------------------------------ pelanggan

#[derive(Debug, Clone)]
pub struct Pelanggan {
    pub id: i64,
    pub nik: String,
    pub nama: String,
    pub unit_kerja: String,
    pub unit_nama: String,
    pub status: String,
    pub limit: Decimal,
}

const PEL_FROM: &str =
    "FROM FIN_ANGGOTA A JOIN FIN_UNITKERJA K ON K.KODE = A.UNIT_KERJA WHERE A.AKTIF = 'Y'";

fn pel_cols() -> String {
    format!(
        "TO_CHAR(A.ID_PELANGGAN), A.NIK, A.NAMA_PELANGGAN, A.UNIT_KERJA, K.NAMA, A.STATUS, {}",
        tm("NVL(A.LIMIT_HUTANG, 0)")
    )
}

fn pel_map(r: &oracle::Row) -> AppResult<Pelanggan> {
    Ok(Pelanggan {
        id: r
            .get::<_, String>(0)?
            .parse()
            .map_err(|_| AppError::Internal("ID_PELANGGAN bukan angka".into()))?,
        nik: r.get(1)?,
        nama: r.get::<_, Option<String>>(2)?.unwrap_or_default(),
        unit_kerja: r.get::<_, Option<String>>(3)?.unwrap_or_default(),
        unit_nama: r.get::<_, Option<String>>(4)?.unwrap_or_default(),
        status: r.get::<_, Option<String>>(5)?.unwrap_or_default(),
        limit: dec(&r.get::<_, String>(6)?)?,
    })
}

pub fn cari_pelanggan(
    conn: &Connection,
    q: Option<&str>,
    page: u32,
    size: u32,
    belum_akun: bool,
) -> AppResult<Halaman<Pelanggan>> {
    let mut b = Binds::default();
    let mut sql = format!("SELECT {} {PEL_FROM}", pel_cols());
    // Hanya anggota yang belum punya akun mobile (untuk admin membuat akun anggota).
    if belum_akun {
        sql.push_str(" AND NOT EXISTS (SELECT 1 FROM MOB_USER U WHERE U.NIK = A.NIK)");
    }
    if let Some(q) = q.filter(|q| !q.trim().is_empty()) {
        b.add("q", like(q));
        sql.push_str(" AND (UPPER(A.NAMA_PELANGGAN) LIKE :q ESCAPE '\\' OR UPPER(A.NIK) LIKE :q ESCAPE '\\')");
    }
    b.add("off", (page.saturating_sub(1) * size).to_string());
    b.add("lim", (size + 1).to_string());
    sql.push_str(" ORDER BY A.NAMA_PELANGGAN, A.NIK OFFSET TO_NUMBER(:off) ROWS FETCH NEXT TO_NUMBER(:lim) ROWS ONLY");
    let mut items = Vec::new();
    b.with(|p| -> AppResult<()> {
        for r in conn.query_named(&sql, p)? {
            items.push(pel_map(&r?)?);
        }
        Ok(())
    })?;
    let ada_lagi = items.len() > size as usize;
    items.truncate(size as usize);
    Ok(Halaman { items, ada_lagi })
}

/// Semua anggota aktif yang belum punya akun mobile, urut unit kerja lalu nama (pembuatan akun massal).
pub fn semua_belum_akun(conn: &Connection) -> AppResult<Vec<Pelanggan>> {
    let sql = format!(
        "SELECT {} {PEL_FROM} AND NOT EXISTS (SELECT 1 FROM MOB_USER U WHERE U.NIK = A.NIK) \
         ORDER BY K.NAMA, A.NAMA_PELANGGAN, A.NIK",
        pel_cols()
    );
    let mut out = Vec::new();
    for r in conn.query(&sql, &[])? {
        out.push(pel_map(&r?)?);
    }
    Ok(out)
}

/// Pelanggan aktif menurut NIK (kartu anggota / barcode NIK / simpan faktur).
pub fn pelanggan_by_nik(conn: &Connection, nik: &str) -> AppResult<Option<Pelanggan>> {
    let sql = format!("SELECT {} {PEL_FROM} AND A.NIK = :nik", pel_cols());
    match opt(conn.query_row_named(&sql, &[("nik", &nik)]))? {
        Some(r) => Ok(Some(pel_map(&r)?)),
        None => Ok(None),
    }
}

// ------------------------------------------------------------------ admin: kelola produk

#[derive(Debug, Serialize)]
pub struct ProdukAdmin {
    pub foto_versi: Option<i64>,
    pub id: i64,
    pub kode: String,
    pub barcode: String,
    pub nama: String,
    pub satuan: String,
    pub harga: i64,
    pub beli: i64,
    pub kategori_id: String,
    pub kategori: String,
    pub aktif: bool,
}

const COLS_ADMIN: &str = "p.PRODUCTID, p.KODE_ITEM, p.BARCODE, p.PRODUCTNAME, p.SATUAN, \
     TO_CHAR(p.PRICE, 'TM9', 'NLS_NUMERIC_CHARACTERS=''.,'''), \
     TO_CHAR(NVL(p.BELI, 0), 'TM9', 'NLS_NUMERIC_CHARACTERS=''.,'''), \
     TO_CHAR(p.KATEGORI_ID), k.KATEGORI, p.AKTIF, TO_CHAR(f.VERSI)";

fn map_admin(r: &oracle::Row) -> AppResult<ProdukAdmin> {
    Ok(ProdukAdmin {
        id: r.get(0)?,
        kode: r.get::<_, Option<String>>(1)?.unwrap_or_default(),
        barcode: r.get::<_, Option<String>>(2)?.unwrap_or_default(),
        nama: r.get::<_, Option<String>>(3)?.unwrap_or_default(),
        satuan: r.get::<_, Option<String>>(4)?.unwrap_or_default(),
        harga: super::tampil(dec(&r.get::<_, String>(5)?)?),
        beli: super::tampil(dec(&r.get::<_, String>(6)?)?),
        kategori_id: r.get::<_, Option<String>>(7)?.unwrap_or_default(),
        kategori: r.get::<_, Option<String>>(8)?.unwrap_or_default(),
        aktif: r
            .get::<_, Option<String>>(9)?
            .is_some_and(|a| a.trim().eq_ignore_ascii_case("Y")),
        foto_versi: r
            .get::<_, Option<String>>(10)?
            .and_then(|v| v.trim().parse().ok()),
    })
}

/// Daftar produk untuk admin: termasuk nonaktif, tanpa batas kategori. `aktif` = "Y" | "N" | None (semua).
#[allow(clippy::too_many_arguments)]
pub fn daftar_produk_admin(
    conn: &Connection,
    q: Option<&str>,
    kategori: Option<&str>,
    aktif: Option<&str>,
    harga_rendah: bool,
    page: u32,
    size: u32,
    barcode_satuan: bool,
) -> AppResult<Halaman<ProdukAdmin>> {
    let mut b = Binds::default();
    let mut sql = format!("SELECT {COLS_ADMIN} {FROM} WHERE 1 = 1");
    if let Some(q) = q.filter(|q| !q.trim().is_empty()) {
        sql.push_str(&format!(" AND {}", kondisi_cari(&mut b, q, barcode_satuan)));
    }
    if let Some(k) = kategori.filter(|k| !k.trim().is_empty()) {
        b.add("kat", k.trim());
        sql.push_str(" AND TO_CHAR(p.KATEGORI_ID) = :kat");
    }
    if harga_rendah {
        // Monitoring margin: produk aktif yang rugi/impas (harga jual <= harga beli) ATAU belum punya HPP (BELI kosong/0).
        sql.push_str(&format!(" AND {HARGA_RENDAH}"));
    } else {
        match aktif {
            Some("Y") => sql.push_str(" AND p.AKTIF = 'Y'"),
            Some("N") => sql.push_str(" AND NVL(p.AKTIF, 'N') <> 'Y'"),
            _ => {}
        }
    }
    b.add("off", (page.saturating_sub(1) * size).to_string());
    b.add("lim", (size + 1).to_string());
    // Harga rendah: yang rugi dulu (margin terkecil), lalu yang belum punya HPP (nama A-Z); selain itu urutan manual admin.
    let urut = if harga_rendah {
        "CASE WHEN NVL(p.BELI, 0) > 0 THEN 0 ELSE 1 END, CASE WHEN NVL(p.BELI, 0) > 0 THEN p.PRICE - p.BELI END, p.PRODUCTNAME, p.PRODUCTID"
    } else {
        "NVL(u.URUT, 999999999), p.PRODUCTNAME, p.PRODUCTID"
    };
    sql.push_str(&format!(" ORDER BY {urut} OFFSET TO_NUMBER(:off) ROWS FETCH NEXT TO_NUMBER(:lim) ROWS ONLY"));
    let mut items = Vec::new();
    b.with(|p| -> AppResult<()> {
        for r in conn.query_named(&sql, p)? {
            items.push(map_admin(&r?)?);
        }
        Ok(())
    })?;
    let ada_lagi = items.len() > size as usize;
    items.truncate(size as usize);
    Ok(Halaman { items, ada_lagi })
}

/// Rugi/impas: ada harga beli (HPP = harga beli terakhir) dan harga jual <= HPP.
const RUGI: &str = "NVL(p.BELI, 0) > 0 AND NVL(p.PRICE, 0) <= p.BELI";
/// Belum punya HPP: harga beli kosong/0 (data belum lengkap; margin tidak dapat dihitung).
const TANPA_HPP: &str = "NVL(p.BELI, 0) <= 0";
/// Isi daftar "Harga jual <= HPP": produk aktif yang rugi/impas ATAU belum punya HPP.
const HARGA_RENDAH: &str = "p.AKTIF = 'Y' AND ((NVL(p.BELI, 0) > 0 AND NVL(p.PRICE, 0) <= p.BELI) OR NVL(p.BELI, 0) <= 0)";

/// Hitungan terpisah agar yang butuh tindakan (rugi) tidak tertutup oleh data belum lengkap (tanpa HPP).
#[derive(Debug, Clone, Copy, PartialEq, Eq, Serialize)]
pub struct HargaRendah {
    pub rugi: i64,
    pub tanpa_hpp: i64,
}

impl HargaRendah {
    /// Panjang daftar "Harga jual <= HPP" (rugi + tanpa HPP; keduanya saling lepas).
    pub fn total(&self) -> i64 {
        self.rugi + self.tanpa_hpp
    }
}

/// Hitungan produk aktif rugi/impas dan tanpa HPP (tanpa filter pencarian/kategori; satu query ringan, tanpa join transaksi).
pub fn jumlah_harga_rendah(conn: &Connection) -> AppResult<HargaRendah> {
    let (rugi, tanpa_hpp) = conn.query_row_as::<(i64, i64)>(
        &format!(
            "SELECT COUNT(CASE WHEN {RUGI} THEN 1 END), COUNT(CASE WHEN {TANPA_HPP} THEN 1 END) FROM POS_PRODUCT p WHERE p.AKTIF = 'Y'"
        ),
        &[],
    )?;
    Ok(HargaRendah { rugi, tanpa_hpp })
}

/// Jumlah produk aktif dengan stok sistem <= 0 (agregasi seluruh transaksi `dari`..`sampai`: berat, cache di pemanggil).
pub fn jumlah_stok_habis(conn: &Connection, dari: NaiveDate, sampai: NaiveDate) -> AppResult<i64> {
    let mut b = Binds::default();
    b.add("dari", dari.format("%Y-%m-%d"));
    b.add("sampai", sampai.format("%Y-%m-%d"));
    let sql = format!(
        "SELECT COUNT(*) FROM POS_PRODUCT p {} WHERE p.AKTIF = 'Y' AND {STOK_EXPR} <= 0",
        stok_join(None)
    );
    Ok(b.with(|p| conn.query_row_as_named::<i64>(&sql, p))?)
}

pub fn produk_admin_by_id(conn: &Connection, id: i64) -> AppResult<Option<ProdukAdmin>> {
    let sql = format!("SELECT {COLS_ADMIN} {FROM} WHERE p.PRODUCTID = :id");
    match opt(conn.query_row_named(&sql, &[("id", &id)]))? {
        Some(r) => Ok(Some(map_admin(&r)?)),
        None => Ok(None),
    }
}

#[derive(Debug, Clone)]
pub struct UbahProduk {
    pub nama: String,
    pub barcode: String,
    pub satuan: String,
    pub kategori_id: String,
    pub harga: i64,
    pub beli: i64,
    pub aktif: bool,
}

/// Aturan isian produk (tanpa DB): panjang kolom mengikuti POS_PRODUCT/BackOffice; harga bilangan bulat >= 0.
pub fn validasi_produk(p: &UbahProduk) -> AppResult<()> {
    let ukuran = |s: &str| s.trim().chars().count();
    if ukuran(&p.nama) == 0 || ukuran(&p.nama) > 100 {
        return Err(AppError::validation(
            "Nama produk wajib diisi (maks 100 karakter)",
        ));
    }
    if ukuran(&p.satuan) == 0 || ukuran(&p.satuan) > 20 {
        return Err(AppError::validation(
            "Satuan wajib diisi (maks 20 karakter)",
        ));
    }
    if ukuran(&p.barcode) > 50 {
        return Err(AppError::validation("Barcode maksimal 50 karakter"));
    }
    if p.kategori_id.trim().is_empty() {
        return Err(AppError::validation("Kategori wajib dipilih"));
    }
    if p.harga < 0 || p.beli < 0 {
        return Err(AppError::validation("Harga tidak boleh negatif"));
    }
    Ok(())
}

/// Riwayat pemakaian satu produk (jumlah baris per tabel, dicocokkan lewat KODE_BARANG = KODE_ITEM).
/// Produk hanya boleh dihapus permanen bila semuanya 0.
#[derive(Debug, Clone, Copy, PartialEq, Eq, Serialize)]
pub struct JejakProduk {
    pub penjualan: i64,
    pub pembelian: i64,
    pub pending: i64,
    pub stok: i64,
    pub opname: i64,
    pub rusak: i64,
}

impl JejakProduk {
    pub fn total(&self) -> i64 {
        self.penjualan + self.pembelian + self.pending + self.stok + self.opname + self.rusak
    }
    pub fn bisa_hapus(&self) -> bool {
        self.total() == 0
    }
    /// Rincian yang dipakai di pesan penolakan, mis. "penjualan (3), stok tahunan (1)".
    pub fn rincian(&self) -> String {
        [
            ("penjualan", self.penjualan),
            ("pembelian", self.pembelian),
            ("transaksi tertunda", self.pending),
            ("stok tahunan", self.stok),
            ("stock opname", self.opname),
            ("barang rusak", self.rusak),
        ]
        .iter()
        .filter(|(_, n)| *n > 0)
        .map(|(k, n)| format!("{k} ({n})"))
        .collect::<Vec<_>>()
        .join(", ")
    }
}

fn jejak_kode(conn: &Connection, kode: &str) -> AppResult<JejakProduk> {
    let c = |t: &str| format!("(SELECT COUNT(*) FROM {t} WHERE KODE_BARANG = :k)");
    let sql = format!(
        "SELECT {}, {}, {}, {}, {}, {} FROM DUAL",
        c("POS_PENJUALAN_DETAIL"),
        c("POS_PEMBELIANDETAIL"),
        c("POS_PENDING_DETAIL"),
        c("POS_STOCK"),
        c("POS_STOCKOPNAME"),
        c("POS_BARANGRUSAK")
    );
    let (penjualan, pembelian, pending, stok, opname, rusak) =
        conn.query_row_as_named::<(i64, i64, i64, i64, i64, i64)>(&sql, &[("k", &kode)])?;
    Ok(JejakProduk { penjualan, pembelian, pending, stok, opname, rusak })
}

/// Riwayat pemakaian produk `id` (NotFound bila produk tidak ada).
pub fn jejak_produk(conn: &Connection, id: i64) -> AppResult<(ProdukAdmin, JejakProduk)> {
    let p = produk_admin_by_id(conn, id)?
        .ok_or_else(|| AppError::NotFound("Produk tidak ditemukan".into()))?;
    let j = jejak_kode(conn, &p.kode)?;
    Ok((p, j))
}

/// Satuan jual tambahan satu produk (urut dari isi terkecil).
pub fn satuan_produk(conn: &Connection, id: i64) -> AppResult<Vec<SatuanJual>> {
    Ok(satuan_map(conn, &[id])?.remove(&id).unwrap_or_default())
}

/// Validasi daftar satuan tambahan terhadap satuan dasar (murni; tanpa DB).
pub fn validasi_satuan(dasar: &str, daftar: &[SatuanJual]) -> AppResult<()> {
    let mut nama_ada: Vec<String> = vec![dasar.trim().to_uppercase()];
    let mut barcode_ada: Vec<&str> = Vec::new();
    if daftar.len() > 10 {
        return Err(AppError::validation("Maksimal 10 satuan tambahan"));
    }
    for s in daftar {
        let nama = s.satuan.trim();
        if nama.is_empty() || nama.chars().count() > 20 {
            return Err(AppError::validation("Nama satuan 1-20 karakter"));
        }
        let up = nama.to_uppercase();
        if nama_ada.contains(&up) {
            return Err(AppError::validation(format!("Satuan ganda atau sama dengan satuan dasar: {nama}")));
        }
        nama_ada.push(up);
        if s.isi <= Decimal::ONE || s.isi.scale() > 4 || s.isi > Decimal::from(1_000_000) {
            return Err(AppError::validation(format!("Isi {nama} harus lebih dari 1 (maks. 4 desimal)")));
        }
        if s.harga <= Decimal::ZERO || s.harga.scale() > 2 {
            return Err(AppError::validation(format!("Harga {nama} harus lebih dari 0")));
        }
        let bc = s.barcode.trim();
        if bc.chars().count() > 50 {
            return Err(AppError::validation("Barcode maksimal 50 karakter"));
        }
        if !bc.is_empty() {
            if barcode_ada.contains(&bc) {
                return Err(AppError::validation(format!("Barcode ganda: {bc}")));
            }
            barcode_ada.push(bc);
        }
    }
    Ok(())
}

// Barcode dipakai bersama dua tabel. Ambil kunci dalam urutan yang sama
// sebelum memeriksa atau menulis barcode dari jalur admin.
fn kunci_barcode(conn: &Connection) -> AppResult<()> {
    for sql in [
        "LOCK TABLE POS_PRODUCT IN SHARE ROW EXCLUSIVE MODE WAIT 5",
        "LOCK TABLE POS_PRODUCT_SATUAN IN SHARE ROW EXCLUSIVE MODE WAIT 5",
    ] {
        conn.execute(sql, &[]).map_err(|e| {
            if matches!(e.oci_code(), Some(54 | 30006)) {
                AppError::Conflict("Katalog sedang diperbarui. Coba simpan lagi.".into())
            } else {
                e.into()
            }
        })?;
    }
    Ok(())
}

fn barcode_tersedia(
    conn: &Connection,
    barcode: &str,
    kecuali_produk: Option<i64>,
    kecuali_satuan: Option<i64>,
) -> AppResult<bool> {
    if barcode.is_empty() {
        return Ok(true);
    }
    let n: i64 = conn.query_row_as_named(
        "SELECT (SELECT COUNT(*) FROM POS_PRODUCT WHERE BARCODE = :b
                   AND (:produk IS NULL OR PRODUCTID <> :produk))
              + (SELECT COUNT(*) FROM POS_PRODUCT_SATUAN WHERE BARCODE = :b
                   AND (:satuan IS NULL OR PRODUCT_ID <> :satuan))
         FROM DUAL",
        &[("b", &barcode), ("produk", &kecuali_produk), ("satuan", &kecuali_satuan)],
    )?;
    Ok(n == 0)
}

fn pastikan_barcode_satuan(
    conn: &Connection,
    daftar: &[SatuanJual],
    kecuali: Option<i64>,
) -> AppResult<()> {
    for s in daftar {
        let barcode = s.barcode.trim();
        if !barcode_tersedia(conn, barcode, None, kecuali)? {
            return Err(AppError::rule("BARCODE_SUDAH_ADA", format!("Barcode {barcode} sudah dipakai")));
        }
    }
    Ok(())
}

fn sisipkan_satuan(conn: &Connection, id: i64, daftar: &[SatuanJual]) -> AppResult<()> {
    let sql = format!(
        "INSERT INTO POS_PRODUCT_SATUAN (PRODUCT_ID, SATUAN, ISI, HARGA, BARCODE)
         VALUES (TO_NUMBER(:id), :sat, {}, {}, :bc)",
        super::num("isi"), super::num("harga")
    );
    for s in daftar {
        let mut b = Binds::default();
        b.add("id", id);
        b.add("sat", s.satuan.trim());
        b.add("isi", s.isi.normalize());
        b.add("harga", s.harga.normalize());
        b.add("bc", s.barcode.trim());
        b.with(|p| conn.execute_named(&sql, p))?;
    }
    Ok(())
}

/// Ganti seluruh satuan tambahan produk (atomik).
pub fn ganti_satuan(conn: &Connection, id: i64, daftar: &[SatuanJual]) -> AppResult<Vec<SatuanJual>> {
    let hasil = (|| -> AppResult<Vec<SatuanJual>> {
        kunci_barcode(conn)?;
        let dasar = opt(conn.query_row_as_named::<Option<String>>(
            "SELECT SATUAN FROM POS_PRODUCT WHERE PRODUCTID = :id FOR UPDATE",
            &[("id", &id)],
        ))?.ok_or_else(|| AppError::NotFound("Produk tidak ditemukan".into()))?;
        validasi_satuan(dasar.as_deref().unwrap_or_default(), daftar)?;
        let daftar = &satuan_kanonik(conn, daftar)?;
        pastikan_barcode_satuan(conn, daftar, Some(id))?;
        conn.execute_named("DELETE FROM POS_PRODUCT_SATUAN WHERE PRODUCT_ID = :id", &[("id", &id)])?;
        sisipkan_satuan(conn, id, daftar)?;
        let satuan = satuan_produk(conn, id)?;
        conn.commit()?;
        Ok(satuan)
    })();
    if hasil.is_err() {
        let _ = conn.rollback();
    }
    hasil
}

/// Hapus permanen produk yang tidak punya riwayat apa pun. Mengunci baris produk (FOR UPDATE), memeriksa ulang
/// riwayat di transaksi yang sama, lalu membersihkan tabel pendamping (foto, urutan, potongan qty). Tidak bisa dibatalkan.
pub fn hapus_produk(conn: &Connection, id: i64) -> AppResult<ProdukAdmin> {
    let hasil = (|| -> AppResult<ProdukAdmin> {
        let terkunci: Option<i64> = opt(conn.query_row_as_named::<i64>(
            "SELECT PRODUCTID FROM POS_PRODUCT WHERE PRODUCTID = :id FOR UPDATE",
            &[("id", &id)],
        ))?;
        if terkunci.is_none() {
            return Err(AppError::NotFound("Produk tidak ditemukan".into()));
        }
        let (p, j) = jejak_produk(conn, id)?;
        if !j.bisa_hapus() {
            return Err(AppError::rule(
                "PRODUK_BERTRANSAKSI",
                format!("Produk sudah punya riwayat: {}. Nonaktifkan saja.", j.rincian()),
            ));
        }
        conn.execute_named("DELETE FROM MOB_PRODUK_FOTO WHERE PRODUCT_ID = :id", &[("id", &id)])?;
        conn.execute_named("DELETE FROM MOB_PRODUK_URUT WHERE PRODUCT_ID = :id", &[("id", &id)])?;
        conn.execute_named("DELETE FROM POS_POTONGANBERDASARKANQTY WHERE KODE_ITEM = :k", &[("k", &p.kode)])?;
        conn.execute_named("DELETE FROM POS_PRODUCT_SATUAN WHERE PRODUCT_ID = :id", &[("id", &id)])?;
        conn.execute_named("DELETE FROM POS_PRODUCT WHERE PRODUCTID = :id", &[("id", &id)])?;
        conn.commit()?;
        Ok(p)
    })();
    if hasil.is_err() {
        let _ = conn.rollback(); // lepas kunci FOR UPDATE
    }
    hasil
}

/// Ubah produk. Mengembalikan (harga jual lama, produk setelah diubah). `KODE_ITEM` tidak berubah.
pub fn ubah_produk(conn: &Connection, id: i64, p: &UbahProduk) -> AppResult<(i64, ProdukAdmin)> {
    validasi_produk(p)?;
    let hasil = (|| -> AppResult<(i64, ProdukAdmin)> {
        let barcode = p.barcode.trim();
        if !barcode.is_empty() {
            kunci_barcode(conn)?;
        }
        let lama = produk_admin_by_id(conn, id)?
            .ok_or_else(|| AppError::NotFound("Produk tidak ditemukan".into()))?;
        if barcode != lama.barcode.trim() && !barcode_tersedia(conn, barcode, Some(id), None)? {
            return Err(AppError::Conflict("Barcode sudah dipakai produk lain".into()));
        }
        // Satuan lama yang tidak diubah tidak diperiksa ulang, supaya produk warisan tetap bisa disunting.
        let satuan_master = if p.satuan.trim() == lama.satuan.trim() {
            lama.satuan.trim().to_string()
        } else {
            pastikan_satuan_master(conn, &p.satuan)?
        };
        let kat = p.kategori_id.trim();
        let n: i64 = conn.query_row_as_named(
            "SELECT COUNT(*) FROM POS_KATEGORI WHERE TO_CHAR(KODE) = :k",
            &[("k", &kat)],
        )?;
        if n == 0 {
            return Err(AppError::validation("Kategori tidak dikenal"));
        }
        conn.execute_named(
            "UPDATE POS_PRODUCT SET PRODUCTNAME = :nama, BARCODE = :barcode, PRICE = :harga, \
             SATUAN = :satuan, KATEGORI_ID = :kat, BELI = :beli, AKTIF = :aktif WHERE PRODUCTID = :id",
            &[
                ("nama", &p.nama.trim()),
                ("barcode", &barcode),
                ("harga", &p.harga),
                ("satuan", &satuan_master),
                ("kat", &kat),
                ("beli", &p.beli),
                ("aktif", &if p.aktif { "Y" } else { "N" }),
                ("id", &id),
            ],
        )?;
        let baru = produk_admin_by_id(conn, id)?
            .ok_or_else(|| AppError::Internal("produk hilang setelah diubah".into()))?;
        conn.commit()?;
        Ok((lama.harga, baru))
    })();
    if hasil.is_err() {
        let _ = conn.rollback();
    }
    hasil
}

/// Aturan tambahan produk baru (sama dengan frmMasterBarang desktop): harga jual dan beli > 0, jual > beli.
pub fn validasi_buat(p: &UbahProduk) -> AppResult<()> {
    validasi_produk(p)?;
    if p.harga <= 0 || p.beli <= 0 {
        return Err(AppError::validation("Harga jual dan harga beli harus lebih dari 0"));
    }
    if p.harga <= p.beli {
        return Err(AppError::validation("Harga jual harus lebih besar dari harga beli"));
    }
    Ok(())
}

/// Sidik isian produk: kunci idempotensi yang dipakai ulang dengan isian lain ditolak.
fn hash_produk(p: &UbahProduk) -> String {
    use sha2::{Digest, Sha256};
    let s = format!(
        "{}|{}|{}|{}|{}|{}",
        p.nama.trim(),
        p.barcode.trim(),
        p.satuan.trim(),
        p.kategori_id.trim(),
        p.harga,
        p.beli
    );
    Sha256::digest(s.as_bytes()).iter().map(|b| format!("{b:02x}")).collect()
}

fn hash_produk_baru(p: &UbahProduk, daftar: &[SatuanJual]) -> String {
    use sha2::{Digest, Sha256};

    let dasar = hash_produk(p);
    if daftar.is_empty() {
        return dasar;
    }
    let mut satuan: Vec<_> = daftar
        .iter()
        .map(|s| (
            s.satuan.trim(),
            s.isi.normalize().to_string(),
            s.harga.normalize().to_string(),
            s.barcode.trim(),
        ))
        .collect();
    satuan.sort_unstable();
    let data = serde_json::json!([dasar, satuan]).to_string();
    Sha256::digest(data.as_bytes()).iter().map(|b| format!("{b:02x}")).collect()
}

/// Trigger DB desktop menolak duplikat dengan ORA-20xxx berisi `DUPLIKAT_*`: jadikan 409, bukan 500.
fn peta_duplikat(e: oracle::Error) -> AppError {
    let m = e.to_string();
    if m.contains("DUPLIKAT_NAMA_BARANG") {
        AppError::Conflict("Nama barang sudah dipakai produk lain".into())
    } else if m.contains("DUPLIKAT_BARCODE") {
        AppError::Conflict("Barcode sudah dipakai produk lain".into())
    } else if m.contains("DUPLIKAT_KODE_BARANG") {
        AppError::Conflict("Kode barang sudah dipakai, coba lagi".into())
    } else {
        e.into()
    }
}

/// Buat produk baru (kode item otomatis `KB-yy-nnnnn` dari urutan NOMOR_TRANSAKSI/BARANG yang dipakai desktop).
/// Idempoten: kunci yang sama dengan isian sama mengembalikan produk yang sudah dibuat.
pub fn buat_produk(
    conn: &Connection,
    user_id: i64,
    key: &str,
    p: &UbahProduk,
    daftar: &[SatuanJual],
) -> AppResult<ProdukAdmin> {
    validasi_buat(p)?;
    validasi_satuan(&p.satuan, daftar)?;
    let barcode = p.barcode.trim();
    if !barcode.is_empty() && daftar.iter().any(|s| s.barcode.trim() == barcode) {
        return Err(AppError::validation("Barcode satuan tambahan sama dengan barcode dasar"));
    }
    let hash = hash_produk_baru(p, daftar);
    let hasil = (|| -> AppResult<ProdukAdmin> {
        if let Some(rid) = super::opname::klaim_kunci(conn, user_id, "PRODUK_BARU", key, &hash)? {
            let id: i64 = rid
                .parse()
                .map_err(|_| AppError::Internal("resource idempotensi produk tidak valid".into()))?;
            return produk_admin_by_id(conn, id)?
                .ok_or_else(|| AppError::NotFound("Produk hasil permintaan ini sudah dihapus".into()));
        }
        let satuan_master = pastikan_satuan_master(conn, &p.satuan)?;
        let daftar = &satuan_kanonik(conn, daftar)?;
        let kat = p.kategori_id.trim();
        let n: i64 = conn.query_row_as_named(
            "SELECT COUNT(*) FROM POS_KATEGORI WHERE TO_CHAR(KODE) = :k",
            &[("k", &kat)],
        )?;
        if n == 0 {
            return Err(AppError::validation("Kategori tidak dikenal"));
        }
        kunci_barcode(conn)?;
        if !barcode_tersedia(conn, barcode, None, None)? {
            return Err(AppError::Conflict("Barcode sudah dipakai produk lain".into()));
        }
        pastikan_barcode_satuan(conn, daftar, None)?;
        let nama = p.nama.trim();
        let n: i64 = conn.query_row_as_named(
            "SELECT COUNT(*) FROM POS_PRODUCT WHERE UPPER(TRIM(PRODUCTNAME)) = UPPER(:n)",
            &[("n", &nama)],
        )?;
        if n > 0 {
            return Err(AppError::Conflict("Nama barang sudah dipakai produk lain".into()));
        }
        // Urutan kode dikunci sampai commit; cermin ke baris BARANG agar desktop melanjutkan dari sini.
        let (hari, _) = super::faktur::sekarang(conn)?;
        let terakhir: Option<String> = opt(conn.query_row_as_named::<Option<String>>(
            "SELECT NOMOR FROM NOMOR_TRANSAKSI WHERE KODE = 'BARANG' FOR UPDATE",
            &[],
        ))?
        .flatten();
        let mut kode = crate::domain::nomor::kode_produk_berikutnya(terakhir.as_deref(), chrono::Datelike::year(&hari));
        // Lewati kode yang sudah terpakai (mis. diketik manual di desktop).
        for _ in 0..50 {
            let ada: i64 = conn.query_row_as_named(
                "SELECT COUNT(*) FROM POS_PRODUCT WHERE KODE_ITEM = :k",
                &[("k", &kode)],
            )?;
            if ada == 0 {
                break;
            }
            kode = crate::domain::nomor::kode_produk_berikutnya(Some(&kode), chrono::Datelike::year(&hari));
        }
        conn.execute_named(
            "INSERT INTO POS_PRODUCT (KODE_ITEM, PRODUCTNAME, BARCODE, BELI, PRICE, SATUAN, KATEGORI_ID, AKTIF) \
             VALUES (:kode, :nama, :barcode, :beli, :harga, :satuan, :kat, 'Y')",
            &[
                ("kode", &kode),
                ("nama", &nama),
                ("barcode", &barcode),
                ("beli", &p.beli),
                ("harga", &p.harga),
                ("satuan", &satuan_master),
                ("kat", &kat),
            ],
        )
        .map_err(peta_duplikat)?;
        conn.execute_named(
            "MERGE INTO NOMOR_TRANSAKSI t USING (SELECT 'BARANG' AS KODE FROM DUAL) s ON (t.KODE = s.KODE) \
             WHEN MATCHED THEN UPDATE SET NOMOR = :n WHEN NOT MATCHED THEN INSERT (KODE, NOMOR) VALUES ('BARANG', :n)",
            &[("n", &kode)],
        )?;
        let id: i64 = conn.query_row_as_named(
            "SELECT PRODUCTID FROM POS_PRODUCT WHERE KODE_ITEM = :k",
            &[("k", &kode)],
        )?;
        sisipkan_satuan(conn, id, daftar)?;
        let baru = produk_admin_by_id(conn, id)?
            .ok_or_else(|| AppError::Internal("produk hilang setelah dibuat".into()))?;
        conn.execute_named(
            "UPDATE MOB_IDEMPOTENCY SET RESOURCE_ID = :r WHERE USER_ID = :u AND OPERASI = 'PRODUK_BARU' AND IDEMPOTENCY_KEY = :k",
            &[("r", &id.to_string()), ("u", &user_id), ("k", &key)],
        )?;
        conn.commit()?;
        Ok(baru)
    })();
    if hasil.is_err() {
        let _ = conn.rollback(); // lepas kunci FOR UPDATE dan klaim idempotensi
    }
    hasil
}

#[cfg(test)]
mod buat_tests {
    use super::*;

    fn p(harga: i64, beli: i64) -> UbahProduk {
        UbahProduk {
            nama: "Teh Botol".into(),
            barcode: "".into(),
            satuan: "PCS".into(),
            kategori_id: "1".into(),
            harga,
            beli,
            aktif: true,
        }
    }

    #[test]
    fn buat_valid() {
        assert!(validasi_buat(&p(5000, 4000)).is_ok());
    }

    #[test]
    fn buat_ditolak() {
        assert!(validasi_buat(&p(4000, 4000)).is_err(), "jual <= beli");
        assert!(validasi_buat(&p(3000, 4000)).is_err(), "jual < beli");
        assert!(validasi_buat(&p(5000, 0)).is_err(), "beli nol");
        assert!(validasi_buat(&p(0, 0)).is_err(), "harga nol");
        let mut kosong = p(5000, 4000);
        kosong.nama = "  ".into();
        assert!(validasi_buat(&kosong).is_err(), "nama kosong");
    }

    #[test]
    fn hash_produk_membedakan_isian() {
        assert_eq!(hash_produk(&p(5000, 4000)), hash_produk(&p(5000, 4000)));
        assert_ne!(hash_produk(&p(5000, 4000)), hash_produk(&p(5100, 4000)));
    }
}

#[cfg(test)]
mod jejak_tests {
    use super::*;

    #[test]
    fn rincian_hanya_memuat_yang_ada() {
        let j = JejakProduk { penjualan: 3, pembelian: 0, pending: 0, stok: 1, opname: 0, rusak: 0 };
        assert_eq!(j.rincian(), "penjualan (3), stok tahunan (1)");
        assert!(!j.bisa_hapus());
        let kosong = JejakProduk { penjualan: 0, pembelian: 0, pending: 0, stok: 0, opname: 0, rusak: 0 };
        assert!(kosong.bisa_hapus() && kosong.rincian().is_empty());
    }
}

#[cfg(test)]
mod produk_admin_tests {
    use super::*;

    fn ok() -> UbahProduk {
        UbahProduk {
            nama: "PERTALITE".into(),
            barcode: "".into(),
            satuan: "LITER".into(),
            kategori_id: "14".into(),
            harga: 18000,
            beli: 15000,
            aktif: true,
        }
    }

    #[test]
    fn produk_valid_lolos() {
        assert!(validasi_produk(&ok()).is_ok());
    }

    #[test]
    fn nama_satuan_kategori_wajib() {
        for f in [
            |p: &mut UbahProduk| p.nama = "  ".into(),
            |p: &mut UbahProduk| p.nama = "x".repeat(101),
            |p: &mut UbahProduk| p.satuan = "".into(),
            |p: &mut UbahProduk| p.satuan = "x".repeat(21),
            |p: &mut UbahProduk| p.barcode = "9".repeat(51),
            |p: &mut UbahProduk| p.kategori_id = " ".into(),
        ] {
            let mut p = ok();
            f(&mut p);
            assert!(validasi_produk(&p).is_err());
        }
    }

    #[test]
    fn harga_tidak_boleh_negatif() {
        let mut p = ok();
        p.harga = -1;
        assert!(validasi_produk(&p).is_err());
        let mut p = ok();
        p.beli = -5;
        assert!(validasi_produk(&p).is_err());
    }
}

#[cfg(test)]
mod satuan_tests {
    use super::*;

    fn sj(n: &str, isi: &str, harga: &str, bc: &str) -> SatuanJual {
        SatuanJual { satuan: n.into(), isi: isi.parse().unwrap(), harga: harga.parse().unwrap(), barcode: bc.into() }
    }

    #[test]
    fn satuan_valid() {
        assert!(validasi_satuan("PCS", &[sj("BOX", "12", "100000", "899"), sj("DUS", "48", "380000", "")]).is_ok());
    }

    #[test]
    fn satuan_ditolak() {
        assert!(validasi_satuan("PCS", &[sj("pcs", "12", "1", "")]).is_err(), "sama dengan dasar");
        assert!(validasi_satuan("PCS", &[sj("BOX", "1", "1", "")]).is_err(), "isi harus > 1");
        assert!(validasi_satuan("PCS", &[sj("BOX", "12", "0", "")]).is_err(), "harga > 0");
        assert!(validasi_satuan("PCS", &[sj("BOX", "12", "1", "a"), sj("DUS", "24", "1", "a")]).is_err(), "barcode ganda");
        assert!(validasi_satuan("PCS", &[sj("BOX", "12", "1", ""), sj("box", "24", "1", "")]).is_err(), "nama ganda");
    }
}
