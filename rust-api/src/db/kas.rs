//! Kas dan bank (web admin). Tabel MOB_KAS_*; mutasi modul lain masuk lewat `tarik` (tanpa trigger):
//! selisih antara nilai di tabel sumber dan yang sudah tercatat di MOB_KAS_MUTASI disisipkan sebagai mutasi baru.
use chrono::NaiveDate;
use oracle::Connection;
use serde::{Deserialize, Serialize};
use serde_json::{Value, json};

use super::{faktur, opt, pembelian::{Operator, transaksi, tulis}};
use crate::error::{AppError, AppResult};

const MAKS_RUPIAH: i64 = 9_000_000_000_000_000;
const MAKS_HARI_TARIK: i64 = 366;

pub fn migrate(c: &Connection) -> AppResult<usize> {
    let mut n = 0;
    for ddl in include_str!("kas.sql").replace("\r\n", "\n").split("\n/\n") {
        match c.execute(ddl.trim(), &[]) {
            Ok(_) => n += 1,
            Err(e) if matches!(e.oci_code(), Some(955 | 1408)) => {}
            Err(e) => return Err(e.into()),
        }
    }
    Ok(n)
}

fn teks(v: &str, panjang: usize, label: &str) -> AppResult<String> {
    let v = v.trim();
    if v.is_empty() || v.chars().count() > panjang || v.chars().any(char::is_control) {
        return Err(AppError::validation(format!("{label} wajib diisi, maksimal {panjang} karakter")));
    }
    Ok(v.to_owned())
}

fn teks_opsional(v: Option<&str>, panjang: usize, label: &str) -> AppResult<Option<String>> {
    v.map(str::trim).filter(|v| !v.is_empty()).map(|v| teks(v, panjang, label)).transpose()
}

fn nominal(n: i64) -> AppResult<i64> {
    if n <= 0 || n > MAKS_RUPIAH {
        return Err(AppError::validation("Nominal harus lebih dari nol"));
    }
    Ok(n)
}

// ---------- Akun ----------

#[derive(Serialize, Deserialize)]
pub struct AkunReq {
    pub kode: String,
    pub nama: String,
    pub jenis: String,
    pub no_rekening: Option<String>,
    pub atas_nama: Option<String>,
    pub saldo_awal: i64,
    pub tgl_saldo_awal: NaiveDate,
    /// "KAS"/"BANK": akun tujuan mutasi hasil tarik transaksi.
    pub default_untuk: Option<String>,
    pub aktif: bool,
}

/// Saldo akun sampai tanggal (inklusif); None = semua mutasi.
const SALDO: &str = "a.SALDO_AWAL + NVL((SELECT SUM(CASE m.ARAH WHEN 'M' THEN m.NOMINAL ELSE -m.NOMINAL END)
     FROM MOB_KAS_MUTASI m WHERE m.AKUN_ID=a.ID AND m.TANGGAL >= a.TGL_SALDO_AWAL
       AND (:sampai IS NULL OR m.TANGGAL <= :sampai)),0)";

pub fn daftar_akun(c: &Connection) -> AppResult<Value> {
    let sql = format!(
        "SELECT a.ID,a.KODE,a.NAMA,a.JENIS,a.NO_REKENING,a.ATAS_NAMA,a.SALDO_AWAL,a.TGL_SALDO_AWAL,
                a.DEFAULT_UNTUK,a.AKTIF,{SALDO}
         FROM MOB_KAS_AKUN a ORDER BY a.JENIS DESC,a.KODE"
    );
    let mut out = Vec::new();
    for r in c.query_named(&sql, &[("sampai", &None::<NaiveDate>)])? {
        let r = r?;
        out.push(json!({
            "id": r.get::<_, i64>(0)?, "kode": r.get::<_, String>(1)?, "nama": r.get::<_, String>(2)?,
            "jenis": r.get::<_, String>(3)?, "no_rekening": r.get::<_, Option<String>>(4)?,
            "atas_nama": r.get::<_, Option<String>>(5)?, "saldo_awal": r.get::<_, i64>(6)?,
            "tgl_saldo_awal": r.get::<_, chrono::NaiveDateTime>(7)?.date(),
            "default_untuk": r.get::<_, Option<String>>(8)?, "aktif": r.get::<_, String>(9)? == "Y",
            "saldo": r.get::<_, i64>(10)?,
        }));
    }
    Ok(json!({ "akun": out }))
}

