//! Barang rusak dari mobile/web. Tabel `POS_BARANGRUSAK` dan nomor `SO-YY-NNNNNN` dibagi dengan Stock Opname dan desktop
//! (`BackOffice/DataLayer/StockOpnameRepository.cs`, `Insert_BarangRusak`). Jumlah rusak mengurangi stok sistem.
//! Qty dalam satuan dasar produk. Ubah/hapus memakai pengaman bersama `dokumen_stok` (periode, dokumen lebih baru, versi).
use std::collections::{HashMap, HashSet};

use chrono::{Datelike, NaiveDate, NaiveDateTime};
use oracle::Connection;
use rust_decimal::Decimal;
use serde::{Deserialize, Serialize};
use sha2::{Digest, Sha256};

use super::dokumen_stok::{self, Jenis, hex};
use super::opname::{alokasi_nomor, klaim_kunci, selesai};
use super::{Binds, dec, katalog, num, tm};
use crate::error::{AppError, AppResult};

const OPERASI_BARU: &str = "RUSAK_BARU";
const OPERASI_UBAH: &str = "RUSAK_UBH";
const OPERASI_HAPUS: &str = "RUSAK_HPS";
const MAKS_QTY: i64 = 1_000_000_000;
const MAKS_KET: usize = 100;
pub const MAKS_BARIS: usize = 200;

/// Satu baris permintaan: `qty` string desimal dalam satuan dasar produk.
#[derive(Debug, Clone, Deserialize)]
pub struct BarisReq {
    pub produk_id: i64,
    pub qty: String,
    #[serde(default)]
    pub keterangan: Option<String>,
}

#[derive(Debug, Clone)]
struct Baris {
    produk_id: i64,
    qty: Decimal,
    keterangan: String,
}

#[derive(Debug, Serialize)]
pub struct BarisRusak {
    pub produk_id: Option<i64>,
    pub kode: String,
    pub nama: String,
    pub qty: Decimal,
    pub hpp: Decimal,
    pub keterangan: String,
}

#[derive(Debug, Serialize)]
pub struct DokumenRusak {
    pub nomor_so: String,
    pub tanggal: String,
    /// Sidik isi dokumen untuk ubah/hapus (409 bila basi).
    pub versi: String,
    pub items: Vec<BarisRusak>,
    /// SUM(qty x HPP): nilai persediaan yang berkurang.
    pub total: Decimal,
    pub ulang: bool,
}

#[derive(Debug, Serialize)]
pub struct RingkasRusak {
    pub nomor_so: String,
    pub tanggal: String,
    pub jumlah_item: i64,
    pub nilai: Decimal,
}

fn versi_rusak(items: &[BarisRusak]) -> String {
    let mut v: Vec<String> = items
        .iter()
        .map(|b| format!("{}|{}|{}|{}", b.kode, b.qty.normalize(), b.hpp.normalize(), b.keterangan))
        .collect();
    v.sort();
    dokumen_stok::sidik_versi(&v)
}

/// Validasi bentuk permintaan: tidak kosong, maks 200, produk unik, qty > 0 maks 4 desimal.
fn baca_baris(items: &[BarisReq]) -> AppResult<Vec<Baris>> {
    if items.is_empty() {
        return Err(AppError::validation("Daftar barang rusak masih kosong"));
    }
    if items.len() > MAKS_BARIS {
        return Err(AppError::validation(format!("Maksimal {MAKS_BARIS} barang per dokumen")));
    }
    let mut ada = HashSet::new();
    let mut out = Vec::with_capacity(items.len());
    for i in items {
        if !ada.insert(i.produk_id) {
            return Err(AppError::validation("Ada barang yang muncul lebih dari satu kali"));
        }
        let qty: Decimal = i
            .qty
            .trim()
            .parse()
            .map_err(|_| AppError::validation("Jumlah rusak tidak valid"))?;
        if qty <= Decimal::ZERO || qty > Decimal::from(MAKS_QTY) || qty.scale() > 4 {
            return Err(AppError::validation("Jumlah rusak harus lebih dari 0 dan maksimal 4 desimal"));
        }
        let ket = i.keterangan.as_deref().map(str::trim).unwrap_or_default().to_string();
        if ket.chars().count() > MAKS_KET {
            return Err(AppError::validation(format!("Keterangan maksimal {MAKS_KET} karakter")));
        }
        out.push(Baris { produk_id: i.produk_id, qty: qty.normalize(), keterangan: ket });
    }
    Ok(out)
}

