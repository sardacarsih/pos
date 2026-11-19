<script lang="ts">
  import { tick } from 'svelte';
  import Plus from '@lucide/svelte/icons/plus';
  import { api, query } from './api';
  import './pembelian.css';

  export let userId: number;
  export let online: boolean;

  type Supplier = { kode: string; nama: string | null; aktif: boolean };
  type Halaman = { items: Supplier[]; page: number; ada_lagi: boolean };
  type Dialog = { jenis: 'tambah' } | { jenis: 'ubah' | 'status'; supplier: Supplier };

  let hasil: Halaman = { items: [], page: 1, ada_lagi: false };
  let pencarian = '', status = '', filter = { q: '', aktif: '' };
  let memuat = false, tersambung = false, permintaan = 0, halamanDiminta = 1;
  let galat = '', pesan = '';
  let dialog: Dialog | undefined, dialogEl: HTMLDialogElement, pencarianEl: HTMLInputElement;
  let pemicu: HTMLElement | null = null;
  let kode = '', nama = '', galatDialog = '', menyimpan = false;

  $: if (online && !tersambung) { tersambung = true; muatUlang(); }
  $: if (!online && tersambung) {
    tersambung = false;
    permintaan++;
    memuat = false;
  }
  $: judul = dialog?.jenis === 'status'
    ? (dialog.supplier.aktif ? 'Nonaktifkan supplier?' : 'Aktifkan kembali supplier?')
    : (dialog?.jenis === 'ubah' ? 'Ubah nama supplier' : 'Tambah supplier');
  $: aksi = dialog?.jenis === 'status' ? (dialog.supplier.aktif ? 'Nonaktifkan' : 'Aktifkan kembali') : 'Simpan';

  async function muat(page: number, mundurJikaKosong = false) {
    if (!online) return;
    const urutan = ++permintaan;
    const kriteria = filter;
    halamanDiminta = page;
    memuat = true;
    galat = '';
    try {
      let data = await api<Halaman>('/supplier' + query({ ...kriteria, page }), { userId });
      if (urutan !== permintaan) return;
      if (mundurJikaKosong && page > 1 && !data.items.length) {
        data = await api<Halaman>('/supplier' + query({ ...kriteria, page: page - 1 }), { userId });
      }
      if (urutan === permintaan) {
        hasil = data;
        halamanDiminta = data.page;
      }
    } catch (e) {
      if (urutan === permintaan) galat = (e as Error).message;
    } finally {
      if (urutan === permintaan) memuat = false;
    }
  }

  function muatUlang() {
    void muat(halamanDiminta);
  }

  function cari() {
    filter = { q: pencarian.trim(), aktif: status };
    void muat(1);
  }

  async function buka(pilihan: Dialog, target: EventTarget | null) {
    if (!online || dialog) return;
    pemicu = target as HTMLElement | null;
    dialog = pilihan;
    kode = pilihan.jenis === 'tambah' ? '' : pilihan.supplier.kode;
    nama = pilihan.jenis === 'tambah' ? '' : pilihan.supplier.nama ?? '';
    galatDialog = '';
    await tick();
    dialogEl.showModal();
    dialogEl.querySelector<HTMLElement>('[data-fokus-awal]')?.focus();
  }

  function tutup(kembaliKePencarian = false) {
    dialogEl.close();
    dialog = undefined;
    const target = pemicu;
    pemicu = null;
    void tick().then(() => {
      if (!kembaliKePencarian && target?.isConnected && !target.matches(':disabled')) target.focus();
      else pencarianEl.focus();
    });
  }

  async function simpan() {
    if (!dialog || !online || menyimpan) return;
    const pilihan = dialog;
    menyimpan = true;
    galatDialog = '';
    pesan = '';
    try {
      if (pilihan.jenis === 'tambah') {
        await api('/supplier', { method: 'POST', body: { kode, nama }, userId });
        pesan = 'Supplier berhasil ditambahkan.';
        pencarian = kode.trim().toUpperCase();
        status = '';
        cari();
      } else {
        await api('/supplier/' + encodeURIComponent(pilihan.supplier.kode), {
          method: 'PATCH',
          body: pilihan.jenis === 'ubah' ? { nama } : { aktif: !pilihan.supplier.aktif },
          userId
        });
        pesan = pilihan.jenis === 'ubah'
          ? 'Nama supplier berhasil diubah.'
          : pilihan.supplier.aktif ? 'Supplier berhasil dinonaktifkan.' : 'Supplier berhasil diaktifkan kembali.';
        void muat(hasil.page, true);
      }
      tutup(true);
    } catch (e) {
      galatDialog = (e as Error).message;
    } finally {
      menyimpan = false;
    }
  }
</script>

