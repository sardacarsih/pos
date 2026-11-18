//! Test integrasi Oracle. Dilewati bila POS_TEST_ORACLE tidak diset.
//! Format: `user/password@//host:1521/service`. Arahkan ke skema UJI sekali pakai:
//! test membuat tabel MOB_* (idempotent) dan user bernama unik, lalu menghapus user itu.
use oracle::Connection;
use pos_api::{
    auth::{Role, password},
    db::{self, user},
};

fn koneksi() -> Option<Connection> {
    let v = std::env::var("POS_TEST_ORACLE").ok()?;
    let (cred, connect) = v.split_once('@')?;
    let (u, p) = cred.split_once('/')?;
    Some(Connection::connect(u, p, connect).expect("koneksi POS_TEST_ORACLE"))
}

#[test]
fn migrasi_idempotent_dan_user_roundtrip() {
    let Some(c) = koneksi() else {
        eprintln!("dilewati: POS_TEST_ORACLE tidak diset");
        return;
    };
    db::migrate(&c).unwrap();
    db::migrate(&c).unwrap(); // kedua kali tidak boleh gagal
    let table_count: i64 = c
        .query_row_as(
            "SELECT COUNT(*) FROM USER_TABLES WHERE TABLE_NAME = 'MOB_IDEMPOTENCY'",
            &[],
        )
        .unwrap();
    assert_eq!(table_count, 1);

    let nama = format!("it.{}", &uuid::Uuid::new_v4().simple().to_string()[..10]);
    let h = password::hash("sementara-123").unwrap();
    let id = user::create_user(&c, &nama, "Uji Integrasi", &h, Role::Kasir, None).unwrap();
    let u = user::find_by_username(&c, &nama).unwrap().unwrap();
    assert_eq!(
        (u.id, u.role, u.active, u.locked),
        (id, Role::Kasir, true, false)
    );
    assert!(password::verify("sementara-123", &u.hash));

    let key = uuid::Uuid::new_v4().to_string();
    c.execute_named(
        "INSERT INTO MOB_IDEMPOTENCY (USER_ID, OPERASI, IDEMPOTENCY_KEY, RESOURCE_ID) VALUES (:u, 'NOMOR', :k, 'BBM-26-000001')",
        &[("u", &id), ("k", &key)],
    )
    .unwrap();
    c.commit().unwrap();
    let duplicate = c.execute_named(
        "INSERT INTO MOB_IDEMPOTENCY (USER_ID, OPERASI, IDEMPOTENCY_KEY) VALUES (:u, 'NOMOR', :k)",
        &[("u", &id), ("k", &key)],
    );
    assert_eq!(duplicate.unwrap_err().oci_code(), Some(1));
    c.rollback().unwrap();

    // 5 gagal berturut-turut -> terkunci sementara
    for _ in 0..5 {
        user::register_failure(&c, id).unwrap();
    }
    assert!(user::find_by_id(&c, id).unwrap().unwrap().locked);
    user::reset_failures(&c, id).unwrap();
    assert!(!user::find_by_id(&c, id).unwrap().unwrap().locked);

    // Siklus password sementara: akun baru wajib ganti; reset membuka kunci; ganti menurunkan flag.
    assert!(user::find_by_id(&c, id).unwrap().unwrap().must_change);
    for _ in 0..5 {
        user::register_failure(&c, id).unwrap();
    }
    let tmp = password::temp_password();
    user::set_password(&c, id, &password::hash(&tmp).unwrap(), true).unwrap();
    let u = user::find_by_id(&c, id).unwrap().unwrap();
    assert!(u.must_change && !u.locked);
    assert!(password::verify(&tmp, &u.hash) && !password::verify("sementara-123", &u.hash));
    user::set_password(&c, id, &password::hash("baru-12345").unwrap(), false).unwrap();
    let u = user::find_by_id(&c, id).unwrap().unwrap();
    assert!(!u.must_change);
    assert!(password::verify("baru-12345", &u.hash) && !password::verify(&tmp, &u.hash));

    c.execute("DELETE FROM MOB_IDEMPOTENCY WHERE USER_ID = :1", &[&id])
        .unwrap();
    c.execute("DELETE FROM MOB_USER WHERE USER_ID = :1", &[&id])
        .unwrap();
    c.commit().unwrap();
}

