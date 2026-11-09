import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kopkar_pos/core/providers.dart';
import 'package:kopkar_pos/core/widgets.dart';
import 'package:kopkar_pos/features/kasir/kasir_page.dart';

void main() {
  Future<double> bawahTombolBayar(WidgetTester t, double inset) async {
    t.view.physicalSize = const Size(400, 800);
    t.view.devicePixelRatio = 1;
    addTearDown(t.view.reset);
    await t.pumpWidget(
      ProviderScope(
        overrides: [
          onlineProvider.overrideWith((ref) => Stream.value(true)),
          periodeProvider.overrideWith((ref) async => null),
        ],
        child: MaterialApp(
          home: MediaQuery(
            data: MediaQueryData(
              size: const Size(400, 800),
              viewPadding: EdgeInsets.only(bottom: inset),
              padding: EdgeInsets.only(bottom: inset),
            ),
            child: Scaffold(body: CartPanel(onBayar: () {})),
          ),
        ),
      ),
    );
    await t.pump();
    return t.getBottomLeft(find.byType(GradientActionButton)).dy;
  }

  testWidgets('tombol Bayar naik setinggi inset navigasi bawah HP', (t) async {
    final tanpaInset = await bawahTombolBayar(t, 0);
    final denganInset = await bawahTombolBayar(t, 48);
    expect(tanpaInset - denganInset, 48);
    expect(denganInset, lessThanOrEqualTo(800 - 48));
  });
}
