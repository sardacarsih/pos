import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:image_picker/image_picker.dart';
import 'package:package_info_plus/package_info_plus.dart';

import '../../core/api.dart';
import '../../core/providers.dart';
import '../../core/update.dart';
import '../../core/widgets.dart';
import '../admin/admin_produk_page.dart' show fotoPersegi;
import 'foto_crop_page.dart';

/// Profil akun sendiri (semua role): foto, nama tampilan, kontak, dan ganti password.
class ProfilPage extends ConsumerStatefulWidget {
  const ProfilPage({super.key});
  @override
  ConsumerState<ProfilPage> createState() => _ProfilPageState();
}

class _ProfilPageState extends ConsumerState<ProfilPage> {
  final _nama = TextEditingController(),
      _telepon = TextEditingController(),
      _email = TextEditingController();
  final _lama = TextEditingController(),
      _baru = TextEditingController(),
      _ulang = TextEditingController();
  String _info = '';
  bool _loading = true, _busy = false, _fotoSibuk = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    _muat();
  }

  @override
  void dispose() {
    for (final c in [_nama, _telepon, _email, _lama, _baru, _ulang]) {
      c.dispose();
    }
    super.dispose();
  }

  void _pesan(String t) =>
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(t)));

  Future<void> _muat() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      // Tanpa invalidate di sini (dipanggil dari initState): pakai data cache dari header, sudah di-invalidate saat ubah.
      final j = await ref.read(profilSayaProvider.future);
      if (!mounted) return;
      setState(() {
        _nama.text = (j['nama'] as String?) ?? '';
        _telepon.text = (j['telepon'] as String?) ?? '';
        _email.text = (j['email'] as String?) ?? '';
        _info =
            '${j['username']} · ${j['role']}${j['nik'] != null ? ' · NIK ${j['nik']}' : ''}';
      });
    } on ApiException catch (e) {
      if (mounted) setState(() => _error = e.pesan);
    } catch (_) {
      if (mounted) setState(() => _error = 'Profil gagal dimuat');
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _simpan() async {
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      await ref
          .read(apiProvider)
          .ubahProfil(
            nama: _nama.text,
            telepon: _telepon.text,
            email: _email.text,
          );
      ref.invalidate(profilSayaProvider); // avatar & inisial ikut berubah
      if (mounted) _pesan('Profil disimpan');
    } on ApiException catch (e) {
      if (mounted) setState(() => _error = e.pesan);
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<void> _pilihFoto(bool adaFoto) async {
    final aksi = await showModalBottomSheet<String>(
      context: context,
      builder: (c) => SafeArea(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            ListTile(
              leading: const Icon(Icons.photo_library_outlined),
              title: const Text('Pilih dari galeri'),
              onTap: () => Navigator.pop(c, 'galeri'),
            ),
            ListTile(
              leading: const Icon(Icons.photo_camera_outlined),
              title: const Text('Ambil dengan kamera'),
              onTap: () => Navigator.pop(c, 'kamera'),
            ),
            if (adaFoto)
              ListTile(
                leading: Icon(
                  Icons.delete_outline,
                  color: Theme.of(context).colorScheme.error,
                ),
                title: const Text('Hapus foto'),
                onTap: () => Navigator.pop(c, 'hapus'),
              ),
          ],
        ),
      ),
    );
    if (!mounted || aksi == null) return;
    if (aksi == 'hapus') return _hapusFoto();
    await _gantiFoto(aksi == 'kamera' ? ImageSource.camera : ImageSource.gallery);
  }

  Future<void> _gantiFoto(ImageSource sumber) async {
    try {
      // Dibatasi 2048 px saat dipilih agar layar crop ringan di memori.
      final x = await ImagePicker().pickImage(
        source: sumber,
        maxWidth: 2048,
        maxHeight: 2048,
        imageQuality: 90,
      );
      if (x == null || !mounted) return;
      final asli = await x.readAsBytes();
      if (!mounted) return;
      final dipotong = await Navigator.of(context).push<Uint8List>(
        MaterialPageRoute(builder: (_) => FotoCropPage(image: asli)),
      );
      if (dipotong == null || !mounted) return;
      setState(() => _fotoSibuk = true);
      final hasil = await compute(fotoPersegi, dipotong);
      if (hasil.bytes == null) {
        if (mounted) _pesan(hasil.galat ?? 'Berkas bukan gambar yang valid');
        return;
      }
      await ref.read(apiProvider).unggahFotoProfil(hasil.bytes!);
      ref.invalidate(profilSayaProvider); // versi baru -> semua avatar ikut berganti
      if (mounted) _pesan('Foto diperbarui');
    } on ApiException catch (e) {
      if (mounted) _pesan(e.pesan);
    } on PlatformException {
      if (mounted) {
        _pesan(
          'Tidak bisa mengakses kamera/galeri. Izinkan aksesnya di Pengaturan perangkat.',
        );
      }
    } catch (_) {
      if (mounted) _pesan('Foto tidak dapat diproses. Coba foto lain.');
    } finally {
      if (mounted) setState(() => _fotoSibuk = false);
    }
  }

  Future<void> _hapusFoto() async {
    final ya = await showDialog<bool>(
      context: context,
      builder: (c) => AlertDialog(
        title: const Text('Hapus foto profil?'),
        content: const Text('Avatar akan kembali memakai inisial nama.'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(c, false),
            child: const Text('Batal'),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(c, true),
            child: const Text('Hapus'),
          ),
        ],
      ),
    );
    if (ya != true || !mounted) return;
    setState(() => _fotoSibuk = true);
    try {
      await ref.read(apiProvider).hapusFotoProfil();
      ref.invalidate(profilSayaProvider);
      if (mounted) _pesan('Foto dihapus');
    } on ApiException catch (e) {
      if (mounted) _pesan(e.pesan);
    } finally {
      if (mounted) setState(() => _fotoSibuk = false);
    }
  }

  Future<void> _gantiPassword() async {
    if (_baru.text.length < 8) {
      return setState(() => _error = 'Password baru minimal 8 karakter');
    }
    if (_baru.text != _ulang.text) {
      return setState(() => _error = 'Konfirmasi password tidak sama');
    }
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      await ref.read(apiProvider).gantiPassword(_lama.text, _baru.text);
      if (!mounted) return;
      _pesan('Password diganti. Silakan masuk kembali.');
      Navigator.of(context).pop();
      ref.read(authProvider.notifier).sesiBerakhir(); // semua sesi dicabut server
    } on ApiException catch (e) {
      if (mounted) setState(() => _error = e.pesan);
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Widget _field(
    TextEditingController c,
    String label, {
    IconData? icon,
    TextInputType? type,
    bool rahasia = false,
  }) => Padding(
    padding: const EdgeInsets.only(bottom: 12),
    child: TextField(
      controller: c,
      keyboardType: type,
      obscureText: rahasia,
      decoration: InputDecoration(
        labelText: label,
        prefixIcon: icon == null ? null : Icon(icon),
        border: const OutlineInputBorder(),
      ),
    ),
  );

  Widget _avatarBesar(bool adaFoto) {
    final cs = Theme.of(context).colorScheme;
    return Center(
      child: Semantics(
        button: true,
        label: 'Ubah foto profil',
        child: InkWell(
          customBorder: const CircleBorder(),
          onTap: _fotoSibuk || _busy ? null : () => _pilihFoto(adaFoto),
          child: Stack(
            alignment: Alignment.center,
            children: [
              const ProfilAvatar(radius: 60),
              if (_fotoSibuk)
                const SizedBox(
                  width: 124,
                  height: 124,
                  child: CircularProgressIndicator(strokeWidth: 4),
                ),
              Positioned(
                right: 0,
                bottom: 0,
                child: CircleAvatar(
                  radius: 18,
                  backgroundColor: cs.primary,
                  child: Icon(Icons.photo_camera, size: 18, color: cs.onPrimary),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    final adaFoto = ref.watch(profilSayaProvider).value?['foto_versi'] != null;
    return TropicalScaffold(
      title: 'Profil Saya',
      leading: IconButton(
        tooltip: 'Kembali',
        icon: const Icon(Icons.arrow_back, color: Colors.white),
        onPressed: () => Navigator.of(context).pop(),
      ),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : ListView(
              padding: const EdgeInsets.fromLTRB(16, 8, 16, 24),
              children: [
                _avatarBesar(adaFoto),
                const SizedBox(height: 8),
                Center(
                  child: Text(
                    adaFoto ? 'Ketuk foto untuk mengubah' : 'Ketuk untuk menambah foto',
                    style: Theme.of(context).textTheme.bodySmall,
                  ),
                ),
                const SizedBox(height: 4),
                Center(child: Text(_info)),
                const SizedBox(height: 16),
                _field(_nama, 'Nama tampilan', icon: Icons.badge_outlined),
                _field(
                  _telepon,
                  'Telepon (opsional)',
                  icon: Icons.phone_outlined,
                  type: TextInputType.phone,
                ),
                _field(
                  _email,
                  'Email (opsional)',
                  icon: Icons.email_outlined,
                  type: TextInputType.emailAddress,
                ),
                if (_error != null)
                  Padding(
                    padding: const EdgeInsets.only(bottom: 12),
                    child: Text(_error!, style: TextStyle(color: cs.error)),
                  ),
                FilledButton(
                  onPressed: _busy ? null : _simpan,
                  child: const Text('Simpan profil'),
                ),
                const SizedBox(height: 16),
                ExpansionTile(
                  title: const Text('Ganti password'),
                  leading: const Icon(Icons.lock_outline),
                  childrenPadding: const EdgeInsets.only(top: 8),
                  children: [
                    _field(_lama, 'Password lama', rahasia: true),
                    _field(_baru, 'Password baru (min. 8 karakter)', rahasia: true),
                    _field(_ulang, 'Ulangi password baru', rahasia: true),
                    FilledButton.tonal(
                      onPressed: _busy ? null : _gantiPassword,
                      child: const Text('Ganti password'),
                    ),
                  ],
                ),
                if (!kIsWeb)
                  ListTile(
                    leading: const Icon(Icons.system_update_outlined),
                    title: const Text('Cek pembaruan'),
                    subtitle: FutureBuilder<PackageInfo>(
                      future: PackageInfo.fromPlatform(),
                      builder: (_, s) => Text(
                        s.hasData
                            ? 'Versi terpasang ${s.data!.version} (build ${s.data!.buildNumber})'
                            : '',
                      ),
                    ),
                    onTap: () => tawarkanUpdate(context, manual: true),
                  ),
              ],
            ),
    );
  }
}
