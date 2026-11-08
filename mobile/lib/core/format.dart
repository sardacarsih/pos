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

String namaPeriode(int periode) {
  const bulan = [
    'Januari', 'Februari', 'Maret', 'April', 'Mei', 'Juni',
    'Juli', 'Agustus', 'September', 'Oktober', 'November', 'Desember'
  ];
  final y = periode ~/ 100;
  final m = periode % 100;
  return '${bulan[(m - 1).clamp(0, 11)]} $y';
}

/// Geser periode yyyyMM sebanyak [delta] bulan.
int geserPeriode(int periode, int delta) {
  final idx = (periode ~/ 100) * 12 + (periode % 100 - 1) + delta;
  return (idx ~/ 12) * 100 + (idx % 12 + 1);
}
