//! Gunakan skema Oracle kosong dan sekali pakai, POS_TEST_ORACLE_KAS=user/password@connect.
use oracle::Connection;
use pos_api::{
    auth::Role,
    db::{self, faktur, kas, pembelian as b, user},
    error::AppError,
};
use uuid::Uuid;

fn key() -> String {
    Uuid::new_v4().to_string()
}

#[test]
fn kas_oracle_akun_manual_transfer_dan_tarik() {
    let Ok(dsn) = std::env::var("POS_TEST_ORACLE_KAS") else {
        eprintln!("POS_TEST_ORACLE_KAS belum diset; uji Oracle dilewati");
        return;
    };
    let (credentials, connect) = dsn.split_once('@').expect("format skema uji");
    let (username, password) = credentials.split_once('/').expect("login skema uji");
    let c = Connection::connect(username, password, connect).expect("koneksi skema uji");
    assert_eq!(
        c.query_row_as::<i64>("SELECT COUNT(*) FROM USER_TABLES", &[]).unwrap(),
        0,
        "Uji kas hanya boleh mengisi skema kosong yang bisa dibuang"
    );
    for ddl in [
        "CREATE TABLE POS_PERIODE (PERIODE NUMBER PRIMARY KEY,BDARI VARCHAR2(12),BSAMPAI VARCHAR2(12),
            R1DARI VARCHAR2(12),R1SAMPAI VARCHAR2(12),R2DARI VARCHAR2(12),R2SAMPAI VARCHAR2(12),REMISE1 CHAR(1),REMISE2 CHAR(1))",
        "CREATE TABLE POS_PENJUALAN (NO_TRANSAKSI VARCHAR2(30),TANGGAL DATE,TOTAL NUMBER,JENIS_BAYAR VARCHAR2(10))",
        "CREATE TABLE FIN_PINJAMAN (NO_TRANSAKSI VARCHAR2(30),TANGGAL DATE,PINJAMAN NUMBER,NAMA_PELANGGAN VARCHAR2(100))",
    ] {
        c.execute(ddl, &[]).unwrap();
    }
    db::migrate(&c).unwrap();
    db::migrate(&c).unwrap();
    c.execute(
        "INSERT INTO POS_PERIODE (PERIODE,BDARI,BSAMPAI,REMISE1,REMISE2)
         VALUES (EXTRACT(YEAR FROM SYSDATE)*100+EXTRACT(MONTH FROM SYSDATE),
         TO_CHAR(TRUNC(SYSDATE,'MM'),'DD-MON-YYYY','NLS_DATE_LANGUAGE=ENGLISH'),
         TO_CHAR(LAST_DAY(SYSDATE),'DD-MON-YYYY','NLS_DATE_LANGUAGE=ENGLISH'),'N','N')",
        &[],
    )
    .unwrap();
    c.commit().unwrap();
    let admin_id = user::create_user(&c, "admin.kas", "Admin", "unused", Role::Admin, None).unwrap();
    user::set_password(&c, admin_id, "unused", false).unwrap();
    let u = b::operator(&c, admin_id, true).unwrap();
    let hari = faktur::sekarang(&c).unwrap().0;

    let akun = |kode: &str, jenis: &str, saldo: i64, def: bool| kas::AkunReq {
        kode: kode.into(),
        nama: format!("Akun {kode}"),
        jenis: jenis.into(),
        no_rekening: None,
        atas_nama: None,
        saldo_awal: saldo,
        tgl_saldo_awal: hari,
        default_untuk: def.then(|| jenis.into()),
        aktif: true,
    };
    let id = |v: serde_json::Value| v["id"].as_i64().unwrap();
    let k = id(kas::simpan_akun(&c, None, &akun("KAS1", "KAS", 1_000, true)).unwrap());
    let bk = id(kas::simpan_akun(&c, None, &akun("BCA", "BANK", 0, true)).unwrap());
    assert!(matches!(kas::simpan_akun(&c, None, &akun("kas1", "KAS", 0, false)), Err(AppError::Conflict(_))));
    let saldo = |akun: i64| {
        kas::buku(&c, &kas::FilterBuku { akun, dari: hari, sampai: hari }).unwrap()["saldo_akhir"]
            .as_i64()
            .unwrap()
    };

    // Manual + idempotensi.
    let m = kas::ManualReq { akun_id: k, tanggal: hari, arah: "K".into(), nominal: 200, keterangan: "ATK".into() };
    let kunci = key();
    let mid = kas::manual(&c, &u, &kunci, &m).unwrap();
    assert_eq!(kas::manual(&c, &u, &kunci, &m).unwrap(), mid);
    assert_eq!(saldo(k), 800);

    // Transfer: total saldo tetap; pembatalan membalik kedua sisi.
    let t = kas::TransferReq { dari_akun: k, ke_akun: bk, tanggal: hari, nominal: 300, keterangan: "Setor".into() };
    kas::transfer(&c, &u, &key(), &t).unwrap();
    assert_eq!((saldo(k), saldo(bk)), (500, 300));
    let trf_id: i64 = c
        .query_row_as("SELECT MIN(ID) FROM MOB_KAS_MUTASI WHERE SUMBER='TRANSFER'", &[])
        .unwrap();
    kas::batal(&c, &u, trf_id, &key(), &kas::Alasan { alasan: "salah".into() }).unwrap();
    assert_eq!((saldo(k), saldo(bk)), (800, 0));
    assert!(matches!(
        kas::batal(&c, &u, trf_id, &key(), &kas::Alasan { alasan: "lagi".into() }),
        Err(AppError::Conflict(_))
    ));

    // Tarik: penjualan tunai per hari dan pinjaman; idempoten; koreksi saat sumber berubah.
    for sql in [
        "INSERT INTO POS_PENJUALAN VALUES ('J1',TRUNC(SYSDATE),5000,'TUNAI')",
        "INSERT INTO POS_PENJUALAN VALUES ('J2',TRUNC(SYSDATE),2500,'TUNAI')",
        "INSERT INTO POS_PENJUALAN VALUES ('J3',TRUNC(SYSDATE),9999,'KREDIT')",
        "INSERT INTO FIN_PINJAMAN VALUES ('P1',TRUNC(SYSDATE),1000,'Budi')",
    ] {
        c.execute(sql, &[]).unwrap();
    }
    c.commit().unwrap();
    let tarik = kas::TarikReq { dari: hari, sampai: hari };
    let h = kas::tarik(&c, &u, &tarik).unwrap();
    assert_eq!((h["penjualan"].as_u64(), h["pinjaman"].as_u64()), (Some(1), Some(1)));
    assert_eq!(saldo(k), 800 + 7_500 - 1_000);
    assert_eq!(kas::tarik(&c, &u, &tarik).unwrap()["penjualan"], 0);
    c.execute("DELETE FROM POS_PENJUALAN WHERE NO_TRANSAKSI='J2'", &[]).unwrap();
    c.execute("DELETE FROM FIN_PINJAMAN", &[]).unwrap();
    c.commit().unwrap();
    kas::tarik(&c, &u, &tarik).unwrap();
    assert_eq!(saldo(k), 800 + 5_000);
    assert!(matches!(
        kas::batal(&c, &u, c.query_row_as::<i64>("SELECT MIN(ID) FROM MOB_KAS_MUTASI WHERE SUMBER='PENJUALAN'", &[]).unwrap(),
            &key(), &kas::Alasan { alasan: "x".into() }),
        Err(AppError::Rule { kode: "MUTASI_OTOMATIS", .. })
    ));
    assert_eq!(kas::daftar_akun(&c).unwrap()["akun"].as_array().unwrap().len(), 2);
}
