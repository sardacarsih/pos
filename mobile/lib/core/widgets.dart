import 'dart:typed_data';
import 'dart:math' as math;

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:qr/qr.dart';

import 'format.dart';
import 'providers.dart';

class TropicalScaffold extends StatelessWidget {
  const TropicalScaffold({
    super.key,
    required this.title,
    required this.body,
    this.actions = const [],
    this.leading,
    this.bottomNavigationBar,
    this.centerTitle = false,
  });

  final String title;
  final Widget body;
  final List<Widget> actions;
  final Widget? leading;
  final Widget? bottomNavigationBar;
  final bool centerTitle;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final topInset = MediaQuery.paddingOf(context).top;
    return Scaffold(
      backgroundColor: theme.scaffoldBackgroundColor,
      bottomNavigationBar: bottomNavigationBar,
      body: Column(
        children: [
          SizedBox(
            // Keyboard terbuka: header diringkas agar daftar di bawahnya tidak terhimpit.
            height: MediaQuery.viewInsetsOf(context).bottom > 0
                ? math.max(100, topInset + 80)
                : math.max(120, topInset + 92),
            child: Stack(
              fit: StackFit.expand,
              children: [
                Image.asset(
                  'assets/login_bg.webp',
                  fit: BoxFit.cover,
                  alignment: Alignment.topCenter,
                ),
                DecoratedBox(
                  decoration: BoxDecoration(
                    gradient: LinearGradient(
                      begin: Alignment.topCenter,
                      end: Alignment.bottomCenter,
                      colors: [
                        Colors.black.withValues(
                          alpha: theme.brightness == Brightness.dark
                              ? .53
                              : .24,
                        ),
                        Colors.black.withValues(
                          alpha: theme.brightness == Brightness.dark
                              ? .34
                              : .14,
                        ),
                      ],
                    ),
                  ),
                ),
                SafeArea(
                  bottom: false,
                  child: SizedBox(
                    height: 64,
                    child: Row(
                      children: [
                        if (leading != null) leading!,
                        Expanded(
                          child: Text(
                            title,
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                            textAlign: centerTitle
                                ? TextAlign.center
                                : TextAlign.left,
                            style: theme.textTheme.titleLarge?.copyWith(
                              color: Colors.white,
                              fontWeight: FontWeight.w700,
                              shadows: const [
                                Shadow(color: Colors.black45, blurRadius: 12),
                              ],
                            ),
                          ),
                        ),
                        ...actions,
                      ],
                    ),
                  ),
                ),
                Positioned(
                  bottom: 0,
                  left: 0,
                  right: 0,
                  child: Container(
                    height: 28,
                    decoration: BoxDecoration(
                      color: theme.scaffoldBackgroundColor,
                      borderRadius: const BorderRadius.vertical(
                        top: Radius.circular(24),
                      ),
                    ),
                  ),
                ),
              ],
            ),
          ),
          Expanded(
            child: ColoredBox(
              color: theme.scaffoldBackgroundColor,
              child: SafeArea(top: false, child: body),
            ),
          ),
        ],
      ),
    );
  }
}

class GradientActionButton extends StatelessWidget {
  const GradientActionButton({
    super.key,
    required this.label,
    required this.onPressed,
    this.icon,
    this.height = 56,
  });

  final String label;
  final VoidCallback? onPressed;
  final IconData? icon;
  final double height;

