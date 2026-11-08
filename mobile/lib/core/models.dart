int _i(dynamic v) => (v as num?)?.toInt() ?? 0;
String _s(dynamic v) => (v as String?) ?? '';

class Profil {
  Profil({
    this.id = 0,
    required this.role,
    required this.nama,
    this.nik,
    this.gantiPassword = false,
    this.defaultNik = '',
  });
  final int id;
  final String role; // KASIR | ANGGOTA | ADMIN
  final String nama;
  final String? nik;
  final bool gantiPassword;
  final String defaultNik;

  factory Profil.fromLogin(Map<String, dynamic> j) => Profil(
    id: _i(j['id']),
    role: _s(j['role']),
    nama: _s(j['nama']),
    nik: j['nik'] as String?,
    gantiPassword: j['ganti_password'] == true,
  );

  Profil withMe(Map<String, dynamic> j) => Profil(
    id: _i(j['id']),
    role: role,
    nama: nama,
    nik: nik,
    gantiPassword: gantiPassword,
    defaultNik: _s(j['default_nik']),
  );
}

class Barang {
  Barang.fromJson(Map<String, dynamic> j)
    : id = _i(j['id']),
      kode = _s(j['kode']),
      barcode = _s(j['barcode']),
      nama = _s(j['nama']),
      satuan = _s(j['satuan']),
      harga = _i(j['harga']),
      stok = (j['stok'] as num?)?.toInt(),
      kategoriId = _s(j['kategori_id']),
      kategori = _s(j['kategori']),
      fotoVersi = (j['foto'] as num?)?.toInt();
  final int id;
  final String kode, barcode, nama, satuan, kategoriId, kategori;
  final int harga;
  final int? stok;

  /// Versi foto unggahan admin (epoch detik); null = tidak ada, pakai ikon/foto bawaan.
  final int? fotoVersi;

  bool get dijualPerLiter {
    final s = satuan.trim().toLowerCase();
    return s == 'l' || s == 'ltr' || s == 'liter' || s == 'litre';
  }
}

class Kategori {
  Kategori.fromJson(Map<String, dynamic> j)
    : id = _s(j['id']),
      nama = _s(j['nama']);
  final String id, nama;
}

class Pelanggan {
  Pelanggan.fromJson(Map<String, dynamic> j)
    : nik = _s(j['nik']),
      nama = _s(j['nama']),
      unit = _s(j['unit_nama']),
      status = _s(j['status']),
      limit = _i(j['limit']),
      tunai = j['tunai'] == true;
  final String nik, nama, unit, status;
  final int limit;
  final bool tunai;
}

class BarisFaktur {
  BarisFaktur.fromJson(Map<String, dynamic> j)
    : nama = _s(j['nama']),
      satuan = _s(j['satuan']),
      qty = (j['qty'] as num?)?.toDouble() ?? 0,
      harga = _i(j['harga']),
      potongan = _i(j['potongan']),
      total = _i(j['total']);
  final String nama, satuan;
  final double qty;
  final int harga, potongan, total;
}

class Jadwal {
  Jadwal.fromJson(Map<String, dynamic> j)
    : ke = _i(j['ke']),
      jatuhTempo = _s(j['jatuh_tempo']),
      angsuran = _i(j['angsuran']),
      saldoAkhir = _i(j['saldo_akhir']);
  final int ke, angsuran, saldoAkhir;
  final String jatuhTempo;
}

