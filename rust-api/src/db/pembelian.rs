use std::collections::BTreeMap;

use chrono::{Datelike, NaiveDate};
use oracle::{Connection, Row};
use rust_decimal::{Decimal, prelude::ToPrimitive};
use serde::{Deserialize, Serialize};
use serde_json::{Value, json};
use sha2::{Digest, Sha256};

use super::{Binds, dec, faktur, katalog, num, opt, tm};
use crate::{
    auth::Role,
    error::{AppError, AppResult},
};

const MAKS_RUPIAH: i64 = 9_000_000_000_000_000;
const MAKS_BARIS: usize = 100;

pub fn migrate(c: &Connection) -> AppResult<usize> {
    let mut n = 0;
    let schema = include_str!("pembelian.sql").replace("\r\n", "\n");
    for ddl in schema.split("\n/\n") {
        match c.execute(ddl.trim(), &[]) {
            Ok(_) => n += 1,
            Err(e) if matches!(e.oci_code(), Some(955 | 1408)) => {}
            Err(e) => return Err(e.into()),
        }
    }
    let ada: i64 = c.query_row_as(
        "SELECT COUNT(*) FROM USER_CONSTRAINTS WHERE CONSTRAINT_NAME = 'CK_MOB_USER_WEB_ROLE'",
        &[],
    )?;
    if ada == 0 {
        c.execute("ALTER TABLE MOB_USER ADD CONSTRAINT CK_MOB_USER_WEB_ROLE CHECK (ROLE IN ('KASIR','ADMIN','ANGGOTA','PEMBELIAN'))", &[])?;
        n += 1;
    }
    let lama: i64 = c.query_row_as(
        "SELECT COUNT(*) FROM USER_CONSTRAINTS WHERE CONSTRAINT_NAME = 'CK_MOB_USER_ROLE'",
        &[],
    )?;
    if lama > 0 {
        c.execute("ALTER TABLE MOB_USER DROP CONSTRAINT CK_MOB_USER_ROLE", &[])?;
        n += 1;
    }
    let tabel: i64 = c.query_row_as(
        "SELECT COUNT(*) FROM USER_TABLES WHERE TABLE_NAME IN ('POS_PEMBELIAN','POS_PEMBELIANDETAIL')", &[],
    )?;
    if tabel == 1 {
        return Err(AppError::Internal(
            "Tabel pembelian Oracle belum lengkap".into(),
        ));
    }
    if tabel == 2 {
        let guards = include_str!("pembelian_guard.sql").replace("\r\n", "\n");
        for (ddl, nama) in guards
            .split("\n/\n")
            .zip(["MOB_BELI_HEADER_GUARD", "MOB_BELI_DETAIL_GUARD"])
        {
            c.execute(ddl.trim(), &[])?;
            let errors: i64 = c.query_row_as_named(
                "SELECT COUNT(*) FROM USER_ERRORS WHERE NAME=:n AND TYPE='TRIGGER' AND ATTRIBUTE='ERROR'",
                &[("n", &nama)],
            )?;
            if errors > 0 {
                c.execute(&format!("ALTER TRIGGER {nama} DISABLE"), &[])?;
                return Err(AppError::Internal(format!(
                    "Trigger {nama} tidak valid; periksa USER_ERRORS"
                )));
            }
            n += 1;
        }
    }
    Ok(n)
}

#[derive(Clone)]
pub struct Operator {
    pub id: i64,
    pub username: String,
    pub nama: String,
    pub role: Role,
}

pub fn operator(c: &Connection, id: i64, admin_only: bool) -> AppResult<Operator> {
    let u = super::user::find_by_id(c, id)?
        .filter(|u| u.active && !u.must_change)
        .ok_or(AppError::Unauthorized)?;
    if u.role != Role::Admin && (admin_only || u.role != Role::Pembelian) {
        return Err(AppError::Forbidden(
            "Akun ini tidak berhak mengakses fitur tersebut".into(),
        ));
    }
    Ok(Operator {
        id,
        username: u.username,
        nama: u.full_name,
        role: u.role,
    })
}

#[derive(Serialize)]
struct Supplier {
    kode: String,
    nama: String,
}

#[derive(Serialize)]
struct Satuan {
    satuan: String,
    isi: String,
    barcode: String,
}

#[derive(Serialize)]
struct Barang {
    id: i64,
    kode: String,
    nama: String,
    beli: String,
    satuan: Vec<Satuan>,
}

pub fn referensi(c: &Connection) -> AppResult<Value> {
    let (hari, _) = faktur::sekarang(c)?;
    let suppliers = c
        .query(
            "SELECT KODE,NAMA FROM POS_SUPPLIER WHERE AKTIF='Y' ORDER BY NAMA,KODE",
            &[],
        )?
        .map(|row| -> AppResult<_> {
            let row = row?;
            Ok(Supplier {
                kode: row.get(0)?,
                nama: row.get(1)?,
            })
        })
        .collect::<AppResult<Vec<_>>>()?;
    let mut produk = BTreeMap::new();
    for row in c.query(
        &format!(
        "SELECT p.PRODUCTID,p.KODE_ITEM,p.PRODUCTNAME,{},p.SATUAN,p.BARCODE,s.SATUAN,{},s.BARCODE
         FROM POS_PRODUCT p LEFT JOIN POS_PRODUCT_SATUAN s ON s.PRODUCT_ID=p.PRODUCTID
         WHERE p.AKTIF='Y' ORDER BY p.PRODUCTID,s.SATUAN",
        tm("NVL(p.BELI,0)"), tm("s.ISI"),
    ),
        &[],
    )? {
        let r = row?;
        let id: i64 = r.get(0)?;
        let barang = match produk.entry(id) {
            std::collections::btree_map::Entry::Occupied(e) => e.into_mut(),
            std::collections::btree_map::Entry::Vacant(e) => e.insert(Barang {
                id,
                kode: r.get(1)?,
                nama: r.get(2)?,
                beli: r.get(3)?,
                satuan: vec![Satuan {
                    satuan: r.get(4)?,
                    isi: "1".into(),
                    barcode: r.get::<_, Option<String>>(5)?.unwrap_or_default(),
                }],
            }),
        };
        if let Some(satuan) = r.get::<_, Option<String>>(6)? {
            barang.satuan.push(Satuan {
                satuan,
                isi: r.get(7)?,
                barcode: r.get::<_, Option<String>>(8)?.unwrap_or_default(),
            });
        }
    }
    Ok(
        json!({"tanggal_server":hari, "supplier":suppliers, "barang":produk.into_values().collect::<Vec<_>>() }),
    )
}

