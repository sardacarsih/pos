//! Stock opname dari mobile/web. Tabel dan nomor SO dibagi dengan desktop
//! (`BackOffice/DataLayer/StockOpnameRepository.cs`): hasilnya langsung terbaca rumus stok dan laporan desktop.
use std::collections::HashSet;

use chrono::{Datelike, NaiveDate};
use oracle::Connection;
use rust_decimal::Decimal;
use serde::{Deserialize, Serialize};
use sha2::{Digest, Sha256};

use super::{Binds, dec, katalog, num, opt, tm};
use crate::error::{AppError, AppResult};

const KODE: &str = "STOCK_OPNAME";
const OPERASI: &str = "OPNAME";
const OPERASI_DOK: &str = "OPNAME_DOK";
const MAKS_QTY: i64 = 1_000_000_000;
/// Batas baris per dokumen SO dari mobile/web (bind IN dan ukuran body tetap kecil).
pub const MAKS_BARIS: usize = 200;

#[derive(Debug, Serialize)]
pub struct HasilOpname {
    pub nomor_so: String,
    pub jumlah_sistem: Decimal,
    pub qty_fisik: Decimal,
    pub selisih: Decimal,
    /// True bila permintaan ini pengulangan (Idempotency-Key sama): tidak ada dokumen SO baru.
    pub ulang: bool,
}

/// Satu baris hitungan fisik dalam permintaan dokumen SO.
#[derive(Debug, Clone, Deserialize)]
pub struct BarisReq {
    pub produk_id: i64,
    pub qty_fisik: Decimal,
}

#[derive(Debug, Serialize)]
pub struct BarisSo {
    pub kode: String,
    pub nama: String,
    pub jumlah_sistem: Decimal,
    pub qty_fisik: Decimal,
    pub selisih: Decimal,
}

#[derive(Debug, Serialize)]
pub struct DokumenSo {
    pub nomor_so: String,
    pub tanggal: String,
    pub items: Vec<BarisSo>,
    pub ulang: bool,
}

#[derive(Debug, Serialize)]
pub struct RingkasSo {
    pub nomor_so: String,
    pub tanggal: String,
    pub jumlah_item: i64,
    /// SUM(SELISIH x HPP): nilai persediaan yang bertambah (+) atau berkurang (-).
    pub nilai_selisih: Decimal,
}

/// Nomor SO seperti desktop: `SO-YY-NNNNNN`.
pub fn format_nomor(tahun: i32, urut: i64) -> String {
    format!("SO-{:02}-{urut:06}", tahun % 100)
}

/// True bila `s` berformat `SO-YY-NNNNNN` (dipakai memvalidasi path sebelum query).
pub fn nomor_valid(s: &str) -> bool {
    let b = s.as_bytes();
    b.len() == 12
        && s.starts_with("SO-")
        && b[5] == b'-'
        && b[3..5].iter().chain(&b[6..]).all(u8::is_ascii_digit)
}

pub fn validasi_qty(qty: Decimal) -> AppResult<()> {
    if qty.is_sign_negative() {
        return Err(AppError::validation("Qty fisik tidak boleh negatif"));
    }
    if qty > Decimal::from(MAKS_QTY) || qty.scale() > 4 {
        return Err(AppError::validation("Qty fisik tidak valid"));
    }
    Ok(())
}

pub fn validasi_dokumen(items: &[BarisReq]) -> AppResult<()> {
    if items.is_empty() {
        return Err(AppError::validation("Daftar barang opname masih kosong"));
    }
    if items.len() > MAKS_BARIS {
        return Err(AppError::validation(format!(
            "Maksimal {MAKS_BARIS} barang per dokumen SO"
        )));
    }
    let mut ada = HashSet::new();
    for i in items {
        validasi_qty(i.qty_fisik)?;
        if !ada.insert(i.produk_id) {
            return Err(AppError::validation("Ada barang yang muncul lebih dari satu kali"));
        }
    }
    Ok(())
}

fn hex(d: impl AsRef<[u8]>) -> String {
    d.as_ref().iter().map(|b| format!("{b:02x}")).collect()
}

/// Sidik payload: kunci idempotensi yang dipakai ulang dengan produk/qty lain ditolak.
fn hash_payload(produk_id: i64, qty: Decimal) -> String {
    hex(Sha256::digest(format!("{produk_id}|{}", qty.normalize()).as_bytes()))
}

