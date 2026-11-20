//! Persediaan: penyesuaian stok manual berjejak (MOB_STOK_SESUAI), kartu stok/mutasi, valuasi per kategori,
//! dan ambang stok minimum. POS_STOCK adalah snapshot saldo awal tahun (dibaca hanya pada TANGGAL = 1 Jan),
//! jadi efek penyesuaian DILIPAT ke baris 1 Jan tahun berjalan (UPDATE, INSERT bila belum ada) agar rumus stok
//! desktop & web tetap satu sumber; tanggal asli/alasan/operator tercatat di audit. Balik menulis baris
//! pembalik (qty dinegasikan, BALIK_ID terisi) + delta lawan — bukan menghapus.
use chrono::{Datelike, NaiveDate};
use oracle::Connection;
use rust_decimal::Decimal;
use serde::{Deserialize, Serialize};
use sha2::{Digest, Sha256};

use super::{Binds, dec, katalog::{self, Halaman, STOK_EXPR, stok_join}, like, num, opt, tm, opname};
use crate::error::{AppError, AppResult};

const OPERASI: &str = "SESUAI";
const OPERASI_BALIK: &str = "SESUAI_BALIK";
const HPP_EXPR: &str = "NVL(p.BELI, 0)";
/// 99.999.999,9999 — batas |qty| penyesuaian (scale maks 4).
const MAKS_QTY: Decimal = rust_decimal::dec!(99999999.9999);

#[derive(Debug, Clone, Deserialize)]
#[serde(deny_unknown_fields)]
pub struct SesuaiReq {
    pub produk_id: i64,
    pub qty: Decimal,
    pub tanggal: String,
    pub alasan: String,
}

#[derive(Debug, Serialize)]
pub struct HasilSesuai {
    pub id: i64,
    pub kode: String,
    pub tanggal: String,
    pub qty: Decimal,
    pub saldo_sesudah: Decimal,
    /// Terisi bila saldo sistem menjadi negatif (penyesuaian minus tetap boleh).
    pub peringatan: Option<String>,
    /// True bila permintaan ini pengulangan (Idempotency-Key sama): tidak ada baris audit baru.
    pub ulang: bool,
}

#[derive(Debug, Serialize)]
pub struct Sesuai {
    pub id: i64,
    pub kode: String,
    pub nama: String,
    pub tanggal: String,
    pub qty: Decimal,
    pub alasan: String,
    pub user_nama: Option<String>,
    pub balik_id: Option<i64>,
    pub dibalik: bool,
    pub created_at: String,
}

#[derive(Debug, Serialize)]
pub struct ProdukKartu {
    pub id: i64,
    pub kode: String,
    pub nama: String,
    pub satuan: String,
}

#[derive(Debug, Serialize)]
pub struct Mutasi {
    pub tanggal: String,
    pub jenis: String,
    #[serde(rename = "ref")]
    pub referensi: String,
    pub qty: Decimal,
    pub saldo: Decimal,
}

#[derive(Debug, Serialize)]
pub struct KartuStok {
    pub produk: ProdukKartu,
    pub dari: String,
    pub sampai: String,
    pub saldo_awal: Decimal,
    pub saldo_akhir: Decimal,
    pub items: Vec<Mutasi>,
    pub ada_lagi: bool,
    pub page: u32,
}

#[derive(Debug, Serialize)]
pub struct KategoriValuasi {
    pub kategori: String,
    pub jumlah_produk: i64,
    pub total_qty: Decimal,
    pub nilai: Decimal,
    pub tanpa_hpp: i64,
}

#[derive(Debug, Serialize)]
pub struct Valuasi {
    pub total_nilai: Decimal,
    pub total_produk: i64,
    pub tanpa_hpp: i64,
    pub jumlah_stok_minim: i64,
    pub per_kategori: Vec<KategoriValuasi>,
}

#[derive(Debug, Serialize)]
pub struct ValuasiItem {
    pub id: i64,
    pub kode: String,
    pub nama: String,
    pub kategori: String,
    pub satuan: String,
    pub stok: Decimal,
    pub hpp: Decimal,
    pub nilai: Decimal,
    pub tanpa_hpp: bool,
}

#[derive(Debug, Serialize)]
pub struct StokMin {
    pub id: i64,
    pub kode: String,
    pub nama: String,
    pub satuan: String,
    pub kategori: String,
    pub stok: Decimal,
    pub stok_min: Decimal,
}

pub fn validasi_qty(qty: Decimal) -> AppResult<()> {
    if qty.is_zero() {
        return Err(AppError::validation("Qty penyesuaian tidak boleh nol"));
    }
    if qty.abs() > MAKS_QTY || qty.scale() > 4 {
        return Err(AppError::validation("Qty penyesuaian tidak valid"));
    }
    Ok(())
}

pub fn alasan_bersih(alasan: &str) -> AppResult<String> {
    let a = alasan.trim();
    if !(3..=250).contains(&a.chars().count()) {
        return Err(AppError::validation("Alasan wajib diisi (3-250 karakter)"));
    }
    Ok(a.to_string())
}

pub fn tanggal_sesuai(tanggal: &str, hari: NaiveDate) -> AppResult<NaiveDate> {
    let t = NaiveDate::parse_from_str(tanggal.trim(), "%Y-%m-%d")
        .map_err(|_| AppError::validation("Tanggal tidak valid (YYYY-MM-DD)"))?;
    let awal = NaiveDate::from_ymd_opt(hari.year(), 1, 1).expect("1 Jan");
    if t < awal || t > hari {
        return Err(AppError::validation(
            "Tanggal penyesuaian harus dalam tahun berjalan sampai hari ini",
        ));
    }
    Ok(t)
}

