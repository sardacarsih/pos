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

fn uji_http_web(
    username: &str,
    password: &str,
    connect: &str,
    akun: &[(i64, Role)],
    hari: chrono::NaiveDate,
) {
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
        registrasi_anggota_web: false,
        belanja_demo: true,
    });
    let pool = db::connect(&cfg).unwrap();
    tokio::runtime::Runtime::new().unwrap().block_on(async {
        let listener = tokio::net::TcpListener::bind(&cfg.bind).await.unwrap();
        let api = format!("http://{}/api/v1", listener.local_addr().unwrap());
        let url = format!("{api}/supplier");
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

            let saldo = format!("{api}/pembelian/B-LUNAS/saldo-awal");
            let koreksi = client.patch(&saldo).bearer_auth(&bearer)
                .header("Idempotency-Key", key())
                .json(&json!({"revisi":1,"saldo_awal":"0","referensi":"Bukti API","alasan":"Verifikasi ulang"}))
                .send().await.unwrap();
            assert_eq!(koreksi.status(), if role == Role::Admin { 200 } else { 403 });
            let bayar = format!("{api}/hutang-supplier/B-LEGACY/pembayaran");
            let dibayar = client.post(&bayar).bearer_auth(&bearer)
                .header("Idempotency-Key", key())
                .json(&json!({"tanggal":hari,"metode":"TUNAI","nominal":"0.10"}))
                .send().await.unwrap();
            assert_eq!(dibayar.status(), if role == Role::Admin { 200 } else { 403 });
            if role == Role::Admin {
                let hasil: serde_json::Value = dibayar.json().await.unwrap();
                let pembayaran_id = hasil["id"].as_str().unwrap();
                let balik = client.post(format!("{bayar}/{pembayaran_id}/balik")).bearer_auth(&bearer)
                    .header("Idempotency-Key", key()).json(&json!({"alasan":"Koreksi uji HTTP"}))
                    .send().await.unwrap();
                assert_eq!(balik.status(), 200);
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
    b::migrate_khusus(&c).unwrap();
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
        rekonsiliasi: None,
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
    // Satuan pembelian hanya satuan dasar (PCS) dan tambahan produk (BOX); stok masuk dalam satuan dasar.
    c.execute("INSERT INTO POS_SATUAN (ID,SATUAN) VALUES (999,'KRT')", &[]).unwrap();
    c.commit().unwrap();
    let baris_beli = |satuan: &str, isi: &str, qty: &str, harga: i64| {
        vec![b::BarisReq {
            product_id: 1,
            satuan: satuan.into(),
            isi: isi.into(),
            qty: qty.into(),
            harga,
            potongan: 0,
        }]
    };
    // Satuan master yang bukan satuan produk ditolak, meski ada di POS_SATUAN.
    req.baris = baris_beli("KRT", "24", "2", 24_000);
    assert!(matches!(
        b::posting(&c, &staff, &key(), &req),
        Err(AppError::Rule { kode: "SATUAN_BERUBAH", .. })
    ));
    assert_eq!(stok(), Decimal::from(36));
    let qty_hpp = |no: &str| -> (Decimal, Decimal) {
        let (q, h) = c
            .query_row_as_named::<(String, String)>(
                "SELECT TO_CHAR(QUANTITY),TO_CHAR(HARGA_BELI) FROM POS_PEMBELIANDETAIL WHERE NO_TRANSAKSI=:no",
                &[("no", &no)],
            )
            .unwrap();
        (q.parse().unwrap(), h.parse().unwrap())
    };
    // Satuan dasar: isi 1, qty apa adanya.
    req.no_faktur = "INV-PCS".into();
    req.baris = baris_beli("PCS", "1", "5", 1_000);
    let no_pcs = b::posting(&c, &staff, &key(), &req).unwrap();
    assert_eq!(stok(), Decimal::from(41));
    assert_eq!(qty_hpp(&no_pcs), (Decimal::from(5), Decimal::from(1_000)));
    // Satuan tambahan: 2 BOX x 12 = 24 PCS, harga dasar 24.000 / 12.
    req.no_faktur = "INV-BOX".into();
    req.baris = baris_beli("BOX", "12", "2", 24_000);
    let no_box = b::posting(&c, &staff, &key(), &req).unwrap();
    assert_eq!(stok(), Decimal::from(65));
    assert_eq!(qty_hpp(&no_box), (Decimal::from(24), Decimal::from(2_000)));
    let baris = &b::detail(&c, &staff, &no_box).unwrap()["baris"][0];
    assert_eq!((baris["satuan"].as_str(), baris["isi"].as_str(), baris["qty"].as_str()), (Some("BOX"), Some("12"), Some("2")));
    // Harga beli terakhir BOX tersimpan dan dikirim di referensi.
    let beli_box = b::referensi(&c).unwrap()["barang"][0]["satuan"][1]["beli"]
        .as_str()
        .unwrap()
        .parse::<Decimal>()
        .unwrap();
    assert_eq!(beli_box, Decimal::from(24_000));
    req.baris = baris_beli("BOX", "6", "2", 12_000);
    req.no_faktur = "INV-2".into();
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
    uji_rekonsiliasi(&c, koneksi, &admin, &staff, hari, &stok);
    uji_http_web(username, password, connect, &akun, hari);
}

fn uji_rekonsiliasi(
    c: &Connection,
    koneksi: impl Fn() -> Connection + Sync,
    admin: &b::Operator,
    staff: &b::Operator,
    hari: chrono::NaiveDate,
    stok: impl Fn() -> Decimal,
) {
    use b::legacy as l;
    let no = "B-LEGACY";
    let sebelumnya = stok();
    c.execute("UPDATE POS_PEMBELIAN SET TOTAL=501.25,TANGGAL=TRUNC(SYSDATE)-1 WHERE NO_TRANSAKSI='B-LEGACY'", &[]).unwrap();
    c.execute("INSERT INTO POS_PEMBELIANDETAIL (PURCHASE_ID,NO_TRANSAKSI,BARIS,KODE_BARANG,SATUAN,QUANTITY,HARGA_BELI,TOTAL)
        SELECT PURCHASE_ID,NO_TRANSAKSI,1,'P-LAMA','PCS',1,501.25,501.25 FROM POS_PEMBELIAN WHERE NO_TRANSAKSI='B-LEGACY'", &[]).unwrap();
    c.execute(
        "INSERT INTO POS_PEMBELIAN (NO_TRANSAKSI,TANGGAL,SUPPLIER_ID,TOTAL)
        VALUES ('B-LUNAS',TRUNC(SYSDATE),'S1',50)",
        &[],
    )
    .unwrap();
    c.commit().unwrap();
    let detail = |no| b::detail(c, admin, no).unwrap();
    let daftar = |q: &str, rekonsiliasi| b::Filter {
        q: Some(q.into()),
        rekonsiliasi,
        supplier: None,
        dari: None,
        sampai: None,
        page: None,
        terlambat: None,
    };
    let awal_detail = detail(no);
    assert_eq!(awal_detail["status"], "SALDO_BELUM_DILACAK");
    assert!(awal_detail["sisa"].is_null());
    assert_eq!(
        b::riwayat(c, admin, &daftar(no, Some(true))).unwrap()["items"][0]["no_transaksi"],
        no
    );
    let mut awal = l::Awal {
        purchase_id: awal_detail["purchase_id"].as_str().unwrap().into(),
        saldo_awal: "500.25".into(),
        tanggal_rekonsiliasi: hari,
        jatuh_tempo: None,
        referensi: "BUKTI-AWAL".into(),
    };
    assert!(matches!(
        l::tetapkan(c, staff, no, &key(), &awal),
        Err(AppError::Forbidden(_))
    ));
    let editor = koneksi();
    editor
        .execute(
            "UPDATE POS_PEMBELIANDETAIL SET SATUAN='BOX' WHERE NO_TRANSAKSI='B-LEGACY'",
            &[],
        )
        .unwrap();
    let ulang = key();
    assert!(matches!(
        l::tetapkan(c, admin, no, &ulang, &awal),
        Err(AppError::Conflict(_))
    ));
    editor.rollback().unwrap();
    assert_eq!(l::tetapkan(c, admin, no, &ulang, &awal).unwrap(), "1");
    assert_eq!(l::tetapkan(c, admin, no, &ulang, &awal).unwrap(), "1");
    assert!(matches!(
        l::tetapkan(c, admin, no, &key(), &awal),
        Err(AppError::Conflict(_))
    ));
    assert_eq!(detail(no)["status"], "TERBUKA");
    assert_eq!(detail(no)["sisa"], "500.25");
    assert!(
        b::riwayat(c, admin, &daftar(no, Some(true))).unwrap()["items"]
            .as_array()
            .unwrap()
            .is_empty()
    );
    for sql in [
        "UPDATE POS_PEMBELIAN SET TOTAL=0 WHERE NO_TRANSAKSI='B-LEGACY'",
        "DELETE FROM POS_PEMBELIAN WHERE NO_TRANSAKSI='B-LEGACY'",
        "DELETE FROM POS_PEMBELIANDETAIL WHERE NO_TRANSAKSI='B-LEGACY'",
        "UPDATE POS_PEMBELIANDETAIL SET SATUAN='BOX' WHERE NO_TRANSAKSI='B-LEGACY'",
    ] {
        assert_eq!(
            editor.execute(sql, &[]).unwrap_err().oci_code(),
            Some(20082)
        );
        editor.rollback().unwrap();
    }
    let bayar = |nominal: &str, tanggal| l::BayarLama {
        nominal: nominal.into(),
        tanggal,
        metode: b::Metode::Transfer,
        referensi: Some("BUKTI-BAYAR".into()),
    };
    assert!(matches!(
        l::bayar(
            c,
            admin,
            no,
            &key(),
            &bayar("0.15", hari.pred_opt().unwrap())
        ),
        Err(AppError::Validation(_))
    ));
    assert!(matches!(
        l::bayar(c, admin, no, &key(), &bayar("0.001", hari)),
        Err(AppError::Validation(_))
    ));
    let id = std::thread::scope(|scope| {
        let catat = || l::bayar(&koneksi(), admin, no, &key(), &bayar("300.10", hari));
        let a = scope.spawn(catat);
        let b = scope.spawn(catat);
        match (a.join().unwrap(), b.join().unwrap()) {
            (
                Ok(id),
                Err(AppError::Rule {
                    kode: "SALDO_BERUBAH",
                    ..
                }),
            )
            | (
                Err(AppError::Rule {
                    kode: "SALDO_BERUBAH",
                    ..
                }),
                Ok(id),
            ) => id,
            hasil => panic!("Hasil pembayaran paralel: {hasil:?}"),
        }
    });
    assert_eq!(detail(no)["sudah_bayar"], "300.1");
    let mut koreksi = l::Koreksi {
        revisi: 1,
        saldo_awal: "299.99".into(),
        jatuh_tempo: hari.pred_opt(),
        alasan: "Koreksi bukti".into(),
        referensi: "BUKTI-KOREKSI".into(),
    };
    assert!(matches!(
        l::koreksi(c, admin, no, &key(), &koreksi),
        Err(AppError::Rule {
            kode: "SALDO_BERUBAH",
            ..
        })
    ));
    koreksi.saldo_awal = "499.99".into();
    let revisi_key = key();
    assert_eq!(
        l::koreksi(c, admin, no, &revisi_key, &koreksi).unwrap(),
        "2"
    );
    assert_eq!(
        l::koreksi(c, admin, no, &revisi_key, &koreksi).unwrap(),
        "2"
    );
    assert!(matches!(
        l::koreksi(c, admin, no, &key(), &koreksi),
        Err(AppError::Conflict(_))
    ));
    assert_eq!(detail(no)["revisi_saldo"].as_array().unwrap().len(), 2);
    assert_eq!(detail(no)["status"], "TERLAMBAT");
    assert_eq!(detail(no)["sisa"], "199.89");
    let daftar_hutang = b::hutang(c, admin, &daftar(no, None)).unwrap();
    assert_eq!(daftar_hutang["items"][0]["sisa"], "199.89");
    assert_eq!(daftar_hutang["supplier"][0]["saldo"], "199.89");
    let lunas_key = key();
    let pelunasan = bayar("199.89", hari);
    let id_lunas = l::bayar(c, admin, no, &lunas_key, &pelunasan).unwrap();
    assert_eq!(
        l::bayar(c, admin, no, &lunas_key, &pelunasan).unwrap(),
        id_lunas
    );
    assert_eq!(detail(no)["status"], "LUNAS");
    let balik_key = key();
    let alasan = b::Alasan {
        alasan: "Salah transaksi".into(),
    };
    let id: i64 = id.parse().unwrap();
    let dibalik = l::balik(c, admin, no, id, &balik_key, &alasan).unwrap();
    assert_eq!(
        l::balik(c, admin, no, id, &balik_key, &alasan).unwrap(),
        dibalik
    );
    assert!(matches!(
        l::balik(c, admin, no, id, &key(), &alasan),
        Err(AppError::Conflict(_))
    ));
    assert_eq!(detail(no)["sisa"], "300.1");
    assert_eq!(detail(no)["pembayaran"][0]["dibalik"], true);
    awal.purchase_id = detail("B-LUNAS")["purchase_id"].as_str().unwrap().into();
    awal.saldo_awal = "0".into();
    l::tetapkan(c, admin, "B-LUNAS", &key(), &awal).unwrap();
    assert_eq!(detail("B-LUNAS")["status"], "LUNAS");
    assert!(
        b::hutang(c, admin, &daftar("B-LUNAS", None)).unwrap()["items"]
            .as_array()
            .unwrap()
            .is_empty()
    );
    assert_eq!(stok(), sebelumnya);
}
