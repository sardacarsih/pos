//! Pembuatan akun anggota massal + ekspor daftar password sementara ke .xlsx (`pos-api user-add-anggota`).

use oracle::Connection;
use rust_xlsxwriter::{Format, Workbook};

use crate::{
    auth::{Role, password},
    db::{katalog::{self, Pelanggan}, user},
    error::AppResult,
    routes::admin::{username_dari_nik, username_valid},
};

pub struct Dibuat {
    pub pel: Pelanggan,
    pub username: String,
    pub password: String,
}

#[derive(Default)]
pub struct Hasil {
    pub dibuat: Vec<Dibuat>,
    pub dilewati: Vec<(Pelanggan, String)>,
}

/// Username akun untuk anggota ini, atau alasan dilewati. Murni (tanpa DB).
pub fn username_untuk(p: &Pelanggan, default_nik: &str) -> Result<String, String> {
    let nik = p.nik.trim();
    if nik == default_nik {
        return Err("NIK pelanggan umum (tunai)".into());
    }
    if nik.chars().count() > 30 {
        return Err("NIK lebih dari 30 karakter".into());
    }
    let u = username_dari_nik(nik);
    if !username_valid(&u) {
        return Err(format!("username dari NIK tidak valid ({u:?})"));
    }
    Ok(u)
}

/// Buat akun untuk semua anggota aktif tanpa akun. `dry_run`: hanya hitung, tidak menulis DB.
/// Kegagalan satu anggota (mis. username kembar) dicatat di `dilewati`, batch jalan terus.
pub fn jalankan(conn: &Connection, default_nik: &str, dry_run: bool) -> AppResult<Hasil> {
    let mut h = Hasil::default();
    for p in katalog::semua_belum_akun(conn)? {
        let username = match username_untuk(&p, default_nik) {
            Ok(u) => u,
            Err(alasan) => {
                h.dilewati.push((p, alasan));
                continue;
            }
        };
        let pw = password::temp_password();
        if !dry_run {
            let r = password::hash(&pw)
                .and_then(|hs| user::create_user(conn, &username, &p.nama, &hs, Role::Anggota, Some(&p.nik)));
            if let Err(e) = r {
                h.dilewati.push((p, format!("gagal dibuat: {e}")));
                continue;
            }
        }
        h.dibuat.push(Dibuat { pel: p, username, password: pw });
    }
    Ok(h)
}

pub fn tulis_xlsx(path: &str, h: &Hasil) -> Result<(), String> {
    let mut wb = Workbook::new();
    let tebal = Format::new().set_bold();
    let teks = Format::new().set_num_format("@");
    let e = |x: rust_xlsxwriter::XlsxError| x.to_string();

    let ws = wb.add_worksheet();
    ws.set_name("Akun Anggota").map_err(e)?;
    let kepala = ["No", "NIK", "Nama", "Kode Unit Kerja", "Nama Unit Kerja", "Username", "Password Sementara"];
    for (c, t) in kepala.iter().enumerate() {
        ws.write_with_format(0, c as u16, *t, &tebal).map_err(e)?;
    }
    for (i, d) in h.dibuat.iter().enumerate() {
        let r = i as u32 + 1;
        ws.write(r, 0, r).map_err(e)?;
        ws.write_with_format(r, 1, &d.pel.nik, &teks).map_err(e)?;
        ws.write(r, 2, &d.pel.nama).map_err(e)?;
        ws.write_with_format(r, 3, &d.pel.unit_kerja, &teks).map_err(e)?;
        ws.write(r, 4, &d.pel.unit_nama).map_err(e)?;
        ws.write_with_format(r, 5, &d.username, &teks).map_err(e)?;
        ws.write(r, 6, &d.password).map_err(e)?;
    }
    ws.autofilter(0, 0, h.dibuat.len() as u32, 6).map_err(e)?;
    ws.set_freeze_panes(1, 0).map_err(e)?;
    ws.autofit();

    let ws = wb.add_worksheet();
    ws.set_name("Dilewati").map_err(e)?;
    for (c, t) in ["NIK", "Nama", "Kode Unit Kerja", "Nama Unit Kerja", "Alasan"].iter().enumerate() {
        ws.write_with_format(0, c as u16, *t, &tebal).map_err(e)?;
    }
    for (i, (p, alasan)) in h.dilewati.iter().enumerate() {
        let r = i as u32 + 1;
        ws.write_with_format(r, 0, &p.nik, &teks).map_err(e)?;
        ws.write(r, 1, &p.nama).map_err(e)?;
        ws.write_with_format(r, 2, &p.unit_kerja, &teks).map_err(e)?;
        ws.write(r, 3, &p.unit_nama).map_err(e)?;
        ws.write(r, 4, alasan).map_err(e)?;
    }
    ws.set_freeze_panes(1, 0).map_err(e)?;
    ws.autofit();

    wb.save(path).map_err(e)
}

#[cfg(test)]
mod tests {
    use super::*;
    use rust_decimal::Decimal;

    fn pel(nik: &str) -> Pelanggan {
        Pelanggan {
            id: 1,
            nik: nik.into(),
            nama: "Budi".into(),
            unit_kerja: "01".into(),
            unit_nama: "Kebun".into(),
            status: "A".into(),
            limit: Decimal::ZERO,
        }
    }

    #[test]
    fn username_dan_alasan_dilewati() {
        assert_eq!(username_untuk(&pel(" 0012 345 "), "UMUM").unwrap(), "0012.345");
        assert!(username_untuk(&pel("UMUM"), "UMUM").is_err());
        assert!(username_untuk(&pel("12"), "UMUM").is_err());
        assert!(username_untuk(&pel(&"1".repeat(31)), "UMUM").is_err());
    }

    #[test]
    fn xlsx_tertulis() {
        let h = Hasil {
            dibuat: vec![Dibuat { pel: pel("0012"), username: "0012".into(), password: "ABCD2345".into() }],
            dilewati: vec![(pel("12"), "x".into())],
        };
        let f = std::env::temp_dir().join("anggota_massal_test.xlsx");
        tulis_xlsx(f.to_str().unwrap(), &h).unwrap();
        assert!(std::fs::metadata(&f).unwrap().len() > 0);
        let _ = std::fs::remove_file(f);
    }
}
