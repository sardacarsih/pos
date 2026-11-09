import 'package:flutter/material.dart';
import 'package:package_info_plus/package_info_plus.dart';

import '../../core/update.dart';
import '../../core/widgets.dart';

/// Riwayat versi aplikasi, diambil dari server (downloads/changelog.json).
class ChangelogPage extends StatefulWidget {
  const ChangelogPage({super.key});
  @override
  State<ChangelogPage> createState() => _ChangelogPageState();
}

class _ChangelogPageState extends State<ChangelogPage> {
  late Future<(List<EntriRilis>, int)> _data = _muat();

  Future<(List<EntriRilis>, int)> _muat() async {
    final daftar = await ambilChangelog();
    final terpasang =
        int.tryParse((await PackageInfo.fromPlatform()).buildNumber) ?? 0;
    return (daftar, terpasang);
  }

  @override
  Widget build(BuildContext context) {
    final tema = Theme.of(context);
    return Scaffold(
      appBar: AppBar(title: const Text('Riwayat versi')),
      body: FutureBuilder<(List<EntriRilis>, int)>(
        future: _data,
        builder: (_, s) {
          if (s.connectionState != ConnectionState.done) {
            return const LoadingKopkar();
          }
          if (s.hasError) {
            return Center(
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  const Text('Tidak bisa memuat riwayat versi.'),
                  const SizedBox(height: 8),
                  FilledButton.tonal(
                    onPressed: () => setState(() => _data = _muat()),
                    child: const Text('Coba lagi'),
                  ),
                ],
              ),
            );
          }
          final (daftar, terpasang) = s.data!;
          return ListView(
            padding: const EdgeInsets.all(16),
            children: [
              for (final e in daftar)
                Card(
                  margin: const EdgeInsets.only(bottom: 12),
                  child: Padding(
                    padding: const EdgeInsets.all(16),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Row(
                          children: [
                            Text(
                              'Versi ${e.versionName} (build ${e.versionCode})',
                              style: tema.textTheme.titleMedium,
                            ),
                            if (e.versionCode == terpasang) ...[
                              const SizedBox(width: 8),
                              Chip(
                                label: const Text('Terpasang'),
                                visualDensity: VisualDensity.compact,
                              ),
                            ],
                          ],
                        ),
                        if (e.tanggal.isNotEmpty)
                          Text(e.tanggal, style: tema.textTheme.bodySmall),
                        const SizedBox(height: 8),
                        for (final p in e.perubahan)
                          Padding(
                            padding: const EdgeInsets.only(bottom: 4),
                            child: Text('• $p'),
                          ),
                      ],
                    ),
                  ),
                ),
            ],
          );
        },
      ),
    );
  }
}
