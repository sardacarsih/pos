//! `pos-api selftest`: menjalankan query READ-ONLY yang dipakai endpoint admin baru terhadap database yang dikonfigurasi
//! (.env di direktori kerja), lalu melaporkan OK/GAGAL per pemeriksaan. Dipakai `tools/deploy-api.ps1` SEBELUM biner baru
//! menggantikan yang berjalan: SQL baru (tren, stok habis, harga <= HPP, rincian penjualan, opname) terbukti valid di
//! Oracle produksi tanpa membuka endpoint dan tanpa menulis apa pun (tidak ada INSERT/UPDATE/COMMIT di sini).
use std::{collections::HashSet, time::Instant};

use chrono::{Datelike, Duration, NaiveDate};
use oracle::Connection;

use crate::{
    db::{dashboard, faktur, katalog, opname, pembelian, user},
    error::{AppError, AppResult},
};

pub struct Hasil {
    pub nama: String,
    pub ok: bool,
    pub info: String,
    pub ms: u128,
}

impl Hasil {
    pub fn baris(&self) -> String {
        format!("{} {} ({} ms): {}", if self.ok { "[OK]   " } else { "[GAGAL]" }, self.nama, self.ms, self.info)
    }
}

/// (jumlah OK, jumlah GAGAL).
pub fn ringkas(h: &[Hasil]) -> (usize, usize) {
    let ok = h.iter().filter(|x| x.ok).count();
    (ok, h.len() - ok)
}

/// Nama kolom yang diharapkan tetapi tidak ada (perbandingan tidak peka huruf besar/kecil).
pub fn kolom_hilang(ada: &HashSet<String>, perlu: &[&str]) -> Vec<String> {
    perlu
        .iter()
        .filter(|k| !ada.contains(&k.to_uppercase()))
        .map(|k| k.to_string())
        .collect()
}

fn cek(nama: impl Into<String>, f: impl FnOnce() -> AppResult<String>) -> Hasil {
    let t = Instant::now();
    let (ok, info) = match f() {
        Ok(i) => (true, i),
        Err(e) => (false, e.to_string()),
    };
    Hasil { nama: nama.into(), ok, info, ms: t.elapsed().as_millis() }
}

/// Kolom yang dipakai kode kita pada tabel itu (dibaca dari metadata, tanpa menyentuh data).
fn cek_kolom(conn: &Connection, tabel: &str, perlu: &[&str]) -> AppResult<String> {
    let mut ada = HashSet::new();
    for r in conn.query_as::<String>("SELECT COLUMN_NAME FROM ALL_TAB_COLUMNS WHERE TABLE_NAME = :1", &[&tabel.to_uppercase()])? {
        ada.insert(r?);
    }
    if ada.is_empty() {
        return Err(AppError::Internal(format!("tabel {tabel} tidak ditemukan")));
    }
    let hilang = kolom_hilang(&ada, perlu);
    if hilang.is_empty() {
        Ok(format!("{} kolom ada", perlu.len()))
    } else {
        Err(AppError::Internal(format!("kolom hilang: {}", hilang.join(", "))))
    }
}

