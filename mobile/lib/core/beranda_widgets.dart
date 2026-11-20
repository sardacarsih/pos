import 'dart:math' as math;

import 'package:flutter/material.dart';

import 'format.dart';
import 'models.dart';
import 'widgets.dart' show LoadingKopkar;

// ---------------------------------------------------------------- logika murni (diuji di test/beranda_test.dart)

enum StatusLimit { takDibatasi, aman, hampir, terlampaui }

/// Limit <= 0 = tidak dibatasi; >= 80% hampir habis; > 100% terlampaui.
StatusLimit statusLimit(int belanja, int limit) {
  if (limit <= 0) return StatusLimit.takDibatasi;
  final p = belanja / limit;
  if (p > 1) return StatusLimit.terlampaui;
  return p >= 0.8 ? StatusLimit.hampir : StatusLimit.aman;
}

/// Persen terpakai 0..100 untuk bar (tidak melebihi penuh).
int persenTerpakai(int belanja, int limit) =>
    limit <= 0 ? 0 : ((belanja / limit) * 100).round().clamp(0, 100);

enum ArahDelta { naik, turun, tetap, tidakAda }

class Delta {
  const Delta(this.arah, this.persen);
  final ArahDelta arah;
  final int persen;
}

/// Selisih persen terhadap pembanding; pembanding 0 = tidak ada (tidak menampilkan angka palsu).
Delta hitungDelta(int sekarang, int pembanding) {
  if (pembanding <= 0) return const Delta(ArahDelta.tidakAda, 0);
  final p = ((sekarang - pembanding) / pembanding * 100).round();
  return Delta(
    p > 0
        ? ArahDelta.naik
        : p < 0
        ? ArahDelta.turun
        : ArahDelta.tetap,
    p,
  );
}

String teksDelta(Delta d) => switch (d.arah) {
  ArahDelta.tidakAda => '—',
  ArahDelta.tetap => 'sama dengan kemarin',
  ArahDelta.naik => '▲ ${d.persen.abs()}% vs kemarin',
  ArahDelta.turun => '▼ ${d.persen.abs()}% vs kemarin',
};

/// Proporsi dua bagian dalam persen (jumlah 100, atau 0/0 bila total nol).
(int, int) proporsi(int a, int b) {
  final t = a + b;
  if (t <= 0) return (0, 0);
  final pa = (a / t * 100).round();
  return (pa, 100 - pa);
}

/// Tinggi batang 0..1 relatif ke nilai terbesar; nilai > 0 minimal 4% agar tetap terlihat.
double tinggiBatang(int nilai, List<int> semua) {
  final maks = semua.fold<int>(0, (a, b) => b > a ? b : a);
  if (maks <= 0 || nilai <= 0) return 0;
  final h = nilai / maks;
  return h < 0.04 ? 0.04 : h;
}

/// Rupiah ringkas untuk label grafik: 950 -> "950", 12500 -> "13 rb", 1250000 -> "1,3 jt".
String singkatRupiah(int n) {
  final a = n.abs();
  String d(double v) => v.toStringAsFixed(1).replaceAll('.', ',');
  if (a >= 1000000000) return '${d(n / 1e9)} M';
  if (a >= 1000000) return '${d(n / 1e6)} jt';
  if (a >= 1000) return '${(n / 1e3).round()} rb';
  return '$n';
}

const _hari = ['Senin', 'Selasa', 'Rabu', 'Kamis', 'Jumat', 'Sabtu', 'Minggu'];

/// "2026-11-18" -> "Rabu, 18 November 2026" ('' bila tidak valid).
String tanggalPanjang(String iso) {
  final d = DateTime.tryParse(iso);
  if (d == null) return '';
  return '${_hari[d.weekday - 1]}, ${d.day} ${namaPeriode(d.year * 100 + d.month)}';
}

/// "2026-11-18" -> "Rab" (3 huruf hari).
String hariSingkat(String iso) {
  final d = DateTime.tryParse(iso);
  return d == null ? '' : _hari[d.weekday - 1].substring(0, 3);
}

/// Label rentang yang dibaca pengguna: satu hari menjadi tanggal penuh, duanya menjadi rentang.
String labelRentang(String dari, String sampai) =>
    dari == sampai ? tanggalPanjang(dari) : rentangTanggal(dari, sampai);

// ---------------------------------------------------------------- warna semantik (ikut tema terang/gelap)

enum Tingkat { ok, peringatan, bahaya }

