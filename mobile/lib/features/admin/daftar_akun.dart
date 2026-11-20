import 'package:flutter/material.dart';

import '../../core/models.dart';
import '../../core/widgets.dart';

enum TindakanAkun { reset, status }

enum _Status {
  semua('Semua', Icons.filter_list),
  terkunci('Terkunci', Icons.lock_outline),
  nonaktif('Nonaktif', Icons.remove_circle_outline),
  aktif('Aktif', Icons.check_circle_outline);

  const _Status(this.label, this.ikon);
  final String label;
  final IconData ikon;

  bool cocok(UserInfo u) => switch (this) {
    _Status.semua => true,
    _Status.terkunci => u.terkunci,
    _Status.nonaktif => !u.aktif,
    _Status.aktif => u.aktif,
  };
}

class DaftarAkun extends StatefulWidget {
  const DaftarAkun({
    super.key,
    required this.users,
    required this.jenis,
    required this.idSendiri,
    required this.loading,
    required this.error,
    required this.onRefresh,
    required this.onTindakan,
  });

  final List<UserInfo> users;
  final String jenis;
  final int? idSendiri;
  final bool loading;
  final String? error;
  final Future<void> Function()? onRefresh;
  final void Function(UserInfo, TindakanAkun)? onTindakan;

  @override
  State<DaftarAkun> createState() => _DaftarAkunState();
}

class _DaftarAkunState extends State<DaftarAkun>
    with AutomaticKeepAliveClientMixin {
  final _cari = TextEditingController();
  var _status = _Status.semua;

  @override
  bool get wantKeepAlive => true;

  @override
  void dispose() {
    _cari.dispose();
    super.dispose();
  }

  void _hapusFilter() => setState(() {
    _cari.clear();
    _status = _Status.semua;
  });

  @override
  Widget build(BuildContext context) {
    super.build(context);
    final q = _cari.text.trim().toLowerCase();
    final hasil = widget.users
        .where(
          (u) =>
              _status.cocok(u) &&
              [
                u.nama,
                u.username,
                u.nik ?? '',
              ].any((v) => v.toLowerCase().contains(q)),
        )
        .toList();
    final tersaring = q.isNotEmpty || _status != _Status.semua;
    final terkunci = widget.users.where((u) => u.terkunci).length;
    final jumlahTersedia =
        widget.users.isNotEmpty || (!widget.loading && widget.error == null);
    final scroll = CustomScrollView(
      physics: const AlwaysScrollableScrollPhysics(),
      slivers: [
        SliverPadding(
          padding: const EdgeInsets.fromLTRB(16, 12, 16, 4),
          sliver: SliverToBoxAdapter(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                TextField(
                  controller: _cari,
                  onChanged: (_) => setState(() {}),
                  textInputAction: TextInputAction.search,
                  decoration: const InputDecoration(
                    labelText: 'Nama, username, atau NIK',
                    prefixIcon: Icon(Icons.search),
                  ),
                ),
                const SizedBox(height: 6),
                Wrap(
                  spacing: 6,
                  children: [
                    for (final status in _Status.values)
                      ChoiceChip(
                        selected: _status == status,
                        showCheckmark: false,
                        avatar: Icon(status.ikon, size: 16),
                        label: Text(
                          status == _Status.terkunci && jumlahTersedia
                              ? 'Terkunci ($terkunci)'
                              : status.label,
                        ),
                        onSelected: (_) => setState(() => _status = status),
                      ),
                  ],
                ),
              ],
            ),
          ),
        ),
        if (tersaring && hasil.isNotEmpty)
          SliverPadding(
            padding: const EdgeInsets.symmetric(horizontal: 16),
            sliver: SliverToBoxAdapter(
              child: Row(
                children: [
                  Expanded(child: Text('${hasil.length} akun cocok')),
                  TextButton.icon(
                    onPressed: _hapusFilter,
                    icon: const Icon(Icons.close, size: 16),
                    label: const Text('Hapus filter'),
                  ),
                ],
              ),
            ),
          ),
        if (widget.error != null)
          SliverToBoxAdapter(
            child: Column(
              children: [
                Text(widget.error!, textAlign: TextAlign.center),
                TextButton.icon(
                  onPressed: widget.onRefresh,
                  icon: const Icon(Icons.refresh),
                  label: const Text('Coba lagi'),
                ),
              ],
            ),
          ),
        if (widget.loading && widget.users.isEmpty)
          const SliverToBoxAdapter(child: LoadingKopkar())
        else if (hasil.isEmpty && widget.error == null)
          SliverToBoxAdapter(
            child: Padding(
              padding: const EdgeInsets.all(20),
              child: Column(
                children: [
                  Icon(tersaring ? Icons.search_off : Icons.people_outline),
                  const SizedBox(height: 8),
                  Text(
                    tersaring
                        ? 'Tidak ada akun yang cocok'
                        : 'Belum ada akun ${widget.jenis}',
                  ),
                  if (tersaring)
                    TextButton(
                      onPressed: _hapusFilter,
                      child: const Text('Hapus filter'),
                    ),
                ],
              ),
            ),
          )
        else
          SliverPadding(
            padding: const EdgeInsets.fromLTRB(16, 0, 16, 16),
            sliver: SliverList.builder(
              itemCount: hasil.length,
              itemBuilder: (_, i) => _KartuAkun(
                key: ValueKey(hasil[i].id),
                user: hasil[i],
                sendiri: hasil[i].id == widget.idSendiri,
                tindakan: widget.onTindakan,
              ),
            ),
          ),
      ],
    );
    // Pertahankan subtree pencarian selama refresh atau mutasi sedang berjalan.
    return RefreshIndicator(
      onRefresh: () async => widget.onRefresh?.call(),
      notificationPredicate: (n) =>
          widget.onRefresh != null && defaultScrollNotificationPredicate(n),
      child: scroll,
    );
  }
}

