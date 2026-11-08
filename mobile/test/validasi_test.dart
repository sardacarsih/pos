import 'package:flutter_test/flutter_test.dart';
import 'package:kopkar_pos/core/validasi.dart';

void main() {
  test('galatUsername: valid, huruf besar dinormalkan, dan kasus salah', () {
    expect(galatUsername('budi.santoso'), isNull);
    expect(galatUsername('  Budi_01 '), isNull); // dinormalkan kecil + trim
    expect(galatUsername('01.00062'), isNull);
    expect(galatUsername(''), contains('wajib'));
    expect(galatUsername('ab'), contains('minimal 3'));
    expect(galatUsername('a' * 51), contains('maksimal 50'));
    expect(galatUsername('budi santoso'), contains('Tanpa spasi'));
    expect(galatUsername('budi/santoso'), isNotNull);
  });

  test('galatNama: wajib dan maks 100 karakter', () {
    expect(galatNama('Aan'), isNull);
    expect(galatNama('   '), contains('wajib'));
    expect(galatNama('x' * 101), contains('maksimal 100'));
    expect(galatNama('é' * 100), isNull); // karakter, bukan byte
  });

  test('usernameDariNik: huruf kecil dan karakter terlarang jadi titik', () {
    expect(usernameDariNik('01.00062'), '01.00062');
    expect(usernameDariNik('A-24-061'), 'a-24-061');
    expect(usernameDariNik('KB 23/001'), 'kb.23.001');
    expect(galatUsername(usernameDariNik('KB 23/001')), isNull);
    expect(usernameDariNik('A' * 60).length, 50);
  });
}
