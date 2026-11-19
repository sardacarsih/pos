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

/// Stok sistem per kode: stok awal tahun + pembelian + selisih opname - penjualan - barang rusak
/// (formula sama dengan GetStocItem desktop, dibuat set-based untuk satu halaman).
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
    let rng = "TO_DATE(:dari,'YYYY-MM-DD') AND TO_DATE(:sampai,'YYYY-MM-DD')";
    let sub = |t: &str, kcol: &str, qcol: &str, tgl: &str| {
        format!(
            "(SELECT {kcol} k, SUM({qcol}) q FROM {t} WHERE {tgl} BETWEEN {rng} AND {kcol} IN ({inl}) GROUP BY {kcol})"
        )
    };
    let sql = format!(
        "SELECT p.KODE_ITEM, {} FROM POS_PRODUCT p \
         LEFT JOIN (SELECT KODE_BARANG k, SUM(QUANTITY) q FROM POS_STOCK WHERE TANGGAL = TO_DATE(:dari,'YYYY-MM-DD') AND KODE_BARANG IN ({inl}) GROUP BY KODE_BARANG) st ON st.k = p.KODE_ITEM \
         LEFT JOIN {} pb ON pb.k = p.KODE_ITEM \
         LEFT JOIN {} pj ON pj.k = p.KODE_ITEM \
         LEFT JOIN {} rs ON rs.k = p.KODE_ITEM \
         LEFT JOIN {} so ON so.k = p.KODE_ITEM \
         WHERE p.KODE_ITEM IN ({inl})",
        tm("NVL(st.q,0) + NVL(pb.q,0) + NVL(so.q,0) - (NVL(pj.q,0) + NVL(rs.q,0))"),
        sub(
            "POS_PEMBELIANDETAIL d JOIN POS_PEMBELIAN m ON m.PURCHASE_ID = d.PURCHASE_ID",
            "d.KODE_BARANG",
            "d.QUANTITY",
            "m.TANGGAL"
        ),
        sub(
            "POS_PENJUALAN_DETAIL d JOIN POS_PENJUALAN m ON m.NO_TRANSAKSI = d.NO_TRANSAKSI",
            "d.KODE_BARANG",
            "d.JUMLAH_BARANG",
            "m.TANGGAL"
        ),
        sub("POS_BARANGRUSAK", "KODE_BARANG", "JUMLAHFISIK", "TANGGAL"),
        sub("POS_STOCKOPNAME", "KODE_BARANG", "SELISIH", "TANGGAL"),
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
pub fn daftar_produk_admin(
    conn: &Connection,
    q: Option<&str>,
    kategori: Option<&str>,
    aktif: Option<&str>,
    page: u32,
    size: u32,
) -> AppResult<Halaman<ProdukAdmin>> {
    let mut b = Binds::default();
    let mut sql = format!("SELECT {COLS_ADMIN} {FROM} WHERE 1 = 1");
    if let Some(q) = q.filter(|q| !q.trim().is_empty()) {
        b.add("q", like(q));
        b.add("qraw", q.trim());
        sql.push_str(" AND (UPPER(p.PRODUCTNAME) LIKE :q ESCAPE '\\' OR UPPER(p.KODE_ITEM) LIKE :q ESCAPE '\\' OR p.BARCODE = :qraw)");
    }
    if let Some(k) = kategori.filter(|k| !k.trim().is_empty()) {
        b.add("kat", k.trim());
        sql.push_str(" AND TO_CHAR(p.KATEGORI_ID) = :kat");
    }
    match aktif {
        Some("Y") => sql.push_str(" AND p.AKTIF = 'Y'"),
        Some("N") => sql.push_str(" AND NVL(p.AKTIF, 'N') <> 'Y'"),
        _ => {}
    }
    b.add("off", (page.saturating_sub(1) * size).to_string());
    b.add("lim", (size + 1).to_string());
    sql.push_str(" ORDER BY NVL(u.URUT, 999999999), p.PRODUCTNAME, p.PRODUCTID OFFSET TO_NUMBER(:off) ROWS FETCH NEXT TO_NUMBER(:lim) ROWS ONLY");
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

/// Ubah produk. Mengembalikan (harga jual lama, produk setelah diubah). `KODE_ITEM` tidak berubah.
pub fn ubah_produk(conn: &Connection, id: i64, p: &UbahProduk) -> AppResult<(i64, ProdukAdmin)> {
    validasi_produk(p)?;
    let lama = produk_admin_by_id(conn, id)?
        .ok_or_else(|| AppError::NotFound("Produk tidak ditemukan".into()))?;
    let barcode = p.barcode.trim();
    if !barcode.is_empty() {
        let n: i64 = conn.query_row_as_named(
            "SELECT COUNT(*) FROM POS_PRODUCT WHERE BARCODE = :b AND PRODUCTID <> :id",
            &[("b", &barcode), ("id", &id)],
        )?;
        if n > 0 {
            return Err(AppError::Conflict(
                "Barcode sudah dipakai produk lain".into(),
            ));
        }
    }
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
            ("satuan", &p.satuan.trim()),
            ("kat", &kat),
            ("beli", &p.beli),
            ("aktif", &if p.aktif { "Y" } else { "N" }),
            ("id", &id),
        ],
    )?;
    conn.commit()?;
    let baru = produk_admin_by_id(conn, id)?
        .ok_or_else(|| AppError::Internal("produk hilang setelah diubah".into()))?;
    Ok((lama.harga, baru))
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