/// Sidik dokumen: urutan baris tidak berpengaruh.
fn hash_dokumen(items: &[BarisReq]) -> String {
    let mut v: Vec<(i64, Decimal)> = items.iter().map(|i| (i.produk_id, i.qty_fisik.normalize())).collect();
    v.sort_by_key(|(id, _)| *id);
    let s: Vec<String> = v.iter().map(|(id, q)| format!("{id}={q}")).collect();
    hex(Sha256::digest(s.join(";").as_bytes()))
}

/// Alokasi nomor berikutnya (UPDATE mengunci baris counter sampai commit, jadi paralel aman).
/// Fallback INSERT + ORA-00001 meniru `AllocateTransactionNumber` desktop.
fn alokasi_nomor(conn: &Connection, tahun: i32) -> AppResult<String> {
    let upd = "UPDATE POS_TRANSACTION_COUNTER SET LAST_NUMBER = LAST_NUMBER + 1 WHERE TRANSACTION_CODE = :c AND TRANSACTION_YEAR = :y";
    let mut n = conn.execute_named(upd, &[("c", &KODE), ("y", &tahun)])?.row_count()?;
    if n == 0 {
        match conn.execute_named(
            "INSERT INTO POS_TRANSACTION_COUNTER (TRANSACTION_CODE, TRANSACTION_YEAR, LAST_NUMBER) VALUES (:c, :y, 1)",
            &[("c", &KODE), ("y", &tahun)],
        ) {
            Ok(_) => n = 1,
            Err(e) if e.oci_code() == Some(1) => {
                n = conn.execute_named(upd, &[("c", &KODE), ("y", &tahun)])?.row_count()?;
            }
            Err(e) => return Err(e.into()),
        }
    }
    if n != 1 {
        return Err(AppError::Internal("counter nomor SO gagal".into()));
    }
    let urut: i64 = conn.query_row_as_named(
        "SELECT LAST_NUMBER FROM POS_TRANSACTION_COUNTER WHERE TRANSACTION_CODE = :c AND TRANSACTION_YEAR = :y",
        &[("c", &KODE), ("y", &tahun)],
    )?;
    Ok(format_nomor(tahun, urut))
}

/// Klaim kunci idempotensi. `None` = kunci baru (lanjutkan); `Some(nomor)` = permintaan ulang, pakai dokumen itu.
/// Permintaan paralel dengan kunci sama menunggu di INSERT lalu jatuh ke ORA-00001.
pub(crate) fn klaim_kunci(conn: &Connection, user_id: i64, operasi: &str, key: &str, hash: &str) -> AppResult<Option<String>> {
    match conn.execute_named(
        "INSERT INTO MOB_IDEMPOTENCY (USER_ID, OPERASI, IDEMPOTENCY_KEY, PAYLOAD_HASH) VALUES (:u, :o, :k, :h)",
        &[("u", &user_id), ("o", &operasi), ("k", &key), ("h", &hash)],
    ) {
        Ok(_) => Ok(None),
        Err(e) if e.oci_code() == Some(1) => {
            let r = opt(conn.query_row_named(
                "SELECT RESOURCE_ID, PAYLOAD_HASH FROM MOB_IDEMPOTENCY WHERE USER_ID = :u AND OPERASI = :o AND IDEMPOTENCY_KEY = :k",
                &[("u", &user_id), ("o", &operasi), ("k", &key)],
            ))?
            .ok_or_else(|| AppError::Conflict("Permintaan sedang diproses, coba lagi".into()))?;
            let (nomor, h): (Option<String>, Option<String>) = (r.get(0)?, r.get(1)?);
            if h.as_deref() != Some(hash) {
                return Err(AppError::Conflict(
                    "Kunci idempotensi sudah dipakai dengan data berbeda".into(),
                ));
            }
            nomor
                .map(Some)
                .ok_or_else(|| AppError::Conflict("Permintaan sedang diproses, coba lagi".into()))
        }
        Err(e) => Err(e.into()),
    }
}