const TABEL_POS: [&str; 14] = [
    "POS_PRODUCT", "POS_KATEGORI", "POS_STOCK", "POS_PEMBELIAN", "POS_PEMBELIANDETAIL", "POS_PENJUALAN",
    "POS_PENJUALAN_DETAIL", "POS_BARANGRUSAK", "POS_STOCKOPNAME", "POS_TRANSACTION_COUNTER", "NOMOR_TRANSAKSI",
    "POS_PENDING_DETAIL", "POS_POTONGANBERDASARKANQTY", "POS_PRODUCT_SATUAN",
];

/// Membuat tabel POS_* minimal untuk uji stok/opname dan menghapusnya lagi (juga bila uji panik).
struct SkemaPos<'a>(&'a Connection);

impl<'a> SkemaPos<'a> {
    fn buat(c: &'a Connection) -> Option<Self> {
        let ada: i64 = c
            .query_row_as(
                &format!("SELECT COUNT(*) FROM USER_TABLES WHERE TABLE_NAME IN ({})", TABEL_POS.map(|t| format!("'{t}'")).join(",")),
                &[],
            )
            .unwrap();
        if ada > 0 {
            eprintln!("dilewati: tabel POS_* sudah ada (bukan skema uji sekali pakai)");
            return None;
        }
        for ddl in [
            "CREATE TABLE POS_PRODUCT (PRODUCTID NUMBER PRIMARY KEY, KODE_ITEM VARCHAR2(30), BARCODE VARCHAR2(30), PRODUCTNAME VARCHAR2(100), SATUAN VARCHAR2(10), PRICE NUMBER, BELI NUMBER, KATEGORI_ID NUMBER, AKTIF VARCHAR2(1))",
            "CREATE TABLE POS_KATEGORI (KODE NUMBER, KATEGORI VARCHAR2(50))",
            "CREATE TABLE POS_STOCK (KODE_BARANG VARCHAR2(30), TANGGAL DATE, QUANTITY NUMBER, HPP NUMBER)",
            "CREATE TABLE POS_PEMBELIAN (PURCHASE_ID NUMBER, TANGGAL DATE)",
            "CREATE TABLE POS_PEMBELIANDETAIL (PURCHASE_ID NUMBER, KODE_BARANG VARCHAR2(30), QUANTITY NUMBER, HARGA_BELI NUMBER)",
            "CREATE TABLE POS_PENJUALAN (NO_TRANSAKSI VARCHAR2(30), TANGGAL DATE, TOTAL NUMBER, JENIS_BAYAR VARCHAR2(10), KASIR VARCHAR2(30), NAMA_KASIR VARCHAR2(60))",
            "CREATE TABLE POS_PENJUALAN_DETAIL (NO_TRANSAKSI VARCHAR2(30), KODE_BARANG VARCHAR2(30), JUMLAH_BARANG NUMBER, ISI NUMBER DEFAULT 1 NOT NULL)",
            "CREATE TABLE POS_BARANGRUSAK (NOMOR_SO VARCHAR2(20), TANGGAL DATE, KODE_BARANG VARCHAR2(30), JUMLAHFISIK NUMBER)",
            "CREATE TABLE POS_STOCKOPNAME (NOMOR_SO VARCHAR2(20), TANGGAL DATE, KODE_BARANG VARCHAR2(30), JUMLAHSISTEM NUMBER, JUMLAHFISIK NUMBER, SELISIH NUMBER, HPP NUMBER, TOTAL NUMBER)",
            "CREATE TABLE POS_TRANSACTION_COUNTER (TRANSACTION_CODE VARCHAR2(30), TRANSACTION_YEAR NUMBER(4), LAST_NUMBER NUMBER, PRIMARY KEY (TRANSACTION_CODE, TRANSACTION_YEAR))",
            "CREATE TABLE NOMOR_TRANSAKSI (KODE VARCHAR2(30), NOMOR VARCHAR2(30))",
            "CREATE TABLE POS_PENDING_DETAIL (NO_TRANSAKSI VARCHAR2(30), KODE_BARANG VARCHAR2(30), JUMLAH_BARANG NUMBER, ISI NUMBER DEFAULT 1 NOT NULL)",
            "CREATE TABLE POS_POTONGANBERDASARKANQTY (KODE_ITEM VARCHAR2(30), QTY NUMBER)",
            "CREATE TABLE POS_PRODUCT_SATUAN (PRODUCT_ID NUMBER, SATUAN VARCHAR2(20), ISI NUMBER, HARGA NUMBER, BARCODE VARCHAR2(50))",
        ] {
            c.execute(ddl, &[]).unwrap();
        }
        Some(Self(c))
    }
}

