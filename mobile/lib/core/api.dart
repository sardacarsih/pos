import 'dart:typed_data';
import 'dart:async';
import 'dart:math';

import 'package:dio/dio.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import 'models.dart';

class ApiException implements Exception {
  ApiException(
    this.pesan, {
    this.kode = 'KESALAHAN',
    this.status,
    this.offline = false,
  });
  final String pesan, kode;
  final int? status;
  final bool offline;
  @override
  String toString() => pesan;
}

/// Penyimpanan token. Hanya token (bukan password) yang disimpan.
abstract class TokenStore {
  Future<({String access, String refresh})?> read();
  Future<void> write(String access, String refresh);
  Future<void> clear();
  Future<String> deviceId();
}

class SecureTokenStore implements TokenStore {
  const SecureTokenStore([this._s = const FlutterSecureStorage()]);
  final FlutterSecureStorage _s;

  @override
  Future<({String access, String refresh})?> read() async {
    final a = await _s.read(key: 'access');
    final r = await _s.read(key: 'refresh');
    return (a == null || r == null) ? null : (access: a, refresh: r);
  }

  @override
  Future<void> write(String access, String refresh) async {
    await _s.write(key: 'access', value: access);
    await _s.write(key: 'refresh', value: refresh);
  }

  @override
  Future<void> clear() async {
    await _s.delete(key: 'access');
    await _s.delete(key: 'refresh');
  }

  @override
  Future<String> deviceId() async {
    var id = await _s.read(key: 'device_id');
    if (id == null) {
      final r = Random.secure();
      id = List.generate(
        16,
        (_) => r.nextInt(256).toRadixString(16).padLeft(2, '0'),
      ).join();
      await _s.write(key: 'device_id', value: id);
    }
    return id;
  }
}

class ApiClient {
  ApiClient({
    required String baseUrl,
    required this.store,
    Dio? dio,
    this.onExpired,
  }) : _dio =
           dio ??
           Dio(
             BaseOptions(
               baseUrl: baseUrl,
               connectTimeout: const Duration(seconds: 10),
               receiveTimeout: const Duration(seconds: 30),
             ),
           ),
       _bare = Dio(
         BaseOptions(
           baseUrl: baseUrl,
           connectTimeout: const Duration(seconds: 10),
           receiveTimeout: const Duration(seconds: 30),
         ),
       ) {
    _dio.interceptors.add(
      InterceptorsWrapper(
        onRequest: (o, h) async {
          final t = await store.read();
          if (t != null && o.extra['auth'] != false)
            o.headers['Authorization'] = 'Bearer ${t.access}';
          h.next(o);
        },
        onError: (e, h) async {
          if (e.response?.statusCode == 401 &&
              e.requestOptions.extra['auth'] != false &&
              e.requestOptions.extra['retried'] != true) {
            if (await _refresh()) {
              final o = e.requestOptions..extra['retried'] = true;
              try {
                return h.resolve(await _dio.fetch<dynamic>(o));
              } on DioException catch (e2) {
                return h.next(e2);
              }
            }
            onExpired?.call();
          }
          h.next(e);
        },
      ),
    );
  }

  final Dio _dio, _bare;
  final TokenStore store;
  final void Function()? onExpired;
  Completer<bool>? _refreshing;

  /// Satu refresh pada satu waktu; token refresh berputar sehingga tidak boleh dipakai paralel.
  Future<bool> _refresh() {
    final running = _refreshing;
    if (running != null) return running.future;
    final c = _refreshing = Completer<bool>();
    () async {
      try {
        final t = await store.read();
        if (t == null) return c.complete(false);
        final r = await _bare.post<Map<String, dynamic>>(
          '/api/v1/auth/refresh',
          data: {
            'refresh_token': t.refresh,
            'device_id': await store.deviceId(),
          },
        );
        await store.write(
          r.data!['access_token'] as String,
          r.data!['refresh_token'] as String,
        );
        c.complete(true);
      } catch (_) {
        await store.clear();
        c.complete(false);
      } finally {
        _refreshing = null;
      }
    }();
    return c.future;
  }