pub fn simpan_akun(c: &Connection, id: Option<i64>, p: &AkunReq) -> AppResult<Value> {
    let kode = teks(&p.kode, 20, "Kode akun")?.to_uppercase();
    let nama = teks(&p.nama, 100, "Nama akun")?;
    if !matches!(p.jenis.as_str(), "KAS" | "BANK") {
        return Err(AppError::validation("Jenis akun harus KAS atau BANK"));
    }
    let rek = teks_opsional(p.no_rekening.as_deref(), 50, "Nomor rekening")?;
    let an = teks_opsional(p.atas_nama.as_deref(), 100, "Atas nama")?;
    let def = p.default_untuk.as_deref().filter(|d| !d.is_empty());
    if def.is_some_and(|d| d != p.jenis) {
        return Err(AppError::validation("Akun default harus sejenis (KAS untuk tunai, BANK untuk transfer)"));
    }
    if def.is_some() && !p.aktif {
        return Err(AppError::validation("Akun default harus aktif"));
    }
    if !(-MAKS_RUPIAH..=MAKS_RUPIAH).contains(&p.saldo_awal) {
        return Err(AppError::validation("Saldo awal melampaui batas"));
    }
    let aktif = if p.aktif { "Y" } else { "T" };
    transaksi(c, || {
        if let Some(d) = def {
            // Pindahkan status default dari akun lain (kolom unik).
            c.execute_named(
                "UPDATE MOB_KAS_AKUN SET DEFAULT_UNTUK=NULL WHERE DEFAULT_UNTUK=:d AND (:id IS NULL OR ID<>:id)",
                &[("d", &d), ("id", &id)],
            )?;
        }
        let r = match id {
            Some(id) => c
                .execute_named(
                    "UPDATE MOB_KAS_AKUN SET KODE=:k,NAMA=:n,JENIS=:j,NO_REKENING=:r,ATAS_NAMA=:a,SALDO_AWAL=:s,
                     TGL_SALDO_AWAL=:t,DEFAULT_UNTUK=:d,AKTIF=:ak WHERE ID=:id",
                    &[("k", &kode), ("n", &nama), ("j", &p.jenis), ("r", &rek), ("a", &an), ("s", &p.saldo_awal),
                      ("t", &p.tgl_saldo_awal), ("d", &def), ("ak", &aktif), ("id", &id)],
                )
                .and_then(|s| s.row_count())
                .map(|n| (n > 0).then_some(id)),
            None => c
                .execute_named(
                    "INSERT INTO MOB_KAS_AKUN (KODE,NAMA,JENIS,NO_REKENING,ATAS_NAMA,SALDO_AWAL,TGL_SALDO_AWAL,DEFAULT_UNTUK,AKTIF)
                     VALUES (:k,:n,:j,:r,:a,:s,:t,:d,:ak) RETURNING ID INTO :id",
                    &[("k", &kode), ("n", &nama), ("j", &p.jenis), ("r", &rek), ("a", &an), ("s", &p.saldo_awal),
                      ("t", &p.tgl_saldo_awal), ("d", &def), ("ak", &aktif), ("id", &None::<i64>)],
                )
                .and_then(|s| s.returned_values::<_, i64>("id"))
                .map(|v| v.into_iter().next()),
        };
        match r {
            Ok(Some(id)) => Ok(json!({ "id": id })),
            Ok(None) => Err(AppError::NotFound("Akun tidak ditemukan".into())),
            Err(e) if e.oci_code() == Some(1) => Err(AppError::Conflict("Kode akun sudah dipakai".into())),
            Err(e) if e.oci_code() == Some(2290) => Err(AppError::validation("Data akun tidak valid")),
            Err(e) => Err(e.into()),
        }
    })
}