impl Drop for SkemaPos<'_> {
    fn drop(&mut self) {
        for t in TABEL_POS {
            let _ = self.0.execute(&format!("DROP TABLE {t} PURGE"), &[]);
        }
    }
}

#[test]
fn stok_habis_dan_opname_idempoten() {
    use pos_api::{db::{faktur, katalog, opname}, error::AppError};
    use rust_decimal::Decimal;

    let Some(c) = koneksi() else {
        eprintln!("dilewati: POS_TEST_ORACLE tidak diset");
        return;
    };
    db::migrate(&c).unwrap();
    let Some(_skema) = SkemaPos::buat(&c) else { return };
    let (hari, _) = faktur::sekarang(&c).unwrap();
    let awal = chrono::NaiveDate::from_ymd_opt(chrono::Datelike::year(&hari), 1, 1).unwrap();
    for (id, kode, beli) in [(1, "IT-A", 1000), (2, "IT-B", 0)] {
        c.execute_named(
            "INSERT INTO POS_PRODUCT VALUES (:i, :k, :k, :k, 'PCS', 2000, :b, 1, 'Y')",
            &[("i", &id), ("k", &kode), ("b", &beli)],
        )
        .unwrap();
    }
    // IT-A: stok awal 5 lalu terjual 5 -> habis. IT-B juga habis (tanpa transaksi) dengan harga beli 0.
    c.execute_named(
        "INSERT INTO POS_STOCK VALUES ('IT-A', TO_DATE(:d,'YYYY-MM-DD'), 5, 1000)",
        &[("d", &awal.format("%Y-%m-%d").to_string())],
    )
    .unwrap();
    c.execute("INSERT INTO POS_PENJUALAN (NO_TRANSAKSI, TANGGAL) VALUES ('J1', TRUNC(SYSDATE))", &[]).unwrap();
    c.execute("INSERT INTO POS_PENJUALAN_DETAIL (NO_TRANSAKSI, KODE_BARANG, JUMLAH_BARANG) VALUES ('J1', 'IT-A', 5)", &[]).unwrap();
    c.commit().unwrap();

    let (h, jumlah) = katalog::stok_habis(&c, None, None, awal, hari, 1, 10).unwrap();
    assert_eq!((h.items.len(), jumlah), (2, Some(2)));
    let (h, _) = katalog::stok_habis(&c, Some("it-a"), None, awal, hari, 1, 10).unwrap();
    assert_eq!(h.items.len(), 1);

    let nama = format!("it.{}", &uuid::Uuid::new_v4().simple().to_string()[..10]);
    let uid = user::create_user(&c, &nama, "Uji Opname", &password::hash("x-12345678").unwrap(), Role::Admin, None).unwrap();
    let key = uuid::Uuid::new_v4().to_string();
    let d = |s: &str| s.parse::<Decimal>().unwrap();

    let r = opname::simpan(&c, uid, &key, 1, d("10"), hari).unwrap();
    assert_eq!((r.selisih, r.ulang), (d("10"), false));
    let stok = katalog::stok_map(&c, &["IT-A".to_string()], awal, hari).unwrap();
    assert_eq!(stok["IT-A"], d("10"));
    // Retry dengan kunci sama: hasil sama, tidak ada dokumen SO baru.
    let ulang = opname::simpan(&c, uid, &key, 1, d("10"), hari).unwrap();
    assert_eq!((ulang.nomor_so.as_str(), ulang.ulang), (r.nomor_so.as_str(), true));
    let n: i64 = c.query_row_as("SELECT COUNT(*) FROM POS_STOCKOPNAME", &[]).unwrap();
    assert_eq!(n, 1);
    // Kunci sama, qty berbeda -> konflik.
    assert!(matches!(opname::simpan(&c, uid, &key, 1, d("11"), hari), Err(AppError::Conflict(_))));
    // Qty sama dengan stok sistem dan tambah stok tanpa harga beli ditolak.
    let k2 = uuid::Uuid::new_v4().to_string();
    assert!(matches!(opname::simpan(&c, uid, &k2, 1, d("10"), hari), Err(AppError::Validation(_))));
    let k3 = uuid::Uuid::new_v4().to_string();
    assert!(matches!(opname::simpan(&c, uid, &k3, 2, d("3"), hari), Err(AppError::Validation(_))));
    // Nomor SO berikutnya naik.
    let k4 = uuid::Uuid::new_v4().to_string();
    let r2 = opname::simpan(&c, uid, &k4, 1, d("4"), hari).unwrap();
    assert_ne!(r2.nomor_so, r.nomor_so);
    assert_eq!(r2.selisih, d("-6"));

    c.execute("DELETE FROM MOB_IDEMPOTENCY WHERE USER_ID = :1", &[&uid]).unwrap();
    c.execute("DELETE FROM MOB_USER WHERE USER_ID = :1", &[&uid]).unwrap();
    c.commit().unwrap();
}

