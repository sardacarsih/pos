import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/api.dart';
import '../../core/providers.dart';
import '../../core/tema_mode.dart';
import '../../core/widgets.dart';

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

  Future<void> _bantuanPassword() => showDialog<void>(
    context: context,
    builder: (c) => AlertDialog(
      icon: const Icon(Icons.support_agent_outlined),
      title: const Text('Lupa password?'),
      content: const Text(
        'Hubungi admin koperasi untuk meminta reset password akun Anda.',
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.pop(c),
          child: const Text('Mengerti'),
        ),
      ],
    ),
  );

  @override
  void dispose() {
    _u.dispose();
    _p.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final dark = theme.brightness == Brightness.dark;
    return Scaffold(
      body: Stack(
        fit: StackFit.expand,
        children: [
          Image.asset('assets/login_bg.webp', fit: BoxFit.cover),
          if (dark) const ColoredBox(color: Color(0x990A2117)),
          SafeArea(
            child: LayoutBuilder(
              builder: (context, constraints) => SingleChildScrollView(
                padding: const EdgeInsets.symmetric(
                  horizontal: 16,
                  vertical: 24,
                ),
                child: Center(
                  child: ConstrainedBox(
                    constraints: BoxConstraints(
                      maxWidth: 440,
                      minHeight: constraints.maxHeight - 48,
                    ),
                    child: Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        const SizedBox(height: 12),
                        Text(
                          'Kopkar\nKusuma Lestari',
                          textAlign: TextAlign.center,
                          style: theme.textTheme.headlineMedium?.copyWith(
                            color: const Color(0xFF075B2C),
                            fontWeight: FontWeight.w700,
                            height: 1.05,
                          ),
                        ),
                        const SizedBox(height: 8),
                        const Text(
                          'Bersama Kita Tumbuh\nLebih Baik',
                          textAlign: TextAlign.center,
                          style: TextStyle(
                            color: Color(0xFF23683A),
                            fontStyle: FontStyle.italic,
                            height: 1.4,
                          ),
                        ),
                        const SizedBox(height: 28),
                        AutofillGroup(
                          child: Card(
                            color: theme.colorScheme.surface,
                            child: Padding(
                              padding: const EdgeInsets.fromLTRB(20, 0, 20, 20),
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.stretch,
                                children: [
                                  Transform.translate(
                                    offset: const Offset(0, -20),
                                    child: CircleAvatar(
                                      radius: 30,
                                      backgroundColor:
                                          theme.colorScheme.primaryContainer,
                                      child: Icon(
                                        Icons.people_alt_outlined,
                                        color: theme.colorScheme.primary,
                                        size: 28,
                                      ),
                                    ),
                                  ),
                                  Text(
                                    'Selamat Datang Kembali!',
                                    textAlign: TextAlign.center,
                                    style: theme.textTheme.titleLarge?.copyWith(
                                      color:
                                          theme.colorScheme.onPrimaryContainer,
                                    ),
                                  ),
                                  const SizedBox(height: 6),
                                  Text(
                                    'Masuk untuk mengakses akun Anda',
                                    textAlign: TextAlign.center,
                                    style: theme.textTheme.bodySmall?.copyWith(
                                      color: theme.colorScheme.onSurfaceVariant,
                                    ),
                                  ),
                                  const SizedBox(height: 20),
                                  TextField(
                                    controller: _u,
                                    autofillHints: const [
                                      AutofillHints.username,
                                    ],
                                    textInputAction: TextInputAction.next,
                                    autocorrect: false,
                                    enableSuggestions: false,
                                    decoration: const InputDecoration(
                                      labelText: 'Username',
                                      prefixIcon: Icon(Icons.person_outline),
                                    ),
                                  ),
                                  const SizedBox(height: 12),
                                  TextField(
                                    controller: _p,
                                    obscureText: !_lihat,
                                    autofillHints: const [
                                      AutofillHints.password,
                                    ],
                                    onSubmitted: (_) => _masuk(),
                                    decoration: InputDecoration(
                                      labelText: 'Password',
                                      prefixIcon: const Icon(
                                        Icons.lock_outline,
                                      ),
                                      suffixIcon: IconButton(
                                        tooltip: _lihat
                                            ? 'Sembunyikan password'
                                            : 'Tampilkan password',
                                        icon: Icon(
                                          _lihat
                                              ? Icons.visibility_off
                                              : Icons.visibility,
                                        ),
                                        onPressed: () =>
                                            setState(() => _lihat = !_lihat),
                                      ),
                                    ),
                                  ),
                                  if (_error != null)
                                    Padding(
                                      padding: const EdgeInsets.only(top: 12),
                                      child: Semantics(
                                        liveRegion: true,
                                        child: Text(
                                          _error!,
                                          style: TextStyle(
                                            color: theme.colorScheme.error,
                                          ),
                                        ),
                                      ),
                                    ),
                                  Row(
                                    mainAxisAlignment: MainAxisAlignment.end,
                                    children: [
                                      TextButton(
                                        onPressed: _bantuanPassword,
                                        child: const Text('Lupa password?'),
                                      ),
                                    ],
                                  ),
                                  GradientActionButton(
                                    label: _busy ? 'Memproses…' : 'Masuk',
                                    icon: _busy ? null : Icons.login,
                                    onPressed: _busy ? null : _masuk,
                                  ),
                                ],
                              ),
                            ),
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
              ),
            ),
          ),
          const Positioned(
            top: 0,
            right: 4,
            child: SafeArea(child: TemaTombol(warna: Colors.white)),
          ),
        ],
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
  final _lama = TextEditingController(),
      _baru = TextEditingController(),
      _ulang = TextEditingController();
  bool _busy = false, _lihat = false;
  String? _error;

  Future<void> _simpan() async {
    if (_baru.text.length < 8)
      return setState(() => _error = 'Password baru minimal 8 karakter');
    if (_baru.text != _ulang.text)
      return setState(() => _error = 'Konfirmasi password tidak sama');
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      await ref.read(apiProvider).gantiPassword(_lama.text, _baru.text);
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Password diganti. Silakan masuk kembali.'),
        ),
      );
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
      padding: const EdgeInsets.only(bottom: 14),
      child: TextField(
        controller: c,
        obscureText: !_lihat,
        decoration: InputDecoration(
          labelText: l,
          prefixIcon: const Icon(Icons.lock_outline),
          suffixIcon: IconButton(
            tooltip: _lihat ? 'Sembunyikan password' : 'Tampilkan password',
            onPressed: () => setState(() => _lihat = !_lihat),
            icon: Icon(_lihat ? Icons.visibility_off : Icons.visibility),
          ),
        ),
      ),
    );
    return TropicalScaffold(
      title: 'Ganti Password',
      body: ListView(
        padding: const EdgeInsets.fromLTRB(16, 8, 16, 24),
        children: [
          Card(
            color: Theme.of(context).brightness == Brightness.dark
                ? const Color(0xFF523800)
                : const Color(0xFFFFEDC7),
            child: const Padding(
              padding: EdgeInsets.all(16),
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Icon(Icons.lock_reset_outlined, color: Color(0xFF9C6200)),
                  SizedBox(width: 12),
                  Expanded(
                    child: Text(
                      'Anda memakai password sementara. Buat password baru untuk melanjutkan.',
                    ),
                  ),
                ],
              ),
            ),
          ),
          const SizedBox(height: 20),
          f(_lama, 'Password sementara'),
          f(_baru, 'Password baru (min. 8 karakter)'),
          f(_ulang, 'Ulangi password baru'),
          if (_error != null)
            Padding(
              padding: const EdgeInsets.only(bottom: 12),
              child: Semantics(
                liveRegion: true,
                child: Text(
                  _error!,
                  style: TextStyle(color: Theme.of(context).colorScheme.error),
                ),
              ),
            ),
          GradientActionButton(
            label: _busy ? 'Menyimpan…' : 'Simpan',
            icon: Icons.check,
            onPressed: _busy ? null : _simpan,
          ),
          const SizedBox(height: 8),
          TextButton.icon(
            onPressed: () => ref.read(authProvider.notifier).logout(),
            icon: const Icon(Icons.logout),
            label: const Text('Keluar'),
          ),
        ],
      ),
    );
  }
}