  Future<dynamic> _call(Future<Response<dynamic>> Function() f) async {
    try {
      return (await f()).data;
    } on DioException catch (e) {
      final d = e.response?.data;
      if (d is Map && d['pesan'] is String) {
        throw ApiException(
          d['pesan'] as String,
          kode: (d['kode'] as String?) ?? 'KESALAHAN',
          status: e.response?.statusCode,
        );
      }
      final offline =
          e.type == DioExceptionType.connectionError ||
          e.type == DioExceptionType.connectionTimeout ||
          e.type == DioExceptionType.receiveTimeout ||
          e.type == DioExceptionType.sendTimeout;
      throw ApiException(
        offline ? 'Tidak dapat terhubung ke server' : 'Terjadi kesalahan',
        status: e.response?.statusCode,
        offline: offline,
      );
    }
  }

  Future<dynamic> get(String p, {Map<String, dynamic>? q}) =>
      _call(() => _dio.get<dynamic>(p, queryParameters: q));
  Future<dynamic> post(
    String p, {
    Object? body,
    bool auth = true,
    Map<String, String>? headers,
  }) => _call(
    () => _dio.post<dynamic>(
      p,
      data: body,
      options: Options(extra: {'auth': auth}, headers: headers),
    ),
  );
  Future<dynamic> put(String p, {Object? body}) =>
      _call(() => _dio.put<dynamic>(p, data: body));

  // ---- auth ----
  Future<Profil> login(String username, String password) async {
    final j = await post(
      '/api/v1/auth/login',
      auth: false,
      body: {
        'username': username,
        'password': password,
        'device_id': await store.deviceId(),
      },
    ) as Map<String, dynamic>;
    await store.write(
      j['access_token'] as String,
      j['refresh_token'] as String,
    );
    return Profil.fromLogin(j);
  }

  Future<Profil> me(Profil base) async =>
      base.withMe(await get('/api/v1/me') as Map<String, dynamic>);

  /// Memulihkan sesi dari token tersimpan; null bila tidak ada / sudah tidak valid.
  Future<Profil?> pulihkan() async {
    if (await store.read() == null) return null;
    try {
      final j = await get('/api/v1/me') as Map<String, dynamic>;
      return Profil(
        id: (j['id'] as num?)?.toInt() ?? 0,
        role: j['role'] as String,
        nama: j['nama'] as String,
        nik: j['nik'] as String?,
        gantiPassword: j['ganti_password'] == true,
        defaultNik: (j['default_nik'] as String?) ?? '',
      );
    } on ApiException catch (e) {
      if (e.status == 401) await store.clear();
      if (e.offline) rethrow;
      return null;
    }
  }

  Future<void> logout() async {
    final t = await store.read();
    try {
      if (t != null)
        await post(
          '/api/v1/auth/logout',
          body: {'refresh_token': t.refresh},
          auth: false,
        );
    } finally {
      await store.clear();
    }
  }

  Future<void> gantiPassword(String lama, String baru) async {
    await post(
      '/api/v1/auth/ganti-password',
      body: {'password_lama': lama, 'password_baru': baru},
    );
    await store.clear(); // semua sesi dicabut server; login ulang
  }

  // ---- kasir ----
  Future<({List<Barang> items, bool lagi})> barang({
    String? q,
    String? kategori,
    int page = 1,
  }) async {
    final j = await get(
      '/api/v1/barang',
      q: {
        if (q != null && q.isNotEmpty) 'q': q,
        'kategori': ?kategori,
        'page': page,
        'size': 30,
      },
    ) as Map<String, dynamic>;
    return (
      items: [
        for (final b in j['items'] as List)
          Barang.fromJson(b as Map<String, dynamic>),
      ],
      lagi: j['ada_lagi'] == true,
    );
  }