#[test]
fn hapus_produk_tanpa_riwayat_dan_tolak_berriwayat() {
    use pos_api::{db::katalog, error::AppError};

    let Some(c) = koneksi() else {
        eprintln!("dilewati: POS_TEST_ORACLE tidak diset");
        return;
    };
    db::migrate(&c).unwrap();
    let Some(_skema) = SkemaPos::buat(&c) else { return };
    for (id, kode) in [(1, "D-BERSIH"), (2, "D-JUAL"), (3, "D-STOK"), (4, "D-OPNAME"), (5, "D-PENDING")] {
        c.execute_named(
            "INSERT INTO POS_PRODUCT VALUES (:i, :k, :k, :k, 'PCS', 1000, 500, 1, 'Y')",
            &[("i", &id), ("k", &kode)],
        )
        .unwrap();
    }
    c.execute("INSERT INTO POS_PENJUALAN_DETAIL (NO_TRANSAKSI, KODE_BARANG, JUMLAH_BARANG) VALUES ('T1', 'D-JUAL', 1)", &[]).unwrap();
    c.execute("INSERT INTO POS_STOCK VALUES ('D-STOK', SYSDATE, 0, 0)", &[]).unwrap();
    c.execute("INSERT INTO POS_STOCKOPNAME VALUES ('SO-1', SYSDATE, 'D-OPNAME', 1, 1, 0, 0, 0)", &[]).unwrap();
    c.execute("INSERT INTO POS_PENDING_DETAIL VALUES ('P1', 'D-PENDING', 1)", &[]).unwrap();
    c.execute("INSERT INTO POS_POTONGANBERDASARKANQTY VALUES ('D-BERSIH', 5)", &[]).unwrap();
    c.execute("INSERT INTO MOB_PRODUK_URUT VALUES (1, 1)", &[]).unwrap();
    c.commit().unwrap();

    let (_, j) = katalog::jejak_produk(&c, 1).unwrap();
    assert!(j.bisa_hapus());
    let p = katalog::hapus_produk(&c, 1).unwrap();
    assert_eq!(p.kode, "D-BERSIH");
    assert!(katalog::produk_admin_by_id(&c, 1).unwrap().is_none());
    let sisa: i64 = c.query_row_as("SELECT COUNT(*) FROM MOB_PRODUK_URUT WHERE PRODUCT_ID = 1", &[]).unwrap();
    assert_eq!(sisa, 0);
    let potongan: i64 = c.query_row_as("SELECT COUNT(*) FROM POS_POTONGANBERDASARKANQTY", &[]).unwrap();
    assert_eq!(potongan, 0);

    for id in 2..=5 {
        match katalog::hapus_produk(&c, id) {
            Err(AppError::Rule { kode, .. }) => assert_eq!(kode, "PRODUK_BERTRANSAKSI"),
            other => panic!("produk {id} harus ditolak: {other:?}"),
        }
        assert!(katalog::produk_admin_by_id(&c, id).unwrap().is_some());
    }
    assert!(matches!(katalog::hapus_produk(&c, 999), Err(AppError::NotFound(_))));
}