Color _warna(BuildContext c, Tingkat t) {
  final cs = Theme.of(c).colorScheme;
  final gelap = Theme.of(c).brightness == Brightness.dark;
  return switch (t) {
    Tingkat.ok => cs.primary,
    Tingkat.peringatan =>
      gelap ? const Color(0xFFFFC857) : const Color(0xFF8A5A00),
    Tingkat.bahaya => cs.error,
  };
}

// ---------------------------------------------------------------- widget

class BerandaScaffold extends StatelessWidget {
  const BerandaScaffold({
    super.key,
    required this.actions,
    required this.onRefresh,
    required this.memuat,
    required this.adaData,
    required this.galat,
    required this.children,
  });

  final List<Widget> actions, children;
  final Future<void> Function() onRefresh;
  final bool memuat, adaData;
  final String? galat;

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(
      title: const Text('Beranda'),
      automaticallyImplyLeading: false,
      actions: actions,
    ),
    body: SafeArea(
      top: false,
      child: LayoutBuilder(
        builder: (context, constraints) => RefreshIndicator(
          onRefresh: onRefresh,
          child: ListView(
            physics: const AlwaysScrollableScrollPhysics(),
            padding: EdgeInsets.fromLTRB(
              math.max(16, (constraints.maxWidth - 720) / 2),
              8,
              math.max(16, (constraints.maxWidth - 720) / 2),
              24,
            ),
            children: [
              if (memuat && !adaData)
                const Padding(
                  padding: EdgeInsets.all(32),
                  child: LoadingKopkar(),
                ),
              if (galat case final pesan?) ...[
                GalatBeranda(
                  judul: adaData
                      ? 'Pembaruan gagal · menampilkan data terakhir'
                      : 'Beranda tidak dapat dimuat',
                  pesan: pesan,
                  cobaLagi: onRefresh,
                ),
                const SizedBox(height: 12),
              ],
              ...children,
            ],
          ),
        ),
      ),
    ),
  );
}

class GalatBeranda extends StatelessWidget {
  const GalatBeranda({
    super.key,
    required this.judul,
    required this.pesan,
    required this.cobaLagi,
  });

  final String judul, pesan;
  final VoidCallback cobaLagi;

  @override
  Widget build(BuildContext context) => Column(
    crossAxisAlignment: CrossAxisAlignment.start,
    children: [
      BannerBeranda(tingkat: Tingkat.bahaya, judul: judul, pesan: pesan),
      TextButton(onPressed: cobaLagi, child: const Text('Coba lagi')),
    ],
  );
}

class BerandaHeader extends StatelessWidget {
  const BerandaHeader({super.key, required this.tanggal, this.nama, this.chip});

  final String tanggal;
  final String? nama;
  final Widget? chip;

  @override
  Widget build(BuildContext context) {
    final tema = Theme.of(context);
    final tanggalTampil = tanggalPanjang(tanggal);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        if (nama case final nama? when nama.isNotEmpty)
          Semantics(
            header: true,
            child: Text('Halo, $nama', style: tema.textTheme.titleLarge),
          ),
        if (tanggalTampil.isNotEmpty)
          Text(
            tanggalTampil,
            style: tema.textTheme.bodyMedium?.copyWith(
              color: tema.colorScheme.onSurfaceVariant,
            ),
          ),
        if (chip != null) ...[const SizedBox(height: 8), chip!],
      ],
    );
  }
}

/// Chip status periode: terbuka (hijau) / tertutup (merah) / belum diatur (kuning), dengan rentang tanggal.
class ChipPeriode extends StatelessWidget {
  const ChipPeriode({super.key, required this.kode, this.periode, this.dari, this.sampai});
  final String? kode;
  final int? periode;
  final String? dari, sampai;

  @override
  Widget build(BuildContext context) {
    final tingkat = kode == null
        ? Tingkat.ok
        : kode == 'PERIODE_TUTUP'
        ? Tingkat.bahaya
        : Tingkat.peringatan;
    final w = _warna(context, tingkat);
    final teks = [
      judulPeriode(kode),
      if (periode != null && dari != null && sampai != null) rentangTanggal(dari!, sampai!),
    ].join(' · ');
    return Align(
      alignment: Alignment.centerLeft,
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 7),
        decoration: BoxDecoration(
          color: w.withValues(alpha: .14),
          borderRadius: BorderRadius.circular(99),
        ),
        child: Text(
          teks,
          style: TextStyle(color: w, fontWeight: FontWeight.w700, fontSize: 12),
        ),
      ),
    );
  }
}