/// Rentang kartu stok: default 1 Jan..hari ini, di-clamp ke tahun berjalan; sampai >= dari.
pub fn rentang_kartu(dari: Option<NaiveDate>, sampai: Option<NaiveDate>, hari: NaiveDate) -> AppResult<(NaiveDate, NaiveDate)> {
    let awal = NaiveDate::from_ymd_opt(hari.year(), 1, 1).expect("1 Jan");
    let d = dari.unwrap_or(awal).clamp(awal, hari);
    let s = sampai.unwrap_or(hari).clamp(awal, hari);
    if s < d {
        return Err(AppError::validation("Rentang tanggal terbalik"));
    }
    Ok((d, s))
}

fn hex(d: impl AsRef<[u8]>) -> String {
    d.as_ref().iter().map(|b| format!("{b:02x}")).collect()
}

fn hash_sesuai(req: &SesuaiReq, alasan: &str, tanggal: NaiveDate) -> String {
    hex(Sha256::digest(
        format!("{}|{}|{}|{alasan}", req.produk_id, req.qty.normalize(), tanggal).as_bytes(),
    ))
}

fn hash_balik(id: i64, alasan: &str) -> String {
    hex(Sha256::digest(format!("{id}|{alasan}").as_bytes()))
}

fn peringatan_saldo(saldo: Decimal) -> Option<String> {
    (saldo < Decimal::ZERO).then(|| format!("Stok sistem menjadi negatif ({saldo})"))
}

fn harga_nol_ditolak(nama: &str) -> AppError {
    AppError::validation(format!(
        "Harga beli \"{nama}\" belum diisi; isi di Katalog produk sebelum menambah stok lewat penyesuaian"
    ))
}

fn saldo_sistem(conn: &Connection, kode: &str, awal: NaiveDate, hari: NaiveDate) -> AppResult<Decimal> {
    let kodes = [kode.to_string()];
    Ok(katalog::stok_map(conn, &kodes, awal, hari)?
        .get(kode)
        .copied()
        .unwrap_or_default())
}

/// Lipat delta ke baris saldo awal 1 Jan (satu-satunya baris POS_STOCK yang dibaca rumus stok).
/// Bila produk belum punya baris, INSERT dengan daftar kolom eksplisit (PRODUCT_ID ikut, HPP = BELI).
fn lipat_ke_saldo_awal(conn: &Connection, kode: &str, produk_id: i64, delta: Decimal, hpp: Decimal, awal: NaiveDate) -> AppResult<()> {
    let tgl = awal.format("%Y-%m-%d").to_string();
    let n = conn.execute_named(
        &format!(
            "UPDATE POS_STOCK SET QUANTITY = NVL(QUANTITY, 0) + {} WHERE KODE_BARANG = :kode AND TANGGAL = TO_DATE(:tgl,'YYYY-MM-DD')",
            num("delta")
        ),
        &[("delta", &delta.to_string()), ("kode", &kode), ("tgl", &tgl)],
    )?.row_count()?;
    match n {
        0 => {
            conn.execute_named(
                &format!(
                    "INSERT INTO POS_STOCK (PRODUCT_ID, KODE_BARANG, TANGGAL, QUANTITY, HPP) \
                     VALUES (:pid, :kode, TO_DATE(:tgl,'YYYY-MM-DD'), {}, {})",
                    num("delta"), num("hpp")
                ),
                &[("pid", &produk_id), ("kode", &kode), ("tgl", &tgl), ("delta", &delta.to_string()), ("hpp", &hpp.to_string())],
            )?;
        }
        1 => {}
        _ => return Err(AppError::Internal(format!("baris saldo awal ganda untuk {kode}"))),
    }
    Ok(())
}

#[allow(clippy::too_many_arguments)]
fn tulis_audit(conn: &Connection, kode: &str, tanggal: NaiveDate, qty: Decimal, alasan: &str, user_id: i64, user_nama: &str, balik_id: Option<i64>) -> AppResult<i64> {
    let stmt = conn.execute_named(
        &format!(
            "INSERT INTO MOB_STOK_SESUAI (KODE_BARANG, TANGGAL, QTY, ALASAN, USER_ID, USER_NAMA, BALIK_ID) \
             VALUES (:kode, TO_DATE(:tgl,'YYYY-MM-DD'), {}, :alasan, :u, :nama, :b) RETURNING ID INTO :id",
            num("qty")
        ),
        &[
            ("kode", &kode), ("tgl", &tanggal.format("%Y-%m-%d").to_string()), ("qty", &qty.to_string()),
            ("alasan", &alasan), ("u", &user_id), ("nama", &user_nama), ("b", &balik_id), ("id", &None::<i64>),
        ],
    )?;
    stmt.returned_values("id")?
        .into_iter()
        .next()
        .ok_or_else(|| AppError::Internal("Oracle tidak mengembalikan ID penyesuaian".into()))
}

fn selesai(conn: &Connection, user_id: i64, operasi: &str, key: &str, id: i64) -> AppResult<()> {
    conn.execute_named(
        "UPDATE MOB_IDEMPOTENCY SET RESOURCE_ID = :n WHERE USER_ID = :u AND OPERASI = :o AND IDEMPOTENCY_KEY = :k",
        &[("n", &id.to_string()), ("u", &user_id), ("o", &operasi), ("k", &key)],
    )?;
    conn.commit()?;
    Ok(())
}

