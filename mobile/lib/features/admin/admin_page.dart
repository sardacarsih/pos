import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/api.dart';
import '../../core/models.dart';
import '../../core/providers.dart';
import '../../core/validasi.dart';
import '../../core/widgets.dart';
import '../profil/profil_aksi.dart';
import 'daftar_akun.dart';

typedef _PasswordAkun = ({String username, String password});

class AdminPage extends ConsumerStatefulWidget {
  const AdminPage({super.key});
  @override
  ConsumerState<AdminPage> createState() => _AdminPageState();
}

class _AdminPageState extends ConsumerState<AdminPage>
    with SingleTickerProviderStateMixin {
  late final _tab = TabController(length: 2, vsync: this)
    ..addListener(() {
      if (mounted) setState(() {});
    });
  bool _memproses = false;
  List<UserInfo> _users = [];
  bool _loading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _muat();
  }

  @override
  void dispose() {
    _tab.dispose();
    super.dispose();
  }

  void _pesan(String m) =>
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(m)));

  Future<void> _muat() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final u = await ref.read(apiProvider).users();
      if (mounted) setState(() => _users = u);
    } on ApiException catch (e) {
      if (mounted) setState(() => _error = e.pesan);
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _tampilPassword(_PasswordAkun akun) => showDialog<void>(
    context: context,
    barrierDismissible: false,
    builder: (c) => PopScope(
      canPop: false,
      child: AlertDialog(
        scrollable: true,
        icon: const Icon(Icons.key_outlined),
        title: const Text('Password sementara'),
        content: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(akun.username, style: Theme.of(c).textTheme.titleMedium),
            const SizedBox(height: 8),
            const Text(
              'Password ini hanya ditampilkan sekali. Berikan kepada pengguna. Saat masuk, pengguna wajib membuat password baru sebelum dapat menggunakan aplikasi.',
            ),
            const SizedBox(height: 12),
            SelectableText(
              akun.password,
              style: Theme.of(c).textTheme.headlineSmall,
            ),
          ],
        ),
        actions: [
          TextButton.icon(
            onPressed: () async {
              try {
                await Clipboard.setData(ClipboardData(text: akun.password));
                if (c.mounted) _pesan('Password tersalin');
              } on PlatformException {
                if (c.mounted) {
                  _pesan('Pilih teks password dan salin secara manual.');
                }
              }
            },
            icon: const Icon(Icons.copy_outlined),
            label: const Text('Salin password'),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(c),
            child: const Text('Sudah dicatat'),
          ),
        ],
      ),
    ),
  );

  /// Dialog buat akun (validasi inline, tetap terbuka bila server menolak) lalu tampilkan password sementara.
  Future<void> _tampilDialogBuat(_BuatAkunDialog dialog) async {
    final akun = await showDialog<_PasswordAkun>(
      context: context,
      barrierDismissible: false,
      builder: (_) => dialog,
    );
    if (akun == null || !mounted) return;
    await _tampilPassword(akun);
    if (mounted) await _muat();
  }

  Future<void> _buatSistem() => _tampilDialogBuat(
    _BuatAkunDialog(
      judul: 'Tambah pengguna sistem',
      pakaiNama: true,
      pakaiRole: true,
      kirim: (u, n, r) =>
          ref.read(apiProvider).buatUser(username: u, nama: n, role: r),
    ),
  );

  Future<void> _buatAnggota() async {
    final a = await showDialog<Pelanggan>(
      context: context,
      builder: (_) => AnggotaPickerDialog(api: ref.read(apiProvider)),
    );
    if (a == null || !mounted) return;
    await _tampilDialogBuat(
      _BuatAkunDialog(
        judul: 'Akun anggota',
        atas: Card(
          margin: EdgeInsets.zero,
          child: ListTile(
            leading: AvatarFoto(nama: a.nama),
            title: Text(a.nama),
            subtitle: Text(
              '${a.nik}\n${a.unit.isEmpty ? 'Unit kerja tidak tersedia' : a.unit}',
            ),
            isThreeLine: true,
          ),
        ),
        usernameAwal: usernameDariNik(a.nik),
        usernameBantu: 'Username akun anggota = NIK (tidak dapat diubah)',
        usernameTerkunci: true,
        kirim: (u, _, _) => ref
            .read(apiProvider)
            .buatUser(username: u, nama: a.nama, role: 'ANGGOTA', nik: a.nik),
      ),
    );
  }

  Future<void> _tindakan(UserInfo user, TindakanAkun tindakan) async {
    final reset = tindakan == TindakanAkun.reset;
    final sendiri = user.id == ref.read(authProvider).profil?.id;
    if (_memproses || _loading || (!reset && user.aktif && sendiri)) return;
    setState(() => _memproses = true);
    final label = reset
        ? 'Reset password'
        : user.aktif
        ? 'Nonaktifkan'
        : 'Aktifkan';
    final dampak = reset
        ? 'Sesi login akan dicabut. Pengguna wajib mengganti password sementara saat masuk.'
        : user.aktif
        ? 'Sesi login akan dicabut. Akses ditutup sampai akun diaktifkan kembali.'
        : 'Pengguna dapat masuk kembali jika akun tidak terkunci.';
    String? password;
    try {
      bool mengirim = false;
      String? galat;
      final selesai = await showDialog<bool>(
        context: context,
        barrierDismissible: false,
        builder: (c) => StatefulBuilder(
          builder: (c, ubah) {
            Future<void> kirim() async {
              if (mengirim) return;
              ubah(() {
                mengirim = true;
                galat = null;
              });
              try {
                final api = ref.read(apiProvider);
                if (reset) {
                  password = await api.resetPassword(user.id);
                } else {
                  await api.setAktif(user.id, !user.aktif);
                }
                if (c.mounted) Navigator.pop(c, true);
              } on ApiException catch (e) {
                if (c.mounted) {
                  ubah(() {
                    mengirim = false;
                    galat = e.pesan;
                  });
                }
              }
            }

            return PopScope(
              canPop: !mengirim,
              child: AlertDialog(
                scrollable: true,
                icon: Icon(
                  reset ? Icons.key_outlined : Icons.power_settings_new,
                ),
                title: Text('$label?'),
                content: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text('${user.nama} (@${user.username})\n\n$dampak'),
                    if (galat != null)
                      Padding(
                        padding: const EdgeInsets.only(top: 12),
                        child: Semantics(
                          liveRegion: true,
                          child: Text(
                            galat!,
                            style: TextStyle(
                              color: Theme.of(c).colorScheme.error,
                            ),
                          ),
                        ),
                      ),
                  ],
                ),
                actions: [
                  TextButton(
                    onPressed: mengirim ? null : () => Navigator.pop(c, false),
                    child: const Text('Batal'),
                  ),
                  FilledButton(
                    onPressed: mengirim ? null : kirim,
                    child: Text(mengirim ? 'Memproses…' : label),
                  ),
                ],
              ),
            );
          },
        ),
      );
      if (selesai != true || !mounted) return;
      setState(() {
        _users = [
          for (final item in _users)
            if (item.id != user.id)
              item
            else if (reset)
              UserInfo.withAccess(item, terkunci: false)
            else
              UserInfo.withAccess(item, aktif: !user.aktif),
        ];
      });
      final sementara = password;
      if (sementara != null) {
        await _tampilPassword((username: user.username, password: sementara));
      } else {
        _pesan(
          'Akun ${user.username} ${user.aktif ? 'dinonaktifkan' : 'diaktifkan'}.',
        );
      }
    } finally {
      if (mounted) setState(() => _memproses = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final sistem = _users.where((u) => u.role != 'ANGGOTA').toList();
    final anggota = _users.where((u) => u.role == 'ANGGOTA').toList();
    final tabSistem = _tab.index == 0;
    final sibuk = _loading || _memproses;
    final idSendiri = ref.watch(authProvider.select((a) => a.profil?.id));
    Widget daftar(List<UserInfo> users, String jenis) => DaftarAkun(
      users: users,
      jenis: jenis,
      idSendiri: idSendiri,
      loading: _loading,
      error: _error,
      onRefresh: sibuk ? null : _muat,
      onTindakan: sibuk ? null : _tindakan,
    );
    return TropicalScaffold(
      title: 'Pengguna',
      actions: [
        const ProfilAksi(),
        IconButton(
          tooltip: 'Keluar',
          icon: const Icon(Icons.logout, color: Colors.white),
          onPressed: () => ref.read(authProvider.notifier).logout(),
        ),
      ],
      bottomNavigationBar: MediaQuery.viewInsetsOf(context).bottom > 0
          ? null
          : SafeArea(
              top: false,
              child: Padding(
                padding: const EdgeInsets.fromLTRB(16, 8, 16, 12),
                child: GradientActionButton(
                  label: tabSistem ? 'Tambah pengguna' : 'Tambah akun anggota',
                  icon: Icons.person_add_alt_1,
                  onPressed: sibuk
                      ? null
                      : tabSistem
                      ? _buatSistem
                      : _buatAnggota,
                ),
              ),
            ),
      body: Column(
        children: [
          TabBar(
            controller: _tab,
            tabs: [
              Tab(text: 'Sistem (${sistem.length})'),
              Tab(text: 'Anggota (${anggota.length})'),
            ],
          ),
          Expanded(
            child: TabBarView(
              controller: _tab,
              children: [daftar(sistem, 'sistem'), daftar(anggota, 'anggota')],
            ),
          ),
        ],
      ),
    );
  }
}

