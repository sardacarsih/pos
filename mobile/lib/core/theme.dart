import 'package:flutter/material.dart';

abstract final class KopkarColors {
  static const green = Color(0xFF0E7A2E);
  static const deepGreen = Color(0xFF0D6A2A);
  static const lightBackground = Color(0xFFEEF7EA);
  static const lightSurface = Color(0xFFFFFFFF);
  static const lightInput = Color(0xFFF4F6F5);
  static const lightText = Color(0xFF1E2B22);
  static const darkBackground = Color(0xFF10241D);
  static const darkSurface = Color(0xFF17211B);
  static const darkInput = Color(0xFF20342A);
  static const darkText = Color(0xFFE6F5EA);
  static const mint = Color(0xFF8BE0A6);
  static const amber = Color(0xFFFFE9BD);
  static const darkAmber = Color(0xFF523800);
  static const outline = Color(0xFFDCE5DD);
}

ThemeData kopkarTheme(Brightness brightness) {
  final dark = brightness == Brightness.dark;
  final background = dark
      ? KopkarColors.darkBackground
      : KopkarColors.lightBackground;
  final surface = dark ? KopkarColors.darkSurface : KopkarColors.lightSurface;
  final input = dark ? KopkarColors.darkInput : KopkarColors.lightInput;
  final foreground = dark ? KopkarColors.darkText : KopkarColors.lightText;
  final accent = dark ? KopkarColors.mint : KopkarColors.green;
  final scheme =
      ColorScheme.fromSeed(
        seedColor: KopkarColors.green,
        brightness: brightness,
      ).copyWith(
        primary: accent,
        onPrimary: dark ? KopkarColors.darkBackground : Colors.white,
        primaryContainer: dark
            ? const Color(0xFF203C2B)
            : const Color(0xFFE2F1DF),
        onPrimaryContainer: dark ? KopkarColors.mint : const Color(0xFF0A6330),
        secondary: dark ? const Color(0xFFB1D6BC) : const Color(0xFF487456),
        surface: surface,
        surfaceContainerLowest: dark ? const Color(0xFF0C1B15) : Colors.white,
        surfaceContainerLow: input,
        surfaceContainer: dark
            ? const Color(0xFF1C2C23)
            : KopkarColors.lightBackground,
        surfaceContainerHigh: dark
            ? const Color(0xFF26392E)
            : const Color(0xFFE8F1E5),
        onSurface: foreground,
        outline: dark ? const Color(0xFF3A5144) : KopkarColors.outline,
        error: dark ? const Color(0xFFFFB4A9) : const Color(0xFFB3261E),
      );
  final textTheme = Typography.material2021().black.apply(
    fontFamily: 'Poppins',
    bodyColor: foreground,
    displayColor: foreground,
  );

  return ThemeData(
    useMaterial3: true,
    brightness: brightness,
    colorScheme: scheme,
    scaffoldBackgroundColor: background,
    fontFamily: 'Poppins',
    textTheme: textTheme.copyWith(
      headlineSmall: textTheme.headlineSmall?.copyWith(
        fontWeight: FontWeight.w700,
      ),
      titleLarge: textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w700),
      titleMedium: textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w600),
      labelLarge: textTheme.labelLarge?.copyWith(fontWeight: FontWeight.w600),
    ),
    appBarTheme: AppBarTheme(
      backgroundColor: Colors.transparent,
      foregroundColor: foreground,
      elevation: 0,
      centerTitle: false,
      titleTextStyle: textTheme.titleLarge?.copyWith(
        color: foreground,
        fontWeight: FontWeight.w700,
      ),
    ),
    cardTheme: CardThemeData(
      color: surface,
      surfaceTintColor: Colors.transparent,
      elevation: 0,
      margin: EdgeInsets.zero,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(20)),
    ),
    inputDecorationTheme: InputDecorationTheme(
      filled: true,
      fillColor: input,
      contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 16),
      border: OutlineInputBorder(
        borderRadius: BorderRadius.circular(16),
        borderSide: BorderSide.none,
      ),
      enabledBorder: OutlineInputBorder(
        borderRadius: BorderRadius.circular(16),
        borderSide: BorderSide(color: scheme.outlineVariant),
      ),
      focusedBorder: OutlineInputBorder(
        borderRadius: BorderRadius.circular(16),
        borderSide: BorderSide(color: accent, width: 1.6),
      ),
      errorBorder: OutlineInputBorder(
        borderRadius: BorderRadius.circular(16),
        borderSide: BorderSide(color: scheme.error),
      ),
      focusedErrorBorder: OutlineInputBorder(
        borderRadius: BorderRadius.circular(16),
        borderSide: BorderSide(color: scheme.error, width: 1.6),
      ),
    ),
    filledButtonTheme: FilledButtonThemeData(
      style: FilledButton.styleFrom(
        backgroundColor: dark ? const Color(0xFF267D32) : KopkarColors.green,
        foregroundColor: Colors.white,
        minimumSize: const Size(48, 52),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(28)),
        textStyle: const TextStyle(
          fontFamily: 'Poppins',
          fontWeight: FontWeight.w600,
        ),
      ),
    ),
    outlinedButtonTheme: OutlinedButtonThemeData(
      style: OutlinedButton.styleFrom(
        minimumSize: const Size(48, 48),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(28)),
      ),
    ),
    chipTheme: ChipThemeData(
      shape: const StadiumBorder(),
      side: BorderSide(color: scheme.outlineVariant),
      selectedColor: scheme.primaryContainer,
      labelStyle: TextStyle(
        fontFamily: 'Poppins',
        color: foreground,
        fontSize: 12,
      ),
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 6),
    ),
    dividerTheme: DividerThemeData(
      color: scheme.outlineVariant,
      thickness: 1,
      space: 1,
    ),
  );
}
