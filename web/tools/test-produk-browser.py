"""Uji Tambah Produk dalam Chromium terhadap build Web dengan API tiruan."""

import base64
import json
import sys
from urllib.parse import urlparse

from playwright.sync_api import expect, sync_playwright


def test_produk(url):
    products = []
    posts = []
    writes = []

    def api(route):
        req = route.request
        path = urlparse(req.url).path.removeprefix("/api/v1")
        if req.method in ("POST", "PUT") and path.startswith("/admin/produk"):
            writes.append((req.method, path))
        if path == "/web/auth/login":
            claims = base64.urlsafe_b64encode(b'{"sub":17,"role":"ADMIN"}').decode().rstrip("=")
            route.fulfill(json={"access_token": f"header.{claims}.signature", "refresh_token": "uji"})
        elif path == "/web/me":
            route.fulfill(json={"id": 17, "role": "ADMIN", "nama": "Admin Uji", "ganti_password": False})
        elif path == "/admin/kategori":
            route.fulfill(json=[{"id": "1", "nama": "Minuman"}])
        elif path == "/admin/produk" and req.method == "GET":
            route.fulfill(json={"items": products, "ada_lagi": False})
        elif path == "/admin/produk" and req.method == "POST":
            body = req.post_data_json
            posts.append((req.headers["idempotency-key"], body))
            if len(posts) == 1:
                route.fulfill(status=503, json={"pesan": "Layanan sementara tidak tersedia"})
                return
            products.append({**body, "id": 1, "kode": "KB-26-00001", "kategori": "Minuman"})
            route.fulfill(status=201, json=products[-1])
        elif path == "/admin/produk/1/satuan" and req.method == "GET":
            route.fulfill(json={"satuan": [
                {**s, "isi": str(s["isi"]), "harga": str(s["harga"])}
                for s in products[0]["satuan_lain"]
            ]})
        else:
            route.fulfill(json={})

    with sync_playwright() as p:
        browser = p.chromium.launch()
        page = browser.new_page(service_workers="block")
        page.route("**/api/v1/**", api)
        page.route("https://fonts.googleapis.com/**", lambda route: route.abort())
        page.goto(url)
        page.get_by_label("Username").fill("admin")
        page.get_by_label("Password", exact=True).fill("uji")
        page.get_by_role("button", name="Masuk").click()
        page.get_by_role("button", name="Katalog produk", exact=True).first.click()
        page.get_by_role("button", name="Tambah produk", exact=True).click()
        dialog = page.get_by_role("dialog", name="Tambah produk")
        dialog.get_by_label("Nama", exact=True).fill("Teh Kotak")
        dialog.get_by_label("Harga jual", exact=True).fill("5000")
        dialog.get_by_label("Harga beli", exact=True).fill("4000")

        for i, (nama, isi, harga, barcode) in enumerate(
            [("BOX", "12", "55000", "899012"), ("PAK", "6.5", "30000.25", "")], 1
        ):
            dialog.get_by_role("button", name="Tambah satuan").click()
            unit = dialog.get_by_role("group", name=f"Satuan tambahan {i}")
            unit.get_by_label("Satuan", exact=True).fill(nama)
            unit.get_by_label("Isi", exact=True).fill(isi)
            unit.get_by_label("Harga jual").fill(harga)
            unit.get_by_label("Barcode (opsional)").fill(barcode)

        simpan = dialog.get_by_role("button", name="Simpan", exact=True)
        simpan.click()
        expect(dialog.get_by_role("alert")).to_be_visible()
        expect(dialog.get_by_role("group", name="Satuan tambahan 2").get_by_label("Isi")).to_have_value("6.5")
        simpan.click()
        expect(dialog).to_be_hidden()
        assert writes == [("POST", "/admin/produk")] * 2
        assert posts[0] == posts[1]
        assert posts[0][0]
        assert posts[0][1]["satuan_lain"] == [
            {"satuan": "BOX", "isi": 12, "harga": 55000, "barcode": "899012"},
            {"satuan": "PAK", "isi": 6.5, "harga": 30000.25, "barcode": None},
        ], json.dumps(posts[0][1])
        page.get_by_role("button", name="Ubah Teh Kotak").click()
        edit = page.get_by_role("dialog", name="Ubah produk")
        box = edit.get_by_role("group", name="Satuan tambahan 1")
        expect(box.get_by_label("Isi")).to_have_value("12")
        expect(box.get_by_label("Barcode (opsional)")).to_have_value("899012")
        browser.close()


if __name__ == "__main__":
    test_produk(sys.argv[1])
    print("Tambah Produk multi-satuan dan retry lulus di Chromium.")