<div class="beli">
  <section class="panel beli-panel">
    <div class="beli-baris-judul">
      <h2>Daftar supplier</h2>
      <button type="button" class="primary beli-ikon-button" disabled={!online} on:click={(e) => buka({ jenis: 'tambah' }, e.currentTarget)}>
        <Plus size={17} aria-hidden="true"/> Tambah supplier
      </button>
    </div>
    {#if !online}<p class="beli-offline" role="status">Sambungkan ke server untuk mengelola supplier.</p>{/if}
    {#if pesan}<p class="notice" role="status">{pesan}</p>{/if}

    <form class="beli-filter" on:submit|preventDefault={cari}>
      <label>Cari kode atau nama <input type="search" maxlength="100" bind:this={pencarianEl} bind:value={pencarian} /></label>
      <label>Status supplier
        <select value={status} on:change={(e) => { status = e.currentTarget.value; cari(); }} disabled={!online}>
          <option value="">Semua</option><option value="true">Aktif</option><option value="false">Nonaktif</option>
        </select>
      </label>
      <button type="submit" class="secondary" disabled={!online}>Cari</button>
    </form>

    {#if memuat}<p role="status">Memuat daftar supplier…</p>{/if}
    {#if galat && online}
      <p class="error" role="alert">{galat}</p>
      <button type="button" class="secondary" on:click={muatUlang}>Coba lagi</button>
    {:else if online}
      <div class="beli-gulir" aria-busy={memuat}>
        <table class="beli-tabel">
          <thead><tr><th scope="col">Kode</th><th scope="col">Nama supplier</th><th scope="col">Status</th><th scope="col">Tindakan</th></tr></thead>
          <tbody>
            {#each hasil.items as supplier (supplier.kode)}
              <tr>
                <th scope="row">{supplier.kode}</th>
                <td>{supplier.nama ?? '—'}</td>
                <td><span class="beli-status" class:waspada={!supplier.aktif}>{supplier.aktif ? 'Aktif' : 'Nonaktif'}</span></td>
                <td>
                  <div class="actions">
                    <button type="button" aria-label={'Ubah nama ' + supplier.kode} disabled={memuat}
                      on:click={(e) => buka({ jenis: 'ubah', supplier }, e.currentTarget)}>Ubah nama</button>
                    <button type="button" aria-label={(supplier.aktif ? 'Nonaktifkan ' : 'Aktifkan kembali ') + supplier.kode}
                      disabled={memuat} on:click={(e) => buka({ jenis: 'status', supplier }, e.currentTarget)}>
                      {supplier.aktif ? 'Nonaktifkan' : 'Aktifkan kembali'}
                    </button>
                  </div>
                </td>
              </tr>
            {:else}
              {#if !memuat}<tr><td colspan="4" class="beli-kosong">Tidak ada supplier yang cocok.</td></tr>{/if}
            {/each}
          </tbody>
        </table>
      </div>
      {#if hasil.page > 1 || hasil.ada_lagi}
        <nav class="beli-aksi" aria-label="Halaman daftar supplier">
          <button type="button" class="secondary" disabled={memuat || hasil.page === 1} on:click={() => muat(hasil.page - 1)}>Sebelumnya</button>
          <span>Halaman {hasil.page}</span>
          <button type="button" class="secondary" disabled={memuat || !hasil.ada_lagi} on:click={() => muat(hasil.page + 1)}>Berikutnya</button>
        </nav>
      {/if}
    {/if}
  </section>

  <dialog class="modal beli-dialog" bind:this={dialogEl} aria-labelledby="judul-dialog-supplier"
    on:cancel|preventDefault={() => { if (!menyimpan) tutup(); }}>
    {#if dialog}
      <h2 id="judul-dialog-supplier">{judul}</h2>
      <form class="beli-form" on:submit|preventDefault={simpan}>
        {#if dialog.jenis === 'status'}
          <p><strong>{dialog.supplier.nama ?? dialog.supplier.kode}</strong> · {dialog.supplier.kode}</p>
          {#if dialog.supplier.aktif}<p>Supplier tidak akan tersedia untuk pembelian baru. Faktur dan hutang yang sudah tercatat tetap dapat diakses.</p>{/if}
        {:else}
          <label>Kode supplier
            <input bind:value={kode} maxlength="20" autocomplete="off" required readonly={dialog.jenis === 'ubah'}
              data-fokus-awal={dialog.jenis === 'tambah' ? '' : undefined} />
          </label>
          <label>Nama supplier
            <input bind:value={nama} maxlength="50" autocomplete="off" required
              data-fokus-awal={dialog.jenis === 'ubah' ? '' : undefined} />
          </label>
        {/if}
        {#if !online}<p class="beli-offline" role="status">Sambungkan kembali untuk menyimpan.</p>{/if}
        {#if galatDialog}<p class="error" role="alert">{galatDialog}</p>{/if}
        <div class="beli-aksi">
          <button type="button" class="secondary" disabled={menyimpan} data-fokus-awal={dialog.jenis === 'status' ? '' : undefined}
            on:click={() => tutup()}>Batal</button>
          <button type="submit" class="primary" disabled={!online || menyimpan}>{menyimpan ? 'Menyimpan…' : aksi}</button>
        </div>
      </form>
    {/if}
  </dialog>
</div>
