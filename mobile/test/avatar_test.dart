import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kopkar_pos/core/widgets.dart';

// PNG 1x1 valid.
final _png = base64Decode(
  'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==',
);

Widget _host(Widget w) => MaterialApp(home: Scaffold(body: Center(child: w)));

void main() {
  userAvatarTests();
  testWidgets('AvatarFoto tanpa foto menampilkan inisial dua kata', (t) async {
    await t.pumpWidget(_host(const AvatarFoto(nama: 'aan sutisna')));
    expect(find.text('AS'), findsOneWidget);
  });

  testWidgets('AvatarFoto satu kata memakai satu huruf', (t) async {
    await t.pumpWidget(_host(const AvatarFoto(nama: 'Aan')));
    expect(find.text('A'), findsOneWidget);
  });

  testWidgets('AvatarFoto nama kosong memakai ikon', (t) async {
    await t.pumpWidget(_host(const AvatarFoto(nama: '  ')));
    expect(find.byIcon(Icons.person), findsOneWidget);
  });

  testWidgets('AvatarFoto dengan foto memasang gambar', (t) async {
    await t.pumpWidget(_host(AvatarFoto(nama: 'Aan', bytes: _png)));
    final a = t.widget<CircleAvatar>(find.byType(CircleAvatar));
    expect(a.foregroundImage, isA<MemoryImage>());
  });
}

void userAvatarTests() {
  testWidgets('UserAvatar tanpa versi foto menampilkan inisial', (t) async {
    await t.pumpWidget(
      ProviderScope(
        child: _host(const UserAvatar(id: 7, nama: 'budi santoso')),
      ),
    );
    expect(find.text('BS'), findsOneWidget);
  });
}