#[derive(Debug, Serialize, Deserialize)]
pub struct BarisReq {
    pub product_id: i64,
    pub satuan: String,
    pub isi: String,
    pub qty: String,
    pub harga: i64,
    pub potongan: i64,
}

#[derive(Debug, Serialize, Deserialize)]
pub struct Posting {
    pub supplier_id: String,
    pub no_faktur: String,
    pub tanggal_faktur: NaiveDate,
    pub tanggal_terima: NaiveDate,
    pub jenis: Jenis,
    pub jatuh_tempo: Option<NaiveDate>,
    pub uang_muka: i64,
    pub metode: Option<Metode>,
    pub referensi: Option<String>,
    pub baris: Vec<BarisReq>,
}

#[derive(Debug, Serialize, Deserialize, PartialEq, Eq)]
#[serde(rename_all = "UPPERCASE")]
pub enum Jenis {
    Tunai,
    Tempo,
}

#[derive(Debug, Clone, Copy, Serialize, Deserialize)]
#[serde(rename_all = "UPPERCASE")]
pub enum Metode {
    Tunai,
    Transfer,
    Lainnya,
}

impl Metode {
    fn kode(self) -> &'static str {
        match self {
            Self::Tunai => "TUNAI",
            Self::Transfer => "TRANSFER",
            Self::Lainnya => "LAINNYA",
        }
    }
}

#[derive(Serialize, Deserialize)]
pub struct Bayar {
    pub tanggal: NaiveDate,
    pub metode: Metode,
    pub referensi: Option<String>,
    pub nominal: i64,
}

#[derive(Serialize, Deserialize)]
pub struct Alasan {
    pub alasan: String,
}

fn wajib(v: &str, panjang: usize, label: &str) -> AppResult<String> {
    let v = v.trim();
    if v.is_empty() || v.chars().count() > panjang || v.chars().any(char::is_control) {
        return Err(AppError::validation(format!(
            "{label} wajib diisi, maksimal {panjang} karakter"
        )));
    }
    Ok(v.to_owned())
}

fn referensi_bayar(v: Option<&str>) -> AppResult<Option<String>> {
    v.map(str::trim)
        .filter(|v| !v.is_empty())
        .map(|v| wajib(v, 100, "Referensi pembayaran"))
        .transpose()
}

pub(super) fn transaksi<T>(c: &Connection, f: impl FnOnce() -> AppResult<T>) -> AppResult<T> {
    let result = f().and_then(|hasil| {
        c.commit()?;
        Ok(hasil)
    });
    if result.is_err() {
        let _ = c.rollback();
    }
    result
}

pub(super) fn tulis<T: Serialize>(
    c: &Connection,
    u: &Operator,
    key: &str,
    operasi: &str,
    payload: &T,
    f: impl FnOnce() -> AppResult<String>,
) -> AppResult<String> {
    let hash: String = Sha256::digest(
        serde_json::to_vec(payload)
            .map_err(|e| AppError::Internal(format!("Payload pembelian: {e}")))?,
    )
    .iter()
    .map(|b| format!("{b:02x}"))
    .collect();
    transaksi(c, || {
        match c.execute_named(
            "INSERT INTO MOB_IDEMPOTENCY (USER_ID,OPERASI,IDEMPOTENCY_KEY,PAYLOAD_HASH) VALUES (:u,:op,:k,:h)",
            &[("u", &u.id), ("op", &operasi), ("k", &key), ("h", &hash)],
        ) {
            Ok(_) => {}
            Err(e) if e.oci_code() == Some(1) => {
                let (hasil, lama): (String, String) = c.query_row_as_named(
                    "SELECT RESOURCE_ID,PAYLOAD_HASH FROM MOB_IDEMPOTENCY WHERE USER_ID=:u AND OPERASI=:op AND IDEMPOTENCY_KEY=:k",
                    &[("u", &u.id), ("op", &operasi), ("k", &key)],
                )?;
                return if lama == hash { Ok(hasil) } else {
                    Err(AppError::Conflict("Kunci pengiriman sudah dipakai untuk data yang berbeda".into()))
                };
            }
            Err(e) => return Err(e.into()),
        }
        let hasil = f()?;
        c.execute_named(
            "UPDATE MOB_IDEMPOTENCY SET RESOURCE_ID=:hasil WHERE USER_ID=:u AND OPERASI=:op AND IDEMPOTENCY_KEY=:k",
            &[("hasil", &hasil), ("u", &u.id), ("op", &operasi), ("k", &key)],
        )?;
        Ok(hasil)
    })
}

fn nomor(c: &Connection, tahun: i32) -> AppResult<String> {
    match c.execute_named(
        "INSERT INTO MOB_BELI_NOMOR (TAHUN,NOMOR) VALUES (:t,0)",
        &[("t", &tahun)],
    ) {
        Ok(_) => {}
        Err(e) if e.oci_code() == Some(1) => {}
        Err(e) => return Err(e.into()),
    }
    let terakhir: i64 = c.query_row_as_named(
        "SELECT NOMOR FROM MOB_BELI_NOMOR WHERE TAHUN=:t FOR UPDATE",
        &[("t", &tahun)],
    )?;
    if terakhir >= 999_999 {
        return Err(AppError::Conflict(
            "Nomor pembelian web tahun ini habis".into(),
        ));
    }
    let berikut = terakhir + 1;
    c.execute_named(
        "UPDATE MOB_BELI_NOMOR SET NOMOR=:n WHERE TAHUN=:t",
        &[("n", &berikut), ("t", &tahun)],
    )?;
    Ok(format!("BW-{:02}-{berikut:06}", tahun % 100))
}

fn cek_penerimaan(c: &Connection, tanggal: NaiveDate, hari: NaiveDate) -> AppResult<()> {
    if tanggal > hari || tanggal.year() != hari.year() {
        return Err(AppError::validation(
            "Tanggal penerimaan harus berada dalam tahun stok berjalan dan tidak boleh melebihi hari ini",
        ));
    }
    faktur::cek_periode(c, tanggal)
}

