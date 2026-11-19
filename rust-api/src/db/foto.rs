//! Foto produk (tabel MOB_PRODUK_FOTO). Admin mengunggah; kasir/admin mengambil lewat endpoint ber-auth.
use oracle::Connection;

use super::{katalog, opt};
use crate::error::{AppError, AppResult};

/// Batas ukuran unggahan (byte). Klien mengecilkan gambar dulu; ini pagar sisi server.
pub const MAKS_BYTE: usize = 1_000_000;

/// Sisi minimum/maksimum (piksel) dan toleransi beda lebar-tinggi (10%): foto katalog persegi.
pub const MIN_SISI: usize = 200;
pub const MAKS_SISI: usize = 2048;

pub struct Foto {
    pub content_type: String,
    pub data: Vec<u8>,
}

/// Jenis gambar dari isi file (magic bytes), bukan dari header klien. Hanya PNG dan JPEG.
pub fn jenis_gambar(data: &[u8]) -> Option<&'static str> {
    if data.starts_with(&[0x89, b'P', b'N', b'G', 0x0D, 0x0A, 0x1A, 0x0A]) {
        Some("image/png")
    } else if data.starts_with(&[0xFF, 0xD8, 0xFF]) {
        Some("image/jpeg")
    } else {
        None
    }
}

/// File lengkap (tidak terpotong): JPEG berakhir EOI (FF D9), PNG berakhir chunk IEND.
fn lengkap(data: &[u8], jenis: &str) -> bool {
    if jenis == "image/jpeg" {
        // Beberapa kamera menambah byte 0 di ujung file.
        let mut n = data.len();
        while n > 0 && data[n - 1] == 0 {
            n -= 1;
        }
        n >= 2 && data[n - 2] == 0xFF && data[n - 1] == 0xD9
    } else {
        data.ends_with(&[0, 0, 0, 0, b'I', b'E', b'N', b'D', 0xAE, 0x42, 0x60, 0x82])
    }
}

/// Validasi foto sebelum disimpan: ukuran file, jenis dari isi, file lengkap, dimensi terbaca,
/// sisi 200-2048 px, dan hampir persegi (selisih lebar-tinggi maks 10%). Mengembalikan content-type.
pub fn validasi_foto(data: &[u8]) -> AppResult<&'static str> {
    if data.is_empty() {
        return Err(AppError::validation("File foto kosong"));
    }
    if data.len() > MAKS_BYTE {
        return Err(AppError::validation("Foto terlalu besar (maks 1 MB)"));
    }
    let jenis = jenis_gambar(data)
        .ok_or_else(|| AppError::validation("Foto harus berformat PNG atau JPEG"))?;
    if !lengkap(data, jenis) {
        return Err(AppError::validation("File foto rusak atau terpotong"));
    }
    let d = imagesize::blob_size(data)
        .map_err(|_| AppError::validation("Foto tidak dapat dibaca; coba foto lain"))?;
    let (w, h) = (d.width, d.height);
    if w < MIN_SISI || h < MIN_SISI {
        return Err(AppError::validation(format!(
            "Foto terlalu kecil ({w}x{h}); minimal {MIN_SISI}x{MIN_SISI} piksel"
        )));
    }
    if w > MAKS_SISI || h > MAKS_SISI {
        return Err(AppError::validation(format!(
            "Foto terlalu besar ({w}x{h}); maksimal {MAKS_SISI}x{MAKS_SISI} piksel"
        )));
    }
    if w.abs_diff(h) * 10 > w.max(h) {
        return Err(AppError::validation(format!(
            "Foto harus persegi (lebar dan tinggi hampir sama); ini {w}x{h}"
        )));
    }
    Ok(jenis)
}

pub fn simpan(conn: &Connection, product_id: i64, data: &[u8]) -> AppResult<i64> {
    let jenis = validasi_foto(data)?;
    katalog::produk_admin_by_id(conn, product_id)?
        .ok_or_else(|| AppError::NotFound("Produk tidak ditemukan".into()))?;
    let versi = chrono::Utc::now().timestamp();
    let n = conn.execute_named(
        "UPDATE MOB_PRODUK_FOTO SET CONTENT_TYPE = :ct, DATA = :d, VERSI = :v WHERE PRODUCT_ID = :id",
        &[("ct", &jenis), ("d", &data), ("v", &versi), ("id", &product_id)],
    )?;
    if n.row_count()? == 0 {
        conn.execute_named(
            "INSERT INTO MOB_PRODUK_FOTO (PRODUCT_ID, CONTENT_TYPE, DATA, VERSI) VALUES (:id, :ct, :d, :v)",
            &[("id", &product_id), ("ct", &jenis), ("d", &data), ("v", &versi)],
        )?;
    }
    conn.commit()?;
    Ok(versi)
}

