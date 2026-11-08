import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/api.dart';
import '../../core/models.dart';
import '../../core/providers.dart';
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

  /// Dialog menutup dengan animasi dan masih memakai controller; dispose langsung memicu assertion.
  void _tundaDispose(List<TextEditingController> cs) =>
      Future.delayed(const Duration(milliseconds: 500), () {
        for (final c in cs) {
          c.dispose();
        }
      });

  Future<void> _kirim({
    required String username,
    required String nama,
    required String role,
    String? nik,
  }) async {
    try {
      final pw = await _jalan(
        () => ref
            .read(apiProvider)
            .buatUser(username: username, nama: nama, role: role, nik: nik),
      );
      await _muat();
      if (mounted) await _tampilPassword('Akun dibuat', pw);
    } on ApiException {
      // pesan sudah ditampilkan
    }
  }

  Future<void> _buatSistem() async {
    final u = TextEditingController(), n = TextEditingController();
    var role = 'KASIR';
    final ok = await showDialog<bool>(
      context: context,
      builder: (c) => StatefulBuilder(
        builder: (c, set) => AlertDialog(
          title: const Text('User sistem baru'),
          content: SingleChildScrollView(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                TextField(
                  controller: u,
                  decoration: const InputDecoration(
                    labelText: 'Username',
                    helperText: 'Huruf kecil, tanpa spasi',
                  ),
                ),
                const SizedBox(height: 12),
                TextField(
                  controller: n,
                  decoration: const InputDecoration(labelText: 'Nama'),
                ),
                const SizedBox(height: 12),
                DropdownButtonFormField<String>(
                  initialValue: role,
                  items: [
                    for (final r in ['KASIR', 'ADMIN'])
                      DropdownMenuItem(value: r, child: Text(r)),
                  ],
                  onChanged: (v) => set(() => role = v ?? 'KASIR'),
                  decoration: const InputDecoration(labelText: 'Role'),
                ),
              ],
            ),
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(c, false),
              child: const Text('Batal'),
            ),
            FilledButton(
              onPressed: () => Navigator.pop(c, true),
              child: const Text('Buat'),
            ),
          ],
        ),
      ),
    );
    final data = (u.text.trim(), n.text.trim());
    _tundaDispose([u, n]);
    if (ok != true) return;
    await _kirim(username: data.$1, nama: data.$2, role: role);
  }

  Future<void> _buatAnggota() async {
    final a = await showDialog<Pelanggan>(
      context: context,
      builder: (_) => AnggotaPickerDialog(api: ref.read(apiProvider)),
    );
    if (a == null || !mounted) return;
    final u = TextEditingController(text: a.nik.toLowerCase());
    final ok = await showDialog<bool>(
      context: context,
      builder: (c) => AlertDialog(
        title: const Text('Akun anggota'),
        content: SingleChildScrollView(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Card(
                margin: EdgeInsets.zero,
                child: ListTile(
                  leading: const Icon(Icons.person),
                  title: Text(a.nama),
                  subtitle: Text(
                    '${a.nik}\n${a.unit.isEmpty ? 'Unit kerja tidak tersedia' : a.unit}',
                  ),
                  isThreeLine: true,
                ),
              ),
              const SizedBox(height: 12),
              TextField(
                controller: u,
                decoration: const InputDecoration(
                  labelText: 'Username',
                  helperText: 'Default: NIK anggota',
                ),
              ),
            ],
          ),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(c, false),
            child: const Text('Batal'),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(c, true),
            child: const Text('Buat'),
          ),
        ],
      ),
    );
    final username = u.text.trim();
    _tundaDispose([u]);
    if (ok != true) return;
    await _kirim(username: username, nama: a.nama, role: 'ANGGOTA', nik: a.nik);
  }

  Widget _kartu(UserInfo u) => Card(
    margin: const EdgeInsets.only(bottom: 8),
    child: ListTile(
      contentPadding: const EdgeInsets.fromLTRB(16, 8, 8, 8),
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
  final List<Pelanggan> _items = [];
  bool _loading = false;
  bool _lagi = false;
  String? _error;
  int _page = 1;
  int _requestId = 0;

  @override
  void initState() {
    super.initState();
    _cari(reset: true);
  }

  Timer? _debounce;

  @override
  void dispose() {
    _debounce?.cancel();
    _query.dispose();
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
          if (_lagi && _error == null)
            TextButton(
              onPressed: _loading ? null : () => _cari(reset: false),
              child: _loading
                  ? const SizedBox.square(
                      dimension: 18,
                      child: CircularProgressIndicator(strokeWidth: 2),
                    )
                  : const Text('Muat anggota lainnya'),
            ),
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
      return const Center(child: Text('Anggota tidak ditemukan.'));
    }
    return ListView.builder(
      itemCount: _items.length,
      itemBuilder: (context, index) {
        final anggota = _items[index];
        return ListTile(
          contentPadding: EdgeInsets.zero,
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
