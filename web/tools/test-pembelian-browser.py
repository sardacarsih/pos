"""Smoke test build produksi dengan API tiruan. Jalankan bun run build dan bun run preview dahulu."""

import base64
import json
import re
import sys
from pathlib import Path
from tempfile import TemporaryDirectory
from playwright.sync_api import expect, sync_playwright


def video_barcode(path):
    """An EAN-13 test card with a leading zero, repeated as a fake webcam feed."""
    digits = "0400638133390"
    patterns = ["0001101", "0011001", "0010011", "0111101", "0100011",
                "0110001", "0101111", "0111011", "0110111", "0001011"]
    left = "".join(patterns[int(d)] for d in digits[1:7])
    right = "".join(patterns[int(d)].translate(str.maketrans("01", "10")) for d in digits[7:])
    modules = "101" + left + "01010" + right + "101"
    width, height = 640, 240
    white = bytes([235]) * width
    bars = bytes([235]) * 80 + b"".join(bytes([16 if bit == "1" else 235]) * 5 for bit in modules)
    bars += bytes([235]) * (width - len(bars))
    luminance = white * 40 + bars * 160 + white * 40
    frame = b"FRAME\n" + luminance + bytes([128]) * (width * height // 2)
    path.write_bytes(b"YUV4MPEG2 W640 H240 F10:1 Ip A1:1 C420jpeg\n" + frame * 12)


def test_pembelian(url):
    akun = dict(id=17, role="ADMIN", nama="Admin Uji", nik=None, default_nik="-", ganti_password=False)
    ref = dict(tanggal_server="2026-11-18", supplier=[dict(kode="S1", nama="Supplier Uji")], barang=[
        dict(id=42, kode="T1", nama="Teh Uji", beli="3000", satuan=[
            dict(satuan="PCS", isi="1", barcode="00123"), dict(satuan="BOX", isi="12", barcode="0400638133390")
        ])
    ])
    faktur = dict(
        no_transaksi="BW-26-000001", supplier_id="S1", supplier_nama="Supplier Uji", no_faktur="INV-UJI",
        tanggal_faktur="2026-11-18", tanggal_terima="2026-11-18", jatuh_tempo="2026-12-01", jenis="TEMPO",
        bruto="72000", potongan="5000", total="67000", operator="Admin Uji", batal_at=None,
        sudah_bayar=0, sisa=67000, status="TERBUKA", asal="WEB",
    )
    percobaan = []
    resmi = {}
    pembayaran = []
    jaringan_putus = False
    petugas = dict(akun, id=18, role="PEMBELIAN", nama="Petugas Uji")

    def mock(route):
        request = route.request
        path = request.url.split("/api/v1")[1].split("?")[0]
        if jaringan_putus:
            route.abort("internetdisconnected")
            return
        if path == "/web/auth/login":
            pemilik = petugas if request.post_data_json["username"] == "petugas" else akun
            payload = base64.urlsafe_b64encode(json.dumps({"sub": pemilik["id"]}).encode()).decode().rstrip("=")
            route.fulfill(json=dict(access_token=f"header.{payload}.signature", refresh_token="refresh"))
        elif path == "/web/me":
            segment = request.headers["authorization"].split(".")[1]
            uid = json.loads(base64.urlsafe_b64decode(segment + "=" * (-len(segment) % 4)))["sub"]
            route.fulfill(json=petugas if uid == petugas["id"] else akun)
        elif path == "/pembelian/referensi":
            route.fulfill(json=ref)
        elif path == "/pembelian" and request.method == "POST":
            key = request.headers["idempotency-key"]
            data = request.post_data_json
            percobaan.append((key, data))
            if key not in resmi:
                resmi[key] = faktur["no_transaksi"]
                faktur.update(sudah_bayar=data["uang_muka"], sisa=67000 - data["uang_muka"])
            if len(percobaan) == 1:
                route.abort("failed")
            else:
                route.fulfill(json=dict(no_transaksi=resmi[key]))
        elif path == "/pembelian":
            route.fulfill(json=dict(items=[faktur] if resmi else [], page=1, ada_lagi=False))
        elif path.endswith("/pembayaran"):
            data = request.post_data_json
            pembayaran.append(dict(id=3, **data, balik_id=None, alasan=None, operator="Admin Uji",
                                   created_at="2026-11-18 13:00:00", dibalik=False))
            faktur.update(sudah_bayar=faktur["sudah_bayar"] + data["nominal"], sisa=faktur["sisa"] - data["nominal"])
            route.fulfill(json=dict(id="3"))
        elif path == "/pembelian/BW-26-000001":
            route.fulfill(json=dict(**faktur, tanggal_server=ref["tanggal_server"], pembayaran=pembayaran, baris=[
                dict(kode="T1", nama="Teh Uji", satuan="BOX", satuan_dasar="PCS", qty="2", isi="12",
                     harga="36000", potongan="5000", total="67000")
            ]))
        elif path == "/hutang-supplier":
            route.fulfill(json=dict(items=[faktur], page=1, ada_lagi=False, supplier=[
                dict(supplier_id="S1", supplier_nama="Supplier Uji", saldo=str(faktur["sisa"]), terlambat="0", faktur=1)
            ]))
        elif path == "/auth/logout":
            route.fulfill(status=204)
        else:
            route.fulfill(json={})

    with TemporaryDirectory() as tmp, sync_playwright() as playwright:
        feed = Path(tmp) / "ean.y4m"
        video_barcode(feed)
        browser = playwright.chromium.launch(headless=True, args=[
            "--use-fake-device-for-media-stream", "--use-fake-ui-for-media-stream",
            f"--use-file-for-fake-video-capture={feed}"
        ])
        context = browser.new_context(viewport={"width": 1440, "height": 900})
        context.route("**/api/v1/**", mock)
        context.route("https://fonts.googleapis.com/**", lambda route: route.abort())
        context.add_init_script("""(() => {
          const getUserMedia = navigator.mediaDevices.getUserMedia.bind(navigator.mediaDevices);
          navigator.mediaDevices.getUserMedia = async constraints => {
            if (window.testDenyCamera) throw new DOMException('Test permission', 'NotAllowedError');
            const stream = await getUserMedia(constraints);
            window.testCameraTrack = stream.getVideoTracks()[0];
            return stream;
          };
        })()""")
        page = context.new_page()
        errors = []
        page.on("pageerror", lambda error: errors.append(str(error)))
        page.goto(url)
        page.wait_for_load_state("networkidle")
        page.get_by_label("Username").fill("admin")
        sandi = page.get_by_label("Password", exact=True)
        sandi.fill("test12345")
        expect(sandi).to_have_attribute("type", "password")
        page.get_by_role("button", name="Tampilkan kata sandi").press("Space")
        expect(sandi).to_have_attribute("type", "text")
        expect(sandi).to_have_value("test12345")
        page.get_by_role("button", name="Sembunyikan kata sandi").click()
        expect(sandi).to_have_attribute("type", "password")
        expect(sandi).to_have_value("test12345")
        page.get_by_label("Ingat saya di perangkat ini").check()
        page.get_by_role("button", name="Masuk", exact=True).press("Enter")
        page.get_by_role("button", name="Pembelian", exact=True).press("Enter")
        scan = page.get_by_role("combobox", name="Barcode atau nama barang")
        expect(scan).to_be_visible()
        page.wait_for_function("""navigator.serviceWorker.controller &&
            new URL(navigator.serviceWorker.controller.scriptURL).searchParams.has('v')""")
        page.get_by_label("Supplier", exact=True).select_option("S1")
        page.get_by_label("No. faktur supplier").fill("INV-UJI")
        metode = page.get_by_role("combobox", name="Metode pembayaran", exact=True)
        expect(metode).to_have_value("TUNAI")
        metode.press("ArrowDown")
        expect(metode).to_have_value("TRANSFER")
        metode.press("ArrowUp")
        expect(metode).to_have_value("TUNAI")
        metode.select_option(label="Transfer")
        expect(metode).to_have_value("TRANSFER")
        metode.select_option(label="Tunai")
        expect(metode).to_have_value("TUNAI")
        page.get_by_label("Jenis pembayaran").select_option("TEMPO")
        page.get_by_label("Jatuh tempo").fill("2026-12-01")
        page.get_by_label("Uang muka").fill("2000")
        expect(page.get_by_label("Uang muka")).to_have_value("2.000")
        metode.press("ArrowDown")
        expect(metode).to_have_value("TRANSFER")

        jaringan_putus = True
        context.set_offline(True)
        expect(page.get_by_text("Mode offline", exact=True)).to_be_visible()
        page.reload()
        expect(metode).to_have_value("TRANSFER")
        expect(page.get_by_label("Uang muka")).to_have_value("2.000")
        page.get_by_role("button", name="Scan dengan kamera").press("Enter")
        expect(page.get_by_label("Jumlah Teh Uji")).to_be_focused()
        assert page.evaluate("window.testCameraTrack.readyState") == "ended"
        expect(page.get_by_role("dialog", name="Scan barcode barang")).to_have_count(0)
        page.get_by_role("button", name="Hapus Teh Uji").press("Enter")
        page.evaluate("window.testDenyCamera = true")
        page.get_by_role("button", name="Scan dengan kamera").press("Enter")
        expect(page.get_by_role("alert")).to_contain_text("Izin kamera ditolak")
        page.keyboard.press("Escape")
        expect(page.get_by_role("dialog", name="Scan barcode barang")).to_have_count(0)
        scan.focus()
        page.keyboard.type("0400638133390")
        page.keyboard.press("Enter")
        jumlah = page.get_by_label("Jumlah Teh Uji")
        harga = page.get_by_label("Harga beli Teh Uji")
        potongan = page.get_by_label("Potongan Teh Uji")
        expect(jumlah).to_be_focused()
        for kolom in (jumlah, harga, potongan, page.get_by_label("Uang muka")):
            expect(kolom).to_have_css("text-align", "right")
        expect(scan).to_have_css("text-align", "start")
        jumlah.fill("1,2345")
        page.keyboard.press("Control+Enter")
        expect(jumlah).to_have_attribute("aria-invalid", "true")
        expect(page.get_by_role("button", name="Masukkan antrean")).to_have_count(0)
        page.get_by_role("button", name="Tinjau faktur", exact=True).click()
        expect(page.get_by_role("button", name="Masukkan antrean")).to_have_count(0)
        jumlah.fill("1.234,5")
        page.keyboard.press("Enter")
        expect(harga).to_be_focused()
        expect(jumlah).to_have_value("1.234,5")
        jumlah.fill("2")
        page.keyboard.press("Enter")
        expect(harga).to_be_focused()
        expect(harga).to_have_value("36.000")
        harga.evaluate("input => input.setSelectionRange(3, 3)")
        page.keyboard.press("Backspace")
        expect(harga).to_have_value("3.000")
        harga.fill("36000")
        expect(harga).to_have_value("36.000")
        page.keyboard.press("Enter")
        expect(potongan).to_be_focused()
        potongan.evaluate("""input => {
            const clipboardData = new DataTransfer();
            clipboardData.setData('text/plain', 'Rp 5.000');
            input.select();
            input.dispatchEvent(new ClipboardEvent('paste', { clipboardData, bubbles: true, cancelable: true }));
        }""")
        expect(potongan).to_have_value("5.000")
        page.keyboard.press("Enter")
        expect(scan).to_be_focused()
        expect(page.get_by_role("cell", name="Rp 67.000", exact=True)).to_have_css("text-align", "right")
        page.keyboard.press("Control+Enter")
        page.get_by_role("button", name="Masukkan antrean").press("Enter")
        expect(page.get_by_text("Menunggu", exact=True)).to_be_visible()
        page.reload()
        expect(page.get_by_text("Mode offline", exact=True)).to_be_visible()
        page.get_by_role("tab", name="Antrean").press("Enter")
        expect(page.get_by_role("rowheader", name=re.compile("^INV-UJI"))).to_be_visible()
        expect(page.get_by_text("Menunggu", exact=True)).to_be_visible()
        assert not percobaan

        tab_lain = context.new_page()
        tab_lain.on("pageerror", lambda error: errors.append(str(error)))
        tab_lain.goto(url)
        expect(tab_lain.get_by_text("Mode offline", exact=True)).to_be_visible()
        jaringan_putus = False
        context.set_offline(False)
        page.evaluate("window.dispatchEvent(new Event('online'))")
        tab_lain.evaluate("window.dispatchEvent(new Event('online'))")
        expect(page.get_by_text("Tersinkron", exact=True)).to_be_visible(timeout=35_000)
        assert len(percobaan) >= 2 and len(resmi) == 1 and percobaan[0] == percobaan[1], percobaan
        assert percobaan[0][1]["metode"] == "TRANSFER"
        assert percobaan[0][1]["uang_muka"] == 2000
        assert percobaan[0][1]["baris"][0] == {
            "product_id": 42, "satuan": "BOX", "isi": "12", "qty": "2", "harga": 36000, "potongan": 5000
        }, percobaan
        tab_lain.close()

        page.get_by_role("button", name="Hutang Supplier", exact=True).press("Enter")
        expect(page.get_by_role("heading", name="Saldo per supplier")).to_be_visible()
        expect(page.get_by_role("cell", name="Rp 65.000", exact=True).first).to_have_css("text-align", "right")
        page.get_by_role("button", name="INV-UJI", exact=True).press("Enter")
        expect(page.get_by_role("cell", name="Rp 36.000", exact=True)).to_have_css("text-align", "right")
        page.get_by_role("button", name="Catat pembayaran").press("Enter")
        nominal = page.get_by_label("Nominal", exact=True)
        expect(nominal).to_have_css("text-align", "right")
        expect(nominal).to_have_value("65.000")
        nominal.fill("65001")
        expect(nominal).to_have_attribute("aria-invalid", "true")
        page.get_by_role("button", name="Konfirmasi", exact=True).press("Enter")
        assert not pembayaran
        nominal.fill("17000")
        expect(nominal).to_have_value("17.000")
        metode.press("ArrowDown")
        expect(metode).to_have_value("TRANSFER")
        page.get_by_role("button", name="Konfirmasi", exact=True).press("Enter")
        expect(page.get_by_text("Pembayaran dicatat.", exact=True)).to_be_visible()
        assert pembayaran[0]["nominal"] == 17000
        assert pembayaran[0]["metode"] == "TRANSFER"
        page.get_by_role("button", name="Tutup", exact=True).press("Enter")
        page.get_by_role("button", name="Keluar", exact=True).press("Enter")
        page.get_by_label("Username").fill("petugas")
        page.get_by_label("Password", exact=True).fill("test12345")
        page.get_by_role("button", name="Masuk", exact=True).press("Enter")
        expect(page.get_by_role("heading", name="Pembelian", exact=True)).to_be_visible()
        expect(page.get_by_role("button", name="Hutang Supplier", exact=True)).to_have_count(0)
        page.get_by_role("tab", name="Antrean").press("Enter")
        expect(page.get_by_text("Belum ada pembelian di antrean ini.", exact=True)).to_be_visible()
        assert not errors, errors
        browser.close()


if __name__ == "__main__":
    test_pembelian(sys.argv[1] if len(sys.argv) > 1 else "http://127.0.0.1:4187")
    print("Chromium: scan kamera offline, input keyboard, reload, retry, pembayaran dan isolasi akun lulus.")
