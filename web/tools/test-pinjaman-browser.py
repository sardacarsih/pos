"""Smoke test build web dengan API tiruan. Jalankan bun run build lalu sajikan build/."""

import base64
import json
import sys
from urllib.parse import parse_qs, urlsplit
from uuid import UUID

from playwright.sync_api import expect, sync_playwright


def test_pinjaman(url):
    akun = dict(id=17, role="ADMIN", nama="Admin Uji", nik=None, ganti_password=False)
    anggota = dict(akun, id=18, role="ANGGOTA", nama="Ani", nik="N1")
    pengajuan = dict(id=7, nik="N1", nama="Ani", jumlah=2000000, tenor=6,
                     angsuran=352246.88, status="DIAJUKAN", keperluan="Renovasi")
    pengajuan_anggota = dict(pengajuan)
    baris = dict(no_transaksi="P-26-000001", tanggal="2026-11-19", nik="N2", nama="Budi",
                 unit_kerja="00", pinjaman=3000000, tenor=6, angsuran=528370.32, angsuran_lewat=1,
                 sisa_pokok=2500000.5, jatuh_tempo_akhir="2027-04-30")
    aksi, pembatalan, filter_laporan, dialog_browser, page_errors = [], [], [], [], []

    def mock(route):
        request = route.request
        url = urlsplit(request.url)
        path = url.path.removeprefix("/api/v1")
        if path == "/web/auth/login":
            user = anggota if request.post_data_json["username"] == "anggota" else akun
            token = base64.urlsafe_b64encode(json.dumps({"sub": user["id"]}).encode()).decode().rstrip("=")
            route.fulfill(json=dict(access_token=f"header.{token}.signature", refresh_token="refresh"))
        elif path == "/web/me":
            segment = request.headers["authorization"].split(".")[1]
            user_id = json.loads(base64.urlsafe_b64decode(segment + "=" * (-len(segment) % 4)))["sub"]
            route.fulfill(json=anggota if user_id == anggota["id"] else akun)
        elif path == "/pinjaman":
            route.fulfill(json=dict(items=[pengajuan], hak=["VERIFIKASI"]))
        elif path == "/pinjaman/antrian":
            route.fulfill(json=dict(periode=202611, dari="2026-11-01", sampai="2026-11-30",
                                    plafon=5000000, terpakai=0, sisa=5000000, items=[]))
        elif path == "/pinjaman/7/verifikasi" and request.method == "POST":
            key = UUID(request.headers["idempotency-key"])
            aksi.append((key.version, request.post_data_json))
            pengajuan.update(status="DIVERIFIKASI", verif_user=akun["id"])
            route.fulfill(json=dict(hasil="7"))
        elif path == "/pinjaman/laporan":
            q = parse_qs(url.query).get("q", [None])[0]
            filter_laporan.append(q)
            items = [baris] if q in (None, "N2") else []
            route.fulfill(json=dict(jumlah=len(items), items=items,
                                    total_pinjaman=sum(b["pinjaman"] for b in items),
                                    total_sisa_pokok=sum(b["sisa_pokok"] for b in items)))
        elif path == "/saya/pinjaman":
            route.fulfill(json=dict(aturan=dict(bunga=1.6, maks_tenor=12, maks_pinjaman=5000000),
                                    pengajuan=[pengajuan_anggota], pinjaman_aktif=[]))
        elif path == "/saya/pinjaman/7/batal" and request.method == "POST":
            pembatalan.append(UUID(request.headers["idempotency-key"]).version)
            pengajuan_anggota.update(status="DIBATALKAN")
            route.fulfill(json=dict(id="7"))
        else:
            route.fulfill(json={})

    with sync_playwright() as playwright:
        browser = playwright.chromium.launch()

        def dismiss(dialog):
            dialog_browser.append(dialog.type)
            dialog.dismiss()

        def masuk(username):
            context = browser.new_context(viewport=dict(width=1280, height=800), service_workers="block")
            context.route("**/api/v1/**", mock)
            context.route("https://fonts.googleapis.com/**", lambda route: route.abort())
            page = context.new_page()
            page.on("pageerror", lambda error: page_errors.append(str(error)))
            page.on("dialog", dismiss)
            page.goto(url)
            page.wait_for_load_state("networkidle")
            page.get_by_label("Username").fill(username)
            page.get_by_label("Password", exact=True).fill("test12345")
            page.get_by_role("button", name="Masuk", exact=True).click()
            page.get_by_role("button", name="Pinjaman", exact=True).click()
            return page

        page = masuk("admin")

        tolak = page.get_by_role("button", name="Tolak", exact=True)
        tolak.click()
        dialog = page.get_by_role("dialog")
        expect(dialog).to_be_visible()
        dialog.get_by_role("button", name="Tolak", exact=True).click()
        expect(dialog.get_by_role("alert")).to_contain_text("Alasan penolakan wajib diisi")
        page.keyboard.press("Escape")
        expect(dialog).to_have_count(0)
        expect(tolak).to_be_focused()
        assert not aksi

        page.get_by_role("button", name="Verifikasi", exact=True).click()
        dialog.get_by_label("Catatan (opsional)").fill("  dokumen lengkap  ")
        dialog.get_by_role("button", name="Verifikasi", exact=True).click()
        expect(page.get_by_role("cell", name="Diverifikasi", exact=True)).to_be_visible()
        assert aksi == [(4, {"catatan": "dokumen lengkap"})]

        page.get_by_role("tab", name="Laporan").click()
        expect(page.get_by_role("heading", name="Pinjaman aktif")).to_be_visible()
        expect(page.get_by_role("row", name="P-26-000001", exact=False)).to_contain_text("Budi")
        expect(page.get_by_text("Rp 2.500.001", exact=True)).to_have_count(2)
        pilih = page.get_by_label("Cari", exact=True)
        pilih.fill("  N2  ")
        pilih.press("Enter")
        expect(page.get_by_role("row", name="P-26-000001", exact=False)).to_be_visible()
        pilih.fill("N99")
        page.get_by_role("button", name="Tampilkan").click()
        expect(page.get_by_text("Tidak ada pinjaman aktif.")).to_be_visible()
        assert filter_laporan == [None, "N2", "N99"]

        page = masuk("anggota")
        batal = page.get_by_role("button", name="Batalkan pengajuan")
        batal.click()
        page.get_by_role("button", name="Tidak", exact=True).click()
        expect(batal).to_be_visible()
        assert not pembatalan
        batal.click()
        page.get_by_role("button", name="Ya, batalkan").click()
        expect(page.get_by_role("status").filter(has_text="Pengajuan dibatalkan")).to_be_visible()
        assert pembatalan == [4]

        assert not dialog_browser, dialog_browser
        assert not page_errors, page_errors
        browser.close()


if __name__ == "__main__":
    test_pinjaman(sys.argv[1] if len(sys.argv) > 1 else "http://127.0.0.1:4187")
    print("Smoke test Pinjaman lulus.")
