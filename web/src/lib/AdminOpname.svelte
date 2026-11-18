<script lang="ts">
  import { onMount, tick } from 'svelte';
  import Barcode from '@lucide/svelte/icons/barcode';
  import Plus from '@lucide/svelte/icons/plus';
  import X from '@lucide/svelte/icons/x';
  import { api, query } from './api';
  import { MAKS_BARANG, bedaStok, hasilSimpanBelumPasti, jumlahFisik } from './opname';

  interface Barang {
    id: number;
    kode: string;
    nama: string;
    barcode: string | null;
    stok: number;
    satuan: string;
  }
  interface Hitungan extends Barang { qty: string }
  interface Halaman<T> { items: T[]; ada_lagi: boolean; tahun?: number }
  interface Ringkasan {
    nomor_so: string;
    tanggal: string;
    jumlah_item: number;
    nilai_selisih: number;
  }
  interface Dokumen {
    nomor_so: string;
    tanggal: string;
    items: {
      kode: string; nama: string; jumlah_sistem: number; qty_fisik: number; selisih: number;
    }[];
  }
  type Filter = 'Semua' | 'Belum dihitung' | 'Berselisih';
  type Permintaan = { key: string; items: { produk_id: number; qty_fisik: number }[] };

  const angka = new Intl.NumberFormat('id-ID', { maximumFractionDigits: 4 });
  const rupiah = new Intl.NumberFormat('id-ID', { style: 'currency', currency: 'IDR', maximumFractionDigits: 0 });
  const filters: Filter[] = ['Semua', 'Belum dihitung', 'Berselisih'];
  const tanda = (nilai: number) => (nilai > 0 ? '+' : '') + angka.format(nilai);

  let tahap: 'riwayat' | 'hitung' | 'tinjau' = 'riwayat';
  let tahun = new Date().getFullYear();
  let daftar: Ringkasan[] = [], halaman = 1, lebih = false, riwayatMemuat = true, galatRiwayat = '';
  let riwayatId = 0, detailId = 0, cariId = 0;
  let detail: Dokumen | null = null, nomorDetail = '', galatDetail = '', dialogDetail: HTMLDialogElement;
  let draf: Hitungan[] = [], filter: Filter = 'Semua', aktif: number | null = null;
  let teksCari = '', kueriHasil = '', hasil: Barang[] = [], halamanCari = 1, lebihCari = false;
  let mencari = false, sudahCari = false, galatCari = '';
  let galatHitung = '', galatSimpan = '', tersimpan = '';
  let menyimpan = false, tertunda: Permintaan | null = null;
  let kolomCari: HTMLInputElement, daftarHitung: HTMLElement, judulTinjau: HTMLElement;
  let kolomTahun: HTMLInputElement, tombolBaru: HTMLButtonElement;
  let hidup = false;

  $: dihitung = draf.filter((b) => jumlahFisik(b.qty) !== null).length;
  $: berselisih = draf.filter((b) => {
    const fisik = jumlahFisik(b.qty);
    return fisik !== null && bedaStok(fisik, b.stok) !== 0;
  }).length;

  onMount(() => {
    hidup = true;
    void muatRiwayat(false, true);
    return () => { hidup = false; };
  });

  export function bolehKeluar(): boolean {
    if (menyimpan) return false;
    if (!draf.length) return true;
    return window.confirm(tertunda
      ? 'Hasil simpan belum terkonfirmasi. Akhiri sesi dan buang draf dari layar ini?'
      : 'Akhiri sesi dan buang hitungan yang belum tersimpan?');
  }

  function sebelumTutup(event: BeforeUnloadEvent) {
    if (draf.length || menyimpan) {
      event.preventDefault();
      event.returnValue = '';
    }
  }

  async function muatRiwayat(lanjut = false, tahunServer = false) {
    const id = ++riwayatId;
    const p = lanjut ? halaman + 1 : 1;
    riwayatMemuat = true;
    galatRiwayat = '';
    if (!lanjut) { daftar = []; halaman = 1; lebih = false; }
    try {
      const r = await api<Halaman<Ringkasan>>('/admin/stok-opname' + query({
        tahun: tahunServer ? undefined : tahun, page: p, size: 50
      }));
      if (!hidup || id !== riwayatId) return;
      tahun = r.tahun ?? tahun;
      daftar = lanjut ? [...daftar, ...r.items] : r.items;
      halaman = p;
      lebih = r.ada_lagi;
    } catch (e) {
      if (hidup && id === riwayatId) galatRiwayat = (e as Error).message;
    } finally {
      if (hidup && id === riwayatId) riwayatMemuat = false;
    }
  }

  function gantiTahun() {
    const nilai = kolomTahun.valueAsNumber;
    if (!kolomTahun.reportValidity() || !Number.isInteger(nilai) || nilai === tahun) return;
    tahun = nilai;
    void muatRiwayat();
  }

  async function bukaDetail(nomor: string) {
    const id = ++detailId;
    nomorDetail = nomor;
    detail = null;
    galatDetail = '';
    if (!dialogDetail.open) dialogDetail.showModal();
    try {
      const r = await api<Dokumen>('/admin/stok-opname/' + encodeURIComponent(nomor));
      if (hidup && id === detailId) detail = r;
    } catch (e) {
      if (hidup && id === detailId) galatDetail = (e as Error).message;
    }
  }

  function resetCari(kosongkan = true) {
    ++cariId;
    if (kosongkan) teksCari = '';
    hasil = [];
    lebihCari = false;
    mencari = false;
    sudahCari = false;
    galatCari = '';
  }

  async function mulai() {
    tersimpan = '';
    galatHitung = '';
    galatSimpan = '';
    filter = 'Semua';
    tahap = 'hitung';
    await tick();
    kolomCari?.focus();
  }

  async function batal() {
    if (!bolehKeluar()) return;
    resetCari();
    draf = [];
    aktif = null;
    tertunda = null;
    tahap = 'riwayat';
    await tick();
    tombolBaru?.focus();
  }

  async function cari(lanjut = false) {
    if (lanjut && mencari) return;
    const q = lanjut ? kueriHasil : teksCari.trim();
    if (!lanjut) resetCari(false);
    if (q === '') {
      galatCari = 'Masukkan nama, kode, atau barcode barang.';
      kolomCari.focus();
      return;
    }
    const id = ++cariId;
    const p = lanjut ? halamanCari + 1 : 1;
    mencari = true;
    galatCari = '';
    try {
      const r = await api<Halaman<Barang>>('/admin/stok' + query({ q, page: p, size: 20 }));
      if (!hidup || id !== cariId) return;
      const persis = r.items.filter((b) => b.barcode === q);
      if (!lanjut && !r.ada_lagi && persis.length === 1) {
        await pilihBarang(persis[0]);
        return;
      }
      kueriHasil = q;
      halamanCari = p;
      sudahCari = true;
      hasil = lanjut ? [...hasil, ...r.items] : r.items;
      lebihCari = r.ada_lagi;
    } catch (e) {
      if (hidup && id === cariId) galatCari = (e as Error).message;
    } finally {
      if (hidup && id === cariId) mencari = false;
    }
  }

  async function fokusJumlah(id: number) {
    aktif = id;
    await tick();
    const kolom = daftarHitung?.querySelector<HTMLInputElement>('#so-fisik-' + id);
    kolom?.focus();
    kolom?.select();
  }

  async function pilihBarang(b: Barang) {
    if (!draf.some((item) => item.id === b.id)) {
      if (draf.length >= MAKS_BARANG) {
        galatHitung = 'Maksimal 200 barang per dokumen.';
        return;
      }
      draf = [{ ...b, qty: '' }, ...draf];
    }
    resetCari();
    galatHitung = '';
    filter = 'Semua';
    await fokusJumlah(b.id);
  }

  function tampil(b: Hitungan, pilihan: Filter, id: number | null) {
    if (pilihan === 'Semua' || b.id === id) return true;
    const fisik = jumlahFisik(b.qty);
    return pilihan === 'Belum dihitung' ? fisik === null : fisik !== null && bedaStok(fisik, b.stok) !== 0;
  }

  function lanjutHitung(event: KeyboardEvent, b: Hitungan) {
    if (event.key !== 'Enter' || event.isComposing) return;
    event.preventDefault();
    if (jumlahFisik(b.qty) === null) {
      galatHitung = 'Isi jumlah fisik yang valid untuk ' + b.nama + '.';
      return;
    }
    galatHitung = '';
    kolomCari.focus();
  }

  async function hapus(id: number) {
    draf = draf.filter((b) => b.id !== id);
    galatHitung = '';
    await tick();
    kolomCari?.focus();
  }

  async function tinjau() {
    const invalid = draf.find((b) => jumlahFisik(b.qty) === null);
    if (invalid) {
      galatHitung = 'Isi jumlah fisik yang valid untuk ' + invalid.nama + '.';
      filter = 'Semua';
      await fokusJumlah(invalid.id);
      return;
    }
    resetCari();
    galatSimpan = '';
    tahap = 'tinjau';
    await tick();
    judulTinjau?.focus();
  }

  async function simpan() {
    if (menyimpan || !draf.length || dihitung !== draf.length) return;
    const request = tertunda ?? {
      key: crypto.randomUUID(),
      items: draf.map((b) => ({ produk_id: b.id, qty_fisik: jumlahFisik(b.qty)! }))
    };
    menyimpan = true;
    galatSimpan = '';
    try {
      const r = await api<Dokumen>('/admin/stok-opname/dokumen', {
        method: 'POST', headers: { 'Idempotency-Key': request.key }, body: { items: request.items }
      });
      if (!hidup) return;
      if (!r.nomor_so) throw new Error('Nomor dokumen belum diterima.');
      tersimpan = r.nomor_so;
      tertunda = null;
      draf = [];
      aktif = null;
      tahap = 'riwayat';
      const tahunDokumen = Number(r.tanggal?.slice(0, 4));
      const tahunValid = tahunDokumen >= 2000 && tahunDokumen <= 2100;
      if (tahunValid) tahun = tahunDokumen;
      void muatRiwayat(false, !tahunValid);
      await tick();
      tombolBaru?.focus();
    } catch (e) {
      if (!hidup) return;
      // Setelah hasil ambigu, penolakan retry belum membuktikan hasil permintaan pertama.
      if (tertunda || hasilSimpanBelumPasti(e)) tertunda = request;
      galatSimpan = (e as Error).message;
    } finally {
      if (hidup) menyimpan = false;
    }
  }
