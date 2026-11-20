<script lang="ts">
  import { onMount } from 'svelte';
  import { api, query } from './api';

  interface KategoriVal { kategori: string; jumlah_produk: number; total_qty: string; nilai: string; tanpa_hpp: number }
  interface Valuasi { total_nilai: string; total_produk: number; tanpa_hpp: number; jumlah_stok_minim: number; per_kategori: KategoriVal[] }
  interface StokMin { id: number; kode: string; nama: string; satuan: string; kategori: string; stok: string; stok_min: string }
  interface Barang { id: number; kode: string; nama: string; satuan: string; stok: number }

  const rp = (n: number) => new Intl.NumberFormat('id-ID', { style: 'currency', currency: 'IDR', maximumFractionDigits: 0 }).format(n || 0);
  const angka = new Intl.NumberFormat('id-ID', { maximumFractionDigits: 4 });
  const POLA_AMBANG = /^\d+(\.\d{1,4})?$/;

  let val: Valuasi | null = null, memuat = false, galat = '', pesan = '';
  let items: StokMin[] = [], jumlah = 0, cariAlert = '', moreAlert = false, pageAlert = 1;
  let ambang: Record<number, string> = {}, menyimpanId = 0;
  let cari = '', hasil: Barang[] = [], mencari = false, produk: Barang | null = null, nilaiBaru = '', galatAmbang = '';

  onMount(() => { void muat(); void muatAlert(); });

  async function muat() {
    memuat = true; galat = '';
    try { val = await api<Valuasi>('/admin/persediaan/valuasi'); }
    catch (e) { galat = (e as Error).message; }
    finally { memuat = false; }
  }

  async function muatAlert(next = false) {
    galat = '';
    try {
      const p = next ? pageAlert + 1 : 1;
      const r = await api<{ items: StokMin[]; ada_lagi: boolean; jumlah: number | null }>('/admin/persediaan/stok-minimum' + query({ q: cariAlert, page: p, size: 50 }));
      items = next ? [...items, ...r.items] : r.items;
      pageAlert = p; moreAlert = r.ada_lagi;
      if (r.jumlah != null) jumlah = r.jumlah;
      for (const it of r.items) ambang[it.id] = it.stok_min;
      ambang = { ...ambang };
    } catch (e) { galat = (e as Error).message; }
  }

  function cekAmbang(teks: string): string | null {
    const t = teks.trim();
    if (t === '') return null;
    if (!POLA_AMBANG.test(t) || Number(t) > 99999999.9999) return 'Ambang stok minimum tidak valid';
    return null;
  }

  async function simpanAmbang(id: number, teks: string) {
    if (menyimpanId) return;
    const salah = cekAmbang(teks);
    if (salah) { galat = salah; return; }
    menyimpanId = id; galat = '';
    try {
      const t = teks.trim();
      await api('/admin/persediaan/stok-min/' + id, { method: 'PUT', body: { stok_min: t === '' ? null : Number(t) } });
      pesan = t === '' ? 'Ambang dihapus (tanpa alert).' : 'Ambang stok minimum disimpan.';
      await Promise.all([muat(), muatAlert()]);
    } catch (e) { galat = (e as Error).message; }
    finally { menyimpanId = 0; }
  }

  async function cariProduk() {
    mencari = true; galatAmbang = '';
    try {
      const r = await api<{ items: Barang[] }>('/admin/stok' + query({ q: cari, page: 1, size: 20 }));
      hasil = r.items;
    } catch (e) { galatAmbang = (e as Error).message; }
    finally { mencari = false; }
  }

  function pilihProduk(b: Barang) {
    produk = b; hasil = []; cari = ''; nilaiBaru = ''; galatAmbang = '';
  }

  async function simpanAmbangBaru() {
    if (!produk || menyimpanId) return;
    const salah = cekAmbang(nilaiBaru);
    if (salah) { galatAmbang = salah; return; }
    if (nilaiBaru.trim() === '') { galatAmbang = 'Isi ambang dahulu, atau kosongkan lewat daftar alert untuk menghapus.'; return; }
    await simpanAmbang(produk.id, nilaiBaru);
    if (!galat) { produk = null; nilaiBaru = ''; }
  }
</script>