/// Baca ulang hasil dari baris audit (untuk replay). Saldo dihitung ulang dari stok sistem terkini,
/// bukan disimpan — saldo bisa berubah oleh transaksi lain setelah penyesuaian.
fn baca_hasil(conn: &Connection, id: i64, hari: NaiveDate, ulang: bool) -> AppResult<HasilSesuai> {
    let sql = format!(
        "SELECT KODE_BARANG, TO_CHAR(TANGGAL, 'YYYY-MM-DD'), {} FROM MOB_STOK_SESUAI WHERE ID = :id",
        tm("QTY")
    );
    let r = opt(conn.query_row_named(&sql, &[("id", &id)]))?
        .ok_or_else(|| AppError::Internal("baris penyesuaian untuk kunci idempotensi hilang".into()))?;
    let kode: String = r.get(0)?;
    let awal = NaiveDate::from_ymd_opt(hari.year(), 1, 1).expect("1 Jan");
    let saldo = saldo_sistem(conn, &kode, awal, hari)?;
    Ok(HasilSesuai {
        id,
        kode,
        tanggal: r.get(1)?,
        qty: dec(&r.get::<_, String>(2)?)?,
        saldo_sesudah: saldo,
        peringatan: peringatan_saldo(saldo),
        ulang,
    })
}

/// Penyesuaian stok manual. Commit di sini; rollback bila gagal. Baris produk dikunci (FOR UPDATE)
/// sebelum stok dibaca sehingga serial dengan opname/pembelian atas produk yang sama.
pub fn sesuaikan(conn: &Connection, user_id: i64, user_nama: &str, key: &str, req: &SesuaiReq, hari: NaiveDate) -> AppResult<HasilSesuai> {
    let r = sesuai_inner(conn, user_id, user_nama, key, req, hari);
    if r.is_err() {
        let _ = conn.rollback();
    }
    r
}

fn sesuai_inner(conn: &Connection, user_id: i64, user_nama: &str, key: &str, req: &SesuaiReq, hari: NaiveDate) -> AppResult<HasilSesuai> {
    validasi_qty(req.qty)?;
    let alasan = alasan_bersih(&req.alasan)?;
    let tanggal = tanggal_sesuai(&req.tanggal, hari)?;
    if let Some(id_str) = opname::klaim_kunci(conn, user_id, OPERASI, key, &hash_sesuai(req, &alasan, tanggal))? {
        let id: i64 = id_str.parse().map_err(|_| AppError::Internal("RESOURCE_ID penyesuaian tidak valid".into()))?;
        return baca_hasil(conn, id, hari, true);
    }
    let sql = format!(
        "SELECT KODE_ITEM, {}, AKTIF, NVL(PRODUCTNAME, KODE_ITEM) FROM POS_PRODUCT WHERE PRODUCTID = :id FOR UPDATE",
        tm("NVL(BELI, 0)")
    );
    let p = opt(conn.query_row_named(&sql, &[("id", &req.produk_id)]))?
        .ok_or_else(|| AppError::NotFound("Produk tidak ditemukan".into()))?;
    let kode: String = p.get(0)?;
    let beli = dec(&p.get::<_, String>(1)?)?;
    if !p.get::<_, Option<String>>(2)?.is_some_and(|a| a.trim().eq_ignore_ascii_case("Y")) {
        return Err(AppError::validation("Produk tidak aktif"));
    }
    let nama: String = p.get(3)?;
    if req.qty > Decimal::ZERO && beli.is_zero() {
        return Err(harga_nol_ditolak(&nama));
    }
    let awal = NaiveDate::from_ymd_opt(hari.year(), 1, 1).expect("1 Jan");
    let sesudah = saldo_sistem(conn, &kode, awal, hari)? + req.qty;
    lipat_ke_saldo_awal(conn, &kode, req.produk_id, req.qty, beli, awal)?;
    let id = tulis_audit(conn, &kode, tanggal, req.qty, &alasan, user_id, user_nama, None)?;
    selesai(conn, user_id, OPERASI, key, id)?;
    Ok(HasilSesuai {
        id,
        kode,
        tanggal: tanggal.format("%Y-%m-%d").to_string(),
        qty: req.qty,
        saldo_sesudah: sesudah,
        peringatan: peringatan_saldo(sesudah),
        ulang: false,
    })
}

/// Balik satu penyesuaian: baris pembalik (tanggal hari ini) + delta lawan ke saldo awal tahun berjalan.
/// Ditolak: balik ganda (Conflict), balik-atas-balik & lintas tahun (Validation).
pub fn balik(conn: &Connection, user_id: i64, user_nama: &str, key: &str, id: i64, alasan: &str, hari: NaiveDate) -> AppResult<HasilSesuai> {
    let r = balik_inner(conn, user_id, user_nama, key, id, alasan, hari);
    if r.is_err() {
        let _ = conn.rollback();
    }
    r
}

