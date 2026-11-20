"""Uji module Persediaan (5 tab, kartu stok, opname cepat, penyesuaian + balik, valuasi/ambang,
peran, dirty-guard opname, lebar numerik + mode gelap) dengan API tiruan. Jalankan terhadap build Web lokal."""

import base64
import datetime
import re
import sys
from urllib.parse import parse_qs, urlparse

from playwright.sync_api import expect, sync_playwright

UUID = re.compile(r"^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$", re.I)
HARI = datetime.date.today().isoformat()
AWAL = HARI[:4] + "-01-01"

BARANG = [
    {"id": 101, "kode": "BRG-101", "nama": "Gula Pasir", "satuan": "kg", "stok": 5, "kategori": "Sembako", "barcode": None},
    {"id": 102, "kode": "BRG-102", "nama": "Kopi Bubuk", "satuan": "pcs", "stok": 0, "kategori": "Minuman", "barcode": None},
]


def test_persediaan(url):
    tulis = []
    kartu_params = []
    habis_gets = []
    galat_opname = [None]
    SATUAN = {101: [{"satuan": "DUS", "isi": 12, "barcode": "BDUS101"}],
              102: [{"satuan": "DUS", "isi": 12, "barcode": "BDUS102"}]}
    akun = {"id": 17, "role": "ADMIN", "nama": "Admin Uji", "ganti_password": False}
    dialog_jawab = {"aksi": "accept"}
    sesuai = [{"id": 9001, "kode": "BRG-101", "nama": "Gula Pasir", "tanggal": HARI, "qty": "5",
               "alasan": "Hitung ulang gudang", "user_nama": "Admin Uji", "balik_id": None,
               "dibalik": False, "created_at": HARI + " 08:00:00"}]
    ambang = {"102": "5"}

    def api(route):
        req = route.request
        path = urlparse(req.url).path.removeprefix("/api/v1")
        qs = {k: v[0] for k, v in parse_qs(urlparse(req.url).query).items()}
        method = req.method
        if path == "/web/auth/login":
            claims = base64.urlsafe_b64encode(('{"sub":17,"role":"%s"}' % akun["role"]).encode()).decode().rstrip("=")
            route.fulfill(json={"access_token": f"header.{claims}.signature", "refresh_token": "uji"})
        elif path == "/web/me":
            route.fulfill(json=akun)
        elif path == "/admin/menu-badge":
            route.fulfill(json={"stok_habis": 3, "harga_rendah": 1})
        elif path == "/admin/stok" and method == "GET":
            qraw = qs.get("q") or ""
            q = qraw.lower()
            items = []
            for b in BARANG:
                if not q or q in b["nama"].lower() or q in b["kode"].lower() \
                        or b["barcode"] == qraw or any(s["barcode"] == qraw for s in SATUAN[b["id"]]):
                    item = dict(b)
                    if qs.get("multi_satuan") == "1":
                        item["satuan_tambahan"] = SATUAN[b["id"]]
                    items.append(item)
            route.fulfill(json={"items": items, "ada_lagi": False, "page": 1})
        elif path == "/admin/stok-habis":
            habis_gets.append(qs)
            item = dict(BARANG[1])
            if qs.get("multi_satuan") == "1":
                item["satuan_tambahan"] = SATUAN[102]
            route.fulfill(json={"items": [item], "ada_lagi": False, "page": 1, "jumlah": 1, "kontrol_stok": True})
        elif path == "/admin/stok-opname" and method == "GET":
            route.fulfill(json={"items": [], "ada_lagi": False, "tahun": int(HARI[:4])})
        elif path == "/admin/stok-opname" and method == "POST":
            key = req.headers.get("idempotency-key", "")
            body = req.post_data_json
            tulis.append(("OPNAME", key, body))
            if galat_opname[0]:
                route.fulfill(status=400, json={"pesan": galat_opname[0]})
                galat_opname[0] = None
            else:
                route.fulfill(json={"nomor_so": "SO-UJI-1", "selisih": 4})
        elif path == "/admin/stok-opname/dokumen" and method == "POST":
            key = req.headers.get("idempotency-key", "")
            tulis.append(("DOKUMEN", key, req.post_data_json))
            route.fulfill(json={"nomor_so": "SO-UJI-2", "tanggal": HARI, "items": []})
        elif path == "/admin/persediaan/kartu":
            kartu_params.append(qs)
            page = int(qs.get("page", "1"))
            items = ([{"tanggal": AWAL[:4] + "-01-05", "jenis": "pembelian", "ref": "PO-1", "qty": "10", "saldo": "20"},
                      {"tanggal": AWAL[:4] + "-02-01", "jenis": "penjualan", "ref": "PJ-9", "qty": "-8", "saldo": "12"}]
                     if page == 1 else
                     [{"tanggal": AWAL[:4] + "-03-01", "jenis": "penyesuaian", "ref": "SESUAI-9001", "qty": "2", "saldo": "14"}])
            route.fulfill(json={"produk": {"id": 101, "kode": "BRG-101", "nama": "Gula Pasir", "satuan": "kg"},
                                "dari": qs.get("dari", AWAL), "sampai": qs.get("sampai", HARI),
                                "saldo_awal": "10", "saldo_akhir": "14", "items": items,
                                "ada_lagi": page == 1, "page": page})
        elif path == "/admin/persediaan/sesuaikan" and method == "GET":
            route.fulfill(json={"items": sesuai, "ada_lagi": False, "page": 1})
        elif path == "/admin/persediaan/sesuaikan" and method == "POST":
            key = req.headers.get("idempotency-key", "")
            body = req.post_data_json
            tulis.append(("SESUAI", key, body))
            route.fulfill(json={"id": 9002, "kode": "BRG-101", "tanggal": body["tanggal"], "qty": str(body["qty"]),
                                "saldo_sesudah": "3", "peringatan": None, "ulang": False})
        elif path == "/admin/persediaan/sesuaikan/9001/balik" and method == "POST":
            key = req.headers.get("idempotency-key", "")
            tulis.append(("BALIK", key, req.post_data_json))
            sesuai[0]["dibalik"] = True
            route.fulfill(json={"id": 9003, "kode": "BRG-101", "tanggal": HARI, "qty": "-5",
                                "saldo_sesudah": "5", "peringatan": None, "ulang": False})
        elif path == "/admin/persediaan/valuasi":
            route.fulfill(json={"total_nilai": "150000", "total_produk": 2, "tanpa_hpp": 1, "jumlah_stok_minim": 1,
                                "per_kategori": [{"kategori": "Sembako", "jumlah_produk": 2, "total_qty": "5",
                                                  "nilai": "150000", "tanpa_hpp": 1}]})
        elif path == "/admin/persediaan/stok-minimum":
            items = [dict(BARANG[1], stok="0", stok_min=ambang["102"])] if ambang["102"] else []
            route.fulfill(json={"items": items, "ada_lagi": False, "page": 1, "jumlah": len(items)})
        elif path.startswith("/admin/persediaan/stok-min/") and method == "PUT":
            pid = path.rsplit("/", 1)[1]
            body = req.post_data_json
            tulis.append(("AMBANG", pid, body))
            ambang[pid] = "" if body["stok_min"] is None else str(body["stok_min"])
            route.fulfill(json={"ok": True})
        else:
            route.fulfill(json={})

    def masuk(page):
        page.get_by_label("Username").fill("admin")
        page.get_by_label("Password", exact=True).fill("uji")
        page.get_by_role("button", name="Masuk", exact=True).click()

    def pilih_produk_kartu(page):
        page.get_by_label("Cari produk").fill("Gula")
        page.get_by_role("button", name="Cari", exact=True).click()
        page.get_by_role("button", name=re.compile("Gula Pasir")).click()

    def ukur(page):
        return page.evaluate("""() => {
            const doc = document.documentElement;
            const hasil = { halaman: doc.scrollWidth <= doc.clientWidth, strip: true, tabel: true };
            const strip = document.querySelector('.sedia-tab');
            if (strip) { const r = strip.getBoundingClientRect(); hasil.strip = r.left >= -1 && r.right <= window.innerWidth + 1; }
            for (const g of document.querySelectorAll('.sedia-gulir')) {
                const r = g.getBoundingClientRect();
                if (r.left < -1 || r.right > window.innerWidth + 1) hasil.tabel = false;
            }
            return hasil;
        }""")

    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        page = browser.new_page(viewport={"width": 1440, "height": 900})
        page.route("**/api/v1/**", api)
        page.route("https://fonts.googleapis.com/**", lambda route: route.abort())
        page.on("dialog", lambda d: d.accept() if dialog_jawab["aksi"] == "accept" else d.dismiss())
        page.goto(url, wait_until="networkidle")
        masuk(page)

        # Lencana nav tetap stok_habis, kini di item Persediaan.
        sidebar = page.get_by_role("navigation", name="Menu utama")
        expect(sidebar.get_by_role("button", name="Persediaan, 3 produk perlu perhatian")).to_be_visible()
        sidebar.get_by_role("button", name=re.compile("^Persediaan")).click()

        tablist = page.get_by_role("tablist", name="Persediaan")
        expect(tablist.get_by_role("tab")).to_have_count(5)
        kartu = tablist.get_by_role("tab", name="Kartu stok", exact=True)
        expect(kartu).to_have_attribute("aria-selected", "true")
        kartu.press("ArrowRight")
        habis = tablist.get_by_role("tab", name="Stok habis", exact=True)
        expect(habis).to_be_focused()
        expect(habis).to_have_attribute("aria-selected", "true")
        page.keyboard.press("Home")
        expect(kartu).to_be_focused()
        expect(kartu).to_have_attribute("aria-selected", "true")
        page.keyboard.press("End")
        valuasi_tab = tablist.get_by_role("tab", name="Valuasi & alert", exact=True)
        expect(valuasi_tab).to_be_focused()
        expect(valuasi_tab).to_have_attribute("aria-selected", "true")

        # Kartu stok: mutasi, saldo, parameter paginasi.
        kartu.click()
        pilih_produk_kartu(page)
        expect(page.get_by_role("cell", name="Pembelian", exact=True)).to_be_visible()
        expect(page.get_by_role("cell", name="PO-1", exact=True)).to_be_visible()
        expect(page.locator("dt", has_text="Saldo akhir")).to_be_visible()
        assert kartu_params[-1]["produk_id"] == "101" and kartu_params[-1]["page"] == "1", kartu_params
        assert kartu_params[-1]["dari"] == AWAL and kartu_params[-1]["sampai"] == HARI, kartu_params
        page.get_by_role("button", name="Berikutnya").click()
        expect(page.get_by_role("cell", name="Penyesuaian stok", exact=True)).to_be_visible()
        assert kartu_params[-1]["page"] == "2", kartu_params
        page.get_by_role("button", name="Sebelumnya").click()
        expect(page.get_by_role("cell", name="Pembelian", exact=True)).to_be_visible()
        assert kartu_params[-1]["page"] == "1", kartu_params

        # Stok habis + opname cepat multi-satuan: beberapa kolom, total gabungan,
        # penolakan faktor berubah -> muat ulang satuan (isian terjaga) -> simpan dengan kunci yang sama.
        habis.click()
        expect(page.get_by_text("1 produk aktif dengan stok sistem ≤ 0")).to_be_visible()
        page.get_by_role("button", name="Opname", exact=True).click()
        dlg = page.get_by_role("dialog", name="Opname stok")
        dlg.get_by_label("Jumlah pcs Kopi Bubuk").fill("3")
        dlg.get_by_label("Jumlah DUS Kopi Bubuk").fill("2")
        expect(dlg.get_by_text("Total fisik 27 pcs")).to_be_visible()
        galat_opname[0] = ('Faktor konversi satuan "DUS" berubah (12 menjadi 24); '
                           'muat ulang satuan produk ini sebelum meninjau')
        dlg.get_by_role("button", name="Simpan opname").click()
        tombol_muat = dlg.get_by_role("button", name="Muat ulang satuan produk")
        expect(tombol_muat).to_be_visible()
        SATUAN[102] = [{"satuan": "DUS", "isi": 24, "barcode": "BDUS102"}]
        tombol_muat.click()
        expect(dlg.get_by_label("Jumlah DUS Kopi Bubuk")).to_have_value("2")
        expect(dlg.get_by_text("(1 DUS = 24 pcs)")).to_be_visible()
        expect(dlg.get_by_text("Total fisik 51 pcs")).to_be_visible()
        dlg.get_by_role("button", name="Simpan opname").click()
        expect(page.get_by_text("Opname SO-UJI-1 tersimpan (selisih 4).")).to_be_visible()
        opname_tulis = [t for t in tulis if t[0] == "OPNAME"]
        assert len(opname_tulis) == 2 and opname_tulis[0][1] == opname_tulis[1][1] and UUID.match(opname_tulis[0][1]), tulis
        assert opname_tulis[0][2] == {"produk_id": 102, "hitungan": [
            {"satuan": "pcs", "qty": "3", "isi": "1"}, {"satuan": "DUS", "qty": "2", "isi": "12"}]}, tulis
        assert opname_tulis[1][2] == {"produk_id": 102, "hitungan": [
            {"satuan": "pcs", "qty": "3", "isi": "1"}, {"satuan": "DUS", "qty": "2", "isi": "24"}]}, tulis
        assert habis_gets and all(g.get("multi_satuan") == "1" for g in habis_gets), habis_gets
        assert any(g.get("q") == "BRG-102" for g in habis_gets), habis_gets
        # Kembalikan faktor ke 12: bagian opname baru di bawah menguji angka dasar.
        SATUAN[102] = [{"satuan": "DUS", "isi": 12, "barcode": "BDUS102"}]

        # Penyesuaian: tanpa alasan ditolak klien; valid tercatat dengan kunci UUID.
        tablist.get_by_role("tab", name="Penyesuaian", exact=True).click()
        page.get_by_role("button", name="Buat penyesuaian").click()
        dlg = page.get_by_role("dialog", name="Buat penyesuaian stok")
        dlg.get_by_label("Cari produk").fill("Gula")
        dlg.get_by_role("button", name="Cari", exact=True).click()
        dlg.get_by_role("button", name=re.compile("Gula Pasir")).click()
        dlg.get_by_label("Qty penyesuaian").fill("-2")
        dlg.get_by_role("button", name="Simpan penyesuaian").click()
        expect(dlg.get_by_role("alert")).to_have_text("Alasan wajib diisi (3-250 karakter)")
        assert not [t for t in tulis if t[0] == "SESUAI"], tulis
        dlg.get_by_label("Alasan (wajib)").fill("Uji coba")
        dlg.get_by_role("button", name="Simpan penyesuaian").click()
        expect(dlg).to_be_hidden()
        expect(page.get_by_text("Penyesuaian BRG-101 -2 tersimpan. Saldo sekarang 3.")).to_be_visible()
        sesuai_tulis = [t for t in tulis if t[0] == "SESUAI"]
        assert len(sesuai_tulis) == 1 and UUID.match(sesuai_tulis[0][1]), tulis
        assert sesuai_tulis[0][2] == {"produk_id": 101, "qty": -2, "tanggal": HARI, "alasan": "Uji coba"}, tulis

        # Balik: alertdialog bernama, Escape batal, konfirmasi tercatat; baris jadi Dibalik.
        page.get_by_role("button", name="Balik", exact=True).click()
        dlg = page.get_by_role("alertdialog", name="Balik penyesuaian?")
        page.keyboard.press("Escape")
        expect(dlg).to_be_hidden()
        assert not [t for t in tulis if t[0] == "BALIK"], tulis
        page.get_by_role("button", name="Balik", exact=True).click()
        dlg = page.get_by_role("alertdialog", name="Balik penyesuaian?")
        dlg.get_by_label("Alasan pembalikan (wajib)").fill("Salah input")
        dlg.get_by_role("button", name="Balik penyesuaian").click()
        expect(dlg).to_be_hidden()
        expect(page.get_by_role("cell", name="Dibalik", exact=True)).to_be_visible()
        expect(page.get_by_role("button", name="Balik", exact=True)).to_be_disabled()
        balik_tulis = [t for t in tulis if t[0] == "BALIK"]
        assert len(balik_tulis) == 1 and UUID.match(balik_tulis[0][1]), tulis
        assert balik_tulis[0][2] == {"alasan": "Salah input"}, tulis

        # Valuasi + editor ambang (PUT angka lalu NULL saat dikosongkan).
        valuasi_tab.click()
        expect(page.get_by_text("Rp 150.000", exact=True).first).to_be_visible()
        expect(page.get_by_text("1 produk belum punya harga beli", exact=False)).to_be_visible()
        ambang_input = page.get_by_label("Ambang minimum Kopi Bubuk")
        expect(ambang_input).to_have_value("5")
        ambang_input.fill("8")
        page.get_by_role("button", name="Simpan", exact=True).click()
        expect(page.get_by_text("Ambang stok minimum disimpan.")).to_be_visible()
        ambang_input.fill("")
        page.get_by_role("button", name="Simpan", exact=True).click()
        expect(page.get_by_text("Ambang dihapus (tanpa alert).")).to_be_visible()
        ambang_tulis = [t for t in tulis if t[0] == "AMBANG"]
        assert ambang_tulis == [("AMBANG", "102", {"stok_min": 8}), ("AMBANG", "102", {"stok_min": None})], tulis

        # Opname baru multi-satuan: kolom per satuan, total = 2 x 12, simpan dokumen memakai payload hitungan.
        tablist.get_by_role("tab", name="Stock opname", exact=True).click()
        page.get_by_role("button", name="Opname baru").click()
        page.get_by_label("Cari atau scan barang").fill("Kopi")
        page.get_by_role("button", name="Cari", exact=True).click()
        page.get_by_role("button", name=re.compile("Kopi Bubuk")).click()
        page.get_by_label("Jumlah DUS Kopi Bubuk").fill("2")
        expect(page.get_by_text("= 24 pcs")).to_be_visible()
        page.get_by_role("button", name="Tinjau dan simpan").click()
        page.get_by_role("button", name="Simpan dokumen").click()
        expect(page.get_by_text("SO-UJI-2")).to_be_visible()
        dokumen_tulis = [t for t in tulis if t[0] == "DOKUMEN"]
        assert len(dokumen_tulis) == 1 and UUID.match(dokumen_tulis[0][1]), tulis
        assert dokumen_tulis[0][2] == {"items": [{"produk_id": 102, "hitungan": [
            {"satuan": "DUS", "qty": "2", "isi": "12"}]}]}, tulis

        # Barcode satuan tambahan: barang terbuka otomatis, kolom DUS fokus, tanpa baris duplikat.
        page.get_by_role("button", name="Opname baru").click()
        page.get_by_label("Cari atau scan barang").fill("BDUS102")
        page.get_by_role("button", name="Cari", exact=True).click()
        page.wait_for_timeout(200)
        expect(page.locator(".hitungan tbody tr")).to_have_count(1)
        assert page.evaluate("document.activeElement.id") == "so-fisik-102-1", page.evaluate("document.activeElement.id")

        # Dirty-guard opname: draf belum disimpan memblokir pindah tab sampai dikonfirmasi.
        dialog_jawab["aksi"] = "dismiss"
        kartu.click()
        expect(tablist.get_by_role("tab", name="Stock opname", exact=True)).to_have_attribute("aria-selected", "true")
        dialog_jawab["aksi"] = "accept"
        kartu.click()
        expect(kartu).to_have_attribute("aria-selected", "true")

        # Pengukuran numerik 3 lebar + mode gelap: tanpa overflow halaman, strip & tabel dalam viewport.
        pilih_produk_kartu(page)
        for gelap in (False, True):
            if gelap:
                page.get_by_role("button", name="Mode gelap").first.click()
            for lebar in (375, 768, 1440):
                page.set_viewport_size({"width": lebar, "height": 900})
                for nama_tab in ("Kartu stok", "Stok habis", "Stock opname", "Penyesuaian", "Valuasi & alert"):
                    tablist.get_by_role("tab", name=nama_tab, exact=True).click()
                    if nama_tab == "Kartu stok" and page.get_by_role("button", name="Ganti produk").count() == 0:
                        pilih_produk_kartu(page)
                    page.wait_for_timeout(150)
                    hasil = ukur(page)
                    assert all(hasil.values()), (gelap, lebar, nama_tab, hasil)
            if gelap:
                page.get_by_role("button", name="Mode terang").first.click()
        page.set_viewport_size({"width": 1440, "height": 900})

        # Peran lain tidak melihat Persediaan.
        for peran in ("KASIR", "ANGGOTA", "PEMBELIAN"):
            akun["role"] = peran
            halaman = browser.new_page(viewport={"width": 1440, "height": 900})
            halaman.route("**/api/v1/**", api)
            halaman.route("https://fonts.googleapis.com/**", lambda route: route.abort())
            halaman.goto(url, wait_until="networkidle")
            masuk(halaman)
            expect(halaman.get_by_role("button", name="Masuk", exact=True)).to_have_count(0)
            assert halaman.get_by_role("button", name=re.compile("^Persediaan")).count() == 0, peran
            assert halaman.get_by_role("tablist", name="Persediaan").count() == 0, peran
            halaman.close()
        browser.close()
    print("Persediaan: 5 tab keyboard, kartu+paginasi, opname cepat multi-satuan (muat ulang satuan), opname baru multi-satuan (barcode tambahan), penyesuaian+balik, valuasi+ambang, peran, dirty-guard, lebar numerik + gelap lulus.")


if __name__ == "__main__":
    test_persediaan(sys.argv[1])