  @override
  Widget build(BuildContext context) {
    final dark = Theme.of(context).brightness == Brightness.dark;
    final colors = dark
        ? const [Color(0xFF3E9E34), Color(0xFF145C27)]
        : const [Color(0xFF43B22E), Color(0xFF0D6A2A)];
    return Opacity(
      opacity: onPressed == null ? .48 : 1,
      child: DecoratedBox(
        decoration: BoxDecoration(
          gradient: LinearGradient(
            colors: colors,
            begin: Alignment.topCenter,
            end: Alignment.bottomCenter,
          ),
          borderRadius: BorderRadius.circular(32),
          boxShadow: onPressed == null
              ? null
              : [
                  BoxShadow(
                    color: colors.last.withValues(alpha: .22),
                    blurRadius: 10,
                    offset: const Offset(0, 4),
                  ),
                ],
        ),
        child: Material(
          color: Colors.transparent,
          child: InkWell(
            onTap: onPressed,
            borderRadius: BorderRadius.circular(32),
            child: SizedBox(
              height: height,
              child: Row(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  if (icon != null) ...[
                    Icon(icon, color: Colors.white),
                    const SizedBox(width: 8),
                  ],
                  Text(
                    label,
                    style: const TextStyle(
                      color: Colors.white,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}

/// Foto produk yang dibundel di app, dicari dari kata kunci nama produk (huruf kecil).
/// Tambah foto baru: taruh di assets/produk/, daftarkan di pubspec.yaml, lalu tambah baris di sini.
const _fotoProduk = {'pertalite': 'assets/produk/pertalite.png'};

class FuelArtwork extends ConsumerWidget {
  const FuelArtwork({
    super.key,
    required this.name,
    this.height = 96,
    this.radius = 18,
    this.produkId,
    this.fotoVersi,
    this.fit = BoxFit.cover,
  });

  final String name;

  /// Bila keduanya ada, foto unggahan admin diambil dari server (di-cache); selain itu foto/ikon bawaan.
  final int? produkId, fotoVersi;

  /// Foto dibuat persegi (lihat admin_produk_page); cover = mengisi area tanpa celah kiri/kanan/atas/bawah.
  final BoxFit fit;

  /// True bila produk punya foto (unggahan admin atau bawaan): tata letak memakai area persegi.
  static bool adaFoto(String name, int? fotoVersi) =>
      fotoVersi != null ||
      _fotoProduk.keys.any((k) => name.toLowerCase().contains(k));
  final double height;
  final double radius;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final n = name.toLowerCase();
    final unggahan = produkId != null && fotoVersi != null
        ? ref
              .watch(fotoProdukProvider((id: produkId!, versi: fotoVersi!)))
              .value
        : null;
    final foto = _fotoProduk.entries
        .where((e) => n.contains(e.key))
        .map((e) => e.value)
        .firstOrNull;
    final colors = n.contains('pertalite')
        ? const [Color(0xFF8BD36A), Color(0xFF2F8F3A)]
        : n.contains('turbo')
        ? const [Color(0xFFF0707A), Color(0xFFA3121F)]
        : n.contains('dexlite')
        ? const [Color(0xFFF6D55C), Color(0xFFC79A00)]
        : n.contains('pertamina dex')
        ? const [Color(0xFF9AA3AE), Color(0xFF3F4854)]
        : n.contains('biosolar')
        ? const [Color(0xFFF4B860), Color(0xFFC26A12)]
        : const [Color(0xFF6FA8F5), Color(0xFF1F4FB8)];
    return Container(
      height: height,
      // Potong lingkaran dekoratif yang melewati sudut membulat (tadinya terlihat sebagai tonjolan abu-abu).
      clipBehavior: Clip.antiAlias,
      decoration: BoxDecoration(
        borderRadius: BorderRadius.circular(radius),
        gradient: LinearGradient(
          // Dengan foto, latar netral agar foto tidak berbingkai warna lain.
          colors: (foto != null || unggahan != null)
              ? const [Color(0xFFF3F7F0), Color(0xFFE2EBDD)]
              : colors,
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
        ),
      ),
      child: Stack(
        children: [
          Positioned(
            left: -24,
            top: -36,
            child: _GlowCircle(
              size: height * 1.1,
              color: Colors.white.withValues(alpha: .12),
            ),
          ),
          Positioned(
            right: -28,
            bottom: -42,
            child: _GlowCircle(
              size: height * .9,
              color: Colors.black.withValues(alpha: .11),
            ),
          ),
          // Foto produk bila ada (lihat _fotoProduk); selain itu ikon generik.
          if (unggahan != null)
            Positioned.fill(
              child: ClipRRect(
                borderRadius: BorderRadius.circular(radius),
                child: Image.memory(
                  unggahan,
                  fit: fit,
                  gaplessPlayback: true,
                  semanticLabel: 'Foto produk $name',
                ),
              ),
            )
          else if (foto != null)
            Positioned.fill(
              child: ClipRRect(
                borderRadius: BorderRadius.circular(radius),
                child: Image.asset(
                  foto,
                  fit: fit,
                  semanticLabel: 'Foto produk $name',
                ),
              ),
            )
          else
            Center(
              child: Icon(
                n.contains('pelumas')
                    ? Icons.oil_barrel_outlined
                    : Icons.water_drop,
                size: height * .48,
                color: Colors.white.withValues(alpha: .96),
              ),
            ),
        ],
      ),
    );
  }
}

class _GlowCircle extends StatelessWidget {
  const _GlowCircle({required this.size, required this.color});
  final double size;
  final Color color;

  @override
  Widget build(BuildContext context) => Container(
    width: size,
    height: size,
    decoration: BoxDecoration(color: color, shape: BoxShape.circle),
  );
}

class StatePill extends StatelessWidget {
  const StatePill({super.key, required this.text, this.color});
  final String text;
  final Color? color;

  @override
  Widget build(BuildContext context) {
    final c = color ?? Theme.of(context).colorScheme.primaryContainer;
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 5),
      decoration: BoxDecoration(
        color: c.withValues(alpha: .18),
        borderRadius: BorderRadius.circular(20),
      ),
      child: Text(
        text,
        style: TextStyle(
          color: color ?? Theme.of(context).colorScheme.primary,
          fontWeight: FontWeight.w600,
          fontSize: 11,
        ),
      ),
    );
  }
}

class MemberQrView extends StatelessWidget {
  const MemberQrView({super.key, required this.data, this.size = 220});
  final String data;
  final double size;

  @override
  Widget build(BuildContext context) {
    final code = QrCode.fromData(
      data: data,
      errorCorrectLevel: QrErrorCorrectLevel.M,
    );
    final image = QrImage(code);
    return Semantics(
      label: 'Kode QR kartu anggota',
      image: true,
      child: Container(
        width: size,
        height: size,
        padding: const EdgeInsets.all(10),
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(20),
        ),
        child: CustomPaint(painter: _QrPainter(image)),
      ),
    );
  }
}

class _QrPainter extends CustomPainter {
  const _QrPainter(this.image);
  final QrImage image;

  @override
  void paint(Canvas canvas, Size size) {
    final cell = size.width / image.moduleCount;
    final paint = Paint()..color = const Color(0xFF1E1B18);
    for (var row = 0; row < image.moduleCount; row++) {
      for (var col = 0; col < image.moduleCount; col++) {
        if (image.isDark(row, col)) {
          canvas.drawRect(
            Rect.fromLTWH(col * cell, row * cell, cell + .2, cell + .2),
            paint,
          );
        }
      }
    }
  }

  @override
  bool shouldRepaint(covariant _QrPainter oldDelegate) =>
      oldDelegate.image != image;
}

/// Kartu status periode (dashboard kasir dan admin): terbuka / tertutup / belum diatur / belum valid.
/// [kode] null = terbuka; selain itu kode server (PERIODE_TUTUP | PERIODE_TIDAK_ADA | PERIODE_TIDAK_VALID).
class StatusPeriodeCard extends StatelessWidget {
  const StatusPeriodeCard({
    super.key,
    required this.kode,
    required this.pesan,
    required this.tanggal,
  });
  final String? kode, pesan;
  final String tanggal;

  @override
  Widget build(BuildContext context) {
    final (warna, ikon) = switch (kode) {
      null => (const Color(0xFF2E7D32), Icons.check_circle),
      'PERIODE_TUTUP' => (Theme.of(context).colorScheme.error, Icons.lock),
      'PERIODE_TIDAK_ADA' => (const Color(0xFFD68A00), Icons.event_busy),
      _ => (const Color(0xFFD68A00), Icons.warning_amber_rounded),
    };
    return Card(
      margin: EdgeInsets.zero,
      color: warna.withValues(alpha: .14),
      child: ListTile(
        leading: Icon(ikon, color: warna),
        title: Text(
          judulPeriode(kode),
          style: const TextStyle(fontWeight: FontWeight.w600),
        ),
        subtitle: Text(
          kode == null ? tanggal : '${pesan ?? ''}\n$hubungiAdminPeriode',
        ),
      ),
    );
  }
}

/// Lingkaran foto: foto bila ada, selain itu inisial nama (atau ikon bila nama kosong).
class AvatarFoto extends StatelessWidget {
  const AvatarFoto({super.key, required this.nama, this.bytes, this.radius = 20});
  final String nama;
  final Uint8List? bytes;
  final double radius;

  String get _inisial {
    final p = nama.trim().split(RegExp(r'\s+')).where((e) => e.isNotEmpty).toList();
    if (p.isEmpty) return '';
    final a = p.first[0];
    return (p.length > 1 ? a + p[1][0] : a).toUpperCase();
  }

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    return CircleAvatar(
      radius: radius,
      backgroundColor: cs.primaryContainer,
      foregroundImage: bytes == null ? null : MemoryImage(bytes!),
      child: _inisial.isEmpty
          ? Icon(Icons.person, size: radius * 1.1, color: cs.onPrimaryContainer)
          : Text(
              _inisial,
              style: TextStyle(
                fontSize: radius * 0.8,
                fontWeight: FontWeight.w700,
                color: cs.onPrimaryContainer,
              ),
            ),
    );
  }
}

/// Foto profil akun sendiri (cache sesi; berganti otomatis saat foto diubah).
class ProfilAvatar extends ConsumerWidget {
  const ProfilAvatar({super.key, this.radius = 16});
  final double radius;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final p = ref.watch(profilSayaProvider).value;
    final nama = (p?['nama'] as String?) ?? ref.watch(authProvider).profil?.nama ?? '';
    final versi = (p?['foto_versi'] as num?)?.toInt();
    final foto = versi == null ? null : ref.watch(fotoProfilProvider(versi)).value;
    return AvatarFoto(nama: nama, bytes: foto, radius: radius);
  }
}

/// Foto anggota lain untuk kasir (verifikasi identitas); inisial bila tidak ada foto.
class FotoAnggotaAvatar extends ConsumerWidget {
  const FotoAnggotaAvatar({super.key, required this.nik, required this.nama, this.radius = 20});
  final String nik, nama;
  final double radius;

  @override
  Widget build(BuildContext context, WidgetRef ref) => AvatarFoto(
    nama: nama,
    bytes: ref.watch(fotoAnggotaProvider(nik)).value,
    radius: radius,
  );
}