fn balik_inner(conn: &Connection, user_id: i64, user_nama: &str, key: &str, id: i64, alasan: &str, hari: NaiveDate) -> AppResult<HasilSesuai> {
    let alasan = alasan_bersih(alasan)?;
    if let Some(id_str) = opname::klaim_kunci(conn, user_id, OPERASI_BALIK, key, &hash_balik(id, &alasan))? {
        let rid: i64 = id_str.parse().map_err(|_| AppError::Internal("RESOURCE_ID pembalikan tidak valid".into()))?;
        return baca_hasil(conn, rid, hari, true);
    }
    let kode: String = opt(conn.query_row_named(
        "SELECT KODE_BARANG FROM MOB_STOK_SESUAI WHERE ID = :id",
        &[("id", &id)],
    ))?
    .ok_or_else(|| AppError::NotFound("Penyesuaian tidak ditemukan".into()))?
    .get(0)?;
    // Kunci produk dulu (serial dengan penyesuaian/opname lain), baru periksa guard SETELAH kunci.
    let p = opt(conn.query_row_named(
        &format!("SELECT PRODUCTID, {} FROM POS_PRODUCT WHERE KODE_ITEM = :kode FOR UPDATE", tm("NVL(BELI, 0)")),
        &[("kode", &kode)],
    ))?
    .ok_or_else(|| AppError::NotFound("Produk tidak ditemukan".into()))?;
    let produk_id: i64 = p.get(0)?;
    let beli = dec(&p.get::<_, String>(1)?)?;
    let sql = format!(
        "SELECT BALIK_ID, EXTRACT(YEAR FROM TANGGAL), \
                (SELECT COUNT(*) FROM MOB_STOK_SESUAI b WHERE b.BALIK_ID = s.ID), {} \
         FROM MOB_STOK_SESUAI s WHERE ID = :id",
        tm("QTY")
    );
    let g = conn.query_row_named(&sql, &[("id", &id)])?;
    if g.get::<_, Option<i64>>(0)?.is_some() {
        return Err(AppError::validation("Baris pembalikan tidak bisa dibalik lagi"));
    }
    if g.get::<_, i64>(1)? != hari.year() as i64 {
        return Err(AppError::validation(
            "Penyesuaian tahun lalu tidak bisa dibalik; efeknya sudah terlipat di saldo awal tahun itu — buat penyesuaian baru",
        ));
    }
    if g.get::<_, i64>(2)? > 0 {
        return Err(AppError::Conflict("Penyesuaian sudah dibalik".into()));
    }
    let qty = dec(&g.get::<_, String>(3)?)?;
    let awal = NaiveDate::from_ymd_opt(hari.year(), 1, 1).expect("1 Jan");
    let delta = -qty;
    let sesudah = saldo_sistem(conn, &kode, awal, hari)? + delta;
    lipat_ke_saldo_awal(conn, &kode, produk_id, delta, beli, awal)?;
    let rid = tulis_audit(conn, &kode, hari, delta, &alasan, user_id, user_nama, Some(id))?;
    selesai(conn, user_id, OPERASI_BALIK, key, rid)?;
    Ok(HasilSesuai {
        id: rid,
        kode,
        tanggal: hari.format("%Y-%m-%d").to_string(),
        qty: delta,
        saldo_sesudah: sesudah,
        peringatan: peringatan_saldo(sesudah),
        ulang: false,
    })
}

/// Daftar audit penyesuaian, terbaru dulu (q mencari kode/nama produk).
pub fn daftar_sesuai(conn: &Connection, q: Option<&str>, page: u32, size: u32) -> AppResult<Halaman<Sesuai>> {
    let mut b = Binds::default();
    let mut where_sql = String::from("WHERE 1 = 1");
    if let Some(q) = q.filter(|q| !q.trim().is_empty()) {
        b.add("q", like(q));
        where_sql.push_str(" AND (UPPER(s.KODE_BARANG) LIKE :q ESCAPE '\\' OR UPPER(NVL(p.PRODUCTNAME, s.KODE_BARANG)) LIKE :q ESCAPE '\\')");
    }
    b.add("off", (page.saturating_sub(1) * size).to_string());
    b.add("lim", (size + 1).to_string());
    let sql = format!(
        "SELECT s.ID, s.KODE_BARANG, NVL(p.PRODUCTNAME, s.KODE_BARANG), TO_CHAR(s.TANGGAL, 'YYYY-MM-DD'), {}, s.ALASAN, s.USER_NAMA, s.BALIK_ID, \
                CASE WHEN EXISTS (SELECT 1 FROM MOB_STOK_SESUAI b WHERE b.BALIK_ID = s.ID) THEN 1 ELSE 0 END, \
                TO_CHAR(s.CREATED_AT, 'YYYY-MM-DD HH24:MI:SS') \
         FROM MOB_STOK_SESUAI s LEFT JOIN POS_PRODUCT p ON p.KODE_ITEM = s.KODE_BARANG {where_sql} \
         ORDER BY s.ID DESC OFFSET TO_NUMBER(:off) ROWS FETCH NEXT TO_NUMBER(:lim) ROWS ONLY",
        tm("s.QTY")
    );
    let mut items = Vec::new();
    b.with(|p| -> AppResult<()> {
        for r in conn.query_named(&sql, p)? {
            let r = r?;
            items.push(Sesuai {
                id: r.get(0)?,
                kode: r.get(1)?,
                nama: r.get(2)?,
                tanggal: r.get(3)?,
                qty: dec(&r.get::<_, String>(4)?)?,
                alasan: r.get(5)?,
                user_nama: r.get(6)?,
                balik_id: r.get(7)?,
                dibalik: r.get::<_, i64>(8)? == 1,
                created_at: r.get(9)?,
            });
        }
        Ok(())
    })?;
    let ada_lagi = items.len() > size as usize;
    items.truncate(size as usize);
    Ok(Halaman { items, ada_lagi })
}