class AnggotaPickerDialog extends ConsumerStatefulWidget {
  const AnggotaPickerDialog({super.key, required this.api});

  final ApiClient api;

  @override
  ConsumerState<AnggotaPickerDialog> createState() =>
      _AnggotaPickerDialogState();
}

class _AnggotaPickerDialogState extends ConsumerState<AnggotaPickerDialog> {
  final _query = TextEditingController();
  final _scroll = ScrollController();
  final List<Pelanggan> _items = [];
  bool _loading = false;
  bool _lagi = false;
  String? _error;
  int _page = 1;
  int _requestId = 0;

  @override
  void initState() {
    super.initState();
    // Sisa anggota dimuat otomatis saat daftar hampir habis; tombol di bawah daftar jadi cadangan.
    _scroll.addListener(() {
      if (_lagi &&
          !_loading &&
          _error == null &&
          _scroll.position.pixels > _scroll.position.maxScrollExtent - 200) {
        _cari(reset: false);
      }
    });
    _cari(reset: true);
  }

  Timer? _debounce;

  @override
  void dispose() {
    _debounce?.cancel();
    _query.dispose();
    _scroll.dispose();
    super.dispose();
  }

  Future<void> _cari({required bool reset}) async {
    final requestId = ++_requestId;
    final page = reset ? 1 : _page + 1;
    setState(() {
      _loading = true;
      _error = null;
      if (reset) {
        _items.clear();
        _page = 0;
        _lagi = false;
      }
    });
    try {
      final result = await widget.api.pelanggan(
        q: _query.text.trim(),
        page: page,
        belumAkun: true, // hanya yang belum ada di daftar user
      );
      if (!mounted || requestId != _requestId) return;
      setState(() {
        _items.addAll(result.items);
        _page = page;
        _lagi = result.lagi;
      });
    } on ApiException catch (e) {
      if (mounted && requestId == _requestId) {
        setState(() => _error = e.pesan);
      }
    } catch (_) {
      if (mounted && requestId == _requestId) {
        setState(() => _error = 'Daftar anggota gagal dimuat. Coba lagi.');
      }
    } finally {
      if (mounted && requestId == _requestId) {
        setState(() => _loading = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) => AlertDialog(
    title: const Text('Pilih anggota'),
    content: SizedBox(
      width: double.maxFinite,
      height: 380,
      child: Column(
        children: [
          TextField(
            controller: _query,
            textInputAction: TextInputAction.search,
            onChanged: (_) {
              _debounce?.cancel();
              _debounce = Timer(
                const Duration(milliseconds: 350),
                () => _cari(reset: true),
              );
            },
            onSubmitted: (_) => _cari(reset: true),
            decoration: InputDecoration(
              labelText: 'Cari nama atau NIK',
              prefixIcon: const Icon(Icons.search),
              suffixIcon: IconButton(
                tooltip: 'Cari',
                onPressed: _loading ? null : () => _cari(reset: true),
                icon: const Icon(Icons.arrow_forward),
              ),
            ),
          ),
          const SizedBox(height: 8),
          Expanded(child: _isiDaftar()),
        ],
      ),
    ),
    actions: [
      TextButton(
        onPressed: () => Navigator.pop(context),
        child: const Text('Batal'),
      ),
    ],
  );

  Widget _isiDaftar() {
    if (_loading && _items.isEmpty) {
      return const LoadingKopkar();
    }
    if (_error != null) {
      return Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Text(_error!, textAlign: TextAlign.center),
            TextButton(
              onPressed: () => _cari(reset: _items.isEmpty),
              child: const Text('Coba lagi'),
            ),
          ],
        ),
      );
    }
    if (_items.isEmpty) {
      return Center(
        child: Text(
          _query.text.trim().isEmpty
              ? 'Semua anggota sudah punya akun.'
              : 'Anggota tidak ditemukan.',
          textAlign: TextAlign.center,
        ),
      );
    }
    return ListView.builder(
      controller: _scroll,
      // Satu baris tambahan berisi indikator saat halaman berikutnya dimuat otomatis (tanpa tombol).
      itemCount: _items.length + (_lagi ? 1 : 0),
      itemBuilder: (context, index) {
        if (index >= _items.length) {
          return const Padding(
            padding: EdgeInsets.all(12),
            child: Center(
              child: SizedBox.square(
                dimension: 20,
                child: CircularProgressIndicator(strokeWidth: 2),
              ),
            ),
          );
        }
        final anggota = _items[index];
        return ListTile(
          contentPadding: EdgeInsets.zero,
          leading: AvatarFoto(nama: anggota.nama),
          title: Text(anggota.nama),
          subtitle: Text(
            '${anggota.nik}\n${anggota.unit.isEmpty ? 'Unit kerja tidak tersedia' : anggota.unit}',
          ),
          isThreeLine: true,
          onTap: () => Navigator.pop(context, anggota),
        );
      },
    );
  }
}

