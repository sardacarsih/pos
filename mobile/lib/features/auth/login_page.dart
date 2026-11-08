import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/api.dart';
import '../../core/providers.dart';

class LoginPage extends ConsumerStatefulWidget {
  const LoginPage({super.key});
  @override
  ConsumerState<LoginPage> createState() => _LoginPageState();
}

class _LoginPageState extends ConsumerState<LoginPage> {
  final _u = TextEditingController();
  final _p = TextEditingController();
  bool _busy = false, _lihat = false;
  String? _error;

  Future<void> _masuk() async {
    if (_busy || _u.text.trim().isEmpty || _p.text.isEmpty) return;
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      await ref.read(authProvider.notifier).login(_u.text, _p.text);
    } on ApiException catch (e) {
      if (mounted) setState(() => _error = e.pesan);
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  void dispose() {
    _u.dispose();
    _p.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: SafeArea(
        child: Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(24),
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 420),
              child: AutofillGroup(
                child: Column(mainAxisSize: MainAxisSize.min, crossAxisAlignment: CrossAxisAlignment.stretch, children: [
                  Icon(Icons.storefront, size: 64, color: Theme.of(context).colorScheme.primary),
                  const SizedBox(height: 8),
                  Text('Kopkar Kusuma Lestari', textAlign: TextAlign.center, style: Theme.of(context).textTheme.headlineSmall),
                  const SizedBox(height: 32),
                  TextField(
                    controller: _u,
                    autofillHints: const [AutofillHints.username],
                    textInputAction: TextInputAction.next,
                    autocorrect: false,
                    enableSuggestions: false,
                    decoration: const InputDecoration(labelText: 'Username', border: OutlineInputBorder()),
                  ),
                  const SizedBox(height: 16),
                  TextField(
                    controller: _p,
                    obscureText: !_lihat,
                    autofillHints: const [AutofillHints.password],
                    onSubmitted: (_) => _masuk(),
                    decoration: InputDecoration(
                      labelText: 'Password',
                      border: const OutlineInputBorder(),
                      suffixIcon: IconButton(
                        tooltip: _lihat ? 'Sembunyikan password' : 'Tampilkan password',
                        icon: Icon(_lihat ? Icons.visibility_off : Icons.visibility),
                        onPressed: () => setState(() => _lihat = !_lihat),
                      ),
                    ),
                  ),
                  if (_error != null)
                    Padding(
                      padding: const EdgeInsets.only(top: 12),
                      child: Semantics(liveRegion: true, child: Text(_error!, style: TextStyle(color: Theme.of(context).colorScheme.error))),
                    ),
                  const SizedBox(height: 24),
                  FilledButton(
                    onPressed: _busy ? null : _masuk,
                    style: FilledButton.styleFrom(minimumSize: const Size.fromHeight(52)),
                    child: _busy ? const SizedBox(height: 22, width: 22, child: CircularProgressIndicator(strokeWidth: 2)) : const Text('Masuk'),
                  ),
                ]),
              ),
            ),
          ),
        ),
      ),
    );
  }
}

/// Wajib setelah akun dibuat/di-reset admin (password sementara).
class GantiPasswordPage extends ConsumerStatefulWidget {
  const GantiPasswordPage({super.key});
  @override
  ConsumerState<GantiPasswordPage> createState() => _GantiPasswordPageState();
}

class _GantiPasswordPageState extends ConsumerState<GantiPasswordPage> {
  final _lama = TextEditingController(), _baru = TextEditingController(), _ulang = TextEditingController();
  bool _busy = false;
  String? _error;

  Future<void> _simpan() async {
    if (_baru.text.length < 8) return setState(() => _error = 'Password baru minimal 8 karakter');
    if (_baru.text != _ulang.text) return setState(() => _error = 'Konfirmasi password tidak sama');
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      await ref.read(apiProvider).gantiPassword(_lama.text, _baru.text);
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Password diganti. Silakan masuk kembali.')));
      ref.read(authProvider.notifier).sesiBerakhir();
    } on ApiException catch (e) {
      if (mounted) setState(() => _error = e.pesan);
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  void dispose() {
    _lama.dispose();
    _baru.dispose();
    _ulang.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    Widget f(TextEditingController c, String l) => Padding(
          padding: const EdgeInsets.only(bottom: 16),
          child: TextField(controller: c, obscureText: true, decoration: InputDecoration(labelText: l, border: const OutlineInputBorder())),
        );
    return Scaffold(
      appBar: AppBar(title: const Text('Ganti Password')),
      body: ListView(padding: const EdgeInsets.all(24), children: [
        const Text('Anda memakai password sementara. Buat password baru untuk melanjutkan.'),
        const SizedBox(height: 24),
        f(_lama, 'Password sementara'),
        f(_baru, 'Password baru (min. 8 karakter)'),
        f(_ulang, 'Ulangi password baru'),
        if (_error != null) Text(_error!, style: TextStyle(color: Theme.of(context).colorScheme.error)),
        const SizedBox(height: 16),
        FilledButton(onPressed: _busy ? null : _simpan, style: FilledButton.styleFrom(minimumSize: const Size.fromHeight(52)), child: const Text('Simpan')),
        TextButton(onPressed: () => ref.read(authProvider.notifier).logout(), child: const Text('Keluar')),
      ]),
    );
  }
}
