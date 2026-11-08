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

    let nama = format!("it.{}", &uuid::Uuid::new_v4().simple().to_string()[..10]);
    let h = password::hash("sementara-123").unwrap();
    let id = user::create_user(&c, &nama, "Uji Integrasi", &h, Role::Kasir, None).unwrap();
    let u = user::find_by_username(&c, &nama).unwrap().unwrap();
    assert_eq!(
        (u.id, u.role, u.active, u.locked),
        (id, Role::Kasir, true, false)
    );
    assert!(password::verify("sementara-123", &u.hash));

    // 5 gagal berturut-turut -> terkunci sementara
    for _ in 0..5 {
        user::register_failure(&c, id).unwrap();
    }
    assert!(user::find_by_id(&c, id).unwrap().unwrap().locked);
    user::reset_failures(&c, id).unwrap();
    assert!(!user::find_by_id(&c, id).unwrap().unwrap().locked);

    c.execute("DELETE FROM MOB_USER WHERE USER_ID = :1", &[&id])
        .unwrap();
    c.commit().unwrap();
}