fn cek_opname(c: &Connection, kode: &str, tanggal: NaiveDate) -> AppResult<()> {
    let ada: i64 = c.query_row_as_named(
        "SELECT COUNT(*) FROM POS_STOCKOPNAME WHERE KODE_BARANG=:k AND TANGGAL>=:t AND ROWNUM=1",
        &[("k", &kode), ("t", &tanggal)],
    )?;
    if ada > 0 {
        return Err(AppError::rule(
            "BENTROK_OPNAME",
            format!("Tanggal penerimaan untuk {kode} berada pada atau sebelum opname terakhir"),
        ));
    }
    Ok(())
}

struct Baris {
    kode: String,
    nama: String,
    dasar: String,
    qty: Decimal,
    isi: Decimal,
    bruto: i64,
}

fn qty_dan_bruto(b: &BarisReq) -> AppResult<(Decimal, i64)> {
    let qty = b
        .qty
        .parse::<Decimal>()
        .map_err(|_| AppError::validation("Jumlah barang tidak valid"))?;
    if qty <= Decimal::ZERO || qty > Decimal::from(999_999) || qty.normalize().scale() > 3 {
        return Err(AppError::validation(
            "Jumlah harus positif, maksimal 999.999 dengan tiga angka desimal",
        ));
    }
    if !(1..=MAKS_RUPIAH).contains(&b.harga) || b.potongan < 0 {
        return Err(AppError::validation(
            "Harga beli harus positif dan potongan tidak boleh negatif",
        ));
    }
    let bruto = (qty * Decimal::from(b.harga))
        .round_dp_with_strategy(0, rust_decimal::RoundingStrategy::MidpointAwayFromZero)
        .to_i64()
        .filter(|v| (1..=MAKS_RUPIAH).contains(v))
        .ok_or_else(|| AppError::validation("Bruto barang di luar batas rupiah"))?;
    if b.potongan > bruto {
        return Err(AppError::validation("Potongan melebihi bruto barang"));
    }
    Ok((qty, bruto))
}

fn siapkan_baris(c: &Connection, req: &Posting) -> AppResult<Vec<Baris>> {
    let mut ids: Vec<_> = req.baris.iter().map(|b| b.product_id).collect();
    ids.sort_unstable();
    ids.dedup();
    let mut produk = BTreeMap::new();
    for id in ids {
        let r = opt(c.query_row_named(
            "SELECT KODE_ITEM,PRODUCTNAME,SATUAN,AKTIF FROM POS_PRODUCT WHERE PRODUCTID=:id FOR UPDATE",
            &[("id", &id)],
        ))?.ok_or_else(|| AppError::rule("BARANG_BERUBAH", format!("Barang #{id} sudah tidak tersedia")))?;
        let (kode, nama, dasar, aktif): (String, String, String, String) =
            (r.get(0)?, r.get(1)?, r.get(2)?, r.get(3)?);
        if aktif != "Y" {
            return Err(AppError::rule(
                "BARANG_BERUBAH",
                format!("Barang {kode} sudah nonaktif"),
            ));
        }
        cek_opname(c, &kode, req.tanggal_terima)?;
        produk.insert(id, (kode, nama, dasar));
    }
    req.baris
        .iter()
        .map(|b| {
            let (kode, nama, dasar) = &produk[&b.product_id];
            let satuan = wajib(&b.satuan, 20, "Satuan")?;
            let isi = if dasar.eq_ignore_ascii_case(&satuan) {
                Decimal::ONE
            } else {
                katalog::satuan_jual(c, b.product_id, &satuan)?
                    .ok_or_else(|| {
                        AppError::rule(
                            "SATUAN_BERUBAH",
                            format!("Satuan {satuan} untuk {kode} sudah tidak tersedia"),
                        )
                    })?
                    .isi
            };
            if b.isi.parse::<Decimal>().ok() != Some(isi) || isi <= Decimal::ZERO {
                return Err(AppError::rule(
                    "SATUAN_BERUBAH",
                    format!("Isi {satuan} untuk {kode} berubah. Pilih ulang satuan"),
                ));
            }
            let (qty, bruto) = qty_dan_bruto(b)?;
            let dasar_qty = qty
                .checked_mul(isi)
                .filter(|v| *v <= Decimal::from(999_999_999) && v.normalize().scale() <= 8);
            if dasar_qty.is_none() {
                return Err(AppError::validation(
                    "Jumlah satuan dasar melampaui batas atau presisi",
                ));
            }
            Ok(Baris {
                kode: kode.clone(),
                nama: nama.clone(),
                dasar: dasar.clone(),
                qty,
                isi,
                bruto,
            })
        })
        .collect()
}

