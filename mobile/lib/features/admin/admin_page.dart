import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/api.dart';
import '../../core/models.dart';
import '../../core/providers.dart';

class AdminPage extends ConsumerStatefulWidget {
  const AdminPage({super.key});
  @override
  ConsumerState<AdminPage> createState() => _AdminPageState();
}

class _AdminPageState extends ConsumerState<AdminPage> {
  List<UserInfo> _users = [];
  bool _loading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _muat();
  }

  void _pesan(String m) => ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(m)));

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
          content: Column(mainAxisSize: MainAxisSize.min, crossAxisAlignment: CrossAxisAlignment.start, children: [
            const Text('Password sementara (hanya tampil sekali). User wajib menggantinya saat login pertama.'),
            const SizedBox(height: 12),
            SelectableText(pw, style: Theme.of(c).textTheme.headlineSmall),
          ]),
          actions: [FilledButton(onPressed: () => Navigator.pop(c), child: const Text('Sudah dicatat'))],
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

  Future<void> _buat() async {
    final u = TextEditingController(), n = TextEditingController(), nik = TextEditingController();
    String role = 'KASIR';
    final ok = await showDialog<bool>(
      context: context,
      builder: (c) => StatefulBuilder(
        builder: (c, set) => AlertDialog(
          title: const Text('User baru'),
          content: SingleChildScrollView(
            child: Column(mainAxisSize: MainAxisSize.min, children: [
              TextField(controller: u, decoration: const InputDecoration(labelText: 'Username (huruf kecil)')),
              TextField(controller: n, decoration: const InputDecoration(labelText: 'Nama')),
              DropdownButtonFormField<String>(
                initialValue: role,
                items: [for (final r in ['KASIR', 'ANGGOTA', 'ADMIN']) DropdownMenuItem(value: r, child: Text(r))],
                onChanged: (v) => set(() => role = v ?? 'KASIR'),
                decoration: const InputDecoration(labelText: 'Role'),
              ),
              if (role == 'ANGGOTA') TextField(controller: nik, decoration: const InputDecoration(labelText: 'NIK anggota')),
            ]),
          ),
          actions: [TextButton(onPressed: () => Navigator.pop(c, false), child: const Text('Batal')), FilledButton(onPressed: () => Navigator.pop(c, true), child: const Text('Buat'))],
        ),
      ),
    );
    final data = (u.text.trim(), n.text.trim(), nik.text.trim());
    u.dispose();
    n.dispose();
    nik.dispose();
    if (ok != true) return;
    try {
      final pw = await _jalan(() => ref.read(apiProvider).buatUser(username: data.$1, nama: data.$2, role: role, nik: role == 'ANGGOTA' ? data.$3 : null));
      await _muat();
      if (mounted) await _tampilPassword('User dibuat', pw);
    } on ApiException {
      // pesan sudah ditampilkan
    }
  }

  Future<void> _kategori(UserInfo u) async {
    try {
      final semua = await _jalan(() => ref.read(apiProvider).semuaKategori());
      if (!mounted) return;
      final pilih = {...u.kategori};
      final ok = await showDialog<bool>(
        context: context,
        builder: (c) => StatefulBuilder(
          builder: (c, set) => AlertDialog(
            title: Text('Kategori ${u.username}'),
            content: SizedBox(
              width: double.maxFinite,
              child: ListView(shrinkWrap: true, children: [
                const Text('Kosong = semua kategori diizinkan.'),
                for (final k in semua)
                  CheckboxListTile(value: pilih.contains(k.id), title: Text(k.nama), onChanged: (v) => set(() => v == true ? pilih.add(k.id) : pilih.remove(k.id))),
              ]),
            ),
            actions: [TextButton(onPressed: () => Navigator.pop(c, false), child: const Text('Batal')), FilledButton(onPressed: () => Navigator.pop(c, true), child: const Text('Simpan'))],
          ),
        ),
      );
      if (ok == true) {
        await _jalan(() => ref.read(apiProvider).setKategoriUser(u.id, pilih.toList()));
        await _muat();
      }
    } on ApiException {
      // pesan sudah ditampilkan
    }
  }

  @override
  Widget build(BuildContext context) => Scaffold(
        appBar: AppBar(title: const Text('Admin · User Mobile'), actions: [
          IconButton(tooltip: 'Keluar', icon: const Icon(Icons.logout), onPressed: () => ref.read(authProvider.notifier).logout()),
        ]),
        floatingActionButton: FloatingActionButton.extended(onPressed: _buat, icon: const Icon(Icons.person_add), label: const Text('User')),
        body: RefreshIndicator(
          onRefresh: _muat,
          child: ListView(padding: const EdgeInsets.only(bottom: 96), children: [
            if (_loading) const Padding(padding: EdgeInsets.all(48), child: Center(child: CircularProgressIndicator())),
            if (_error != null) Padding(padding: const EdgeInsets.all(24), child: Text(_error!)),
            for (final u in _users)
              ListTile(
                title: Text('${u.nama} (${u.username})'),
                subtitle: Text('${u.role}${u.nik != null ? ' · ${u.nik}' : ''}${u.aktif ? '' : ' · NONAKTIF'}${u.terkunci ? ' · TERKUNCI' : ''}'),
                trailing: PopupMenuButton<String>(
                  onSelected: (a) async {
                    try {
                      if (a == 'reset') {
                        final pw = await _jalan(() => ref.read(apiProvider).resetPassword(u.id));
                        if (mounted) await _tampilPassword('Password direset', pw);
                      } else if (a == 'aktif') {
                        await _jalan(() => ref.read(apiProvider).setAktif(u.id, !u.aktif));
                        await _muat();
                      } else if (a == 'kategori') {
                        await _kategori(u);
                      }
                    } on ApiException {
                      // pesan sudah ditampilkan
                    }
                  },
                  itemBuilder: (_) => [
                    const PopupMenuItem(value: 'reset', child: Text('Reset password')),
                    PopupMenuItem(value: 'aktif', child: Text(u.aktif ? 'Nonaktifkan' : 'Aktifkan')),
                    if (u.role == 'KASIR') const PopupMenuItem(value: 'kategori', child: Text('Kategori diizinkan')),
                  ],
                ),
              ),
          ]),
        ),
      );
}
