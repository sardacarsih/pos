import 'dart:convert';

import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:uuid/uuid.dart';

import 'models.dart';

/// A checkout is immutable once its first network request starts.
class CheckoutDraft {
  const CheckoutDraft({
    required this.key,
    required this.nik,
    required this.tenor,
    required this.uangBayar,
    required this.baris,
    this.nomor,
  });

  final String key;
  final String? nomor;
  final String nik;
  final int tenor;
  final int? uangBayar;
  final List<Map<String, Object>> baris;

  CheckoutDraft withNomor(String value) => CheckoutDraft(
    key: key,
    nomor: value,
    nik: nik,
    tenor: tenor,
    uangBayar: uangBayar,
    baris: baris,
  );

  Map<String, Object?> toJson() => {
    'key': key,
    'nomor': nomor,
    'nik': nik,
    'tenor': tenor,
    'uang_bayar': uangBayar,
    'baris': baris,
  };

  factory CheckoutDraft.fromJson(Map<String, dynamic> json) => CheckoutDraft(
    key: json['key'] as String,
    nomor: json['nomor'] as String?,
    nik: json['nik'] as String,
    tenor: (json['tenor'] as num).toInt(),
    uangBayar: (json['uang_bayar'] as num?)?.toInt(),
    baris: [
      for (final row in json['baris'] as List)
        Map<String, Object>.from(row as Map),
    ],
  );
}

abstract interface class CheckoutStore {
  Future<CheckoutDraft?> read(int userId);
  Future<void> write(int userId, CheckoutDraft draft);
  Future<void> clear(int userId);
}

/// Pending NIK and request payload stay encrypted and are isolated per cashier.
class SecureCheckoutStore implements CheckoutStore {
  const SecureCheckoutStore([this._storage = const FlutterSecureStorage()]);
  final FlutterSecureStorage _storage;

  String _storageKey(int userId) => 'pending_checkout_$userId';

  @override
  Future<CheckoutDraft?> read(int userId) async {
    final raw = await _storage.read(key: _storageKey(userId));
    if (raw == null) return null;
    try {
      return CheckoutDraft.fromJson(jsonDecode(raw) as Map<String, dynamic>);
    } on Object {
      await clear(userId);
      return null;
    }
  }

  @override
  Future<void> write(int userId, CheckoutDraft draft) => _storage.write(
    key: _storageKey(userId),
    value: jsonEncode(draft.toJson()),
  );

  @override
  Future<void> clear(int userId) => _storage.delete(key: _storageKey(userId));
}

String newCheckoutKey() => const Uuid().v4();

/// Reconcile a persisted attempt: check the server first, replay only when absent.
Future<Faktur> pulihkanCheckout({
  required int userId,
  required CheckoutDraft draft,
  required CheckoutStore store,
  required Future<String> Function(String key) terbitkanNomor,
  required Future<Faktur?> Function(String nomor) cariFaktur,
  required Future<Faktur> Function(CheckoutDraft draft) simpan,
}) async {
  final nomor = draft.nomor ?? await terbitkanNomor(draft.key);
  final siap = draft.nomor == nomor ? draft : draft.withNomor(nomor);
  if (draft.nomor == null) await store.write(userId, siap);

  final tersimpan = await cariFaktur(nomor);
  final hasil = tersimpan ?? await simpan(siap);
  await store.clear(userId);
  return hasil;
}
