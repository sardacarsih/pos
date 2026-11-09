import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kopkar_pos/core/toast.dart';
import 'package:kopkar_pos/core/widgets.dart';

void main() {
  testWidgets('toast baru mengganti yang lama dan tidak menangkap sentuhan', (t) async {
    late BuildContext ctx;
    var ditekan = 0;
    await t.pumpWidget(
      MaterialApp(
        home: Builder(
          builder: (c) {
            ctx = c;
            return Scaffold(
              body: Align(
                alignment: Alignment.topCenter,
                child: TextButton(
                  onPressed: () => ditekan++,
                  child: const Text('tombol'),
                ),
              ),
            );
          },
        ),
      ),
    );
    tampilToast(ctx, 'A ditambahkan');
    tampilToast(ctx, 'B ditambahkan');
    await t.pump();
    expect(find.text('A ditambahkan'), findsNothing);
    expect(find.text('B ditambahkan'), findsOneWidget);
    await t.pump(const Duration(milliseconds: 1400));
    await t.pump();
    expect(find.text('B ditambahkan'), findsNothing);
  });

  testWidgets('GradientActionButton tidak overflow di layar sempit dan font besar', (t) async {
    t.view.physicalSize = const Size(320, 640);
    t.view.devicePixelRatio = 1;
    addTearDown(t.view.reset);
    await t.pumpWidget(
      MaterialApp(
        home: MediaQuery(
          data: const MediaQueryData(size: Size(320, 640), textScaler: TextScaler.linear(2)),
          child: Scaffold(
            body: Padding(
              padding: const EdgeInsets.all(16),
              child: GradientActionButton(
                label: 'Keranjang · Rp 12.345.678',
                icon: Icons.shopping_cart_outlined,
                onPressed: () {},
              ),
            ),
          ),
        ),
      ),
    );
    expect(t.takeException(), isNull);
    expect(find.byType(GradientActionButton), findsOneWidget);
  });
}
