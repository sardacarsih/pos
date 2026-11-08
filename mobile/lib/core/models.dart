int _i(dynamic v) => (v as num?)?.toInt() ?? 0;
String _s(dynamic v) => (v as String?) ?? '';

class Profil {
  Profil({
    required this.role,
    required this.nama,
    this.nik,
    this.gantiPassword = false,
    this.defaultNik = '',
  });
  final String role; // KASIR | ANGGOTA | ADMIN
  final String nama;
  final String? nik;
  final bool gantiPassword;
  final String defaultNik;

  factory Profil.fromLogin(Map<String, dynamic> j) => Profil(
        role: _s(j['role']),
        nama: _s(j['nama']),
        nik: j['nik'] as String?,
        gantiPassword: j['ganti_password'] == true,
      );

  Profil withMe(Map<String, dynamic> j) => Profil(
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
        kategori = _s(j['kategori']);
  final int id;
  final String kode, barcode, nama, satuan, kategoriId, kategori;
  final int harga;
  final int? stok;
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
        qty = _i(j['qty']),
        harga = _i(j['harga']),
        potongan = _i(j['potongan']),
        total = _i(j['total']);
  final String nama, satuan;
  final int qty, harga, potongan, total;
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
        baris = [for (final b in (j['baris'] as List? ?? [])) BarisFaktur.fromJson(b as Map<String, dynamic>)],
        jadwal = [for (final b in (j['jadwal'] as List? ?? [])) Jadwal.fromJson(b as Map<String, dynamic>)];
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
        limit = _i(j['limit']),
        belanja = _i(j['belanja_periode']),
        sisa = (j['sisa_limit'] as num?)?.toInt();
  final String nama;
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

class UserInfo {
  UserInfo.fromJson(Map<String, dynamic> j)
      : id = _i(j['id']),
        username = _s(j['username']),
        nama = _s(j['nama']),
        role = _s(j['role']),
        nik = j['nik'] as String?,
        aktif = j['aktif'] == true,
        terkunci = j['terkunci'] == true,
        kategori = [for (final k in (j['kategori'] as List? ?? [])) k as String];
  final int id;
  final String username, nama, role;
  final String? nik;
  final bool aktif, terkunci;
  final List<String> kategori;
}
