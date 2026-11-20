import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../profil/profil_aksi.dart';
import '../../core/api.dart';
import '../../core/beranda_widgets.dart';
import '../../core/format.dart';
import '../../core/models.dart';
import '../../core/providers.dart';
import '../../core/widgets.dart';
import 'admin_katalog_page.dart' show adminKatalogTab;

/// Indeks tab di [AdminShell].
const adminTabBeranda = 0,
    adminTabProduk = 1,
    adminTabRekap = 2,
    adminTabUser = 3,
    adminTabPinjaman = 4;

/// Beranda admin: perlu perhatian -> KPI hari ini -> penjualan per kasir -> tren dan laporan pendukung.
/// [aktif] berubah true saat tab dibuka -> data dimuat ulang.
class AdminDashboardPage extends ConsumerStatefulWidget {
  const AdminDashboardPage({super.key, required this.aktif, this.onBuka});
  final bool aktif;

  /// Pindah ke tab shell (lihat `adminTab*`); sub-tab Produk diatur lewat [adminKatalogTab].
  final void Function(int tab)? onBuka;

  @override
  ConsumerState<AdminDashboardPage> createState() => _AdminDashboardPageState();
}

class _AdminDashboardPageState extends ConsumerState<AdminDashboardPage> {
  DashboardAdmin? _data;
  int? _stokHabis; // dimuat terpisah (agregasi berat di server)
  bool _loading = false;
  bool _stokMemuat = false;
  String? _stokError;
  String? _error;

  // Rincian penjualan: rentang yang dipilih + hasilnya dimuat terpisah dari Beranda.
  PresetRentang _preset = PresetRentang.hari;
  Rentang? _bebas;
  RincianPenjualan? _rincian;
  bool _rincianLoading = false;
  String? _rincianError;
  int _rincianId = 0;

  @override
  void initState() {
    super.initState();
    _muat();
  }

  @override
  void didUpdateWidget(covariant AdminDashboardPage old) {
    super.didUpdateWidget(old);
    if (widget.aktif && !old.aktif) _muat();
  }