  Future<Barang> barangBarcode(String kode) async => Barang.fromJson(
    await get('/api/v1/barang/by-barcode/${Uri.encodeComponent(kode)}')
        as Map<String, dynamic>,
  );

  Future<List<Kategori>> kategori() async => [
    for (final k in await get('/api/v1/kategori') as List)
      Kategori.fromJson(k as Map<String, dynamic>),
  ];

  Future<({List<Pelanggan> items, bool lagi})> pelanggan({
    String? q,
    int page = 1,
  }) async {
    final j = await get(
      '/api/v1/pelanggan',
      q: {if (q != null && q.isNotEmpty) 'q': q, 'page': page, 'size': 30},
    ) as Map<String, dynamic>;
    return (
      items: [
        for (final b in j['items'] as List)
          Pelanggan.fromJson(b as Map<String, dynamic>),
      ],
      lagi: j['ada_lagi'] == true,
    );
  }

  Future<Pelanggan> pelangganNik(String nik) async => Pelanggan.fromJson(
    await get('/api/v1/pelanggan/by-nik/${Uri.encodeComponent(nik)}')
        as Map<String, dynamic>,
  );

  Future<String> nomor({required String idempotencyKey}) async =>
      (await post(
            '/api/v1/transaksi/nomor',
            headers: {'Idempotency-Key': idempotencyKey},
          ) as Map<String, dynamic>)['no_transaksi']
          as String;

  Future<DashboardKasir> dashboardKasir() async => DashboardKasir.fromJson(
    await get('/api/v1/kasir/dashboard') as Map<String, dynamic>,
  );

  /// Riwayat transaksi kasir pada [tanggal] (YYYY-MM-DD); null = hari ini server.
  Future<({String tanggal, List<TransaksiTerakhir> items})> riwayat({
    String? tanggal,
  }) async {
    final j = await get(
      '/api/v1/kasir/riwayat',
      q: {'tanggal': ?tanggal},
    ) as Map<String, dynamic>;
    return (
      tanggal: j['tanggal'] as String,
      items: [
        for (final t in j['items'] as List)
          TransaksiTerakhir.fromJson(t as Map<String, dynamic>),
      ],
    );
  }

  Future<Map<String, dynamic>> periodeStatus() async =>
      await get('/api/v1/periode/status') as Map<String, dynamic>;

  /// Kunci UUID dan payload yang sama mengembalikan faktur tersimpan tanpa duplikasi.
  Future<Faktur> simpan({
    required String idempotencyKey,
    required String nomor,
    required String nik,
    required int tenor,
    required int? uangBayar,
    required List<Map<String, Object>> baris,
  }) async => Faktur.fromJson(
    await post(
      '/api/v1/faktur',
      headers: {'Idempotency-Key': idempotencyKey},
      body: {
        'no_transaksi': nomor,
        'nik': nik,
        'tenor': tenor,
        'uang_bayar': ?uangBayar,
        'baris': baris,
      },
    ) as Map<String, dynamic>,
  );

  // ---- anggota ----
  Future<Ringkasan> ringkasan(int periode) async => Ringkasan.fromJson(
    await get('/api/v1/saya/ringkasan', q: {'periode': periode})
        as Map<String, dynamic>,
  );

  Future<({List<TransaksiRingkas> items, bool lagi})> transaksi(
    int periode, {
    int page = 1,
  }) async {
    final j = await get(
      '/api/v1/saya/transaksi',
      q: {'periode': periode, 'page': page, 'size': 30},
    ) as Map<String, dynamic>;
    return (
      items: [
        for (final b in j['items'] as List)
          TransaksiRingkas.fromJson(b as Map<String, dynamic>),
      ],
      lagi: j['ada_lagi'] == true,
    );
  }

  Future<DashboardAnggota> dashboardSaya() async => DashboardAnggota.fromJson(
    await get('/api/v1/saya/dashboard') as Map<String, dynamic>,
  );