pub fn ambil(conn: &Connection, product_id: i64) -> AppResult<Option<Foto>> {
    match opt(conn.query_row_named(
        "SELECT CONTENT_TYPE, DATA FROM MOB_PRODUK_FOTO WHERE PRODUCT_ID = :id",
        &[("id", &product_id)],
    ))? {
        Some(r) => Ok(Some(Foto {
            content_type: r.get(0)?,
            data: r.get(1)?,
        })),
        None => Ok(None),
    }
}

pub fn hapus(conn: &Connection, product_id: i64) -> AppResult<bool> {
    let n = conn.execute_named(
        "DELETE FROM MOB_PRODUK_FOTO WHERE PRODUCT_ID = :id",
        &[("id", &product_id)],
    )?;
    conn.commit()?;
    Ok(n.row_count()? > 0)
}

#[cfg(test)]
mod tests {
    use super::*;

    fn png(w: u32, h: u32, lengkap: bool) -> Vec<u8> {
        let mut v = vec![
            0x89, b'P', b'N', b'G', 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 13, b'I', b'H', b'D', b'R',
        ];
        v.extend(w.to_be_bytes());
        v.extend(h.to_be_bytes());
        v.extend([8, 2, 0, 0, 0, 0, 0, 0, 0]); // bit depth, color, dll + CRC (tidak diperiksa)
        if lengkap {
            v.extend([0, 0, 0, 0, b'I', b'E', b'N', b'D', 0xAE, 0x42, 0x60, 0x82]);
        }
        v
    }

    fn jpeg(w: u16, h: u16, lengkap: bool) -> Vec<u8> {
        let mut v = vec![0xFF, 0xD8, 0xFF, 0xC0, 0, 0x11, 8];
        v.extend(h.to_be_bytes());
        v.extend(w.to_be_bytes());
        v.extend([3, 1, 0x22, 0, 2, 0x11, 1, 3, 0x11, 1]);
        if lengkap {
            v.extend([0xFF, 0xD9]);
        }
        v
    }

    #[test]
    fn foto_persegi_valid_lolos() {
        assert_eq!(validasi_foto(&png(600, 600, true)).unwrap(), "image/png");
        assert_eq!(validasi_foto(&jpeg(600, 590, true)).unwrap(), "image/jpeg");
    }

    #[test]
    fn foto_ditolak_bila_tidak_valid() {
        let kasus: Vec<(Vec<u8>, &str)> = vec![
            (vec![], "kosong"),
            (b"GIF89a".to_vec(), "PNG atau JPEG"),
            (png(600, 600, false), "terpotong"),
            (jpeg(600, 600, false), "terpotong"),
            (png(150, 150, true), "terlalu kecil"),
            (png(3000, 3000, true), "terlalu besar"),
            (png(800, 400, true), "persegi"),
            (jpeg(400, 800, true), "persegi"),
        ];
        for (data, pesan) in kasus {
            let e = validasi_foto(&data).expect_err(pesan).to_string();
            assert!(e.contains(pesan), "{pesan}: {e}");
        }
    }

    #[test]
    fn foto_lebih_dari_batas_byte_ditolak() {
        let mut besar = png(600, 600, true);
        besar.resize(MAKS_BYTE + 1, 0);
        assert!(
            validasi_foto(&besar)
                .unwrap_err()
                .to_string()
                .contains("1 MB")
        );
    }

    #[test]
    fn jenis_dari_magic_bytes() {
        assert_eq!(
            jenis_gambar(&[0xFF, 0xD8, 0xFF, 0xE0, 0, 0]),
            Some("image/jpeg")
        );
        assert_eq!(
            jenis_gambar(&[0x89, b'P', b'N', b'G', 0x0D, 0x0A, 0x1A, 0x0A, 0]),
            Some("image/png")
        );
        assert_eq!(jenis_gambar(b"GIF89a"), None);
        assert_eq!(jenis_gambar(b"<svg>"), None);
        assert_eq!(jenis_gambar(&[]), None);
    }
}