fn hash_baris(baris: &[Baris]) -> String {
    let mut v: Vec<String> = baris
        .iter()
        .map(|b| format!("{}={}|{}", b.produk_id, b.qty.normalize(), b.keterangan))
        .collect();
    v.sort();
    hex(Sha256::digest(v.join(";").as_bytes()))
}

struct Prod {
    kode: String,
    nama: String,
    hpp: Decimal,
    aktif: bool,
}

/// Kunci produk (urut PRODUCTID) menurut id dan/atau kode yang sudah ada di dokumen.
fn kunci_produk(conn: &Connection, ids: &[i64], kodes: &[String]) -> AppResult<HashMap<i64, Prod>> {
    let mut b = Binds::default();
    let mut kondisi = Vec::new();
    if !ids.is_empty() {
        let n: Vec<String> = ids
            .iter()
            .enumerate()
            .map(|(i, id)| {
                b.add(format!("p{i}"), id);
                format!("TO_NUMBER(:p{i})")
            })
            .collect();
        kondisi.push(format!("PRODUCTID IN ({})", n.join(",")));
    }
    if !kodes.is_empty() {
        let n: Vec<String> = kodes
            .iter()
            .enumerate()
            .map(|(i, k)| {
                b.add(format!("k{i}"), k);
                format!(":k{i}")
            })
            .collect();
        kondisi.push(format!("KODE_ITEM IN ({})", n.join(",")));
    }
    let sql = format!(
        "SELECT PRODUCTID, KODE_ITEM, NVL(PRODUCTNAME, KODE_ITEM), {}, AKTIF FROM POS_PRODUCT WHERE {} ORDER BY PRODUCTID FOR UPDATE",
        tm("NVL(BELI, 0)"),
        kondisi.join(" OR ")
    );
    let mut prod = HashMap::new();
    b.with(|p| -> AppResult<()> {
        for r in conn.query_named(&sql, p)? {
            let r = r?;
            prod.insert(
                r.get::<_, i64>(0)?,
                Prod {
                    kode: r.get(1)?,
                    nama: r.get(2)?,
                    hpp: dec(&r.get::<_, String>(3)?)?,
                    aktif: r
                        .get::<_, Option<String>>(4)?
                        .is_some_and(|a| a.trim().eq_ignore_ascii_case("Y")),
                },
            );
        }
        Ok(())
    })?;
    Ok(prod)
}

fn tulis_baris(conn: &Connection, nomor: &str, tanggal: NaiveDate, kode: &str, qty: Decimal, hpp: Decimal, ket: &str) -> AppResult<()> {
    let tgl = tanggal.format("%Y-%m-%d").to_string();
    conn.execute_named(
        &format!(
            "INSERT INTO POS_BARANGRUSAK (NOMOR_SO, TANGGAL, KODE_BARANG, JUMLAHFISIK, HPP, TOTAL, KETERANGAN) \
             VALUES (:no, TO_DATE(:tgl,'YYYY-MM-DD'), :kode, {}, {}, {}, :ket)",
            num("qty"),
            num("hpp"),
            num("tot")
        ),
        &[
            ("no", &nomor),
            ("tgl", &tgl),
            ("kode", &kode),
            ("qty", &qty.to_string()),
            ("hpp", &hpp.to_string()),
            ("tot", &(qty * hpp).to_string()),
            ("ket", &ket),
        ],
    )?;
    Ok(())
}

fn stok_kurang(nama: &str, tersedia: Decimal, qty: Decimal) -> AppError {
    AppError::rule(
        "STOK_KURANG",
        format!(
            "Stok sistem \"{nama}\" hanya {} ({} diminta); barang rusak tidak boleh membuat stok negatif",
            tersedia.normalize(),
            qty.normalize()
        ),
    )
}