</script>

<svelte:window on:beforeunload={sebelumTutup}/>

<section class="panel so">
  {#if tahap === 'riwayat'}
    <header class="kepala">
      <div><h2>Stock opname</h2><p>Riwayat dokumen</p></div>
      <button class="primary" bind:this={tombolBaru} on:click={mulai}><Plus size={18} aria-hidden="true"/> Opname baru</button>
    </header>
    {#if tersimpan}
      <div class="hasil-simpan">
        <span role="status">Dokumen <strong>{tersimpan}</strong> tersimpan.</span>
        <button class="secondary" on:click={() => bukaDetail(tersimpan)}>Buka dokumen</button>
      </div>
    {/if}
    <form class="tahun" on:submit|preventDefault={gantiTahun}>
      <label for="so-tahun">Tahun dokumen</label>
      <input id="so-tahun" bind:this={kolomTahun} value={tahun} type="number" min="2000" max="2100" required on:change={gantiTahun}/>
    </form>
    {#if galatRiwayat}
      <div class="error" role="alert">{galatRiwayat} <button class="secondary" on:click={() => muatRiwayat(daftar.length > 0)}>Coba lagi</button></div>
    {/if}
    {#if daftar.length}
      <table class="riwayat">
        <thead><tr><th scope="col">Nomor dokumen</th><th scope="col">Tanggal</th><th scope="col" class="angka">Barang</th><th scope="col" class="angka">Nilai selisih</th></tr></thead>
        <tbody>
          {#each daftar as s (s.nomor_so)}
            <tr>
              <th scope="row"><button class="tautan" on:click={() => bukaDetail(s.nomor_so)}>{s.nomor_so}</button></th>
              <td data-label="Tanggal">{s.tanggal}</td><td class="angka" data-label="Barang">{s.jumlah_item}</td>
              <td class="angka" data-label="Nilai selisih">{rupiah.format(s.nilai_selisih)}</td>
            </tr>
          {/each}
        </tbody>
      </table>
    {:else if !riwayatMemuat && !galatRiwayat}
      <p class="kosong">Belum ada dokumen pada tahun {tahun}.</p>
    {/if}
    {#if riwayatMemuat}<p role="status" class="status">Memuat riwayat…</p>{/if}
    {#if lebih && !galatRiwayat}
      <button class="secondary muat" disabled={riwayatMemuat} on:click={() => muatRiwayat(true)}>Muat lebih banyak</button>
    {/if}
  {:else if tahap === 'hitung'}
    <header class="kepala">
      <div><h2>Opname baru</h2><p>Daftar hitung</p></div>
      <button class="secondary" on:click={batal}>Batalkan opname</button>
    </header>
    <form class="pencarian" on:submit|preventDefault={() => cari()}>
      <label for="so-cari">Cari atau scan barang</label>
      <div class="kolom-cari">
        <Barcode size={20} aria-hidden="true"/>
        <input id="so-cari" type="text" autocomplete="off" placeholder="Nama, kode, barcode" bind:this={kolomCari}
          bind:value={teksCari} on:input={() => resetCari(false)}/>
        <button class="secondary">Cari</button>
      </div>
    </form>
    {#if mencari}<p role="status" class="status">Mencari barang…</p>{/if}
    {#if galatCari}
      <div class="error" role="alert">
        {galatCari}
        {#if teksCari.trim()}<button class="secondary" on:click={() => cari(sudahCari)}>Coba lagi</button>{/if}
      </div>
    {/if}
    {#if hasil.length}
      <ul class="hasil-cari" aria-label="Hasil pencarian">
        {#each hasil as b (b.id)}
          <li><button type="button" on:click={() => pilihBarang(b)}>
            <span><strong>{b.nama}</strong><small>{b.kode} · {angka.format(b.stok)} {b.satuan}</small></span>
            <span>{draf.some((item) => item.id === b.id) ? 'Ubah' : 'Hitung'}</span>
          </button></li>
        {/each}
      </ul>
      {#if lebihCari && !galatCari}<button class="secondary" disabled={mencari} on:click={() => cari(true)}>Muat lagi</button>{/if}
    {:else if sudahCari && !mencari && !galatCari}
      <p role="status" class="status">Barang tidak ditemukan.</p>
    {/if}
    {#if galatHitung}<p role="alert" class="error">{galatHitung}</p>{/if}
    <div class="filter" role="group" aria-label="Filter daftar hitung">
      {#each filters as opsi}
        <button type="button" aria-pressed={filter === opsi} on:click={() => { aktif = null; filter = opsi; }}>{opsi}</button>
      {/each}
    </div>
    <div bind:this={daftarHitung}>
      <table class="hitungan" aria-label="Daftar hitung">
        <thead><tr><th scope="col">Barang</th><th scope="col" class="angka">Sistem</th><th scope="col">Fisik</th><th scope="col" class="angka">Selisih</th><th scope="col"><span class="sr-only">Aksi</span></th></tr></thead>
        <tbody>
          {#each draf.filter((b) => tampil(b, filter, aktif)) as b (b.id)}
            {@const fisik = jumlahFisik(b.qty)}
            {@const beda = fisik === null ? null : bedaStok(fisik, b.stok)}
            <tr class:aktif={aktif === b.id}>
              <th scope="row">{b.nama}<small>{b.kode} · {b.satuan}</small></th>
              <td class="angka" data-label="Sistem">{angka.format(b.stok)}</td>
              <td data-label="Fisik">
                <input id={'so-fisik-' + b.id} aria-label={'Jumlah fisik ' + b.nama} inputmode="decimal" autocomplete="off"
                  aria-invalid={b.qty.trim() !== '' && fisik === null}
                  bind:value={b.qty} on:focus={() => aktif = b.id} on:blur={() => aktif = null}
                  on:input={() => galatHitung = ''} on:keydown={(e) => lanjutHitung(e, b)}/>
              </td>
              <td class="angka" data-label="Selisih"><span class:beda={beda !== null && beda !== 0}>{beda === null ? '—' : tanda(beda)}</span></td>
              <td class="aksi"><button class="ikon" aria-label={'Hapus ' + b.nama} on:click={() => hapus(b.id)}><X size={18} aria-hidden="true"/></button></td>
            </tr>
          {:else}
            <tr class="tanpa-baris"><td colspan="5" class="kosong">{draf.length ? 'Tidak ada barang pada filter ini.' : 'Scan barcode atau cari barang untuk mulai menghitung.'}</td></tr>
          {/each}
        </tbody>
      </table>
    </div>
    <footer class="bilah">
      <span><strong>{dihitung}/{draf.length}</strong> dihitung <small>{berselisih} berselisih</small></span>
      <button class="primary" disabled={!draf.length} on:click={tinjau}>Tinjau dan simpan</button>
    </footer>
  {:else}
    <header class="kepala">
      <div><h2 tabindex="-1" bind:this={judulTinjau}>Tinjau opname</h2><p>{draf.length} barang · {berselisih} berselisih</p></div>
      <button class="secondary" disabled={menyimpan || !!tertunda} on:click={mulai}>Kembali ke hitungan</button>
    </header>
    <p class="status">Selisih masih perkiraan. Menyimpan dokumen akan memperbarui stok sistem.</p>
    <table class="tinjau" aria-label="Tinjauan hasil hitung">
      <thead><tr><th scope="col">Barang</th><th scope="col" class="angka">Sistem</th><th scope="col" class="angka">Fisik</th><th scope="col" class="angka">Selisih</th></tr></thead>
      <tbody>
        {#each draf as b (b.id)}
          {@const beda = bedaStok(jumlahFisik(b.qty)!, b.stok)}
          <tr>
            <th scope="row">{b.nama}<small>{b.kode} · {b.satuan}</small></th>
            <td class="angka" data-label="Sistem">{angka.format(b.stok)}</td>
            <td class="angka" data-label="Fisik">{angka.format(jumlahFisik(b.qty)!)}</td>
            <td class="angka" data-label="Selisih"><span class:beda={beda !== 0}>{tanda(beda)}</span></td>
          </tr>
        {/each}
      </tbody>
    </table>
    <footer class="bilah">
      {#if galatSimpan}
        <div role="alert" class="error galat-simpan">
          {#if tertunda}<strong>Hasil simpan belum terkonfirmasi.</strong> Coba simpan lagi untuk memeriksa dokumen yang sama. {/if}{galatSimpan}
        </div>
      {/if}
      <button class="secondary" disabled={menyimpan} on:click={batal}>Batalkan opname</button>
      <button class="primary" disabled={menyimpan} on:click={simpan}>
        {menyimpan ? 'Menyimpan…' : tertunda ? 'Coba simpan lagi' : 'Simpan dokumen'}
      </button>
    </footer>
  {/if}
</section>

<dialog class="so-detail" aria-labelledby="so-nomor" bind:this={dialogDetail} on:close={() => { ++detailId; }}>
  <header class="kepala">
    <div><h2 id="so-nomor">{nomorDetail}</h2>{#if detail}<p>{detail.tanggal} · {detail.items.length} barang</p>{/if}</div>
    <button class="ikon" aria-label="Tutup detail" on:click={() => dialogDetail.close()}><X size={22} aria-hidden="true"/></button>
  </header>
  {#if detail}
    <table class="tinjau" aria-label="Hasil stock opname">
      <thead><tr><th scope="col">Barang</th><th scope="col" class="angka">Sistem</th><th scope="col" class="angka">Fisik</th><th scope="col" class="angka">Selisih</th></tr></thead>
      <tbody>
        {#each detail.items as b}
          <tr><th scope="row">{b.nama}<small>{b.kode}</small></th>
            <td class="angka" data-label="Sistem">{angka.format(b.jumlah_sistem)}</td>
            <td class="angka" data-label="Fisik">{angka.format(b.qty_fisik)}</td>
            <td class="angka" data-label="Selisih"><span class:beda={b.selisih !== 0}>{tanda(b.selisih)}</span></td>
          </tr>
        {/each}
      </tbody>
    </table>
  {:else if galatDetail}
    <div class="error" role="alert">{galatDetail} <button class="secondary" on:click={() => bukaDetail(nomorDetail)}>Coba lagi</button></div>
  {:else}
    <p role="status">Memuat dokumen…</p>
  {/if}
</dialog>

<style>
  .so, .so-detail {
    --so-ink: #1e2b22;
    --so-muted: #506759;
    --so-paper: #fff;
    --so-subtle: #f1f7f0;
    --so-line: #dce7dc;
    --so-accent: #156333;
    --so-beda: #8d4c08;
    --so-focus: #19733c;
    --so-overlay: #071f1399;
    color: var(--so-ink);
  }
  :global(.dark) .so, :global(.dark) .so-detail {
    --so-ink: #e6f5ea;
    --so-muted: #adc3b2;
    --so-paper: #172a21;
    --so-subtle: #223b2f;
    --so-line: #3a5747;
    --so-accent: #a5e4b6;
    --so-beda: #f2c47d;
    --so-focus: #a5e4b6;
  }
  .so { padding: clamp(12px, 3vw, 26px); }
  .so-detail { width: min(92vw, 720px); max-height: 86dvh; border: 1px solid var(--so-line); border-radius: 16px; background: var(--so-paper); padding: clamp(12px, 3vw, 24px); }
  .so-detail::backdrop { background: var(--so-overlay); }
  h2, th, td { overflow-wrap: anywhere; }
  .kepala, .hasil-simpan, .bilah { display: flex; flex-wrap: wrap; justify-content: space-between; align-items: center; gap: 12px; }
  .kepala { margin-bottom: 18px; }
  .kepala p, small, .status, .kosong { color: var(--so-muted); }
  .kepala p { margin: 4px 0 0; }
  button { min-height: 44px; white-space: nowrap; }
  button:focus-visible { outline: 2px solid var(--so-focus); outline-offset: 2px; }
  .primary { display: inline-flex; align-items: center; justify-content: center; gap: 6px; }
  .secondary { color: var(--so-accent); background: var(--so-subtle); }
  .hasil-simpan { background: var(--so-subtle); border: 1px solid var(--so-line); border-radius: 10px; padding: 8px 12px; margin-bottom: 16px; }
  .tahun, .pencarian { margin: 0 0 14px; }
  label { margin: 0 0 6px; }
  .tahun input { max-width: 10rem; }
  .kolom-cari { display: flex; align-items: center; gap: 8px; }
  .kolom-cari input { min-width: 0; }
  .kolom-cari :global(svg) { color: var(--so-accent); flex: none; }
  .hasil-cari { list-style: none; padding: 0; margin: 0 0 10px; border: 1px solid var(--so-line); border-radius: 10px; max-height: 18rem; overflow: auto; }
  .hasil-cari li + li { border-top: 1px solid var(--so-line); }
  .hasil-cari button { display: flex; align-items: center; justify-content: space-between; gap: 8px; text-align: left; white-space: normal; width: 100%; padding: 9px 12px; }
  .hasil-cari button span:last-child, .tautan { color: var(--so-accent); font-weight: 700; }
  .hasil-cari button:hover, tr.aktif { background: var(--so-subtle); }
  .filter { display: flex; flex-wrap: wrap; gap: 6px; margin: 16px 0 10px; }
  .filter button { padding: 6px 10px; border: 1px solid var(--so-line); border-radius: 8px; }
  .filter button[aria-pressed='true'] { border-color: var(--so-accent); color: var(--so-accent); background: var(--so-subtle); font-weight: 700; }
  table { border-collapse: collapse; width: 100%; text-align: left; }
  th, td { padding: 8px 10px; border-bottom: 1px solid var(--so-line); }
  thead { background: var(--so-subtle); font-size: .82rem; }
  td, th[scope='row'] { font-size: .92rem; }
  .angka, .hitungan input, .bilah strong { font-variant-numeric: tabular-nums; }
  .angka { text-align: right; }
  td input { min-width: 0; width: min(100%, 8rem); text-align: right; padding: 10px; }
  input[aria-invalid='true'] { border-color: var(--so-beda); }
  .hitungan th:nth-child(3) { width: 9rem; }
  .aksi { width: 44px; }
  .ikon { min-width: 44px; display: inline-grid; place-items: center; border-radius: 8px; }
  .ikon:hover { background: var(--so-subtle); }
  .tautan { padding: 0; text-align: left; text-decoration: underline; text-underline-offset: .2em; }
  .beda { color: var(--so-beda); font-weight: 700; }
  .kosong { padding: 22px 8px; text-align: center; }
  .status { margin: 12px 0; }
  .muat { margin-top: 12px; }
  .bilah { position: sticky; bottom: 0; margin-top: 12px; padding: 10px 0; border-top: 1px solid var(--so-line); background: var(--so-paper); }
  .galat-simpan { flex: 0 0 100%; margin: 0; }
  @media (max-width: 650px) {
    .bilah { bottom: calc(64px + env(safe-area-inset-bottom)); }
    .filter button { font-size: .84rem; }
    .kolom-cari :global(svg) { display: none; }
  }
  @media (max-width: 720px) {
    table, tbody { display: block; }
    thead { position: absolute; width: 1px; height: 1px; overflow: hidden; clip-path: inset(50%); }
    tr { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); border-bottom: 1px solid var(--so-line); align-items: start; gap: 8px; padding: 10px 2px; }
    th, td { padding: 0; border: 0; }
    th[scope='row'] { grid-column: 1 / -1; }
    td[data-label]::before { content: attr(data-label); display: block; color: var(--so-muted); font-size: .75rem; }
    td.angka, td input { text-align: left; }
    .hitungan tr:not(.tanpa-baris) { grid-template-columns: minmax(0, 1fr) minmax(0, 1.25fr) minmax(0, 1fr) 44px; }
    .hitungan th[scope='row'] { grid-column: 1 / 4; grid-row: 1; }
    .aksi { grid-column: 4; grid-row: 1 / 3; }
    .tanpa-baris td { grid-column: 1 / -1; }
    .tautan { min-height: 44px; }
  }
</style>
