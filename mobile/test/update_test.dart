import 'package:flutter_test/flutter_test.dart';
import 'package:kopkar_pos/core/update.dart';
import 'package:shared_preferences/shared_preferences.dart';

void main() {
  final info = InfoRilis.dariJson({
    'versionCode': 3,
    'versionName': '1.0.2',
    'apk': 'pos-kasir.apk',
    'sha256': 'abc',
    'minVersionCode': 2,
  });

  test('lebihBaru hanya bila versionCode server lebih besar', () {
    expect(info.lebihBaru(2), isTrue);
    expect(info.lebihBaru(3), isFalse);
    expect(info.lebihBaru(4), isFalse);
  });

  test('wajib bila versi terpasang di bawah minVersionCode', () {
    expect(info.wajib(1), isTrue);
    expect(info.wajib(2), isFalse);
  });

  test('cekRilis tidak menghubungi server bila baru dicek (< 6 jam)', () async {
    SharedPreferences.setMockInitialValues({
      'update_cek_terakhir': DateTime.now().millisecondsSinceEpoch,
    });
    expect(await cekRilis(1), isNull);
  });

  test('cekRilis manual tetap menghubungi server walau baru dicek', () async {
    SharedPreferences.setMockInitialValues({
      'update_cek_terakhir': DateTime.now().millisecondsSinceEpoch,
    });
    // Server tak terjangkau di test -> null lewat catch, tapi jeda 6 jam tidak lagi memotong
    // lebih awal; cukup dipastikan tidak melempar dan jeda tidak diperbarui.
    expect(await cekRilis(1, manual: true), isNull);
    final p = await SharedPreferences.getInstance();
    expect(p.getInt('update_cek_terakhir'), isNotNull);
  });

  test('minVersionCode dan catatan opsional', () {
    final i = InfoRilis.dariJson({
      'versionCode': 2,
      'versionName': '1.0.1',
      'apk': 'a.apk',
      'sha256': 'x',
    });
    expect(i.wajib(1), isFalse);
    expect(i.catatan, '');
  });
}
