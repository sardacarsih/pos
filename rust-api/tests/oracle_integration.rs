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
    // Trigger MOB_PUSH_SALE_INSERT butuh POS_PENJUALAN ada sebelum migrate.
    let punya_penjualan: i64 = c
        .query_row_as(
            "SELECT COUNT(*) FROM USER_TABLES WHERE TABLE_NAME = 'POS_PENJUALAN'",
            &[],
        )
        .unwrap();
    if punya_penjualan == 0 {
        c.execute(
            "CREATE TABLE POS_PENJUALAN (ID_PENJUALAN NUMBER, NO_TRANSAKSI VARCHAR2(30), NIK VARCHAR2(30), TANGGAL DATE)",
            &[],
        )
        .unwrap();
    }
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
    if punya_penjualan == 0 {
        c.execute("DROP TABLE POS_PENJUALAN", &[]).unwrap();
    }
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
            "CREATE TABLE POS_PRODUCT (PRODUCTID NUMBER GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY, KODE_ITEM VARCHAR2(30), BARCODE VARCHAR2(30), PRODUCTNAME VARCHAR2(100), SATUAN VARCHAR2(10), PRICE NUMBER, BELI NUMBER, KATEGORI_ID NUMBER, AKTIF VARCHAR2(1), STOK_MIN NUMBER(12,4))",
            "CREATE TABLE POS_KATEGORI (KODE NUMBER PRIMARY KEY, KATEGORI VARCHAR2(50))",
            "CREATE TABLE POS_STOCK (PRODUCT_ID NUMBER, KODE_BARANG VARCHAR2(30), TANGGAL DATE, QUANTITY NUMBER, HPP NUMBER)",
            "CREATE TABLE POS_PEMBELIAN (PURCHASE_ID NUMBER PRIMARY KEY, NO_TRANSAKSI VARCHAR2(30), TANGGAL DATE)",
            "CREATE TABLE POS_PEMBELIANDETAIL (PURCHASE_ID NUMBER, NO_TRANSAKSI VARCHAR2(30), KODE_BARANG VARCHAR2(30), QUANTITY NUMBER, HARGA_BELI NUMBER)",
            "CREATE TABLE POS_PENJUALAN (ID_PENJUALAN NUMBER GENERATED BY DEFAULT AS IDENTITY, NO_TRANSAKSI VARCHAR2(30), NIK VARCHAR2(30), TANGGAL DATE, TOTAL NUMBER, JENIS_BAYAR VARCHAR2(10), KASIR VARCHAR2(30), NAMA_KASIR VARCHAR2(60))",
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
        // MOB_STOK_SESUAI juga dibuat migrasi 0010; dibuat di sini agar tersedia bila migrate belum jalan.
        let _ = c.execute("DROP TABLE MOB_STOK_SESUAI PURGE", &[]);
        c.execute(
            "CREATE TABLE MOB_STOK_SESUAI (ID NUMBER GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY, \
             KODE_BARANG VARCHAR2(30) NOT NULL, TANGGAL DATE NOT NULL, QTY NUMBER(12,4) NOT NULL CHECK (QTY <> 0), \
             ALASAN VARCHAR2(250) NOT NULL, USER_ID NUMBER NOT NULL, USER_NAMA VARCHAR2(100), \
             BALIK_ID NUMBER REFERENCES MOB_STOK_SESUAI(ID), CREATED_AT TIMESTAMP DEFAULT SYSTIMESTAMP NOT NULL)",
            &[],
        )
        .unwrap();
        Some(Self(c))
    }
}

