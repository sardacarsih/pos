import 'dart:convert';
import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kopkar_pos/core/api.dart';

class _Store implements TokenStore {
  @override
  Future<String> deviceId() async => '0123456789abcdef0123456789abcdef';
  @override
  Future<({String access, String refresh})?> read() async =>
      (access: 'access', refresh: 'refresh');
  @override
  Future<void> clear() async {}
  @override
  Future<void> write(String access, String refresh) async {}
}

class _Adapter implements HttpClientAdapter {
  final calls =
      <
        ({
          String method,
          String path,
          String? authorization,
          Map<String, dynamic> body,
        })
      >[];
  @override
  Future<ResponseBody> fetch(
    RequestOptions options,
    Stream<Uint8List>? requestStream,
    Future<void>? cancelFuture,
  ) async {
    final bytes = <int>[];
    if (requestStream != null)
      await for (final chunk in requestStream) {
        bytes.addAll(chunk);
      }
    calls.add((
      method: options.method,
      path: options.path,
      authorization: options.headers['Authorization'] as String?,
      body: jsonDecode(utf8.decode(bytes)) as Map<String, dynamic>,
    ));
    return ResponseBody.fromString('', 204);
  }

  @override
  void close({bool force = false}) {}
}

void main() {
  test(
    'pendaftaran dan penghapusan token memakai sesi dan perangkat yang sama',
    () async {
      final adapter = _Adapter();
      final dio = Dio(BaseOptions(baseUrl: 'https://example.test'))
        ..httpClientAdapter = adapter;
      final api = ApiClient(
        baseUrl: 'https://example.test',
        store: _Store(),
        dio: dio,
      );
      await api.daftarPush('token-pertama');
      await api.daftarPush('token-baru');
      await api.hapusPush();
      expect(adapter.calls.map((c) => c.method), ['PUT', 'PUT', 'DELETE']);
      expect(
        adapter.calls.every(
          (c) =>
              c.path == '/api/v1/push/device' &&
              c.authorization == 'Bearer access',
        ),
        isTrue,
      );
      expect(adapter.calls[1].body['token'], 'token-baru');
      expect(
        adapter.calls.every(
          (c) => c.body['device_id'] == '0123456789abcdef0123456789abcdef',
        ),
        isTrue,
      );
    },
  );
}