  Future<void> _muat() async {
    if (_loading) return;
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final d = await ref.read(apiProvider).dashboardAdmin();
      if (!mounted) return;
      setState(() {
        _data = d;
        if (_preset == PresetRentang.periode && _rentangPeriode(d) == null) {
          _preset = PresetRentang.hari;
        }
      });
      _muatStok();
      _muatRincian(_rentangAktif(d));
    } on ApiException catch (e) {
      if (mounted) setState(() => _error = e.pesan);
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _muatStok() async {
    if (_stokMemuat) return;
    setState(() {
      _stokMemuat = true;
      _stokError = null;
    });
    try {
      final jumlah = await ref.read(apiProvider).stokHabisJumlah();
      if (mounted) setState(() => _stokHabis = jumlah);
    } on ApiException catch (e) {
      if (mounted) setState(() => _stokError = e.pesan);
    } finally {
      if (mounted) setState(() => _stokMemuat = false);
    }
  }

  Rentang? _rentangPeriode(DashboardAdmin d) => rentangPreset(
    PresetRentang.periode,
    d.tanggal,
    periodeDari: d.periodeDari,
    periodeSampai: d.periodeSampai,
  );

  Rentang? _rentangAktif(DashboardAdmin d) => switch (_preset) {
    PresetRentang.bebas => _bebas,
    PresetRentang.periode => _rentangPeriode(d),
    _ => rentangPreset(_preset, d.tanggal),
  };

  Future<void> _muatRincian(Rentang? r) async {
    if (r == null) return;
    final id = ++_rincianId;
    setState(() {
      _rincianLoading = true;
      _rincianError = null;
      if (_rincian?.dari != r.dari || _rincian?.sampai != r.sampai) {
        _rincian = null;
      }
    });
    try {
      final hasil = await ref
          .read(apiProvider)
          .rincianPenjualan(r.dari, r.sampai);
      if (mounted && id == _rincianId) setState(() => _rincian = hasil);
    } on ApiException catch (e) {
      if (mounted && id == _rincianId) setState(() => _rincianError = e.pesan);
    } finally {
      if (mounted && id == _rincianId) setState(() => _rincianLoading = false);
    }
  }

  Future<void> _pilihPreset(PresetRentang p, DashboardAdmin d) async {
    if (p == PresetRentang.bebas) {
      final hariIni = DateTime.tryParse(d.tanggal);
      if (hariIni == null) return;
      final awal = _rentangAktif(d);
      final dipilih = await showDateRangePicker(
        context: context,
        firstDate: DateTime(2020),
        lastDate: hariIni,
        initialDateRange: awal == null
            ? null
            : DateTimeRange(
                start: DateTime.parse(awal.dari),
                end: DateTime.parse(awal.sampai),
              ),
        helpText: 'Pilih rentang penjualan',
      );
      if (dipilih == null || !mounted) return;
      final r = Rentang(isoTanggal(dipilih.start), isoTanggal(dipilih.end));
      final galat = validasiRentang(r, d.tanggal);
      if (galat != null) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text(galat)));
        return;
      }
      setState(() {
        _preset = p;
        _bebas = r;
      });
      await _muatRincian(r);
      return;
    }
    setState(() => _preset = p);
    await _muatRincian(_rentangAktif(d));
  }

  List<Widget> _bagianRincian(DashboardAdmin d) {
    final adaPeriode = _rentangPeriode(d) != null;
    final rentang = _rentangAktif(d);
    final rc = _rincian;
    Widget chip(PresetRentang p, String label, {bool aktif = true}) =>
        ChoiceChip(
          label: Text(label),
          selected: _preset == p,
          materialTapTargetSize: MaterialTapTargetSize.padded,
          visualDensity: VisualDensity.standard,
          onSelected: aktif ? (_) => _pilihPreset(p, d) : null,
        );
    return [
      const JudulBagian('Penjualan per kasir'),
      Wrap(
        spacing: 8,
        runSpacing: 4,
        children: [
          chip(PresetRentang.hari, 'Hari ini'),
          chip(PresetRentang.tujuh, '7 hari'),
          chip(PresetRentang.periode, 'Periode berjalan', aktif: adaPeriode),
          chip(PresetRentang.bebas, 'Pilih tanggal', aktif: rentang != null),
        ],
      ),
      if (rentang != null) ...[
        const SizedBox(height: 8),
        Text(
          labelRentang(rentang.dari, rentang.sampai),
          style: Theme.of(context).textTheme.bodySmall,
        ),
      ],
      const SizedBox(height: 8),
      if (_rincianLoading)
        Semantics(
          liveRegion: true,
          label: 'Memuat penjualan per kasir',
          child: const LinearProgressIndicator(),
        ),
      if (_rincianError case final pesan? when rentang != null)
        GalatBeranda(
          judul: rc == null
              ? 'Penjualan per kasir tidak dapat dimuat'
              : 'Pembaruan rincian gagal · menampilkan data terakhir',
          pesan: pesan,
          cobaLagi: () => _muatRincian(rentang),
        ),
      if (rc != null) ...[
        KasirRincianKartu(
          judul: 'Semua kasir',
          pecahan: rc.ringkasan,
          total: true,
        ),
        const SizedBox(height: 8),
        if (rc.ringkasan.total.jumlah == 0)
          DaftarBeranda(
            kosong:
                'Belum ada transaksi pada ${labelRentang(rc.dari, rc.sampai)}.',
            baris: const [],
          )
        else
          for (final k in rc.perKasir) ...[
            KasirRincianKartu(
              judul: k.nama.isEmpty ? k.kasir : k.nama,
              subjudul: k.nama.isEmpty ? null : k.kasir,
              pecahan: k.pecahan,
            ),
            const SizedBox(height: 8),
          ],
        if (rc.perKasir.length >= 50)
          Text(
            'Menampilkan 50 kasir dengan penjualan terbesar.',
            style: Theme.of(context).textTheme.bodySmall,
          ),
      ],
    ];
  }

  List<Widget> _trenRincian(RincianPenjualan? rincian) {
    if (rincian == null ||
        rincian.ringkasan.total.jumlah == 0 ||
        rincian.harian.length <= 1) {
      return const [];
    }
    return [
      const JudulBagian('Tren harian pada rentang terpilih'),
      Card(
        child: Padding(
          padding: const EdgeInsets.all(14),
          child: rincian.harian.length > maksHariGrafik
              ? Text(
                  'Grafik harian tersedia untuk rentang hingga $maksHariGrafik hari.',
                )
              : Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    ProporsiJenis(pecahan: rincian.ringkasan),
                    const SizedBox(height: 8),
                    TrenBertumpuk(harian: rincian.harian),
                  ],
                ),
        ),
      ),
    ];
  }

  void _buka(int tab, {int? subTabProduk}) {
    if (subTabProduk != null) adminKatalogTab.value = subTabProduk;
    widget.onBuka?.call(tab);
  }

  List<Widget> _perhatian(DashboardAdmin d) {
    final item = <Widget>[
      if (!d.periodeTerbuka)
        PerhatianKartu(
          tingkat: Tingkat.bahaya,
          judul: 'Periode tidak terbuka',
          pesan: d.periodePesan ?? judulPeriode(d.periodeKode),
        ),
      if (d.userTerkunci > 0)
        PerhatianKartu(
          tingkat: Tingkat.peringatan,
          judul: '${d.userTerkunci} akun terkunci',
          pesan: 'Buka kunci di menu User.',
          onTap: () => _buka(adminTabUser),
        ),
      if (d.hargaRendah > 0)
        PerhatianKartu(
          tingkat: Tingkat.peringatan,
          judul: '${d.hargaRendah} produk harga jual ≤ HPP',
          pesan: 'Periksa dan naikkan harga jual.',
          onTap: () => _buka(adminTabProduk, subTabProduk: 2),
        ),
      if (d.tanpaHpp > 0)
        PerhatianKartu(
          tingkat: Tingkat.peringatan,
          judul: '${d.tanpaHpp} produk belum ada harga beli',
          pesan: 'Isi harga beli agar margin dapat dipantau.',
          onTap: () => _buka(adminTabProduk, subTabProduk: 2),
        ),
      if ((_stokHabis ?? 0) > 0)
        PerhatianKartu(
          tingkat: Tingkat.peringatan,
          judul: '$_stokHabis produk stok habis',
          pesan: 'Lakukan opname atau pembelian.',
          onTap: () => _buka(adminTabProduk, subTabProduk: 1),
        ),
    ];
    if (_stokMemuat || (_stokHabis == null && _stokError == null)) {
      item.add(
        const BannerBeranda(
          tingkat: Tingkat.ok,
          judul: 'Memeriksa stok habis…',
        ),
      );
    }
    if (_stokError case final pesan?) {
      item.add(
        GalatBeranda(
          judul: _stokHabis == null
              ? 'Status stok habis belum tersedia'
              : 'Status stok habis gagal diperbarui',
          pesan: pesan,
          cobaLagi: _muatStok,
        ),
      );
    }
    if (item.isEmpty) {
      return const [
        BannerBeranda(
          tingkat: Tingkat.ok,
          judul: 'Tidak ada yang perlu perhatian saat ini',
        ),
      ];
    }
    return [
      for (final w in item) ...[w, const SizedBox(height: 8)],
    ];
  }

  @override
  Widget build(BuildContext context) {
    final d = _data;
    final warna = Theme.of(context).colorScheme;
    final profil = ref.watch(authProvider).profil;
    return BerandaScaffold(
      actions: [
        const ProfilAksi(warnaTema: null),
        IconButton(
          tooltip: 'Keluar',
          icon: const Icon(Icons.logout),
          onPressed: () => ref.read(authProvider.notifier).logout(),
        ),
      ],
      onRefresh: _muat,
      memuat: _loading,
      adaData: d != null,
      galat: _error,
      children: [
        if (d != null) ...[
          BerandaHeader(
            nama: profil?.nama.trim().split(' ').first,
            tanggal: d.tanggal,
            chip: ChipPeriode(
              kode: d.periodeKode,
              periode: d.periode,
              dari: d.periodeDari,
              sampai: d.periodeSampai,
            ),
          ),
          const JudulBagian('Perlu perhatian'),
          ..._perhatian(d),
          const JudulBagian('Penjualan hari ini'),
          RingkasanPenjualan(
            label: 'Total penjualan (semua kasir)',
            total: d.total,
            jumlah: d.jumlah,
            kemarin: d.kemarin,
          ),
          ..._bagianRincian(d),
          ..._trenRincian(_rincian),
          if (d.tren.isNotEmpty) ...[
            const JudulBagian('Tren penjualan 7 hari'),
            Card(
              child: Padding(
                padding: const EdgeInsets.fromLTRB(10, 14, 10, 12),
                child: TrenBatang(tren: d.tren),
              ),
            ),
          ],
          const JudulBagian('Barang terlaris hari ini'),
          DaftarBeranda(
            kosong: 'Belum ada penjualan hari ini.',
            baris: [
              for (final t in d.terlaris)
                BarisBeranda(
                  judul: t.nama.isEmpty ? t.kode : t.nama,
                  detail: '${t.kode} · terjual ${formatQty(t.qty, '')}',
                  nilai: rupiah(t.total),
                ),
            ],
          ),
          const JudulBagian('User mobile'),
          Wrap(
            spacing: 8,
            runSpacing: 8,
            children: [
              StatePill(text: 'Admin ${d.userAdmin}'),
              StatePill(text: 'Kasir ${d.userKasir}'),
              StatePill(text: 'Anggota ${d.userAnggota}'),
              StatePill(
                text: 'Nonaktif ${d.userNonaktif}',
                color: warna.onSurfaceVariant,
              ),
              StatePill(
                text: 'Terkunci ${d.userTerkunci}',
                color: warna.error,
              ),
            ],
          ),
        ],
      ],
    );
  }
}

