import 'package:qr/qr.dart';

/// QR Kartu Anggota yang identik (per modul) dengan QR yang dicetak desktop di rptKTA
/// (BackOffice/Laporan/rptKTA.Designer.cs): DevExpress QRCodeGenerator, versi 6, koreksi kesalahan H,
/// mode AlphaNumeric (default DevExpress). Mask dipilih seperti DevExpress, diukur dari 66 sampel hasil cetak:
/// penalti ISO/IEC 18004 (N1 3+(k-5), N2 3, N3 40, N4 10 per 5%) dihitung sebelum info format ditulis,
/// dengan modul gelap tetap hitam; mask penalti terendah (seri: mask terkecil).
/// Paket `qr` memilih mask dengan cara lain sehingga gambarnya berbeda walau isinya sama.
final _alnum = RegExp(r'^[0-9A-Z $%*+\-./:]+$');
final _cache = <String, QrImage>{};

QrImage qrKartu(String data) {
  final cached = _cache[data];
  if (cached != null) return cached;
  final code = QrCode(6, QrErrorCorrectLevel.H);
  // Desktop tidak bisa membuat QR AlphaNumeric untuk huruf kecil (tidak tercetak); mobile memakai Byte agar tetap tampil.
  if (_alnum.hasMatch(data)) {
    code.addAlphaNumeric(data);
  } else {
    code.addData(data);
  }
  var best = 0;
  var bestPenalti = 1 << 30;
  for (var m = 0; m < 8; m++) {
    final p = _penalti(_tanpaFormat(QrImage.withMaskPattern(code, m)));
    if (p < bestPenalti) {
      bestPenalti = p;
      best = m;
    }
  }
  final image = QrImage.withMaskPattern(code, best);
  if (_cache.length > 64) _cache.clear();
  return _cache[data] = image;
}

/// Matriks modul (true = gelap) dengan area info format dikosongkan, modul gelap tetap hitam.
List<List<bool>> _tanpaFormat(QrImage q) {
  final n = q.moduleCount;
  final m = [
    for (var r = 0; r < n; r++) [for (var c = 0; c < n; c++) q.isDark(r, c)],
  ];
  for (var i = 0; i < 9; i++) {
    m[8][i] = false;
    m[i][8] = false;
  }
  for (var i = 0; i < 8; i++) {
    m[8][n - 1 - i] = false;
    m[n - 1 - i][8] = false;
  }
  m[n - 8][8] = true; // modul gelap
  return m;
}

int _penalti(List<List<bool>> m) {
  final n = m.length;
  var s = 0;
  // N1: deret >= 5 modul sewarna (baris dan kolom)
  for (var a = 0; a < n; a++) {
    for (final h in [true, false]) {
      var run = 1;
      for (var b = 1; b < n; b++) {
        final cur = h ? m[a][b] : m[b][a];
        final prev = h ? m[a][b - 1] : m[b - 1][a];
        if (cur == prev) {
          run++;
        } else {
          if (run >= 5) s += run - 2;
          run = 1;
        }
      }
      if (run >= 5) s += run - 2;
    }
  }
  // N2: blok 2x2 sewarna
  for (var r = 0; r < n - 1; r++) {
    for (var c = 0; c < n - 1; c++) {
      final v = m[r][c];
      if (v == m[r][c + 1] && v == m[r + 1][c] && v == m[r + 1][c + 1]) s += 3;
    }
  }
  // N3: pola 1:1:3:1:1 dengan 4 modul terang di salah satu sisi (jendela 11 modul penuh di dalam simbol)
  const p1 = [true, false, true, true, true, false, true, false, false, false, false];
  const p2 = [false, false, false, false, true, false, true, true, true, false, true];
  for (var a = 0; a < n; a++) {
    for (final h in [true, false]) {
      for (var c = 0; c <= n - 11; c++) {
        var x1 = true, x2 = true;
        for (var k = 0; k < 11; k++) {
          final v = h ? m[a][c + k] : m[c + k][a];
          if (v != p1[k]) x1 = false;
          if (v != p2[k]) x2 = false;
        }
        if (x1) s += 40;
        if (x2) s += 40;
      }
    }
  }
  // N4: proporsi modul gelap menyimpang dari 50%
  var gelap = 0;
  for (final r in m) {
    for (final v in r) {
      if (v) gelap++;
    }
  }
  s += 10 * ((gelap * 100 / (n * n) - 50).abs() ~/ 5);
  return s;
}