/// Dialog buat akun mobile: validasi inline (aturan sama dengan server), tombol Buat nonaktif selama data
/// belum valid atau sedang mengirim, dan dialog tetap terbuka dengan pesan server bila permintaan ditolak
/// (mis. "Username sudah dipakai"). Menutup dengan password sementara saat sukses.
class _BuatAkunDialog extends StatefulWidget {
  const _BuatAkunDialog({
    required this.judul,
    required this.kirim,
    this.atas,
    this.usernameAwal = '',
    this.usernameBantu =
        'Huruf kecil, angka, titik, garis bawah, atau strip (3-50)',
    this.usernameTerkunci = false,
    this.pakaiNama = false,
    this.pakaiRole = false,
  });
  final bool usernameTerkunci;
  final String judul;
  final Widget? atas;
  final String usernameAwal, usernameBantu;
  final bool pakaiNama, pakaiRole;
  final Future<String> Function(String username, String nama, String role)
  kirim;

  @override
  State<_BuatAkunDialog> createState() => _BuatAkunDialogState();
}

class _BuatAkunDialogState extends State<_BuatAkunDialog> {
  late final _u = TextEditingController(text: widget.usernameAwal);
  final _n = TextEditingController();
  var _role = 'KASIR';
  var _sentuhU = false, _sentuhN = false, _busy = false;
  String? _galatServer;