/// Tulis satu baris dokumen SO (nilai TOTAL = selisih x HPP).
#[allow(clippy::too_many_arguments)]
fn tulis_baris(
    conn: &Connection,
    nomor: &str,
    hari: NaiveDate,
    kode: &str,
    sistem: Decimal,
    fisik: Decimal,
    selisih: Decimal,
    hpp: Decimal,
) -> AppResult<()> {
    let tgl = hari.format("%Y-%m-%d").to_string();
    conn.execute_named(
        &format!(
            "INSERT INTO POS_STOCKOPNAME (NOMOR_SO, TANGGAL, KODE_BARANG, JUMLAHSISTEM, JUMLAHFISIK, SELISIH, HPP, TOTAL) \
             VALUES (:no, TO_DATE(:tgl,'YYYY-MM-DD'), :kode, {}, {}, {}, {}, {})",
            num("sis"), num("fis"), num("sel"), num("hpp"), num("tot")
        ),
        &[
            ("no", &nomor),
            ("tgl", &tgl),
            ("kode", &kode),
            ("sis", &sistem.to_string()),
            ("fis", &fisik.to_string()),
            ("sel", &selisih.to_string()),
            ("hpp", &hpp.to_string()),
            ("tot", &(selisih * hpp).to_string()),
        ],
    )?;
    Ok(())
}

/// Cermin nomor terakhir untuk pembaca lama (sama seperti MergeLegacyNumberSql desktop) + tandai kunci selesai.
fn selesai(conn: &Connection, user_id: i64, operasi: &str, key: &str, nomor: &str) -> AppResult<()> {
    conn.execute_named(
        "MERGE INTO nomor_transaksi t USING (SELECT 'STOCK_OPNAME' kode, :no nomor FROM dual) s ON (t.kode = s.kode) \
         WHEN MATCHED THEN UPDATE SET t.nomor = s.nomor \
         WHEN NOT MATCHED THEN INSERT (kode, nomor) VALUES (s.kode, s.nomor)",
        &[("no", &nomor)],
    )?;
    conn.execute_named(
        "UPDATE MOB_IDEMPOTENCY SET RESOURCE_ID = :n WHERE USER_ID = :u AND OPERASI = :o AND IDEMPOTENCY_KEY = :k",
        &[("n", &nomor), ("u", &user_id), ("o", &operasi), ("k", &key)],
    )?;
    conn.commit()?;
    Ok(())
}

fn harga_nol_ditolak(nama: &str) -> AppError {
    AppError::validation(format!(
        "Harga beli \"{nama}\" belum diisi; isi di Katalog produk sebelum menambah stok lewat opname"
    ))
}

// ---------------------------------------------------------------- 1 barang

/// Baca ulang hasil dokumen SO satu barang (untuk balasan permintaan ulang).
fn baca_hasil(conn: &Connection, nomor: &str) -> AppResult<HasilOpname> {
    let sql = format!(
        "SELECT {}, {}, {} FROM POS_STOCKOPNAME WHERE NOMOR_SO = :n FETCH FIRST 1 ROW ONLY",
        tm("JUMLAHSISTEM"),
        tm("JUMLAHFISIK"),
        tm("SELISIH")
    );
    let r = opt(conn.query_row_named(&sql, &[("n", &nomor)]))?
        .ok_or_else(|| AppError::Internal("dokumen SO untuk kunci idempotensi hilang".into()))?;
    Ok(HasilOpname {
        nomor_so: nomor.to_string(),
        jumlah_sistem: dec(&r.get::<_, String>(0)?)?,
        qty_fisik: dec(&r.get::<_, String>(1)?)?,
        selisih: dec(&r.get::<_, String>(2)?)?,
        ulang: true,
    })
}

/// Simpan satu baris opname. `hari` = tanggal server. Commit di sini; rollback bila gagal.
/// Baris produk dikunci (FOR UPDATE) sebelum stok dibaca, jadi opname paralel untuk produk yang sama antre
/// dan masing-masing menghitung selisih dari stok terbaru. Idempotensi lewat MOB_IDEMPOTENCY (operasi OPNAME).
pub fn simpan(
    conn: &Connection,
    user_id: i64,
    idempotency_key: &str,
    produk_id: i64,
    qty_fisik: Decimal,
    hari: NaiveDate,
) -> AppResult<HasilOpname> {
    validasi_qty(qty_fisik)?;
    let r = simpan_inner(conn, user_id, idempotency_key, produk_id, qty_fisik, hari);
    if r.is_err() {
        let _ = conn.rollback();
    }
    r
}