/// Banner pesan (periode tutup, mode demo, dst.). Diumumkan sebagai live region untuk pembaca layar.
class BannerBeranda extends StatelessWidget {
  const BannerBeranda({super.key, required this.tingkat, required this.judul, this.pesan, this.catatan});
  final Tingkat tingkat;
  final String judul;
  final String? pesan, catatan;

  @override
  Widget build(BuildContext context) {
    final w = _warna(context, tingkat);
    return Semantics(
      liveRegion: true,
      child: Container(
        width: double.infinity,
        padding: const EdgeInsets.all(14),
        decoration: BoxDecoration(
          color: w.withValues(alpha: .12),
          borderRadius: BorderRadius.circular(14),
          border: Border.all(color: w.withValues(alpha: .5)),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(judul, style: TextStyle(color: w, fontWeight: FontWeight.w700)),
            if (pesan != null && pesan!.isNotEmpty) Text(pesan!),
            if (catatan != null) Text(catatan!, style: Theme.of(context).textTheme.bodySmall),
          ],
        ),
      ),
    );
  }
}

/// Tombol aksi utama Beranda (besar, target sentuh >= 56).
class AksiUtama extends StatelessWidget {
  const AksiUtama({
    super.key,
    required this.label,
    required this.icon,
    required this.onPressed,
    this.sekunder = false,
  });
  final String label;
  final IconData icon;
  final VoidCallback? onPressed;
  final bool sekunder;

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    final style = sekunder ? FilledButton.tonal : FilledButton.new;
    return SizedBox(
      width: double.infinity,
      child: style(
        onPressed: onPressed,
        style: FilledButton.styleFrom(
          minimumSize: const Size(48, 56),
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(14),
          ),
          backgroundColor: sekunder ? cs.primaryContainer : null,
          foregroundColor: sekunder ? cs.onPrimaryContainer : null,
        ),
        child: Row(
          children: [
            Icon(icon),
            const SizedBox(width: 12),
            Expanded(
              child: Text(
                label,
                style: const TextStyle(
                  fontWeight: FontWeight.w700,
                  fontSize: 16,
                ),
              ),
            ),
            if (!sekunder) const Icon(Icons.arrow_forward),
          ],
        ),
      ),
    );
  }
}

/// Metrik penjualan: label kecil, nilai besar, dan (opsional) pembanding.
class KpiKartu extends StatelessWidget {
  const KpiKartu({
    super.key,
    required this.label,
    required this.nilai,
    this.delta,
    this.hero = false,
  });
  final String label, nilai;
  final Delta? delta;
  final bool hero;

  @override
  Widget build(BuildContext context) {
    final t = Theme.of(context).textTheme;
    final cs = Theme.of(context).colorScheme;
    final Color? dw = switch (delta?.arah) {
      ArahDelta.naik => _warna(context, Tingkat.ok),
      ArahDelta.turun => cs.error,
      _ => null,
    };
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(label, style: t.bodySmall?.copyWith(color: cs.onSurfaceVariant)),
        const SizedBox(height: 4),
        Text(
          nilai,
          style: (hero ? t.headlineMedium : t.titleMedium)?.copyWith(
            fontWeight: FontWeight.w700,
            fontFeatures: const [FontFeature.tabularFigures()],
          ),
        ),
        if (delta != null) ...[
          const SizedBox(height: 4),
          Text(
            teksDelta(delta!),
            style: t.bodySmall?.copyWith(
              fontWeight: FontWeight.w700,
              color: dw ?? cs.onSurfaceVariant,
            ),
          ),
        ],
      ],
    );
  }
}

bool _metrikSejajar(BuildContext context, BoxConstraints batas) =>
    batas.maxWidth >= 320 && MediaQuery.textScalerOf(context).scale(16) <= 20;

class _PasanganMetrik extends StatelessWidget {
  const _PasanganMetrik(this.kiri, this.kanan);

  final Widget kiri, kanan;

  @override
  Widget build(BuildContext context) => LayoutBuilder(
    builder: (context, batas) => _metrikSejajar(context, batas)
        ? Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Expanded(child: kiri),
              const SizedBox(width: 16),
              Expanded(child: kanan),
            ],
          )
        : Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [kiri, const SizedBox(height: 12), kanan],
          ),
  );
}

