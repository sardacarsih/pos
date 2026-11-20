import 'models.dart';

final _polaJumlah = RegExp(r'^(?:\d+(?:\.\d{0,4})?|\.\d{1,4})$');
final _polaPecahan = RegExp(r'^(\d*)(?:\.(\d*))?$');

/// Teks desimal tanpa notasi eksponensial untuk nilai double dari JSON.
String teksDesimal(double v) {
  if (v == v.roundToDouble() && v.abs() < 1e15) return v.toStringAsFixed(0);
  final s = '$v';
  if (!s.contains('e')) return s;
  return v.toStringAsFixed(10).replaceFirst(RegExp(r'0+$'), '');
}

/// Bentuk normal (titik) dari isian user; null bila di luar 0..1 miliar atau > 4 desimal.
String? jumlahFisik(String teks) {
  final nilai = teks.trim().replaceAll(',', '.');
  if (!_polaJumlah.hasMatch(nilai)) return null;
  final jumlah = double.tryParse(nilai);
  return jumlah != null && jumlah <= 1000000000 ? nilai : null;
}

({BigInt v, int s})? _pecahan(String teks) {
  final m = _polaPecahan.firstMatch(teks);
  if (m == null) return null;
  final bulat = m.group(1) ?? '', frac = m.group(2) ?? '';
  if (bulat.isEmpty && frac.isEmpty) return null;
  return (
    v: BigInt.parse('${bulat.isEmpty ? '0' : bulat}$frac'),
    s: frac.length,
  );
}

/// Satu kolom hitung: nama satuan, faktor ke satuan dasar, dan isian petugas (teks mentah).
class KolomHitung {
  KolomHitung({required this.satuan, required this.isi, this.qty = ''});
  final String satuan;
  final double isi;
  String qty;

  bool get terisi => qty.trim().isNotEmpty;
}

typedef HitunganKirim = ({String satuan, String qty, String isi});
typedef StatusHitung = ({double? nilai, String? galat, int terisi});

/// Kolom input: satuan dasar (faktor 1) + satuan tambahan Katalog.
/// [qtyLama] (kunci nama satuan huruf besar) mempertahankan isian saat satuan dimuat ulang.
List<KolomHitung> kolomHitungan(StokItem b, {Map<String, String>? qtyLama}) {
  String lama(String nama) => qtyLama?[nama.trim().toUpperCase()] ?? '';
  return [
    KolomHitung(satuan: b.satuan, isi: 1, qty: lama(b.satuan)),
    for (final s in b.satuanTambahan)
      KolomHitung(satuan: s.satuan, isi: s.isi, qty: lama(s.satuan)),
  ];
}

/// Total fisik dalam satuan dasar dari kolom terisi; kolom kosong dilewati.
/// Semua kosong = belum dihitung (nilai null tanpa galat); nol harus eksplisit.
/// Aritmetika BigInt agar batas 4 desimal dan 1 miliar diperiksa eksak, bukan lewat double.
StatusHitung statusHitungan(List<KolomHitung> kolom) {
  final isi = kolom.where((k) => k.terisi).toList();
  if (isi.isEmpty) return (nilai: null, galat: null, terisi: 0);
  var total = BigInt.zero;
  final sepuluh = BigInt.from(10);
  for (final k in isi) {
    if (jumlahFisik(k.qty) == null) {
      return (
        nilai: null,
        galat:
            'Jumlah ${k.satuan} tidak valid: 0 sampai 1 miliar, maksimal 4 desimal, tanpa negatif.',
        terisi: isi.length,
      );
    }
    final q = _pecahan(k.qty.trim().replaceAll(',', '.'))!;
    final f = _pecahan(teksDesimal(k.isi));
    if (f == null || f.v <= BigInt.zero) {
      return (
        nilai: null,
        galat: 'Faktor konversi ${k.satuan} tidak valid.',
        terisi: isi.length,
      );
    }
    var v = q.v * f.v;
    var s = q.s + f.s;
    while (s > 4 && v % sepuluh == BigInt.zero) {
      v ~/= sepuluh;
      s--;
    }
    if (s > 4) {
      return (
        nilai: null,
        galat:
            'Hasil konversi ${k.satuan} (×${teksDesimal(k.isi)}) perlu lebih dari 4 desimal.',
        terisi: isi.length,
      );
    }
    total += v * sepuluh.pow(4 - s);
  }
  if (total > sepuluh.pow(13)) {
    return (
      nilai: null,
      galat: 'Total fisik melebihi 1 miliar.',
      terisi: isi.length,
    );
  }
  return (nilai: total.toDouble() / 1e4, galat: null, terisi: isi.length);
}

/// Payload `hitungan` untuk API: hanya kolom terisi, angka dan faktor sebagai string desimal.
List<HitunganKirim> payloadHitungan(List<KolomHitung> kolom) => [
  for (final k in kolom.where((k) => k.terisi))
    (satuan: k.satuan, qty: jumlahFisik(k.qty)!, isi: teksDesimal(k.isi)),
];

/// Ringkasan isian untuk ditinjau, mis. "2 DUS × 12 + 3 PCS".
String rincianHitungan(List<KolomHitung> kolom) => [
  for (final k in kolom.where((k) => k.terisi))
    '${jumlahFisik(k.qty)} ${k.satuan}${k.isi != 1 ? ' × ${teksDesimal(k.isi)}' : ''}',
].join(' + ');