// ---------------------------------------------------------------- buat

/// Simpan dokumen barang rusak baru (satu nomor). Semua-atau-tidak-sama-sekali; produk dikunci FOR UPDATE.
pub fn simpan(conn: &Connection, user_id: i64, key: &str, items: &[BarisReq], hari: NaiveDate) -> AppResult<DokumenRusak> {
    let baris = baca_baris(items)?;
    let r = simpan_inner(conn, user_id, key, &baris, hari);
    if r.is_err() {
        let _ = conn.rollback();
    }
    r
}

fn simpan_inner(conn: &Connection, user_id: i64, key: &str, baris: &[Baris], hari: NaiveDate) -> AppResult<DokumenRusak> {
    if let Some(nomor) = klaim_kunci(conn, user_id, OPERASI_BARU, key, &hash_baris(baris))? {
        let mut d = detail(conn, &nomor)?;
        d.ulang = true;
        return Ok(d);
    }
    faktur_periode(conn, hari)?;
    let mut ids: Vec<i64> = baris.iter().map(|b| b.produk_id).collect();
    ids.sort_unstable();
    let prod = kunci_produk(conn, &ids, &[])?;
    if ids.iter().any(|id| !prod.contains_key(id)) {
        return Err(AppError::NotFound("Ada produk yang tidak ditemukan".into()));
    }
    let awal = NaiveDate::from_ymd_opt(hari.year(), 1, 1).expect("1 Jan");
    let kodes: Vec<String> = ids.iter().map(|id| prod[id].kode.clone()).collect();
    let stok = katalog::stok_map(conn, &kodes, awal, hari)?;
    for b in baris {
        let p = &prod[&b.produk_id];
        if !p.aktif {
            return Err(AppError::validation(format!("Produk \"{}\" tidak aktif", p.nama)));
        }
        let tersedia = stok.get(&p.kode).copied().unwrap_or_default();
        if b.qty > tersedia {
            return Err(stok_kurang(&p.nama, tersedia, b.qty));
        }
    }
    let nomor = alokasi_nomor(conn, hari.year())?;
    for b in baris {
        let p = &prod[&b.produk_id];
        tulis_baris(conn, &nomor, hari, &p.kode, b.qty, p.hpp, &b.keterangan)?;
    }
    selesai(conn, user_id, OPERASI_BARU, key, &nomor)?;
    detail(conn, &nomor)
}

fn faktur_periode(conn: &Connection, tanggal: NaiveDate) -> AppResult<()> {
    super::faktur::cek_periode(conn, tanggal)
}

// ---------------------------------------------------------------- baca

struct BarisDb {
    qty: Decimal,
    hpp: Decimal,
}

pub fn detail(conn: &Connection, nomor: &str) -> AppResult<DokumenRusak> {
    let sql = format!(
        "SELECT B.KODE_BARANG, NVL(P.PRODUCTNAME, B.KODE_BARANG), {}, {}, NVL(B.KETERANGAN, ''), TO_CHAR(B.TANGGAL, 'YYYY-MM-DD') \
         FROM POS_BARANGRUSAK B LEFT JOIN POS_PRODUCT P ON P.KODE_ITEM = B.KODE_BARANG \
         WHERE B.NOMOR_SO = :n ORDER BY 2, 1",
        tm("NVL(B.JUMLAHFISIK, 0)"),
        tm("NVL(B.HPP, 0)")
    );
    let mut items = Vec::new();
    let mut tanggal = String::new();
    for r in conn.query_named(&sql, &[("n", &nomor)])? {
        let r = r?;
        tanggal = r.get(5)?;
        items.push(BarisRusak {
            produk_id: r.get(6)?,
            kode: r.get(0)?,
            nama: r.get(1)?,
            qty: dec(&r.get::<_, String>(2)?)?,
            hpp: dec(&r.get::<_, String>(3)?)?,
            keterangan: r.get::<_, Option<String>>(4)?.unwrap_or_default(),
        });
    }
    if items.is_empty() {
        return Err(AppError::NotFound("Dokumen barang rusak tidak ditemukan".into()));
    }
    let total = items.iter().map(|b| b.qty * b.hpp).sum();
    Ok(DokumenRusak { nomor_so: nomor.to_string(), tanggal, versi: versi_rusak(&items), items, total, ulang: false })
}

