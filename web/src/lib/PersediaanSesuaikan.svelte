<script lang="ts">
  import { onMount } from 'svelte';
  import Plus from '@lucide/svelte/icons/plus';
  import Undo2 from '@lucide/svelte/icons/undo-2';
  import X from '@lucide/svelte/icons/x';
  import { api, query } from './api';
  import { validasiSesuaikan } from './persediaan';

  interface Barang { id: number; kode: string; nama: string; satuan: string; stok: number }
  interface Sesuai {
    id: number; kode: string; nama: string; tanggal: string; qty: string; alasan: string;
    user_nama: string | null; balik_id: number | null; dibalik: boolean; created_at: string;
  }
  interface Hasil { id: number; kode: string; tanggal: string; qty: string; saldo_sesudah: string; peringatan: string | null; ulang: boolean }

  const angka = new Intl.NumberFormat('id-ID', { maximumFractionDigits: 4 });
  const kini = new Date();
  const hariIni = `${kini.getFullYear()}-${String(kini.getMonth() + 1).padStart(2, '0')}-${String(kini.getDate()).padStart(2, '0')}`;
  const tanda = (s: string) => { const n = Number(s); return (n > 0 ? '+' : '') + angka.format(n); };

  let items: Sesuai[] = [], search = '', page = 1, more = false, loading = false;
  let galat = '', pesan = '', peringatan = '';
  let dialogBuat: HTMLDialogElement, dialogBalik: HTMLDialogElement;
  let buat = { produk_id: 0, qty: '', tanggal: hariIni, alasan: '' };
  let buatGalat = '', menyimpan = false, buatKey = '';
  let cari = '', hasil: Barang[] = [], mencari = false, produkDipilih: Barang | null = null;
  let balikPilihan: Sesuai | null = null, balikAlasan = '', balikGalat = '', membalik = false, balikKey = '';

  onMount(() => { void muat(); });

  async function muat(next = false) {
    loading = true; galat = '';
    try {
      const p = next ? page + 1 : 1;
      const r = await api<{ items: Sesuai[]; ada_lagi: boolean }>('/admin/persediaan/sesuaikan' + query({ q: search, page: p, size: 20 }));
      items = next ? [...items, ...r.items] : r.items;
      page = p; more = r.ada_lagi;
    } catch (e) { galat = (e as Error).message; }
    finally { loading = false; }
  }

  function bukaBuat() {
    buat = { produk_id: 0, qty: '', tanggal: hariIni, alasan: '' };
    buatGalat = ''; cari = ''; hasil = []; produkDipilih = null;
    buatKey = crypto.randomUUID();
    dialogBuat.showModal();
  }

  async function cariProduk() {
    mencari = true; buatGalat = '';
    try {
      const r = await api<{ items: Barang[] }>('/admin/stok' + query({ q: cari, page: 1, size: 20 }));
      hasil = r.items;
    } catch (e) { buatGalat = (e as Error).message; }
    finally { mencari = false; }
  }

  function pilihProduk(b: Barang) {
    produkDipilih = b; buat.produk_id = b.id; hasil = []; cari = '';
  }

  async function simpanBuat() {
    if (menyimpan) return;
    const salah = validasiSesuaikan(buat, hariIni);
    if (salah) { buatGalat = salah; return; }
    const qty = Number(buat.qty.trim());
    if (qty < 0 && produkDipilih && produkDipilih.stok + qty < 0
      && !confirm(`Stok ${produkDipilih.nama} akan menjadi ${angka.format(produkDipilih.stok + qty)} ${produkDipilih.satuan} (negatif). Lanjutkan?`)) return;
    menyimpan = true; buatGalat = '';
    try {
      const r = await api<Hasil>('/admin/persediaan/sesuaikan', {
        method: 'POST', headers: { 'Idempotency-Key': buatKey },
        body: { produk_id: buat.produk_id, qty, tanggal: buat.tanggal.trim(), alasan: buat.alasan.trim() }
      });
      dialogBuat.close();
      pesan = `Penyesuaian ${r.kode} ${tanda(r.qty)} tersimpan. Saldo sekarang ${angka.format(Number(r.saldo_sesudah))}.`;
      peringatan = r.peringatan ?? '';
      await muat();
    } catch (e) { buatGalat = (e as Error).message; }
    finally { menyimpan = false; }
  }

  function bukaBalik(a: Sesuai) {
    balikPilihan = a; balikAlasan = ''; balikGalat = '';
    balikKey = crypto.randomUUID();
    dialogBalik.showModal();
  }

  async function konfirmasiBalik() {
    if (!balikPilihan || membalik) return;
    const alasan = balikAlasan.trim();
    if (alasan.length < 3 || alasan.length > 250) { balikGalat = 'Alasan wajib diisi (3-250 karakter)'; return; }
    membalik = true; balikGalat = '';
    try {
      const r = await api<Hasil>('/admin/persediaan/sesuaikan/' + balikPilihan.id + '/balik', {
        method: 'POST', headers: { 'Idempotency-Key': balikKey }, body: { alasan }
      });
      dialogBalik.close();
      pesan = `Penyesuaian ${r.kode} dibalik (${tanda(r.qty)}). Saldo sekarang ${angka.format(Number(r.saldo_sesudah))}.`;
      peringatan = r.peringatan ?? '';
      await muat();
    } catch (e) { balikGalat = (e as Error).message; }
    finally { membalik = false; }
  }
</script>