  Future<String> nomorSaya({required String idempotencyKey}) async =>
      (await post(
            '/api/v1/saya/belanja/nomor',
            headers: {'Idempotency-Key': idempotencyKey},
          ) as Map<String, dynamic>)['no_transaksi']
          as String;

  /// Belanja mandiri: NIK diambil server dari token; selalu kredit.
  Future<Faktur> simpanSaya({
    required String idempotencyKey,
    required String nomor,
    required int tenor,
    required List<Map<String, Object>> baris,
  }) async => Faktur.fromJson(
    await post(
      '/api/v1/saya/belanja',
      headers: {'Idempotency-Key': idempotencyKey},
      body: {'no_transaksi': nomor, 'nik': '', 'tenor': tenor, 'baris': baris},
    ) as Map<String, dynamic>,
  );

  Future<Faktur> detail(String no) async => Faktur.fromJson(
    await get('/api/v1/saya/transaksi/${Uri.encodeComponent(no)}')
        as Map<String, dynamic>,
  );

  Future<Faktur> fakturKasir(String no) async => Faktur.fromJson(
    await get('/api/v1/faktur/${Uri.encodeComponent(no)}')
        as Map<String, dynamic>,
  );

  // ---- admin ----
  Future<List<UserInfo>> users() async => [
    for (final u in await get('/api/v1/admin/users') as List)
      UserInfo.fromJson(u as Map<String, dynamic>),
  ];

  Future<String> buatUser({
    required String username,
    required String nama,
    required String role,
    String? nik,
  }) async =>
      ((await post(
            '/api/v1/admin/users',
            body: {
              'username': username,
              'nama': nama,
              'role': role,
              'nik': ?nik,
            },
          ) as Map<String, dynamic>)['password_sementara']
          as String);

  Future<String> resetPassword(int id) async =>
      ((await post('/api/v1/admin/users/$id/reset-password')
              as Map<String, dynamic>)['password_sementara']
          as String);

  Future<void> setAktif(int id, bool aktif) async =>
      put('/api/v1/admin/users/$id/aktif', body: {'aktif': aktif});

  Future<List<String>> kategoriTampil() async => [
    for (final k in await get('/api/v1/admin/kategori-tampil') as List)
      k as String,
  ];

  Future<void> setKategoriTampil(List<String> ids) async =>
      put('/api/v1/admin/kategori-tampil', body: {'kategori_ids': ids});

  Future<void> setUrutProduk(List<int> ids) async =>
      put('/api/v1/admin/produk-urut', body: {'produk_ids': ids});

  Future<List<Kategori>> semuaKategori() async => [
    for (final k in await get('/api/v1/admin/kategori') as List)
      Kategori.fromJson(k as Map<String, dynamic>),
  ];

  Future<DashboardAdmin> dashboardAdmin() async => DashboardAdmin.fromJson(
    await get('/api/v1/admin/dashboard') as Map<String, dynamic>,
  );

  /// aktif: 'Y' | 'N' | null (semua).
  Future<({List<ProdukAdmin> items, bool lagi})> produkAdmin({
    String? q,
    String? kategori,
    String? aktif,
    int page = 1,
  }) async {
    final j = await get(
      '/api/v1/admin/produk',
      q: {
        if (q != null && q.isNotEmpty) 'q': q,
        'kategori': ?kategori,
        'aktif': ?aktif,
        'page': page,
        'size': 30,
      },
    ) as Map<String, dynamic>;
    return (
      items: [
        for (final p in j['items'] as List)
          ProdukAdmin.fromJson(p as Map<String, dynamic>),
      ],
      lagi: j['ada_lagi'] == true,
    );
  }

  Future<ProdukAdmin> ubahProduk(
    int id, {
    required String nama,
    required String barcode,
    required String satuan,
    required String kategoriId,
    required int harga,
    required int beli,
    required bool aktif,
  }) async => ProdukAdmin.fromJson(
    await put(
      '/api/v1/admin/produk/$id',
      body: {
        'nama': nama,
        'barcode': barcode,
        'satuan': satuan,
        'kategori_id': kategoriId,
        'harga': harga,
        'beli': beli,
        'aktif': aktif,
      },
    ) as Map<String, dynamic>,
  );