#[test]
fn harga_rendah_menyaring_dan_mengurutkan() {
    use pos_api::db::katalog;

    let Some(c) = koneksi() else {
        eprintln!("dilewati: POS_TEST_ORACLE tidak diset");
        return;
    };
    db::migrate(&c).unwrap();
    let Some(_skema) = SkemaPos::buat(&c) else { return };
    // (id, kode, harga jual, harga beli, aktif)
    for (id, kode, harga, beli, aktif) in [
        (1, "H-RUGI", 900, 1000, "Y"),
        (2, "H-IMPAS", 1000, 1000, "Y"),
        (3, "H-UNTUNG", 1500, 1000, "Y"),
        (4, "H-BELI0", 0, 0, "Y"),
        (5, "H-NONAKTIF", 500, 1000, "N"),
        (6, "H-NULL", 700, 0, "Y"), // BELI di-NULL-kan di bawah
    ] {
        c.execute_named(
            "INSERT INTO POS_PRODUCT VALUES (:i, :k, :k, :k, 'PCS', :h, :b, 1, :a)",
            &[("i", &id), ("k", &kode), ("h", &harga), ("b", &beli), ("a", &aktif)],
        )
        .unwrap();
    }
    c.execute("UPDATE POS_PRODUCT SET BELI = NULL WHERE KODE_ITEM = 'H-NULL'", &[]).unwrap();
    c.commit().unwrap();

    let h = katalog::daftar_produk_admin(&c, None, None, None, true, 1, 10).unwrap();
    let kode: Vec<&str> = h.items.iter().map(|p| p.kode.as_str()).collect();
    // Rugi dulu (margin terkecil), lalu produk tanpa HPP; untung dan nonaktif tidak ada.
    assert_eq!(kode, ["H-RUGI", "H-IMPAS", "H-BELI0", "H-NULL"]);
    let hr = katalog::jumlah_harga_rendah(&c).unwrap();
    assert_eq!((hr.rugi, hr.tanpa_hpp, hr.total()), (2, 2, 4));
    // Tanpa filter, semua produk tetap muncul (jalur lama tidak berubah).
    let semua = katalog::daftar_produk_admin(&c, None, None, None, false, 1, 10).unwrap();
    assert_eq!(semua.items.len(), 6);
}

