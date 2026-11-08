import 'package:flutter/material.dart';

import 'belanja_page.dart';
import 'dashboard_anggota_page.dart';
import 'transaksi_saya_page.dart';

/// Shell anggota: Dashboard (awal) · Belanja mandiri · Riwayat (Transaksi Saya).
class AnggotaShell extends StatefulWidget {
  const AnggotaShell({super.key});
  @override
  State<AnggotaShell> createState() => _AnggotaShellState();
}

class _AnggotaShellState extends State<AnggotaShell> {
  int _i = 0;
  // Riwayat dibuat ulang setelah belanja agar daftar & limit segar.
  int _riwayat = 0;

  @override
  Widget build(BuildContext context) => Scaffold(
    resizeToAvoidBottomInset: false,
    body: MediaQuery.removePadding(
      context: context,
      removeBottom: true,
      child: IndexedStack(
        index: _i,
        children: [
          DashboardAnggotaPage(aktif: _i == 0, onBelanja: () => setState(() => _i = 1)),
          BelanjaPage(
            onSelesai: () => setState(() {
              _riwayat++;
              _i = 0;
            }),
          ),
          TransaksiSayaPage(key: ValueKey(_riwayat)),
        ],
      ),
    ),
    bottomNavigationBar: MediaQuery.viewInsetsOf(context).bottom > 0
        ? null
        : NavigationBar(
            selectedIndex: _i,
            onDestinationSelected: (v) => setState(() => _i = v),
            destinations: const [
              NavigationDestination(
                icon: Icon(Icons.dashboard_outlined),
                selectedIcon: Icon(Icons.dashboard),
                label: 'Dashboard',
              ),
              NavigationDestination(
                icon: Icon(Icons.shopping_cart_outlined),
                selectedIcon: Icon(Icons.shopping_cart),
                label: 'Belanja',
              ),
              NavigationDestination(
                icon: Icon(Icons.receipt_long_outlined),
                selectedIcon: Icon(Icons.receipt_long),
                label: 'Riwayat',
              ),
            ],
          ),
  );
}
