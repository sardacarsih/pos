//! `pos-api selftest`: menjalankan query READ-ONLY yang dipakai endpoint admin baru terhadap database yang dikonfigurasi
//! (.env di direktori kerja), lalu melaporkan OK/GAGAL per pemeriksaan. Dipakai `tools/deploy-api.ps1` SEBELUM biner baru
//! menggantikan yang berjalan: SQL baru (tren, stok habis, harga <= HPP, rincian penjualan, opname) terbukti valid di
//! Oracle produksi tanpa membuka endpoint dan tanpa menulis apa pun (tidak ada INSERT/UPDATE/COMMIT di sini).
use std::{collections::HashSet, time::Instant};

use chrono::{Datelike, Duration, NaiveDate};
use oracle::Connection;

use crate::{
    auth::Role,
    db::{dashboard, faktur, katalog, opname, opt, pembelian, persediaan, supplier, user},
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
        ("POS_PRODUCT", &["PRODUCTID", "KODE_ITEM", "BARCODE", "PRODUCTNAME", "SATUAN", "PRICE", "BELI", "KATEGORI_ID", "AKTIF", "STOK_MIN"]),
        ("POS_KATEGORI", &["KODE", "KATEGORI"]),
        ("POS_STOCK", &["PRODUCT_ID", "KODE_BARANG", "TANGGAL", "QUANTITY", "HPP"]),
        ("POS_SUPPLIER", &["KODE", "NAMA", "AKTIF"]),
        ("POS_PEMBELIAN", &["PURCHASE_ID", "NO_TRANSAKSI", "TANGGAL", "SUPPLIER_ID", "BRUTO", "POTONGAN", "TOTAL", "TERMIN", "USERID", "NAMA_USER"]),
        ("POS_PEMBELIANDETAIL", &["PURCHASE_ID", "NO_TRANSAKSI", "BARIS", "PRODUCT_ID", "KODE_BARANG", "NAMA_BARANG", "SATUAN", "QUANTITY", "HARGA_BELI", "BRUTO", "POTONGAN", "TOTAL"]),
        ("MOB_BELI", &["NO_TRANSAKSI", "PURCHASE_ID", "NO_FAKTUR", "BATAL_AT"]),
        ("MOB_BELI_BARIS", &["NO_TRANSAKSI", "BARIS", "ISI", "QTY"]),
        ("MOB_BELI_BAYAR", &["ID", "NO_TRANSAKSI", "NOMINAL", "BALIK_ID"]),
        ("POS_PENJUALAN", &["NO_TRANSAKSI", "TANGGAL", "TOTAL", "JENIS_BAYAR", "KASIR", "NAMA_KASIR"]),
        ("POS_PENJUALAN_DETAIL", &["NO_TRANSAKSI", "KODE_BARANG", "NAMA_BARANG", "JUMLAH_BARANG", "ISI", "TOTAL_HARGA"]),
        ("POS_PRODUCT_SATUAN", &["PRODUCT_ID", "SATUAN", "ISI", "HARGA", "BARCODE"]),
        ("POS_BARANGRUSAK", &["NOMOR_SO", "KODE_BARANG", "TANGGAL", "JUMLAHFISIK"]),
        ("POS_DOKUMEN_STOK_LOG", &["ID", "JENIS", "NOMOR", "AKSI", "USER_ID", "USER_NAMA", "ALASAN", "JUMLAH_BARIS", "TOTAL_NILAI", "CREATED_AT"]),
        ("POS_STOCKOPNAME", &["NOMOR_SO", "TANGGAL", "KODE_BARANG", "JUMLAHSISTEM", "JUMLAHFISIK", "SELISIH", "HPP", "TOTAL"]),
        ("POS_TRANSACTION_COUNTER", &["TRANSACTION_CODE", "TRANSACTION_YEAR", "LAST_NUMBER"]),
        ("NOMOR_TRANSAKSI", &["KODE", "NOMOR"]),
        ("POS_PENDING_DETAIL", &["KODE_BARANG", "ISI"]),
        ("POS_POTONGANBERDASARKANQTY", &["KODE_ITEM"]),
        ("MOB_PRODUK_FOTO", &["PRODUCT_ID"]),
        ("MOB_PRODUK_URUT", &["PRODUCT_ID"]),
        ("MOB_IDEMPOTENCY", &["USER_ID", "OPERASI", "IDEMPOTENCY_KEY", "RESOURCE_ID", "PAYLOAD_HASH"]),
        ("MOB_STOK_SESUAI", &["ID", "KODE_BARANG", "TANGGAL", "QTY", "ALASAN", "USER_ID", "USER_NAMA", "BALIK_ID", "CREATED_AT"]),
    ];
    for (tabel, kolom) in skema {
        h.push(cek(format!("skema {tabel}"), || cek_kolom(conn, tabel, kolom)));
    }
    h.push(cek("proteksi dan seri pembelian web", || {
        pembelian::cek_guard(conn)?;
        pembelian::legacy::cek_guard(conn)?;
        let lebar: i64 = conn.query_row_as(
            "SELECT NVL(MIN(CHAR_LENGTH), 0) FROM USER_TAB_COLUMNS
             WHERE TABLE_NAME IN ('POS_PEMBELIAN','POS_PEMBELIANDETAIL') AND COLUMN_NAME='NO_TRANSAKSI'", &[],
        )?;
        if lebar < "BW-26-000001".len() as i64 {
            return Err(AppError::Internal(format!("NO_TRANSAKSI pembelian memerlukan 12 karakter, tersedia {lebar}")));
        }
        Ok("Kedua trigger valid dan seri web muat".into())
    }));
    let admin = pembelian::Operator {
        id: 0,
        username: String::new(),
        nama: String::new(),
        role: Role::Admin,
    };
    h.push(cek("detail faktur pembelian backoffice", || {
        let nomor: Option<String> = opt(conn.query_row_as(
            "SELECT b.NO_TRANSAKSI FROM POS_PEMBELIAN b
             WHERE b.NO_TRANSAKSI IS NOT NULL AND b.TANGGAL IS NOT NULL
             AND NOT EXISTS (SELECT 1 FROM MOB_BELI w WHERE w.PURCHASE_ID=b.PURCHASE_ID)
             ORDER BY b.TANGGAL DESC, b.NO_TRANSAKSI DESC FETCH FIRST 1 ROW ONLY",
            &[],
        ))?;
        let Some(nomor) = nomor else {
            return match pembelian::detail(conn, &admin, "~SELFTEST~") {
                Err(AppError::NotFound(_)) => Ok("Belum ada faktur backoffice".into()),
                Err(e) => Err(e),
                Ok(_) => Err(AppError::Internal("Nomor uji faktur seharusnya tidak ditemukan".into())),
            };
        };
        let detail = pembelian::detail(conn, &admin, &nomor)?;
        let baris = detail["baris"].as_array().filter(|_| detail["asal"] == "BACKOFFICE")
            .ok_or_else(|| AppError::Internal("Detail faktur backoffice tidak lengkap".into()))?;
        Ok(format!("{nomor}, {} baris", baris.len()))
    }));
    let filter = pembelian::Filter {
        q: None,
        supplier: None,
        dari: None,
        sampai: None,
        page: None,
        terlambat: None,
        rekonsiliasi: None,
    };
    h.push(cek("ringkasan hutang termasuk faktur Backoffice", || {
        let hasil = pembelian::hutang(conn, &admin, &filter)?;
        let jumlah = hasil["supplier"].as_array().map_or(0, Vec::len);
        Ok(format!("{jumlah} supplier dengan saldo"))
    }));
    h.push(cek("riwayat petugas pembelian", || {
        let petugas = pembelian::Operator { role: Role::Pembelian, ..admin };
        pembelian::riwayat(conn, &petugas, &filter)?;
        Ok("Kueri dengan filter petugas berhasil".into())
    }));
    h.push(cek("daftar supplier dan filter", || {
        let semua = supplier::daftar(conn, &supplier::Filter::default())?;
        let aktif = supplier::daftar(conn, &supplier::Filter {
            q: semua.items.first().map(|s| s.kode.clone()),
            aktif: Some(true),
            page: Some(1),
        })?;
        Ok(format!("{} baris, {} cocok dengan filter", semua.items.len(), aktif.items.len()))
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
    h.push(cek("daftar stok habis (cari + paginasi + urut qty)", || {
        use katalog::UrutanStokHabis::{Nama, Qty};

        let (a, jumlah) = katalog::stok_habis(conn, None, None, awal, hari, 1, 5, false, Nama)?;
        let (b, _) = katalog::stok_habis(conn, Some("a"), Some("1"), awal, hari, 1, 5, false, Nama)?;
        let (urut, _) = katalog::stok_habis(conn, None, None, awal, hari, 1, 5, false, Qty)?;
        if urut.items.windows(2).any(|pasang| pasang[0].stok > pasang[1].stok) {
            return Err(AppError::Internal("Urutan qty stok habis tidak menaik".into()));
        }
        Ok(format!("{} baris, total {:?}, dengan filter {} baris", a.items.len(), jumlah, b.items.len()))
    }));
    h.push(cek("harga jual <= HPP (hitung + daftar)", || {
        let n = katalog::jumlah_harga_rendah(conn)?;
        let d = katalog::daftar_produk_admin(conn, None, None, None, true, 1, 5, false)?;
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
    h.push(cek("daftar produk admin", || Ok(format!("{} baris", katalog::daftar_produk_admin(conn, None, None, None, false, 1, 5, false)?.items.len()))));
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
    h.push(cek("daftar penyesuaian stok (audit)", || {
        let h = persediaan::daftar_sesuai(conn, None, 1, 5)?;
        Ok(format!("{} baris tampil", h.items.len()))
    }));
    h.push(cek("kartu stok rekonsiliasi dengan stok sistem", || {
        let p: Option<(i64, String)> = conn
            .query_row_as::<(i64, String)>("SELECT PRODUCTID, KODE_ITEM FROM POS_PRODUCT WHERE AKTIF = 'Y' AND ROWNUM = 1", &[])
            .ok();
        match p {
            Some((id, kode)) => {
                let k = persediaan::kartu_stok(conn, id, None, None, hari, 1, 5)?;
                let sistem = katalog::stok_map(conn, std::slice::from_ref(&kode), awal, hari)?
                    .get(&kode).copied().unwrap_or_default();
                if k.saldo_akhir != sistem {
                    return Err(AppError::Internal(format!("saldo akhir kartu {} != stok sistem {sistem}", k.saldo_akhir)));
                }
                Ok(format!("{kode}: awal {}, akhir {}, {} mutasi tampil", k.saldo_awal, k.saldo_akhir, k.items.len()))
            }
            None => Ok("tidak ada produk aktif".into()),
        }
    }));
    h.push(cek("valuasi persediaan per kategori", || {
        let v = persediaan::valuasi(conn, None, awal, hari)?;
        Ok(format!("{} produk, nilai {}, tanpa HPP {}, {} kategori", v.total_produk, v.total_nilai, v.tanpa_hpp, v.per_kategori.len()))
    }));
    h.push(cek("valuasi persediaan per item", || {
        let h = persediaan::valuasi_item(conn, None, awal, hari, 1, 50)?;
        Ok(format!("{} item pada halaman pertama, ada_lagi={}", h.items.len(), h.ada_lagi))
    }));
    h.push(cek("stok minimum (daftar + jumlah)", || {
        let (h, jumlah) = persediaan::stok_minimum(conn, None, None, awal, hari, 1, 5)?;
        let n = persediaan::jumlah_stok_minimum(conn, awal, hari)?;
        if jumlah != Some(n) {
            return Err(AppError::Internal(format!("jumlah daftar {jumlah:?} != hitungan {n}")));
        }
        Ok(format!("{n} produk di bawah ambang, {} baris tampil", h.items.len()))
    }));
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