pub fn jalankan(conn: &Connection) -> AppResult<Vec<Hasil>> {
    let (hari, _) = faktur::sekarang(conn)?;
    let awal = NaiveDate::from_ymd_opt(hari.year(), 1, 1).expect("1 Jan");
    let mut h: Vec<Hasil> = Vec::new();

    // Skema yang dibutuhkan fitur baru, termasuk jalur TULIS opname (diverifikasi lewat metadata, tanpa menulis).
    let skema: &[(&str, &[&str])] = &[
        ("POS_PRODUCT", &["PRODUCTID", "KODE_ITEM", "BARCODE", "PRODUCTNAME", "SATUAN", "PRICE", "BELI", "KATEGORI_ID", "AKTIF"]),
        ("POS_KATEGORI", &["KODE", "KATEGORI"]),
        ("POS_STOCK", &["KODE_BARANG", "TANGGAL", "QUANTITY"]),
        ("POS_SUPPLIER", &["KODE", "NAMA", "AKTIF"]),
        ("POS_PEMBELIAN", &["PURCHASE_ID", "NO_TRANSAKSI", "TANGGAL", "SUPPLIER_ID", "BRUTO", "POTONGAN", "TOTAL", "TERMIN", "USERID", "NAMA_USER"]),
        ("POS_PEMBELIANDETAIL", &["PURCHASE_ID", "NO_TRANSAKSI", "BARIS", "PRODUCT_ID", "KODE_BARANG", "NAMA_BARANG", "SATUAN", "QUANTITY", "HARGA_BELI", "BRUTO", "POTONGAN", "TOTAL"]),
        ("MOB_BELI", &["NO_TRANSAKSI", "PURCHASE_ID", "NO_FAKTUR", "BATAL_AT"]),
        ("MOB_BELI_BARIS", &["NO_TRANSAKSI", "BARIS", "ISI", "QTY"]),
        ("MOB_BELI_BAYAR", &["ID", "NO_TRANSAKSI", "NOMINAL", "BALIK_ID"]),
        ("POS_PENJUALAN", &["NO_TRANSAKSI", "TANGGAL", "TOTAL", "JENIS_BAYAR", "KASIR", "NAMA_KASIR"]),
        ("POS_PENJUALAN_DETAIL", &["NO_TRANSAKSI", "KODE_BARANG", "NAMA_BARANG", "JUMLAH_BARANG", "ISI", "TOTAL_HARGA"]),
        ("POS_PRODUCT_SATUAN", &["PRODUCT_ID", "SATUAN", "ISI", "HARGA", "BARCODE"]),
        ("POS_BARANGRUSAK", &["KODE_BARANG", "TANGGAL", "JUMLAHFISIK"]),
        ("POS_STOCKOPNAME", &["NOMOR_SO", "TANGGAL", "KODE_BARANG", "JUMLAHSISTEM", "JUMLAHFISIK", "SELISIH", "HPP", "TOTAL"]),
        ("POS_TRANSACTION_COUNTER", &["TRANSACTION_CODE", "TRANSACTION_YEAR", "LAST_NUMBER"]),
        ("NOMOR_TRANSAKSI", &["KODE", "NOMOR"]),
        ("POS_PENDING_DETAIL", &["KODE_BARANG", "ISI"]),
        ("POS_POTONGANBERDASARKANQTY", &["KODE_ITEM"]),
        ("MOB_PRODUK_FOTO", &["PRODUCT_ID"]),
        ("MOB_PRODUK_URUT", &["PRODUCT_ID"]),
        ("MOB_IDEMPOTENCY", &["USER_ID", "OPERASI", "IDEMPOTENCY_KEY", "RESOURCE_ID", "PAYLOAD_HASH"]),
    ];
    for (tabel, kolom) in skema {
        h.push(cek(format!("skema {tabel}"), || cek_kolom(conn, tabel, kolom)));
    }
    h.push(cek("proteksi dan seri pembelian web", || {
        pembelian::cek_guard(conn)?;
        let lebar: i64 = conn.query_row_as(
            "SELECT NVL(MIN(CHAR_LENGTH), 0) FROM USER_TAB_COLUMNS
             WHERE TABLE_NAME IN ('POS_PEMBELIAN','POS_PEMBELIANDETAIL') AND COLUMN_NAME='NO_TRANSAKSI'", &[],
        )?;
        if lebar < "BW-26-000001".len() as i64 {
            return Err(AppError::Internal(format!("NO_TRANSAKSI pembelian memerlukan 12 karakter, tersedia {lebar}")));
        }
        Ok("Kedua trigger valid dan seri web muat".into())
    }));
    h.push(cek("skema MOB_IDEMPOTENCY.OPERASI muat 'OPNAME_DOK'", || {
        let n: i64 = conn.query_row_as(
            "SELECT NVL(MAX(CHAR_LENGTH), 0) FROM ALL_TAB_COLUMNS WHERE TABLE_NAME = 'MOB_IDEMPOTENCY' AND COLUMN_NAME = 'OPERASI'",
            &[],
        )?;
        if n >= "OPNAME_DOK".len() as i64 { Ok(format!("panjang kolom {n}")) } else { Err(AppError::Internal(format!("panjang kolom {n} < 10"))) }
    }));

    // Query baca yang dipakai endpoint.
    h.push(cek("periode hari ini", || {
        let i = faktur::info_periode(conn, hari)?;
        Ok(format!("{:?}", i.map(|x| x.periode)))
    }));
    h.push(cek("dashboard admin (kemarin, tren, perhatian)", || {
        let d = dashboard::dashboard_admin(conn)?;
        Ok(format!("hari ini {} transaksi, tren {} hari, harga<=HPP {}, tanpa HPP {}", d.ringkasan.jumlah, d.tren.len(), d.perhatian.harga_rendah, d.perhatian.tanpa_hpp))
    }));
    h.push(cek("tren penjualan 7 hari", || Ok(format!("{} hari", dashboard::tren_penjualan(conn, hari, 7)?.len()))));
    for (nama, hari_lalu) in [("hari ini", 0), ("7 hari", 6), ("31 hari", 30)] {
        h.push(cek(format!("rincian penjualan {nama}"), || {
            let r = dashboard::rincian_penjualan(conn, hari - Duration::days(hari_lalu), hari)?;
            let jumlah_kasir: i64 = r.per_kasir.iter().map(|k| k.pecahan.total.total).sum();
            if jumlah_kasir != r.ringkasan.total.total {
                return Err(AppError::Internal(format!("total kasir {jumlah_kasir} != ringkasan {}", r.ringkasan.total.total)));
            }
            Ok(format!("{} transaksi, {} kasir, {} hari", r.ringkasan.total.jumlah, r.per_kasir.len(), r.harian.len()))
        }));
    }
    h.push(cek("jumlah stok habis", || Ok(katalog::jumlah_stok_habis(conn, awal, hari)?.to_string())));
    h.push(cek("daftar stok habis (cari + paginasi)", || {
        let (a, jumlah) = katalog::stok_habis(conn, None, None, awal, hari, 1, 5)?;
        let (b, _) = katalog::stok_habis(conn, Some("a"), Some("1"), awal, hari, 1, 5)?;
        Ok(format!("{} baris, total {:?}, dengan filter {} baris", a.items.len(), jumlah, b.items.len()))
    }));
    h.push(cek("harga jual <= HPP (hitung + daftar)", || {
        let n = katalog::jumlah_harga_rendah(conn)?;
        let d = katalog::daftar_produk_admin(conn, None, None, None, true, 1, 5)?;
        // Daftar tidak boleh memuat produk untung (BELI > 0 dan harga jual > BELI); yang tampil hanya rugi/impas atau tanpa HPP.
        if let Some(p) = d.items.iter().find(|p| p.beli > 0 && p.harga > p.beli) {
            return Err(AppError::Internal(format!("produk untung ikut tampil: {} (jual {} > beli {})", p.kode, p.harga, p.beli)));
        }
        // Rugi harus lebih dulu daripada tanpa HPP.
        let urutan_benar = d.items.windows(2).all(|w| !(w[0].beli <= 0 && w[1].beli > 0));
        if !urutan_benar {
            return Err(AppError::Internal("urutan salah: produk tanpa HPP muncul sebelum yang rugi".into()));
        }
        Ok(format!("rugi {}, tanpa HPP {}, total daftar {}, {} baris tampil", n.rugi, n.tanpa_hpp, n.total(), d.items.len()))
    }));
    h.push(cek("riwayat pemakaian produk (jejak, baca saja)", || {
        let id: Option<i64> = conn.query_row_as::<i64>("SELECT MIN(PRODUCTID) FROM POS_PRODUCT", &[]).ok();
        match id {
            Some(id) => {
                let (p, j) = katalog::jejak_produk(conn, id)?;
                Ok(format!("{} -> total {}, bisa hapus {}", p.kode, j.total(), j.bisa_hapus()))
            }
            None => Ok("tidak ada produk".into()),
        }
    }));
    h.push(cek("daftar produk admin", || Ok(format!("{} baris", katalog::daftar_produk_admin(conn, None, None, None, false, 1, 5)?.items.len()))));
    h.push(cek("stok sistem (stok_map) satu produk", || {
        let kode: Option<String> = conn
            .query_row_as::<(String,)>("SELECT KODE_ITEM FROM POS_PRODUCT WHERE AKTIF = 'Y' AND ROWNUM = 1", &[])
            .ok()
            .map(|r| r.0);
        match kode {
            Some(k) => {
                let m = katalog::stok_map(conn, std::slice::from_ref(&k), awal, hari)?;
                Ok(format!("{k} -> {:?}", m.get(&k)))
            }
            None => Ok("tidak ada produk aktif".into()),
        }
    }));
    h.push(cek("riwayat SO tahun ini", || {
        let (daftar, _) = opname::daftar_so(conn, hari.year(), 1, 5)?;
        match daftar.first() {
            Some(s) => Ok(format!("{} dokumen; detail {} -> {} baris", daftar.len(), s.nomor_so, opname::detail_so(conn, &s.nomor_so)?.items.len())),
            None => Ok("belum ada dokumen SO tahun ini".into()),
        }
    }));
    h.push(cek("pengaturan KONTROL_SALDO_QTY", || Ok(format!("{:?}", katalog::setting(conn, "KONTROL_SALDO_QTY")?))));
    h.push(cek("daftar user", || Ok(format!("{} akun", user::list_users(conn)?.len()))));
    Ok(h)
}

