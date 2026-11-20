"""Alur akun admin dan password sementara menggunakan API tiruan di build web lokal."""

import sys
from urllib.parse import urlparse

from playwright.sync_api import expect, sync_playwright


def test_users(url):
    users = {
        "admin": dict(id=1, username="admin", nama="Administrator", role="ADMIN", aktif=True, terkunci=False, nik=None),
        "kasir": dict(id=2, username="kasir", nama="Kasir Utama", role="KASIR", aktif=True, terkunci=True, nik=None),
    }
    passwords = {"admin": "admin-uji"}
    pending = set()
    reset_calls = []
    member = dict(nama="Rani", nik="NIK123", unit_nama="Gudang")

    def api(route):
        req = route.request
        path = urlparse(req.url).path.removeprefix("/api/v1")
        username = req.headers.get("authorization", "").removeprefix("Bearer ")
        if path == "/web/auth/login":
            form = req.post_data_json
            username = form["username"]
            if passwords.get(username) != form["password"]:
                return route.fulfill(status=401, json={"pesan": "Login gagal"})
            result = dict(access_token=username, refresh_token="uji")
        elif path == "/web/me":
            result = dict(**users[username], ganti_password=username in pending)
        elif path == "/web/admin/users" and req.method == "GET":
            result = list(users.values())
        elif path == "/pelanggan":
            result = dict(items=[member], ada_lagi=False)
        elif path == "/web/admin/users":
            form = req.post_data_json
            username = form["username"]
            users[username] = dict(id=len(users) + 1, aktif=True, terkunci=False, **form)
            passwords[username] = "TEMPBUAT"
            pending.add(username)
            result = dict(password_sementara=passwords[username])
        elif path.endswith("/reset-password"):
            reset_calls.append(path)
            if len(reset_calls) == 1:
                return route.fulfill(status=503, json={"pesan": "Coba kembali"})
            users["kasir"]["terkunci"] = False
            passwords["kasir"] = "TEMPUJIX"
            pending.add("kasir")
            result = dict(password_sementara=passwords["kasir"])
        elif path == "/admin/users/2/aktif":
            users["kasir"]["aktif"] = req.post_data_json["aktif"]
            return route.fulfill(status=204)
        elif path == "/auth/ganti-password":
            form = req.post_data_json
            if form["password_lama"] != passwords[username]:
                return route.fulfill(status=400, json={"pesan": "Password lama salah"})
            pending.remove(username)
            passwords[username] = form["password_baru"]
            return route.fulfill(status=204)
        else:
            result = {}
        route.fulfill(json=result)

    with sync_playwright() as p:
        browser = p.chromium.launch()

        def login(username):
            ctx = browser.new_context(viewport=dict(width=1440, height=900))
            ctx.route("**/api/v1/**", api)
            ctx.route("https://fonts.googleapis.com/**", lambda route: route.abort())
            page = ctx.new_page()
            page.goto(url)
            page.get_by_label("Username", exact=True).fill(username)
            page.get_by_label("Password", exact=True).fill(passwords[username])
            page.get_by_role("button", name="Masuk", exact=True).click()
            return page

        page = login("admin")
        button = lambda name: page.get_by_role("button", name=name, exact=True)
        button("Pengguna").press("Enter")
        expect(button("Nonaktifkan admin")).to_be_disabled()
        button("Mode gelap").click()
        page.set_viewport_size(dict(width=390, height=844))
        search = page.get_by_role("searchbox")
        search.fill(" KASIR ")
        button("Terkunci 1").press("Enter")
        row = page.get_by_role("row").filter(has_text="Kasir Utama")
        expect(row.get_by_text("Aktif", exact=True)).to_be_visible()
        expect(row.get_by_text("Terkunci", exact=True)).to_be_visible()
        page.wait_for_function("document.documentElement.scrollWidth <= innerWidth")
        reset = button("Reset password kasir")
        reset.press("Enter")
        page.keyboard.press("Escape")
        expect(reset).to_be_focused()
        assert not reset_calls
        reset.press("Enter")
        button("Reset password").press("Enter")
        expect(page.get_by_role("alert")).to_have_text("Coba kembali")
        button("Reset password").press("Enter")
        expect(page.get_by_role("dialog")).to_contain_text("TEMPUJIX")
        page.context.grant_permissions(["clipboard-read", "clipboard-write"])
        button("Salin password").click()
        expect(page.get_by_role("status").filter(has_text="Password tersalin")).to_be_visible()
        assert page.evaluate("navigator.clipboard.readText()") == "TEMPUJIX"
        page.keyboard.press("Escape")
        expect(page.get_by_role("dialog")).to_be_visible()
        button("Sudah dicatat").click()
        expect(search).to_have_value(" KASIR ")
        expect(button("Terkunci 0")).to_have_attribute("aria-pressed", "true")
        expect(row).to_have_count(0)
        button("Hapus filter").press("Enter")
        expect(row).to_be_visible()

        page.set_viewport_size(dict(width=1440, height=900))
        button("Nonaktifkan kasir").click()
        page.get_by_role("dialog").get_by_role("button", name="Nonaktifkan", exact=True).click()
        button("Nonaktif").click()
        expect(row).to_be_visible()
        button("Aktifkan kasir").click()
        page.get_by_role("dialog").get_by_role("button", name="Aktifkan", exact=True).click()
        expect(row).to_have_count(0)
        button("Hapus filter").click()
        button("Tambah pengguna").click()
        dialog = page.get_by_role("dialog")
        dialog.get_by_label("Nama", exact=True).fill("Budi")
        dialog.get_by_label("Username").fill("budi")
        dialog.get_by_label("Peran").select_option("KASIR")
        button("Buat pengguna").click()
        expect(dialog).to_contain_text("budi")
        button("Sudah dicatat").click()
        button("Anggota 0").click()
        button("Tambah akun anggota").click()
        dialog.get_by_role("button", name="Rani", exact=False).click()
        button("Buat akun anggota").click()
        expect(dialog).to_contain_text("nik123")
        button("Sudah dicatat").click()
        search.fill("NIK123")
        expect(page.get_by_role("rowheader").filter(has_text="Rani")).to_be_visible()

        for username in ["kasir", "budi", "nik123"]:
            user_page = login(username)
            gate = user_page.get_by_role("heading", name="Buat password baru")
            expect(gate).to_be_visible()
            user_page.reload()
            expect(gate).to_be_visible()
            old = user_page.get_by_label("Password sementara")
            old.fill("keliru")
            user_page.get_by_label("Password baru (minimal 8 karakter)").fill("PasswordBaruUji")
            user_page.get_by_role("button", name="Ganti password").click()
            expect(user_page.get_by_role("alert")).to_have_text("Password lama salah")
            old.fill(passwords[username])
            user_page.get_by_role("button", name="Ganti password").click()
            expect(user_page.get_by_role("status")).to_contain_text("Silakan masuk kembali")
            user_page.context.close()
            signed_in = login(username)
            expect(signed_in.locator("main#konten")).to_be_visible()
            signed_in.context.close()
        browser.close()


if __name__ == "__main__":
    test_users(sys.argv[1])