fn simpan_inner(
    conn: &Connection,
    user_id: i64,
    key: &str,
    produk_id: i64,
    qty_fisik: Decimal,
    hari: NaiveDate,
) -> AppResult<HasilOpname> {
    let hash = hash_payload(produk_id, qty_fisik);
    if let Some(nomor) = klaim_kunci(conn, user_id, OPERASI, key, &hash)? {
        return baca_hasil(conn, &nomor);
    }
    // Kunci baris produk: opname lain atas produk ini menunggu sampai commit/rollback kita.
    let sql = format!(
        "SELECT KODE_ITEM, {}, AKTIF, PRODUCTNAME FROM POS_PRODUCT WHERE PRODUCTID = :id FOR UPDATE",
        tm("NVL(BELI, 0)")
    );
    let p = opt(conn.query_row_named(&sql, &[("id", &produk_id)]))?
        .ok_or_else(|| AppError::NotFound("Produk tidak ditemukan".into()))?;
    let kode: String = p.get(0)?;
    let hpp = dec(&p.get::<_, String>(1)?)?;
    if !p
        .get::<_, Option<String>>(2)?
        .is_some_and(|a| a.trim().eq_ignore_ascii_case("Y"))
    {
        return Err(AppError::validation("Produk tidak aktif"));
    }
    let nama: String = p.get::<_, Option<String>>(3)?.unwrap_or_else(|| kode.clone());
    let awal = NaiveDate::from_ymd_opt(hari.year(), 1, 1).expect("1 Jan");
    let sistem = katalog::stok_map(conn, std::slice::from_ref(&kode), awal, hari)?
        .get(&kode)
        .copied()
        .unwrap_or_default();
    let selisih = qty_fisik - sistem;
    if selisih.is_zero() {
        return Err(AppError::validation("Stok fisik sama dengan stok sistem"));
    }
    // Menambah stok tanpa harga beli membuat nilai persediaan (TOTAL = selisih x HPP) tidak berubah.
    if selisih > Decimal::ZERO && hpp.is_zero() {
        return Err(harga_nol_ditolak(&nama));
    }
    let nomor = alokasi_nomor(conn, hari.year())?;
    tulis_baris(conn, &nomor, hari, &kode, sistem, qty_fisik, selisih, hpp)?;
    selesai(conn, user_id, OPERASI, key, &nomor)?;
    Ok(HasilOpname {
        nomor_so: nomor,
        jumlah_sistem: sistem,
        qty_fisik,
        selisih,
        ulang: false,
    })
}

// ---------------------------------------------------------------- dokumen multi-barang

/// Simpan satu dokumen SO berisi beberapa barang (satu nomor SO). Semua baris produk dikunci sekaligus dalam
/// urutan PRODUCTID (tanpa deadlock antar admin); baris dengan selisih 0 ikut dicatat seperti desktop.
/// Semua-atau-tidak-sama-sekali: satu baris bermasalah membatalkan seluruh dokumen.
pub fn simpan_dokumen(
    conn: &Connection,
    user_id: i64,
    key: &str,
    items: &[BarisReq],
    hari: NaiveDate,
) -> AppResult<DokumenSo> {
    validasi_dokumen(items)?;
    let r = dokumen_inner(conn, user_id, key, items, hari);
    if r.is_err() {
        let _ = conn.rollback();
    }
    r
}

