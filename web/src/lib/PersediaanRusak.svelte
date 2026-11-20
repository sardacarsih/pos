<script lang="ts">
  import { onMount } from 'svelte';
  import Plus from '@lucide/svelte/icons/plus';
  import X from '@lucide/svelte/icons/x';
  import { api, query } from './api';
  import DokumenEditor from './DokumenEditor.svelte';
  import { teksQty, type BarisEdit } from './dokumenEdit';

  interface Ringkasan { nomor_so: string; tanggal: string; jumlah_item: number; nilai: number }
  interface Dokumen {
    nomor_so: string; tanggal: string; versi: string; total: number;
    items: { produk_id: number | null; kode: string; nama: string; qty: number; hpp: number; keterangan: string }[];
  }

  const angka = new Intl.NumberFormat('id-ID', { maximumFractionDigits: 4 });
  const rupiah = new Intl.NumberFormat('id-ID', { style: 'currency', currency: 'IDR', maximumFractionDigits: 0 });

  let tahun = new Date().getFullYear();
  let daftar: Ringkasan[] = [], halaman = 1, lebih = false, memuat = true, galat = '', riwayatId = 0, hidup = false;
  let mode: 'riwayat' | 'baru' = 'riwayat';
  let detail: Dokumen | null = null, nomorDetail = '', galatDetail = '', dialog: HTMLDialogElement, mengubah = false, detailId = 0;
  let menghapus = false, alasanHapus = '', galatHapus = '', prosesHapus = false, kunciHapus = '', sidikHapus = '';

  onMount(() => { hidup = true; void muat(); return () => { hidup = false; }; });

  async function muat(lanjut = false) {
    const id = ++riwayatId;
    const p = lanjut ? halaman + 1 : 1;
    memuat = true; galat = '';
    if (!lanjut) { daftar = []; halaman = 1; lebih = false; }
    try {
      const r = await api<{ items: Ringkasan[]; ada_lagi: boolean; tahun?: number }>('/admin/barang-rusak' + query({ tahun, page: p, size: 50 }));
      if (!hidup || id !== riwayatId) return;
      daftar = lanjut ? [...daftar, ...r.items] : r.items;
      halaman = p; lebih = r.ada_lagi;
    } catch (e) {
      if (hidup && id === riwayatId) galat = (e as Error).message;
    } finally {
      if (hidup && id === riwayatId) memuat = false;
    }
  }

  async function buka(nomor: string) {
    const id = ++detailId;
    nomorDetail = nomor; detail = null; galatDetail = '';
    mengubah = false; menghapus = false; alasanHapus = ''; galatHapus = '';
    if (!dialog.open) dialog.showModal();
    try {
      const r = await api<Dokumen>('/admin/barang-rusak/' + encodeURIComponent(nomor));
      if (hidup && id === detailId) detail = r;
    } catch (e) {
      if (hidup && id === detailId) galatDetail = (e as Error).message;
    }
  }

  const barisUbah = (d: Dokumen): BarisEdit[] =>
    d.items.map((b) => ({ produk_id: b.produk_id ?? 0, kode: b.kode, nama: b.nama, satuan: '', qty: teksQty(b.qty), keterangan: b.keterangan }));

  const bodyBaris = (baris: BarisEdit[]) =>
    baris.map((b) => ({ produk_id: b.produk_id, qty: b.qty.trim().replace(',', '.'), keterangan: b.keterangan.trim() }));

  async function simpanBaru(baris: BarisEdit[], _alasan: string, key: string) {
    await api('/admin/barang-rusak', { method: 'POST', headers: { 'Idempotency-Key': key }, body: { items: bodyBaris(baris) } });
  }

  async function simpanUbah(baris: BarisEdit[], alasan: string, key: string) {
    if (!detail) return;
    await api('/admin/barang-rusak/' + encodeURIComponent(detail.nomor_so), {
      method: 'PUT', headers: { 'Idempotency-Key': key }, body: { versi: detail.versi, alasan, items: bodyBaris(baris) }
    });
  }

  async function selesaiBaru() { mode = 'riwayat'; await muat(); }
  async function selesaiUbah() { mengubah = false; void muat(); await buka(nomorDetail); }

  async function hapusDokumen() {
    if (!detail || prosesHapus) return;
    if (!alasanHapus.trim()) { galatHapus = 'Alasan wajib diisi.'; return; }
    const sidik = detail.versi + '|' + alasanHapus.trim();
    if (sidik !== sidikHapus) { kunciHapus = crypto.randomUUID(); sidikHapus = sidik; }
    prosesHapus = true; galatHapus = '';
    try {
      await api('/admin/barang-rusak/' + encodeURIComponent(detail.nomor_so), {
        method: 'DELETE', headers: { 'Idempotency-Key': kunciHapus }, body: { versi: detail.versi, alasan: alasanHapus.trim() }
      });
      dialog.close();
      void muat();
    } catch (e) {
      galatHapus = (e as Error).message;
    } finally {
      prosesHapus = false;
    }
  }

  function gantiTahun(e: Event) {
    const n = (e.currentTarget as HTMLInputElement).valueAsNumber;
    if (!Number.isInteger(n) || n < 2000 || n > 2100 || n === tahun) return;
    tahun = n; void muat();
  }
</script>

