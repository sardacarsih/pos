"""Uji tab Kas & Bank terhadap build web lokal dengan API tiruan."""

import base64
import json
import sys
from datetime import date
from urllib.parse import parse_qs, urlparse

from playwright.sync_api import expect, sync_playwright


def test_kas(url):
    hari = date.today().isoformat()
    awal = hari[:8] + "01"
    akun = [
        dict(id=1, kode="KAS", nama="Kas utama", jenis="KAS", no_rekening=None, atas_nama=None,
             saldo_awal=10000, tgl_saldo_awal=awal, saldo=10000, default_untuk="KAS", aktif=True),
        dict(id=2, kode="BANK", nama="Bank utama", jenis="BANK", no_rekening="1234567890", atas_nama=None,
             saldo_awal=20000, tgl_saldo_awal=awal, saldo=20000, default_untuk="BANK", aktif=True),
    ]
    mutasi = []
    baca_buku = []
    baca_akun = []
    kunci_mutasi = []
    tunggu_akun = []
    user = {"id": 17, "role": "ADMIN", "nama": "Admin Uji", "ganti_password": False}
    claims = base64.urlsafe_b64encode(json.dumps(dict(sub=user["id"], role=user["role"])).encode()).decode().rstrip("=")

    def catat(sumber, keterangan, masuk=0, keluar=0):
        mutasi.append(dict(id=len(mutasi) + 1, tanggal=hari, masuk=masuk, keluar=keluar, saldo=0,
                           keterangan=keterangan, sumber=sumber, ref=None, user=None, pembalik=False, dibalik=False))

    def api(route):
        req = route.request
        parsed = urlparse(req.url)
        path = parsed.path.removeprefix("/api/v1")
        method = req.method
        if path == "/web/auth/login":
            route.fulfill(json=dict(access_token=f"header.{claims}.signature", refresh_token="uji"))
        elif path == "/web/me":
            route.fulfill(json=user)
        elif path == "/kas/akun" and method == "GET":
            baca_akun.append(path)
            if len(baca_akun) == 1:
                tunggu_akun.append(route)
            else:
                route.fulfill(json=dict(akun=akun))
        elif path == "/kas/buku":
            filter_buku = {k: v[0] for k, v in parse_qs(parsed.query).items()}
            baca_buku.append(filter_buku)
            saldo_awal = next(a["saldo_awal"] for a in akun if a["id"] == int(filter_buku["akun"]))
            saldo = saldo_awal
            hasil = []
            if filter_buku["akun"] == "1":
                for baris in mutasi:
                    if filter_buku["dari"] <= baris["tanggal"] <= filter_buku["sampai"]:
                        saldo += baris["masuk"] - baris["keluar"]
                        hasil.append(dict(baris, saldo=saldo))
            route.fulfill(json=dict(saldo_awal=saldo_awal, saldo_akhir=saldo, mutasi=hasil))
        elif path == "/kas/akun" and method == "POST":
            body = req.post_data_json
            akun.append(dict(body, id=3, saldo=body["saldo_awal"]))
            route.fulfill(json={})
        elif path == "/kas/akun/3" and method == "PUT":
            akun[2].update(req.post_data_json)
            route.fulfill(json={})
        elif path == "/kas/transfer":
            body = req.post_data_json
            assert (body["dari_akun"], body["ke_akun"], body["nominal"]) == (1, 2, 2500)
            kunci_mutasi.append(req.headers["idempotency-key"])
            if len(kunci_mutasi) == 1:
                route.fulfill(status=500, json=dict(pesan="Server sedang sibuk"))
            else:
                catat("TRANSFER", body["keterangan"], keluar=body["nominal"])
                route.fulfill(json={})
        elif path == "/kas/mutasi/1/batal":
            assert req.post_data_json == {"alasan": "Salah catat"}
            mutasi[0]["dibalik"] = True
            catat("TRANSFER", "Pembatalan transfer", masuk=2500)
            mutasi[-1]["pembalik"] = True
            route.fulfill(json={})
        elif path == "/kas/tarik":
            assert req.post_data_json == {"dari": "2020-01-02", "sampai": hari}
            catat("PENJUALAN", "Penjualan tunai harian", masuk=1500)
            route.fulfill(json=dict(penjualan=1, beli_bayar=0, pinjaman=0))
        else:
            route.fulfill(json={})

    def buka_kas(page):
        page.route("**/api/v1/**", api)
        page.route("https://fonts.googleapis.com/**", lambda route: route.abort())
        page.goto(url, wait_until="networkidle")
        page.get_by_label("Username").fill("admin")
        page.get_by_label("Password", exact=True).fill("uji")
        page.get_by_role("button", name="Masuk", exact=True).click()
        page.get_by_role("navigation", name="Menu utama").get_by_role("button", name="Kas & Bank", exact=True).click()

    def menu_ponsel(page, nama):
        page.get_by_role("button", name="Lainnya", exact=True).click()
        page.get_by_role("dialog", name="Semua menu").get_by_role("button", name=nama, exact=True).click()

    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        page = browser.new_page(viewport={"width": 1440, "height": 900})
        page.set_default_timeout(7000)
        buka_kas(page)
        tabs = page.get_by_role("tablist", name="Kas & Bank")
        buku_tab = tabs.get_by_role("tab", name="Buku Kas / Bank")
        mutasi_tab = tabs.get_by_role("tab", name="Catat Mutasi")
        akun_tab = tabs.get_by_role("tab", name="Akun", exact=True)
        tarik_tab = tabs.get_by_role("tab", name="Tarik Transaksi")
        buku_panel = page.get_by_role("tabpanel", name="Buku Kas / Bank")
        mutasi_panel = page.get_by_role("tabpanel", name="Catat Mutasi")
        akun_panel = page.get_by_role("tabpanel", name="Akun", exact=True)
        tarik_panel = page.get_by_role("tabpanel", name="Tarik Transaksi")

        expect(tabs.get_by_role("tab")).to_have_count(4)
        expect(buku_tab).to_have_attribute("aria-selected", "true")
        expect(buku_panel.get_by_role("status")).to_have_text("Memuat buku kas / bank…")
        buku_tab.press("ArrowRight")
        expect(mutasi_tab).to_be_focused()
        page.keyboard.press("End")
        expect(tarik_tab).to_be_focused()
        page.keyboard.press("ArrowRight")
        expect(buku_tab).to_be_focused()
        page.keyboard.press("ArrowLeft")
        expect(tarik_tab).to_be_focused()
        page.keyboard.press("Home")
        expect(buku_tab).to_be_focused()
        expect(page.get_by_role("tabpanel")).to_have_count(1)
        tunggu_akun[0].fulfill(json=dict(akun=akun))
        expect(buku_panel.get_by_text("Belum ada mutasi pada periode ini.")).to_be_visible()
        expect(buku_panel.get_by_text("Saldo awal", exact=True)).to_be_visible()
        expect(buku_panel.get_by_text("Saldo akhir", exact=True)).to_be_visible()
        assert len(baca_akun) == len(baca_buku) == 1

        buku_panel.get_by_label("Dari").fill("2020-01-01")
        buku_panel.get_by_role("button", name="Tampilkan").click()
        expect(buku_panel.get_by_role("table", name=f"Kas utama · 2020-01-01 s.d. {hari}")).to_be_visible()
        assert baca_buku[-1] == {"akun": "1", "dari": "2020-01-01", "sampai": hari}
        buku_panel.get_by_label("Akun").select_option(label="BANK — Bank utama")

        mutasi_tab.click()
        expect(buku_panel).to_be_hidden()
        expect(mutasi_panel.get_by_label("Ke akun")).to_have_count(0)
        mutasi_panel.get_by_label("Jenis").select_option("TRANSFER")
        mutasi_panel.get_by_label("Dari akun").select_option(label="KAS — Kas utama")
        mutasi_panel.get_by_label("Ke akun").select_option(label="BANK — Bank utama")
        mutasi_panel.get_by_label("Nominal").fill("2500")
        mutasi_panel.get_by_label("Keterangan").fill("Setoran bank")

        akun_tab.click()
        tambah = akun_panel.get_by_role("button", name="Tambah akun")
        tambah.click()
        form = akun_panel.get_by_role("form", name="Tambah akun")
        expect(form.get_by_label("Kode")).to_be_focused()
        form.get_by_label("Kode").fill("CAD")
        form.get_by_label("Nama", exact=True).fill("Kas cadangan")
        tarik_tab.click()
        tarik_panel.get_by_label("Dari").fill("2020-01-02")
        akun_tab.click()
        expect(form.get_by_label("Nama", exact=True)).to_have_value("Kas cadangan")
        assert len(baca_akun) == 1 and len(baca_buku) == 2
        form.get_by_role("button", name="Simpan").click()
        expect(tambah).to_be_focused()
        ubah = akun_panel.get_by_role("button", name="Ubah Kas cadangan")
        ubah.click()
        edit = akun_panel.get_by_role("form", name="Ubah akun")
        edit.get_by_label("Aktif", exact=True).uncheck()
        edit.get_by_role("button", name="Simpan").click()
        expect(ubah).to_be_focused()
        expect(akun_panel.get_by_text("nonaktif", exact=True)).to_be_visible()

        mutasi_tab.click()
        expect(mutasi_panel.get_by_label("Keterangan")).to_have_value("Setoran bank")
        expect(mutasi_panel.get_by_role("option", name="CAD — Kas cadangan", include_hidden=True)).to_have_count(0)
        simpan = mutasi_panel.get_by_role("button", name="Simpan")
        simpan.click()
        expect(page.get_by_role("alert")).to_have_text("Server sedang sibuk")
        simpan.click()
        expect(page.get_by_role("status").filter(has_text="Mutasi dicatat.")).to_be_visible()
        assert len(kunci_mutasi) == 2 and kunci_mutasi[0] == kunci_mutasi[1]

        buku_tab.click()
        expect(buku_panel.get_by_label("Akun")).to_have_value("2")
        expect(buku_panel.get_by_role("table", name=f"Kas utama · 2020-01-01 s.d. {hari}")).to_be_visible()
        expect(buku_panel.get_by_text("Setoran bank")).to_be_visible()
        page.once("dialog", lambda dialog: dialog.accept("Salah catat"))
        buku_panel.get_by_role("button", name="Batal", exact=True).click()
        expect(buku_panel.get_by_text("dibatalkan", exact=True)).to_be_visible()
        expect(buku_panel.get_by_text("Pembatalan transfer")).to_be_visible()
        buku_panel.get_by_role("button", name="Tampilkan").click()
        expect(buku_panel.get_by_role("table", name=f"Bank utama · 2020-01-01 s.d. {hari}")).to_be_visible()
        buku_panel.get_by_label("Akun").select_option(label="KAS — Kas utama")
        buku_panel.get_by_role("button", name="Tampilkan").click()

        tarik_tab.click()
        expect(tarik_panel.get_by_label("Dari")).to_have_value("2020-01-02")
        tarik_panel.get_by_role("button", name="Tarik transaksi").click()
        expect(page.get_by_role("status").filter(has_text="Tarik selesai")).to_be_visible()
        buku_tab.click()
        expect(buku_panel.get_by_text("Penjualan tunai harian")).to_be_visible()
        expect(buku_panel.get_by_role("row", name="Saldo akhir Rp 11.500")).to_be_visible()

        # Periksa gulir lokal dan fokus tab pada viewport ponsel dengan mode gelap aktif.
        page.set_viewport_size({"width": 360, "height": 800})
        menu_ponsel(page, "Mode gelap")
        expect(page.locator(".app.dark")).to_have_count(1)
        expect(buku_panel.get_by_text("Penjualan tunai harian")).to_be_visible()
        gulir = buku_panel.get_by_role("region", name="Rincian buku kas dan bank")
        assert gulir.evaluate("node => node.scrollWidth > node.clientWidth")
        buku_tab.press("End")
        expect(tarik_tab).to_be_focused()
        expect(tarik_panel).to_be_visible()
        tanggal = tarik_panel.get_by_label("Dari").bounding_box()
        akhir = tarik_panel.get_by_label("Sampai").bounding_box()
        assert tanggal["x"] == akhir["x"] and tanggal["y"] < akhir["y"]

        for lebar in (320, 375, 414, 768):
            page.set_viewport_size({"width": lebar, "height": 800})
            for tab in (buku_tab, mutasi_tab, akun_tab, tarik_tab):
                tab.click()
                expect(tab).to_have_attribute("aria-selected", "true")
                assert page.evaluate("document.documentElement.scrollWidth === window.innerWidth"), lebar

        akun.clear()
        page.set_viewport_size({"width": 360, "height": 800})
        page.reload(wait_until="networkidle")
        menu_ponsel(page, "Kas & Bank")
        buku_panel.get_by_role("button", name="Buka tab Akun").click()
        expect(akun_tab).to_be_focused()
        expect(akun_panel.get_by_text("Belum ada akun.", exact=False)).to_be_visible()
        browser.close()

    print("Kas & Bank: tab, draf, filter, mutasi, pembatalan, akun, penarikan dan tampilan seluler lulus.")


if __name__ == "__main__":
    test_kas(sys.argv[1])
