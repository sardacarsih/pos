import 'dart:typed_data';

import 'package:pdf/pdf.dart';
import 'package:pdf/widgets.dart' as pw;

import '../../core/format.dart';
import '../../core/models.dart';

/// Struk roll 58 mm meniru isi laporan rptFakturJual desktop.
Future<Uint8List> bangunStrukPdf(Faktur f) {
  final doc = pw.Document();
  const s = 8.0;
  pw.Widget baris(String l, String v, {bool tebal = false}) => pw.Row(
        mainAxisAlignment: pw.MainAxisAlignment.spaceBetween,
        children: [
          pw.Text(l, style: pw.TextStyle(fontSize: s, fontWeight: tebal ? pw.FontWeight.bold : null)),
          pw.Text(v, style: pw.TextStyle(fontSize: s, fontWeight: tebal ? pw.FontWeight.bold : null)),
        ],
      );
  doc.addPage(pw.Page(
    pageFormat: PdfPageFormat(58 * PdfPageFormat.mm, double.infinity, marginAll: 3 * PdfPageFormat.mm),
    build: (_) => pw.Column(crossAxisAlignment: pw.CrossAxisAlignment.stretch, children: [
      pw.Center(child: pw.Text('KOPKAR - KUSUMA LESTARI', style: pw.TextStyle(fontSize: s + 1, fontWeight: pw.FontWeight.bold))),
      pw.SizedBox(height: 4),
      pw.Text('No: ${f.no}', style: const pw.TextStyle(fontSize: s)),
      pw.Text('${f.tanggal} ${f.jam}', style: const pw.TextStyle(fontSize: s)),
      pw.Text('Pelanggan: ${f.nik}, ${f.namaPelanggan.toUpperCase()}', style: const pw.TextStyle(fontSize: s)),
      pw.Text('Bayar: ${f.jenisBayar}${f.tenor > 1 ? ' (${f.tenor}x)' : ''}', style: const pw.TextStyle(fontSize: s)),
      pw.Divider(thickness: 0.5),
      for (final b in f.baris) ...[
        pw.Text(b.nama, style: const pw.TextStyle(fontSize: s)),
        baris('${b.qty} x ${rupiah(b.harga)}', rupiah(b.qty * b.harga)),
        if (b.potongan > 0) baris('  potongan', '-${rupiah(b.potongan)}'),
      ],
      pw.Divider(thickness: 0.5),
      baris('Bruto', rupiah(f.bruto)),
      baris('Potongan', rupiah(f.potongan)),
      baris('TOTAL', rupiah(f.total), tebal: true),
      if (f.uangBayar != null) baris('Bayar', rupiah(f.uangBayar!)),
      if (f.kembalian != null) baris('Kembalian', rupiah(f.kembalian!)),
      if (f.jadwal.isNotEmpty) ...[
        pw.Divider(thickness: 0.5),
        for (final j in f.jadwal) baris('Ke-${j.ke} ${j.jatuhTempo}', rupiah(j.angsuran)),
      ],
      pw.SizedBox(height: 6),
      pw.Center(child: pw.Text('Terima kasih', style: const pw.TextStyle(fontSize: s))),
    ]),
  ));
  return doc.save();
}