class _KartuAkun extends StatelessWidget {
  const _KartuAkun({
    super.key,
    required this.user,
    required this.sendiri,
    required this.tindakan,
  });
  final UserInfo user;
  final bool sendiri;
  final void Function(UserInfo, TindakanAkun)? tindakan;

  @override
  Widget build(BuildContext context) {
    final tema = Theme.of(context);
    final warna = tema.colorScheme;
    final peran = switch (user.role) {
      'ADMIN' => 'Admin',
      'KASIR' => 'Kasir',
      'ANGGOTA' => 'Anggota',
      _ => user.role,
    };
    Widget penanda(String teks, IconData ikon, Color tinta) => Text.rich(
      TextSpan(
        children: [
          WidgetSpan(
            alignment: PlaceholderAlignment.middle,
            child: Icon(ikon, size: 15, color: tinta),
          ),
          TextSpan(text: ' $teks'),
        ],
      ),
      style: tema.textTheme.labelMedium?.copyWith(color: tinta),
    );
    final labelStatus = user.aktif ? 'Nonaktifkan' : 'Aktifkan';
    return Card(
      margin: const EdgeInsets.only(top: 6),
      child: Padding(
        padding: const EdgeInsets.fromLTRB(12, 8, 4, 10),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Padding(
              padding: const EdgeInsets.only(top: 4),
              child: UserAvatar(
                id: user.id,
                nama: user.nama,
                fotoVersi: user.fotoVersi,
              ),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(user.nama, style: tema.textTheme.titleMedium),
                  Text('@${user.username}', style: tema.textTheme.bodySmall),
                  if (user.nik != null)
                    Text('NIK ${user.nik}', style: tema.textTheme.bodySmall),
                  const SizedBox(height: 6),
                  Wrap(
                    spacing: 10,
                    runSpacing: 4,
                    children: [
                      Text(peran, style: tema.textTheme.labelMedium),
                      penanda(
                        user.aktif ? 'Aktif' : 'Nonaktif',
                        user.aktif ? _Status.aktif.ikon : _Status.nonaktif.ikon,
                        user.aktif ? warna.primary : warna.onSurfaceVariant,
                      ),
                      if (user.terkunci)
                        penanda('Terkunci', _Status.terkunci.ikon, warna.error),
                    ],
                  ),
                  if (sendiri)
                    Text(
                      'Akun yang Anda gunakan',
                      style: tema.textTheme.bodySmall,
                    ),
                  if (user.terkunci)
                    TextButton.icon(
                      onPressed: tindakan == null
                          ? null
                          : () => tindakan!(user, TindakanAkun.reset),
                      icon: const Icon(Icons.key_outlined),
                      label: const Text('Reset password'),
                    ),
                ],
              ),
            ),
            PopupMenuButton<TindakanAkun>(
              tooltip: 'Tindakan untuk ${user.username}',
              enabled: tindakan != null,
              onSelected: (aksi) => tindakan?.call(user, aksi),
              itemBuilder: (_) => [
                if (!user.terkunci)
                  const PopupMenuItem(
                    value: TindakanAkun.reset,
                    child: ListTile(
                      leading: Icon(Icons.key_outlined),
                      title: Text('Reset password'),
                    ),
                  ),
                PopupMenuItem(
                  value: TindakanAkun.status,
                  enabled: !(sendiri && user.aktif),
                  child: ListTile(
                    enabled: !(sendiri && user.aktif),
                    leading: const Icon(Icons.power_settings_new),
                    title: Text(labelStatus),
                  ),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}
