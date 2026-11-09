package id.kopkar.kopkar_pos

import android.Manifest
import android.bluetooth.BluetoothAdapter
import android.bluetooth.BluetoothManager
import android.content.pm.PackageManager
import android.os.Build
import io.flutter.embedding.android.FlutterActivity
import io.flutter.embedding.engine.FlutterEngine
import io.flutter.plugin.common.MethodChannel
import java.util.UUID
import kotlin.concurrent.thread

class MainActivity : FlutterActivity() {
    private var pendingList: MethodChannel.Result? = null
    private val permissionCode = 7601
    private val adapter: BluetoothAdapter?
        get() = (getSystemService(BLUETOOTH_SERVICE) as? BluetoothManager)?.adapter

    override fun configureFlutterEngine(flutterEngine: FlutterEngine) {
        super.configureFlutterEngine(flutterEngine)
        MethodChannel(flutterEngine.dartExecutor.binaryMessenger, "id.kopkar.pos/bluetooth_printer")
            .setMethodCallHandler { call, result ->
                when (call.method) {
                    "list" -> {
                        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S &&
                            checkSelfPermission(Manifest.permission.BLUETOOTH_CONNECT) != PackageManager.PERMISSION_GRANTED
                        ) {
                            if (pendingList != null) {
                                result.error("busy", "Permintaan izin sedang berlangsung.", null)
                            } else {
                                pendingList = result
                                requestPermissions(arrayOf(Manifest.permission.BLUETOOTH_CONNECT), permissionCode)
                            }
                        } else listDevices(result)
                    }
                    "print" -> {
                        val address = call.argument<String>("address")
                        val data = call.argument<ByteArray>("data")
                        if (address == null || data == null) {
                            result.error("invalid", "Data printer tidak valid.", null)
                        } else printData(address, data, result)
                    }
                    else -> result.notImplemented()
                }
            }
    }

    override fun onRequestPermissionsResult(requestCode: Int, permissions: Array<out String>, grantResults: IntArray) {
        super.onRequestPermissionsResult(requestCode, permissions, grantResults)
        if (requestCode != permissionCode) return
        val result = pendingList ?: return
        pendingList = null
        if (grantResults.firstOrNull() == PackageManager.PERMISSION_GRANTED) listDevices(result)
        else result.error("permission", "Izin perangkat sekitar diperlukan untuk mengakses printer Bluetooth.", null)
    }

    private fun listDevices(result: MethodChannel.Result) {
        try {
            val bluetooth = adapter
            if (bluetooth == null) {
                result.error("unavailable", "Perangkat ini tidak mendukung Bluetooth.", null)
            } else if (!bluetooth.isEnabled) {
                result.error("disabled", "Aktifkan Bluetooth terlebih dahulu.", null)
            } else {
                result.success(bluetooth.bondedDevices.map {
                    mapOf("name" to (it.name ?: "Perangkat Bluetooth"), "address" to it.address)
                }.sortedBy { it["name"] })
            }
        } catch (e: SecurityException) {
            result.error("permission", "Izin Bluetooth belum diberikan.", null)
        }
    }

    private fun printData(address: String, data: ByteArray, result: MethodChannel.Result) {
        thread {
            try {
                val bluetooth = adapter ?: throw IllegalStateException("Bluetooth tidak tersedia.")
                if (!bluetooth.isEnabled) throw IllegalStateException("Aktifkan Bluetooth terlebih dahulu.")
                val device = bluetooth.bondedDevices.firstOrNull { it.address == address }
                    ?: throw IllegalStateException("Printer tidak lagi dipasangkan. Pasangkan kembali di pengaturan Bluetooth.")
                device.createRfcommSocketToServiceRecord(UUID.fromString("00001101-0000-1000-8000-00805f9b34fb")).use { socket ->
                    socket.connect()
                    socket.outputStream.write(data)
                    socket.outputStream.flush()
                }
                runOnUiThread { result.success(null) }
            } catch (e: Exception) {
                runOnUiThread { result.error("print_failed", "Gagal mengirim nota. Pastikan printer menyala, terpasang dan mendukung ESC/POS Bluetooth Classic.", null) }
            }
        }
    }
}
