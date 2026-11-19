<script lang="ts">
  import { onMount } from 'svelte';
  import { api, query } from './api';

  interface Item {
    id: number;
    kode: string;
    nama: string;
    kategori: string;
    satuan: string;
    stok: string;
    hpp: string;
    nilai: string;
    tanpa_hpp: boolean;
  }

  interface Halaman { items: Item[]; ada_lagi: boolean; page: number }

  const angka = new Intl.NumberFormat('id-ID', { maximumFractionDigits: 4 });
  const rupiah = new Intl.NumberFormat('id-ID', {
    style: 'currency', currency: 'IDR', minimumFractionDigits: 0, maximumFractionDigits: 4
  });

  let cari = '', pencarian = '', galat = '';
  let items: Item[] = [], page = 0, adaLagi = false, memuat = false;

  onMount(() => { void muat(1); });

  function cariItem() {
    if (memuat) return;
    pencarian = cari.trim();
    items = [];
    page = 0;
    adaLagi = false;
    void muat(1);
  }

  async function muat(tujuan: number) {
    if (memuat) return;
    memuat = true;
    galat = '';
    try {
      const hasil = await api<Halaman>('/admin/persediaan/valuasi-item' + query({ q: pencarian, page: tujuan, size: 50 }));
      items = tujuan === 1 ? hasil.items : [...items, ...hasil.items];
      page = hasil.page;
      adaLagi = hasil.ada_lagi;
    } catch (e) {
      galat = (e as Error).message;
    } finally {
      memuat = false;
    }
  }
</script>

<section class="panel sedia-panel">
  <h2>Valuasi Item</h2>
  <p class="sedia-petunjuk">Nilai persediaan = stok sistem × harga beli terakhir (HPP).</p>
  <form class="search" on:submit|preventDefault={cariItem}>
    <input bind:value={cari} placeholder="Cari nama, kode, atau barcode" aria-label="Cari item valuasi" />
    <button disabled={memuat}>Cari</button>
  </form>

  {#if memuat}<p class="muted" role="status">Memuat data…</p>{/if}
  {#if galat}<div class="error" role="alert">{galat}</div>{/if}

  {#if page > 0}
    <div class="sedia-gulir">
      <table class="sedia-tabel" aria-label="Valuasi per item">
        <thead>
          <tr>
            <th scope="col">Produk</th>
            <th scope="col">Kategori</th>
            <th scope="col" class="sedia-angka">Stok sistem</th>
            <th scope="col" class="sedia-angka">HPP per unit</th>
            <th scope="col" class="sedia-angka">Nilai persediaan</th>
          </tr>
        </thead>
        <tbody>
          {#each items as item (item.id)}
            <tr>
              <th scope="row">{item.nama || item.kode}{#if item.nama && item.kode}<small>{item.kode}</small>{/if}</th>
              <td>{item.kategori}</td>
              <td class="sedia-angka">{angka.format(Number(item.stok))} {item.satuan}</td>
              <td class="sedia-angka">
                {#if item.tanpa_hpp}<span class="sedia-status waspada">Tanpa HPP</span>
                {:else}{rupiah.format(Number(item.hpp))}{/if}
              </td>
              <td class="sedia-angka">{rupiah.format(Number(item.nilai))}</td>
            </tr>
          {:else}
            <tr><td colspan="5" class="sedia-kosong">{pencarian ? 'Tidak ada produk yang cocok.' : 'Belum ada produk aktif.'}</td></tr>
          {/each}
        </tbody>
      </table>
    </div>
  {/if}

  {#if adaLagi || galat}
    <button type="button" class="secondary" disabled={memuat} on:click={() => muat(page + 1)}>
      {galat ? 'Coba lagi' : 'Muat lebih banyak'}
    </button>
  {/if}
</section>
