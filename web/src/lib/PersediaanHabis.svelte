<script lang="ts">
  import { onMount } from 'svelte';
  import X from '@lucide/svelte/icons/x';
  import { api, query } from './api';
  import type { Data } from './types';

  interface ProdukStok {
    id: number;
    nama: string;
    kode: string;
    kategori: string;
    stok: string;
    satuan: string;
  }

  interface HalamanStok {
    items: ProdukStok[];
    ada_lagi: boolean;
    jumlah: number | null;
    kontrol_stok: boolean;
  }

  const angka = new Intl.NumberFormat('id-ID', { maximumFractionDigits: 4 });
  let products: ProdukStok[] = [], search = '', pencarian = '', page = 1, more = false, loading = false;
  let error = '', message = '', stokTotal = 0, kontrolStok = true;
  let opname: ProdukStok | null = null, qty = '', saving = false, opnameKey = '';

  onMount(() => { void load(); });

  async function load(next = false, q = pencarian) {
    if (loading) return;
    loading = true; error = '';
    try {
      const p = next ? page + 1 : 1;
      const result = await api<HalamanStok>('/admin/stok-habis' + query({ q, page: p, size: 100, urut: 'qty' }));
      products = next ? [...products, ...result.items] : result.items;
      page = p; more = result.ada_lagi;
      pencarian = q;
      if (result.jumlah != null) stokTotal = result.jumlah;
      kontrolStok = result.kontrol_stok !== false;
    } catch (e) { error = (e as Error).message; }
    finally { loading = false; }
  }

  function openOpname(p: ProdukStok) { opname = p; qty = ''; error = ''; message = ''; opnameKey = crypto.randomUUID(); }

  async function saveOpname() {
    if (!opname || saving) return;
    const n = Number(qty);
    if (qty.trim() === '' || !Number.isFinite(n) || n < 0) { error = 'Qty fisik tidak valid.'; return; }
    const o = opname;
    if (!confirm('Koreksi stok ' + o.nama + '?\nStok sistem ' + o.stok + ' → stok fisik ' + n + ' (selisih ' + (n - Number(o.stok)) + ').\nDokumen SO tidak dapat dibatalkan dari web.')) return;
    saving = true; error = '';
    try {
      const r = await api<Data>('/admin/stok-opname', { method: 'POST', headers: { 'Idempotency-Key': opnameKey }, body: { produk_id: o.id, qty_fisik: n } });
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
  <form class="search" on:submit|preventDefault={() => load(false, search.trim())}>
    <input bind:value={search} placeholder="Cari produk" aria-label="Cari produk" />
    <button disabled={loading || saving}>Cari</button>
  </form>
  <p class="muted">{stokTotal} produk aktif dengan stok sistem ≤ 0. Isi hasil hitung fisik untuk mengoreksi stok.</p>
  {#if !kontrolStok}<div class="notice">Kontrol stok di kasir nonaktif (KONTROL_SALDO_QTY = 0): daftar ini dapat memuat barang yang memang tidak dilacak stoknya.</div>{/if}
  <div class="sedia-gulir">
    <table class="sedia-tabel" aria-label="Daftar stok habis">
      <thead>
        <tr>
          <th scope="col">Produk</th>
          <th scope="col">Kategori</th>
          <th scope="col" class="sedia-angka" aria-sort="ascending">Qty Stok</th>
          <th scope="col">Tindakan</th>
        </tr>
      </thead>
      <tbody>
        {#each products as p (p.id)}
          <tr>
            <th scope="row">{p.nama}<small>{p.kode}</small></th>
            <td>{p.kategori}</td>
            <td class="sedia-angka">{angka.format(Number(p.stok))} {p.satuan}</td>
            <td><button type="button" disabled={loading || saving} on:click={() => openOpname(p)}>Opname</button></td>
          </tr>
        {:else}
          {#if !loading && !error}<tr><td colspan="4" class="sedia-kosong">Tidak ada barang dengan stok habis.</td></tr>{/if}
        {/each}
      </tbody>
    </table>
  </div>
  {#if more}<button type="button" class="secondary" disabled={loading || saving} on:click={() => load(true)}>Muat lebih banyak</button>{/if}
</section>
{#if opname}<div class="overlay"><div class="modal" role="dialog" aria-modal="true" aria-labelledby="judul-opname"><div class="row"><h2 id="judul-opname">Opname stok</h2><button aria-label="Tutup" class="icon" on:click={() => opname = null}><X size={20} aria-hidden="true"/></button></div>{#if error}<div class="error">{error}</div>{/if}<p><strong>{opname.nama}</strong><br/><small>{opname.kode} · stok sistem {opname.stok} {opname.satuan}</small></p><form on:submit|preventDefault={saveOpname}><label>Qty fisik<input bind:value={qty} inputmode="decimal" required placeholder="0"/></label><button class="primary" disabled={saving}>{saving ? 'Menyimpan…' : 'Simpan opname'}</button></form></div></div>{/if}
