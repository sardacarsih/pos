import 'dart:async';
import 'dart:math';

import 'package:dio/dio.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import 'models.dart';

class ApiException implements Exception {
  ApiException(this.pesan, {this.kode = 'KESALAHAN', this.status, this.offline = false});
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
      id = List.generate(16, (_) => r.nextInt(256).toRadixString(16).padLeft(2, '0')).join();
      await _s.write(key: 'device_id', value: id);
    }
    return id;
  }
}

class ApiClient {
  ApiClient({required String baseUrl, required this.store, Dio? dio, this.onExpired})
      : _dio = dio ?? Dio(BaseOptions(baseUrl: baseUrl, connectTimeout: const Duration(seconds: 10), receiveTimeout: const Duration(seconds: 30))),
        _bare = Dio(BaseOptions(baseUrl: baseUrl, connectTimeout: const Duration(seconds: 10), receiveTimeout: const Duration(seconds: 30))) {
    _dio.interceptors.add(InterceptorsWrapper(onRequest: (o, h) async {
      final t = await store.read();
      if (t != null && o.extra['auth'] != false) o.headers['Authorization'] = 'Bearer ${t.access}';
      h.next(o);
    }, onError: (e, h) async {
      if (e.response?.statusCode == 401 && e.requestOptions.extra['auth'] != false && e.requestOptions.extra['retried'] != true) {
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
    }));
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
        final r = await _bare.post<Map<String, dynamic>>('/api/v1/auth/refresh', data: {'refresh_token': t.refresh, 'device_id': await store.deviceId()});
        await store.write(r.data!['access_token'] as String, r.data!['refresh_token'] as String);
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
        throw ApiException(d['pesan'] as String, kode: (d['kode'] as String?) ?? 'KESALAHAN', status: e.response?.statusCode);
      }
      final offline = e.type == DioExceptionType.connectionError || e.type == DioExceptionType.connectionTimeout || e.type == DioExceptionType.receiveTimeout || e.type == DioExceptionType.sendTimeout;
      throw ApiException(offline ? 'Tidak dapat terhubung ke server' : 'Terjadi kesalahan', status: e.response?.statusCode, offline: offline);
    }
  }

  Future<dynamic> get(String p, {Map<String, dynamic>? q}) => _call(() => _dio.get<dynamic>(p, queryParameters: q));
  Future<dynamic> post(String p, {Object? body, bool auth = true}) =>
      _call(() => _dio.post<dynamic>(p, data: body, options: Options(extra: {'auth': auth})));
  Future<dynamic> put(String p, {Object? body}) => _call(() => _dio.put<dynamic>(p, data: body));

  // ---- auth ----
  Future<Profil> login(String username, String password) async {
    final j = await post('/api/v1/auth/login', auth: false, body: {'username': username, 'password': password, 'device_id': await store.deviceId()}) as Map<String, dynamic>;
    await store.write(j['access_token'] as String, j['refresh_token'] as String);
    return Profil.fromLogin(j);
  }

  Future<Profil> me(Profil base) async => base.withMe(await get('/api/v1/me') as Map<String, dynamic>);

  /// Memulihkan sesi dari token tersimpan; null bila tidak ada / sudah tidak valid.
  Future<Profil?> pulihkan() async {
    if (await store.read() == null) return null;
    try {
      final j = await get('/api/v1/me') as Map<String, dynamic>;
      return Profil(role: j['role'] as String, nama: j['nama'] as String, nik: j['nik'] as String?, gantiPassword: j['ganti_password'] == true, defaultNik: (j['default_nik'] as String?) ?? '');
    } on ApiException catch (e) {
      if (e.status == 401) await store.clear();
      if (e.offline) rethrow;
      return null;
    }
  }

  Future<void> logout() async {
    final t = await store.read();
    try {
      if (t != null) await post('/api/v1/auth/logout', body: {'refresh_token': t.refresh}, auth: false);
    } finally {
      await store.clear();
    }
  }

  Future<void> gantiPassword(String lama, String baru) async {
    await post('/api/v1/auth/ganti-password', body: {'password_lama': lama, 'password_baru': baru});
    await store.clear(); // semua sesi dicabut server; login ulang
  }

  // ---- kasir ----
  Future<({List<Barang> items, bool lagi})> barang({String? q, String? kategori, int page = 1}) async {
    final j = await get('/api/v1/barang', q: {if (q != null && q.isNotEmpty) 'q': q, 'kategori': ?kategori, 'page': page, 'size': 30}) as Map<String, dynamic>;
    return (items: [for (final b in j['items'] as List) Barang.fromJson(b as Map<String, dynamic>)], lagi: j['ada_lagi'] == true);
  }

  Future<Barang> barangBarcode(String kode) async => Barang.fromJson(await get('/api/v1/barang/by-barcode/${Uri.encodeComponent(kode)}') as Map<String, dynamic>);

  Future<List<Kategori>> kategori() async => [for (final k in await get('/api/v1/kategori') as List) Kategori.fromJson(k as Map<String, dynamic>)];

  Future<({List<Pelanggan> items, bool lagi})> pelanggan({String? q, int page = 1}) async {
    final j = await get('/api/v1/pelanggan', q: {if (q != null && q.isNotEmpty) 'q': q, 'page': page, 'size': 30}) as Map<String, dynamic>;
    return (items: [for (final b in j['items'] as List) Pelanggan.fromJson(b as Map<String, dynamic>)], lagi: j['ada_lagi'] == true);
  }

  Future<Pelanggan> pelangganNik(String nik) async => Pelanggan.fromJson(await get('/api/v1/pelanggan/by-nik/${Uri.encodeComponent(nik)}') as Map<String, dynamic>);

  Future<String> nomor() async => (await post('/api/v1/transaksi/nomor') as Map<String, dynamic>)['no_transaksi'] as String;

  Future<Map<String, dynamic>> periodeStatus() async => await get('/api/v1/periode/status') as Map<String, dynamic>;

  /// Idempotent: kunci = nomor transaksi. Mengulang dengan nomor sama mengembalikan faktur yang sama.
  Future<Faktur> simpan({required String nomor, required String nik, required int tenor, required int? uangBayar, required List<Map<String, int>> baris}) async =>
      Faktur.fromJson(await post('/api/v1/faktur', body: {'no_transaksi': nomor, 'nik': nik, 'tenor': tenor, 'uang_bayar': ?uangBayar, 'baris': baris}) as Map<String, dynamic>);

  // ---- anggota ----
  Future<Ringkasan> ringkasan(int periode) async => Ringkasan.fromJson(await get('/api/v1/saya/ringkasan', q: {'periode': periode}) as Map<String, dynamic>);

  Future<({List<TransaksiRingkas> items, bool lagi})> transaksi(int periode, {int page = 1}) async {
    final j = await get('/api/v1/saya/transaksi', q: {'periode': periode, 'page': page, 'size': 30}) as Map<String, dynamic>;
    return (items: [for (final b in j['items'] as List) TransaksiRingkas.fromJson(b as Map<String, dynamic>)], lagi: j['ada_lagi'] == true);
  }

  Future<Faktur> detail(String no) async => Faktur.fromJson(await get('/api/v1/saya/transaksi/${Uri.encodeComponent(no)}') as Map<String, dynamic>);

  // ---- admin ----
  Future<List<UserInfo>> users() async => [for (final u in await get('/api/v1/admin/users') as List) UserInfo.fromJson(u as Map<String, dynamic>)];

  Future<String> buatUser({required String username, required String nama, required String role, String? nik, List<String>? kategori}) async =>
      ((await post('/api/v1/admin/users', body: {'username': username, 'nama': nama, 'role': role, 'nik': ?nik, 'kategori': ?kategori}) as Map<String, dynamic>)['password_sementara'] as String);

  Future<String> resetPassword(int id) async => ((await post('/api/v1/admin/users/$id/reset-password') as Map<String, dynamic>)['password_sementara'] as String);

  Future<void> setAktif(int id, bool aktif) async => put('/api/v1/admin/users/$id/aktif', body: {'aktif': aktif});

  Future<void> setKategoriUser(int id, List<String> ids) async => put('/api/v1/admin/users/$id/kategori', body: {'kategori_ids': ids});

  Future<List<Kategori>> semuaKategori() async => [for (final k in await get('/api/v1/admin/kategori') as List) Kategori.fromJson(k as Map<String, dynamic>)];
}
