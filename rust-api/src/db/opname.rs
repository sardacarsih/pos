//! Stock opname dari mobile/web. Tabel dan nomor SO dibagi dengan desktop
//! (`BackOffice/DataLayer/StockOpnameRepository.cs`): hasilnya langsung terbaca rumus stok dan laporan desktop.
use std::collections::HashSet;

use chrono::{Datelike, NaiveDate};
use oracle::Connection;
use rust_decimal::Decimal;
use serde::{Deserialize, Serialize};
use sha2::{Digest, Sha256};

use super::{Binds, dec, dokumen_stok, katalog, num, opt, tm};
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

/// Satu kolom hitungan: angka dan faktor dikirim sebagai string desimal oleh klien.
/// `satuan` = nama satuan dasar (faktor 1) atau satuan tambahan di Katalog.
#[derive(Debug, Clone, Deserialize)]
pub struct HitunganReq {
    pub satuan: String,
    pub qty: String,
    pub isi: String,
}

/// Satu baris hitungan fisik dalam permintaan dokumen SO.
/// Tepat satu dari `qty_fisik` (klien lama, satuan dasar) atau `hitungan` (multi-satuan).
#[derive(Debug, Clone, Deserialize)]
pub struct BarisReq {
    pub produk_id: i64,
    #[serde(default)]
    pub qty_fisik: Option<Decimal>,
    #[serde(default)]
    pub hitungan: Option<Vec<HitunganReq>>,
}

/// Hitungan yang sudah diparsing dan divalidasi bentuknya (faktor belum dicocokkan ke Katalog).
#[derive(Debug, Clone)]
pub struct HitunganParsed {
    pub satuan: String,
    pub qty: Decimal,
    pub isi: Decimal,
}

#[derive(Debug, Clone)]
enum Fisik {
    Langsung(Decimal),
    Hitungan(Vec<HitunganParsed>),
}

fn parse_desimal(teks: &str, label: &str) -> AppResult<Decimal> {
    teks.trim()
        .parse()
        .map_err(|_| AppError::validation(format!("{label} tidak valid")))
}

fn parse_hitungan(h: &[HitunganReq]) -> AppResult<Vec<HitunganParsed>> {
    if h.is_empty() {
        return Err(AppError::validation(
            "Hitungan masih kosong; isi 0 untuk menyatakan stok fisik kosong",
        ));
    }
    let mut ada = HashSet::new();
    let mut out = Vec::with_capacity(h.len());
    for e in h {
        let satuan = e.satuan.trim().to_string();
        if satuan.is_empty() {
            return Err(AppError::validation("Nama satuan pada hitungan kosong"));
        }
        if !ada.insert(satuan.to_uppercase()) {
            return Err(AppError::validation(format!(
                "Satuan \"{satuan}\" dihitung lebih dari satu kali"
            )));
        }
        let qty = parse_desimal(&e.qty, "Jumlah hitungan")?;
        validasi_qty(qty)?;
        let isi = parse_desimal(&e.isi, "Faktor konversi")?;
        if isi <= Decimal::ZERO {
            return Err(AppError::validation("Faktor konversi harus lebih dari 0"));
        }
        out.push(HitunganParsed { satuan, qty, isi });
    }
    Ok(out)
}

/// Bentuk payload valid: tepat satu dari qty_fisik / hitungan, angka sudah diparsing.
fn baca_fisik(b: &BarisReq) -> AppResult<Fisik> {
    match (&b.qty_fisik, &b.hitungan) {
        (Some(q), None) => {
            validasi_qty(*q)?;
            Ok(Fisik::Langsung(*q))
        }
        (None, Some(h)) => Ok(Fisik::Hitungan(parse_hitungan(h)?)),
        _ => Err(AppError::validation(
            "Isi tepat satu dari qty_fisik atau hitungan per barang",
        )),
    }
}

/// Sidik fisik untuk hash idempotensi: berbasis payload (bukan hasil konversi),
/// sehingga retry dengan kunci+payload sama tetap cocok meski Katalog kemudian berubah.
fn sidik_fisik(f: &Fisik) -> String {
    match f {
        Fisik::Langsung(q) => q.normalize().to_string(),
        Fisik::Hitungan(v) => {
            let mut e: Vec<String> = v
                .iter()
                .map(|h| {
                    format!(
                        "{}|{}|{}",
                        h.satuan.to_uppercase(),
                        h.qty.normalize(),
                        h.isi.normalize()
                    )
                })
                .collect();
            e.sort();
            format!("[{}]", e.join(";"))
        }
    }
}

