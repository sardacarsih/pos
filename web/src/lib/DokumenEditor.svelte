<script lang="ts">
  import { createEventDispatcher, onDestroy } from 'svelte';
  import X from '@lucide/svelte/icons/x';
  import { api, query } from './api';
  import { jumlahFisik } from './opname';
  import { bolehSimpan, kunciBaru, type BarisEdit } from './dokumenEdit';

  /// Editor daftar barang untuk Ubah Opname dan Barang Rusak (baru/ubah). Jumlah dalam satuan dasar produk.
  /// `simpan` melempar Error bila ditolak; kunci idempotensi dipakai ulang selama isi tidak berubah.
  export let judul: string;
  export let labelQty: string;
  export let awal: BarisEdit[] = [];
  export let denganKeterangan = false;
  export let perluAlasan = true;
  export let izinNol = true;
  export let simpan: (baris: BarisEdit[], alasan: string, key: string) => Promise<void>;

  interface Barang { id: number; kode: string; nama: string; stok: number; satuan: string }
  const dispatch = createEventDispatcher<{ selesai: void; batal: void }>();
  const angka = new Intl.NumberFormat('id-ID', { maximumFractionDigits: 4 });

  let baris: BarisEdit[] = awal.map((b) => ({ ...b }));
  let teksCari = '', hasil: Barang[] = [], mencari = false, galat = '', alasan = '';
  let menyimpan = false, cariId = 0, timer: ReturnType<typeof setTimeout>;
  let kunci: string = crypto.randomUUID(), sidikTerakhir = '';
  onDestroy(() => clearTimeout(timer));

  function cari() {
    clearTimeout(timer);
    const q = teksCari.trim();
    if (!q) { hasil = []; return; }
    timer = setTimeout(async () => {
      const id = ++cariId;
      mencari = true;
      try {
        const r = await api<{ items: Barang[] }>('/admin/stok' + query({ q, size: 10 }));
        if (id === cariId) hasil = r.items;
      } catch (e) {
        if (id === cariId) galat = (e as Error).message;
      } finally {
        if (id === cariId) mencari = false;
      }
    }, 350);
  }

  function tambah(b: Barang) {
    if (baris.some((x) => x.produk_id === b.id)) { galat = b.nama + ' sudah ada di daftar.'; return; }
    baris = [...baris, { produk_id: b.id, kode: b.kode, nama: b.nama, satuan: b.satuan, qty: '', keterangan: '' }];
    hasil = []; teksCari = ''; galat = '';
  }

  function hapus(id: number) { baris = baris.filter((b) => b.produk_id !== id); }

  async function kirim() {
    const masalah = bolehSimpan(baris, alasan, perluAlasan, izinNol);
    if (masalah) { galat = masalah; return; }
    const sidik = JSON.stringify([alasan.trim(), baris.map((b) => [b.produk_id, b.qty.trim(), b.keterangan.trim()])]);
    [kunci, sidikTerakhir] = kunciBaru(kunci, sidikTerakhir, sidik, () => crypto.randomUUID());
    menyimpan = true;
    galat = '';
    try {
      await simpan(baris, alasan.trim(), kunci);
      dispatch('selesai');
    } catch (e) {
      galat = (e as Error).message;
    } finally {
      menyimpan = false;
    }
  }
</script>

<section class="editor" aria-label={judul}>
  <header class="kepala">
    <h2>{judul}</h2>
    <button class="secondary" disabled={menyimpan} on:click={() => dispatch('batal')}>Batal</button>
  </header>

  <form class="cari" on:submit|preventDefault={cari}>
    <label for="de-cari">Tambah barang (nama, kode, atau barcode)</label>
    <input id="de-cari" type="text" autocomplete="off" bind:value={teksCari} on:input={cari}/>
  </form>
  {#if mencari}<p role="status" class="status">Mencari barang…</p>{/if}
  {#if hasil.length}
    <ul class="hasil" aria-label="Hasil pencarian">
      {#each hasil as b (b.id)}
        <li><button type="button" on:click={() => tambah(b)}>
          <span><strong>{b.nama}</strong><small>{b.kode} · stok {angka.format(b.stok)} {b.satuan}</small></span><span>Tambah</span>
        </button></li>
      {/each}
    </ul>
  {/if}

  <table aria-label="Daftar barang">
    <thead><tr><th scope="col">Barang</th><th scope="col">{labelQty}</th>{#if denganKeterangan}<th scope="col">Keterangan</th>{/if}<th scope="col"><span class="sr-only">Aksi</span></th></tr></thead>
    <tbody>
      {#each baris as b (b.produk_id)}
        <tr>
          <th scope="row">{b.nama}<small>{b.kode}{b.satuan ? ' · ' + b.satuan : ''}</small></th>
          <td><input inputmode="decimal" autocomplete="off" aria-label={labelQty + ' ' + b.nama}
            aria-invalid={b.qty.trim() !== '' && jumlahFisik(b.qty) === null} bind:value={b.qty}/></td>
          {#if denganKeterangan}<td><input maxlength="100" aria-label={'Keterangan ' + b.nama} bind:value={b.keterangan}/></td>{/if}
          <td><button class="ikon" aria-label={'Hapus ' + b.nama} on:click={() => hapus(b.produk_id)}><X size={18} aria-hidden="true"/></button></td>
        </tr>
      {:else}
        <tr><td colspan={denganKeterangan ? 4 : 3} class="kosong">Cari barang di atas untuk menambah.</td></tr>
      {/each}
    </tbody>
  </table>

  {#if perluAlasan}
    <label for="de-alasan">Alasan perubahan</label>
    <input id="de-alasan" maxlength="250" bind:value={alasan}/>
  {/if}
  {#if galat}<p class="error" role="alert">{galat}</p>{/if}
  <footer class="bilah">
    <button class="primary" disabled={menyimpan} on:click={kirim}>{menyimpan ? 'Menyimpan…' : 'Simpan'}</button>
  </footer>
</section>

<style>
  .editor { display: grid; gap: 10px; }
  .kepala, .bilah { display: flex; flex-wrap: wrap; justify-content: space-between; align-items: center; gap: 12px; }
  .hasil { list-style: none; margin: 0; padding: 0; border: 1px solid #dce7dc; border-radius: 10px; max-height: 14rem; overflow: auto; }
  .hasil li + li { border-top: 1px solid #dce7dc; }
  .hasil button { display: flex; justify-content: space-between; gap: 8px; width: 100%; text-align: left; padding: 9px 12px; min-height: 44px; }
  table { border-collapse: collapse; width: 100%; text-align: left; }
  th, td { padding: 8px 10px; border-bottom: 1px solid #dce7dc; overflow-wrap: anywhere; }
  small { display: block; color: #506759; }
  td input { width: min(100%, 10rem); }
  input[aria-invalid='true'] { border-color: #8d4c08; }
  .ikon { min-width: 44px; min-height: 44px; }
  .kosong { text-align: center; padding: 22px 8px; color: #506759; }
  .sr-only { position: absolute; width: 1px; height: 1px; overflow: hidden; clip-path: inset(50%); }
</style>