fn simpan_baris_pos(
    c: &Connection,
    no: &str,
    purchase_id: i64,
    n: i64,
    req: &BarisReq,
    b: &Baris,
) -> AppResult<()> {
    let qty_dasar = (b.qty * b.isi).normalize().to_string();
    let hpp = (Decimal::from(req.harga) / b.isi)
        .round_dp(8)
        .normalize()
        .to_string();
    let total = b.bruto - req.potongan;
    c.execute_named(
        &format!("INSERT INTO POS_PEMBELIANDETAIL
         (PURCHASE_ID,NO_TRANSAKSI,BARIS,PRODUCT_ID,KODE_BARANG,NAMA_BARANG,SATUAN,QUANTITY,HARGA_BELI,BRUTO,POTONGAN,TOTAL)
         VALUES (:id,:no,:n,:p,:kode,:nama,:dasar,{},{},:bruto,:pot,:total)", num("qty"), num("hpp")),
        &[("id", &purchase_id), ("no", &no), ("n", &n), ("p", &req.product_id), ("kode", &b.kode), ("nama", &b.nama),
          ("dasar", &b.dasar), ("qty", &qty_dasar), ("hpp", &hpp), ("bruto", &b.bruto), ("pot", &req.potongan), ("total", &total)],
    )?;
    let tersimpan: String = c.query_row_as_named(
        &format!(
            "SELECT {} FROM POS_PEMBELIANDETAIL WHERE PURCHASE_ID=:id AND BARIS=:n",
            tm("QUANTITY")
        ),
        &[("id", &purchase_id), ("n", &n)],
    )?;
    if dec(&tersimpan)? != b.qty * b.isi {
        return Err(AppError::validation(
            "Jumlah dasar terlalu presisi untuk skema stok Oracle",
        ));
    }
    Ok(())
}

fn simpan_snapshot(c: &Connection, no: &str, n: i64, req: &BarisReq, b: &Baris) -> AppResult<()> {
    let qty = b.qty.normalize().to_string();
    let isi = b.isi.normalize().to_string();
    c.execute_named(
        &format!("INSERT INTO MOB_BELI_BARIS
         (NO_TRANSAKSI,BARIS,PRODUCT_ID,KODE_BARANG,NAMA_BARANG,SATUAN,SATUAN_DASAR,ISI,QTY,HARGA,POTONGAN)
         VALUES (:no,:n,:p,:kode,:nama,:sat,:dasar,{},{},:harga,:pot)", num("isi"), num("qty")),
        &[("no", &no), ("n", &n), ("p", &req.product_id), ("kode", &b.kode), ("nama", &b.nama),
          ("sat", &req.satuan.trim()), ("dasar", &b.dasar), ("isi", &isi), ("qty", &qty), ("harga", &req.harga), ("pot", &req.potongan)],
    )?;
    Ok(())
}

fn tgl_bayar(tanggal: NaiveDate, faktur: NaiveDate, hari: NaiveDate) -> AppResult<()> {
    if !(faktur..=hari).contains(&tanggal) {
        return Err(AppError::validation(
            "Tanggal pembayaran harus di antara tanggal faktur dan hari ini",
        ));
    }
    Ok(())
}

fn total_rupiah(mut nilai: impl Iterator<Item = i64>) -> AppResult<i64> {
    nilai.try_fold(0_i64, |a, b| {
        a.checked_add(b)
            .filter(|n| *n <= MAKS_RUPIAH)
            .ok_or_else(|| AppError::validation("Total pembelian melampaui batas rupiah"))
    })
}

pub fn posting(c: &Connection, u: &Operator, key: &str, req: &Posting) -> AppResult<String> {
    tulis(c, u, key, "BELI_POST", req, || {
        let supplier_id = wajib(&req.supplier_id, 50, "Supplier")?;
        let no_faktur = wajib(&req.no_faktur, 100, "Nomor faktur supplier")?;
        let (hari, _) = faktur::sekarang(c)?;
        cek_penerimaan(c, req.tanggal_terima, hari)?;
        if req.tanggal_faktur > hari {
            return Err(AppError::validation(
                "Tanggal faktur tidak boleh melebihi hari ini",
            ));
        }
        let supplier = opt(c.query_row_named(
            "SELECT NAMA,AKTIF FROM POS_SUPPLIER WHERE KODE=:k FOR UPDATE",
            &[("k", &supplier_id)],
        ))?
        .ok_or_else(|| AppError::rule("SUPPLIER_BERUBAH", "Supplier sudah tidak tersedia"))?;
        let supplier_nama: String = supplier.get(0)?;
        if supplier.get::<_, String>(1)? != "Y" {
            return Err(AppError::rule(
                "SUPPLIER_BERUBAH",
                "Supplier sudah nonaktif",
            ));
        }
        if req.baris.is_empty() || req.baris.len() > MAKS_BARIS {
            return Err(AppError::validation(
                "Pembelian harus berisi 1–100 baris barang",
            ));
        }
        let baris = siapkan_baris(c, req)?;
        let bruto = total_rupiah(baris.iter().map(|b| b.bruto))?;
        let potongan = total_rupiah(req.baris.iter().map(|b| b.potongan))?;
        let total = bruto - potongan;
        if total <= 0 {
            return Err(AppError::validation("Total faktur harus lebih dari nol"));
        }
        let (jenis, termin, bayar) = match req.jenis {
            Jenis::Tunai if req.jatuh_tempo.is_none() && req.uang_muka == 0 => ("TUNAI", 0, total),
            Jenis::Tempo if (0..total).contains(&req.uang_muka) => {
                let jatuh = req
                    .jatuh_tempo
                    .filter(|t| *t >= req.tanggal_faktur)
                    .ok_or_else(|| {
                        AppError::validation(
                            "Jatuh tempo wajib dan minimal sama dengan tanggal faktur",
                        )
                    })?;
                let hari_tempo = (jatuh - req.tanggal_faktur).num_days();
                if hari_tempo > 3650 {
                    return Err(AppError::validation("Jatuh tempo terlalu jauh"));
                }
                ("TEMPO", hari_tempo, req.uang_muka)
            }
            _ => {
                return Err(AppError::validation(
                    "Periksa jenis pembayaran, uang muka, dan jatuh tempo",
                ));
            }
        };
        let referensi = referensi_bayar(req.referensi.as_deref())?;
        if (bayar > 0) != req.metode.is_some() || (bayar == 0 && referensi.is_some()) {
            return Err(AppError::validation("Metode wajib bila ada pembayaran"));
        }
        cek_guard(c)?;
        let no = nomor(c, hari.year())?;
        let stmt = c.execute_named(
            "INSERT INTO POS_PEMBELIAN
             (NO_TRANSAKSI,TANGGAL,SUPPLIER_ID,BRUTO,POTONGAN,TOTAL,TERMIN,USERID,NAMA_USER)
             VALUES (:no,:tgl,:s,:br,:po,:neto,:te,:usr,:nama) RETURNING PURCHASE_ID INTO :id",
            &[
                ("no", &no),
                ("tgl", &req.tanggal_terima),
                ("s", &supplier_id),
                ("br", &bruto),
                ("po", &potongan),
                ("neto", &total),
                ("te", &termin),
                ("usr", &u.username),
                ("nama", &u.nama),
                ("id", &None::<i64>),
            ],
        )?;
        let id: i64 = stmt
            .returned_values("id")?
            .into_iter()
            .next()
            .ok_or_else(|| AppError::Internal("Oracle tidak mengembalikan PURCHASE_ID".into()))?;
        for (i, (r, b)) in req.baris.iter().zip(&baris).enumerate() {
            simpan_baris_pos(c, &no, id, i as i64 + 1, r, b)?;
        }
        match c.execute_named(
            "INSERT INTO MOB_BELI (NO_TRANSAKSI,PURCHASE_ID,SUPPLIER_ID,SUPPLIER_NAMA,NO_FAKTUR,TANGGAL_FAKTUR,
             TANGGAL_TERIMA,JATUH_TEMPO,JENIS,BRUTO,POTONGAN,TOTAL,USER_ID,USER_NAMA)
             VALUES (:no,:id,:s,:sn,:nf,:tf,:tt,:jt,:j,:br,:po,:neto,:u,:nama)",
            &[("no", &no), ("id", &id), ("s", &supplier_id), ("sn", &supplier_nama), ("nf", &no_faktur),
              ("tf", &req.tanggal_faktur), ("tt", &req.tanggal_terima), ("jt", &req.jatuh_tempo), ("j", &jenis),
              ("br", &bruto), ("po", &potongan), ("neto", &total), ("u", &u.id), ("nama", &u.nama)],
        ) {
            Ok(_) => {}
            Err(e) if e.oci_code() == Some(1) => return Err(AppError::rule("FAKTUR_DUPLIKAT", "Nomor faktur ini sudah tercatat untuk supplier tersebut")),
            Err(e) => return Err(e.into()),
        }
        for (i, (r, b)) in req.baris.iter().zip(&baris).enumerate() {
            simpan_snapshot(c, &no, i as i64 + 1, r, b)?;
        }
        if let Some(metode) = req.metode {
            let p = Bayar {
                tanggal: req.tanggal_terima.max(req.tanggal_faktur),
                metode,
                referensi,
                nominal: bayar,
            };
            entri(c, u, &no, &p, None)?;
        }
        Ok(no)
    })
}

pub fn cek_guard(c: &Connection) -> AppResult<()> {
    let n: i64 = c.query_row_as(
        "SELECT COUNT(*) FROM USER_TRIGGERS t JOIN USER_OBJECTS o
         ON o.OBJECT_NAME=t.TRIGGER_NAME AND o.OBJECT_TYPE='TRIGGER'
         WHERE t.TRIGGER_NAME IN ('MOB_BELI_HEADER_GUARD','MOB_BELI_DETAIL_GUARD')
           AND t.STATUS='ENABLED' AND o.STATUS='VALID'",
        &[],
    )?;
    if n != 2 {
        return Err(AppError::Internal(
            "Proteksi faktur pembelian Oracle belum aktif".into(),
        ));
    }
    Ok(())
}

struct KunciFaktur {
    id: i64,
    tanggal: NaiveDate,
    tanggal_faktur: NaiveDate,
    total: i64,
    bayar: i64,
}

fn kunci_faktur(c: &Connection, no: &str) -> AppResult<KunciFaktur> {
    let r = opt(c.query_row_named(
        "SELECT PURCHASE_ID,TRUNC(TANGGAL_TERIMA),TRUNC(TANGGAL_FAKTUR),TOTAL,CASE WHEN BATAL_AT IS NULL THEN 0 ELSE 1 END
         FROM MOB_BELI WHERE NO_TRANSAKSI=:no FOR UPDATE", &[("no", &no)],
    ))?.ok_or_else(|| AppError::NotFound("Faktur web tidak ditemukan".into()))?;
    if r.get::<_, i64>(4)? == 1 {
        return Err(AppError::Conflict("Faktur sudah dibatalkan".into()));
    }
    let bayar = c.query_row_as_named(
        "SELECT NVL(SUM(NOMINAL),0) FROM MOB_BELI_BAYAR WHERE NO_TRANSAKSI=:no",
        &[("no", &no)],
    )?;
    Ok(KunciFaktur {
        id: r.get(0)?,
        tanggal: r.get(1)?,
        tanggal_faktur: r.get(2)?,
        total: r.get(3)?,
        bayar,
    })
}

fn entri(
    c: &Connection,
    u: &Operator,
    no: &str,
    p: &Bayar,
    balik: Option<(i64, &str)>,
) -> AppResult<String> {
    let referensi = referensi_bayar(p.referensi.as_deref())?;
    let balik_id = balik.map(|b| b.0);
    let alasan = balik.map(|b| b.1);
    let metode = p.metode.kode();
    let stmt = c.execute_named(
        "INSERT INTO MOB_BELI_BAYAR (NO_TRANSAKSI,TANGGAL,METODE,REFERENSI,NOMINAL,BALIK_ID,ALASAN,USER_ID,USER_NAMA)
         VALUES (:no,:tgl,:m,:r,:n,:b,:a,:u,:nama) RETURNING ID INTO :id",
        &[("no", &no), ("tgl", &p.tanggal), ("m", &metode), ("r", &referensi), ("n", &p.nominal),
          ("b", &balik_id), ("a", &alasan), ("u", &u.id), ("nama", &u.nama), ("id", &None::<i64>)],
    )?;
    let id: i64 = stmt
        .returned_values("id")?
        .into_iter()
        .next()
        .ok_or_else(|| AppError::Internal("Oracle tidak mengembalikan ID pembayaran".into()))?;
    Ok(id.to_string())
}

pub fn bayar(c: &Connection, u: &Operator, no: &str, key: &str, p: &Bayar) -> AppResult<String> {
    tulis(c, u, key, "BELI_BAYAR", &(no, p), || {
        let f = kunci_faktur(c, no)?;
        tgl_bayar(p.tanggal, f.tanggal_faktur, faktur::sekarang(c)?.0)?;
        if p.nominal <= 0 || p.nominal > f.total - f.bayar {
            return Err(AppError::rule(
                "SALDO_BERUBAH",
                "Nominal pembayaran harus positif dan tidak boleh melebihi sisa hutang",
            ));
        }
        entri(c, u, no, p, None)
    })
}

pub fn balik(
    c: &Connection,
    u: &Operator,
    no: &str,
    id: i64,
    key: &str,
    req: &Alasan,
) -> AppResult<String> {
    tulis(c, u, key, "BELI_BALIK", &(no, id, req), || {
        kunci_faktur(c, no)?;
        let alasan = wajib(&req.alasan, 250, "Alasan pembalikan")?;
        let r = opt(c.query_row_named(
            "SELECT p.NOMINAL,p.METODE,p.REFERENSI,
                    (SELECT COUNT(*) FROM MOB_BELI_BAYAR b WHERE b.BALIK_ID=p.ID)
             FROM MOB_BELI_BAYAR p WHERE p.ID=:id AND p.NO_TRANSAKSI=:no AND p.BALIK_ID IS NULL",
            &[("id", &id), ("no", &no)],
        ))?
        .ok_or_else(|| AppError::NotFound("Pembayaran tidak ditemukan".into()))?;
        if r.get::<_, i64>(3)? > 0 {
            return Err(AppError::Conflict("Pembayaran sudah dibalik".into()));
        }
        let metode = match r.get::<_, String>(1)?.as_str() {
            "TUNAI" => Metode::Tunai,
            "TRANSFER" => Metode::Transfer,
            _ => Metode::Lainnya,
        };
        entri(
            c,
            u,
            no,
            &Bayar {
                tanggal: faktur::sekarang(c)?.0,
                metode,
                referensi: r.get(2)?,
                nominal: -r.get::<_, i64>(0)?,
            },
            Some((id, &alasan)),
        )
    })
}

pub fn batal(c: &Connection, u: &Operator, no: &str, key: &str, req: &Alasan) -> AppResult<String> {
    tulis(c, u, key, "BELI_BATAL", &(no, req), || {
        // Urutan sama dengan posting: supplier dikunci sebelum faktur dan produk.
        let supplier: Option<String> = opt(c.query_row_as_named(
            "SELECT s.KODE FROM POS_SUPPLIER s WHERE s.KODE=(SELECT b.SUPPLIER_ID FROM MOB_BELI b WHERE b.NO_TRANSAKSI=:no) FOR UPDATE",
            &[("no", &no)],
        ))?;
        if supplier.is_none() {
            return Err(AppError::NotFound(
                "Supplier faktur web tidak ditemukan".into(),
            ));
        }
        let f = kunci_faktur(c, no)?;
        let alasan = wajib(&req.alasan, 250, "Alasan pembatalan")?;
        if f.bayar != 0 {
            return Err(AppError::Conflict(
                "Balik seluruh pembayaran sebelum membatalkan faktur".into(),
            ));
        }
        cek_guard(c)?;
        cek_penerimaan(c, f.tanggal, faktur::sekarang(c)?.0)?;
        let sql = "SELECT p.PRODUCTID FROM POS_PRODUCT p WHERE EXISTS (
            SELECT 1 FROM MOB_BELI_BARIS b WHERE b.NO_TRANSAKSI=:no AND b.PRODUCT_ID=p.PRODUCTID)
            ORDER BY p.PRODUCTID FOR UPDATE";
        for r in c.query_named(sql, &[("no", &no)])? {
            r?;
        }
        for r in c.query_named(
            "SELECT DISTINCT KODE_BARANG FROM MOB_BELI_BARIS WHERE NO_TRANSAKSI=:no",
            &[("no", &no)],
        )? {
            cek_opname(c, &r?.get::<_, String>(0)?, f.tanggal)?;
        }
        c.execute_named(
            "UPDATE MOB_BELI SET BATAL_AT=SYSTIMESTAMP,BATAL_USER=:u,BATAL_NAMA=:nama,BATAL_ALASAN=:a WHERE NO_TRANSAKSI=:no",
            &[("u", &u.id), ("nama", &u.nama), ("a", &alasan), ("no", &no)],
        )?;
        c.execute_named(
            "DELETE FROM POS_PEMBELIANDETAIL WHERE PURCHASE_ID=:id",
            &[("id", &f.id)],
        )?;
        let stmt = c.execute_named(
            "DELETE FROM POS_PEMBELIAN WHERE PURCHASE_ID=:id",
            &[("id", &f.id)],
        )?;
        if stmt.row_count()? != 1 {
            return Err(AppError::Internal("Header faktur aktif hilang".into()));
        }
        Ok(no.to_owned())
    })
}