/// Riwayat dokumen barang rusak per tahun, terbaru dulu.
pub fn daftar(conn: &Connection, tahun: i32, page: u32, size: u32) -> AppResult<(Vec<RingkasRusak>, bool)> {
    let mut b = Binds::default();
    b.add("y", tahun);
    b.add("off", (page.saturating_sub(1) * size).to_string());
    b.add("lim", (size + 1).to_string());
    let sql = format!(
        "SELECT NOMOR_SO, TO_CHAR(MIN(TANGGAL), 'YYYY-MM-DD'), COUNT(*), {} FROM POS_BARANGRUSAK \
         WHERE EXTRACT(YEAR FROM TANGGAL) = TO_NUMBER(:y) GROUP BY NOMOR_SO \
         ORDER BY MIN(TANGGAL) DESC, NOMOR_SO DESC OFFSET TO_NUMBER(:off) ROWS FETCH NEXT TO_NUMBER(:lim) ROWS ONLY",
        tm("SUM(NVL(JUMLAHFISIK, 0) * NVL(HPP, 0))")
    );
    let mut out = Vec::new();
    b.with(|p| -> AppResult<()> {
        for r in conn.query_named(&sql, p)? {
            let r = r?;
            out.push(RingkasRusak {
                nomor_so: r.get(0)?,
                tanggal: r.get(1)?,
                jumlah_item: r.get(2)?,
                nilai: dec(&r.get::<_, String>(3)?)?,
            });
        }
        Ok(())
    })?;
    let lagi = out.len() > size as usize;
    out.truncate(size as usize);
    Ok((out, lagi))
}

// ---------------------------------------------------------------- ubah dan hapus

/// Kunci baris dokumen, cocokkan `versi`. Dokumen dengan barang ganda (dari desktop) tidak bisa diubah di sini.
fn kunci_dokumen(conn: &Connection, nomor: &str, versi: &str) -> AppResult<(NaiveDate, HashMap<String, BarisDb>)> {
    let sql = format!(
        "SELECT KODE_BARANG, {}, {}, TRUNC(TANGGAL) FROM POS_BARANGRUSAK WHERE NOMOR_SO = :n FOR UPDATE",
        tm("NVL(JUMLAHFISIK, 0)"),
        tm("NVL(HPP, 0)")
    );
    let mut peta = HashMap::new();
    let mut tanggal: Option<NaiveDate> = None;
    for r in conn.query_named(&sql, &[("n", &nomor)])? {
        let r = r?;
        tanggal = Some(r.get::<_, NaiveDateTime>(3)?.date());
        let kode: String = r.get(0)?;
        let baris = BarisDb { qty: dec(&r.get::<_, String>(1)?)?, hpp: dec(&r.get::<_, String>(2)?)? };
        if peta.insert(kode.clone(), baris).is_some() {
            return Err(AppError::rule(
                "DOKUMEN_GANDA",
                format!("Dokumen memuat barang {kode} lebih dari sekali; ubah atau hapus lewat desktop"),
            ));
        }
    }
    let tanggal = tanggal.ok_or_else(|| AppError::NotFound("Dokumen barang rusak tidak ditemukan".into()))?;
    if detail(conn, nomor)?.versi != versi {
        return Err(AppError::Conflict("Dokumen sudah berubah sejak dibuka; muat ulang lalu ulangi".into()));
    }
    Ok((tanggal, peta))
}