#[test]
fn dokumen_so_multi_barang_riwayat_dan_idempotensi() {
    use pos_api::{db::{faktur, katalog, opname::{self, BarisReq}}, error::AppError};
    use rust_decimal::Decimal;

    let Some(c) = koneksi() else {
        eprintln!("dilewati: POS_TEST_ORACLE tidak diset");
        return;
    };
    db::migrate(&c).unwrap();
    let Some(_skema) = SkemaPos::buat(&c) else { return };
    let (hari, _) = faktur::sekarang(&c).unwrap();
    let awal = chrono::NaiveDate::from_ymd_opt(chrono::Datelike::year(&hari), 1, 1).unwrap();
    for (id, kode, beli, aktif) in [(1, "D-A", 1000, "Y"), (2, "D-B", 500, "Y"), (3, "D-C", 0, "Y"), (4, "D-OFF", 100, "N")] {
        c.execute_named(
            "INSERT INTO POS_PRODUCT VALUES (:i, :k, :k, :k, 'PCS', 2000, :b, 1, :a)",
            &[("i", &id), ("k", &kode), ("b", &beli), ("a", &aktif)],
        )
        .unwrap();
    }
    c.execute_named(
        "INSERT INTO POS_STOCK VALUES ('D-A', TO_DATE(:d,'YYYY-MM-DD'), 5, 1000)",
        &[("d", &awal.format("%Y-%m-%d").to_string())],
    )
    .unwrap();
    c.commit().unwrap();

    let nama = format!("it.{}", &uuid::Uuid::new_v4().simple().to_string()[..10]);
    let uid = user::create_user(&c, &nama, "Uji SO", &password::hash("x-12345678").unwrap(), Role::Admin, None).unwrap();
    let d = |s: &str| s.parse::<Decimal>().unwrap();
    let br = |id: i64, q: &str| BarisReq { produk_id: id, qty_fisik: d(q) };
    let key = uuid::Uuid::new_v4().to_string();

    // D-A: sistem 5 -> fisik 8 (+3); D-B: sistem 0 -> fisik 4 (+4). Satu nomor SO untuk dua baris.
    let r = opname::simpan_dokumen(&c, uid, &key, &[br(2, "4"), br(1, "8")], hari).unwrap();
    assert_eq!((r.items.len(), r.ulang), (2, false));
    let a = r.items.iter().find(|b| b.kode == "D-A").unwrap();
    assert_eq!((a.jumlah_sistem, a.qty_fisik, a.selisih), (d("5"), d("8"), d("3")));
    let stok = katalog::stok_map(&c, &["D-A".into(), "D-B".into()], awal, hari).unwrap();
    assert_eq!((stok["D-A"], stok["D-B"]), (d("8"), d("4")));
    let n: i64 = c
        .query_row_as("SELECT COUNT(DISTINCT NOMOR_SO) FROM POS_STOCKOPNAME", &[])
        .unwrap();
    assert_eq!(n, 1);

    // Retry kunci sama (urutan baris berbeda): dokumen yang sama, tanpa baris baru. Payload beda -> konflik.
    let ulang = opname::simpan_dokumen(&c, uid, &key, &[br(1, "8"), br(2, "4")], hari).unwrap();
    assert_eq!((ulang.nomor_so.as_str(), ulang.ulang), (r.nomor_so.as_str(), true));
    let baris: i64 = c.query_row_as("SELECT COUNT(*) FROM POS_STOCKOPNAME", &[]).unwrap();
    assert_eq!(baris, 2);
    assert!(matches!(
        opname::simpan_dokumen(&c, uid, &key, &[br(1, "9"), br(2, "4")], hari),
        Err(AppError::Conflict(_))
    ));

    // Ditolak seluruhnya: produk ganda, produk nonaktif, tambah stok tanpa harga beli, produk tak ada.
    let k = || uuid::Uuid::new_v4().to_string();
    assert!(opname::simpan_dokumen(&c, uid, &k(), &[br(1, "1"), br(1, "2")], hari).is_err());
    assert!(opname::simpan_dokumen(&c, uid, &k(), &[br(1, "1"), br(4, "2")], hari).is_err());
    assert!(opname::simpan_dokumen(&c, uid, &k(), &[br(1, "1"), br(3, "2")], hari).is_err());
    assert!(matches!(
        opname::simpan_dokumen(&c, uid, &k(), &[br(1, "1"), br(99, "2")], hari),
        Err(AppError::NotFound(_))
    ));
    let baris: i64 = c.query_row_as("SELECT COUNT(*) FROM POS_STOCKOPNAME", &[]).unwrap();
    assert_eq!(baris, 2); // tidak ada baris sebagian yang tertinggal

    // Selisih 0 ikut dicatat, nomor SO berikutnya naik, riwayat dan detail sesuai.
    let r2 = opname::simpan_dokumen(&c, uid, &k(), &[br(1, "8")], hari).unwrap();
    assert_ne!(r2.nomor_so, r.nomor_so);
    assert_eq!(r2.items[0].selisih, Decimal::ZERO);
    let (daftar, lagi) = opname::daftar_so(&c, chrono::Datelike::year(&hari), 1, 10).unwrap();
    assert_eq!((daftar.len(), lagi), (2, false));
    assert_eq!(daftar[0].nomor_so, r2.nomor_so); // terbaru dulu
    let asli = daftar.iter().find(|s| s.nomor_so == r.nomor_so).unwrap();
    assert_eq!((asli.jumlah_item, asli.nilai_selisih), (2, d("5000"))); // 3 x 1000 + 4 x 500
    let det = opname::detail_so(&c, &r.nomor_so).unwrap();
    assert_eq!(det.items.len(), 2);
    assert!(matches!(opname::detail_so(&c, "SO-00-000000"), Err(AppError::NotFound(_))));

    c.execute("DELETE FROM MOB_IDEMPOTENCY WHERE USER_ID = :1", &[&uid]).unwrap();
    c.execute("DELETE FROM MOB_USER WHERE USER_ID = :1", &[&uid]).unwrap();
    c.commit().unwrap();
}