  @override
  void dispose() {
    _u.dispose();
    _n.dispose();
    super.dispose();
  }

  String? get _galatU => galatUsername(_u.text);
  String? get _galatN => widget.pakaiNama ? galatNama(_n.text) : null;
  bool get _valid => _galatU == null && _galatN == null;

  Future<void> _buat() async {
    if (_busy || !_valid) return;
    final username = _u.text.trim().toLowerCase();
    setState(() {
      _busy = true;
      _galatServer = null;
    });
    try {
      final pw = await widget.kirim(username, _n.text.trim(), _role);
      if (mounted) Navigator.pop(context, (username: username, password: pw));
    } on ApiException catch (e) {
      if (mounted) setState(() => _galatServer = e.pesan);
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    return PopScope(
      canPop: !_busy,
      child: AlertDialog(
        title: Text(widget.judul),
        content: SingleChildScrollView(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              if (widget.atas != null) ...[
                widget.atas!,
                const SizedBox(height: 12),
              ],
              TextField(
                controller: _u,
                enabled: !_busy,
                readOnly: widget.usernameTerkunci,
                autocorrect: false,
                onChanged: (_) => setState(() {
                  _sentuhU = true;
                  _galatServer = null;
                }),
                decoration: InputDecoration(
                  labelText: 'Username',
                  helperText: widget.usernameBantu,
                  helperMaxLines: 2,
                  errorText: _sentuhU ? _galatU : null,
                  errorMaxLines: 2,
                ),
              ),
              if (widget.pakaiNama) ...[
                const SizedBox(height: 12),
                TextField(
                  controller: _n,
                  enabled: !_busy,
                  onChanged: (_) => setState(() {
                    _sentuhN = true;
                    _galatServer = null;
                  }),
                  decoration: InputDecoration(
                    labelText: 'Nama',
                    errorText: _sentuhN ? _galatN : null,
                  ),
                ),
              ],
              if (widget.pakaiRole) ...[
                const SizedBox(height: 12),
                DropdownButtonFormField<String>(
                  initialValue: _role,
                  items: [
                    for (final r in ['KASIR', 'ADMIN'])
                      DropdownMenuItem(value: r, child: Text(r)),
                  ],
                  onChanged: _busy
                      ? null
                      : (v) => setState(() => _role = v ?? 'KASIR'),
                  decoration: const InputDecoration(labelText: 'Role'),
                ),
              ],
              if (_galatServer != null)
                Padding(
                  padding: const EdgeInsets.only(top: 12),
                  child: Text(_galatServer!, style: TextStyle(color: cs.error)),
                ),
            ],
          ),
        ),
        actions: [
          TextButton(
            onPressed: _busy ? null : () => Navigator.pop(context),
            child: const Text('Batal'),
          ),
          FilledButton(
            onPressed: _valid && !_busy ? _buat : null,
            child: _busy
                ? const SizedBox.square(
                    dimension: 18,
                    child: CircularProgressIndicator(strokeWidth: 2),
                  )
                : const Text('Buat'),
          ),
        ],
      ),
    );
  }
}
