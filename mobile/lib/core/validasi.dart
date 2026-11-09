/// Aturan sama dengan server (rust-api/src/routes/admin.rs `validasi_buat`) agar kesalahan terlihat sebelum dikirim.
final _pola = RegExp(r'^[a-z0-9._-]+$');
final _terlarang = RegExp(r'[^a-z0-9._-]+');

/// Pesan galat username (huruf besar dinormalkan ke kecil seperti di server); null = valid.
String? galatUsername(String v) {
  final u = v.trim().toLowerCase();
  if (u.isEmpty) return 'Username wajib diisi';
  if (u.length < 3) return 'Username minimal 3 karakter';
  if (u.length > 50) return 'Username maksimal 50 karakter';
  if (!_pola.hasMatch(u)) {
    return 'Tanpa spasi; hanya huruf, angka, titik, _ dan -';
  }
  return null;
}

/// Nama wajib, maksimal 100 karakter (bukan byte); null = valid.
String? galatNama(String v) {
  final n = v.trim();
  if (n.isEmpty) return 'Nama wajib diisi';
  if (n.runes.length > 100) return 'Nama maksimal 100 karakter';
  return null;
}

/// Username awal dari NIK anggota: huruf kecil, karakter terlarang (spasi, '/', dll) diganti titik, maks 50.
String usernameDariNik(String nik) {
  final u = nik.trim().toLowerCase().replaceAll(_terlarang, '.');
  return u.length > 50 ? u.substring(0, 50) : u;
}