/// Total fisik dalam satuan dasar. Faktor dibaca dari Katalog; faktor yang dikirim klien
/// harus sama dengan faktor saat ini (editor dibuka dengan data lama -> tolak, minta muat ulang).
fn total_hitungan(
    conn: &Connection,
    produk_id: i64,
    dasar: &str,
    nama: &str,
    hitungan: &[HitunganParsed],
) -> AppResult<Decimal> {
    let mut total = Decimal::ZERO;
    for h in hitungan {
        let faktor = if h.satuan.eq_ignore_ascii_case(dasar.trim()) {
            Decimal::ONE
        } else {
            katalog::satuan_jual(conn, produk_id, &h.satuan)?
                .ok_or_else(|| {
                    AppError::validation(format!(
                        "Satuan \"{}\" {} sudah dihapus; muat ulang satuan produk ini",
                        h.satuan, nama
                    ))
                })?
                .isi
        };
        if h.isi.normalize() != faktor.normalize() {
            return Err(AppError::validation(format!(
                "Faktor konversi {} \"{}\" berubah ({} menjadi {}); muat ulang satuan produk ini sebelum meninjau",
                nama, h.satuan, h.isi.normalize(), faktor.normalize()
            )));
        }
        let konversi = hasil_konversi(h.qty, faktor).map_err(|_| {
            AppError::validation(format!(
                "Hasil konversi {} {} perlu lebih dari 4 desimal",
                nama, h.satuan
            ))
        })?;
        total += konversi;
    }
    validasi_qty(total)?;
    Ok(total)
}

/// qty x faktor dalam satuan dasar; maksimal 4 desimal setelah nol trailing dibuang.
fn hasil_konversi(qty: Decimal, faktor: Decimal) -> AppResult<Decimal> {
    let k = (qty * faktor).normalize();
    if k.scale() > 4 {
        return Err(AppError::validation("Hasil konversi perlu lebih dari 4 desimal"));
    }
    Ok(k)
}

fn resolve_fisik(
    conn: &Connection,
    produk_id: i64,
    dasar: &str,
    nama: &str,
    f: &Fisik,
) -> AppResult<Decimal> {
    match f {
        Fisik::Langsung(q) => Ok(*q),
        Fisik::Hitungan(v) => total_hitungan(conn, produk_id, dasar, nama, v),
    }
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
    /// Sidik isi dokumen; dikirim balik pada ubah/hapus agar data yang sudah berubah ditolak (409).
    pub versi: String,
    pub items: Vec<BarisSo>,
    pub ulang: bool,
}