#[allow(clippy::too_many_arguments)]
pub fn ubah(
    conn: &Connection,
    user_id: i64,
    user_nama: &str,
    key: &str,
    nomor: &str,
    versi: &str,
    alasan: Option<&str>,
    items: &[BarisReq],
    hari: NaiveDate,
) -> AppResult<DokumenRusak> {
    let baris = baca_baris(items)?;
    let alasan = dokumen_stok::alasan_valid(alasan)?;
    let r = ubah_inner(conn, user_id, user_nama, key, nomor, versi, &alasan, &baris, hari);
    if r.is_err() {
        let _ = conn.rollback();
    }
    r
}

#[allow(clippy::too_many_arguments)]
fn ubah_inner(
    conn: &Connection,
    user_id: i64,
    user_nama: &str,
    key: &str,
    nomor: &str,
    versi: &str,
    alasan: &str,
    baris: &[Baris],
    hari: NaiveDate,
) -> AppResult<DokumenRusak> {
    let hash = hex(Sha256::digest(format!("{nomor}|{versi}|{}", hash_baris(baris)).as_bytes()));
    if let Some(n) = klaim_kunci(conn, user_id, OPERASI_UBAH, key, &hash)? {
        let mut d = detail(conn, &n)?;
        d.ulang = true;
        return Ok(d);
    }
    let (tanggal, lama) = kunci_dokumen(conn, nomor, versi)?;
    let mut ids: Vec<i64> = baris.iter().map(|b| b.produk_id).collect();
    ids.sort_unstable();
    let kode_lama: Vec<String> = lama.keys().cloned().collect();
    let prod = kunci_produk(conn, &ids, &kode_lama)?;
    if ids.iter().any(|id| !prod.contains_key(id)) {
        return Err(AppError::NotFound("Ada produk yang tidak ditemukan".into()));
    }
    let kode_baru: HashSet<&str> = ids.iter().map(|id| prod[id].kode.as_str()).collect();
    let mut terpengaruh: Vec<String> = kode_baru.iter().map(|k| k.to_string()).collect();
    terpengaruh.extend(kode_lama.iter().filter(|k| !kode_baru.contains(k.as_str())).cloned());
    dokumen_stok::cek_boleh_ubah(conn, nomor, tanggal, &terpengaruh)?;

    let awal = NaiveDate::from_ymd_opt(hari.year(), 1, 1).expect("1 Jan");
    let stok = katalog::stok_map(conn, &terpengaruh, awal, hari)?;
    let mut total = Decimal::ZERO;
    for kode in kode_lama.iter().filter(|k| !kode_baru.contains(k.as_str())) {
        conn.execute_named(
            "DELETE FROM POS_BARANGRUSAK WHERE NOMOR_SO = :n AND KODE_BARANG = :k",
            &[("n", &nomor), ("k", kode)],
        )?;
    }
    for b in baris {
        let p = &prod[&b.produk_id];
        let l = lama.get(&p.kode);
        // Stok sekarang sudah dikurangi qty lama dokumen ini: kembalikan dulu sebelum membandingkan qty baru.
        let tersedia = stok.get(&p.kode).copied().unwrap_or_default() + l.map_or(Decimal::ZERO, |l| l.qty);
        if b.qty > tersedia {
            return Err(stok_kurang(&p.nama, tersedia, b.qty));
        }
        match l {
            Some(l) => {
                conn.execute_named(
                    &format!(
                        "UPDATE POS_BARANGRUSAK SET JUMLAHFISIK = {}, TOTAL = {}, KETERANGAN = :ket \
                         WHERE NOMOR_SO = :n AND KODE_BARANG = :k",
                        num("qty"),
                        num("tot")
                    ),
                    &[
                        ("qty", &b.qty.to_string()),
                        ("tot", &(b.qty * l.hpp).to_string()),
                        ("ket", &b.keterangan),
                        ("n", &nomor),
                        ("k", &p.kode),
                    ],
                )?;
                total += b.qty * l.hpp;
            }
            None => {
                if !p.aktif {
                    return Err(AppError::validation(format!("Produk \"{}\" tidak aktif", p.nama)));
                }
                tulis_baris(conn, nomor, tanggal, &p.kode, b.qty, p.hpp, &b.keterangan)?;
                total += b.qty * p.hpp;
            }
        }
    }
    dokumen_stok::catat_log(conn, Jenis::Rusak, nomor, "UBAH", user_id, user_nama, Some(alasan), baris.len(), total)?;
    dokumen_stok::tandai_selesai(conn, user_id, OPERASI_UBAH, key, nomor)?;
    conn.commit()?;
    detail(conn, nomor)
}

