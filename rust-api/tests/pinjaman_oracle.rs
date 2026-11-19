//! Gunakan skema Oracle kosong dan sekali pakai, POS_TEST_ORACLE_PINJAMAN=user/password@connect.
use oracle::Connection;
use pos_api::{
    auth::Role,
    db::{self, pembelian as b, pinjaman as p, user},
    error::AppError,
};
use uuid::Uuid;

fn key() -> String {
    Uuid::new_v4().to_string()
}

fn kode(e: AppError) -> &'static str {
    match e {
        AppError::Rule { kode, .. } => kode,
        e => panic!("bukan error aturan: {e:?}"),
    }
}

#[test]
fn pinjaman_oracle_alur_antrian_dan_pencairan() {
    let Ok(dsn) = std::env::var("POS_TEST_ORACLE_PINJAMAN") else {
        eprintln!("POS_TEST_ORACLE_PINJAMAN belum diset; uji Oracle dilewati");
        return;
    };
    let (credentials, connect) = dsn.split_once('@').expect("format skema uji");
    let (username, password) = credentials.split_once('/').expect("login skema uji");
    let c = Connection::connect(username, password, connect).expect("koneksi skema uji");
    assert_eq!(
        c.query_row_as::<i64>("SELECT COUNT(*) FROM USER_TABLES", &[]).unwrap(),
        0,
        "Uji pinjaman hanya boleh mengisi skema kosong yang bisa dibuang"
    );
    for ddl in [
        "CREATE TABLE POS_PERIODE (PERIODE NUMBER PRIMARY KEY,BDARI VARCHAR2(12),BSAMPAI VARCHAR2(12),
            R1DARI VARCHAR2(12),R1SAMPAI VARCHAR2(12),R2DARI VARCHAR2(12),R2SAMPAI VARCHAR2(12),REMISE1 CHAR(1),REMISE2 CHAR(1))",
        "CREATE TABLE POS_PENJUALAN (NO_TRANSAKSI VARCHAR2(30),TANGGAL DATE,TOTAL NUMBER,JENIS_BAYAR VARCHAR2(10))",
        "CREATE TABLE FIN_ANGGOTA (ID_PELANGGAN NUMBER,NIK VARCHAR2(30),NAMA_PELANGGAN VARCHAR2(100),STATUS VARCHAR2(20),
            UNIT_KERJA VARCHAR2(20),AKTIF CHAR(1),ANGGOTA CHAR(1))",
        "CREATE TABLE FIN_SETTINGS (CONFIG VARCHAR2(50),JUMLAH NUMBER)",
        "CREATE TABLE NOMOR_TRANSAKSI (KODE VARCHAR2(30),NOMOR VARCHAR2(30))",
        "CREATE TABLE FIN_PINJAMAN (NO_TRANSAKSI VARCHAR2(30) PRIMARY KEY,TANGGAL DATE,KASIR VARCHAR2(50),ID_PELANGGAN NUMBER,
            NIK VARCHAR2(30),NAMA_PELANGGAN VARCHAR2(100),STATUS VARCHAR2(20),UNIT_KERJA VARCHAR2(20),PINJAMAN NUMBER,
            TENOR NUMBER,BUNGA NUMBER,ANGSURAN NUMBER,KETERANGAN VARCHAR2(200),ISLUNAS CHAR(1) DEFAULT 'T')",
        "CREATE TABLE FIN_PINJAMAN_DETAIL (PERIODE NUMBER,NO_TRANSAKSI VARCHAR2(30),TANGGALJATUHTEMPO DATE,ANGSURANKE NUMBER,
            SALDOAWAL NUMBER,POKOK NUMBER,BUNGA NUMBER,ANGSURAN NUMBER,SALDOAKHIR NUMBER,ISTAGIH CHAR(1) DEFAULT 'T')",
    ] {
        c.execute(ddl, &[]).unwrap();
    }
    db::migrate(&c).unwrap();
    db::migrate(&c).unwrap();
    for sql in [
        "INSERT INTO POS_PERIODE (PERIODE,BDARI,BSAMPAI,REMISE1,REMISE2)
         VALUES (EXTRACT(YEAR FROM SYSDATE)*100+EXTRACT(MONTH FROM SYSDATE),
         TO_CHAR(TRUNC(SYSDATE,'MM'),'DD-MON-YYYY','NLS_DATE_LANGUAGE=ENGLISH'),
         TO_CHAR(LAST_DAY(SYSDATE),'DD-MON-YYYY','NLS_DATE_LANGUAGE=ENGLISH'),'N','N')",
        "INSERT INTO FIN_ANGGOTA VALUES (1,'N1','Ani','BULANAN','U1','Y','Y')",
        "INSERT INTO FIN_ANGGOTA VALUES (2,'N2','Budi','BULANAN','U1','Y','Y')",
        "INSERT INTO FIN_ANGGOTA VALUES (3,'N3','Cici','BULANAN','U1','T','Y')",
        "INSERT INTO FIN_ANGGOTA VALUES (4,'N4','Dodi','BULANAN','U1','Y','Y')",
        "INSERT INTO FIN_SETTINGS VALUES ('BUNGA_EFEKTIF',1.6)",
        "INSERT INTO FIN_SETTINGS VALUES ('MAXANGSURAN',12)",
        "INSERT INTO FIN_SETTINGS VALUES ('MAX_PINJAMAN',5000000)",
        // Nomor dari BackOffice yang belum tercatat di NOMOR_TRANSAKSI.
        "INSERT INTO NOMOR_TRANSAKSI VALUES ('PINJAMAN','P-' || TO_CHAR(SYSDATE,'YY') || '-000003')",
        "INSERT INTO FIN_PINJAMAN (NO_TRANSAKSI,TANGGAL,NIK,PINJAMAN,ISLUNAS) VALUES ('P-' || TO_CHAR(SYSDATE,'YY') || '-000005',
            TRUNC(SYSDATE),'LAMA',1000000,'T')",
    ] {
        c.execute(sql, &[]).unwrap();
    }
    c.commit().unwrap();

    let buat = |nama: &str, role: Role, nik: Option<&str>| {
        let id = user::create_user(&c, nama, nama, "unused", role, nik).unwrap();
        user::set_password(&c, id, "unused", false).unwrap();
        id
    };
    let ang = |id: i64| b::Operator { id, username: "a".into(), nama: "a".into(), role: Role::Anggota };
    let (a1, a2) = (ang(buat("ani", Role::Anggota, Some("N1"))), ang(buat("budi", Role::Anggota, Some("N2"))));
    let ver = b::operator(&c, buat("ver", Role::Admin, None), true).unwrap();
    let ketua = b::operator(&c, buat("ketua", Role::Admin, None), true).unwrap();
    let bend = b::operator(&c, buat("bend", Role::Admin, None), true).unwrap();
    for (u, t) in [(&ver, p::Tahap::Verifikasi), (&ketua, p::Tahap::Setuju), (&bend, p::Tahap::Cair)] {
        p::simpan_hak(&c, &p::HakReq { user_id: u.id, tahap: vec![t, p::Tahap::Verifikasi] }).unwrap();
    }

    // Pengajuan: kelayakan.
    let aju = |jumlah, tenor| p::AjuReq { jumlah, tenor, keperluan: "Renovasi".into() };
    assert_eq!(kode(p::ajukan(&c, &a1, "N1", &key(), &aju(6_000_000, 6)).unwrap_err()), "MELEBIHI_PLAFON_ANGGOTA");
    assert_eq!(kode(p::ajukan(&c, &a1, "N1", &key(), &aju(1_000_000, 13)).unwrap_err()), "TENOR");
    assert_eq!(kode(p::ajukan(&c, &a1, "N3", &key(), &aju(1_000_000, 6)).unwrap_err()), "ANGGOTA_TIDAK_AKTIF");
    let k = key();
    let id1: i64 = p::ajukan(&c, &a1, "N1", &k, &aju(3_000_000, 6)).unwrap().parse().unwrap();
    assert_eq!(p::ajukan(&c, &a1, "N1", &k, &aju(3_000_000, 6)).unwrap(), id1.to_string(), "idempoten");
    assert_eq!(kode(p::ajukan(&c, &a1, "N1", &key(), &aju(1_000_000, 6)).unwrap_err()), "PENGAJUAN_BERJALAN");
    let id2: i64 = p::ajukan(&c, &a2, "N2", &key(), &aju(2_000_000, 3)).unwrap().parse().unwrap();

    let jalan = |u: &b::Operator, id, aksi| p::proses(&c, u, id, aksi, &key(), &p::ProsesReq { catatan: None });
    use p::Aksi::*;
    assert_eq!(kode(jalan(&ketua, id1, Setujui).unwrap_err()), "STATUS");
    jalan(&ver, id1, Verifikasi).unwrap();
    assert_eq!(kode(jalan(&ver, id1, Setujui).unwrap_err()), "HAK_TIDAK_ADA");
    jalan(&ketua, id1, Setujui).unwrap();
    jalan(&ver, id2, Verifikasi).unwrap();
    jalan(&ketua, id2, Setujui).unwrap();

    // Antrian dana: plafon belum diatur, bukan kepala, dana tidak cukup.
    assert_eq!(kode(jalan(&bend, id1, Cairkan).unwrap_err()), "PLAFON_BELUM_DIATUR");
    let periode = p::antrian(&c).unwrap()["periode"].as_i64().unwrap() as i32;
    p::simpan_plafon(&c, &bend, periode, &p::PlafonReq { plafon: 5_000_000 }).unwrap();
    assert_eq!(kode(jalan(&bend, id2, Cairkan).unwrap_err()), "BUKAN_KEPALA_ANTRIAN");
    assert_eq!(kode(jalan(&ketua, id1, Cairkan).unwrap_err()), "HAK_TIDAK_ADA");
    let no = jalan(&bend, id1, Cairkan).unwrap();
    let tahun = &no[2..4];
    assert_eq!(no, format!("P-{tahun}-000006"), "melewati nomor BackOffice tertinggi");
    let detail: i64 = c
        .query_row_as_named("SELECT COUNT(*) FROM FIN_PINJAMAN_DETAIL WHERE NO_TRANSAKSI=:n", &[("n", &no)])
        .unwrap();
    assert_eq!(detail, 6);
    // Sisa 5jt - 1jt (lama) - 3jt = 1jt < 2jt.
    assert_eq!(kode(jalan(&bend, id2, Cairkan).unwrap_err()), "DANA_TIDAK_CUKUP");
    p::simpan_plafon(&c, &bend, periode, &p::PlafonReq { plafon: 6_000_000 }).unwrap();
    assert_eq!(jalan(&bend, id2, Cairkan).unwrap(), format!("P-{tahun}-000007"));

    // Pinjaman aktif memblok pengajuan baru; status anggota terlihat.
    assert_eq!(kode(p::ajukan(&c, &a1, "N1", &key(), &aju(1_000_000, 6)).unwrap_err()), "PINJAMAN_AKTIF");
    let saya = p::saya(&c, "N1").unwrap();
    assert_eq!(saya["pengajuan"][0]["status"], "DICAIRKAN");
    assert_eq!(saya["pinjaman_aktif"].as_array().unwrap().len(), 1);

    // Pengurus yang memverifikasi tidak boleh menyetujui; tolak wajib alasan.
    let a4 = ang(buat("dodi", Role::Anggota, Some("N4")));
    let id4: i64 = p::ajukan(&c, &a4, "N4", &key(), &aju(1_000_000, 2)).unwrap().parse().unwrap();
    jalan(&ketua, id4, Verifikasi).unwrap();
    assert_eq!(kode(jalan(&ketua, id4, Setujui).unwrap_err()), "TAHAP_SAMA_USER");
    assert!(matches!(jalan(&ketua, id4, Tolak), Err(AppError::Validation(_))));
    p::proses(&c, &ketua, id4, Tolak, &key(), &p::ProsesReq { catatan: Some("Dokumen kurang".into()) }).unwrap();
    assert_eq!(p::saya(&c, "N4").unwrap()["pengajuan"][0]["status"], "DITOLAK");
}
