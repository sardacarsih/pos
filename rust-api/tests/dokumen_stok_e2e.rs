//! Uji ujung-ke-ujung ubah/hapus dokumen Opname dan Barang Rusak terhadap Oracle SUNGGUHAN (DB dev).
//! Dilewati bila `POS_E2E_ORACLE` tidak diset. Format: `user/password@//host:1521/service`.
//! Tidak memakai skema uji sekali pakai: test membuat dokumen baru untuk satu produk uji (`POS_E2E_PRODUK`,
//! default UJIMS00001, harus aktif ber-HPP > 0 dan punya stok sistem >= 0), lalu menghapusnya sendiri; stok
//! kembali ke nilai awal. Efek samping yang tersisa: nomor SO terpakai (counter) dan baris log/idempotensi.
//! Jalankan: `cargo test --test dokumen_stok_e2e -- --nocapture`.
use chrono::{Datelike, NaiveDate};
use oracle::Connection;
use pos_api::{
    db::{barang_rusak, faktur, katalog, opname},
    error::AppError,
};
use rust_decimal::Decimal;

const USER: i64 = 1;
const NAMA: &str = "e2e";

fn koneksi() -> Option<Connection> {
    let v = std::env::var("POS_E2E_ORACLE").ok()?;
    let (cred, connect) = v.split_once('@')?;
    let (u, p) = cred.split_once('/')?;
    Some(Connection::connect(u, p, connect).expect("koneksi POS_E2E_ORACLE"))
}

fn stok(c: &Connection, kode: &str, hari: NaiveDate) -> Decimal {
    let awal = NaiveDate::from_ymd_opt(hari.year(), 1, 1).unwrap();
    katalog::stok_map(c, &[kode.to_string()], awal, hari).unwrap().get(kode).copied().unwrap_or_default()
}

fn jumlah_log(c: &Connection) -> i64 {
    c.query_row_as("SELECT COUNT(*) FROM POS_DOKUMEN_STOK_LOG", &[]).unwrap()
}

/// Hapus dokumen uji dengan SQL langsung bila test gagal di tengah jalan (tidak meninggalkan stok bergeser).
struct Bersih<'a>(&'a Connection, std::cell::RefCell<Vec<String>>);

impl Drop for Bersih<'_> {
    fn drop(&mut self) {
        for n in self.1.borrow().iter() {
            let _ = self.0.execute_named("DELETE FROM POS_STOCKOPNAME WHERE NOMOR_SO = :n", &[("n", n)]);
            let _ = self.0.execute_named("DELETE FROM POS_BARANGRUSAK WHERE NOMOR_SO = :n", &[("n", n)]);
        }
        let _ = self.0.commit();
    }
}

fn d(s: &str) -> Decimal {
    s.parse().unwrap()
}

fn so_req(id: i64, fisik: Decimal) -> opname::BarisReq {
    opname::BarisReq { produk_id: id, qty_fisik: Some(fisik), hitungan: None }
}

fn kunci() -> String {
    uuid_sederhana()
}

fn uuid_sederhana() -> String {
    use std::sync::atomic::{AtomicU64, Ordering};
    static N: AtomicU64 = AtomicU64::new(0);
    let t = std::time::SystemTime::now().duration_since(std::time::UNIX_EPOCH).unwrap().as_nanos();
    // Kolom IDEMPOTENCY_KEY = VARCHAR2(36), seperti panjang UUID.
    format!("e2e{:033}", (t % 10u128.pow(30)) * 1000 + u128::from(N.fetch_add(1, Ordering::SeqCst)))
}