<section class="panel sedia-panel">
  <div class="sedia-baris-judul">
    <h2>Penyesuaian stok</h2>
    <button type="button" class="primary sedia-ikon-button" on:click={bukaBuat}><Plus size={16} aria-hidden="true"/>Buat penyesuaian</button>
  </div>
  <p class="sedia-petunjuk">Penyesuaian manual berjejak: tanggal, alasan, dan operator tercatat. Efek langsung ke stok sistem; pembalikan menulis baris lawan, bukan menghapus.</p>
  {#if galat}<div class="error" role="alert">{galat}</div>{/if}
  {#if pesan}<div class="notice" role="status">{pesan}</div>{/if}
  {#if peringatan}<div class="sedia-peringatan" role="status">{peringatan}</div>{/if}
  <form class="search" on:submit|preventDefault={() => muat()}><input bind:value={search} placeholder="Cari kode atau nama produk" aria-label="Cari penyesuaian"/><button>Cari</button></form>
  {#if loading}<p class="muted">Memuat data…</p>{/if}
  <div class="sedia-gulir"><table class="sedia-tabel">
    <thead><tr><th>Produk</th><th>Tanggal</th><th class="sedia-angka">Qty</th><th>Alasan</th><th>Status</th><th>Tindakan</th></tr></thead>
    <tbody>
      {#each items as a (a.id)}
        <tr>
          <th scope="row">{a.nama}<small>{a.kode}</small></th>
          <td>{a.tanggal}<small>{a.user_nama ?? ''}</small></td>
          <td class="sedia-angka">{tanda(a.qty)}</td>
          <td>{a.alasan}</td>
          <td>{#if a.balik_id != null}<span class="sedia-status">Baris pembalik</span>
            {:else if a.dibalik}<span class="sedia-status waspada">Dibalik</span>
            {:else}<span class="sedia-status">Aktif</span>{/if}</td>
          <td><button type="button" class="secondary sedia-ikon-button" disabled={a.dibalik || a.balik_id != null} on:click={() => bukaBalik(a)}>
            <Undo2 size={14} aria-hidden="true"/>Balik</button></td>
        </tr>
      {:else}
        {#if !loading}<tr><td colspan="6" class="sedia-kosong">Belum ada penyesuaian stok.</td></tr>{/if}
      {/each}
    </tbody>
  </table></div>
  {#if more}<button type="button" class="secondary" on:click={() => muat(true)}>Muat lebih banyak</button>{/if}
</section>

<dialog class="modal sedia-dialog" bind:this={dialogBuat} aria-labelledby="sedia-buat-judul">
  <div class="sedia-baris-judul">
    <h2 id="sedia-buat-judul">Buat penyesuaian stok</h2>
    <button type="button" aria-label="Tutup" class="icon" disabled={menyimpan} on:click={() => dialogBuat.close()}><X size={20} aria-hidden="true"/></button>
  </div>
  {#if buatGalat}<div class="error" role="alert">{buatGalat}</div>{/if}
  <form class="sedia-form" on:submit|preventDefault={simpanBuat}>
    {#if !produkDipilih}
      <label>Cari produk
        <span style="display:flex;gap:6px">
          <input bind:value={cari} placeholder="Nama, kode, atau barcode" style="flex:1"/>
          <button type="button" class="secondary" disabled={mencari} on:click={cariProduk}>{mencari ? 'Mencari…' : 'Cari'}</button>
        </span>
      </label>
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
      <div class="sedia-baris-judul">
        <p style="margin:0"><strong>{produkDipilih.nama}</strong> <small class="muted">{produkDipilih.kode} · stok {angka.format(produkDipilih.stok)} {produkDipilih.satuan}</small></p>
        <button type="button" class="sedia-teks-button" on:click={() => { produkDipilih = null; buat.produk_id = 0; }}>Ganti</button>
      </div>
    {/if}
    <div class="sedia-form kisi">
      <label>Qty penyesuaian<input bind:value={buat.qty} inputmode="decimal" placeholder="contoh: -2.5" aria-invalid={buatGalat.startsWith('Qty') || undefined}/></label>
      <label>Tanggal<input type="date" bind:value={buat.tanggal} min={hariIni.slice(0, 4) + '-01-01'} max={hariIni}/></label>
    </div>
    <label>Alasan (wajib)<textarea bind:value={buat.alasan} maxlength={250} placeholder="contoh: hasil hitung ulang gudang"></textarea></label>
    <p class="sedia-petunjuk">Qty positif menambah stok, minus mengurangi. Saldo negatif diizinkan dengan peringatan.</p>
    <div class="sedia-aksi">
      <button type="button" class="secondary" disabled={menyimpan} on:click={() => dialogBuat.close()}>Batal</button>
      <button class="primary" disabled={menyimpan}>{menyimpan ? 'Menyimpan…' : 'Simpan penyesuaian'}</button>
    </div>
  </form>
</dialog>

<dialog class="modal sedia-dialog" bind:this={dialogBalik} role="alertdialog" aria-labelledby="sedia-balik-judul">
  <h2 id="sedia-balik-judul">Balik penyesuaian?</h2>
  {#if balikPilihan}
    <p><strong>{balikPilihan.nama}</strong> ({balikPilihan.kode}) · {tanda(balikPilihan.qty)} pada {balikPilihan.tanggal}.<br/>
      Baris pembalik bertanggal hari ini akan ditulis; stok kembali seperti sebelum penyesuaian.</p>
  {/if}
  {#if balikGalat}<div class="error" role="alert">{balikGalat}</div>{/if}
  <form class="sedia-form" on:submit|preventDefault={konfirmasiBalik}>
    <label>Alasan pembalikan (wajib)<textarea bind:value={balikAlasan} maxlength={250} placeholder="contoh: salah input qty"></textarea></label>
    <div class="sedia-aksi">
      <button type="button" class="secondary" disabled={membalik} on:click={() => dialogBalik.close()}>Batal</button>
      <button class="primary" disabled={membalik}>{membalik ? 'Memproses…' : 'Balik penyesuaian'}</button>
    </div>
  </form>
</dialog>