fn akun_aktif(c: &Connection, id: i64) -> AppResult<NaiveDate> {
    let r = opt(c.query_row_named(
        "SELECT TGL_SALDO_AWAL FROM MOB_KAS_AKUN WHERE ID=:id AND AKTIF='Y' FOR UPDATE",
        &[("id", &id)],
    ))?
    .ok_or_else(|| AppError::NotFound("Akun kas/bank aktif tidak ditemukan".into()))?;
    Ok(r.get::<_, chrono::NaiveDateTime>(0)?.date())
}

// ---------- Buku ----------

#[derive(Deserialize)]
pub struct FilterBuku {
    pub akun: i64,
    pub dari: NaiveDate,
    pub sampai: NaiveDate,
}

pub fn buku(c: &Connection, f: &FilterBuku) -> AppResult<Value> {
    if f.dari > f.sampai {
        return Err(AppError::validation("Rentang tanggal tidak valid"));
    }
    let kemarin = f.dari.pred_opt().unwrap_or(f.dari);
    let sql = format!("SELECT a.TGL_SALDO_AWAL, CASE WHEN :dari <= a.TGL_SALDO_AWAL THEN a.SALDO_AWAL ELSE {SALDO} END
                       FROM MOB_KAS_AKUN a WHERE a.ID=:id");
    let r = opt(c.query_row_named(&sql, &[("dari", &f.dari), ("sampai", &Some(kemarin)), ("id", &f.akun)]))?
        .ok_or_else(|| AppError::NotFound("Akun tidak ditemukan".into()))?;
    let mulai = r.get::<_, chrono::NaiveDateTime>(0)?.date();
    let saldo_awal: i64 = r.get(1)?;
    let mut saldo = saldo_awal;
    let mut baris = Vec::new();
    for r in c.query_named(
        "SELECT m.ID,m.TANGGAL,m.ARAH,m.NOMINAL,m.KETERANGAN,m.SUMBER,m.REF,m.USER_NAMA,m.BALIK_ID,
                (SELECT COUNT(*) FROM MOB_KAS_MUTASI b WHERE b.BALIK_ID=m.ID)
         FROM MOB_KAS_MUTASI m WHERE m.AKUN_ID=:id AND m.TANGGAL BETWEEN :dari AND :sampai AND m.TANGGAL >= :mulai
         ORDER BY m.TANGGAL,m.ID",
        &[("id", &f.akun), ("dari", &f.dari), ("sampai", &f.sampai), ("mulai", &mulai)],
    )? {
        let r = r?;
        let arah: String = r.get(2)?;
        let n: i64 = r.get(3)?;
        saldo += if arah == "M" { n } else { -n };
        baris.push(json!({
            "id": r.get::<_, i64>(0)?, "tanggal": r.get::<_, chrono::NaiveDateTime>(1)?.date(),
            "masuk": if arah == "M" { n } else { 0 }, "keluar": if arah == "K" { n } else { 0 },
            "saldo": saldo, "keterangan": r.get::<_, String>(4)?, "sumber": r.get::<_, String>(5)?,
            "ref": r.get::<_, Option<String>>(6)?, "user": r.get::<_, Option<String>>(7)?,
            "pembalik": r.get::<_, Option<i64>>(8)?.is_some(), "dibalik": r.get::<_, i64>(9)? > 0,
        }));
    }
    Ok(json!({ "saldo_awal": saldo_awal, "saldo_akhir": saldo, "mutasi": baris }))
}

// ---------- Mutasi manual, transfer, pembalikan ----------

#[derive(Serialize, Deserialize)]
pub struct ManualReq {
    pub akun_id: i64,
    pub tanggal: NaiveDate,
    /// "M" masuk, "K" keluar.
    pub arah: String,
    pub nominal: i64,
    pub keterangan: String,
}

#[derive(Serialize, Deserialize)]
pub struct TransferReq {
    pub dari_akun: i64,
    pub ke_akun: i64,
    pub tanggal: NaiveDate,
    pub nominal: i64,
    pub keterangan: String,
}

#[derive(Serialize, Deserialize)]
pub struct Alasan {
    pub alasan: String,
}

#[allow(clippy::too_many_arguments)]
fn sisip(
    c: &Connection, u: &Operator, akun: i64, tgl: NaiveDate, arah: &str, n: i64, ket: &str,
    sumber: &str, refr: Option<&str>, balik: Option<i64>,
) -> AppResult<i64> {
    let s = c.execute_named(
        "INSERT INTO MOB_KAS_MUTASI (AKUN_ID,TANGGAL,ARAH,NOMINAL,KETERANGAN,SUMBER,REF,BALIK_ID,USER_ID,USER_NAMA)
         VALUES (:a,:t,:ar,:n,:k,:s,:r,:b,:u,:un) RETURNING ID INTO :id",
        &[("a", &akun), ("t", &tgl), ("ar", &arah), ("n", &n), ("k", &ket), ("s", &sumber), ("r", &refr),
          ("b", &balik), ("u", &u.id), ("un", &u.nama), ("id", &None::<i64>)],
    )?;
    s.returned_values::<_, i64>("id")?
        .into_iter()
        .next()
        .ok_or_else(|| AppError::Internal("Oracle tidak mengembalikan ID mutasi".into()))
}

fn cek_tanggal(c: &Connection, tgl: NaiveDate, mulai: NaiveDate) -> AppResult<()> {
    if tgl < mulai {
        return Err(AppError::validation("Tanggal sebelum tanggal saldo awal akun"));
    }
    if tgl > faktur::sekarang(c)?.0 {
        return Err(AppError::validation("Tanggal tidak boleh melewati hari ini"));
    }
    faktur::cek_periode(c, tgl)
}

pub fn manual(c: &Connection, u: &Operator, key: &str, p: &ManualReq) -> AppResult<String> {
    tulis(c, u, key, "KAS_MANUAL", p, || {
        if !matches!(p.arah.as_str(), "M" | "K") {
            return Err(AppError::validation("Arah harus M (masuk) atau K (keluar)"));
        }
        let n = nominal(p.nominal)?;
        let ket = teks(&p.keterangan, 250, "Keterangan")?;
        let mulai = akun_aktif(c, p.akun_id)?;
        cek_tanggal(c, p.tanggal, mulai)?;
        Ok(sisip(c, u, p.akun_id, p.tanggal, &p.arah, n, &ket, "MANUAL", None, None)?.to_string())
    })
}

pub fn transfer(c: &Connection, u: &Operator, key: &str, p: &TransferReq) -> AppResult<String> {
    tulis(c, u, key, "KAS_TRANSFER", p, || {
        if p.dari_akun == p.ke_akun {
            return Err(AppError::validation("Akun asal dan tujuan harus berbeda"));
        }
        let n = nominal(p.nominal)?;
        let ket = teks(&p.keterangan, 250, "Keterangan")?;
        // Kunci berurutan ID agar dua transfer berlawanan tidak deadlock.
        let (a, b) = (p.dari_akun.min(p.ke_akun), p.dari_akun.max(p.ke_akun));
        let mulai = akun_aktif(c, a)?.max(akun_aktif(c, b)?);
        cek_tanggal(c, p.tanggal, mulai)?;
        let keluar = sisip(c, u, p.dari_akun, p.tanggal, "K", n, &ket, "TRANSFER", None, None)?;
        let refr = format!("TRF-{keluar}");
        c.execute_named("UPDATE MOB_KAS_MUTASI SET REF=:r WHERE ID=:id", &[("r", &refr), ("id", &keluar)])?;
        sisip(c, u, p.ke_akun, p.tanggal, "M", n, &ket, "TRANSFER", Some(&refr), None)?;
        Ok(refr)
    })
}

/// Hanya mutasi MANUAL/TRANSFER yang boleh dibalik; mutasi hasil tarik dikoreksi di modul asalnya lalu ditarik ulang.
pub fn batal(c: &Connection, u: &Operator, id: i64, key: &str, req: &Alasan) -> AppResult<String> {
    tulis(c, u, key, "KAS_BATAL", &(id, req), || {
        let alasan = teks(&req.alasan, 200, "Alasan pembatalan")?;
        let r = opt(c.query_row_named(
            "SELECT SUMBER,REF FROM MOB_KAS_MUTASI WHERE ID=:id AND BALIK_ID IS NULL FOR UPDATE",
            &[("id", &id)],
        ))?
        .ok_or_else(|| AppError::NotFound("Mutasi tidak ditemukan".into()))?;
        let sumber: String = r.get(0)?;
        let refr: Option<String> = r.get(1)?;
        let target: Vec<i64> = match sumber.as_str() {
            "MANUAL" => vec![id],
            "TRANSFER" => c
                .query_as_named::<i64>(
                    "SELECT ID FROM MOB_KAS_MUTASI WHERE SUMBER='TRANSFER' AND REF=:r AND BALIK_ID IS NULL ORDER BY ID FOR UPDATE",
                    &[("r", &refr)],
                )?
                .collect::<Result<_, _>>()?,
            _ => return Err(AppError::rule("MUTASI_OTOMATIS", "Koreksi transaksi ini di modul asalnya lalu tarik ulang")),
        };
        let hari = faktur::sekarang(c)?.0;
        faktur::cek_periode(c, hari)?;
        let mut hasil = String::new();
        for t in target {
            let r = c.query_row_named(
                "SELECT AKUN_ID,ARAH,NOMINAL,(SELECT COUNT(*) FROM MOB_KAS_MUTASI b WHERE b.BALIK_ID=m.ID)
                 FROM MOB_KAS_MUTASI m WHERE ID=:id",
                &[("id", &t)],
            )?;
            if r.get::<_, i64>(3)? > 0 {
                return Err(AppError::Conflict("Mutasi sudah dibatalkan".into()));
            }
            let arah = if r.get::<_, String>(1)? == "M" { "K" } else { "M" };
            let ket = format!("Batal #{t}: {alasan}");
            hasil = sisip(c, u, r.get(0)?, hari, arah, r.get(2)?, &ket, &sumber, refr.as_deref(), Some(t))?.to_string();
        }
        Ok(hasil)
    })
}

// ---------- Tarik transaksi dari modul lain ----------

#[derive(Deserialize)]
pub struct TarikReq {
    pub dari: NaiveDate,
    pub sampai: NaiveDate,
}

/// (sumber, SQL nilai seharusnya per REF: REF, TANGGAL, AKUN, NILAI bertanda (+masuk/-keluar), KETERANGAN).
/// :kas/:bank = akun default, :dari/:sampai = rentang.
const SUMBER: [(&str, &str); 3] = [
    (
        // Penjualan tunai digabung per hari agar buku kas tidak berisi ratusan baris per hari.
        "PENJUALAN",
        "SELECT 'JUAL-' || TO_CHAR(TRUNC(TANGGAL),'YYYYMMDD') REF, TRUNC(TANGGAL) TGL, :kas AKUN, SUM(TOTAL) NILAI,
                'Penjualan tunai ' || TO_CHAR(TRUNC(TANGGAL),'DD-MM-YYYY') KET
         FROM POS_PENJUALAN WHERE JENIS_BAYAR='TUNAI' AND TANGGAL >= :dari AND TANGGAL < :sampai + 1
         GROUP BY TRUNC(TANGGAL)",
    ),
    (
        "BELI_BAYAR",
        "SELECT 'BB-' || ID REF, TANGGAL TGL, CASE WHEN METODE='TUNAI' THEN :kas ELSE :bank END AKUN, -NOMINAL NILAI,
                SUBSTR(CASE WHEN NOMINAL > 0 THEN 'Bayar supplier ' ELSE 'Batal bayar supplier ' END || NO_TRANSAKSI, 1, 250) KET
         FROM MOB_BELI_BAYAR WHERE TANGGAL BETWEEN :dari AND :sampai",
    ),
    (
        "PINJAMAN",
        "SELECT NO_TRANSAKSI REF, TRUNC(TANGGAL) TGL, :kas AKUN, -SUM(PINJAMAN) NILAI,
                SUBSTR('Pencairan pinjaman ' || NO_TRANSAKSI || ' ' || MAX(NAMA_PELANGGAN), 1, 250) KET
         FROM FIN_PINJAMAN WHERE TANGGAL >= :dari AND TANGGAL < :sampai + 1
         GROUP BY NO_TRANSAKSI, TRUNC(TANGGAL)",
    ),
];

/// Idempoten: hanya selisih yang disisipkan, jadi transaksi sumber yang diedit/dihapus menghasilkan baris koreksi.
pub fn tarik(c: &Connection, u: &Operator, p: &TarikReq) -> AppResult<Value> {
    if p.dari > p.sampai || (p.sampai - p.dari).num_days() >= MAKS_HARI_TARIK {
        return Err(AppError::validation("Rentang tanggal tidak valid (maksimal 1 tahun)"));
    }
    if p.sampai > faktur::sekarang(c)?.0 {
        return Err(AppError::validation("Tanggal akhir tidak boleh melewati hari ini"));
    }
    faktur::cek_periode(c, p.dari)?;
    faktur::cek_periode(c, p.sampai)?;
    transaksi(c, || {
        // Mengunci akun default sekaligus menyerialkan tarik paralel.
        let mut kas = None;
        let mut bank = None;
        for r in c.query("SELECT ID,DEFAULT_UNTUK FROM MOB_KAS_AKUN WHERE DEFAULT_UNTUK IS NOT NULL FOR UPDATE", &[])? {
            let r = r?;
            match r.get::<_, String>(1)?.as_str() {
                "KAS" => kas = Some(r.get::<_, i64>(0)?),
                _ => bank = Some(r.get::<_, i64>(0)?),
            }
        }
        let (Some(kas), Some(bank)) = (kas, bank) else {
            return Err(AppError::rule("AKUN_DEFAULT", "Tetapkan akun default KAS dan BANK terlebih dahulu"));
        };
        let mut hasil = serde_json::Map::new();
        for (sumber, seharusnya) in SUMBER {
            let sql = format!(
                "INSERT INTO MOB_KAS_MUTASI (AKUN_ID,TANGGAL,ARAH,NOMINAL,KETERANGAN,SUMBER,REF,USER_ID,USER_NAMA)
                 SELECT NVL(x.AKUN,e.AKUN), NVL(x.TGL,e.TGL), CASE WHEN d.SELISIH > 0 THEN 'M' ELSE 'K' END, ABS(d.SELISIH),
                        CASE WHEN x.REF IS NULL THEN e.KET ELSE SUBSTR('Koreksi ' || NVL(e.KET, d.REF), 1, 250) END,
                        :sumber, d.REF, :u, :un
                 FROM (SELECT NVL(e.REF,x.REF) REF, NVL(e.NILAI,0) - NVL(x.NILAI,0) SELISIH
                       FROM ({seharusnya}) e
                       FULL JOIN (SELECT REF, SUM(CASE ARAH WHEN 'M' THEN NOMINAL ELSE -NOMINAL END) NILAI
                                  FROM MOB_KAS_MUTASI WHERE SUMBER=:sumber AND TANGGAL BETWEEN :dari AND :sampai
                                  GROUP BY REF) x ON x.REF=e.REF) d
                 LEFT JOIN ({seharusnya}) e ON e.REF=d.REF
                 LEFT JOIN (SELECT REF, MIN(TANGGAL) TGL, MIN(AKUN_ID) KEEP (DENSE_RANK FIRST ORDER BY ID) AKUN
                            FROM MOB_KAS_MUTASI WHERE SUMBER=:sumber AND TANGGAL BETWEEN :dari AND :sampai
                            GROUP BY REF) x ON x.REF=d.REF
                 WHERE d.SELISIH <> 0"
            );
            let n = c
                .execute_named(
                    &sql,
                    &[("sumber", &sumber), ("kas", &kas), ("bank", &bank), ("dari", &p.dari),
                      ("sampai", &p.sampai), ("u", &u.id), ("un", &u.nama)],
                )?
                .row_count()?;
            hasil.insert(sumber.to_lowercase(), json!(n));
        }
        Ok(Value::Object(hasil))
    })
}
