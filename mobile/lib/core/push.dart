import 'dart:async';

import 'package:firebase_core/firebase_core.dart';
import 'package:firebase_messaging/firebase_messaging.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_local_notifications/flutter_local_notifications.dart';

import 'api.dart';
import 'models.dart';

/// Pengelolaan FCM hanya untuk Android; kegagalan konfigurasi tidak memblokir login.
class PushService {
  PushService(this.api);
  // Id baru: suara channel Android tidak bisa diganti setelah channel dibuat.
  static const _channelId = 'transaksi_suara2';
  static const _suara = RawResourceAndroidNotificationSound('notif_transaksi');
  final ApiClient api;
  void Function(String no)? onTap;
  final _local = FlutterLocalNotificationsPlugin();
  final _subscriptions = <StreamSubscription<dynamic>>[];
  Future<void> _pending = Future.value();
  bool _initialized = false;
  int? _user;

  Future<void> initialize() async {
    if (kIsWeb || defaultTargetPlatform != TargetPlatform.android) return;
    try {
      await Firebase.initializeApp();
      await _local.initialize(
        settings: const InitializationSettings(
          android: AndroidInitializationSettings('@mipmap/ic_launcher'),
        ),
        onDidReceiveNotificationResponse: (response) => _tap(response.payload),
      );
      // Channel dibuat di sini agar notif FCM saat app tertutup (channel_id dari server) memakai suaranya.
      await _local
          .resolvePlatformSpecificImplementation<
            AndroidFlutterLocalNotificationsPlugin
          >()
          ?.createNotificationChannel(
            const AndroidNotificationChannel(
              _channelId,
              'Transaksi anggota',
              importance: Importance.high,
              sound: _suara,
            ),
          );
      _subscriptions.add(
        FirebaseMessaging.onMessage.listen((message) async {
          if (_user == null || message.data['type'] != 'transaction') return;
          final no = message.data['no'];
          if (no is! String || no.isEmpty) return;
          await _local.show(
            id: no.hashCode & 0x7fffffff,
            title: 'Kopkar Kusuma Lestari',
            body: 'Ada transaksi baru pada akun Anda.',
            notificationDetails: const NotificationDetails(
              android: AndroidNotificationDetails(
                _channelId,
                'Transaksi anggota',
                importance: Importance.high,
                priority: Priority.high,
                sound: _suara,
              ),
            ),
            payload: no,
          );
        }),
      );
      _subscriptions.add(
        FirebaseMessaging.onMessageOpenedApp.listen(_messageTap),
      );
      _subscriptions.add(
        FirebaseMessaging.instance.onTokenRefresh.listen((token) {
          if (_user != null) _queueToken(token, _user!);
        }),
      );
      _initialized = true;
      final initial = await FirebaseMessaging.instance.getInitialMessage();
      if (initial != null) _messageTap(initial);
      final launch = await _local.getNotificationAppLaunchDetails();
      if (launch?.didNotificationLaunchApp == true)
        _tap(launch?.notificationResponse?.payload);
    } catch (e) {
      debugPrint('Notifikasi belum tersedia: $e');
    }
  }

  void _messageTap(RemoteMessage message) {
    if (message.data['type'] == 'transaction')
      _tap(message.data['no'] as String?);
  }

  void _tap(String? no) {
    if (no != null && no.isNotEmpty && no.length <= 100) onTap?.call(no);
  }

  Future<void> sync(Profil? profil) async {
    if (!_initialized) return;
    if (profil?.role != 'ANGGOTA' || profil!.gantiPassword) {
      _user = null;
      return;
    }
    final user = profil.id;
    _user = user;
    try {
      final permission = await FirebaseMessaging.instance.requestPermission();
      if (_user != user) return;
      if (permission.authorizationStatus != AuthorizationStatus.authorized) {
        await _pending;
        if (_user == user) {
          try {
            await api.hapusPush();
          } catch (_) {}
        }
        return;
      }
      final token = await FirebaseMessaging.instance.getToken();
      if (token != null && _user == user) await _queueToken(token, user);
    } catch (_) {
      // Sinkronisasi akan dicoba lagi saat aplikasi aktif kembali.
    }
  }

  Future<void> _queueToken(String token, int user) {
    _pending = _pending.then((_) async {
      if (_user != user) return;
      try {
        await api.daftarPush(token);
      } catch (_) {}
    });
    return _pending;
  }

  Future<void> logout() async {
    final hadUser = _user != null;
    _user = null;
    await _pending;
    if (hadUser) {
      try {
        await api.hapusPush();
      } catch (_) {}
    }
  }

  void dispose() {
    for (final s in _subscriptions) {
      s.cancel();
    }
  }
}