<section class="panel sedia-panel">
  <h2>Valuasi &amp; alert</h2>
  <p class="sedia-petunjuk">Nilai persediaan = stok sistem × harga beli terakhir (HPP). Ambang minimum memicu alert bila stok sistem di bawahnya.</p>
  {#if galat}<div class="error" role="alert">{galat}</div>{/if}
  {#if pesan}<div class="notice" role="status">{pesan}</div>{/if}
  {#if memuat}<p class="muted">Memuat data…</p>{/if}
  {#if val}
    <dl class="sedia-total">
      <div><dt>Total produk aktif</dt><dd>{val.total_produk}</dd></div>
      <div><dt>Produk tanpa HPP</dt><dd>{val.tanpa_hpp}</dd></div>
      <div><dt>Produk di bawah ambang minimum</dt><dd>{val.jumlah_stok_minim}</dd></div>
      <div class="sedia-total-utama"><dt>Total nilai persediaan</dt><dd>{rp(Number(val.total_nilai))}</dd></div>
    </dl>
    {#if val.tanpa_hpp > 0}<div class="sedia-peringatan" role="status">{val.tanpa_hpp} produk belum punya harga beli (HPP) sehingga dihitung Rp0. Isi harga beli agar valuasi tidak menyesatkan.</div>{/if}
    <div class="sedia-gulir"><table class="sedia-tabel">
      <thead><tr><th>Kategori</th><th class="sedia-angka">Produk</th><th class="sedia-angka">Total qty</th><th class="sedia-angka">Nilai</th><th class="sedia-angka">Tanpa HPP</th></tr></thead>
      <tbody>
        {#each val.per_kategori as k (k.kategori)}
          <tr><td>{k.kategori}</td><td class="sedia-angka">{k.jumlah_produk}</td>
            <td class="sedia-angka">{angka.format(Number(k.total_qty))}</td><td class="sedia-angka">{rp(Number(k.nilai))}</td>
            <td class="sedia-angka">{k.tanpa_hpp}</td></tr>
        {:else}
          <tr><td colspan="5" class="sedia-kosong">Belum ada produk aktif.</td></tr>
        {/each}
      </tbody>
    </table></div>
  {/if}

  <h2 style="margin-top:18px">Alert stok minimum <span class="sedia-status waspada">{jumlah}</span></h2>
  <form class="search" on:submit|preventDefault={() => muatAlert()}><input bind:value={cariAlert} placeholder="Cari produk di bawah ambang" aria-label="Cari alert stok minimum"/><button>Cari</button></form>
  <div class="sedia-gulir"><table class="sedia-tabel">
    <thead><tr><th>Produk</th><th class="sedia-angka">Stok</th><th>Ambang minimum</th><th>Tindakan</th></tr></thead>
    <tbody>
      {#each items as it (it.id)}
        <tr>
          <th scope="row">{it.nama}<small>{it.kode} · {it.kategori}</small></th>
          <td class="sedia-angka">{angka.format(Number(it.stok))} {it.satuan}</td>
          <td><span class="sedia-ambang"><input bind:value={ambang[it.id]} inputmode="decimal" aria-label="Ambang minimum {it.nama}" placeholder="kosong = tanpa alert"/></span></td>
          <td><button type="button" class="secondary" disabled={menyimpanId === it.id} on:click={() => simpanAmbang(it.id, ambang[it.id] ?? '')}>{menyimpanId === it.id ? 'Menyimpan…' : 'Simpan'}</button></td>
        </tr>
      {:else}
        <tr><td colspan="4" class="sedia-kosong">Tidak ada produk di bawah ambang minimum.</td></tr>
      {/each}
    </tbody>
  </table></div>
  {#if moreAlert}<button type="button" class="secondary" on:click={() => muatAlert(true)}>Muat lebih banyak</button>{/if}

  <h2 style="margin-top:18px">Atur ambang produk lain</h2>
  {#if !produk}
    <form class="search" on:submit|preventDefault={cariProduk}>
      <input bind:value={cari} placeholder="Cari nama, kode, atau barcode" aria-label="Cari produk untuk ambang"/>
      <button disabled={mencari}>{mencari ? 'Mencari…' : 'Cari'}</button>
    </form>
    {#if hasil.length}
      <ul class="sedia-pilih" role="listbox" aria-label="Hasil pencarian produk" style="position:static">
        {#each hasil as b (b.id)}
          <li role="option" aria-selected="false"><button type="button" on:click={() => pilihProduk(b)}>
            <strong>{b.nama}</strong><span>{b.kode} · stok {angka.format(b.stok)} {b.satuan}</span>
          </button></li>
        {/each}
      </ul>
    {/if}
  {:else}
    {#if galatAmbang}<div class="error" role="alert">{galatAmbang}</div>{/if}
    <form class="sedia-form kisi" on:submit|preventDefault={simpanAmbangBaru}>
      <label>Produk<input value="{produk.nama} ({produk.kode})" readonly/></label>
      <label>Ambang minimum<input bind:value={nilaiBaru} inputmode="decimal" placeholder="contoh: 10"/></label>
      <div class="sedia-aksi" style="grid-column:1/-1">
        <button type="button" class="secondary" on:click={() => { produk = null; nilaiBaru = ''; }}>Ganti produk</button>
        <button class="primary" disabled={!!menyimpanId}>{menyimpanId ? 'Menyimpan…' : 'Simpan ambang'}</button>
      </div>
    </form>
  {/if}
</section>