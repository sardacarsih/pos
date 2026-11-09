import 'dart:async';

import 'package:flutter/material.dart';

OverlayEntry? _entri;
Timer? _timer;

/// Toast ringan di bawah header: tidak menangkap sentuhan, tidak menutupi tombol aksi di bawah,
/// dan panggilan baru mengganti yang lama (tidak menumpuk seperti antrean SnackBar).
void tampilToast(BuildContext context, String pesan) {
  final overlay = Overlay.maybeOf(context, rootOverlay: true);
  if (overlay == null) return;
  _timer?.cancel();
  _entri?.remove();
  final theme = Theme.of(context);
  final atas = MediaQuery.paddingOf(context).top + 72;
  final entri = OverlayEntry(
    builder: (_) => Positioned(
      top: atas,
      left: 16,
      right: 16,
      child: IgnorePointer(
        child: Semantics(
          liveRegion: true,
          child: Center(
            child: Material(
              color: theme.colorScheme.inverseSurface,
              elevation: 4,
              borderRadius: BorderRadius.circular(24),
              child: Padding(
                padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
                child: Text(
                  pesan,
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                  textAlign: TextAlign.center,
                  style: theme.textTheme.bodyMedium?.copyWith(
                    color: theme.colorScheme.onInverseSurface,
                  ),
                ),
              ),
            ),
          ),
        ),
      ),
    ),
  );
  _entri = entri;
  overlay.insert(entri);
  _timer = Timer(const Duration(milliseconds: 1300), () {
    if (_entri == entri) {
      entri.remove();
      _entri = null;
    }
  });
}