class Faktur {
  Faktur.fromJson(Map<String, dynamic> j)
    : no = _s(j['no_transaksi']),
      tanggal = _s(j['tanggal']),
      jam = _s(j['jam']),
      namaKasir = _s(j['nama_kasir']),
      nik = _s(j['nik']),
      namaPelanggan = _s(j['nama_pelanggan']),
      jenisBayar = _s(j['jenis_bayar']),
      tenor = _i(j['tenor']),
      bruto = _i(j['bruto']),
      potongan = _i(j['potongan']),
      total = _i(j['total']),
      duplikat = j['duplikat'] == true,
      uangBayar = (j['uang_bayar'] as num?)?.toInt(),
      kembalian = (j['kembalian'] as num?)?.toInt(),
      baris = [
        for (final b in (j['baris'] as List? ?? []))
          BarisFaktur.fromJson(b as Map<String, dynamic>),
      ],
      jadwal = [
        for (final b in (j['jadwal'] as List? ?? []))
          Jadwal.fromJson(b as Map<String, dynamic>),
      ];
  final String no, tanggal, jam, namaKasir, nik, namaPelanggan, jenisBayar;
  final int tenor, bruto, potongan, total;
  final int? uangBayar, kembalian;
  final bool duplikat;
  final List<BarisFaktur> baris;
  final List<Jadwal> jadwal;
}

class Ringkasan {
  Ringkasan.fromJson(Map<String, dynamic> j)
    : nama = _s(j['nama']),
      periode = _i(j['periode']),
      dari = _s(j['dari']),
      sampai = _s(j['sampai']),
      limit = _i(j['limit']),
      belanja = _i(j['belanja_periode']),
      sisa = (j['sisa_limit'] as num?)?.toInt();
  final String nama, dari, sampai; // dari/sampai: YYYY-MM-DD
  final int periode, limit, belanja;
  final int? sisa; // null = tidak dibatasi
}

class TransaksiRingkas {
  TransaksiRingkas.fromJson(Map<String, dynamic> j)
    : no = _s(j['no_transaksi']),
      tanggal = _s(j['tanggal']),
      jam = _s(j['jam']),
      jenisBayar = _s(j['jenis_bayar']),
      tenor = _i(j['tenor']),
      total = _i(j['total']);
  final String no, tanggal, jam, jenisBayar;
  final int tenor, total;
}

class DashboardAnggota {
  DashboardAnggota.fromJson(Map<String, dynamic> j)
    : belanjaDemo = j['belanja_demo'] == true,
      periodeTerbuka = j['periode_terbuka'] == true,
      pesan = j['pesan'] as String?,
      ringkasan = j['ringkasan'] == null
          ? null
          : Ringkasan.fromJson(j['ringkasan'] as Map<String, dynamic>),
      terakhir = [
        for (final t in j['terakhir'] as List)
          TransaksiRingkas.fromJson(t as Map<String, dynamic>),
      ];
  final bool periodeTerbuka;
  final bool belanjaDemo; // true = belanja mandiri hanya simulasi (tidak disimpan)
  final String? pesan;
  final Ringkasan? ringkasan; // null = tanggal di luar semua periode
  final List<TransaksiRingkas> terakhir;
}

class TransaksiTerakhir {
  TransaksiTerakhir.fromJson(Map<String, dynamic> j)
    : no = _s(j['no_transaksi']),
      jam = _s(j['jam']),
      pelanggan = _s(j['nama_pelanggan']),
      jenisBayar = _s(j['jenis_bayar']),
      total = _i(j['total']);
  final String no, jam, pelanggan, jenisBayar;
  final int total;
}

/// Ringkasan hari ini milik kasir yang login (server membatasi ke username token).
class DashboardKasir {
  DashboardKasir.fromJson(Map<String, dynamic> j)
    : tanggal = _s(j['tanggal']),
      periodeTerbuka = j['periode_terbuka'] == true,
      periodePesan = j['periode_pesan'] as String?,
      periodeKode = j['periode_kode'] as String?,
      jumlah = _i((j['ringkasan'] as Map)['jumlah']),
      total = _i((j['ringkasan'] as Map)['total']),
      tunai = _i((j['ringkasan'] as Map)['tunai']),
      kredit = _i((j['ringkasan'] as Map)['kredit']),
      terakhir = [
        for (final t in (j['terakhir'] as List? ?? []))
          TransaksiTerakhir.fromJson(t as Map<String, dynamic>),
      ];
  final String tanggal;
  final bool periodeTerbuka;
  final String? periodePesan;

