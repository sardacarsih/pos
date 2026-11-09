import 'dart:convert';
import 'dart:io';

import 'package:flutter_test/flutter_test.dart';
import 'package:kopkar_pos/core/update.dart';

void main() {
  test('changelog.json valid dan entri teratas = versi pubspec', () {
    final daftar = (jsonDecode(File('changelog.json').readAsStringSync()) as List)
        .map((e) => EntriRilis.dariJson(e as Map<String, dynamic>))
        .toList();
    final m = RegExp(
      r'^version:\s*(\S+)\+(\d+)',
      multiLine: true,
    ).firstMatch(File('pubspec.yaml').readAsStringSync())!;
    expect(daftar.first.versionName, m.group(1));
    expect(daftar.first.versionCode, int.parse(m.group(2)!));
    expect(daftar.every((e) => e.perubahan.isNotEmpty), isTrue);
  });
}