#[test]
fn tren_penjualan_dan_jumlah_stok_habis() {
    use pos_api::db::{dashboard, faktur, katalog};

    let Some(c) = koneksi() else {
        eprintln!("dilewati: POS_TEST_ORACLE tidak diset");
        return;
    };
    db::migrate(&c).unwrap();
    let Some(_skema) = SkemaPos::buat(&c) else { return };
    let (hari, _) = faktur::sekarang(&c).unwrap();
    let awal = chrono::NaiveDate::from_ymd_opt(chrono::Datelike::year(&hari), 1, 1).unwrap();
    // Hari ini: 2 transaksi (300 + 200); kemarin: 1 (100); 10 hari lalu: di luar jendela 7 hari.
    for (no, hari_lalu, total, bayar) in [("T1", 0, 300, "TUNAI"), ("T2", 0, 200, "KREDIT"), ("T3", 1, 100, "TUNAI"), ("T4", 10, 999, "TUNAI")] {
        c.execute_named(
            "INSERT INTO POS_PENJUALAN (NO_TRANSAKSI, TANGGAL, TOTAL, JENIS_BAYAR, KASIR) VALUES (:n, TRUNC(SYSDATE) - :h + 0.5, :t, :b, 'k')",
            &[("n", &no), ("h", &hari_lalu), ("t", &total), ("b", &bayar)],
        )
        .unwrap();
    }
    // Dua produk aktif: satu dengan stok 0, satu dengan stok 3; satu produk nonaktif tanpa stok.
    for (id, kode, aktif) in [(1, "R-A", "Y"), (2, "R-B", "Y"), (3, "R-OFF", "N")] {
        c.execute_named(
            "INSERT INTO POS_PRODUCT VALUES (:i, :k, :k, :k, 'PCS', 100, 50, 1, :a)",
            &[("i", &id), ("k", &kode), ("a", &aktif)],
        )
        .unwrap();
    }
    c.execute_named(
        "INSERT INTO POS_STOCK VALUES ('R-B', TO_DATE(:d,'YYYY-MM-DD'), 3, 50)",
        &[("d", &awal.format("%Y-%m-%d").to_string())],
    )
    .unwrap();
    c.commit().unwrap();

    let t = dashboard::tren_penjualan(&c, hari, 7).unwrap();
    assert_eq!(t.len(), 7);
    assert_eq!((t[6].jumlah, t[6].total), (2, 500));
    assert_eq!((t[5].jumlah, t[5].total), (1, 100));
    assert_eq!(t[..5].iter().map(|h| h.total).sum::<i64>(), 0); // T4 (10 hari lalu) tidak ikut
    assert_eq!(katalog::jumlah_stok_habis(&c, awal, hari).unwrap(), 1);
}