fn dokumen_inner(
    conn: &Connection,
    user_id: i64,
    key: &str,
    items: &[BarisReq],
    hari: NaiveDate,
) -> AppResult<DokumenSo> {
    if let Some(nomor) = klaim_kunci(conn, user_id, OPERASI_DOK, key, &hash_dokumen(items))? {
        let mut d = detail_so(conn, &nomor)?;
        d.ulang = true;
        return Ok(d);
    }
    let mut ids: Vec<i64> = items.iter().map(|i| i.produk_id).collect();
    ids.sort_unstable();
    let mut b = Binds::default();
    let names: Vec<String> = ids
        .iter()
        .enumerate()
        .map(|(i, id)| {
            b.add(format!("p{i}"), id);
            format!("TO_NUMBER(:p{i})")
        })
        .collect();
    let sql = format!(
        "SELECT PRODUCTID, KODE_ITEM, NVL(PRODUCTNAME, KODE_ITEM), {}, AKTIF FROM POS_PRODUCT \
         WHERE PRODUCTID IN ({}) ORDER BY PRODUCTID FOR UPDATE",
        tm("NVL(BELI, 0)"),
        names.join(",")
    );
    struct Prod {
        kode: String,
        nama: String,
        hpp: Decimal,
    }
    let mut prod = std::collections::HashMap::new();
    b.with(|p| -> AppResult<()> {
        for r in conn.query_named(&sql, p)? {
            let r = r?;
            let nama: String = r.get(2)?;
            if !r
                .get::<_, Option<String>>(4)?
                .is_some_and(|a| a.trim().eq_ignore_ascii_case("Y"))
            {
                return Err(AppError::validation(format!("Produk \"{nama}\" tidak aktif")));
            }
            prod.insert(
                r.get::<_, i64>(0)?,
                Prod {
                    kode: r.get(1)?,
                    nama,
                    hpp: dec(&r.get::<_, String>(3)?)?,
                },
            );
        }
        Ok(())
    })?;
    if prod.len() != ids.len() {
        return Err(AppError::NotFound("Ada produk yang tidak ditemukan".into()));
    }
    let awal = NaiveDate::from_ymd_opt(hari.year(), 1, 1).expect("1 Jan");
    let kodes: Vec<String> = prod.values().map(|p| p.kode.clone()).collect();
    let stok = katalog::stok_map(conn, &kodes, awal, hari)?;
    let mut baris = Vec::with_capacity(items.len());
    for i in items {
        let p = &prod[&i.produk_id];
        let sistem = stok.get(&p.kode).copied().unwrap_or_default();
        let selisih = i.qty_fisik - sistem;
        if selisih > Decimal::ZERO && p.hpp.is_zero() {
            return Err(harga_nol_ditolak(&p.nama));
        }
        baris.push((p, sistem, i.qty_fisik, selisih));
    }
    baris.sort_by(|a, b| a.0.nama.cmp(&b.0.nama));
    let nomor = alokasi_nomor(conn, hari.year())?;
    for (p, sistem, fisik, selisih) in &baris {
        tulis_baris(conn, &nomor, hari, &p.kode, *sistem, *fisik, *selisih, p.hpp)?;
    }
    selesai(conn, user_id, OPERASI_DOK, key, &nomor)?;
    Ok(DokumenSo {
        nomor_so: nomor,
        tanggal: hari.format("%Y-%m-%d").to_string(),
        items: baris
            .into_iter()
            .map(|(p, sistem, fisik, selisih)| BarisSo {
                kode: p.kode.clone(),
                nama: p.nama.clone(),
                jumlah_sistem: sistem,
                qty_fisik: fisik,
                selisih,
            })
            .collect(),
        ulang: false,
    })
}

// ---------------------------------------------------------------- riwayat

/// Riwayat dokumen SO satu tahun, terbaru dulu. Mengembalikan (baris, ada_lagi).
pub fn daftar_so(conn: &Connection, tahun: i32, page: u32, size: u32) -> AppResult<(Vec<RingkasSo>, bool)> {
    let mut b = Binds::default();
    b.add("y", tahun);
    b.add("off", (page.saturating_sub(1) * size).to_string());
    b.add("lim", (size + 1).to_string());
    let sql = format!(
        "SELECT NOMOR_SO, TO_CHAR(MIN(TANGGAL), 'YYYY-MM-DD'), COUNT(*), {} FROM POS_STOCKOPNAME \
         WHERE EXTRACT(YEAR FROM TANGGAL) = TO_NUMBER(:y) GROUP BY NOMOR_SO \
         ORDER BY MIN(TANGGAL) DESC, NOMOR_SO DESC OFFSET TO_NUMBER(:off) ROWS FETCH NEXT TO_NUMBER(:lim) ROWS ONLY",
        tm("SUM(NVL(SELISIH, 0) * NVL(HPP, 0))")
    );
    let mut out = Vec::new();
    b.with(|p| -> AppResult<()> {
        for r in conn.query_named(&sql, p)? {
            let r = r?;
            out.push(RingkasSo {
                nomor_so: r.get(0)?,
                tanggal: r.get(1)?,
                jumlah_item: r.get(2)?,
                nilai_selisih: dec(&r.get::<_, String>(3)?)?,
            });
        }
        Ok(())
    })?;
    let lagi = out.len() > size as usize;
    out.truncate(size as usize);
    Ok((out, lagi))
}