class RingkasanPenjualan extends StatelessWidget {
  const RingkasanPenjualan({
    super.key,
    required this.label,
    required this.total,
    required this.jumlah,
    this.kemarin,
    this.pembayaran,
  });

  final String label;
  final int total, jumlah;
  final HariJual? kemarin;
  final (int, int)? pembayaran;

  @override
  Widget build(BuildContext context) => Card(
    child: Padding(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          KpiKartu(
            label: label,
            nilai: rupiah(total),
            hero: true,
            delta: kemarin == null ? null : hitungDelta(total, kemarin!.total),
          ),
          const Divider(height: 24),
          _PasanganMetrik(
            KpiKartu(
              label: 'Transaksi',
              nilai: '$jumlah',
              delta: kemarin == null
                  ? null
                  : hitungDelta(jumlah, kemarin!.jumlah),
            ),
            KpiKartu(
              label: 'Rata-rata per transaksi',
              nilai: rupiah(jumlah == 0 ? 0 : (total / jumlah).round()),
            ),
          ),
          if (pembayaran case (final tunai, final kredit)) ...[
            const Divider(height: 24),
            _PasanganMetrik(
              KpiKartu(label: 'Tunai', nilai: rupiah(tunai)),
              KpiKartu(label: 'Kredit', nilai: rupiah(kredit)),
            ),
          ],
        ],
      ),
    ),
  );
}

class DaftarBeranda extends StatelessWidget {
  const DaftarBeranda({super.key, required this.kosong, required this.baris});

  final String kosong;
  final List<Widget> baris;

  @override
  Widget build(BuildContext context) => Card(
    clipBehavior: Clip.antiAlias,
    child: baris.isEmpty
        ? Padding(padding: const EdgeInsets.all(16), child: Text(kosong))
        : Column(
            children: [
              for (final (i, item) in baris.indexed) ...[
                if (i > 0) const Divider(height: 1, indent: 16, endIndent: 16),
                item,
              ],
            ],
          ),
  );
}

class BarisBeranda extends StatelessWidget {
  const BarisBeranda({
    super.key,
    required this.judul,
    required this.detail,
    this.nilai,
    this.onTap,
  });

  final String judul, detail;
  final String? nilai;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) {
    final tema = Theme.of(context);
    final angka = nilai == null
        ? null
        : Text(
            nilai!,
            style: tema.textTheme.titleSmall?.copyWith(
              fontFeatures: const [FontFeature.tabularFigures()],
            ),
          );
    return InkWell(
      onTap: onTap,
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
        child: LayoutBuilder(
          builder: (context, batas) {
            final sejajar = _metrikSejajar(context, batas);
            return Row(
              children: [
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        judul,
                        style: tema.textTheme.titleSmall?.copyWith(
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                      const SizedBox(height: 4),
                      Text(
                        detail,
                        style: tema.textTheme.bodySmall?.copyWith(
                          color: tema.colorScheme.onSurfaceVariant,
                        ),
                      ),
                      if (!sejajar && angka != null) ...[
                        const SizedBox(height: 4),
                        angka,
                      ],
                    ],
                  ),
                ),
                if (sejajar && angka != null) ...[
                  const SizedBox(width: 12),
                  Flexible(child: angka),
                ],
                if (onTap != null) const Icon(Icons.chevron_right),
              ],
            );
          },
        ),
      ),
    );
  }
}

/// Hero limit anggota: sisa limit besar + bar progres + status berwarna.
class LimitHero extends StatelessWidget {
  const LimitHero({super.key, required this.r});
  final Ringkasan r;