/// CTE semua mutasi satu produk dalam tahun berjalan (1 Jan..hari). Alias `referensi` (bukan `ref` — reserved).
fn cte_mutasi() -> &'static str {
    "WITH mutasi AS ( \
       SELECT m.TANGGAL tgl, 'pembelian' jenis, NVL(m.NO_TRANSAKSI, 'BELI-' || TO_CHAR(m.PURCHASE_ID)) referensi, NVL(d.QUANTITY, 0) qty \
       FROM POS_PEMBELIANDETAIL d JOIN POS_PEMBELIAN m ON m.PURCHASE_ID = d.PURCHASE_ID \
       WHERE d.KODE_BARANG = :kode AND m.TANGGAL BETWEEN TO_DATE(:awal,'YYYY-MM-DD') AND TO_DATE(:hari,'YYYY-MM-DD') \
       UNION ALL \
       SELECT m.TANGGAL, 'penjualan', m.NO_TRANSAKSI, -(NVL(d.JUMLAH_BARANG, 0) * NVL(d.ISI, 1)) \
       FROM POS_PENJUALAN_DETAIL d JOIN POS_PENJUALAN m ON m.NO_TRANSAKSI = d.NO_TRANSAKSI \
       WHERE d.KODE_BARANG = :kode AND m.TANGGAL BETWEEN TO_DATE(:awal,'YYYY-MM-DD') AND TO_DATE(:hari,'YYYY-MM-DD') \
       UNION ALL \
       SELECT TANGGAL, 'opname', NOMOR_SO, NVL(SELISIH, 0) FROM POS_STOCKOPNAME \
       WHERE KODE_BARANG = :kode AND TANGGAL BETWEEN TO_DATE(:awal,'YYYY-MM-DD') AND TO_DATE(:hari,'YYYY-MM-DD') \
       UNION ALL \
       SELECT TANGGAL, 'rusak', NOMOR_SO, -NVL(JUMLAHFISIK, 0) FROM POS_BARANGRUSAK \
       WHERE KODE_BARANG = :kode AND TANGGAL BETWEEN TO_DATE(:awal,'YYYY-MM-DD') AND TO_DATE(:hari,'YYYY-MM-DD') \
       UNION ALL \
       SELECT TANGGAL, CASE WHEN BALIK_ID IS NULL THEN 'penyesuaian' ELSE 'penyesuaian_balik' END, 'SESUAI-' || TO_CHAR(ID), QTY \
       FROM MOB_STOK_SESUAI \
       WHERE KODE_BARANG = :kode AND TANGGAL BETWEEN TO_DATE(:awal,'YYYY-MM-DD') AND TO_DATE(:hari,'YYYY-MM-DD'))"
}

/// Kartu stok: mutasi bertanggal + saldo berjalan. Saldo awal murni = SUM(POS_STOCK@1Jan) - SUM(net penyesuaian
/// tahun berjalan) karena efek penyesuaian terlipat di saldo awal; saldo_akhir wajib sama dengan stok sistem
/// yang dipakai view lama (katalog::stok_map). Saldo berjalan dihitung analytic atas rentang penuh SEBELUM paginasi.
pub fn kartu_stok(conn: &Connection, produk_id: i64, dari: Option<NaiveDate>, sampai: Option<NaiveDate>, hari: NaiveDate, page: u32, size: u32) -> AppResult<KartuStok> {
    let (dari, sampai) = rentang_kartu(dari, sampai, hari)?;
    let awal = NaiveDate::from_ymd_opt(hari.year(), 1, 1).expect("1 Jan");
    let p = opt(conn.query_row_named(
        "SELECT KODE_ITEM, NVL(PRODUCTNAME, KODE_ITEM), NVL(SATUAN, '') FROM POS_PRODUCT WHERE PRODUCTID = :id",
        &[("id", &produk_id)],
    ))?
    .ok_or_else(|| AppError::NotFound("Produk tidak ditemukan".into()))?;
    let produk = ProdukKartu { id: produk_id, kode: p.get(0)?, nama: p.get(1)?, satuan: p.get(2)? };
    let cte = cte_mutasi();
    let fmt = |d: NaiveDate| d.format("%Y-%m-%d").to_string();

    // Ringkasan: saldo awal murni + jumlah mutasi sebelum `dari` dan sampai `sampai`.
    // Oracle menolak subquery skalar bercampur fungsi grup (ORA-00937): murni dihitung terpisah.
    let murni_expr = "NVL((SELECT SUM(QUANTITY) FROM POS_STOCK WHERE KODE_BARANG = :kode AND TANGGAL = TO_DATE(:awal,'YYYY-MM-DD')), 0) \
                      - NVL((SELECT SUM(QTY) FROM MOB_STOK_SESUAI WHERE KODE_BARANG = :kode AND TANGGAL BETWEEN TO_DATE(:awal,'YYYY-MM-DD') AND TO_DATE(:hari,'YYYY-MM-DD')), 0)";
    let mut b = Binds::default();
    b.add("kode", &produk.kode);
    b.add("awal", fmt(awal));
    b.add("hari", fmt(hari));
    let r = b.with(|p| conn.query_row_named(&format!("SELECT {} FROM DUAL", tm(murni_expr)), p))?;
    let murni = dec(&r.get::<_, String>(0)?)?;
    let sql = format!(
        "{cte} SELECT {}, {} FROM mutasi",
        tm("NVL(SUM(CASE WHEN tgl < TO_DATE(:dari,'YYYY-MM-DD') THEN qty END), 0)"),
        tm("NVL(SUM(CASE WHEN tgl <= TO_DATE(:sampai,'YYYY-MM-DD') THEN qty END), 0)")
    );
    let mut b = Binds::default();
    b.add("kode", &produk.kode);
    b.add("awal", fmt(awal));
    b.add("hari", fmt(hari));
    b.add("dari", fmt(dari));
    b.add("sampai", fmt(sampai));
    let r = b.with(|p| conn.query_row_named(&sql, p))?;
    let saldo_awal = murni + dec(&r.get::<_, String>(0)?)?;
    let saldo_akhir = murni + dec(&r.get::<_, String>(1)?)?;

    // Baris: analytic atas tahun penuh, baru filter rentang + OFFSET/FETCH (saldo nyambung antar halaman).
    let sql = format!(
        "{cte}, jalan AS (SELECT tgl, jenis, referensi, qty, SUM(qty) OVER (ORDER BY tgl, referensi ROWS UNBOUNDED PRECEDING) saldo FROM mutasi) \
         SELECT TO_CHAR(j.tgl, 'YYYY-MM-DD'), j.jenis, j.referensi, {}, {} FROM jalan j \
         WHERE j.tgl BETWEEN TO_DATE(:dari,'YYYY-MM-DD') AND TO_DATE(:sampai,'YYYY-MM-DD') \
         ORDER BY j.tgl, j.referensi OFFSET TO_NUMBER(:off) ROWS FETCH NEXT TO_NUMBER(:lim) ROWS ONLY",
        tm("j.qty"),
        tm(&format!("{} + j.saldo", num("murni")))
    );
    let mut b = Binds::default();
    b.add("kode", &produk.kode);
    b.add("awal", fmt(awal));
    b.add("hari", fmt(hari));
    b.add("murni", murni.to_string());
    b.add("dari", fmt(dari));
    b.add("sampai", fmt(sampai));
    b.add("off", (page.saturating_sub(1) * size).to_string());
    b.add("lim", (size + 1).to_string());
    let mut items = Vec::new();
    b.with(|p| -> AppResult<()> {
        for r in conn.query_named(&sql, p)? {
            let r = r?;
            items.push(Mutasi {
                tanggal: r.get(0)?,
                jenis: r.get(1)?,
                referensi: r.get(2)?,
                qty: dec(&r.get::<_, String>(3)?)?,
                saldo: dec(&r.get::<_, String>(4)?)?,
            });
        }
        Ok(())
    })?;
    let ada_lagi = items.len() > size as usize;
    items.truncate(size as usize);
    Ok(KartuStok { produk, dari: fmt(dari), sampai: fmt(sampai), saldo_awal, saldo_akhir, items, ada_lagi, page })
}

