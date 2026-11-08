import 'package:flutter_test/flutter_test.dart';
import 'package:kopkar_pos/core/checkout.dart';
import 'package:kopkar_pos/core/models.dart';

class _MemoryCheckoutStore implements CheckoutStore {
  final Map<int, CheckoutDraft> entries = {};

  @override
  Future<CheckoutDraft?> read(int userId) async => entries[userId];

  @override
  Future<void> write(int userId, CheckoutDraft draft) async {
    entries[userId] = draft;
  }

  @override
  Future<void> clear(int userId) async {
    entries.remove(userId);
  }
}

Faktur _faktur() => Faktur.fromJson({
  'no_transaksi': 'BBM-26-000001',
  'tanggal': '2026-11-08',
  'jam': '12:00:00',
  'nama_kasir': 'Kasir',
  'nik': '1234567890123456',
  'nama_pelanggan': 'Anggota',
  'jenis_bayar': 'TUNAI',
  'baris': [],
  'jadwal': [],
});

void main() {
  test('checkout key is a UUID v4', () {
    final key = newCheckoutKey();
    expect(
      key,
      matches(
        RegExp(
          r'^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$',
        ),
      ),
    );
  });

  test('checkout snapshot round trips and keeps fractional qty numeric', () {
    const draft = CheckoutDraft(
      key: 'd9428888-122b-4f8a-8e3c-20d3c7eab5f1',
      nomor: 'BBM-26-000001',
      nik: '1234567890123456',
      tenor: 1,
      uangBayar: 50000,
      baris: [
        {'product_id': 8, 'qty': 4.1},
      ],
    );

    final copy = CheckoutDraft.fromJson(draft.toJson());
    expect(copy.key, draft.key);
    expect(copy.nomor, draft.nomor);
    expect(copy.baris.single['qty'], 4.1);
  });

  test(
    'restart recovery fetches committed invoice without resubmitting',
    () async {
      const userId = 7;
      const draft = CheckoutDraft(
        key: 'd9428888-122b-4f8a-8e3c-20d3c7eab5f1',
        nomor: 'BBM-26-000001',
        nik: '1234567890123456',
        tenor: 1,
        uangBayar: 50000,
        baris: [
          {'product_id': 8, 'qty': 4.1},
        ],
      );
      final store = _MemoryCheckoutStore()..entries[userId] = draft;
      var submitCalled = false;

      final result = await pulihkanCheckout(
        userId: userId,
        draft: (await store.read(userId))!, // loaded from storage after restart
        store: store,
        terbitkanNomor: (_) async => fail('number already persisted'),
        cariFaktur: (_) async => _faktur(),
        simpan: (_) async {
          submitCalled = true;
          return _faktur();
        },
      );

      expect(result.no, draft.nomor);
      expect(submitCalled, isFalse);
      expect(await store.read(userId), isNull);
    },
  );

  test(
    'restart recovery replays absent invoice with same key and snapshot',
    () async {
      const userId = 7;
      const draft = CheckoutDraft(
        key: 'd9428888-122b-4f8a-8e3c-20d3c7eab5f1',
        nomor: 'BBM-26-000001',
        nik: '1234567890123456',
        tenor: 3,
        uangBayar: null,
        baris: [
          {'product_id': 8, 'qty': 4.1},
        ],
      );
      final store = _MemoryCheckoutStore()..entries[userId] = draft;
      CheckoutDraft? submitted;

      await pulihkanCheckout(
        userId: userId,
        draft: (await store.read(userId))!,
        store: store,
        terbitkanNomor: (_) async => fail('number already persisted'),
        cariFaktur: (_) async => null,
        simpan: (snapshot) async {
          submitted = snapshot;
          return _faktur();
        },
      );

      expect(submitted?.key, draft.key);
      expect(submitted?.baris.single['qty'], 4.1);
      expect(await store.read(userId), isNull);
    },
  );

  test(
    'restart recovery repeats number allocation with original key',
    () async {
      const userId = 7;
      const draft = CheckoutDraft(
        key: 'd9428888-122b-4f8a-8e3c-20d3c7eab5f1',
        nik: '1234567890123456',
        tenor: 1,
        uangBayar: 50000,
        baris: [
          {'product_id': 8, 'qty': 4.1},
        ],
      );
      final store = _MemoryCheckoutStore()..entries[userId] = draft;
      String? allocationKey;
      String? submittedNumber;

      await pulihkanCheckout(
        userId: userId,
        draft: (await store.read(userId))!,
        store: store,
        terbitkanNomor: (key) async {
          allocationKey = key;
          return 'BBM-26-000001';
        },
        cariFaktur: (_) async => null,
        simpan: (snapshot) async {
          submittedNumber = snapshot.nomor;
          return _faktur();
        },
      );

      expect(allocationKey, draft.key);
      expect(submittedNumber, 'BBM-26-000001');
      expect(await store.read(userId), isNull);
    },
  );
}