#[test]
fn ubah_hapus_opname_dan_rusak_dengan_pengaman() {
    let Some(c) = koneksi() else {
        eprintln!("dilewati: POS_E2E_ORACLE tidak diset");
        return;
    };
    let _ = tracing_subscriber::fmt().with_test_writer().try_init();
    let kode = std::env::var("POS_E2E_PRODUK").unwrap_or_else(|_| "UJIMS00001".into());
    let produk: i64 = c
        .query_row_as_named("SELECT PRODUCTID FROM POS_PRODUCT WHERE KODE_ITEM = :k AND AKTIF = 'Y'", &[("k", &kode)])
        .expect("produk uji aktif");
    let (hari, _) = faktur::sekarang(&c).unwrap();
    let awal = stok(&c, &kode, hari);
    let log0 = jumlah_log(&c);

    // 1) Buat SO: fisik = stok + 2 -> selisih +2.
    let k1 = kunci();
    let so = opname::simpan_dokumen(&c, USER, &k1, &[so_req(produk, awal + d("2"))], hari).unwrap();
    let nomor = so.nomor_so.clone();
    let bersih = Bersih(&c, std::cell::RefCell::new(vec![nomor.clone()]));
    assert_eq!(stok(&c, &kode, hari), awal + d("2"));
    assert_eq!(so.items[0].produk_id, Some(produk));

    // 2) Ubah: alasan wajib, versi basi ditolak, ubah berhasil dan stok bergeser sebesar delta selisih.
    let ubah = |versi: &str, alasan: Option<&str>, fisik: Decimal, key: &str| {
        opname::ubah_dokumen(&c, USER, NAMA, key, &nomor, versi, alasan, &[so_req(produk, fisik)], hari)
    };
    assert!(matches!(ubah(&so.versi, None, awal + d("5"), &kunci()), Err(AppError::Validation(_))));
    let basi = ubah("versi-basi", Some("uji"), awal + d("5"), &kunci());
    assert!(matches!(basi, Err(AppError::Conflict(_))), "versi basi: {basi:?}");
    let k2 = kunci();
    let baru = ubah(&so.versi, Some("koreksi hitung"), awal + d("5"), &k2).unwrap();
    assert_eq!(stok(&c, &kode, hari), awal + d("5"));
    assert_eq!(baru.items[0].selisih, d("5")); // JUMLAHSISTEM lama dipertahankan
    assert_ne!(baru.versi, so.versi);
    // Retry dengan kunci dan payload sama = hasil yang sama, tanpa efek ganda.
    let ulang = ubah(&so.versi, Some("koreksi hitung"), awal + d("5"), &k2).unwrap();
    assert!(ulang.ulang);
    assert_eq!(stok(&c, &kode, hari), awal + d("5"));

    // 3) Barang rusak: stok tidak cukup ditolak; qty valid dibuat; dokumen lebih baru memblokir ubah/hapus SO.
    let rusak_req = |q: &str| barang_rusak::BarisReq { produk_id: produk, qty: q.into(), keterangan: Some("uji".into()) };
    assert!(matches!(
        barang_rusak::simpan(&c, USER, &kunci(), &[rusak_req("999999999")], hari),
        Err(AppError::Rule { kode: "STOK_KURANG", .. })
    ));
    let rusak = barang_rusak::simpan(&c, USER, &kunci(), &[rusak_req("1")], hari).unwrap();
    bersih.1.borrow_mut().push(rusak.nomor_so.clone());
    assert_eq!(stok(&c, &kode, hari), awal + d("4"));
    assert!(matches!(
        ubah(&baru.versi, Some("uji"), awal + d("6"), &kunci()),
        Err(AppError::Rule { kode: "BENTROK_DOKUMEN_BARU", .. })
    ));
    assert!(matches!(
        opname::hapus_dokumen(&c, USER, NAMA, &kunci(), &nomor, &baru.versi, Some("uji")),
        Err(AppError::Rule { kode: "BENTROK_DOKUMEN_BARU", .. })
    ));

    // 4) Ubah rusak (1 -> 2) lalu hapus; stok kembali.
    let rusak2 = barang_rusak::ubah(&c, USER, NAMA, &kunci(), &rusak.nomor_so, &rusak.versi, Some("salah catat"), &[rusak_req("2")], hari).unwrap();
    assert_eq!(stok(&c, &kode, hari), awal + d("3"));
    assert!(matches!(
        barang_rusak::hapus(&c, USER, NAMA, &kunci(), &rusak.nomor_so, "versi-basi", Some("uji")),
        Err(AppError::Conflict(_))
    ));
    barang_rusak::hapus(&c, USER, NAMA, &kunci(), &rusak.nomor_so, &rusak2.versi, Some("uji selesai")).unwrap();
    assert_eq!(stok(&c, &kode, hari), awal + d("5"));
    assert!(matches!(barang_rusak::detail(&c, &rusak.nomor_so), Err(AppError::NotFound(_))));

    // 5) Hapus SO (kini yang terbaru): stok kembali ke awal; hapus ulang dengan kunci sama = no-op.
    let kh = kunci();
    opname::hapus_dokumen(&c, USER, NAMA, &kh, &nomor, &baru.versi, Some("uji selesai")).unwrap();
    assert_eq!(stok(&c, &kode, hari), awal);
    opname::hapus_dokumen(&c, USER, NAMA, &kh, &nomor, &baru.versi, Some("uji selesai")).unwrap();
    assert!(matches!(opname::detail_so(&c, &nomor), Err(AppError::NotFound(_))));

    // 6) Log: ubah SO, ubah rusak, hapus rusak, hapus SO.
    assert_eq!(jumlah_log(&c) - log0, 4);
}
