import 'dart:typed_data';

import 'package:crop_your_image/crop_your_image.dart';
import 'package:flutter/material.dart';

/// Atur foto profil: geser dan cubit/zoom foto di dalam lingkaran, lalu "Pakai". Mengembalikan bytes hasil potongan.
class FotoCropPage extends StatefulWidget {
  const FotoCropPage({super.key, required this.image});
  final Uint8List image;

  @override
  State<FotoCropPage> createState() => _FotoCropPageState();
}

class _FotoCropPageState extends State<FotoCropPage> {
  final _controller = CropController();
  bool _memotong = false;

  void _pakai() {
    setState(() => _memotong = true);
    _controller.crop();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Colors.black,
      appBar: AppBar(
        backgroundColor: Colors.black,
        foregroundColor: Colors.white,
        // Tema aplikasi memberi judul AppBar warna gelap; di latar hitam harus putih.
        title: const Text('Atur foto', style: TextStyle(color: Colors.white)),
        leading: IconButton(
          tooltip: 'Batal',
          icon: const Icon(Icons.close),
          onPressed: _memotong ? null : () => Navigator.of(context).pop(),
        ),
      ),
      body: Column(
        children: [
          Expanded(
            child: Crop(
              image: widget.image,
              controller: _controller,
              aspectRatio: 1,
              withCircleUi: true,
              interactive: true,
              fixCropRect: true,
              baseColor: Colors.black,
              maskColor: Colors.black.withValues(alpha: .6),
              progressIndicator: const CircularProgressIndicator(),
              onCropped: (result) {
                switch (result) {
                  case CropSuccess(:final croppedImage):
                    Navigator.of(context).pop(croppedImage);
                  case CropFailure():
                    if (!mounted) return;
                    setState(() => _memotong = false);
                    ScaffoldMessenger.of(context).showSnackBar(
                      const SnackBar(content: Text('Foto gagal dipotong, coba lagi')),
                    );
                }
              },
            ),
          ),
          const Padding(
            padding: EdgeInsets.fromLTRB(16, 8, 16, 0),
            child: Text(
              'Geser dan cubit untuk mengatur wajah di dalam lingkaran',
              textAlign: TextAlign.center,
              style: TextStyle(color: Colors.white70),
            ),
          ),
          SafeArea(
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: SizedBox(
                width: double.infinity,
                child: FilledButton(
                  onPressed: _memotong ? null : _pakai,
                  child: _memotong
                      ? const SizedBox(
                          width: 20,
                          height: 20,
                          child: CircularProgressIndicator(strokeWidth: 2),
                        )
                      : const Text('Pakai foto ini'),
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }
}