  @override
  Widget build(BuildContext context) {
    final status = statusLimit(r.belanja, r.limit);
    final persen = persenTerpakai(r.belanja, r.limit);
    final warna = switch (status) {
      StatusLimit.hampir => const [Color(0xFF6B4A00), Color(0xFF805800)],
      StatusLimit.terlampaui => const [Color(0xFF6E1D18), Color(0xFFB3372F)],
      _ => const [Color(0xFF104A2B), Color(0xFF1E6634)],
    };
    const putih = Colors.white;
    final t = Theme.of(context).textTheme;
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        borderRadius: BorderRadius.circular(20),
        gradient: LinearGradient(
          colors: warna,
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
        ),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            'LIMIT BELANJA · ${namaPeriode(r.periode).toUpperCase()}',
            style: t.labelSmall?.copyWith(
              color: putih.withValues(alpha: .8),
              letterSpacing: 1.2,
            ),
          ),
          Text(
            rentangTanggal(r.dari, r.sampai),
            style: t.bodySmall?.copyWith(color: putih.withValues(alpha: .8)),
          ),
          const SizedBox(height: 10),
          if (status == StatusLimit.takDibatasi) ...[
            Text(
              'Tidak dibatasi',
              style: t.headlineMedium?.copyWith(
                color: putih,
                fontWeight: FontWeight.w700,
              ),
            ),
            Text(
              'Belanja periode ini ${rupiah(r.belanja)}',
              style: const TextStyle(color: putih),
            ),
          ] else ...[
            Text(
              'Sisa limit',
              style: t.bodySmall?.copyWith(color: putih.withValues(alpha: .85)),
            ),
            Text(
              rupiah(math.max(0, r.sisa ?? 0)),
              style: t.headlineMedium?.copyWith(
                color: putih,
                fontWeight: FontWeight.w700,
                fontFeatures: const [FontFeature.tabularFigures()],
              ),
            ),
            const SizedBox(height: 10),
            Semantics(
              label: 'Limit terpakai $persen persen',
              child: ClipRRect(
                borderRadius: BorderRadius.circular(99),
                child: LinearProgressIndicator(
                  value: persen / 100,
                  minHeight: 12,
                  color: putih,
                  backgroundColor: putih.withValues(alpha: .25),
                ),
              ),
            ),
            const SizedBox(height: 8),
            Text(
              '${rupiah(r.belanja)} terpakai dari ${rupiah(r.limit)} ($persen%)'
              '${status == StatusLimit.hampir
                  ? ' · Hampir habis'
                  : status == StatusLimit.terlampaui
                  ? ' · Limit terlampaui'
                  : ''}',
              style: const TextStyle(color: putih),
            ),
          ],
        ],
      ),
    );
  }
}

/// Kartu "Perlu perhatian" admin: tingkat berwarna, ketuk untuk menuju layar terkait.
class PerhatianKartu extends StatelessWidget {
  const PerhatianKartu({super.key, required this.tingkat, required this.judul, required this.pesan, this.onTap});
  final Tingkat tingkat;
  final String judul, pesan;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) {
    final w = _warna(context, tingkat);
    return Card(
      margin: EdgeInsets.zero,
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        onTap: onTap,
        child: Container(
          decoration: BoxDecoration(border: Border(left: BorderSide(color: w, width: 5))),
          constraints: const BoxConstraints(minHeight: 56),
          padding: const EdgeInsets.fromLTRB(14, 12, 8, 12),
          child: Row(
            children: [
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(judul, style: const TextStyle(fontWeight: FontWeight.w700)),
                    Text(pesan, style: Theme.of(context).textTheme.bodySmall),
                  ],
                ),
              ),
              if (onTap != null) Icon(Icons.chevron_right, color: w),
            ],
          ),
        ),
      ),
    );
  }
}

