import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/providers.dart';
import '../../core/tema_mode.dart';
import '../../core/widgets.dart';
import 'profil_page.dart';

/// Tombol avatar di header: membuka Profil Saya. Dipakai di semua layar utama kasir dan admin.
class ProfilAksi extends StatelessWidget {
  const ProfilAksi({super.key});

  @override
  Widget build(BuildContext context) => Row(
    mainAxisSize: MainAxisSize.min,
    children: [
      const TemaTombol(warna: Colors.white),
      IconButton(
        tooltip: 'Profil saya',
        icon: const ProfilAvatar(radius: 14),
        onPressed: () => Navigator.of(context).push(
          MaterialPageRoute(builder: (_) => const ProfilPage()),
        ),
      ),
    ],
  );
}

/// Sapaan di dashboard kasir/admin: avatar + "Halo, nama" + peran.
class SapaanProfil extends ConsumerWidget {
  const SapaanProfil({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final p = ref.watch(authProvider).profil;
    if (p == null) return const SizedBox.shrink();
    return Padding(
      padding: const EdgeInsets.only(bottom: 12),
      child: Row(
        children: [
          const ProfilAvatar(radius: 24),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  'Halo, ${p.nama}',
                  style: Theme.of(context).textTheme.titleMedium,
                ),
                Text(p.role, style: Theme.of(context).textTheme.bodySmall),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