/// Isi satu dokumen SO (dari mobile/web maupun desktop).
pub fn detail_so(conn: &Connection, nomor: &str) -> AppResult<DokumenSo> {
    let sql = format!(
        "SELECT S.KODE_BARANG, NVL(B.PRODUCTNAME, S.KODE_BARANG), {}, {}, {}, TO_CHAR(S.TANGGAL, 'YYYY-MM-DD') \
         FROM POS_STOCKOPNAME S LEFT JOIN POS_PRODUCT B ON B.KODE_ITEM = S.KODE_BARANG \
         WHERE S.NOMOR_SO = :n ORDER BY 2, 1",
        tm("NVL(S.JUMLAHSISTEM, 0)"),
        tm("NVL(S.JUMLAHFISIK, 0)"),
        tm("NVL(S.SELISIH, 0)")
    );
    let mut items = Vec::new();
    let mut tanggal = String::new();
    for r in conn.query_named(&sql, &[("n", &nomor)])? {
        let r = r?;
        tanggal = r.get(5)?;
        items.push(BarisSo {
            kode: r.get(0)?,
            nama: r.get(1)?,
            jumlah_sistem: dec(&r.get::<_, String>(2)?)?,
            qty_fisik: dec(&r.get::<_, String>(3)?)?,
            selisih: dec(&r.get::<_, String>(4)?)?,
        });
    }
    if items.is_empty() {
        return Err(AppError::NotFound("Dokumen SO tidak ditemukan".into()));
    }
    Ok(DokumenSo {
        nomor_so: nomor.to_string(),
        tanggal,
        items,
        ulang: false,
    })
}

#[cfg(test)]
mod tests {
    use super::*;

    fn d(s: &str) -> Decimal {
        s.parse().unwrap()
    }

    fn br(id: i64, q: &str) -> BarisReq {
        BarisReq { produk_id: id, qty_fisik: d(q) }
    }

    #[test]
    fn nomor_so_format() {
        assert_eq!(format_nomor(2026, 7), "SO-26-000007");
        assert_eq!(format_nomor(2031, 123456), "SO-31-123456");
    }

    #[test]
    fn nomor_valid_hanya_format_so() {
        assert!(nomor_valid("SO-26-000007"));
        assert!(!nomor_valid("SO-26-00007"));
        assert!(!nomor_valid("XX-26-000007"));
        assert!(!nomor_valid("SO-2a-000007"));
        assert!(!nomor_valid("SO-26-000007'--"));
    }

    #[test]
    fn qty_divalidasi() {
        assert!(validasi_qty(Decimal::from(0)).is_ok());
        assert!(validasi_qty(d("12.5")).is_ok());
        assert!(validasi_qty(Decimal::from(-1)).is_err());
        assert!(validasi_qty(d("1.00001")).is_err());
        assert!(validasi_qty(Decimal::from(MAKS_QTY + 1)).is_err());
    }

    #[test]
    fn hash_payload_stabil_dan_membedakan() {
        let a = hash_payload(7, d("10"));
        assert_eq!(a, hash_payload(7, d("10.00")));
        assert_ne!(a, hash_payload(7, d("11")));
        assert_ne!(a, hash_payload(8, d("10")));
        assert_eq!(a.len(), 64);
    }

    #[test]
    fn dokumen_divalidasi() {
        assert!(validasi_dokumen(&[]).is_err());
        assert!(validasi_dokumen(&[br(1, "5"), br(2, "0")]).is_ok());
        assert!(validasi_dokumen(&[br(1, "5"), br(1, "6")]).is_err());
        assert!(validasi_dokumen(&[br(1, "-1")]).is_err());
        let banyak: Vec<BarisReq> = (0..=MAKS_BARIS as i64).map(|i| br(i, "1")).collect();
        assert!(validasi_dokumen(&banyak).is_err());
        assert!(validasi_dokumen(&banyak[..MAKS_BARIS]).is_ok());
    }

    #[test]
    fn hash_dokumen_tidak_peduli_urutan() {
        let a = hash_dokumen(&[br(1, "5"), br(2, "3.0")]);
        assert_eq!(a, hash_dokumen(&[br(2, "3"), br(1, "5.00")]));
        assert_ne!(a, hash_dokumen(&[br(1, "5"), br(2, "4")]));
        assert_ne!(a, hash_dokumen(&[br(1, "5")]));
    }
}