  /// PERIODE_TUTUP | PERIODE_TIDAK_ADA | PERIODE_TIDAK_VALID; null = terbuka.
  final String? periodeKode;
  final int jumlah, total, tunai, kredit;
  final List<TransaksiTerakhir> terakhir;
}

class PerKasir {
  PerKasir.fromJson(Map<String, dynamic> j)
    : kasir = _s(j['kasir']),
      nama = _s(j['nama']),
      jumlah = _i(j['jumlah']),
      total = _i(j['total']);
  final String kasir, nama;
  final int jumlah, total;
}

class Terlaris {
  Terlaris.fromJson(Map<String, dynamic> j)
    : kode = _s(j['kode']),
      nama = _s(j['nama']),
      qty = (j['qty'] as num?)?.toDouble() ?? 0,
      total = _i(j['total']);
  final String kode, nama;
  final double qty;
  final int total;
}

/// Ringkasan hari ini semua kasir + user mobile untuk dashboard admin.
class DashboardAdmin {
  DashboardAdmin.fromJson(Map<String, dynamic> j)
    : tanggal = _s(j['tanggal']),
      periodeTerbuka = j['periode_terbuka'] == true,
      periodePesan = j['periode_pesan'] as String?,
      periodeKode = j['periode_kode'] as String?,
      jumlah = _i((j['ringkasan'] as Map)['jumlah']),
      total = _i((j['ringkasan'] as Map)['total']),
      tunai = _i((j['ringkasan'] as Map)['tunai']),
      kredit = _i((j['ringkasan'] as Map)['kredit']),
      perKasir = [
        for (final k in (j['per_kasir'] as List? ?? []))
          PerKasir.fromJson(k as Map<String, dynamic>),
      ],
      terlaris = [
        for (final t in (j['terlaris'] as List? ?? []))
          Terlaris.fromJson(t as Map<String, dynamic>),
      ],
      userAdmin = _i((j['user'] as Map)['admin']),
      userKasir = _i((j['user'] as Map)['kasir']),
      userAnggota = _i((j['user'] as Map)['anggota']),
      userNonaktif = _i((j['user'] as Map)['nonaktif']),
      userTerkunci = _i((j['user'] as Map)['terkunci']);
  final String tanggal;
  final bool periodeTerbuka;
  final String? periodePesan, periodeKode;
  final int jumlah, total, tunai, kredit;
  final List<PerKasir> perKasir;
  final List<Terlaris> terlaris;
  final int userAdmin, userKasir, userAnggota, userNonaktif, userTerkunci;
}

class ProdukAdmin {
  ProdukAdmin.fromJson(Map<String, dynamic> j)
    : id = _i(j['id']),
      kode = _s(j['kode']),
      barcode = _s(j['barcode']),
      nama = _s(j['nama']),
      satuan = _s(j['satuan']),
      harga = _i(j['harga']),
      beli = _i(j['beli']),
      kategoriId = _s(j['kategori_id']),
      kategori = _s(j['kategori']),
      aktif = j['aktif'] == true,
      fotoVersi = (j['foto_versi'] as num?)?.toInt();
  final int? fotoVersi;
  final int id, harga, beli;
  final String kode, barcode, nama, satuan, kategoriId, kategori;
  final bool aktif;
}

class UserInfo {
  UserInfo.fromJson(Map<String, dynamic> j)
    : id = _i(j['id']),
      username = _s(j['username']),
      nama = _s(j['nama']),
      role = _s(j['role']),
      nik = j['nik'] as String?,
      aktif = j['aktif'] == true,
      terkunci = j['terkunci'] == true,
      fotoVersi = (j['foto_versi'] as num?)?.toInt(),
      kategori = [for (final k in (j['kategori'] as List? ?? [])) k as String];
  final int id;
  final String username, nama, role;
  final String? nik;
  final bool aktif, terkunci;
  final int? fotoVersi; // null = tanpa foto profil
  final List<String> kategori;
}