  /// Foto produk unggahan admin (bytes). 404 -> ApiException.
  Future<Uint8List> fotoProduk(int id) async {
    try {
      final r = await _dio.get<List<int>>(
        '/api/v1/produk/$id/foto',
        options: Options(responseType: ResponseType.bytes),
      );
      return Uint8List.fromList(r.data ?? const []);
    } on DioException catch (e) {
      throw ApiException(
        e.response?.statusCode == 404 ? 'Foto tidak ada' : 'Foto gagal dimuat',
        status: e.response?.statusCode,
      );
    }
  }

  /// Unggah foto (PNG/JPEG, maks 1 MB; jenis dicek server dari isi file). Mengembalikan versi baru.
  Future<int> unggahFoto(int id, Uint8List bytes) async {
    final j = await _call(
      () => _dio.put<dynamic>(
        '/api/v1/admin/produk/$id/foto',
        data: Stream<List<int>>.fromIterable([bytes]),
        options: Options(
          contentType: 'application/octet-stream',
          headers: {Headers.contentLengthHeader: bytes.length},
        ),
      ),
    ) as Map<String, dynamic>;
    return (j['versi'] as num).toInt();
  }

  Future<void> hapusFoto(int id) async {
    await _call(() => _dio.delete<dynamic>('/api/v1/admin/produk/$id/foto'));
  }

  // ---- profil sendiri ----
  Future<Map<String, dynamic>> profilSaya() async =>
      await get('/api/v1/profil') as Map<String, dynamic>;

  Future<void> ubahProfil({
    required String nama,
    required String telepon,
    required String email,
  }) async {
    await put(
      '/api/v1/profil',
      body: {'nama': nama, 'telepon': telepon, 'email': email},
    );
  }

  Future<Uint8List> fotoProfil() async {
    try {
      final r = await _dio.get<List<int>>(
        '/api/v1/profil/foto',
        options: Options(responseType: ResponseType.bytes),
      );
      return Uint8List.fromList(r.data ?? const []);
    } on DioException catch (e) {
      throw ApiException('Foto gagal dimuat', status: e.response?.statusCode);
    }
  }

  Future<void> unggahFotoProfil(Uint8List bytes) async {
    await _call(
      () => _dio.put<dynamic>(
        '/api/v1/profil/foto',
        data: Stream<List<int>>.fromIterable([bytes]),
        options: Options(
          contentType: 'application/octet-stream',
          headers: {Headers.contentLengthHeader: bytes.length},
        ),
      ),
    );
  }

  /// Foto profil akun lain untuk daftar admin. 404 -> ApiException.
  Future<Uint8List> fotoUser(int id) async {
    try {
      final r = await _dio.get<List<int>>(
        '/api/v1/admin/users/$id/foto',
        options: Options(responseType: ResponseType.bytes),
      );
      return Uint8List.fromList(r.data ?? const []);
    } on DioException catch (e) {
      throw ApiException('Foto tidak ada', status: e.response?.statusCode);
    }
  }

  /// Foto anggota untuk kasir (verifikasi identitas). 404 -> ApiException.
  Future<Uint8List> fotoAnggota(String nik) async {
    try {
      final r = await _dio.get<List<int>>(
        '/api/v1/pelanggan/${Uri.encodeComponent(nik)}/foto',
        options: Options(responseType: ResponseType.bytes),
      );
      return Uint8List.fromList(r.data ?? const []);
    } on DioException catch (e) {
      throw ApiException('Foto tidak ada', status: e.response?.statusCode);
    }
  }

  Future<void> hapusFotoProfil() async {
    await _call(() => _dio.delete<dynamic>('/api/v1/profil/foto'));
  }
}
