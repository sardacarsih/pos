import 'dart:convert';

import 'package:flutter/services.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kopkar_pos/core/models.dart';
import 'package:kopkar_pos/features/kasir/struk_bluetooth.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();
  final faktur = Faktur.fromJson({
    'no_transaksi': 'BBM-001',
    'tanggal': '2026-11-09',
    'jam': '10:30',
    'nama_kasir': 'Siti',
    'nik': '123',
    'nama_pelanggan': 'Budi',
    'jenis_bayar': 'TUNAI',
    'bruto': 15000,
    'total': 15000,
    'uang_bayar': 20000,
    'kembalian': 5000,
    'baris': [
      {
        'nama': 'Produk',
        'qty': 1,
        'satuan': 'pcs',
        'harga': 15000,
        'total': 15000,
      },
    ],
    'jadwal': [
      {'ke': 1, 'jatuh_tempo': '2026-12-01', 'angsuran': 15000},
    ],
  });

  test('nota ESC/POS memuat identitas dan rincian transaksi', () {
    final bytes = bangunStrukBluetooth(faktur);
    final teks = ascii.decode(bytes);
    expect(bytes.sublist(0, 2), [0x1b, 0x40]);
    for (final bagian in [
      'BBM-001',
      'Kasir: Siti',
      'BUDI',
      'Produk',
      'TOTAL',
      'Rp 15.000',
      'Rp 5.000',
      'Ke-1 2026-12-01',
    ]) {
      expect(teks, contains(bagian));
    }
  });

  test(
    'layanan mengirim perangkat terpilih dan nota melalui channel',
    () async {
      const channel = MethodChannel('id.kopkar.pos/bluetooth_printer');
      final messenger =
          TestDefaultBinaryMessengerBinding.instance.defaultBinaryMessenger;
      final calls = <MethodCall>[];
      messenger.setMockMethodCallHandler(channel, (call) async {
        calls.add(call);
      if (call.method == 'list') {
          return [
            {'name': 'Printer', 'address': 'AA:BB'},
          ];
      }
        return null;
      });
      addTearDown(() => messenger.setMockMethodCallHandler(channel, null));
      const layanan = LayananPrinterBluetooth();
      final devices = await layanan.daftar();
      expect(devices.single.nama, 'Printer');
      await layanan.cetak(devices.single, faktur);
      expect(calls.last.arguments['address'], 'AA:BB');
      expect(calls.last.arguments['data'], bangunStrukBluetooth(faktur));
    },
  );
}