/// Valuasi persediaan: SUM(stok sistem x BELI) per kategori. BELI 0/NULL dihitung 0 dan ditandai tanpa_hpp.
pub fn valuasi(conn: &Connection, kategori: Option<&str>, dari: NaiveDate, sampai: NaiveDate) -> AppResult<Valuasi> {
    let mut b = Binds::default();
    b.add("dari", dari.format("%Y-%m-%d"));
    b.add("sampai", sampai.format("%Y-%m-%d"));
    let mut where_sql = String::from("WHERE p.AKTIF = 'Y'");
    if let Some(k) = kategori.filter(|k| !k.trim().is_empty()) {
        b.add("kat", k.trim());
        where_sql.push_str(" AND TO_CHAR(p.KATEGORI_ID) = :kat");
    }
    let sql = format!(
        "SELECT NVL(k.KATEGORI, 'Tanpa kategori'), COUNT(*), {}, {}, \
                SUM(CASE WHEN {HPP_EXPR} = 0 THEN 1 ELSE 0 END), \
                SUM(CASE WHEN p.STOK_MIN IS NOT NULL AND {STOK_EXPR} < p.STOK_MIN THEN 1 ELSE 0 END) \
         FROM POS_PRODUCT p LEFT JOIN POS_KATEGORI k ON TO_CHAR(k.KODE) = TO_CHAR(p.KATEGORI_ID) {} {where_sql} \
         GROUP BY NVL(k.KATEGORI, 'Tanpa kategori') ORDER BY 1",
        tm(&format!("SUM({STOK_EXPR})")),
        tm(&format!("SUM(({STOK_EXPR}) * {HPP_EXPR})")),
        stok_join(None)
    );
    let mut per_kategori = Vec::new();
    let mut v = Valuasi { total_nilai: Decimal::ZERO, total_produk: 0, tanpa_hpp: 0, jumlah_stok_minim: 0, per_kategori: Vec::new() };
    b.with(|p| -> AppResult<()> {
        for r in conn.query_named(&sql, p)? {
            let r = r?;
            let jumlah_produk: i64 = r.get(1)?;
            let total_qty = dec(&r.get::<_, String>(2)?)?;
            let nilai = dec(&r.get::<_, String>(3)?)?;
            let tanpa_hpp: i64 = r.get(4)?;
            let minim: i64 = r.get(5)?;
            v.total_produk += jumlah_produk;
            v.total_nilai += nilai;
            v.tanpa_hpp += tanpa_hpp;
            v.jumlah_stok_minim += minim;
            per_kategori.push(KategoriValuasi { kategori: r.get(0)?, jumlah_produk, total_qty, nilai, tanpa_hpp });
        }
        Ok(())
    })?;
    v.per_kategori = per_kategori;
    Ok(v)
}

