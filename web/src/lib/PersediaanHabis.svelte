<script lang="ts">
  import { onMount } from 'svelte';
  import X from '@lucide/svelte/icons/x';
  import { api, query } from './api';
  import type { Data } from './types';
  import { bedaStok, kolomHitungan, payloadHitungan, statusHitungan } from './opname';
  import type { KolomHitung, SatuanTambahan } from './opname';

  type BarangStok = { satuan: string; satuan_tambahan?: SatuanTambahan[] };

  const angka = new Intl.NumberFormat('id-ID', { maximumFractionDigits: 4 });

  let products: Data[] = [], search = '', page = 1, more = false, loading = false;
  let error = '', message = '', stokTotal = 0, kontrolStok = true;
  let opname: Data | null = null, kolom: KolomHitung[] = [], saving = false, opnameKey = '', memuatSatuan = false;

  $: st = statusHitungan(kolom);
  $: beda = opname && st.nilai !== null ? bedaStok(st.nilai, Number(opname.stok)) : null;

  onMount(() => { void load(); });

  async function load(next = false) {
    loading = true; error = '';
    try {
      const p = next ? page + 1 : 1;
      const result = await api<Data>('/admin/stok-habis' + query({ q: search, page: p, size: 100, multi_satuan: 1 }));
      products = next ? [...products, ...result.items] : result.items;
      page = p; more = result.ada_lagi;
      if (result.jumlah != null) stokTotal = result.jumlah;
      kontrolStok = result.kontrol_stok !== false;
    } catch (e) { error = (e as Error).message; }
    finally { loading = false; }
  }

  function openOpname(p: Data) {
    opname = p;
    kolom = kolomHitungan(p as unknown as BarangStok);
    error = ''; message = '';
    opnameKey = crypto.randomUUID();
  }

  async function muatUlangSatuan() {
    if (!opname || memuatSatuan) return;
    memuatSatuan = true; error = '';
    try {
      const r = await api<Data>('/admin/stok-habis' + query({ q: opname.kode, size: 100, multi_satuan: 1 }));
      const baru = (r.items as Data[]).find((x) => x.id === opname!.id);
      if (!baru) throw new Error('Produk ' + opname.nama + ' tidak ditemukan saat memuat ulang satuan.');
      const lama = new Map(kolom.map((k) => [k.satuan.trim().toUpperCase(), k.qty]));
      kolom = kolomHitungan(baru as unknown as BarangStok, lama);
      opname = { ...opname, satuan_tambahan: baru.satuan_tambahan };
      message = 'Satuan produk dimuat ulang. Periksa hitungan lalu simpan kembali.';
    } catch (e) { error = (e as Error).message; }
    finally { memuatSatuan = false; }
  }

  async function saveOpname() {
    if (!opname || saving) return;
    if (st.galat) { error = st.galat; return; }
    if (st.nilai === null) { error = 'Isi jumlah fisik; kolom kosong semua berarti belum dihitung. Isi 0 untuk stok fisik kosong.'; return; }
    const o = opname, total = st.nilai;
    if (!confirm('Koreksi stok ' + o.nama + '?\nStok sistem ' + o.stok + ' → stok fisik ' + total + ' ' + o.satuan + ' (selisih ' + beda + ').\nDokumen SO tidak dapat dibatalkan dari web.')) return;
    saving = true; error = ''; message = '';
    try {
      const r = await api<Data>('/admin/stok-opname', { method: 'POST', headers: { 'Idempotency-Key': opnameKey }, body: { produk_id: o.id, hitungan: payloadHitungan(kolom) } });
      opname = null;
      message = 'Opname ' + r.nomor_so + ' tersimpan (selisih ' + r.selisih + ').';
      await load();
    } catch (e) { error = (e as Error).message; }
    finally { saving = false; }
  }

  function escapeHabis(e: KeyboardEvent) {
    if (e.key === 'Escape' && opname && !saving) opname = null;
  }
</script>

<svelte:window on:keydown={escapeHabis} />
<section class="panel sedia-panel">
  <h2>Stok habis</h2>
  {#if error && !opname}<div class="error" role="alert">{error}</div>{/if}
  {#if message}<div class="notice">{message}</div>{/if}
  {#if loading}<p class="muted">Memuat data…</p>{/if}
  <form class="search" on:submit|preventDefault={() => load()}><input bind:value={search} placeholder="Cari produk" aria-label="Cari produk"/><button>Cari</button></form>
  <p class="muted">{stokTotal} produk aktif dengan stok sistem ≤ 0. Isi hasil hitung fisik untuk mengoreksi stok.</p>
  {#if !kontrolStok}<div class="notice">Kontrol stok di kasir nonaktif (KONTROL_SALDO_QTY = 0): daftar ini dapat memuat barang yang memang tidak dilacak stoknya.</div>{/if}
  {#each products as p (p.id)}
    <div class="admin-row"><div><strong>{p.nama}</strong><small>{p.kode} · {p.kategori} · stok {p.stok} {p.satuan}</small></div>
      <div class="actions"><button on:click={() => openOpname(p)}>Opname</button></div></div>
  {:else}
    {#if !loading}<p class="muted">Tidak ada barang dengan stok habis.</p>{/if}
  {/each}
  {#if more}<button class="secondary" on:click={() => load(true)}>Muat lebih banyak</button>{/if}
</section>
{#if opname}
  <div class="overlay"><div class="modal" role="dialog" aria-modal="true" aria-labelledby="judul-opname">
    <div class="row"><h2 id="judul-opname">Opname stok</h2><button aria-label="Tutup" class="icon" on:click={() => opname = null}><X size={20} aria-hidden="true"/></button></div>
    {#if error}
      <div class="error" role="alert">
        {error}
        {#if error.includes('muat ulang satuan')}
          <button class="secondary" disabled={memuatSatuan} on:click={muatUlangSatuan}>{memuatSatuan ? 'Memuat satuan…' : 'Muat ulang satuan produk'}</button>
        {/if}
      </div>
    {/if}
    {#if message}<div class="notice" role="status">{message}</div>{/if}
    <p><strong>{opname.nama}</strong><br/><small>{opname.kode} · stok sistem {opname.stok} {opname.satuan}</small></p>
    <form on:submit|preventDefault={saveOpname}>
      {#each kolom as k (k.satuan)}
        <label>{k.satuan}{#if k.isi !== 1} <small>(1 {k.satuan} = {angka.format(k.isi)} {opname.satuan})</small>{/if}
          <input bind:value={k.qty} inputmode="decimal" autocomplete="off" placeholder="0" aria-label={'Jumlah ' + k.satuan + ' ' + opname.nama}/>
        </label>
      {/each}
      {#if st.galat}<p class="error" role="alert">{st.galat}</p>{/if}
      {#if st.nilai !== null}
        <p class="muted">Total fisik <strong>{angka.format(st.nilai)} {opname.satuan}</strong> · selisih <strong>{beda !== null && beda > 0 ? '+' : ''}{angka.format(beda ?? 0)}</strong></p>
      {:else if !st.galat}
        <p class="muted">Kolom kosong dilewati. Isi 0 untuk menyatakan stok fisik kosong.</p>
      {/if}
      <button class="primary" disabled={saving || st.nilai === null}>{saving ? 'Menyimpan…' : 'Simpan opname'}</button>
    </form>
  </div></div>
{/if}