/// Grafik batang penjualan harian (7 hari). Hari terakhir (hari ini) disorot; nilai tersedia untuk pembaca layar.
class TrenBatang extends StatelessWidget {
  const TrenBatang({super.key, required this.tren});
  final List<HariJual> tren;

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    final nilai = [for (final h in tren) h.total];
    final t = Theme.of(context).textTheme;
    return Semantics(
      label:
          'Penjualan harian: ${[for (final h in tren) '${hariSingkat(h.tanggal)} ${rupiah(h.total)}'].join(', ')}',
      child: ExcludeSemantics(
        child: _AreaGrafik(
          kolom: tren.length,
          lebarKolom: 44,
          height: 170,
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              for (var i = 0; i < tren.length; i++)
                Expanded(
                  child: Padding(
                    padding: const EdgeInsets.symmetric(horizontal: 3),
                    child: Column(
                      children: [
                        Text(
                          singkatRupiah(tren[i].total),
                          style: t.labelSmall?.copyWith(fontSize: 9),
                          maxLines: 1,
                          overflow: TextOverflow.clip,
                        ),
                        const SizedBox(height: 4),
                        Expanded(
                          child: DecoratedBox(
                            decoration: BoxDecoration(
                              color: cs.outline.withValues(alpha: .45),
                              borderRadius: BorderRadius.circular(8),
                            ),
                            child: Align(
                              alignment: Alignment.bottomCenter,
                              child: FractionallySizedBox(
                                heightFactor: tinggiBatang(
                                  tren[i].total,
                                  nilai,
                                ),
                                widthFactor: 1,
                                child: DecoratedBox(
                                  decoration: BoxDecoration(
                                    color: i == tren.length - 1
                                        ? cs.primary
                                        : cs.primary.withValues(alpha: .4),
                                    borderRadius: const BorderRadius.vertical(
                                      top: Radius.circular(8),
                                    ),
                                  ),
                                ),
                              ),
                            ),
                          ),
                        ),
                        const SizedBox(height: 4),
                        Text(
                          hariSingkat(tren[i].tanggal),
                          style: t.labelSmall?.copyWith(
                            fontWeight: FontWeight.w700,
                            color: i == tren.length - 1 ? cs.primary : null,
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
            ],
          ),
        ),
      ),
    );
  }
}

class _AreaGrafik extends StatelessWidget {
  const _AreaGrafik({
    required this.kolom,
    required this.lebarKolom,
    required this.height,
    required this.child,
  });

  final int kolom;
  final double lebarKolom, height;
  final Widget child;

  @override
  Widget build(BuildContext context) => LayoutBuilder(
    builder: (context, batas) {
      final lebar = kolom * MediaQuery.textScalerOf(context).scale(lebarKolom);
      if (lebar <= batas.maxWidth) {
        return SizedBox(height: height, child: child);
      }
      return SingleChildScrollView(
        scrollDirection: Axis.horizontal,
        child: SizedBox(width: lebar, height: height, child: child),
      );
    },
  );
}

/// Judul bagian Beranda.
class JudulBagian extends StatelessWidget {
  const JudulBagian(this.teks, {super.key, this.aksi});
  final String teks;
  final Widget? aksi;

  @override
  Widget build(BuildContext context) {
    final judul = Semantics(
      header: true,
      child: Text(teks, style: Theme.of(context).textTheme.titleMedium),
    );
    final teksBesar = MediaQuery.textScalerOf(context).scale(16) > 20;
    return Padding(
      padding: const EdgeInsets.only(top: 20, bottom: 8),
      child: aksi == null
          ? judul
          : teksBesar
          ? Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [judul, aksi!],
            )
          : Row(
              children: [
                Expanded(child: judul),
                aksi!,
              ],
            ),
    );
  }
}

// ---------------------------------------------------------------- rincian penjualan: rentang waktu (logika murni)

enum PresetRentang { hari, tujuh, periode, bebas }

class Rentang {
  const Rentang(this.dari, this.sampai);
  final String dari, sampai; // YYYY-MM-DD

  @override
  bool operator ==(Object other) => other is Rentang && other.dari == dari && other.sampai == sampai;
  @override
  int get hashCode => Object.hash(dari, sampai);
}

/// Batas rentang yang diterima server (hari) dan batas grafik harian.
const maksHariRincian = 92;
const maksHariGrafik = 31;

DateTime? _tgl(String s) => RegExp(r'^\d{4}-\d{2}-\d{2}$').hasMatch(s) ? DateTime.tryParse('${s}T00:00:00Z') : null;

String isoTanggal(DateTime d) =>
    '${d.year.toString().padLeft(4, '0')}-${d.month.toString().padLeft(2, '0')}-${d.day.toString().padLeft(2, '0')}';

String tambahHari(String iso, int n) => isoTanggal(_tgl(iso)!.add(Duration(days: n)));

int jumlahHari(Rentang r) => _tgl(r.sampai)!.difference(_tgl(r.dari)!).inDays + 1;

/// Rentang dari pilihan cepat. "Hari ini" mengikuti tanggal server. Periode berjalan dipotong sampai hari ini;
/// null bila tidak diketahui.
Rentang? rentangPreset(PresetRentang p, String hariIni, {String? periodeDari, String? periodeSampai}) {
  if (_tgl(hariIni) == null) return null;
  switch (p) {
    case PresetRentang.hari:
      return Rentang(hariIni, hariIni);
    case PresetRentang.tujuh:
      return Rentang(tambahHari(hariIni, -6), hariIni);
    case PresetRentang.periode:
      if (periodeDari == null || periodeSampai == null) return null;
      if (_tgl(periodeDari) == null || _tgl(periodeSampai) == null || periodeDari.compareTo(hariIni) > 0) return null;
      return Rentang(periodeDari, periodeSampai.compareTo(hariIni) < 0 ? periodeSampai : hariIni);
    case PresetRentang.bebas:
      return null;
  }
}

/// Pesan galat untuk rentang yang tidak akan diterima server; null bila valid.
String? validasiRentang(Rentang r, String hariIni) {
  if (_tgl(r.dari) == null || _tgl(r.sampai) == null) return 'Isi tanggal awal dan akhir.';
  if (r.dari.compareTo(r.sampai) > 0) return 'Tanggal awal tidak boleh setelah tanggal akhir.';
  if (r.sampai.compareTo(hariIni) > 0) return 'Tanggal akhir tidak boleh melewati hari ini.';
  if (jumlahHari(r) > maksHariRincian) return 'Rentang maksimal $maksHariRincian hari.';
  return null;
}

/// Persen bagian terhadap total (0 bila total 0).
int persenJenis(int bagian, int total) => total > 0 ? (bagian / total * 100).round() : 0;

// ---------------------------------------------------------------- rincian penjualan: widget

const _kuning = Color(0xFFE0A100);

Color _warnaLain(BuildContext c) =>
    Theme.of(c).brightness == Brightness.dark ? const Color(0xFF6F8D78) : const Color(0xFF8AA08F);

/// Bar proporsi tiga bagian (tunai, kredit, lainnya) terhadap total rentang.
class ProporsiJenis extends StatelessWidget {
  const ProporsiJenis({super.key, required this.pecahan});
  final PecahanJual pecahan;

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    final total = pecahan.total.total;
    int f(int v) => total <= 0 ? 0 : v;
    final seg = <(int, Color)>[
      (f(pecahan.tunai.total), cs.primary),
      (f(pecahan.kredit.total), _kuning),
      (f(pecahan.lain.total), _warnaLain(context)),
    ].where((s) => s.$1 > 0).toList();
    Widget legenda(String label, Color warna) => Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Container(
          width: 10,
          height: 10,
          decoration: BoxDecoration(color: warna, shape: BoxShape.circle),
        ),
        const SizedBox(width: 6),
        Text(label),
      ],
    );
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Semantics(
          label:
              'Tunai ${persenJenis(pecahan.tunai.total, total)} persen, '
              'kredit ${persenJenis(pecahan.kredit.total, total)} persen, '
              'lainnya ${persenJenis(pecahan.lain.total, total)} persen',
          child: ClipRRect(
            borderRadius: BorderRadius.circular(99),
            child: SizedBox(
              height: 8,
              child: seg.isEmpty
                  ? ColoredBox(color: cs.outline)
                  : Row(
                      children: [
                        for (final s in seg)
                          Expanded(
                            flex: s.$1,
                            child: ColoredBox(
                              color: s.$2,
                              child: const SizedBox.expand(),
                            ),
                          ),
                      ],
                    ),
            ),
          ),
        ),
        const SizedBox(height: 8),
        Wrap(
          spacing: 16,
          runSpacing: 4,
          children: [
            legenda('Tunai', cs.primary),
            legenda('Kredit', _kuning),
            if (pecahan.lain.jumlah != 0 || pecahan.lain.total != 0)
              legenda('Lainnya', _warnaLain(context)),
          ],
        ),
      ],
    );
  }
}