#[test]
fn rincian_penjualan_tunai_kredit_per_kasir() {
    use pos_api::db::{dashboard, faktur};

    let Some(c) = koneksi() else {
        eprintln!("dilewati: POS_TEST_ORACLE tidak diset");
        return;
    };
    db::migrate(&c).unwrap();
    let Some(_skema) = SkemaPos::buat(&c) else { return };
    let (hari, _) = faktur::sekarang(&c).unwrap();
    let dari = hari - chrono::Duration::days(2);
    // (no, hari_lalu, total, jenis, kasir, nama): k1 tunai+kredit hari ini, k2 tanpa nama + jenis lain kemarin,
    // satu baris 5 hari lalu di luar rentang.
    for (no, lalu, total, jenis, kasir, nama) in [
        ("R1", 0, 300, "TUNAI", "k1", "Rina"),
        ("R2", 0, 200, "KREDIT", "k1", "Rina"),
        ("R3", 2, 100, "TUNAI", "k1", "Rina"),
        ("R4", 1, 50, "TRANSFER", "k2", ""),
        ("R5", 1, 70, "KREDIT", "k2", ""),
        ("R6", 5, 999, "TUNAI", "k1", "Rina"),
    ] {
        c.execute_named(
            "INSERT INTO POS_PENJUALAN (NO_TRANSAKSI, TANGGAL, TOTAL, JENIS_BAYAR, KASIR, NAMA_KASIR) \
             VALUES (:n, TRUNC(SYSDATE) - :l + 0.5, :t, :j, :k, NULLIF(:m, ''))",
            &[("n", &no), ("l", &lalu), ("t", &total), ("j", &jenis), ("k", &kasir), ("m", &nama)],
        )
        .unwrap();
    }
    c.commit().unwrap();

    let r = dashboard::rincian_penjualan(&c, dari, hari).unwrap();
    assert_eq!(r.hari, 3);
    assert_eq!((r.ringkasan.total.jumlah, r.ringkasan.total.total), (5, 720));
    assert_eq!((r.ringkasan.tunai.jumlah, r.ringkasan.tunai.total), (2, 400));
    assert_eq!((r.ringkasan.kredit.jumlah, r.ringkasan.kredit.total), (2, 270));
    assert_eq!((r.ringkasan.lain.jumlah, r.ringkasan.lain.total), (1, 50));
    // Per hari: lama -> baru, hari kosong tidak ada karena semua hari punya transaksi.
    assert_eq!(r.harian.len(), 3);
    assert_eq!((r.harian[2].tunai, r.harian[2].kredit), (300, 200)); // hari ini
    assert_eq!((r.harian[1].kredit, r.harian[1].lain), (70, 50)); // kemarin
    // Per kasir: urut total menurun, tanpa baris di luar rentang, dan jumlahnya cocok dengan ringkasan.
    assert_eq!(r.per_kasir.len(), 2);
    assert_eq!((r.per_kasir[0].kasir.as_str(), r.per_kasir[0].nama.as_str()), ("k1", "Rina"));
    assert_eq!(r.per_kasir[0].pecahan.total.total, 600);
    assert_eq!((r.per_kasir[1].kasir.as_str(), r.per_kasir[1].nama.as_str()), ("k2", ""));
    let jumlah: i64 = r.per_kasir.iter().map(|k| k.pecahan.total.total).sum();
    assert_eq!(jumlah, r.ringkasan.total.total);

    // Rentang satu hari tanpa transaksi: semua nol, satu baris harian.
    let kosong = dashboard::rincian_penjualan(&c, hari - chrono::Duration::days(20), hari - chrono::Duration::days(20)).unwrap();
    assert_eq!((kosong.hari, kosong.harian.len(), kosong.per_kasir.len()), (1, 1, 0));
    assert_eq!(kosong.ringkasan.total.total, 0);
}