/// Rincian valuasi produk aktif dengan periode saldo dan HPP yang sama dengan ringkasan.
pub fn valuasi_item(conn: &Connection, q: Option<&str>, dari: NaiveDate, sampai: NaiveDate, page: u32, size: u32) -> AppResult<Halaman<ValuasiItem>> {
    let mut b = Binds::default();
    b.add("dari", dari.format("%Y-%m-%d"));
    b.add("sampai", sampai.format("%Y-%m-%d"));
    let mut where_sql = String::from("WHERE p.AKTIF = 'Y'");
    if let Some(q) = q.map(str::trim).filter(|q| !q.is_empty()) {
        b.add("q", like(q));
        b.add("qraw", q);
        where_sql.push_str(" AND (UPPER(p.PRODUCTNAME) LIKE :q ESCAPE '\\' OR UPPER(p.KODE_ITEM) LIKE :q ESCAPE '\\' OR p.BARCODE = :qraw)");
    }
    b.add("off", (u64::from(page.saturating_sub(1)) * u64::from(size)).to_string());
    b.add("lim", (size + 1).to_string());
    let sql = format!(
        "SELECT p.PRODUCTID, p.KODE_ITEM, p.PRODUCTNAME, NVL(k.KATEGORI, 'Tanpa kategori'), p.SATUAN, {}, {}, {} \
         FROM POS_PRODUCT p LEFT JOIN POS_KATEGORI k ON TO_CHAR(k.KODE) = TO_CHAR(p.KATEGORI_ID) {} {where_sql} \
         ORDER BY p.PRODUCTNAME, p.PRODUCTID OFFSET TO_NUMBER(:off) ROWS FETCH NEXT TO_NUMBER(:lim) ROWS ONLY",
        tm(STOK_EXPR),
        tm(HPP_EXPR),
        tm(&format!("({STOK_EXPR}) * {HPP_EXPR}")),
        stok_join(None)
    );
    let mut items = Vec::new();
    b.with(|p| -> AppResult<()> {
        for r in conn.query_named(&sql, p)? {
            let r = r?;
            let hpp = dec(&r.get::<_, String>(6)?)?;
            items.push(ValuasiItem {
                id: r.get(0)?,
                kode: r.get::<_, Option<String>>(1)?.unwrap_or_default(),
                nama: r.get::<_, Option<String>>(2)?.unwrap_or_default(),
                kategori: r.get(3)?,
                satuan: r.get::<_, Option<String>>(4)?.unwrap_or_default(),
                stok: dec(&r.get::<_, String>(5)?)?,
                hpp,
                nilai: dec(&r.get::<_, String>(7)?)?,
                tanpa_hpp: hpp.is_zero(),
            });
        }
        Ok(())
    })?;
    let ada_lagi = items.len() > size as usize;
    items.truncate(size as usize);
    Ok(Halaman { items, ada_lagi })
}

/// Produk aktif dengan ambang minimum terisi dan stok sistem < ambang (strict `<`; stok == ambang tidak alert).
pub fn stok_minimum(conn: &Connection, q: Option<&str>, kategori: Option<&str>, dari: NaiveDate, sampai: NaiveDate, page: u32, size: u32) -> AppResult<(Halaman<StokMin>, Option<i64>)> {
    let mut b = Binds::default();
    b.add("dari", dari.format("%Y-%m-%d"));
    b.add("sampai", sampai.format("%Y-%m-%d"));
    let mut where_sql = format!("WHERE p.AKTIF = 'Y' AND p.STOK_MIN IS NOT NULL AND {STOK_EXPR} < p.STOK_MIN");
    if let Some(q) = q.filter(|q| !q.trim().is_empty()) {
        b.add("q", like(q));
        b.add("qraw", q.trim());
        where_sql.push_str(" AND (UPPER(p.PRODUCTNAME) LIKE :q ESCAPE '\\' OR UPPER(p.KODE_ITEM) LIKE :q ESCAPE '\\' OR p.BARCODE = :qraw)");
    }
    if let Some(k) = kategori.filter(|k| !k.trim().is_empty()) {
        b.add("kat", k.trim());
        where_sql.push_str(" AND TO_CHAR(p.KATEGORI_ID) = :kat");
    }
    b.add("off", (page.saturating_sub(1) * size).to_string());
    b.add("lim", (size + 1).to_string());
    let sql = format!(
        "SELECT p.PRODUCTID, p.KODE_ITEM, p.PRODUCTNAME, p.SATUAN, k.KATEGORI, {}, {}, COUNT(*) OVER () \
         FROM POS_PRODUCT p LEFT JOIN POS_KATEGORI k ON TO_CHAR(k.KODE) = TO_CHAR(p.KATEGORI_ID) {} {where_sql} \
         ORDER BY p.PRODUCTNAME, p.PRODUCTID OFFSET TO_NUMBER(:off) ROWS FETCH NEXT TO_NUMBER(:lim) ROWS ONLY",
        tm(STOK_EXPR),
        tm("p.STOK_MIN"),
        stok_join(None)
    );
    let mut items = Vec::new();
    let mut total: Option<i64> = None;
    b.with(|p| -> AppResult<()> {
        for r in conn.query_named(&sql, p)? {
            let r = r?;
            total.get_or_insert(r.get::<_, i64>(7)?);
            items.push(StokMin {
                id: r.get(0)?,
                kode: r.get::<_, Option<String>>(1)?.unwrap_or_default(),
                nama: r.get::<_, Option<String>>(2)?.unwrap_or_default(),
                satuan: r.get::<_, Option<String>>(3)?.unwrap_or_default(),
                kategori: r.get::<_, Option<String>>(4)?.unwrap_or_default(),
                stok: dec(&r.get::<_, String>(5)?)?,
                stok_min: dec(&r.get::<_, String>(6)?)?,
            });
        }
        Ok(())
    })?;
    let ada_lagi = items.len() > size as usize;
    items.truncate(size as usize);
    let jumlah = (page <= 1).then(|| total.unwrap_or(0));
    Ok((Halaman { items, ada_lagi }, jumlah))
}

pub fn jumlah_stok_minimum(conn: &Connection, dari: NaiveDate, sampai: NaiveDate) -> AppResult<i64> {
    let mut b = Binds::default();
    b.add("dari", dari.format("%Y-%m-%d"));
    b.add("sampai", sampai.format("%Y-%m-%d"));
    let sql = format!(
        "SELECT COUNT(*) FROM POS_PRODUCT p {} WHERE p.AKTIF = 'Y' AND p.STOK_MIN IS NOT NULL AND {STOK_EXPR} < p.STOK_MIN",
        stok_join(None)
    );
    b.with(|p| Ok(conn.query_row_as_named(&sql, p)?))
}