/// Grafik batang bertumpuk harian: tunai (bawah), kredit, lainnya. Hanya untuk rentang <= [maksHariGrafik] hari.
class TrenBertumpuk extends StatelessWidget {
  const TrenBertumpuk({super.key, required this.harian});
  final List<RincianHari> harian;

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    final t = Theme.of(context).textTheme;
    final totals = [for (final h in harian) h.tunai + h.kredit + h.lain];
    final lain = _warnaLain(context);
    String label(String iso) =>
        harian.length <= 7 ? hariSingkat(iso) : iso.substring(8);
    return Semantics(
      label:
          'Penjualan harian tunai dan kredit: ${[for (final h in harian) '${label(h.tanggal)} tunai ${rupiah(h.tunai)}, kredit ${rupiah(h.kredit)}'].join('; ')}',
      child: ExcludeSemantics(
        child: _AreaGrafik(
          kolom: harian.length,
          lebarKolom: 26,
          height: 160,
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              for (var i = 0; i < harian.length; i++)
                Expanded(
                  child: Padding(
                    padding: EdgeInsets.symmetric(
                      horizontal: harian.length > 14 ? 1 : 3,
                    ),
                    child: Column(
                      children: [
                        Expanded(
                          child: DecoratedBox(
                            decoration: BoxDecoration(
                              color: cs.outline.withValues(alpha: .45),
                              borderRadius: BorderRadius.circular(6),
                            ),
                            child: Align(
                              alignment: Alignment.bottomCenter,
                              child: totals[i] <= 0
                                  ? const SizedBox.shrink()
                                  : FractionallySizedBox(
                                      heightFactor: tinggiBatang(
                                        totals[i],
                                        totals,
                                      ),
                                      child: ClipRRect(
                                        borderRadius:
                                            const BorderRadius.vertical(
                                              top: Radius.circular(6),
                                            ),
                                        child: Column(
                                          children: [
                                            if (harian[i].lain > 0)
                                              Expanded(
                                                flex: harian[i].lain,
                                                child: ColoredBox(
                                                  color: lain,
                                                  child:
                                                      const SizedBox.expand(),
                                                ),
                                              ),
                                            if (harian[i].kredit > 0)
                                              Expanded(
                                                flex: harian[i].kredit,
                                                child: const ColoredBox(
                                                  color: _kuning,
                                                  child: SizedBox.expand(),
                                                ),
                                              ),
                                            if (harian[i].tunai > 0)
                                              Expanded(
                                                flex: harian[i].tunai,
                                                child: ColoredBox(
                                                  color: cs.primary,
                                                  child:
                                                      const SizedBox.expand(),
                                                ),
                                              ),
                                          ],
                                        ),
                                      ),
                                    ),
                            ),
                          ),
                        ),
                        const SizedBox(height: 4),
                        Text(
                          label(harian[i].tanggal),
                          style: t.labelSmall?.copyWith(
                            fontSize: 9,
                            fontWeight: FontWeight.w700,
                          ),
                          maxLines: 1,
                          overflow: TextOverflow.clip,
                        ),
                      ],
                    ),
                  ),
                ),
            ],
          ),
        ),
      ),
    );
  }
}

