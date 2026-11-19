"""Smoke test CRUD Supplier dengan API tiruan. Jalankan terhadap build Web lokal."""

import base64
import json
import sys
from urllib.parse import parse_qs, unquote, urlparse

from playwright.sync_api import expect, sync_playwright


def test_supplier(url):
    peran = {"admin": "ADMIN", "petugas": "PEMBELIAN", "kasir": "KASIR", "anggota": "ANGGOTA"}
    data = {"S1": {"kode": "S1", "nama": "Supplier awal", "aktif": True}}
    for i in range(31):
        kode = f"P{i:02}"
        data[kode] = {"kode": kode, "nama": "Paginasi", "aktif": True}

    def api(route):
        request = route.request
        parsed = urlparse(request.url)
        path = parsed.path.removeprefix("/api/v1")
        method = request.method
        if path == "/web/auth/login":
            akun = request.post_data_json["username"]
            payload = base64.urlsafe_b64encode(json.dumps({"sub": 17, "role": peran[akun]}).encode()).decode().rstrip("=")
            route.fulfill(json={"access_token": f"header.{payload}.signature", "refresh_token": "uji"})
        elif path == "/web/me":
            token = request.headers["authorization"].split(".")[1]
            claims = json.loads(base64.urlsafe_b64decode(token + "=" * (-len(token) % 4)))
            route.fulfill(json={"id": claims["sub"], "role": claims["role"], "nama": "Akun Uji", "ganti_password": False})
        elif path == "/pembelian/referensi":
            supplier = [row for row in data.values() if row["aktif"]]
            route.fulfill(json={"tanggal_server": "2026-11-19", "supplier": supplier, "barang": []})
        elif path == "/supplier" and method == "GET":
            q = parse_qs(parsed.query)
            page = int(q.get("page", ["1"])[0])
            term = q.get("q", [""])[0].upper()
            status = q.get("aktif", [""])[0]
            items = [row for row in data.values() if term in row["kode"].upper() or term in row["nama"].upper()]
            items = [row for row in items if not status or row["aktif"] == (status == "true")]
            items.sort(key=lambda row: (row["nama"], row["kode"]))
            route.fulfill(json={"items": items[(page - 1) * 30:page * 30], "page": page, "ada_lagi": len(items) > page * 30})
        elif path == "/supplier" and method == "POST":
            form = request.post_data_json
            kode = form["kode"].strip().upper()
            if kode in data:
                route.fulfill(status=409, json={"pesan": "Kode supplier sudah digunakan"})
                return
            data[kode] = {"kode": kode, "nama": form["nama"].strip().upper(), "aktif": True}
            route.fulfill(status=201, json=data[kode])
        elif path.startswith("/supplier/") and method == "PATCH":
            row = data[unquote(path.removeprefix("/supplier/"))]
            form = request.post_data_json
            if "nama" in form:
                form["nama"] = form["nama"].strip().upper()
            row.update(form)
            route.fulfill(json=row)
        else:
            route.fulfill(json={})

    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)

        def buka(akun):
            context = browser.new_context(viewport={"width": 1440, "height": 900})
            context.route("**/api/v1/**", api)
            context.route("https://fonts.googleapis.com/**", lambda route: route.abort())
            page = context.new_page()
            page.goto(url, wait_until="networkidle")
            page.get_by_label("Username").fill(akun)
            page.get_by_label("Password", exact=True).fill("uji")
            page.get_by_role("button", name="Masuk").click()
            return page

        page = buka("petugas")
        nav = page.get_by_role("button", name="Supplier", exact=True).first
        nav.press("Enter")
        page.get_by_role("button", name="Berikutnya").press("Enter")
        expect(page.get_by_text("Halaman 2")).to_be_visible()
        page.get_by_role("button", name="Sebelumnya").press("Enter")
        expect(page.get_by_text("Halaman 1")).to_be_visible()
        page.get_by_role("button", name="Tambah supplier").press("Enter")
        dialog = page.get_by_role("dialog", name="Tambah supplier")
        kode = dialog.get_by_label("Kode supplier")
        expect(kode).to_be_focused()
        kode.fill(" Baru_% ")
        kode.press("Tab")
        nama = dialog.get_by_label("Nama supplier")
        expect(nama).to_be_focused()
        nama.fill("Toko Baru")
        nama.press("Enter")
        expect(dialog).to_be_hidden()
        cari = page.get_by_role("searchbox")
        expect(cari).to_be_focused()
        expect(page.get_by_role("rowheader", name="BARU_%")).to_be_visible()

        page.get_by_role("button", name="Ubah nama BARU_%").press("Enter")
        dialog = page.get_by_role("dialog", name="Ubah nama supplier")
        expect(dialog.get_by_label("Kode supplier")).to_have_attribute("readonly", "")
        nama = dialog.get_by_label("Nama supplier")
        expect(nama).to_be_focused()
        nama.fill("Nama Diubah")
        nama.press("Enter")
        expect(page.get_by_role("cell", name="NAMA DIUBAH")).to_be_visible()

        nonaktifkan = page.get_by_role("button", name="Nonaktifkan BARU_%")
        nonaktifkan.press("Enter")
        dialog = page.get_by_role("dialog", name="Nonaktifkan supplier?")
        expect(dialog.get_by_role("button", name="Batal")).to_be_focused()
        page.keyboard.press("Escape")
        expect(dialog).to_be_hidden()
        expect(nonaktifkan).to_be_focused()
        nonaktifkan.press("Enter")
        dialog.get_by_role("button", name="Nonaktifkan").press("Enter")
        expect(page.get_by_text("Supplier berhasil dinonaktifkan.")).to_be_visible()
        page.get_by_label("Status supplier").select_option("true")
        expect(page.get_by_text("Tidak ada supplier yang cocok.")).to_be_visible()
        page.get_by_label("Status supplier").select_option("false")
        expect(page.get_by_role("rowheader", name="BARU_%")).to_be_visible()

        page.get_by_role("button", name="Tambah supplier").press("Enter")
        dialog = page.get_by_role("dialog", name="Tambah supplier")
        dialog.get_by_label("Kode supplier").fill("baru_%")
        dialog.get_by_label("Nama supplier").fill("Duplikat")
        dialog.get_by_role("button", name="Simpan").press("Enter")
        expect(dialog.get_by_role("alert")).to_have_text("Kode supplier sudah digunakan")
        page.keyboard.press("Escape")
        page.get_by_role("button", name="Aktifkan kembali BARU_%").press("Enter")
        page.get_by_role("dialog").get_by_role("button", name="Aktifkan kembali").press("Enter")
        expect(page.get_by_text("Supplier berhasil diaktifkan kembali.")).to_be_visible()

        page.get_by_label("Status supplier").select_option("true")
        page.get_by_role("button", name="Ubah nama BARU_%").press("Enter")
        dialog = page.get_by_role("dialog", name="Ubah nama supplier")
        page.evaluate("Object.defineProperty(navigator, 'onLine', {get: () => false, configurable: true})")
        page.evaluate("window.dispatchEvent(new Event('offline'))")
        expect(dialog.get_by_role("button", name="Simpan")).to_be_disabled()
        page.keyboard.press("Escape")
        page.evaluate("delete navigator.onLine")
        page.evaluate("window.dispatchEvent(new Event('online'))")
        page.get_by_role("button", name="Pembelian", exact=True).first.press("Enter")
        expect(page.get_by_role("combobox", name="Supplier").locator("option", has_text="NAMA DIUBAH")).to_have_count(1)
        page.evaluate("Object.defineProperty(navigator, 'onLine', {get: () => false, configurable: true})")
        page.evaluate("window.dispatchEvent(new Event('offline'))")
        expect(nav).to_have_count(0)
        page.context.close()

        admin = buka("admin")
        admin.get_by_role("button", name="Supplier", exact=True).first.press("Enter")
        expect(admin.get_by_role("heading", name="Daftar supplier")).to_be_visible()
        admin.context.close()
        for akun in ("kasir", "anggota"):
            page = buka(akun)
            expect(page.get_by_role("main")).to_be_visible()
            expect(page.get_by_role("button", name="Supplier", exact=True)).to_have_count(0)
            page.context.close()
        browser.close()
    print("CRUD Supplier, keyboard, paginasi, izin menu, offline, dan katalog Pembelian lulus.")


if __name__ == "__main__":
    test_supplier(sys.argv[1])