const WEB: &str = "SELECT b.NO_TRANSAKSI,b.SUPPLIER_ID,b.SUPPLIER_NAMA,b.NO_FAKTUR,b.TANGGAL_FAKTUR,
    b.TANGGAL_TERIMA,b.JATUH_TEMPO,b.JENIS,b.BRUTO,b.POTONGAN,b.TOTAL,b.USER_NAMA,b.USER_ID,
    TO_CHAR(b.POSTED_AT,'YYYY-MM-DD HH24:MI:SS') POSTED,
    TO_CHAR(b.BATAL_AT,'YYYY-MM-DD HH24:MI:SS') BATAL,b.BATAL_NAMA,b.BATAL_ALASAN,
    NVL(p.BAYAR,0) BAYAR,'WEB' ASAL
    FROM MOB_BELI b LEFT JOIN (SELECT NO_TRANSAKSI,SUM(NOMINAL) BAYAR FROM MOB_BELI_BAYAR GROUP BY NO_TRANSAKSI) p
    ON p.NO_TRANSAKSI=b.NO_TRANSAKSI";

const LAMA: &str = "SELECT b.NO_TRANSAKSI,b.SUPPLIER_ID,NVL(s.NAMA,b.SUPPLIER_ID),NULL,NULL,
    b.TANGGAL,NULL,NULL,NVL(b.BRUTO,0),NVL(b.POTONGAN,0),NVL(b.TOTAL,0),b.NAMA_USER,NULL,
    NULL,NULL,NULL,NULL,NULL,'BACKOFFICE'
    FROM POS_PEMBELIAN b LEFT JOIN POS_SUPPLIER s ON s.KODE=b.SUPPLIER_ID
    WHERE b.NO_TRANSAKSI IS NOT NULL AND b.TANGGAL IS NOT NULL AND NOT EXISTS
    (SELECT 1 FROM MOB_BELI w WHERE w.PURCHASE_ID=b.PURCHASE_ID)";