/// Sidik isi dokumen, tidak bergantung urutan baris.
fn versi_so(items: &[BarisSo]) -> String {
    let mut v: Vec<String> = items
        .iter()
        .map(|b| {
            format!(
                "{}|{}|{}|{}",
                b.kode,
                b.jumlah_sistem.normalize(),
                b.qty_fisik.normalize(),
                b.selisih.normalize()
            )
        })
        .collect();
    v.sort();
    dokumen_stok::sidik_versi(&v)
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
        baca_fisik(i)?;
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
fn hash_payload(produk_id: i64, sidik: &str) -> String {
    hex(Sha256::digest(format!("{produk_id}|{sidik}").as_bytes()))
}

/// Sidik dokumen: urutan baris tidak berpengaruh.
fn hash_dokumen(items: &[BarisReq], fisik: &[Fisik]) -> String {
    let mut v: Vec<(i64, String)> = items
        .iter()
        .zip(fisik)
        .map(|(i, f)| (i.produk_id, sidik_fisik(f)))
        .collect();
    v.sort_by_key(|(id, _)| *id);
    let s: Vec<String> = v.iter().map(|(id, q)| format!("{id}={q}")).collect();
    hex(Sha256::digest(s.join(";").as_bytes()))
}

/// Alokasi nomor berikutnya (UPDATE mengunci baris counter sampai commit, jadi paralel aman).
/// Fallback INSERT + ORA-00001 meniru `AllocateTransactionNumber` desktop.
pub(crate) fn alokasi_nomor(conn: &Connection, tahun: i32) -> AppResult<String> {
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
pub(crate) fn selesai(conn: &Connection, user_id: i64, operasi: &str, key: &str, nomor: &str) -> AppResult<()> {
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
    baris: &BarisReq,
    hari: NaiveDate,
) -> AppResult<HasilOpname> {
    let fisik = baca_fisik(baris)?;
    let r = simpan_inner(conn, user_id, idempotency_key, baris.produk_id, &fisik, hari);
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
    fisik: &Fisik,
    hari: NaiveDate,
) -> AppResult<HasilOpname> {
    let hash = hash_payload(produk_id, &sidik_fisik(fisik));
    if let Some(nomor) = klaim_kunci(conn, user_id, OPERASI, key, &hash)? {
        return baca_hasil(conn, &nomor);
    }
    // Kunci baris produk: opname lain atas produk ini menunggu sampai commit/rollback kita.
    let sql = format!(
        "SELECT KODE_ITEM, {}, AKTIF, PRODUCTNAME, SATUAN FROM POS_PRODUCT WHERE PRODUCTID = :id FOR UPDATE",
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
    let dasar: String = p.get::<_, Option<String>>(4)?.unwrap_or_default();
    let qty_fisik = resolve_fisik(conn, produk_id, &dasar, &nama, fisik)?;
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
    let fisik: Vec<Fisik> = items.iter().map(baca_fisik).collect::<AppResult<_>>()?;
    let r = dokumen_inner(conn, user_id, key, items, &fisik, hari);
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
    fisik: &[Fisik],
    hari: NaiveDate,
) -> AppResult<DokumenSo> {
    if let Some(nomor) = klaim_kunci(conn, user_id, OPERASI_DOK, key, &hash_dokumen(items, fisik))? {
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
        "SELECT PRODUCTID, KODE_ITEM, NVL(PRODUCTNAME, KODE_ITEM), {}, AKTIF, SATUAN FROM POS_PRODUCT \
         WHERE PRODUCTID IN ({}) ORDER BY PRODUCTID FOR UPDATE",
        tm("NVL(BELI, 0)"),
        names.join(",")
    );
    struct Prod {
        kode: String,
        nama: String,
        hpp: Decimal,
        dasar: String,
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
                    dasar: r.get::<_, Option<String>>(5)?.unwrap_or_default(),
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
    for (i, f) in items.iter().zip(fisik) {
        let p = &prod[&i.produk_id];
        let qty_fisik = resolve_fisik(conn, i.produk_id, &p.dasar, &p.nama, f)?;
        let sistem = stok.get(&p.kode).copied().unwrap_or_default();
        let selisih = qty_fisik - sistem;
        if selisih > Decimal::ZERO && p.hpp.is_zero() {
            return Err(harga_nol_ditolak(&p.nama));
        }
        baris.push((p, sistem, qty_fisik, selisih));
    }
    baris.sort_by(|a, b| a.0.nama.cmp(&b.0.nama));
    let nomor = alokasi_nomor(conn, hari.year())?;
    for (p, sistem, fisik, selisih) in &baris {
        tulis_baris(conn, &nomor, hari, &p.kode, *sistem, *fisik, *selisih, p.hpp)?;
    }
    selesai(conn, user_id, OPERASI_DOK, key, &nomor)?;
    let items: Vec<BarisSo> = baris
        .into_iter()
        .map(|(p, sistem, fisik, selisih)| BarisSo {
            kode: p.kode.clone(),
            nama: p.nama.clone(),
            jumlah_sistem: sistem,
            qty_fisik: fisik,
            selisih,
        })
        .collect();
    Ok(DokumenSo {
        nomor_so: nomor,
        tanggal: hari.format("%Y-%m-%d").to_string(),
        versi: versi_so(&items),
        items,
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
        versi: versi_so(&items),
        items,
        ulang: false,
    })
}

// ---------------------------------------------------------------- ubah dan hapus dokumen

const OPERASI_UBAH: &str = "OPNAME_UBH";
const OPERASI_HAPUS: &str = "OPNAME_HPS";

/// Baris dokumen apa adanya di DB (dikunci FOR UPDATE).
struct BarisLama {
    sistem: Decimal,
    hpp: Decimal,
    selisih: Decimal,
}

/// Kunci semua baris dokumen, cocokkan `versi`, dan kembalikan tanggal + peta kode -> baris. NotFound bila kosong.
fn kunci_dokumen(
    conn: &Connection,
    nomor: &str,
    versi: &str,
) -> AppResult<(NaiveDate, std::collections::HashMap<String, BarisLama>)> {
    let sql = format!(
        "SELECT KODE_BARANG, {}, {}, {}, TRUNC(TANGGAL) FROM POS_STOCKOPNAME WHERE NOMOR_SO = :n FOR UPDATE",
        tm("NVL(JUMLAHSISTEM, 0)"),
        tm("NVL(HPP, 0)"),
        tm("NVL(SELISIH, 0)")
    );
    let mut peta = std::collections::HashMap::new();
    let mut tanggal: Option<NaiveDate> = None;
    for r in conn.query_named(&sql, &[("n", &nomor)])? {
        let r = r?;
        tanggal = Some(r.get::<_, chrono::NaiveDateTime>(4)?.date());
        peta.insert(
            r.get::<_, String>(0)?,
            BarisLama {
                sistem: dec(&r.get::<_, String>(1)?)?,
                hpp: dec(&r.get::<_, String>(2)?)?,
                selisih: dec(&r.get::<_, String>(3)?)?,
            },
        );
    }
    let tanggal = tanggal.ok_or_else(|| AppError::NotFound("Dokumen SO tidak ditemukan".into()))?;
    // Dibaca setelah dikunci: bila `versi` cocok, isi dokumen tidak berubah sejak klien membacanya.
    if detail_so(conn, nomor)?.versi != versi {
        return Err(AppError::Conflict(
            "Dokumen SO sudah berubah sejak dibuka; muat ulang lalu ulangi".into(),
        ));
    }
    Ok((tanggal, peta))
}

/// Ubah dokumen SO (nomor dan tanggal tetap): ubah fisik, tambah, atau hapus barang.
/// Baris lama: `selisih = fisik baru - JUMLAHSISTEM lama` dengan HPP lama (stok bergeser tepat sebesar delta
/// selisih); baris baru memakai stok dan HPP sekarang. Semua-atau-tidak-sama-sekali, tercatat di log.
#[allow(clippy::too_many_arguments)]
pub fn ubah_dokumen(
    conn: &Connection,
    user_id: i64,
    user_nama: &str,
    key: &str,
    nomor: &str,
    versi: &str,
    alasan: Option<&str>,
    items: &[BarisReq],
    hari: NaiveDate,
) -> AppResult<DokumenSo> {
    validasi_dokumen(items)?;
    let alasan = dokumen_stok::alasan_valid(alasan)?;
    let fisik: Vec<Fisik> = items.iter().map(baca_fisik).collect::<AppResult<_>>()?;
    let r = ubah_inner(conn, user_id, user_nama, key, nomor, versi, &alasan, items, &fisik, hari);
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
    items: &[BarisReq],
    fisik: &[Fisik],
    hari: NaiveDate,
) -> AppResult<DokumenSo> {
    let hash = hex(Sha256::digest(
        format!("{nomor}|{versi}|{}", hash_dokumen(items, fisik)).as_bytes(),
    ));
    if let Some(n) = klaim_kunci(conn, user_id, OPERASI_UBAH, key, &hash)? {
        let mut d = detail_so(conn, &n)?;
        d.ulang = true;
        return Ok(d);
    }
    let (tanggal, lama) = kunci_dokumen(conn, nomor, versi)?;

    // Kunci produk yang diminta dan yang sudah ada di dokumen (urut PRODUCTID: tanpa deadlock antar admin).
    let mut ids: Vec<i64> = items.iter().map(|i| i.produk_id).collect();
    ids.sort_unstable();
    let mut b = Binds::default();
    let id_names: Vec<String> = ids
        .iter()
        .enumerate()
        .map(|(i, id)| {
            b.add(format!("p{i}"), id);
            format!("TO_NUMBER(:p{i})")
        })
        .collect();
    let kode_names: Vec<String> = lama
        .keys()
        .enumerate()
        .map(|(i, k)| {
            b.add(format!("k{i}"), k);
            format!(":k{i}")
        })
        .collect();
    let sql = format!(
        "SELECT PRODUCTID, KODE_ITEM, NVL(PRODUCTNAME, KODE_ITEM), {}, AKTIF, SATUAN FROM POS_PRODUCT \
         WHERE PRODUCTID IN ({}) OR KODE_ITEM IN ({}) ORDER BY PRODUCTID FOR UPDATE",
        tm("NVL(BELI, 0)"),
        id_names.join(","),
        kode_names.join(",")
    );
    struct Prod {
        kode: String,
        nama: String,
        hpp: Decimal,
        aktif: bool,
        dasar: String,
    }
    let mut prod = std::collections::HashMap::new();
    b.with(|p| -> AppResult<()> {
        for r in conn.query_named(&sql, p)? {
            let r = r?;
            let kode: String = r.get(1)?;
            prod.insert(
                r.get::<_, i64>(0)?,
                Prod {
                    nama: r.get(2)?,
                    hpp: dec(&r.get::<_, String>(3)?)?,
                    aktif: r
                        .get::<_, Option<String>>(4)?
                        .is_some_and(|a| a.trim().eq_ignore_ascii_case("Y")),
                    dasar: r.get::<_, Option<String>>(5)?.unwrap_or_default(),
                    kode,
                },
            );
        }
        Ok(())
    })?;
    if ids.iter().any(|id| !prod.contains_key(id)) {
        return Err(AppError::NotFound("Ada produk yang tidak ditemukan".into()));
    }

    // Kode yang terpengaruh = kode di permintaan + kode lama yang dihapus.
    let kode_baru: HashSet<&str> = ids.iter().map(|id| prod[id].kode.as_str()).collect();
    let mut terpengaruh: Vec<String> = kode_baru.iter().map(|k| k.to_string()).collect();
    terpengaruh.extend(lama.keys().filter(|k| !kode_baru.contains(k.as_str())).cloned());
    dokumen_stok::cek_boleh_ubah(conn, nomor, tanggal, &terpengaruh)?;

    // Baris dihapus.
    for kode in lama.keys().filter(|k| !kode_baru.contains(k.as_str())) {
        conn.execute_named(
            "DELETE FROM POS_STOCKOPNAME WHERE NOMOR_SO = :n AND KODE_BARANG = :k",
            &[("n", &nomor), ("k", kode)],
        )?;
    }
    let awal = NaiveDate::from_ymd_opt(hari.year(), 1, 1).expect("1 Jan");
    let kodes: Vec<String> = prod.values().map(|p| p.kode.clone()).collect();
    let stok = katalog::stok_map(conn, &kodes, awal, hari)?;
    let mut total = Decimal::ZERO;
    for (i, f) in items.iter().zip(fisik) {
        let p = &prod[&i.produk_id];
        let qty_fisik = resolve_fisik(conn, i.produk_id, &p.dasar, &p.nama, f)?;
        match lama.get(&p.kode) {
            Some(l) => {
                let selisih = qty_fisik - l.sistem;
                if selisih > l.selisih && selisih > Decimal::ZERO && l.hpp.is_zero() {
                    return Err(harga_nol_ditolak(&p.nama));
                }
                conn.execute_named(
                    &format!(
                        "UPDATE POS_STOCKOPNAME SET JUMLAHFISIK = {}, SELISIH = {}, TOTAL = {} \
                         WHERE NOMOR_SO = :n AND KODE_BARANG = :k",
                        num("fis"),
                        num("sel"),
                        num("tot")
                    ),
                    &[
                        ("fis", &qty_fisik.to_string()),
                        ("sel", &selisih.to_string()),
                        ("tot", &(selisih * l.hpp).to_string()),
                        ("n", &nomor),
                        ("k", &p.kode),
                    ],
                )?;
                total += selisih * l.hpp;
            }
            None => {
                if !p.aktif {
                    return Err(AppError::validation(format!("Produk \"{}\" tidak aktif", p.nama)));
                }
                let sistem = stok.get(&p.kode).copied().unwrap_or_default();
                let selisih = qty_fisik - sistem;
                if selisih > Decimal::ZERO && p.hpp.is_zero() {
                    return Err(harga_nol_ditolak(&p.nama));
                }
                tulis_baris(conn, nomor, tanggal, &p.kode, sistem, qty_fisik, selisih, p.hpp)?;
                total += selisih * p.hpp;
            }
        }
    }
    dokumen_stok::catat_log(
        conn,
        dokumen_stok::Jenis::So,
        nomor,
        "UBAH",
        user_id,
        user_nama,
        Some(alasan),
        items.len(),
        total,
    )?;
    dokumen_stok::tandai_selesai(conn, user_id, OPERASI_UBAH, key, nomor)?;
    conn.commit()?;
    detail_so(conn, nomor)
}

/// Hapus permanen satu dokumen SO (nomor tidak dipakai ulang). Pengaman sama dengan `ubah_dokumen`.
pub fn hapus_dokumen(
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

fn hapus_inner(
    conn: &Connection,
    user_id: i64,
    user_nama: &str,
    key: &str,
    nomor: &str,
    versi: &str,
    alasan: &str,
) -> AppResult<()> {
    let hash = hex(Sha256::digest(format!("{nomor}|{versi}").as_bytes()));
    if klaim_kunci(conn, user_id, OPERASI_HAPUS, key, &hash)?.is_some() {
        return Ok(()); // permintaan ulang: dokumen sudah terhapus
    }
    let (tanggal, lama) = kunci_dokumen(conn, nomor, versi)?;
    let kodes: Vec<String> = lama.keys().cloned().collect();
    dokumen_stok::cek_boleh_ubah(conn, nomor, tanggal, &kodes)?;
    let total: Decimal = lama.values().map(|l| l.selisih * l.hpp).sum();
    conn.execute_named("DELETE FROM POS_STOCKOPNAME WHERE NOMOR_SO = :n", &[("n", &nomor)])?;
    dokumen_stok::catat_log(
        conn,
        dokumen_stok::Jenis::So,
        nomor,
        "HAPUS",
        user_id,
        user_nama,
        Some(alasan),
        lama.len(),
        total,
    )?;
    dokumen_stok::tandai_selesai(conn, user_id, OPERASI_HAPUS, key, nomor)?;
    conn.commit()?;
    Ok(())
}

#[cfg(test)]
mod tests {
    use super::*;

    fn d(s: &str) -> Decimal {
        s.parse().unwrap()
    }

    fn br(id: i64, q: &str) -> BarisReq {
        BarisReq { produk_id: id, qty_fisik: Some(d(q)), hitungan: None }
    }

    fn hr(satuan: &str, qty: &str, isi: &str) -> HitunganReq {
        HitunganReq { satuan: satuan.into(), qty: qty.into(), isi: isi.into() }
    }

    fn bm(id: i64, h: Vec<HitunganReq>) -> BarisReq {
        BarisReq { produk_id: id, qty_fisik: None, hitungan: Some(h) }
    }

    fn fisik(items: &[BarisReq]) -> Vec<Fisik> {
        items.iter().map(baca_fisik).collect::<AppResult<_>>().unwrap()
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
        let s = |q: &str| sidik_fisik(&Fisik::Langsung(d(q)));
        let a = hash_payload(7, &s("10"));
        assert_eq!(a, hash_payload(7, &s("10.00")));
        assert_ne!(a, hash_payload(7, &s("11")));
        assert_ne!(a, hash_payload(8, &s("10")));
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
        let a_items = [br(1, "5"), br(2, "3.0")];
        let b_items = [br(2, "3"), br(1, "5.00")];
        let a = hash_dokumen(&a_items, &fisik(&a_items));
        assert_eq!(a, hash_dokumen(&b_items, &fisik(&b_items)));
        let c_items = [br(1, "5"), br(2, "4")];
        assert_ne!(a, hash_dokumen(&c_items, &fisik(&c_items)));
        let d_items = [br(1, "5")];
        assert_ne!(a, hash_dokumen(&d_items, &fisik(&d_items)));
    }

    #[test]
    fn bentuk_payload_tepat_satu() {
        assert!(baca_fisik(&BarisReq { produk_id: 1, qty_fisik: None, hitungan: None }).is_err());
        let ganda = BarisReq {
            produk_id: 1,
            qty_fisik: Some(d("5")),
            hitungan: Some(vec![hr("PCS", "5", "1")]),
        };
        assert!(baca_fisik(&ganda).is_err());
        assert!(baca_fisik(&br(1, "5")).is_ok());
        assert!(baca_fisik(&bm(1, vec![hr("PCS", "5", "1")])).is_ok());
    }

    #[test]
    fn hitungan_kosong_dan_satuan_ganda_ditolak() {
        assert!(parse_hitungan(&[]).is_err());
        assert!(parse_hitungan(&[hr("DUS", "1", "12"), hr("dus", "2", "12")]).is_err());
        assert!(parse_hitungan(&[hr(" ", "1", "12")]).is_err());
        assert!(parse_hitungan(&[hr("DUS", "1", "12"), hr("PCS", "3", "1")]).is_ok());
    }

    #[test]
    fn hitungan_angka_divalidasi() {
        assert!(parse_hitungan(&[hr("PCS", "-1", "1")]).is_err());
        assert!(parse_hitungan(&[hr("PCS", "1000000001", "1")]).is_err());
        assert!(parse_hitungan(&[hr("PCS", "1.00001", "1")]).is_err());
        assert!(parse_hitungan(&[hr("PCS", "abc", "1")]).is_err());
        assert!(parse_hitungan(&[hr("PCS", "", "1")]).is_err());
        assert!(parse_hitungan(&[hr("DUS", "1", "0")]).is_err());
        assert!(parse_hitungan(&[hr("DUS", "1", "-12")]).is_err());
        assert!(parse_hitungan(&[hr("DUS", "1", "x")]).is_err());
        assert!(parse_hitungan(&[hr("PCS", "0", "1")]).is_ok());
        assert!(parse_hitungan(&[hr("PCS", "2,5", "1")]).is_err()); // klien mengirim titik desimal
    }

    #[test]
    fn konversi_presisi_maks_4_desimal() {
        assert_eq!(hasil_konversi(d("2"), d("12")).unwrap(), d("24"));
        assert_eq!(hasil_konversi(d("0.5"), d("0.5")).unwrap(), d("0.25"));
        assert_eq!(hasil_konversi(d("1.50"), d("2")).unwrap(), d("3"));
        assert!(hasil_konversi(d("0.0001"), d("0.3")).is_err()); // 0.00003
        assert!(hasil_konversi(d("0.12345"), d("1")).is_err());
        assert_eq!(hasil_konversi(d("0.0001"), d("3")).unwrap(), d("0.0003"));
    }

    #[test]
    fn total_gabungan_dan_batas_miliar() {
        // 2 dus x 12 + 3 pcs = 27 (faktor dari "Katalog" = isi yang dikirim, murni di sini).
        let h = parse_hitungan(&[hr("DUS", "2", "12"), hr("PCS", "3", "1")]).unwrap();
        let total: Decimal = h.iter().map(|x| hasil_konversi(x.qty, x.isi).unwrap()).sum();
        assert_eq!(total, d("27"));
        let besar = parse_hitungan(&[hr("DUS", "100000000", "12")]).unwrap();
        let t: Decimal = besar.iter().map(|x| hasil_konversi(x.qty, x.isi).unwrap()).sum();
        assert!(validasi_qty(t).is_err()); // 1,2 miliar
    }

    #[test]
    fn sidik_hitungan_stabil_terhadap_urutan_dan_nol_trailing() {
        let a_items = [bm(1, vec![hr("DUS", "2", "12"), hr("PCS", "3", "1")])];
        let b_items = [bm(1, vec![hr("pcs", "3.0", "1.00"), hr("dus", "2.00", "12")])];
        assert_eq!(
            hash_dokumen(&a_items, &fisik(&a_items)),
            hash_dokumen(&b_items, &fisik(&b_items))
        );
        let c_items = [bm(1, vec![hr("DUS", "2", "12"), hr("PCS", "4", "1")])];
        assert_ne!(
            hash_dokumen(&a_items, &fisik(&a_items)),
            hash_dokumen(&c_items, &fisik(&c_items))
        );
        // Faktor ikut disidik: payload dengan faktor lama != faktor baru.
        let e_items = [bm(1, vec![hr("DUS", "2", "24"), hr("PCS", "3", "1")])];
        assert_ne!(
            hash_dokumen(&a_items, &fisik(&a_items)),
            hash_dokumen(&e_items, &fisik(&e_items))
        );
    }

    #[test]
    fn sidik_legacy_tidak_berubah() {
        // Kunci idempotensi payload lama harus tetap cocok dengan hash sebelum multi-satuan.
        let items = [br(1, "5"), br(2, "3")];
        assert_eq!(
            hash_dokumen(&items, &fisik(&items)),
            hash_dokumen(&[br(2, "3.00"), br(1, "5.0")], &fisik(&[br(2, "3.00"), br(1, "5.0")]))
        );
        assert_eq!(sidik_fisik(&Fisik::Langsung(d("5.00"))), "5");
    }
}