<section class="panel rusak">
  {#if mode === 'baru'}
    <DokumenEditor judul="Barang rusak baru" labelQty="Jumlah rusak (satuan dasar)" awal={[]} denganKeterangan perluAlasan={false} izinNol={false}
      simpan={simpanBaru} on:selesai={selesaiBaru} on:batal={() => (mode = 'riwayat')}/>
  {:else}
    <header class="kepala">
      <div><h2>Barang rusak</h2><p>Riwayat dokumen; jumlah rusak mengurangi stok sistem</p></div>
      <button class="primary" on:click={() => (mode = 'baru')}><Plus size={18} aria-hidden="true"/> Rusak baru</button>
    </header>
    <label for="rusak-tahun">Tahun dokumen</label>
    <input id="rusak-tahun" type="number" min="2000" max="2100" value={tahun} on:change={gantiTahun}/>
    {#if galat}<div class="error" role="alert">{galat} <button class="secondary" on:click={() => muat(daftar.length > 0)}>Coba lagi</button></div>{/if}
    {#if daftar.length}
      <table aria-label="Riwayat barang rusak">
        <thead><tr><th scope="col">Nomor dokumen</th><th scope="col">Tanggal</th><th scope="col">Barang</th><th scope="col">Nilai</th></tr></thead>
        <tbody>
          {#each daftar as s (s.nomor_so)}
            <tr>
              <th scope="row"><button class="tautan" on:click={() => buka(s.nomor_so)}>{s.nomor_so}</button></th>
              <td>{s.tanggal}</td><td>{s.jumlah_item}</td><td>{rupiah.format(s.nilai)}</td>
            </tr>
          {/each}
        </tbody>
      </table>
    {:else if !memuat && !galat}
      <p class="kosong">Belum ada dokumen barang rusak pada tahun {tahun}.</p>
    {/if}
    {#if memuat}<p role="status">Memuat riwayat…</p>{/if}
    {#if lebih && !galat}<button class="secondary" disabled={memuat} on:click={() => muat(true)}>Muat lebih banyak</button>{/if}
  {/if}
</section>

<dialog class="rusak-detail" aria-labelledby="rusak-nomor" bind:this={dialog} on:close={() => { ++detailId; }}>
  <header class="kepala">
    <div><h2 id="rusak-nomor">{nomorDetail}</h2>{#if detail}<p>{detail.tanggal} · {detail.items.length} barang · {rupiah.format(detail.total)}</p>{/if}</div>
    <button class="ikon" aria-label="Tutup detail" on:click={() => dialog.close()}><X size={22} aria-hidden="true"/></button>
  </header>
  {#if detail && mengubah}
    <DokumenEditor judul={'Ubah ' + detail.nomor_so} labelQty="Jumlah rusak (satuan dasar)" awal={barisUbah(detail)} denganKeterangan izinNol={false}
      simpan={simpanUbah} on:selesai={selesaiUbah} on:batal={() => (mengubah = false)}/>
  {:else if detail}
    <div class="aksi-dokumen">
      <button class="secondary" disabled={detail.items.some((b) => b.produk_id === null)} on:click={() => (mengubah = true)}>Ubah dokumen</button>
      <button class="secondary" on:click={() => { menghapus = !menghapus; galatHapus = ''; }}>Hapus dokumen</button>
    </div>
    {#if menghapus}
      <div class="hapus-dokumen">
        <p>Dokumen dihapus permanen dan stok sistem bertambah kembali.</p>
        <label for="rusak-alasan-hapus">Alasan</label>
        <input id="rusak-alasan-hapus" maxlength="250" bind:value={alasanHapus}/>
        {#if galatHapus}<p class="error" role="alert">{galatHapus}</p>{/if}
        <button class="primary" disabled={prosesHapus} on:click={hapusDokumen}>{prosesHapus ? 'Menghapus…' : 'Hapus permanen'}</button>
      </div>
    {/if}
    <table aria-label="Isi dokumen barang rusak">
      <thead><tr><th scope="col">Barang</th><th scope="col">Rusak</th><th scope="col">HPP</th><th scope="col">Keterangan</th></tr></thead>
      <tbody>
        {#each detail.items as b}
          <tr><th scope="row">{b.nama}<small>{b.kode}</small></th><td>{angka.format(b.qty)}</td><td>{rupiah.format(b.hpp)}</td><td>{b.keterangan}</td></tr>
        {/each}
      </tbody>
    </table>
  {:else if galatDetail}
    <div class="error" role="alert">{galatDetail} <button class="secondary" on:click={() => buka(nomorDetail)}>Coba lagi</button></div>
  {:else}
    <p role="status">Memuat dokumen…</p>
  {/if}
</dialog>

<style>
  .rusak { padding: clamp(12px, 3vw, 26px); display: grid; gap: 12px; }
  .rusak-detail { width: min(92vw, 720px); max-height: 86dvh; border-radius: 16px; padding: clamp(12px, 3vw, 24px); }
  .kepala { display: flex; flex-wrap: wrap; justify-content: space-between; align-items: center; gap: 12px; }
  .kepala p { margin: 4px 0 0; opacity: .75; }
  .primary { display: inline-flex; align-items: center; gap: 6px; }
  button { min-height: 44px; }
  table { border-collapse: collapse; width: 100%; text-align: left; }
  th, td { padding: 8px 10px; border-bottom: 1px solid #dce7dc; overflow-wrap: anywhere; }
  small { display: block; opacity: .75; }
  .tautan { padding: 0; text-decoration: underline; text-underline-offset: .2em; font-weight: 700; }
  .aksi-dokumen { display: flex; flex-wrap: wrap; gap: 8px; margin: 12px 0; }
  .hapus-dokumen { display: grid; gap: 8px; margin-bottom: 12px; padding: 12px; border: 1px solid #dce7dc; border-radius: 10px; }
  .kosong { padding: 22px 8px; text-align: center; opacity: .75; }
  .ikon { min-width: 44px; }
</style>
