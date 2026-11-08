import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/api.dart';
import '../../core/models.dart';
import '../../core/providers.dart';
import '../../core/validasi.dart';
import '../profil/profil_aksi.dart';
import '../../core/widgets.dart';

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
  final _q = TextEditingController();
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
    _q.dispose();
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

  Future<void> _tampilPassword(String judul, String pw) => showDialog<void>(
    context: context,
    barrierDismissible: false,
    builder: (c) => AlertDialog(
      title: Text(judul),
      content: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Text(
            'Password sementara (hanya tampil sekali). User wajib menggantinya saat login pertama.',
          ),
          const SizedBox(height: 12),
          SelectableText(pw, style: Theme.of(c).textTheme.headlineSmall),
        ],
      ),
      actions: [
        TextButton.icon(
          onPressed: () {
            Clipboard.setData(ClipboardData(text: pw));
            ScaffoldMessenger.of(
              context,
            ).showSnackBar(const SnackBar(content: Text('Password tersalin')));
          },
          icon: const Icon(Icons.copy, size: 18),
          label: const Text('Salin'),
        ),
        FilledButton(
          onPressed: () => Navigator.pop(c),
          child: const Text('Sudah dicatat'),
        ),
      ],
    ),
  );

  Future<T> _jalan<T>(Future<T> Function() f) async {
    try {
      return await f();
    } on ApiException catch (e) {
      if (mounted) _pesan(e.pesan);
      rethrow;
    }
  }

  /// Dialog buat akun (validasi inline, tetap terbuka bila server menolak) lalu tampilkan password sementara.
  Future<void> _tampilDialogBuat(_BuatAkunDialog dialog) async {
    final pw = await showDialog<String>(
      context: context,
      barrierDismissible: false,
      builder: (_) => dialog,
    );
    if (pw == null || !mounted) return;
    await _muat();
    if (mounted) await _tampilPassword('Akun dibuat', pw);
  }

  Future<void> _buatSistem() => _tampilDialogBuat(
    _BuatAkunDialog(
      judul: 'User sistem baru',
      pakaiNama: true,
      pakaiRole: true,
      kirim: (u, n, r) => ref
          .read(apiProvider)
          .buatUser(username: u, nama: n, role: r),
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
        usernameBantu: 'Default: NIK anggota',
        kirim: (u, _, _) => ref
            .read(apiProvider)
            .buatUser(username: u, nama: a.nama, role: 'ANGGOTA', nik: a.nik),
      ),
    );
  }

  Widget _kartu(UserInfo u) => Card(
    margin: const EdgeInsets.only(bottom: 8),
    child: ListTile(
      contentPadding: const EdgeInsets.fromLTRB(16, 8, 8, 8),
      leading: UserAvatar(id: u.id, nama: u.nama, fotoVersi: u.fotoVersi),
      title: Text(
        '${u.nama} (${u.username})',
        style: const TextStyle(fontWeight: FontWeight.w600),
      ),
      subtitle: Padding(
        padding: const EdgeInsets.only(top: 6),
        child: Wrap(
          spacing: 6,
          runSpacing: 6,
          crossAxisAlignment: WrapCrossAlignment.center,
          children: [
            StatePill(text: u.role),
            if (u.nik != null)
              Text(u.nik!, style: Theme.of(context).textTheme.bodySmall),
            if (!u.aktif)
              const StatePill(text: 'NONAKTIF', color: Color(0xFF78817B)),
            if (u.terkunci)
              const StatePill(text: 'TERKUNCI', color: Color(0xFFD68A00)),
          ],
        ),
      ),
      trailing: PopupMenuButton<String>(
        onSelected: (a) async {
          try {
            if (a == 'reset') {
              final pw = await _jalan(
                () => ref.read(apiProvider).resetPassword(u.id),
              );
              if (mounted) await _tampilPassword('Password direset', pw);
            } else if (a == 'aktif') {
              await _jalan(
                () => ref.read(apiProvider).setAktif(u.id, !u.aktif),
              );
              await _muat();
            }
          } on ApiException {
            // Pesan sudah ditampilkan.
          }
        },
        itemBuilder: (_) => [
          const PopupMenuItem(value: 'reset', child: Text('Reset password')),
          PopupMenuItem(
            value: 'aktif',
            child: Text(u.aktif ? 'Nonaktifkan' : 'Aktifkan'),
          ),
        ],
      ),
    ),
  );

  Widget _daftar(List<UserInfo> items, String kosong) => RefreshIndicator(
    onRefresh: _muat,
    child: ListView(
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.fromLTRB(16, 8, 16, 20),
      children: [
        if (_loading)
          const Padding(
            padding: EdgeInsets.all(40),
            child: Center(child: CircularProgressIndicator()),
          ),
        if (_error != null)
          Card(
            color: Theme.of(context).colorScheme.errorContainer,
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                children: [
                  Text(_error!),
                  TextButton(onPressed: _muat, child: const Text('Coba lagi')),
                ],
              ),
            ),
          ),
        if (!_loading && _error == null && items.isEmpty)
          Padding(
            padding: const EdgeInsets.all(36),
            child: Center(child: Text(kosong)),
          ),
        for (final u in items) _kartu(u),
      ],
    ),
  );

  @override
  Widget build(BuildContext context) {
    final sistem = [
      for (final u in _users)
        if (u.role != 'ANGGOTA') u,
    ];
    final semuaAnggota = [
      for (final u in _users)
        if (u.role == 'ANGGOTA') u,
    ];
    final q = _q.text.trim().toLowerCase();
    final anggota = q.isEmpty
        ? semuaAnggota
        : [
            for (final u in semuaAnggota)
              if ('${u.nama} ${u.nik ?? ''} ${u.username}'
                  .toLowerCase()
                  .contains(q))
                u,
          ];
    final tabSistem = _tab.index == 0;
    return TropicalScaffold(
      title: 'Admin · User Mobile',
      actions: [
        const ProfilAksi(),
        IconButton(
          tooltip: 'Keluar',
          icon: const Icon(Icons.logout, color: Colors.white),
          onPressed: () => ref.read(authProvider.notifier).logout(),
        ),
      ],
      bottomNavigationBar: SafeArea(
        top: false,
        child: Padding(
          padding: const EdgeInsets.fromLTRB(16, 8, 16, 12),
          child: GradientActionButton(
            label: tabSistem ? 'User sistem' : 'Akun anggota',
            icon: tabSistem ? Icons.manage_accounts : Icons.person_add_alt_1,
            onPressed: tabSistem ? _buatSistem : _buatAnggota,
          ),
        ),
      ),
      body: Column(
        children: [
          TabBar(
            controller: _tab,
            tabs: [
              Tab(text: 'Sistem (${sistem.length})'),
              Tab(text: 'Anggota (${semuaAnggota.length})'),
            ],
          ),
          Expanded(
            child: TabBarView(
              controller: _tab,
              children: [
                _daftar(sistem, 'Belum ada user sistem.'),
                Column(
                  children: [
                    Padding(
                      padding: const EdgeInsets.fromLTRB(16, 12, 16, 0),
                      child: TextField(
                        controller: _q,
                        onChanged: (_) => setState(() {}),
                        decoration: const InputDecoration(
                          labelText: 'Cari nama atau NIK',
                          prefixIcon: Icon(Icons.search),
                        ),
                      ),
                    ),
                    Expanded(
                      child: _daftar(
                        anggota,
                        q.isEmpty
                            ? 'Belum ada akun anggota.'
                            : 'Tidak ada akun anggota yang cocok.',
                      ),
                    ),
                  ],
                ),
              ],
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
      return const Center(child: CircularProgressIndicator());
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
    this.usernameBantu = 'Huruf kecil, angka, titik, garis bawah, atau strip (3-50)',
    this.pakaiNama = false,
    this.pakaiRole = false,
  });
  final String judul;
  final Widget? atas;
  final String usernameAwal, usernameBantu;
  final bool pakaiNama, pakaiRole;
  final Future<String> Function(String username, String nama, String role) kirim;

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
    setState(() {
      _busy = true;
      _galatServer = null;
    });
    try {
      final pw = await widget.kirim(
        _u.text.trim().toLowerCase(),
        _n.text.trim(),
        _role,
      );
      if (mounted) Navigator.pop(context, pw);
    } on ApiException catch (e) {
      if (mounted) setState(() => _galatServer = e.pesan);
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    return AlertDialog(
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
                onChanged: _busy ? null : (v) => setState(() => _role = v ?? 'KASIR'),
                decoration: const InputDecoration(labelText: 'Role'),
              ),
            ],
            if (_galatServer != null)
              Padding(
                padding: const EdgeInsets.only(top: 12),
                child: Text(
                  _galatServer!,
                  style: TextStyle(color: cs.error),
                ),
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
    );
  }
}