/// Set/bersihkan ambang stok minimum produk (NULL = tanpa alert). >= 0, scale <= 4.
pub fn set_stok_min(conn: &Connection, produk_id: i64, nilai: Option<Decimal>) -> AppResult<()> {
    if let Some(v) = nilai
        && (v.is_sign_negative() || v > MAKS_QTY || v.scale() > 4)
    {
        return Err(AppError::validation("Ambang stok minimum tidak valid"));
    }
    let n = match nilai {
        Some(v) => conn.execute_named(
            &format!("UPDATE POS_PRODUCT SET STOK_MIN = {} WHERE PRODUCTID = :id", num("v")),
            &[("v", &v.to_string()), ("id", &produk_id)],
        )?.row_count()?,
        None => conn.execute_named(
            "UPDATE POS_PRODUCT SET STOK_MIN = NULL WHERE PRODUCTID = :id",
            &[("id", &produk_id)],
        )?.row_count()?,
    };
    if n == 0 {
        return Err(AppError::NotFound("Produk tidak ditemukan".into()));
    }
    conn.commit()?;
    Ok(())
}

#[cfg(test)]
mod tests {
    use super::*;

    fn d(s: &str) -> Decimal {
        s.parse().unwrap()
    }

    fn hari() -> NaiveDate {
        NaiveDate::from_ymd_opt(2026, 11, 19).unwrap()
    }

    fn req(qty: &str) -> SesuaiReq {
        SesuaiReq { produk_id: 1, qty: d(qty), tanggal: "2026-11-19".into(), alasan: "Koreksi hitung".into() }
    }

    #[test]
    fn qty_divalidasi() {
        assert!(validasi_qty(d("5")).is_ok());
        assert!(validasi_qty(d("-13.5")).is_ok());
        assert!(validasi_qty(d("0")).is_err());
        assert!(validasi_qty(d("1.00001")).is_err());
        assert!(validasi_qty(d("100000000")).is_err());
        assert!(validasi_qty(d("-100000000")).is_err());
        assert!(validasi_qty(d("99999999.9999")).is_ok());
    }

    #[test]
    fn alasan_wajib_3_sampai_250() {
        assert_eq!(alasan_bersih("  abc  ").unwrap(), "abc");
        assert!(alasan_bersih("ab").is_err());
        assert!(alasan_bersih("   ").is_err());
        assert!(alasan_bersih(&"x".repeat(250)).is_ok());
        assert!(alasan_bersih(&"x".repeat(251)).is_err());
    }

    #[test]
    fn tanggal_harus_tahun_berjalan_sampai_hari_ini() {
        assert!(tanggal_sesuai("2026-01-01", hari()).is_ok());
        assert!(tanggal_sesuai("2026-11-19", hari()).is_ok());
        assert!(tanggal_sesuai("2025-12-31", hari()).is_err());
        assert!(tanggal_sesuai("2026-11-20", hari()).is_err());
        assert!(tanggal_sesuai("19-11-2026", hari()).is_err());
    }

    #[test]
    fn rentang_kartu_default_clamp_dan_terbalik() {
        let awal = NaiveDate::from_ymd_opt(2026, 1, 1).unwrap();
        assert_eq!(rentang_kartu(None, None, hari()).unwrap(), (awal, hari()));
        let lama = NaiveDate::from_ymd_opt(2025, 6, 1).unwrap();
        let besok = hari() + chrono::Duration::days(1);
        assert_eq!(rentang_kartu(Some(lama), Some(besok), hari()).unwrap(), (awal, hari()));
        assert!(rentang_kartu(Some(hari()), Some(awal), hari()).is_err());
    }

    #[test]
    fn hash_stabil_dan_membedakan() {
        let a = hash_sesuai(&req("10"), "Koreksi hitung", hari());
        assert_eq!(a, hash_sesuai(&req("10.00"), "Koreksi hitung", hari()));
        assert_ne!(a, hash_sesuai(&req("11"), "Koreksi hitung", hari()));
        assert_ne!(a, hash_sesuai(&req("10"), "Alasan lain", hari()));
        assert_eq!(a.len(), 64);
        let b = hash_balik(7, "Salah input");
        assert_eq!(b, hash_balik(7, "Salah input"));
        assert_ne!(b, hash_balik(8, "Salah input"));
    }

    #[test]
    fn req_menolak_field_asing_dan_mutasi_memakai_ref() {
        let r: SesuaiReq = serde_json::from_str(r#"{"produk_id":1,"qty":"5","tanggal":"2026-11-19","alasan":"Koreksi"}"#).unwrap();
        assert_eq!(r.qty, d("5"));
        assert!(serde_json::from_str::<SesuaiReq>(r#"{"produk_id":1,"qty":"5","tanggal":"2026-11-19","alasan":"Koreksi","x":1}"#).is_err());
        let m = Mutasi { tanggal: "2026-11-19".into(), jenis: "penyesuaian".into(), referensi: "SESUAI-1".into(), qty: d("5"), saldo: d("10") };
        let j = serde_json::to_value(&m).unwrap();
        assert_eq!(j["ref"], "SESUAI-1");
        assert!(j.get("referensi").is_none());
    }

    #[test]
    fn peringatan_hanya_saat_negatif() {
        assert!(peringatan_saldo(d("0")).is_none());
        assert!(peringatan_saldo(d("3")).is_none());
        assert!(peringatan_saldo(d("-3")).is_some_and(|p| p.contains("-3")));
    }
}