fn faktur_json(r: &Row, hari: NaiveDate) -> AppResult<Value> {
    let total = dec(&r.get::<_, String>(10)?)?;
    let tempo: Option<NaiveDate> = r.get(6)?;
    let batal: Option<String> = r.get(14)?;
    let bayar: Option<i64> = r.get(17)?;
    let sisa = bayar.map(|n| {
        if batal.is_some() {
            0
        } else {
            total.to_i64().unwrap_or(0) - n
        }
    });
    let status = match (batal.is_some(), sisa) {
        (true, _) => "BATAL",
        (_, None) => "SALDO_BELUM_DILACAK",
        (_, Some(0)) => "LUNAS",
        (_, Some(_)) if tempo.is_some_and(|d| d < hari) => "TERLAMBAT",
        (_, Some(_)) => "TERBUKA",
    };
    Ok(json!({
        "no_transaksi":r.get::<_, String>(0)?, "supplier_id":r.get::<_, Option<String>>(1)?,
        "supplier_nama":r.get::<_, Option<String>>(2)?, "no_faktur":r.get::<_, Option<String>>(3)?,
        "tanggal_faktur":r.get::<_, Option<NaiveDate>>(4)?, "tanggal_terima":r.get::<_, NaiveDate>(5)?,
        "jatuh_tempo":tempo, "jenis":r.get::<_, Option<String>>(7)?, "bruto":r.get::<_, String>(8)?,
        "potongan":r.get::<_, String>(9)?, "total":total.normalize().to_string(), "operator":r.get::<_, Option<String>>(11)?,
        "posted_at":r.get::<_, Option<String>>(13)?, "batal_at":batal, "batal_nama":r.get::<_, Option<String>>(15)?,
        "batal_alasan":r.get::<_, Option<String>>(16)?, "sudah_bayar":bayar, "sisa":sisa, "status":status,
        "asal":r.get::<_, String>(18)?,
    }))
}

