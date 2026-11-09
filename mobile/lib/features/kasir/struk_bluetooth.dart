import 'dart:convert';
import 'dart:typed_data';

import 'package:flutter/services.dart';

import '../../core/format.dart';
import '../../core/models.dart';

class PrinterBluetooth {
  const PrinterBluetooth({required this.nama, required this.alamat});
  final String nama;
  final String alamat;
}

class LayananPrinterBluetooth {
  const LayananPrinterBluetooth();
  static const _channel = MethodChannel('id.kopkar.pos/bluetooth_printer');

  Future<List<PrinterBluetooth>> daftar() async {
    final devices = await _channel.invokeListMethod<Map<Object?, Object?>>(
      'list',
    );
    return [
      for (final device in devices ?? [])
        PrinterBluetooth(
          nama: device['name'] as String,
          alamat: device['address'] as String,
        ),
    ];
  }

  Future<void> cetak(PrinterBluetooth printer, Faktur faktur) =>
      _channel.invokeMethod<void>('print', {
        'address': printer.alamat,
        'data': bangunStrukBluetooth(faktur),
      });
}

/// ESC/POS untuk kertas 58 mm (32 kolom), dengan pemotongan teks per baris.
Uint8List bangunStrukBluetooth(Faktur f) {
  const lebar = 32;
  final out = BytesBuilder()..add([0x1b, 0x40]);
  void teks(String value) {
    // Karakter yang tidak tersedia di ASCII diganti agar tidak bergantung pada
    // code page bawaan masing-masing printer.
    final aman = value.replaceAll(RegExp(r'[^\x20-\x7e]'), '?');
    for (var start = 0; start < aman.length; start += lebar) {
      out.add(
        ascii.encode(
          aman.substring(start, (start + lebar).clamp(0, aman.length)),
        ),
      );
      out.addByte(10);
    }
    if (aman.isEmpty) out.addByte(10);
  }

  void jumlah(String label, String nilai) {
    final spasi = lebar - label.length - nilai.length;
    if (spasi < 1) {
      teks(label);
      teks(nilai);
    } else {
      teks('$label${' ' * spasi}$nilai');
    }
  }

  out.add([0x1b, 0x61, 1]);
  teks('KOPKAR - KUSUMA LESTARI');
  out.add([0x1b, 0x61, 0]);
  teks('No: ${f.no}');
  teks('${f.tanggal} ${f.jam}');
  teks('Kasir: ${f.identitasKasir}');
  teks('Pelanggan: ${f.nik}, ${f.namaPelanggan.toUpperCase()}');
  teks('Bayar: ${f.jenisBayar}${f.tenor > 1 ? ' (${f.tenor}x)' : ''}');
  teks('-' * lebar);
  for (final b in f.baris) {
    teks(b.nama);
    jumlah(
      '${formatQty(b.qty, b.satuan)} x ${rupiah(b.harga)}',
      rupiah(b.total),
    );
    if (b.potongan > 0) jumlah('  potongan', '-${rupiah(b.potongan)}');
  }
  teks('-' * lebar);
  jumlah('Bruto', rupiah(f.bruto));
  jumlah('Potongan', rupiah(f.potongan));
  out.add([0x1b, 0x45, 1]);
  jumlah('TOTAL', rupiah(f.total));
  out.add([0x1b, 0x45, 0]);
  if (f.uangBayar != null) jumlah('Bayar', rupiah(f.uangBayar!));
  if (f.kembalian != null) jumlah('Kembalian', rupiah(f.kembalian!));
  if (f.jadwal.isNotEmpty) {
    teks('-' * lebar);
    for (final j in f.jadwal) {
      jumlah('Ke-${j.ke} ${j.jatuhTempo}', rupiah(j.angsuran));
    }
  }
  out.add([0x1b, 0x61, 1]);
  teks('Terima kasih');
  out.add([10, 10, 10, 0x1b, 0x61, 0]);
  return out.takeBytes();
}
