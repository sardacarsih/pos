/// Format tampilan rupiah dari integer (tanpa parsing teks berformat lokal).
String rupiah(int v) {
  final neg = v < 0;
  final s = v.abs().toString();
  final b = StringBuffer();
  for (var i = 0; i < s.length; i++) {
    if (i > 0 && (s.length - i) % 3 == 0) b.write('.');
    b.write(s[i]);
  }
  return '${neg ? '-' : ''}Rp ${b.toString()}';
}

/// Ambil hanya digit dari input pengguna; kosong -> 0. Tidak mengurai pemisah ribuan/desimal.
int digitsToInt(String s) {
  final d = s.replaceAll(RegExp(r'[^0-9]'), '');
  if (d.isEmpty) return 0;
  return int.tryParse(d) ?? 0;
}

String formatQty(num quantity, String satuan) {
  final centi = (quantity * 100).round();
  final unit = satuan.trim();
  final liter = ['l', 'ltr', 'liter', 'litre'].contains(unit.toLowerCase());
  if (liter)
    return '${centi ~/ 100},${(centi % 100).toString().padLeft(2, '0')} $unit';
  final whole = centi % 100 == 0
      ? '${centi ~/ 100}'
      : '${centi ~/ 100},${(centi % 100).toString().padLeft(2, '0')}';
  return '$whole${unit.isEmpty ? '' : ' $unit'}';
}

/// Converts a rupiah amount to the nearest centiliter (0.01 L) without float math.
int nominalKeCenti(int nominal, int hargaPerLiter) {
  if (nominal <= 0 || hargaPerLiter <= 0) return 0;
  return (nominal * 100 + hargaPerLiter ~/ 2) ~/ hargaPerLiter;
}

String namaPeriode(int periode) {
  const bulan = [
    'Januari',
    'Februari',
    'Maret',
    'April',
    'Mei',
    'Juni',
    'Juli',
    'Agustus',
    'September',
    'Oktober',
    'November',
    'Desember',
  ];
  final y = periode ~/ 100;
  final m = periode % 100;
  return '${bulan[(m - 1).clamp(0, 11)]} $y';
}

/// "2026-11-01" + "2026-11-30" -> "01 Nov 2026 – 30 Nov 2026" (rentang tanggal periode dari server).
String rentangTanggal(String dari, String sampai) {
  const b = ['Jan', 'Feb', 'Mar', 'Apr', 'Mei', 'Jun', 'Jul', 'Agu', 'Sep', 'Okt', 'Nov', 'Des'];
  String f(String s) {
    final p = s.split('-');
    final m = p.length == 3 ? int.tryParse(p[1]) : null;
    return m == null || m < 1 || m > 12 ? s : '${p[2]} ${b[m - 1]} ${p[0]}';
  }
  return '${f(dari)} – ${f(sampai)}';
}

/// Geser periode yyyyMM sebanyak [delta] bulan.
int geserPeriode(int periode, int delta) {
  final idx = (periode ~/ 100) * 12 + (periode % 100 - 1) + delta;
  return (idx ~/ 12) * 100 + (idx % 12 + 1);
}

/// Judul status periode dari kode server (null = terbuka).
String judulPeriode(String? kode) => switch (kode) {
  null => 'Periode terbuka',
  'PERIODE_TUTUP' => 'Periode tertutup',
  'PERIODE_TIDAK_ADA' => 'Periode belum diatur',
  _ => 'Periode belum valid',
};

const hubungiAdminPeriode = 'Hubungi admin untuk mengatur periode.';