#[derive(Deserialize)]
pub struct Filter {
    pub q: Option<String>,
    pub supplier: Option<String>,
    pub dari: Option<NaiveDate>,
    pub sampai: Option<NaiveDate>,
    pub page: Option<u32>,
    pub terlambat: Option<bool>,
}

fn daftar(c: &Connection, u: &Operator, filter: &Filter, khusus_hutang: bool) -> AppResult<Value> {
    let hari = faktur::sekarang(c)?.0;
    let page = filter.page.unwrap_or(1).clamp(1, 100_000);
    let mut b = Binds::default();
    b.add("offset_param", (page - 1) * 30);
    let sumber = if u.role == Role::Pembelian {
        b.add("uid", u.id);
        format!("{WEB} WHERE b.USER_ID=TO_NUMBER(:uid)")
    } else if khusus_hutang {
        WEB.to_owned()
    } else {
        format!("{WEB} UNION ALL {LAMA}")
    };
    let mut where_sql = String::from("1=1");
    if let Some(q) = filter.q.as_deref().filter(|q| !q.trim().is_empty()) {
        b.add("q", super::like(&wajib(q, 100, "Pencarian")?));
        where_sql.push_str(
            " AND (UPPER(NO_TRANSAKSI) LIKE :q ESCAPE '\\' OR UPPER(NO_FAKTUR) LIKE :q ESCAPE '\\'
            OR UPPER(SUPPLIER_NAMA) LIKE :q ESCAPE '\\')",
        );
    }
    if let Some(s) = filter.supplier.as_deref().filter(|s| !s.trim().is_empty()) {
        b.add("supplier", wajib(s, 50, "Supplier")?);
        where_sql.push_str(" AND SUPPLIER_ID=:supplier");
    }
    if let Some(t) = filter.dari {
        b.add("dari", t);
        where_sql.push_str(" AND TANGGAL_TERIMA>=TO_DATE(:dari,'YYYY-MM-DD')");
    }
    if let Some(t) = filter.sampai {
        b.add("sampai", t);
        where_sql.push_str(" AND TANGGAL_TERIMA<TO_DATE(:sampai,'YYYY-MM-DD')+1");
    }
    if khusus_hutang {
        where_sql.push_str(" AND BATAL IS NULL AND TOTAL>BAYAR");
        if filter.terlambat == Some(true) {
            where_sql.push_str(" AND JATUH_TEMPO<TRUNC(SYSDATE)");
        }
    }
    let urutan = if khusus_hutang {
        "JATUH_TEMPO NULLS FIRST, TANGGAL_TERIMA, NO_TRANSAKSI"
    } else {
        "TANGGAL_TERIMA DESC, NO_TRANSAKSI DESC"
    };
    let sql = format!(
        "SELECT NO_TRANSAKSI,SUPPLIER_ID,SUPPLIER_NAMA,NO_FAKTUR,TANGGAL_FAKTUR,TANGGAL_TERIMA,JATUH_TEMPO,JENIS,
         {},{},{},USER_NAMA,USER_ID,POSTED,BATAL,BATAL_NAMA,BATAL_ALASAN,BAYAR,ASAL
         FROM ({sumber}) WHERE {where_sql} ORDER BY {urutan}
         OFFSET TO_NUMBER(:offset_param) ROWS FETCH NEXT 31 ROWS ONLY",
        tm("BRUTO"), tm("POTONGAN"), tm("TOTAL"),
    );
    let mut items = Vec::new();
    b.with(|params| -> AppResult<()> {
        for row in c.query_named(&sql, params)? {
            items.push(faktur_json(&row?, hari)?);
        }
        Ok(())
    })?;
    let ada_lagi = items.len() > 30;
    items.truncate(30);
    Ok(json!({"items":items, "ada_lagi":ada_lagi, "page":page, "tanggal_server":hari}))
}

pub fn riwayat(c: &Connection, u: &Operator, filter: &Filter) -> AppResult<Value> {
    daftar(c, u, filter, false)
}

pub fn hutang(c: &Connection, u: &Operator, filter: &Filter) -> AppResult<Value> {
    let mut data = daftar(c, u, filter, true)?;
    let sql = format!("SELECT SUPPLIER_ID,MAX(SUPPLIER_NAMA),{}, {},COUNT(*)
        FROM ({WEB}) WHERE BATAL IS NULL AND TOTAL>BAYAR GROUP BY SUPPLIER_ID ORDER BY MAX(SUPPLIER_NAMA),SUPPLIER_ID",
        tm("SUM(TOTAL-BAYAR)"), tm("SUM(CASE WHEN JATUH_TEMPO<TRUNC(SYSDATE) THEN TOTAL-BAYAR ELSE 0 END)"));
    let mut ringkasan = Vec::new();
    for row in c.query(&sql, &[])? {
        let r = row?;
        ringkasan.push(json!({
            "supplier_id":r.get::<_, String>(0)?, "supplier_nama":r.get::<_, String>(1)?,
            "saldo":r.get::<_, String>(2)?, "terlambat":r.get::<_, String>(3)?, "faktur":r.get::<_, i64>(4)?,
        }));
    }
    data["supplier"] = json!(ringkasan);
    Ok(data)
}

