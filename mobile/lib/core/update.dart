import 'package:dio/dio.dart';
import 'package:flutter/foundation.dart' show kIsWeb;
import 'package:flutter/material.dart';
import 'package:ota_update/ota_update.dart';
import 'package:package_info_plus/package_info_plus.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'providers.dart';

/// Isi `downloads/version.json` di server (dibuat oleh tools/rilis-apk.ps1).
class InfoRilis {
  const InfoRilis({
    required this.versionCode,
    required this.versionName,
    required this.apk,
    required this.sha256,
    this.minVersionCode = 0,
    this.catatan = '',
  });

  factory InfoRilis.dariJson(Map<String, dynamic> j) => InfoRilis(
    versionCode: j['versionCode'] as int,
    versionName: j['versionName'] as String,
    apk: j['apk'] as String,
    sha256: j['sha256'] as String,
    minVersionCode: (j['minVersionCode'] as int?) ?? 0,
    catatan: (j['catatan'] as String?) ?? '',
  );

  final int versionCode;
  final String versionName;
  final String apk;
  final String sha256;
  final int minVersionCode;
  final String catatan;

  bool lebihBaru(int terpasang) => versionCode > terpasang;

  /// Versi terpasang terlalu lama: dialog tidak boleh ditutup.
  bool wajib(int terpasang) => terpasang < minVersionCode;
}

const _kunciDilewati = 'update_dilewati';
const _kunciCekTerakhir = 'update_cek_terakhir';
const _jedaCek = Duration(hours: 6);

/// Null bila tidak ada update (atau server tidak terjangkau: tidak boleh mengganggu aplikasi).
/// Tanpa update, server tidak ditanya lagi selama [_jedaCek]; ada update = ditanya tiap panggilan.
/// [manual] (tombol "Cek pembaruan"): abaikan jeda 6 jam dan status "Nanti", dan lempar galat
/// jaringan agar UI bisa membedakan "sudah terbaru" dari "tidak bisa memeriksa".
Future<InfoRilis?> cekRilis(int terpasang, {bool manual = false}) async {
  try {
    final p = await SharedPreferences.getInstance();
    final terakhir = p.getInt(_kunciCekTerakhir) ?? 0;
    final sekarang = DateTime.now().millisecondsSinceEpoch;
    if (!manual &&
        sekarang - terakhir < _jedaCek.inMilliseconds &&
        sekarang >= terakhir) {
      return null;
    }
    final dio = Dio(
      BaseOptions(
        connectTimeout: const Duration(seconds: 5),
        receiveTimeout: const Duration(seconds: 5),
      ),
    );
    final res = await dio.get<Map<String, dynamic>>(
      '$apiUrl/downloads/version.json',
    );
    final info = InfoRilis.dariJson(res.data!);
    if (!info.lebihBaru(terpasang)) {
      await p.setInt(_kunciCekTerakhir, sekarang);
      return null;
    }
    if (!manual &&
        !info.wajib(terpasang) &&
        p.getInt(_kunciDilewati) == info.versionCode) {
      await p.setInt(_kunciCekTerakhir, sekarang);
      return null;
    }
    return info;
  } catch (_) {
    if (manual) rethrow;
    return null;
  }
}

/// Pengecekan sekali per pemanggilan; dialog ditampilkan di atas navigator [context].
Future<void> tawarkanUpdate(BuildContext context, {bool manual = false}) async {
  if (kIsWeb) return; // PWA diperbarui lewat service worker, bukan APK
  final terpasang = int.parse((await PackageInfo.fromPlatform()).buildNumber);
  final InfoRilis? info;
  try {
    info = await cekRilis(terpasang, manual: manual);
  } catch (_) {
    if (context.mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Tidak bisa memeriksa pembaruan. Periksa koneksi lalu coba lagi.')),
      );
    }
    return;
  }
  if (!context.mounted) return;
  if (info == null) {
    if (manual) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Aplikasi sudah versi terbaru.')),
      );
    }
    return;
  }
  final rilis = info;
  final wajib = rilis.wajib(terpasang);
  await showDialog<void>(
    context: context,
    barrierDismissible: !wajib,
    builder: (_) => _DialogUpdate(info: rilis, wajib: wajib),
  );
}

class _DialogUpdate extends StatefulWidget {
  const _DialogUpdate({required this.info, required this.wajib});
  final InfoRilis info;
  final bool wajib;
  @override
  State<_DialogUpdate> createState() => _DialogUpdateState();
}

class _DialogUpdateState extends State<_DialogUpdate> {
  bool _mengunduh = false;
  int _persen = 0;
  String? _galat;

  void _mulai() {
    setState(() {
      _mengunduh = true;
      _galat = null;
    });
    OtaUpdate()
        .execute(
          '$apiUrl/downloads/${widget.info.apk}',
          destinationFilename: 'pos-kasir.apk',
          sha256checksum: widget.info.sha256,
        )
        .listen((e) {
          if (!mounted) return;
          switch (e.status) {
            case OtaStatus.DOWNLOADING:
              setState(() => _persen = int.tryParse(e.value ?? '') ?? _persen);
            case OtaStatus.INSTALLING:
            case OtaStatus.INSTALLATION_DONE:
              // Layar pemasangan Android mengambil alih; bila dibatalkan, tombol harus aktif lagi.
              setState(() => _mengunduh = false);
            case OtaStatus.CHECKSUM_ERROR:
              _gagal('File unduhan rusak. Coba lagi.');
            case OtaStatus.PERMISSION_NOT_GRANTED_ERROR:
              _gagal(
                'Izinkan "Pasang aplikasi tidak dikenal" untuk aplikasi ini, lalu coba lagi.',
              );
            default:
              _gagal('Gagal memperbarui. Periksa koneksi lalu coba lagi.');
          }
        });
  }

  void _gagal(String pesan) => setState(() {
    _mengunduh = false;
    _galat = pesan;
  });

  Future<void> _nanti() async {
    final p = await SharedPreferences.getInstance();
    await p.setInt(_kunciDilewati, widget.info.versionCode);
    if (mounted) Navigator.of(context).pop();
  }

  @override
  Widget build(BuildContext context) {
    return PopScope(
      canPop: !widget.wajib,
      child: AlertDialog(
        title: Text('Versi ${widget.info.versionName} tersedia'),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            if (widget.info.catatan.isNotEmpty) Text(widget.info.catatan),
            if (widget.wajib)
              const Padding(
                padding: EdgeInsets.only(top: 8),
                child: Text('Pembaruan ini wajib untuk melanjutkan.'),
              ),
            if (_mengunduh)
              Padding(
                padding: const EdgeInsets.only(top: 12),
                child: LinearProgressIndicator(value: _persen / 100),
              ),
            if (_galat != null)
              Padding(
                padding: const EdgeInsets.only(top: 8),
                child: Text(
                  _galat!,
                  style: TextStyle(color: Theme.of(context).colorScheme.error),
                ),
              ),
          ],
        ),
        actions: [
          if (!widget.wajib && !_mengunduh)
            TextButton(onPressed: _nanti, child: const Text('Nanti')),
          FilledButton(
            onPressed: _mengunduh ? null : _mulai,
            child: Text(_mengunduh ? 'Mengunduh $_persen%' : 'Perbarui'),
          ),
        ],
      ),
    );
  }
}