impl Drop for SkemaPos<'_> {
    fn drop(&mut self) {
        let _ = self.0.execute("DROP TABLE MOB_STOK_SESUAI CASCADE CONSTRAINTS PURGE", &[]);
        for t in TABEL_POS {
            let _ = self.0.execute(&format!("DROP TABLE {t} CASCADE CONSTRAINTS PURGE"), &[]);
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
    let _ = tracing_subscriber::fmt().with_test_writer().try_init();
    let Some(_skema) = SkemaPos::buat(&c) else { return };
    db::migrate(&c).unwrap();
    let (hari, _) = faktur::sekarang(&c).unwrap();
    let awal = chrono::NaiveDate::from_ymd_opt(chrono::Datelike::year(&hari), 1, 1).unwrap();
    for (id, kode, beli) in [(1, "IT-A", 1000), (2, "IT-B", 0)] {
        c.execute_named(
            "INSERT INTO POS_PRODUCT (PRODUCTID, KODE_ITEM, BARCODE, PRODUCTNAME, SATUAN, PRICE, BELI, KATEGORI_ID, AKTIF) VALUES (:i, :k, :k, :k, 'PCS', 2000, :b, 1, 'Y')",
            &[("i", &id), ("k", &kode), ("b", &beli)],
        )
        .unwrap();
    }
    // IT-A: stok awal 5 lalu terjual 5 -> habis. IT-B juga habis (tanpa transaksi) dengan harga beli 0.
    c.execute_named(
        "INSERT INTO POS_STOCK (KODE_BARANG, TANGGAL, QUANTITY, HPP) VALUES ('IT-A', TO_DATE(:d,'YYYY-MM-DD'), 5, 1000)",
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
    let Some(_skema) = SkemaPos::buat(&c) else { return };
    db::migrate(&c).unwrap();
    for (id, kode) in [(1, "D-BERSIH"), (2, "D-JUAL"), (3, "D-STOK"), (4, "D-OPNAME"), (5, "D-PENDING")] {
        c.execute_named(
            "INSERT INTO POS_PRODUCT (PRODUCTID, KODE_ITEM, BARCODE, PRODUCTNAME, SATUAN, PRICE, BELI, KATEGORI_ID, AKTIF) VALUES (:i, :k, :k, :k, 'PCS', 1000, 500, 1, 'Y')",
            &[("i", &id), ("k", &kode)],
        )
        .unwrap();
    }
    c.execute("INSERT INTO POS_PENJUALAN_DETAIL (NO_TRANSAKSI, KODE_BARANG, JUMLAH_BARANG) VALUES ('T1', 'D-JUAL', 1)", &[]).unwrap();
    c.execute("INSERT INTO POS_STOCK (KODE_BARANG, TANGGAL, QUANTITY, HPP) VALUES ('D-STOK', SYSDATE, 0, 0)", &[]).unwrap();
    c.execute("INSERT INTO POS_STOCKOPNAME VALUES ('SO-1', SYSDATE, 'D-OPNAME', 1, 1, 0, 0, 0)", &[]).unwrap();
    c.execute("INSERT INTO POS_PENDING_DETAIL (NO_TRANSAKSI, KODE_BARANG, JUMLAH_BARANG) VALUES ('P1', 'D-PENDING', 1)", &[]).unwrap();
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
fn kategori_crud_dan_hapus_ditolak_saat_dipakai() {
    use pos_api::{db::katalog, error::AppError};

    let Some(c) = koneksi() else {
        eprintln!("dilewati: POS_TEST_ORACLE tidak diset");
        return;
    };
    let Some(_skema) = SkemaPos::buat(&c) else { return };
    db::migrate(&c).unwrap();

    let a = katalog::kategori_buat(&c, "  Minuman Uji  ").unwrap();
    assert_eq!(a.nama, "Minuman Uji");
    let b = katalog::kategori_buat(&c, "Camilan Uji").unwrap();
    assert_ne!(a.id, b.id);
    assert!(katalog::semua_kategori(&c).unwrap().iter().any(|k| k.id == a.id));
    assert!(matches!(katalog::kategori_buat(&c, "minuman uji"), Err(AppError::Conflict(_))));

    katalog::kategori_ubah(&c, &a.id, "Minuman Ringan").unwrap();
    assert!(matches!(katalog::kategori_ubah(&c, &a.id, "CAMILAN UJI"), Err(AppError::Conflict(_))));
    katalog::kategori_ubah(&c, &a.id, "Minuman Ringan").unwrap();
    assert!(matches!(katalog::kategori_ubah(&c, "999999", "X"), Err(AppError::NotFound(_))));
    assert!(matches!(katalog::kategori_buat(&c, "   "), Err(AppError::Validation(_))));

    c.execute_named(
        "INSERT INTO POS_PRODUCT (PRODUCTID, KODE_ITEM, BARCODE, PRODUCTNAME, SATUAN, PRICE, BELI, KATEGORI_ID, AKTIF) VALUES (1, 'K-UJI', 'K-UJI', 'K-UJI', 'PCS', 1000, 500, :k, 'Y')",
        &[("k", &a.id.parse::<i64>().unwrap())],
    )
    .unwrap();
    c.commit().unwrap();
    let (k, n) = katalog::kategori_jejak(&c, &a.id).unwrap();
    assert_eq!((k.nama.as_str(), n), ("Minuman Ringan", 1));
    assert!(matches!(katalog::kategori_hapus(&c, &a.id), Err(AppError::Conflict(_))));

    user::set_kategori(&c, &[a.id.clone(), b.id.clone()]).unwrap();
    c.execute("UPDATE POS_PRODUCT SET KATEGORI_ID = NULL WHERE PRODUCTID = 1", &[]).unwrap();
    c.commit().unwrap();
    let (_, n0) = katalog::kategori_jejak(&c, &a.id).unwrap();
    assert_eq!(n0, 0);
    katalog::kategori_hapus(&c, &a.id).unwrap();
    assert!(matches!(katalog::kategori_hapus(&c, &a.id), Err(AppError::NotFound(_))));
    assert_eq!(user::allowed_kategori(&c).unwrap(), vec![b.id.clone()]);

    katalog::kategori_hapus(&c, &b.id).unwrap();
    c.execute("DELETE FROM POS_PRODUCT WHERE PRODUCTID = 1", &[]).unwrap();
    c.commit().unwrap();
}

#[test]
fn harga_rendah_menyaring_dan_mengurutkan() {
    use pos_api::db::katalog;

    let Some(c) = koneksi() else {
        eprintln!("dilewati: POS_TEST_ORACLE tidak diset");
        return;
    };
    let Some(_skema) = SkemaPos::buat(&c) else { return };
    db::migrate(&c).unwrap();
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
            "INSERT INTO POS_PRODUCT (PRODUCTID, KODE_ITEM, BARCODE, PRODUCTNAME, SATUAN, PRICE, BELI, KATEGORI_ID, AKTIF) VALUES (:i, :k, :k, :k, 'PCS', :h, :b, 1, :a)",
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
    let _ = tracing_subscriber::fmt().with_test_writer().try_init();
    let Some(_skema) = SkemaPos::buat(&c) else { return };
    db::migrate(&c).unwrap();
    let (hari, _) = faktur::sekarang(&c).unwrap();
    let awal = chrono::NaiveDate::from_ymd_opt(chrono::Datelike::year(&hari), 1, 1).unwrap();
    for (id, kode, beli, aktif) in [(1, "D-A", 1000, "Y"), (2, "D-B", 500, "Y"), (3, "D-C", 0, "Y"), (4, "D-OFF", 100, "N")] {
        c.execute_named(
            "INSERT INTO POS_PRODUCT (PRODUCTID, KODE_ITEM, BARCODE, PRODUCTNAME, SATUAN, PRICE, BELI, KATEGORI_ID, AKTIF) VALUES (:i, :k, :k, :k, 'PCS', 2000, :b, 1, :a)",
            &[("i", &id), ("k", &kode), ("b", &beli), ("a", &aktif)],
        )
        .unwrap();
    }
    c.execute_named(
        "INSERT INTO POS_STOCK (KODE_BARANG, TANGGAL, QUANTITY, HPP) VALUES ('D-A', TO_DATE(:d,'YYYY-MM-DD'), 5, 1000)",
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
    let _ = tracing_subscriber::fmt().with_test_writer().try_init();
    let Some(_skema) = SkemaPos::buat(&c) else { return };
    db::migrate(&c).unwrap();
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
            "INSERT INTO POS_PRODUCT (PRODUCTID, KODE_ITEM, BARCODE, PRODUCTNAME, SATUAN, PRICE, BELI, KATEGORI_ID, AKTIF) VALUES (:i, :k, :k, :k, 'PCS', 100, 50, 1, :a)",
            &[("i", &id), ("k", &kode), ("a", &aktif)],
        )
        .unwrap();
    }
    c.execute_named(
        "INSERT INTO POS_STOCK (KODE_BARANG, TANGGAL, QUANTITY, HPP) VALUES ('R-B', TO_DATE(:d,'YYYY-MM-DD'), 3, 50)",
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
    let _ = tracing_subscriber::fmt().with_test_writer().try_init();
    let Some(_skema) = SkemaPos::buat(&c) else { return };
    db::migrate(&c).unwrap();
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
             VALUES (:n, TRUNC(SYSDATE) - :l + 0.5, :t, :j, :k, :m)",
            &[("n", &no), ("l", &lalu), ("t", &total), ("j", &jenis), ("k", &kasir), ("m", &(!nama.is_empty()).then_some(nama))],
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

#[test]
fn buat_produk_kode_berurutan_duplikat_dan_idempoten() {
    use pos_api::{db::{faktur, katalog}, error::AppError};
    use rust_decimal::Decimal;

    let Some(c) = koneksi() else {
        eprintln!("dilewati: POS_TEST_ORACLE tidak diset");
        return;
    };
    let Some(_skema) = SkemaPos::buat(&c) else { return };
    db::migrate(&c).unwrap();
    let _ = tracing_subscriber::fmt().with_test_writer().try_init();
    for ddl in [
        "ALTER TABLE POS_PENJUALAN ADD (JAM VARCHAR2(10), ID_PELANGGAN NUMBER, NAMA_PELANGGAN VARCHAR2(100),
            UNIT_KERJA VARCHAR2(30), STATUS VARCHAR2(10), BRUTO NUMBER, POTONGAN NUMBER,
            KET_BAYAR VARCHAR2(10), TENOR NUMBER, ANGSURAN NUMBER, PENDING VARCHAR2(1))",
        "ALTER TABLE POS_PENJUALAN_DETAIL ADD (ID_PENJUALAN NUMBER, BARIS NUMBER, PRODUCT_ID NUMBER,
            BARCODE VARCHAR2(50), NAMA_BARANG VARCHAR2(100), SATUAN VARCHAR2(20), HPP NUMBER,
            HARGA_BARANG NUMBER, BRUTO NUMBER, POTONGAN NUMBER, TOTAL_HARGA NUMBER)",
        "ALTER TABLE POS_POTONGANBERDASARKANQTY ADD (MINQTY NUMBER, POTONGAN NUMBER)",
        "CREATE TABLE POS_PERIODE (PERIODE NUMBER, BDARI VARCHAR2(20), BSAMPAI VARCHAR2(20),
            REMISE1 VARCHAR2(1), REMISE2 VARCHAR2(1), R1DARI VARCHAR2(20), R1SAMPAI VARCHAR2(20),
            R2DARI VARCHAR2(20), R2SAMPAI VARCHAR2(20))",
        "CREATE TABLE FIN_ANGGOTA (ID_PELANGGAN NUMBER, NIK VARCHAR2(30), NAMA_PELANGGAN VARCHAR2(100),
            UNIT_KERJA VARCHAR2(30), STATUS VARCHAR2(10), LIMIT_HUTANG NUMBER, AKTIF VARCHAR2(1))",
        "CREATE TABLE FIN_UNITKERJA (KODE VARCHAR2(30), NAMA VARCHAR2(100))",
        "CREATE TABLE FIN_SETTINGS (CONFIG VARCHAR2(50), JUMLAH NUMBER)",
    ] {
        c.execute(ddl, &[]).unwrap();
    }
    // Hanya skema inti API, karena fixture POS tidak memuat skema pembelian dan kas lengkap.
    for ddl in include_str!("../src/db/schema.sql").split("\n/\n") {
        let ddl = ddl.lines().filter(|s| !s.starts_with("--")).collect::<Vec<_>>().join("\n");
        let ddl = ddl.trim().trim_end_matches('/').trim();
        if !ddl.is_empty() {
            match c.execute(ddl, &[]) {
                Ok(_) => {}
                Err(e) if e.oci_code() == Some(955) => {}
                Err(e) => panic!("{e}: {ddl}"),
            }
        }
    }
    c.execute("INSERT INTO POS_KATEGORI VALUES (1, 'Umum')", &[]).unwrap();
    c.commit().unwrap();
    let (hari, _) = faktur::sekarang(&c).unwrap();
    let yy = chrono::Datelike::year(&hari) % 100;

    let nama = format!("it.{}", &uuid::Uuid::new_v4().simple().to_string()[..10]);
    let uid = user::create_user(&c, &nama, "Uji Buat", &password::hash("x-12345678").unwrap(), Role::Admin, None).unwrap();
    let produk = |n: &str, bc: &str| katalog::UbahProduk {
        nama: n.into(),
        barcode: bc.into(),
        satuan: "PCS".into(),
        kategori_id: "1".into(),
        harga: 5000,
        beli: 4000,
        aktif: true,
    };

    let k1 = uuid::Uuid::new_v4().to_string();
    let a = katalog::buat_produk(&c, uid, &k1, &produk("Teh Botol", "899001"), &[]).unwrap();
    assert_eq!(a.kode, format!("KB-{yy:02}-00001"));
    assert!(a.aktif);
    // permintaan ulang dengan kunci sama = produk yang sama, tidak membuat baris baru
    let ulang = katalog::buat_produk(&c, uid, &k1, &produk("Teh Botol", "899001"), &[]).unwrap();
    assert_eq!(ulang.id, a.id);
    // kunci sama dengan isian lain ditolak
    assert!(matches!(katalog::buat_produk(&c, uid, &k1, &produk("Lain", ""), &[]), Err(AppError::Conflict(_))));

    let b = katalog::buat_produk(&c, uid, &uuid::Uuid::new_v4().to_string(), &produk("Kopi", ""), &[]).unwrap();
    assert_eq!(b.kode, format!("KB-{yy:02}-00002"));
    // nama dan barcode ganda ditolak (409), kategori tak dikenal ditolak (400)
    assert!(matches!(katalog::buat_produk(&c, uid, &uuid::Uuid::new_v4().to_string(), &produk("kopi", ""), &[]), Err(AppError::Conflict(_))));
    assert!(matches!(katalog::buat_produk(&c, uid, &uuid::Uuid::new_v4().to_string(), &produk("Baru", "899001"), &[]), Err(AppError::Conflict(_))));
    let mut salah = produk("Baru2", "");
    salah.kategori_id = "999".into();
    assert!(matches!(katalog::buat_produk(&c, uid, &uuid::Uuid::new_v4().to_string(), &salah, &[]), Err(AppError::Validation(_))));
    let total: i64 = c.query_row_as("SELECT COUNT(*) FROM POS_PRODUCT", &[]).unwrap();
    assert_eq!(total, 2);

    let baru = produk("Teh Kotak", "899002");
    let satuan = |nama: &str, isi: Decimal, barcode: &str| katalog::SatuanJual {
        satuan: nama.into(),
        isi,
        harga: Decimal::new(55000, 0),
        barcode: barcode.into(),
    };
    let box12 = satuan("BOX", Decimal::new(12, 0), "899012");
    let pak6 = satuan("PAK", Decimal::new(6, 0), "");
    let k2 = uuid::Uuid::new_v4().to_string();
    c.execute("ALTER TABLE POS_PRODUCT_SATUAN ADD CHECK (SATUAN <> 'GAGAL_UJI')", &[]).unwrap();
    let gagal = satuan("GAGAL_UJI", Decimal::new(24, 0), "899024");
    assert!(katalog::buat_produk(&c, uid, &k2, &baru, &[box12.clone(), gagal]).is_err());
    assert_eq!(c.query_row_as::<i64>("SELECT COUNT(*) FROM POS_PRODUCT", &[]).unwrap(), 2);
    assert_eq!(c.query_row_as::<i64>("SELECT COUNT(*) FROM POS_PRODUCT_SATUAN", &[]).unwrap(), 0);
    assert_eq!(c.query_row_as::<i64>(
        "SELECT COUNT(*) FROM MOB_IDEMPOTENCY WHERE IDEMPOTENCY_KEY = :1", &[&k2],
    ).unwrap(), 0);

    let p = katalog::buat_produk(&c, uid, &k2, &baru, &[box12.clone(), pak6.clone()]).unwrap();
    assert_eq!(p.kode, format!("KB-{yy:02}-00003"));
    let (id, nama_satuan) = katalog::produk_by_barcode_satuan(&c, "899012").unwrap().unwrap();
    assert_eq!((id, nama_satuan.as_str()), (p.id, "BOX"));
    let unit = katalog::satuan_jual(&c, id, &nama_satuan).unwrap().unwrap();
    assert_eq!((unit.isi, unit.harga), (Decimal::new(12, 0), Decimal::new(55000, 0)));
    assert!(matches!(
        katalog::ubah_produk(&c, a.id, &produk("Teh Botol", "899012")),
        Err(AppError::Conflict(_))
    ));
    assert!(matches!(
        katalog::ganti_satuan(&c, a.id, &[box12.clone()]),
        Err(AppError::Rule { kode: "BARCODE_SUDAH_ADA", .. })
    ));
    let box_normal = satuan(" BOX ", Decimal::new(1200, 2), " 899012 ");
    let ulangan = katalog::buat_produk(&c, uid, &k2, &baru, &[pak6, box_normal]).unwrap();
    assert_eq!(ulangan.id, id);
    assert_eq!(katalog::satuan_produk(&c, id).unwrap().len(), 2);
    assert!(matches!(katalog::buat_produk(&c, uid, &k2, &baru, &[box12]), Err(AppError::Conflict(_))));

    let mulai = std::sync::Barrier::new(2);
    let (hasil_dasar, hasil_satuan) = std::thread::scope(|scope| {
        let dasar = scope.spawn(|| {
            mulai.wait();
            let c = koneksi().unwrap();
            katalog::buat_produk(&c, uid, &uuid::Uuid::new_v4().to_string(), &produk("Dasar paralel", "899099"), &[])
        });
        let tambahan = scope.spawn(|| {
            mulai.wait();
            let c = koneksi().unwrap();
            katalog::buat_produk(
                &c, uid, &uuid::Uuid::new_v4().to_string(), &produk("Satuan paralel", ""),
                &[satuan("BOX", Decimal::new(12, 0), "899099")],
            )
        });
        (dasar.join().unwrap(), tambahan.join().unwrap())
    });
    assert!(matches!(
        (hasil_dasar, hasil_satuan),
        (Ok(_), Err(AppError::Rule { .. })) | (Err(AppError::Conflict(_)), Ok(_))
    ));
    let total_barcode: i64 = c.query_row_as(
        "SELECT (SELECT COUNT(*) FROM POS_PRODUCT WHERE BARCODE = '899099')
              + (SELECT COUNT(*) FROM POS_PRODUCT_SATUAN WHERE BARCODE = '899099') FROM DUAL",
        &[],
    ).unwrap();
    assert_eq!(total_barcode, 1);

    for sql in [
        "INSERT INTO POS_PERIODE (PERIODE, BDARI, BSAMPAI) VALUES
            (202601, '01-JAN-2000', '31-DEC-2099')",
        "INSERT INTO FIN_UNITKERJA VALUES ('UJI', 'Unit Uji')",
        "INSERT INTO FIN_ANGGOTA VALUES (1, 'TUNAI', 'Pembeli Tunai', 'UJI', 'A', 0, 'Y')",
        "INSERT INTO FIN_SETTINGS VALUES ('KONTROL_SALDO_QTY', 1)",
    ] {
        c.execute(sql, &[]).unwrap();
    }
    let awal = chrono::NaiveDate::from_ymd_opt(chrono::Datelike::year(&hari), 1, 1).unwrap();
    c.execute("INSERT INTO POS_STOCK (KODE_BARANG, TANGGAL, QUANTITY, HPP) VALUES (:1, :2, 30, 4000)", &[&p.kode, &awal]).unwrap();
    c.commit().unwrap();
    let key = uuid::Uuid::new_v4().to_string();
    let (nomor, _) = faktur::terbitkan_nomor(&c, uid, &key).unwrap();
    let faktur = faktur::simpan(
        &c,
        &faktur::Kasir { id: uid, username: nama, nama: "Kasir Uji".into(), kategori: vec![] },
        &faktur::SimpanReq {
            no_transaksi: nomor.clone(),
            nik: "TUNAI".into(),
            tenor: None,
            uang_bayar: Some(60000),
            baris: vec![faktur::BarisReq { product_id: id, qty: Decimal::ONE, satuan: Some(nama_satuan) }],
        },
        &faktur::Opsi { default_nik: "TUNAI", default_max_tenor: 1, demo: false },
        &key,
    ).unwrap();
    assert_eq!(faktur.baris[0].satuan, "BOX");
    assert_eq!(faktur.total, 55000);
    let stok = katalog::stok_map(&c, &[p.kode.clone()], awal, hari).unwrap();
    assert_eq!(stok[&p.kode], Decimal::new(18, 0));

    c.execute("DELETE FROM MOB_PUSH_QUEUE WHERE NO_TRANSAKSI = :1", &[&nomor]).unwrap();
    c.execute("DELETE FROM MOB_NOMOR WHERE USER_ID = :1", &[&uid]).unwrap();
    c.execute("DELETE FROM MOB_IDEMPOTENCY WHERE USER_ID = :1", &[&uid]).unwrap();
    c.execute("DELETE FROM MOB_USER WHERE USER_ID = :1", &[&uid]).unwrap();
    c.commit().unwrap();
    for tabel in ["POS_PERIODE", "FIN_ANGGOTA", "FIN_UNITKERJA", "FIN_SETTINGS"] {
        c.execute(&format!("DROP TABLE {tabel} PURGE"), &[]).unwrap();
    }
}