/// Shell admin: tab Beranda (awal), Produk (Produk | Stok habis | Harga ≤ HPP | Opname | Kategori), Rekap anggota, User, dan Pinjaman.
class AdminShell extends StatefulWidget {
  const AdminShell({
    super.key,
    required this.produk,
    required this.rekap,
    required this.user,
    required this.pinjaman,
  });
  final Widget produk;
  final Widget rekap;
  final Widget user;
  final Widget pinjaman;

  @override
  State<AdminShell> createState() => _AdminShellState();
}

class _AdminShellState extends State<AdminShell> {
  int _i = adminTabBeranda;
  bool _pernahBukaPinjaman = false;

  void _buka(int tab) => setState(() {
    _i = tab;
    if (tab == adminTabPinjaman) _pernahBukaPinjaman = true;
  });

  @override
  Widget build(BuildContext context) => Scaffold(
    // Keyboard ditangani Scaffold halaman anak (jangan dihitung dua kali).
    resizeToAvoidBottomInset: false,
    // Halaman anak adalah Scaffold sendiri; inset bawah sudah ditangani NavigationBar di sini.
    body: MediaQuery.removePadding(
      context: context,
      removeBottom: true,
      child: IndexedStack(
        index: _i,
        children: [
          AdminDashboardPage(
            aktif: _i == adminTabBeranda,
            onBuka: _buka,
          ),
          widget.produk,
          widget.rekap,
          widget.user,
          _pernahBukaPinjaman ? widget.pinjaman : const SizedBox.shrink(),
        ],
      ),
    ),
    // Disembunyikan saat keyboard terbuka agar layar tidak overflow di viewport sempit.
    bottomNavigationBar: MediaQuery.viewInsetsOf(context).bottom > 0
        ? null
        : NavigationBar(
            selectedIndex: _i,
            onDestinationSelected: _buka,
            destinations: const [
              NavigationDestination(
                icon: Icon(Icons.dashboard_outlined),
                selectedIcon: Icon(Icons.dashboard),
                label: 'Beranda',
              ),
              NavigationDestination(
                icon: Icon(Icons.inventory_2_outlined),
                selectedIcon: Icon(Icons.inventory_2),
                label: 'Produk',
              ),
              NavigationDestination(
                icon: Icon(Icons.summarize_outlined),
                selectedIcon: Icon(Icons.summarize),
                label: 'Rekap',
              ),
              NavigationDestination(
                icon: Icon(Icons.group_outlined),
                selectedIcon: Icon(Icons.group),
                label: 'User',
              ),
              NavigationDestination(
                icon: Icon(Icons.account_balance_wallet_outlined),
                selectedIcon: Icon(Icons.account_balance_wallet),
                label: 'Pinjaman',
              ),
            ],
          ),
  );
}
