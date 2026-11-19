//! Gunakan skema Oracle kosong dan sekali pakai, POS_TEST_ORACLE_PEMBELIAN=user/password@connect.
use std::{sync::Arc, time::Duration};

use chrono::Datelike;
use oracle::Connection;
use pos_api::{
    auth::{Role, throttle::IpLimiter, token},
    config::Config,
    db::{self, faktur, katalog, pembelian as b, supplier, user},
    error::AppError,
    routes::{self, AppState},
};
use rust_decimal::Decimal;
use serde_json::json;
use uuid::Uuid;

fn key() -> String {
    Uuid::new_v4().to_string()
}

fn uji_http_supplier(username: &str, password: &str, connect: &str, akun: &[(i64, Role)]) {
    let cfg = Arc::new(Config {
        bind: "127.0.0.1:0".into(),
        db_user: username.into(),
        db_password: password.into(),
        db_connect: connect.into(),
        pool_max: 3,
        jwt_secret: "rahasia-khusus-pengujian-supplier-lokal".into(),
        default_nik: "NIK-UJI".into(),
        trust_proxy: false,
        default_max_tenor: 12,
        belanja_demo: true,
    });
    let pool = db::connect(&cfg).unwrap();
    tokio::runtime::Runtime::new().unwrap().block_on(async {
        let listener = tokio::net::TcpListener::bind(&cfg.bind).await.unwrap();
        let url = format!("http://{}/api/v1/supplier", listener.local_addr().unwrap());
        let app = routes::router(AppState {
            cfg: cfg.clone(),
            pool,
            limiter: Arc::new(IpLimiter::new(20, Duration::from_secs(60))),
        });
        let server = tokio::spawn(async move { axum::serve(listener, app).await.unwrap() });
        let client = reqwest::Client::builder()
            .no_proxy()
            .timeout(Duration::from_secs(5))
            .build()
            .unwrap();
        assert_eq!(client.get(&url).send().await.unwrap().status(), 401);

        for &(id, role) in akun {
            let bearer = token::issue(
                &cfg.jwt_secret,
                token::Claims {
                    sub: id,
                    usr: "uji".into(),
                    nama: "Uji".into(),
                    role,
                    nik: None,
                    ganti: false,
                    iat: 0,
                    exp: 0,
                },
                chrono::Utc::now().timestamp(),
            )
            .unwrap();
            let boleh = matches!(role, Role::Admin | Role::Pembelian);
            let daftar = client.get(&url).bearer_auth(&bearer).send().await.unwrap();
            assert_eq!(daftar.status(), if boleh { 200 } else { 403 });

            let kode = format!("UJI%{}", role.as_str());
            let buat = client
                .post(&url)
                .bearer_auth(&bearer)
                .json(&json!({"kode": kode, "nama": "Supplier HTTP"}))
                .send()
                .await
                .unwrap();
            assert_eq!(buat.status(), if boleh { 201 } else { 403 });
            let rute = format!("{url}/{}", kode.replace('%', "%25"));
            let ubah = client
                .patch(&rute)
                .bearer_auth(&bearer)
                .json(&json!({"aktif": false}))
                .send()
                .await
                .unwrap();
            assert_eq!(ubah.status(), if boleh { 200 } else { 403 });
            if boleh {
                let supplier: serde_json::Value = ubah.json().await.unwrap();
                assert_eq!(supplier["kode"], kode);
                assert_eq!(supplier["aktif"], false);
            }
        }
        server.abort();
        let _ = server.await;
    });
}