// ---------------------------------------------------------------- persediaan

fn user_uji(c: &Connection, label: &str) -> i64 {
    let nama = format!("it.{}", &uuid::Uuid::new_v4().simple().to_string()[..10]);
    user::create_user(c, &nama, label, &password::hash("x-12345678").unwrap(), Role::Admin, None).unwrap()
}

fn bersihkan_user(c: &Connection, uid: i64) {
    c.execute("DELETE FROM MOB_IDEMPOTENCY WHERE USER_ID = :1", &[&uid]).unwrap();
    c.execute("DELETE FROM MOB_USER WHERE USER_ID = :1", &[&uid]).unwrap();
    c.commit().unwrap();
}

#[test]
fn penyesuaian_idempoten_dan_balik() {
    use chrono::Datelike;
    use pos_api::{db::{faktur, katalog, persediaan}, error::AppError};
    use rust_decimal::Decimal;

    let Some(c) = koneksi() else {
        eprintln!("dilewati: POS_TEST_ORACLE tidak diset");
        return;
    };
    let _ = tracing_subscriber::fmt().with_test_writer().try_init();
    let Some(_skema) = SkemaPos::buat(&c) else { return };
    db::migrate(&c).unwrap();
    let (hari, _) = faktur::sekarang(&c).unwrap();
    let awal = chrono::NaiveDate::from_ymd_opt(chrono::Datelike::year(&hari), 1, 1).unwrap();
    let d = |s: &str| s.parse::<Decimal>().unwrap();
    for (id, kode, beli) in [(1, "PS-A", 1000), (2, "PS-B", 0)] {
        c.execute_named(
            "INSERT INTO POS_PRODUCT (PRODUCTID, KODE_ITEM, BARCODE, PRODUCTNAME, SATUAN, PRICE, BELI, KATEGORI_ID, AKTIF) VALUES (:i, :k, :k, :k, 'PCS', 2000, :b, 1, 'Y')",
            &[("i", &id), ("k", &kode), ("b", &beli)],
        ).unwrap();
    }
    c.execute_named(
        "INSERT INTO POS_STOCK (KODE_BARANG, TANGGAL, QUANTITY, HPP) VALUES ('PS-A', TO_DATE(:d,'YYYY-MM-DD'), 5, 1000)",
        &[("d", &awal.format("%Y-%m-%d").to_string())],
    ).unwrap();
    c.commit().unwrap();

    let uid = user_uji(&c, "Uji Sesuai");
    let tgl_hari = hari.format("%Y-%m-%d").to_string();
    let req = |qty: &str| persediaan::SesuaiReq {
        produk_id: 1,
        qty: d(qty),
        tanggal: tgl_hari.clone(),
        alasan: "Koreksi uji".into(),
    };
    let k = || uuid::Uuid::new_v4().to_string();
    let key = k();

    // +5 -> stok 10; efek terlipat ke baris saldo awal 1 Jan, bukan baris baru bertanggal hari ini.
    let r = persediaan::sesuaikan(&c, uid, "Uji Sesuai", &key, &req("5"), hari).unwrap();
    assert_eq!((r.qty, r.saldo_sesudah, r.ulang), (d("5"), d("10"), false));
    assert!(r.peringatan.is_none());
    let stok = katalog::stok_map(&c, &["PS-A".to_string()], awal, hari).unwrap();
    assert_eq!(stok["PS-A"], d("10"));
    let (n_awal, n_lain): (i64, i64) = c.query_row_as(
        "SELECT SUM(CASE WHEN TANGGAL = TO_DATE(:1,'YYYY-MM-DD') THEN 1 ELSE 0 END), \
                SUM(CASE WHEN TANGGAL <> TO_DATE(:1,'YYYY-MM-DD') THEN 1 ELSE 0 END) \
         FROM POS_STOCK WHERE KODE_BARANG = 'PS-A'",
        &[&awal.format("%Y-%m-%d").to_string()],
    ).unwrap();
    assert_eq!((n_awal, n_lain), (1, 0));
    let q: String = c.query_row_as(&format!("SELECT {} FROM POS_STOCK WHERE KODE_BARANG = 'PS-A'", pos_api::db::tm("QUANTITY")), &[]).unwrap();
    assert_eq!(q, "10");
    let audit: (String, String) = c.query_row_as(
        "SELECT ALASAN, TO_CHAR(TANGGAL,'YYYY-MM-DD') FROM MOB_STOK_SESUAI WHERE ID = :1", &[&r.id],
    ).unwrap();
    assert_eq!(audit, ("Koreksi uji".to_string(), tgl_hari.clone()));

    // Replay kunci sama: hasil sama, tidak ada baris audit baru.
    let ulang = persediaan::sesuaikan(&c, uid, "Uji Sesuai", &key, &req("5"), hari).unwrap();
    assert!(ulang.ulang);
    assert_eq!(ulang.id, r.id);
    let n: i64 = c.query_row_as("SELECT COUNT(*) FROM MOB_STOK_SESUAI", &[]).unwrap();
    assert_eq!(n, 1);
    // Kunci sama, payload beda -> konflik.
    assert!(matches!(persediaan::sesuaikan(&c, uid, "U", &key, &req("6"), hari), Err(AppError::Conflict(_))));

    // Validasi qty/alasan/tanggal.
    assert!(matches!(persediaan::sesuaikan(&c, uid, "U", &k(), &req("0"), hari), Err(AppError::Validation(_))));
    assert!(matches!(persediaan::sesuaikan(&c, uid, "U", &k(), &req("1.00001"), hari), Err(AppError::Validation(_))));
    let tahun_lalu = chrono::NaiveDate::from_ymd_opt(hari.year() - 1, 12, 31).unwrap().format("%Y-%m-%d").to_string();
    let mut req_lalu = req("1");
    req_lalu.tanggal = tahun_lalu.clone();
    assert!(matches!(persediaan::sesuaikan(&c, uid, "U", &k(), &req_lalu, hari), Err(AppError::Validation(_))));
    let mut req_besok = req("1");
    req_besok.tanggal = (hari + chrono::Duration::days(1)).format("%Y-%m-%d").to_string();
    assert!(matches!(persediaan::sesuaikan(&c, uid, "U", &k(), &req_besok, hari), Err(AppError::Validation(_))));
    let mut req_alasan = req("1");
    req_alasan.alasan = "ab".into();
    assert!(matches!(persediaan::sesuaikan(&c, uid, "U", &k(), &req_alasan, hari), Err(AppError::Validation(_))));
    // Qty positif tanpa harga beli ditolak (pola opname).
    let mut req_b = req("1");
    req_b.produk_id = 2;
    assert!(matches!(persediaan::sesuaikan(&c, uid, "U", &k(), &req_b, hari), Err(AppError::Validation(_))));
    // Minus boleh membuat stok negatif, dengan peringatan (10 - 13 = -3).
    let rm = persediaan::sesuaikan(&c, uid, "U", &k(), &req("-13"), hari).unwrap();
    assert_eq!(rm.saldo_sesudah, d("-3"));
    assert!(rm.peringatan.is_some());

    // Balik penyesuaian pertama (+5): saldo -3 -> -8, baris pembalik ber-BALIK_ID.
    let kb = k();
    let rb = persediaan::balik(&c, uid, "U", &kb, r.id, "Salah input", hari).unwrap();
    assert_eq!(rb.qty, d("-5"));
    assert_eq!(rb.saldo_sesudah, d("-8"));
    let stok = katalog::stok_map(&c, &["PS-A".to_string()], awal, hari).unwrap();
    assert_eq!(stok["PS-A"], d("-8"));
    let bid: Option<i64> = c.query_row_as("SELECT BALIK_ID FROM MOB_STOK_SESUAI WHERE ID = :1", &[&rb.id]).unwrap();
    assert_eq!(bid, Some(r.id));
    // Replay balik.
    let ub = persediaan::balik(&c, uid, "U", &kb, r.id, "Salah input", hari).unwrap();
    assert!(ub.ulang);
    assert_eq!(ub.id, rb.id);
    // Balik ganda -> Conflict; balik atas balik -> Validation.
    assert!(matches!(persediaan::balik(&c, uid, "U", &k(), r.id, "Lagi", hari), Err(AppError::Conflict(_))));
    assert!(matches!(persediaan::balik(&c, uid, "U", &k(), rb.id, "Lagi", hari), Err(AppError::Validation(_))));
    // Balik lintas tahun -> Validation (delta sudah terlipat di saldo awal tahun lama).
    c.execute_named(
        "INSERT INTO MOB_STOK_SESUAI (KODE_BARANG, TANGGAL, QTY, ALASAN, USER_ID, USER_NAMA) VALUES ('PS-A', TO_DATE(:t,'YYYY-MM-DD'), 1, 'Uji tahun lalu', :u, 'U')",
        &[("t", &tahun_lalu), ("u", &uid)],
    ).unwrap();
    let id_lalu: i64 = c.query_row_as("SELECT MAX(ID) FROM MOB_STOK_SESUAI", &[]).unwrap();
    assert!(matches!(persediaan::balik(&c, uid, "U", &k(), id_lalu, "Lagi", hari), Err(AppError::Validation(_))));
    assert!(matches!(persediaan::balik(&c, uid, "U", &k(), 999999, "Lagi", hari), Err(AppError::NotFound(_))));

    // Produk tanpa baris saldo awal: INSERT fallback membawa PRODUCT_ID dan HPP.
    let mut req_f = req("-1");
    req_f.produk_id = 2;
    let rf = persediaan::sesuaikan(&c, uid, "U", &k(), &req_f, hari).unwrap();
    assert_eq!(rf.saldo_sesudah, d("-1"));
    let (pid, hpp): (i64, String) = c.query_row_as(
        &format!("SELECT PRODUCT_ID, {} FROM POS_STOCK WHERE KODE_BARANG = 'PS-B'", pos_api::db::tm("HPP")), &[],
    ).unwrap();
    assert_eq!((pid, hpp.as_str()), (2, "0"));

    // Daftar audit: filter kode/nama dan flag dibalik.
    let h = persediaan::daftar_sesuai(&c, Some("ps-a"), 1, 50).unwrap();
    assert!(h.items.iter().all(|s| s.kode == "PS-A"));
    let asal = h.items.iter().find(|s| s.id == r.id).unwrap();
    assert!(asal.dibalik);
    let pembalik = h.items.iter().find(|s| s.id == rb.id).unwrap();
    assert_eq!(pembalik.balik_id, Some(r.id));
    assert!(!pembalik.dibalik);
    assert_eq!(persediaan::daftar_sesuai(&c, Some("tidak-ada"), 1, 50).unwrap().items.len(), 0);

    bersihkan_user(&c, uid);
}

