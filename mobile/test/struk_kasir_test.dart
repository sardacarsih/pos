import 'dart:convert';
import 'dart:io';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kopkar_pos/core/models.dart';
import 'package:kopkar_pos/features/kasir/bayar_page.dart';
import 'package:kopkar_pos/features/kasir/struk_pdf.dart';

Faktur faktur({String nama = '', String kasir = ''}) => Faktur.fromJson({
  'no_transaksi': 'BBM-26-000001',
  'tanggal': '2026-11-08',
  'jam': '12:00',
  'nama_kasir': nama,
  'kasir': kasir,
  'nama_pelanggan': 'Anggota',
  'jenis_bayar': 'TUNAI',
});

void main() {
  final cases = [
    (nama: '  Siti Aminah  ', kasir: 'siti', expected: 'Siti Aminah'),
    (nama: '  ', kasir: '  siti  ', expected: 'siti'),
    (nama: '', kasir: '', expected: 'Tidak tersedia'),
  ];

  for (final c in cases) {
    testWidgets('identitas kasir ${c.expected} pada struk baru dan riwayat', (
      tester,
    ) async {
      for (final dariRiwayat in [false, true]) {
        final f = faktur(nama: c.nama, kasir: c.kasir);
        await tester.pumpWidget(
          ProviderScope(
            child: MaterialApp(
              home: StrukPage(faktur: f, dariRiwayat: dariRiwayat),
            ),
          ),
        );
        await tester.pump();
        expect(find.text('Kasir: ${c.expected}'), findsOneWidget);
        expect(f.identitasKasir, c.expected);
        final pdf = latin1.decode(await bangunStrukPdf(f));
        final streams = RegExp(r'stream\r?\n([\s\S]*?)\r?\nendstream')
            .allMatches(pdf)
            .map((match) {
              try {
                return latin1.decode(
                  zlib.decode(latin1.encode(match.group(1)!)),
                );
              } on FormatException {
                return '';
              }
            })
            .join('\n');
        final teksPdf = RegExp(r'\[\((.*?)\)\]TJ')
            .allMatches(streams)
            .map((match) => match.group(1))
            .join(' ');
        expect(teksPdf, contains('Kasir: ${c.expected}'));
        await tester.pumpWidget(const SizedBox());
      }
    });
  }
}