pub fn detail(c: &Connection, u: &Operator, no: &str) -> AppResult<Value> {
    let hari = faktur::sekarang(c)?.0;
    let select = format!("SELECT NO_TRANSAKSI,SUPPLIER_ID,SUPPLIER_NAMA,NO_FAKTUR,TANGGAL_FAKTUR,TANGGAL_TERIMA,
        JATUH_TEMPO,JENIS,{},{},{},USER_NAMA,USER_ID,POSTED,BATAL,BATAL_NAMA,BATAL_ALASAN,BAYAR,ASAL FROM",
        tm("BRUTO"), tm("POTONGAN"), tm("TOTAL"));
    let sql = format!("{select} ({WEB}) WHERE NO_TRANSAKSI=:no AND (:admin=1 OR USER_ID=:uid)");
    let admin = i64::from(u.role == Role::Admin);
    let web = opt(c.query_row_named(&sql, &[("no", &no), ("admin", &admin), ("uid", &u.id)]))?;
    let (r, is_web) = if let Some(r) = web {
        (r, true)
    } else if admin == 1 {
        let sql = format!("{select} ({WEB} WHERE 1=0 UNION ALL {LAMA}) WHERE NO_TRANSAKSI=:no");
        let r = opt(c.query_row_named(&sql, &[("no", &no)]))?
            .ok_or_else(|| AppError::NotFound("Faktur tidak ditemukan".into()))?;
        (r, false)
    } else {
        return Err(AppError::NotFound("Faktur tidak ditemukan".into()));
    };
    let mut hasil = faktur_json(&r, hari)?;
    let (lines, payments) = if is_web {
        let sql = format!(
            "SELECT KODE_BARANG,NAMA_BARANG,SATUAN,SATUAN_DASAR,{},{},{},{},
            {} FROM MOB_BELI_BARIS WHERE NO_TRANSAKSI=:no ORDER BY BARIS",
            tm("QTY"),
            tm("ISI"),
            tm("HARGA"),
            tm("POTONGAN"),
            tm("ROUND(QTY*HARGA)-POTONGAN")
        );
        (
            ambil_baris(c, &sql, no)?,
            if admin == 1 {
                pembayaran(c, no)?
            } else {
                Vec::new()
            },
        )
    } else {
        let sql = format!(
            "SELECT d.KODE_BARANG,d.NAMA_BARANG,d.SATUAN,d.SATUAN,{},{},{},{},{}
            FROM POS_PEMBELIANDETAIL d JOIN POS_PEMBELIAN b ON b.PURCHASE_ID=d.PURCHASE_ID
            WHERE b.NO_TRANSAKSI=:no ORDER BY d.BARIS",
            tm("d.QUANTITY"),
            tm("1"),
            tm("d.HARGA_BELI"),
            tm("NVL(d.POTONGAN,0)"),
            tm("NVL(d.TOTAL,0)")
        );
        (ambil_baris(c, &sql, no)?, Vec::new())
    };
    hasil["baris"] = json!(lines);
    hasil["pembayaran"] = json!(payments);
    hasil["tanggal_server"] = json!(hari);
    Ok(hasil)
}

fn ambil_baris(c: &Connection, sql: &str, no: &str) -> AppResult<Vec<Value>> {
    c.query_named(sql, &[("no", &no)])?.map(|r| -> AppResult<_> {
        let r = r?;
        Ok(json!({
            "kode":r.get::<_, Option<String>>(0)?, "nama":r.get::<_, Option<String>>(1)?,
            "satuan":r.get::<_, Option<String>>(2)?, "satuan_dasar":r.get::<_, Option<String>>(3)?,
            "qty":r.get::<_, Option<String>>(4)?, "isi":r.get::<_, String>(5)?,
            "harga":r.get::<_, Option<String>>(6)?, "potongan":r.get::<_, String>(7)?, "total":r.get::<_, String>(8)?,
        }))
    }).collect()
}

fn pembayaran(c: &Connection, no: &str) -> AppResult<Vec<Value>> {
    c.query_named("SELECT p.ID,p.TANGGAL,p.METODE,p.REFERENSI,p.NOMINAL,p.BALIK_ID,p.ALASAN,p.USER_NAMA,
            TO_CHAR(p.CREATED_AT,'YYYY-MM-DD HH24:MI:SS'),CASE WHEN r.ID IS NULL THEN 0 ELSE 1 END
        FROM MOB_BELI_BAYAR p LEFT JOIN MOB_BELI_BAYAR r ON r.BALIK_ID=p.ID
        WHERE p.NO_TRANSAKSI=:no ORDER BY p.ID", &[("no", &no)])?.map(|r| -> AppResult<_> {
        let r = r?;
        Ok(json!({
            "id":r.get::<_, i64>(0)?, "tanggal":r.get::<_, NaiveDate>(1)?, "metode":r.get::<_, String>(2)?,
            "referensi":r.get::<_, Option<String>>(3)?, "nominal":r.get::<_, i64>(4)?,
            "balik_id":r.get::<_, Option<i64>>(5)?, "alasan":r.get::<_, Option<String>>(6)?,
            "operator":r.get::<_, String>(7)?, "created_at":r.get::<_, String>(8)?, "dibalik":r.get::<_, i64>(9)? == 1,
        }))
    }).collect()
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn bruto_dibulatkan_per_baris_dan_presisi_qty_dibatasi() {
        let mut baris = BarisReq {
            product_id: 1,
            satuan: "KG".into(),
            isi: "1".into(),
            qty: "1.125".into(),
            harga: 100,
            potongan: 13,
        };
        assert_eq!(qty_dan_bruto(&baris).unwrap().1, 113);
        baris.potongan = 114;
        assert!(qty_dan_bruto(&baris).is_err());
        baris.potongan = 0;
        baris.qty = "1.0001".into();
        assert!(qty_dan_bruto(&baris).is_err());
        assert!(total_rupiah([MAKS_RUPIAH, 1].into_iter()).is_err());
    }
}