/// Satu kasir (atau baris total): tunai, kredit, lainnya (bila ada), total, dan jumlah transaksi.
class KasirRincianKartu extends StatelessWidget {
  const KasirRincianKartu({
    super.key,
    required this.judul,
    this.subjudul,
    required this.pecahan,
    this.total = false,
  });
  final String judul;
  final String? subjudul;
  final PecahanJual pecahan;
  final bool total;

  @override
  Widget build(BuildContext context) {
    final t = Theme.of(context).textTheme;
    final cs = Theme.of(context).colorScheme;
    Widget baris(String label, BagianJual bagian, {bool tebal = false}) =>
        Padding(
          padding: const EdgeInsets.symmetric(vertical: 4),
          child: LayoutBuilder(
            builder: (context, batas) {
              final info = Text(
                '$label · ${bagian.jumlah} transaksi',
                style: tebal ? t.titleSmall : t.bodyMedium,
              );
              final nilai = Text(
                rupiah(bagian.total),
                style: t.titleSmall?.copyWith(
                  fontWeight: tebal ? FontWeight.w800 : FontWeight.w600,
                  fontFeatures: const [FontFeature.tabularFigures()],
                ),
              );
              if (batas.maxWidth < 220 ||
                  MediaQuery.textScalerOf(context).scale(16) > 20) {
                return Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [info, nilai],
                );
              }
              return Row(
                children: [
                  Expanded(child: info),
                  const SizedBox(width: 12),
                  Flexible(
                    child: Align(
                      alignment: Alignment.centerRight,
                      child: nilai,
                    ),
                  ),
                ],
              );
            },
          ),
        );
    return Card(
      color: total ? cs.primaryContainer : null,
      child: Padding(
        padding: const EdgeInsets.all(14),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Semantics(header: true, child: Text(judul, style: t.titleMedium)),
            if (subjudul != null && subjudul!.isNotEmpty)
              Text(subjudul!, style: t.bodySmall),
            const SizedBox(height: 6),
            baris('Tunai', pecahan.tunai),
            baris('Kredit', pecahan.kredit),
            if (pecahan.lain.jumlah != 0 || pecahan.lain.total != 0)
              baris('Lainnya', pecahan.lain),
            const Divider(height: 14),
            baris('Total', pecahan.total, tebal: true),
          ],
        ),
      ),
    );
  }
}

/// Status margin produk: beli <= 0 = HPP belum diisi (margin tidak dapat dihitung), selain itu margin = harga - beli.
({bool tanpaHpp, int? margin}) statusMargin(int harga, int beli) =>
    beli <= 0 ? (tanpaHpp: true, margin: null) : (tanpaHpp: false, margin: harga - beli);
