"""Uji CRUD Kategori + tab Katalog produk dengan API tiruan. Jalankan terhadap build Web lokal."""

import base64
import json
import sys
from urllib.parse import unquote, urlparse

from playwright.sync_api import expect, sync_playwright


def test_kategori(url):
    kategori = {"1": {"id": "1", "nama": "Minuman"}}
    dipakai = {"1": 1}
    tampil = []
    tulis = []

    def api(route):
        req = route.request
        path = urlparse(req.url).path.removeprefix("/api/v1")
        method = req.method
        if path == "/web/auth/login":
            claims = base64.urlsafe_b64encode(b'{"sub":17,"role":"ADMIN"}').decode().rstrip("=")
            route.fulfill(json={"access_token": f"header.{claims}.signature", "refresh_token": "uji"})
        elif path == "/web/me":
            route.fulfill(json={"id": 17, "role": "ADMIN", "nama": "Admin Uji", "ganti_password": False})
        elif path == "/admin/kategori" and method == "GET":
            route.fulfill(json=sorted(kategori.values(), key=lambda k: k["nama"]))
        elif path == "/admin/kategori" and method == "POST":
            nama = req.post_data_json["nama"].strip()
            tulis.append(("POST", nama))
            if any(k["nama"].upper() == nama.upper() for k in kategori.values()):
                route.fulfill(status=409, json={"pesan": "Nama kategori sudah dipakai"})
                return
            baru = str(max(int(k) for k in kategori) + 1)
            kategori[baru] = {"id": baru, "nama": nama}
            route.fulfill(status=201, json=kategori[baru])
        elif path.startswith("/admin/kategori/") and path.endswith("/jejak"):
            kid = unquote(path.removeprefix("/admin/kategori/").removesuffix("/jejak"))
            n = dipakai.get(kid, 0)
            route.fulfill(json={"id": kid, "nama": kategori[kid]["nama"], "bisa_hapus": n == 0, "jumlah_produk": n})
        elif path.startswith("/admin/kategori/") and method == "PUT":
            kid = unquote(path.removeprefix("/admin/kategori/"))
            nama = req.post_data_json["nama"].strip()
            tulis.append(("PUT", kid, nama))
            if any(k["nama"].upper() == nama.upper() for i, k in kategori.items() if i != kid):
                route.fulfill(status=409, json={"pesan": "Nama kategori sudah dipakai"})
                return
            kategori[kid]["nama"] = nama
            route.fulfill(status=204)
        elif path.startswith("/admin/kategori/") and method == "DELETE":
            kid = unquote(path.removeprefix("/admin/kategori/"))
            tulis.append(("DELETE", kid))
            if dipakai.get(kid, 0):
                route.fulfill(status=409, json={"pesan": f"Kategori masih dipakai {dipakai[kid]} produk."})
                return
            del kategori[kid]
            route.fulfill(status=204)
        elif path == "/admin/kategori-tampil" and method == "GET":
            route.fulfill(json=tampil)
        elif path == "/admin/kategori-tampil" and method == "PUT":
            tampil[:] = req.post_data_json["kategori_ids"]
            tulis.append(("TAMPIL", list(tampil)))
            route.fulfill(status=204)
        elif path == "/admin/produk" and method == "GET":
            route.fulfill(json={"items": [], "ada_lagi": False})
        else:
            route.fulfill(json={})

    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        page = browser.new_page(viewport={"width": 1440, "height": 900})
        page.route("**/api/v1/**", api)
        page.route("https://fonts.googleapis.com/**", lambda route: route.abort())
        page.goto(url, wait_until="networkidle")
        page.get_by_label("Username").fill("admin")
        page.get_by_label("Password", exact=True).fill("uji")
        page.get_by_role("button", name="Masuk").click()
        page.get_by_role("button", name="Katalog produk", exact=True).first.click()

        tablist = page.get_by_role("tablist", name="Katalog produk")
        expect(tablist.get_by_role("tab")).to_have_count(3)
        produk = tablist.get_by_role("tab", name="Produk", exact=True)
        expect(produk).to_have_attribute("aria-selected", "true")
        produk.press("ArrowRight")
        kat = tablist.get_by_role("tab", name="Kategori", exact=True)
        expect(kat).to_be_focused()
        expect(kat).to_have_attribute("aria-selected", "true")
        expect(page.get_by_role("tabpanel")).to_be_visible()

        page.get_by_role("button", name="Tambah kategori").press("Enter")
        dialog = page.get_by_role("dialog", name="Tambah kategori")
        dialog.get_by_label("Nama kategori").fill("Makanan")
        dialog.get_by_role("button", name="Simpan", exact=True).press("Enter")
        expect(dialog).to_be_hidden()
        expect(page.get_by_text('Kategori "Makanan" ditambahkan.')).to_be_visible()
        expect(page.get_by_role("button", name="Ubah Makanan")).to_be_visible()

        page.get_by_role("button", name="Tambah kategori").press("Enter")
        dialog = page.get_by_role("dialog", name="Tambah kategori")
        dialog.get_by_label("Nama kategori").fill("makanan")
        dialog.get_by_role("button", name="Simpan", exact=True).press("Enter")
        expect(dialog.get_by_role("alert")).to_have_text("Nama kategori sudah dipakai")
        page.keyboard.press("Escape")
        expect(dialog).to_be_hidden()

        page.get_by_role("button", name="Ubah Makanan").press("Enter")
        dialog = page.get_by_role("dialog", name="Ubah kategori")
        dialog.get_by_label("Nama kategori").fill("Makanan Ringan")
        dialog.get_by_role("button", name="Simpan", exact=True).press("Enter")
        expect(page.get_by_role("button", name="Ubah Makanan Ringan")).to_be_visible()

        page.get_by_role("button", name="Hapus Minuman").press("Enter")
        dlg = page.get_by_role("alertdialog", name="Hapus kategori?")
        expect(dlg.get_by_role("alert")).to_contain_text("masih dipakai 1 produk")
        expect(dlg.get_by_role("button", name="Hapus kategori")).to_have_count(0)
        page.keyboard.press("Escape")
        expect(dlg).to_be_hidden()

        page.get_by_role("button", name="Hapus Makanan Ringan").press("Enter")
        dlg = page.get_by_role("alertdialog", name="Hapus kategori?")
        dlg.get_by_role("button", name="Hapus kategori").press("Enter")
        expect(page.get_by_text('Kategori "Makanan Ringan" dihapus.')).to_be_visible()
        expect(page.get_by_role("button", name="Hapus Makanan Ringan")).to_have_count(0)

        page.get_by_role("checkbox", name="Minuman").press("Space")
        page.get_by_role("button", name="Simpan kategori tampil").press("Enter")
        expect(page.get_by_text("Berhasil disimpan.")).to_be_visible()
        assert ("TAMPIL", ["1"]) in tulis, tulis

        tablist.get_by_role("tab", name="Urutan produk").press("Enter")
        expect(page.get_by_label("Kategori")).to_be_visible()
        tablist.get_by_role("tab", name="Produk", exact=True).press("Enter")
        expect(page.get_by_role("combobox", name="Kategori")).to_be_visible()
        assert ("POST", "Makanan") in tulis and ("DELETE", "2") in tulis, tulis
        browser.close()
    print("CRUD Kategori, tab keyboard, hapus dibatasi, dan kategori tampil lulus.")


if __name__ == "__main__":
    test_kategori(sys.argv[1])