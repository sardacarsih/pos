//! Gunakan skema Oracle kosong dan sekali pakai, POS_TEST_ORACLE_PEMBELIAN=user/password@connect.
use chrono::Datelike;
use oracle::Connection;
use pos_api::{
    auth::Role,
    db::{self, faktur, katalog, pembelian as b, user},
    error::AppError,
};
use rust_decimal::Decimal;
use uuid::Uuid;

fn key() -> String {
    Uuid::new_v4().to_string()
}

#[test]
fn pembelian_oracle_stok_jurnal_idempotensi_dan_koreksi() {
    let Ok(dsn) = std::env::var("POS_TEST_ORACLE_PEMBELIAN") else {
        eprintln!("POS_TEST_ORACLE_PEMBELIAN belum diset; uji Oracle dilewati");
        return;
    };
    let (credentials, connect) = dsn.split_once('@').expect("format skema uji");
    let (username, password) = credentials.split_once('/').expect("login skema uji");
    let koneksi = || Connection::connect(username, password, connect).expect("koneksi skema uji");
    let c = koneksi();
    assert_eq!(
        c.query_row_as::<i64>("SELECT COUNT(*) FROM USER_TABLES", &[])
            .unwrap(),
        0,
        "Uji pembelian hanya boleh mengisi skema kosong yang bisa dibuang"
    );
    for ddl in [
        "CREATE TABLE POS_PENJUALAN (ID_PENJUALAN NUMBER,NO_TRANSAKSI VARCHAR2(30),NIK VARCHAR2(30),TANGGAL DATE)",
        "CREATE TABLE POS_PENJUALAN_DETAIL (NO_TRANSAKSI VARCHAR2(30),KODE_BARANG VARCHAR2(30),JUMLAH_BARANG NUMBER,ISI NUMBER)",
        "CREATE TABLE POS_STOCK (KODE_BARANG VARCHAR2(30),TANGGAL DATE,QUANTITY NUMBER)",
        "CREATE TABLE POS_BARANGRUSAK (KODE_BARANG VARCHAR2(30),TANGGAL DATE,JUMLAHFISIK NUMBER)",
        "CREATE TABLE POS_STOCKOPNAME (KODE_BARANG VARCHAR2(30),TANGGAL DATE,SELISIH NUMBER)",
        "CREATE TABLE POS_PERIODE (PERIODE NUMBER PRIMARY KEY,BDARI VARCHAR2(12),BSAMPAI VARCHAR2(12),
            R1DARI VARCHAR2(12),R1SAMPAI VARCHAR2(12),R2DARI VARCHAR2(12),R2SAMPAI VARCHAR2(12),REMISE1 CHAR(1),REMISE2 CHAR(1))",
        "CREATE TABLE POS_PRODUCT (PRODUCTID NUMBER PRIMARY KEY,KODE_ITEM VARCHAR2(30),PRODUCTNAME VARCHAR2(100),
            SATUAN VARCHAR2(20),BARCODE VARCHAR2(50),BELI NUMBER,AKTIF CHAR(1))",
        "CREATE TABLE POS_PRODUCT_SATUAN (PRODUCT_ID NUMBER,SATUAN VARCHAR2(20),BARCODE VARCHAR2(50),ISI NUMBER,HARGA NUMBER)",
        "CREATE TABLE POS_SUPPLIER (KODE VARCHAR2(30) PRIMARY KEY,NAMA VARCHAR2(100),AKTIF CHAR(1))",
        "CREATE TABLE POS_PEMBELIAN (PURCHASE_ID NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,NO_TRANSAKSI VARCHAR2(20) UNIQUE,
            TANGGAL DATE,SUPPLIER_ID VARCHAR2(30),BRUTO NUMBER,POTONGAN NUMBER,TOTAL NUMBER,TERMIN NUMBER,USERID VARCHAR2(50),NAMA_USER VARCHAR2(100))",
        "CREATE TABLE POS_PEMBELIANDETAIL (PURCHASE_ID NUMBER REFERENCES POS_PEMBELIAN(PURCHASE_ID),
            NO_TRANSAKSI VARCHAR2(20),BARIS NUMBER,PRODUCT_ID NUMBER,KODE_BARANG VARCHAR2(30),NAMA_BARANG VARCHAR2(100),
            SATUAN VARCHAR2(20),QUANTITY NUMBER(18,3),HARGA_BELI NUMBER,BRUTO NUMBER,POTONGAN NUMBER,TOTAL NUMBER)",
    ] {
        c.execute(ddl, &[]).unwrap();
    }
    db::migrate(&c).unwrap();
    db::migrate(&c).unwrap();
    for sql in [
        "INSERT INTO POS_SUPPLIER VALUES ('S1','Supplier Uji','Y')",
        "INSERT INTO POS_PRODUCT VALUES (1,'P1','Barang Uji','PCS','0001',1000,'Y')",
        "INSERT INTO POS_PRODUCT_SATUAN VALUES (1,'BOX','00002',12,15000)",
        "INSERT INTO POS_PERIODE (PERIODE,BDARI,BSAMPAI,REMISE1,REMISE2)
         VALUES (EXTRACT(YEAR FROM SYSDATE)*100+EXTRACT(MONTH FROM SYSDATE),
         TO_CHAR(TRUNC(SYSDATE,'MM'),'DD-MON-YYYY','NLS_DATE_LANGUAGE=ENGLISH'),
         TO_CHAR(LAST_DAY(SYSDATE),'DD-MON-YYYY','NLS_DATE_LANGUAGE=ENGLISH'),'N','N')",
        "INSERT INTO POS_PEMBELIAN (NO_TRANSAKSI,TANGGAL,SUPPLIER_ID,BRUTO,POTONGAN,TOTAL)
         VALUES ('B-LEGACY',TRUNC(SYSDATE),'S1',500,0,500)",
        "UPDATE POS_PEMBELIAN SET TOTAL=501 WHERE NO_TRANSAKSI='B-LEGACY'",
    ] {
        c.execute(sql, &[]).unwrap();
    }
    c.commit().unwrap();
    let admin_id =
        user::create_user(&c, "admin.uji", "Admin", "unused", Role::Admin, None).unwrap();
    let staff_id = user::create_user(
        &c,
        "petugas.uji",
        "Petugas",
        "unused",
        Role::Pembelian,
        None,
    )
    .unwrap();
    for id in [admin_id, staff_id] {
        user::set_password(&c, id, "unused", false).unwrap();
    }
    let admin = b::operator(&c, admin_id, true).unwrap();
    let staff = b::operator(&c, staff_id, false).unwrap();
    assert!(matches!(
        b::operator(&c, staff_id, true),
        Err(AppError::Forbidden(_))
    ));
    let hari = faktur::sekarang(&c).unwrap().0;
    let lewat = hari.pred_opt().unwrap();
    let awal = hari.with_ordinal(1).unwrap();
    let stok = || katalog::stok_map(&c, &["P1".into()], awal, hari).unwrap()["P1"];
    assert_eq!(
        b::referensi(&c).unwrap()["barang"][0]["satuan"][1]["barcode"],
        "00002"
    );

    let mut req = b::Posting {
        supplier_id: "S1".into(),
        no_faktur: "INV-1".into(),
        tanggal_faktur: lewat,
        tanggal_terima: hari,
        jenis: b::Jenis::Tempo,
        jatuh_tempo: Some(lewat),
        uang_muka: 2_000,
        metode: Some(b::Metode::Transfer),
        referensi: Some("DP-1".into()),
        baris: vec![b::BarisReq {
            product_id: 1,
            satuan: "BOX".into(),
            isi: "12".into(),
            qty: "2".into(),
            harga: 12_000,
            potongan: 4_000,
        }],
    };
    let k = key();
    let no = b::posting(&c, &staff, &k, &req).unwrap();
    assert!(no.starts_with("BW-"));
    assert_eq!(b::posting(&c, &staff, &k, &req).unwrap(), no);
    assert_eq!(stok(), Decimal::from(24));
    assert!(matches!(
        b::posting(&c, &admin, &key(), &req),
        Err(AppError::Rule {
            kode: "FAKTUR_DUPLIKAT",
            ..
        })
    ));
    req.baris[0].qty = "3".into();
    assert!(matches!(
        b::posting(&c, &staff, &k, &req),
        Err(AppError::Conflict(_))
    ));
    assert_eq!(
        c.execute_named(
            "DELETE FROM POS_PEMBELIANDETAIL WHERE NO_TRANSAKSI=:no",
            &[("no", &no)]
        )
        .unwrap_err()
        .oci_code(),
        Some(20081)
    );
    assert!(matches!(
        b::detail(&c, &staff, "B-LEGACY"),
        Err(AppError::NotFound(_))
    ));
    assert_eq!(
        b::detail(&c, &admin, "B-LEGACY").unwrap()["status"],
        "SALDO_BELUM_DILACAK"
    );
    let filter = b::Filter {
        q: Some("INV-1".into()),
        supplier: Some("S1".into()),
        dari: Some(hari),
        sampai: Some(hari),
        page: Some(1),
        terlambat: Some(true),
    };
    assert_eq!(
        b::riwayat(&c, &staff, &filter).unwrap()["items"][0]["no_transaksi"],
        no
    );
    let hutang = b::hutang(&c, &admin, &filter).unwrap();
    assert_eq!(hutang["items"][0]["status"], "TERLAMBAT");
    assert_eq!(hutang["supplier"][0]["saldo"], "18000");

    std::thread::scope(|scope| {
        let bayar = || {
            b::bayar(
                &koneksi(),
                &admin,
                &no,
                &key(),
                &b::Bayar {
                    tanggal: hari,
                    metode: b::Metode::Tunai,
                    referensi: None,
                    nominal: 10_000,
                },
            )
        };
        let a = scope.spawn(bayar);
        let b = scope.spawn(bayar);
        let hasil = [a.join().unwrap(), b.join().unwrap()];
        assert_eq!(hasil.iter().filter(|r| r.is_ok()).count(), 1);
        assert!(hasil.iter().any(|r| matches!(
            r,
            Err(AppError::Rule {
                kode: "SALDO_BERUBAH",
                ..
            })
        )));
    });
    let detail = b::detail(&c, &admin, &no).unwrap();
    assert_eq!(detail["sisa"], 8_000);
    let lunas = b::Bayar {
        tanggal: hari,
        metode: b::Metode::Transfer,
        referensi: Some("LUNAS".into()),
        nominal: 8_000,
    };
    let k_lunas = key();
    assert_eq!(
        b::bayar(&c, &admin, &no, &k_lunas, &lunas).unwrap(),
        b::bayar(&c, &admin, &no, &k_lunas, &lunas).unwrap()
    );
    assert_eq!(b::detail(&c, &admin, &no).unwrap()["status"], "LUNAS");
    let alasan = b::Alasan {
        alasan: "Koreksi uji".into(),
    };
    assert!(matches!(
        b::batal(&c, &admin, &no, &key(), &alasan),
        Err(AppError::Conflict(_))
    ));
    for p in b::detail(&c, &admin, &no).unwrap()["pembayaran"]
        .as_array()
        .unwrap()
    {
        b::balik(&c, &admin, &no, p["id"].as_i64().unwrap(), &key(), &alasan).unwrap();
    }
    c.execute("UPDATE POS_PERIODE SET REMISE1='Y', REMISE2='Y'", &[])
        .unwrap();
    c.commit().unwrap();
    assert!(matches!(
        b::batal(&c, &admin, &no, &key(), &alasan),
        Err(AppError::Rule {
            kode: "PERIODE_TUTUP",
            ..
        })
    ));
    c.execute("UPDATE POS_PERIODE SET REMISE1='N', REMISE2='N'", &[])
        .unwrap();
    c.commit().unwrap();
    b::batal(&c, &admin, &no, &key(), &alasan).unwrap();
    assert_eq!(stok(), Decimal::ZERO);
    let batal = b::detail(&c, &admin, &no).unwrap();
    assert_eq!(batal["status"], "BATAL");
    assert_eq!(batal["baris"][0]["qty"], "2");
    req.jenis = b::Jenis::Tunai;
    req.jatuh_tempo = None;
    req.uang_muka = 0;
    assert_eq!(
        b::detail(&c, &admin, &b::posting(&c, &admin, &key(), &req).unwrap()).unwrap()["status"],
        "LUNAS"
    );
    assert_eq!(stok(), Decimal::from(36));
    req.no_faktur = "INV-2".into();
    req.baris[0].isi = "6".into();
    assert!(matches!(
        b::posting(&c, &staff, &key(), &req),
        Err(AppError::Rule {
            kode: "SATUAN_BERUBAH",
            ..
        })
    ));
    c.execute(
        "INSERT INTO POS_STOCKOPNAME VALUES ('P1',TRUNC(SYSDATE),0)",
        &[],
    )
    .unwrap();
    c.commit().unwrap();
    assert!(matches!(
        b::posting(&c, &staff, &key(), &req),
        Err(AppError::Rule {
            kode: "BENTROK_OPNAME",
            ..
        })
    ));
}