pub fn hapus(
    conn: &Connection,
    user_id: i64,
    user_nama: &str,
    key: &str,
    nomor: &str,
    versi: &str,
    alasan: Option<&str>,
) -> AppResult<()> {
    let alasan = dokumen_stok::alasan_valid(alasan)?;
    let r = hapus_inner(conn, user_id, user_nama, key, nomor, versi, &alasan);
    if r.is_err() {
        let _ = conn.rollback();
    }
    r
}

fn hapus_inner(conn: &Connection, user_id: i64, user_nama: &str, key: &str, nomor: &str, versi: &str, alasan: &str) -> AppResult<()> {
    let hash = hex(Sha256::digest(format!("{nomor}|{versi}").as_bytes()));
    if klaim_kunci(conn, user_id, OPERASI_HAPUS, key, &hash)?.is_some() {
        return Ok(()); // permintaan ulang: dokumen sudah terhapus
    }
    let (tanggal, lama) = kunci_dokumen(conn, nomor, versi)?;
    let kodes: Vec<String> = lama.keys().cloned().collect();
    dokumen_stok::cek_boleh_ubah(conn, nomor, tanggal, &kodes)?;
    let total: Decimal = lama.values().map(|l| l.qty * l.hpp).sum();
    conn.execute_named("DELETE FROM POS_BARANGRUSAK WHERE NOMOR_SO = :n", &[("n", &nomor)])?;
    dokumen_stok::catat_log(conn, Jenis::Rusak, nomor, "HAPUS", user_id, user_nama, Some(alasan), lama.len(), total)?;
    dokumen_stok::tandai_selesai(conn, user_id, OPERASI_HAPUS, key, nomor)?;
    conn.commit()?;
    Ok(())
}

#[cfg(test)]
mod tests {
    use super::*;

    fn br(id: i64, q: &str) -> BarisReq {
        BarisReq { produk_id: id, qty: q.into(), keterangan: None }
    }

    #[test]
    fn validasi_bentuk_permintaan() {
        assert!(baca_baris(&[]).is_err());
        assert!(baca_baris(&[br(1, "0")]).is_err());
        assert!(baca_baris(&[br(1, "-1")]).is_err());
        assert!(baca_baris(&[br(1, "abc")]).is_err());
        assert!(baca_baris(&[br(1, "1.23456")]).is_err()); // > 4 desimal
        assert!(baca_baris(&[br(1, "2"), br(1, "3")]).is_err()); // produk ganda
        assert!(baca_baris(&[BarisReq { produk_id: 1, qty: "1".into(), keterangan: Some("x".repeat(101)) }]).is_err());
        let ok = baca_baris(&[br(1, "2.5000"), br(2, "1")]).unwrap();
        assert_eq!(ok[0].qty.to_string(), "2.5");
    }

    #[test]
    fn hash_tidak_peduli_urutan() {
        let a = baca_baris(&[br(1, "2"), br(2, "3")]).unwrap();
        let b = baca_baris(&[br(2, "3"), br(1, "2")]).unwrap();
        assert_eq!(hash_baris(&a), hash_baris(&b));
        let c = baca_baris(&[br(1, "2"), br(2, "4")]).unwrap();
        assert_ne!(hash_baris(&a), hash_baris(&c));
    }

    #[test]
    fn versi_berubah_bila_qty_berubah() {
        let b = |q: &str| vec![BarisRusak { produk_id: Some(1), kode: "K".into(), nama: "N".into(), qty: q.parse().unwrap(), hpp: Decimal::ONE, keterangan: String::new() }];
        assert_eq!(versi_rusak(&b("2")), versi_rusak(&b("2.0")));
        assert_ne!(versi_rusak(&b("2")), versi_rusak(&b("3")));
    }
}