#[cfg(test)]
mod tests {
    use super::*;

    fn hasil(ok: bool) -> Hasil {
        Hasil { nama: "x".into(), ok, info: "i".into(), ms: 3 }
    }

    #[test]
    fn ringkas_menghitung_ok_dan_gagal() {
        assert_eq!(ringkas(&[hasil(true), hasil(false), hasil(true)]), (2, 1));
        assert_eq!(ringkas(&[]), (0, 0));
    }

    #[test]
    fn kolom_hilang_tidak_peka_huruf() {
        let ada: HashSet<String> = ["KASIR", "TOTAL"].iter().map(|s| s.to_string()).collect();
        assert!(kolom_hilang(&ada, &["kasir", "TOTAL"]).is_empty());
        assert_eq!(kolom_hilang(&ada, &["KASIR", "NAMA_KASIR"]), vec!["NAMA_KASIR".to_string()]);
    }

    #[test]
    fn baris_memuat_status_nama_dan_info() {
        assert!(hasil(true).baris().starts_with("[OK]"));
        let g = hasil(false).baris();
        assert!(g.starts_with("[GAGAL]") && g.contains(" x ") && g.ends_with(": i"));
    }

    #[test]
    fn cek_menangkap_galat_tanpa_panik() {
        let g = cek("uji", || Err(AppError::Internal("ORA-00904".into())));
        assert!(!g.ok && g.info.contains("ORA-00904"));
        assert!(cek("uji", || Ok("fine".into())).ok);
    }
}