#[test]
fn kartu_stok_rekonsiliasi_dan_paginasi() {
    use pos_api::{db::{faktur, katalog, persediaan}, error::AppError};
    use rust_decimal::Decimal;

    let Some(c) = koneksi() else {
        eprintln!("dilewati: POS_TEST_ORACLE tidak diset");
        return;
    };
    let _ = tracing_subscriber::fmt().with_test_writer().try_init();
    let Some(_skema) = SkemaPos::buat(&c) else { return };
    db::migrate(&c).unwrap();
    let (hari, _) = faktur::sekarang(&c).unwrap();
    let awal = chrono::NaiveDate::from_ymd_opt(chrono::Datelike::year(&hari), 1, 1).unwrap();
    if (hari - awal).num_days() < 30 {
        eprintln!("dilewati: uji kartu stok butuh >= 30 hari sejak 1 Jan");
        return;
    }
    let d = |s: &str| s.parse::<Decimal>().unwrap();
    let tgl = |mundur: i64| (hari - chrono::Duration::days(mundur)).format("%Y-%m-%d").to_string();
    c.execute("INSERT INTO POS_PRODUCT (PRODUCTID, KODE_ITEM, BARCODE, PRODUCTNAME, SATUAN, PRICE, BELI, KATEGORI_ID, AKTIF) VALUES (1, 'KR-A', 'KR-A', 'Kopi Recil', 'PCS', 2000, 1000, 1, 'Y')", &[]).unwrap();
    c.execute_named(
        "INSERT INTO POS_STOCK (KODE_BARANG, TANGGAL, QUANTITY, HPP) VALUES ('KR-A', TO_DATE(:d,'YYYY-MM-DD'), 10, 1000)",
        &[("d", &awal.format("%Y-%m-%d").to_string())],
    ).unwrap();
    c.execute_named("INSERT INTO POS_PEMBELIAN (PURCHASE_ID, NO_TRANSAKSI, TANGGAL) VALUES (1, 'BW-UJI-1', TO_DATE(:t,'YYYY-MM-DD'))", &[("t", &tgl(20))]).unwrap();
    c.execute("INSERT INTO POS_PEMBELIANDETAIL (PURCHASE_ID, KODE_BARANG, QUANTITY, HARGA_BELI) VALUES (1, 'KR-A', 4, 1000)", &[]).unwrap();
    c.execute_named("INSERT INTO POS_PENJUALAN (NO_TRANSAKSI, TANGGAL) VALUES ('J-UJI-1', TO_DATE(:t,'YYYY-MM-DD'))", &[("t", &tgl(15))]).unwrap();
    c.execute("INSERT INTO POS_PENJUALAN_DETAIL (NO_TRANSAKSI, KODE_BARANG, JUMLAH_BARANG, ISI) VALUES ('J-UJI-1', 'KR-A', 2, 1)", &[]).unwrap();
    c.execute_named("INSERT INTO POS_STOCKOPNAME (NOMOR_SO, TANGGAL, KODE_BARANG, JUMLAHSISTEM, JUMLAHFISIK, SELISIH, HPP, TOTAL) VALUES ('SO-26-000001', TO_DATE(:t,'YYYY-MM-DD'), 'KR-A', 12, 15, 3, 1000, 3000)", &[("t", &tgl(10))]).unwrap();
    c.execute_named("INSERT INTO POS_BARANGRUSAK (NOMOR_SO, TANGGAL, KODE_BARANG, JUMLAHFISIK) VALUES ('SO-26-000002', TO_DATE(:t,'YYYY-MM-DD'), 'KR-A', 1)", &[("t", &tgl(8))]).unwrap();
    c.commit().unwrap();

    let uid = user_uji(&c, "Uji Kartu");
    let mut req = persediaan::SesuaiReq { produk_id: 1, qty: d("2"), tanggal: tgl(5), alasan: "Koreksi kartu".into() };
    let r = persediaan::sesuaikan(&c, uid, "U", &uuid::Uuid::new_v4().to_string(), &req, hari).unwrap();
    req.alasan = "Balik koreksi kartu".into();
    let _rb = persediaan::balik(&c, uid, "U", &uuid::Uuid::new_v4().to_string(), r.id, &req.alasan, hari).unwrap();

    // 6 mutasi: +4 beli, -2 jual, +3 opname, -1 rusak, +2 sesuai, -2 balik. Saldo awal 10, akhir 14.
    let kartu = persediaan::kartu_stok(&c, 1, None, None, hari, 1, 100).unwrap();
    assert_eq!(kartu.items.len(), 6);
    assert_eq!(kartu.saldo_awal, d("10"));
    let saldos: Vec<Decimal> = kartu.items.iter().map(|m| m.saldo).collect();
    assert_eq!(saldos, vec![d("14"), d("12"), d("15"), d("14"), d("16"), d("14")]);
    assert_eq!(kartu.saldo_akhir, d("14"));
    // Pembuktian rekonsiliasi: saldo akhir kartu == stok sistem yang dipakai view lama.
    let stok = katalog::stok_map(&c, &["KR-A".to_string()], awal, hari).unwrap();
    assert_eq!(kartu.saldo_akhir, stok["KR-A"]);
    let jenis: Vec<&str> = kartu.items.iter().map(|m| m.jenis.as_str()).collect();
    assert_eq!(jenis, vec!["pembelian", "penjualan", "opname", "rusak", "penyesuaian", "penyesuaian_balik"]);
    assert_eq!(kartu.items[0].referensi, "BW-UJI-1");
    assert_eq!(kartu.items[2].referensi, "SO-26-000001");

    // Paginasi size 2: saldo berjalan nyambung antar halaman.
    let p1 = persediaan::kartu_stok(&c, 1, None, None, hari, 1, 2).unwrap();
    let p2 = persediaan::kartu_stok(&c, 1, None, None, hari, 2, 2).unwrap();
    let p3 = persediaan::kartu_stok(&c, 1, None, None, hari, 3, 2).unwrap();
    assert!(p1.ada_lagi && p2.ada_lagi && !p3.ada_lagi);
    assert_eq!(p1.items.last().unwrap().saldo, d("12"));
    assert_eq!(p2.items[0].saldo, d("15"));
    assert_eq!(p3.items.last().unwrap().saldo, d("14"));
    assert_eq!(p3.saldo_akhir, d("14"));

    // Rentang pertengahan: dari setelah penjualan -> saldo awal 12, 4 mutasi.
    let k2 = persediaan::kartu_stok(&c, 1, Some(hari - chrono::Duration::days(12)), None, hari, 1, 100).unwrap();
    assert_eq!((k2.saldo_awal, k2.items.len(), k2.saldo_akhir), (d("12"), 4, d("14")));
    // Sampai pertengahan: hanya beli/jual/opname -> saldo akhir 15.
    let k3 = persediaan::kartu_stok(&c, 1, None, Some(hari - chrono::Duration::days(9)), hari, 1, 100).unwrap();
    assert_eq!((k3.items.len(), k3.saldo_akhir), (3, d("15")));
    assert!(matches!(persediaan::kartu_stok(&c, 999, None, None, hari, 1, 10), Err(AppError::NotFound(_))));

    bersihkan_user(&c, uid);
}

