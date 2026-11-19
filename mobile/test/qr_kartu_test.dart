import 'dart:io';

import 'package:flutter_test/flutter_test.dart';
import 'package:kopkar_pos/core/qr_kartu.dart';

/// Golden: matriks modul 41x41 hasil render DevExpress (QRCodeGenerator versi 6, level H, AlphaNumeric; sama dengan
/// rptKTA di desktop) untuk 66 NIK. Format tiap baris: NIK, tab, heksa dari 1681 bit (baris demi baris).
void main() {
  final golden = <String, String>{};
  for (final l in File('test/data/qr_desktop_golden.txt').readAsLinesSync()) {
    if (l.isEmpty) continue;
    final i = l.lastIndexOf('\t');
    golden[l.substring(0, i)] = l.substring(i + 1);
  }

  test('golden berisi sampel yang cukup', () {
    expect(golden.length, greaterThanOrEqualTo(60));
  });

  test('QR kartu mobile sama persis (tiap modul) dengan QR cetak desktop', () {
    final gagal = <String>[];
    golden.forEach((nik, hex) {
      final bits = BigInt.parse(hex, radix: 16).toRadixString(2).padLeft(1681, '0');
      final img = qrKartu(nik);
      var beda = 0;
      for (var r = 0; r < 41; r++) {
        for (var c = 0; c < 41; c++) {
          if (img.isDark(r, c) != (bits[r * 41 + c] == '1')) beda++;
        }
      }
      if (beda != 0) gagal.add('$nik ($beda modul berbeda)');
    });
    expect(gagal, isEmpty);
  });

  test('versi 6 (41 modul) untuk semua NIK', () {
    expect(qrKartu('01.00062').moduleCount, 41);
    expect(qrKartu('A-24-061').moduleCount, 41);
  });

  test('NIK huruf kecil tidak gagal (Byte); desktop tidak mencetak QR untuk kasus ini', () {
    expect(qrKartu('ab.12345').moduleCount, 41);
  });
}