#[test]
fn pembelian_oracle_stok_jurnal_idempotensi_dan_koreksi() {
    let _ = tracing_subscriber::fmt().with_test_writer().try_init();
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
        "CREATE TABLE POS_SUPPLIER (KODE VARCHAR2(20 BYTE) PRIMARY KEY,NAMA VARCHAR2(50 BYTE),AKTIF VARCHAR2(1 BYTE))",
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
    let mut akun = vec![(admin_id, Role::Admin), (staff_id, Role::Pembelian)];
    for (username, role) in [("kasir.uji", Role::Kasir), ("anggota.uji", Role::Anggota)] {
        let nik = (role == Role::Anggota).then_some("NIK-UJI");
        let id = user::create_user(&c, username, "Uji", "unused", role, nik).unwrap();
        user::set_password(&c, id, "unused", false).unwrap();
        assert!(matches!(
            b::operator(&c, id, false),
            Err(AppError::Forbidden(_))
        ));
        akun.push((id, role));
    }
    user::set_active(&c, staff_id, false).unwrap();
    assert!(matches!(
        b::operator(&c, staff_id, false),
        Err(AppError::Unauthorized)
    ));
    user::set_active(&c, staff_id, true).unwrap();
    let baru = supplier::Baru {
        kode: " uji_% ".into(),
        nama: " Toko Uji ".into(),
    };
    let dibuat = supplier::buat(&c, &baru).unwrap();
    assert_eq!(
        (dibuat.kode.as_str(), dibuat.nama.as_deref(), dibuat.aktif),
        ("UJI_%", Some("TOKO UJI"), true)
    );
    let status = |aktif| supplier::Ubah {
        nama: None,
        aktif: Some(aktif),
    };
    let filter_supplier = |aktif| supplier::Filter {
        q: Some("_%".into()),
        aktif: Some(aktif),
        page: Some(1),
    };
    assert_eq!(
        supplier::daftar(&c, &filter_supplier(true))
            .unwrap()
            .items
            .len(),
        1
    );
    let ganti_nama = supplier::Ubah {
        nama: Some(" nama baru ".into()),
        aktif: None,
    };
    let diubah = supplier::ubah(&c, "UJI_%", &ganti_nama).unwrap();
    assert_eq!(diubah.nama.as_deref(), Some("NAMA BARU"));
    assert!(!supplier::ubah(&c, "UJI_%", &status(false)).unwrap().aktif);
    assert!(matches!(
        supplier::buat(&c, &baru),
        Err(AppError::Conflict(_))
    ));
    assert!(
        supplier::daftar(&c, &filter_supplier(true))
            .unwrap()
            .items
            .is_empty()
    );
    assert_eq!(
        supplier::daftar(&c, &filter_supplier(false)).unwrap().items[0].kode,
        "UJI_%"
    );
    assert!(supplier::ubah(&c, "UJI_%", &status(true)).unwrap().aktif);
    assert!(matches!(
        supplier::ubah(&c, "TIDAK_ADA", &status(true)),
        Err(AppError::NotFound(_))
    ));
    let dari_backoffice: (String, String) = koneksi()
        .query_row_as(
            "SELECT NAMA,AKTIF FROM POS_SUPPLIER WHERE KODE='UJI_%'",
            &[],
        )
        .unwrap();
    assert_eq!(dari_backoffice, ("NAMA BARU".into(), "Y".into()));
    for i in 1..=30 {
        supplier::buat(
            &c,
            &supplier::Baru {
                kode: format!("PAGE{i:02}"),
                nama: "PAGINASI".into(),
            },
        )
        .unwrap();
    }
    let halaman_1 = supplier::daftar(&c, &supplier::Filter::default()).unwrap();
    let halaman_2 = supplier::daftar(
        &c,
        &supplier::Filter {
            page: Some(2),
            ..Default::default()
        },
    )
    .unwrap();
    assert_eq!(halaman_1.items.len(), 30);
    assert!(halaman_1.ada_lagi);
    assert_eq!(halaman_2.items.len(), 2);
    assert!(!halaman_2.ada_lagi);
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
    supplier::ubah(&c, "S1", &ganti_nama).unwrap();
    supplier::ubah(&c, "S1", &status(false)).unwrap();
    assert!(
        b::referensi(&c).unwrap()["supplier"]
            .as_array()
            .unwrap()
            .iter()
            .all(|s| s["kode"] != "S1")
    );
    assert_eq!(
        b::detail(&c, &admin, &no).unwrap()["supplier_nama"],
        "Supplier Uji"
    );
    assert_eq!(
        b::detail(&c, &admin, "B-LEGACY").unwrap()["supplier_nama"],
        "NAMA BARU"
    );
    assert!(matches!(
        b::posting(&c, &staff, &key(), &req),
        Err(AppError::Rule {
            kode: "SUPPLIER_BERUBAH",
            ..
        })
    ));
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
    supplier::ubah(&c, "S1", &status(true)).unwrap();
    assert!(
        b::referensi(&c).unwrap()["supplier"]
            .as_array()
            .unwrap()
            .iter()
            .any(|s| s["kode"] == "S1")
    );
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
    uji_http_supplier(username, password, connect, &akun);
}