#[test]
fn valuasi_dan_stok_minimum() {
    use pos_api::{db::{faktur, persediaan}, error::AppError};
    use rust_decimal::Decimal;

    let Some(c) = koneksi() else {
        eprintln!("dilewati: POS_TEST_ORACLE tidak diset");
        return;
    };
    let _ = tracing_subscriber::fmt().with_test_writer().try_init();
    let Some(_skema) = SkemaPos::buat(&c) else { return };
    db::migrate(&c).unwrap();
    let (hari, _) = faktur::sekarang(&c).unwrap();
    let awal = chrono::NaiveDate::from_ymd_opt(chrono::Datelike::year(&hari), 1, 1).unwrap();
    let d = |s: &str| s.parse::<Decimal>().unwrap();
    c.execute("INSERT INTO POS_KATEGORI (KODE, KATEGORI) VALUES (1, 'Minuman')", &[]).unwrap();
    c.execute("INSERT INTO POS_KATEGORI (KODE, KATEGORI) VALUES (2, 'Makanan')", &[]).unwrap();
    for (id, kode, beli, kat) in [(1, "VL-A", 1000, 1), (2, "VL-B", 0, 1), (3, "VL-C", 500, 2)] {
        c.execute_named(
            "INSERT INTO POS_PRODUCT (PRODUCTID, KODE_ITEM, BARCODE, PRODUCTNAME, SATUAN, PRICE, BELI, KATEGORI_ID, AKTIF) VALUES (:i, :k, :k, :k, 'PCS', 2000, :b, :kat, 'Y')",
            &[("i", &id), ("k", &kode), ("b", &beli), ("kat", &kat)],
        ).unwrap();
    }
    for (kode, qty) in [("VL-A", 10), ("VL-B", 4), ("VL-C", 2)] {
        c.execute_named(
            "INSERT INTO POS_STOCK (KODE_BARANG, TANGGAL, QUANTITY, HPP) VALUES (:k, TO_DATE(:d,'YYYY-MM-DD'), :q, 0)",
            &[("k", &kode), ("d", &awal.format("%Y-%m-%d").to_string()), ("q", &qty)],
        ).unwrap();
    }
    c.commit().unwrap();

    // Valuasi = saldo x BELI per kategori: Minuman 10*1000 + 4*0 = 10000 (1 tanpa HPP), Makanan 2*500 = 1000.
    let v = persediaan::valuasi(&c, None, awal, hari).unwrap();
    assert_eq!(v.total_nilai, d("11000"));
    assert_eq!((v.total_produk, v.tanpa_hpp, v.jumlah_stok_minim), (3, 1, 0));
    assert_eq!(v.per_kategori.len(), 2);
    let k1 = v.per_kategori.iter().find(|k| k.kategori == "Minuman").unwrap();
    assert_eq!((k1.jumlah_produk, k1.total_qty, k1.nilai, k1.tanpa_hpp), (2, d("14"), d("10000"), 1));
    let vf = persediaan::valuasi(&c, Some("2"), awal, hari).unwrap();
    assert_eq!((vf.total_produk, vf.total_nilai), (1, d("1000")));

    // Ambang minimum: stok == ambang TIDAK alert (strict <); stok < ambang alert.
    persediaan::set_stok_min(&c, 1, Some(d("10"))).unwrap();
    persediaan::set_stok_min(&c, 3, Some(d("5"))).unwrap();
    let (h, jumlah) = persediaan::stok_minimum(&c, None, None, awal, hari, 1, 10).unwrap();
    assert_eq!((h.items.len(), jumlah), (1, Some(1)));
    assert_eq!((h.items[0].kode.as_str(), h.items[0].stok, h.items[0].stok_min), ("VL-C", d("2"), d("5")));
    assert_eq!(persediaan::jumlah_stok_minimum(&c, awal, hari).unwrap(), 1);
    assert_eq!(persediaan::valuasi(&c, None, awal, hari).unwrap().jumlah_stok_minim, 1);
    let (h, _) = persediaan::stok_minimum(&c, Some("vl-c"), None, awal, hari, 1, 10).unwrap();
    assert_eq!(h.items.len(), 1);
    let (h, _) = persediaan::stok_minimum(&c, None, Some("1"), awal, hari, 1, 10).unwrap();
    assert_eq!(h.items.len(), 0);
    // NULL membersihkan ambang.
    persediaan::set_stok_min(&c, 3, None).unwrap();
    assert_eq!(persediaan::jumlah_stok_minimum(&c, awal, hari).unwrap(), 0);
    assert!(persediaan::set_stok_min(&c, 1, Some(d("-1"))).is_err());
    assert!(persediaan::set_stok_min(&c, 1, Some(d("1.00001"))).is_err());
    assert!(matches!(persediaan::set_stok_min(&c, 999, Some(d("1"))), Err(AppError::NotFound(_))));

    c.execute("INSERT INTO POS_PRODUCT (PRODUCTID, KODE_ITEM, BARCODE, PRODUCTNAME, BELI, AKTIF) VALUES (4, 'VL-D', '123456', 'Produk Nol', NULL, 'Y')", &[]).unwrap();
    c.execute("INSERT INTO POS_PRODUCT (PRODUCTID, KODE_ITEM, PRODUCTNAME, BELI, AKTIF) VALUES (5, 'VL-E', 'Produk Negatif', 12.5, 'Y')", &[]).unwrap();
    c.execute("INSERT INTO POS_PRODUCT (PRODUCTID, KODE_ITEM, PRODUCTNAME, BELI, AKTIF) VALUES (6, 'VL-F', 'Produk Nonaktif', 10, 'N')", &[]).unwrap();
    for (kode, qty) in [("VL-E", -3), ("VL-F", 5)] {
        c.execute_named(
            "INSERT INTO POS_STOCK (KODE_BARANG, TANGGAL, QUANTITY, HPP) VALUES (:k, TO_DATE(:d,'YYYY-MM-DD'), :q, 0)",
            &[("k", &kode), ("d", &awal.format("%Y-%m-%d").to_string()), ("q", &qty)],
        ).unwrap();
    }
    c.commit().unwrap();

    let pertama = persediaan::valuasi_item(&c, None, awal, hari, 1, 3).unwrap();
    let berikutnya = persediaan::valuasi_item(&c, None, awal, hari, 2, 3).unwrap();
    assert!(pertama.ada_lagi && !berikutnya.ada_lagi);
    let items: Vec<_> = pertama.items.into_iter().chain(berikutnya.items).collect();
    assert_eq!(items.iter().map(|i| i.id).collect::<Vec<_>>(), [5, 4, 1, 2, 3]);
    let v = persediaan::valuasi(&c, None, awal, hari).unwrap();
    assert_eq!(items.len() as i64, v.total_produk);
    assert_eq!(items.iter().map(|i| i.nilai).sum::<Decimal>(), v.total_nilai);
    assert_eq!(items.iter().filter(|i| i.tanpa_hpp).count() as i64, v.tanpa_hpp);
    let negatif = &items[0];
    assert_eq!((negatif.stok, negatif.hpp, negatif.nilai), (d("-3"), d("12.5"), d("-37.5")));
    let kosong = &items[1];
    assert_eq!((kosong.stok, kosong.hpp, kosong.nilai), (Decimal::ZERO, Decimal::ZERO, Decimal::ZERO));
    assert!(kosong.tanpa_hpp && items[3].tanpa_hpp);
    assert_eq!(kosong.kategori, "Tanpa kategori");
    for (q, id) in [(" produk negatif ", 5), ("vl-b", 2), ("123456", 4)] {
        let h = persediaan::valuasi_item(&c, Some(q), awal, hari, 1, 10).unwrap();
        assert_eq!(h.items.iter().map(|i| i.id).collect::<Vec<_>>(), [id]);
    }
    for q in ["123", "VL-F", "%"] {
        let h = persediaan::valuasi_item(&c, Some(q), awal, hari, 1, 10).unwrap();
        assert!(h.items.is_empty() && !h.ada_lagi);
    }

    bersihkan_user(&c, 0);
    c.execute("DELETE FROM MOB_STOK_SESUAI", &[]).unwrap();
    c.commit().unwrap();
}
