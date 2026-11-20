<script lang="ts">
  import { tick } from 'svelte';
  import ArrowLeftRight from '@lucide/svelte/icons/arrow-left-right';
  import Download from '@lucide/svelte/icons/download';
  import Landmark from '@lucide/svelte/icons/landmark';
  import NotebookText from '@lucide/svelte/icons/notebook-text';
  import { api, ApiError, query } from './api';
  import { rpBeli } from './pembelian';
  import { bisaDibatal, cekMutasi, labelSumber, permintaanMutasi, type Akun, type Buku, type IsianMutasi, type JenisAkun, type Mutasi } from './kas';
  import type { User } from './types';
  import './pembelian.css';

  export let user: User;

  const tabKas = [
    { id: 'buku', label: 'Buku Kas / Bank', ikon: NotebookText },
    { id: 'mutasi', label: 'Catat Mutasi', ikon: ArrowLeftRight },
    { id: 'akun', label: 'Akun', ikon: Landmark },
    { id: 'tarik', label: 'Tarik Transaksi', ikon: Download },
  ] as const;
  type TabKas = (typeof tabKas)[number]['id'];
  let tab: TabKas = 'buku';

  function pilihTab(tujuan: TabKas) {
    tab = tujuan;
    document.getElementById(`kas-tab-${tujuan}`)?.focus();
  }

  function tombolTab(event: KeyboardEvent, index: number) {
    const arah = event.key === 'ArrowLeft' ? -1 : event.key === 'ArrowRight' ? 1 : 0;
    if (!arah && event.key !== 'Home' && event.key !== 'End') return;
    event.preventDefault();
    const tujuan = event.key === 'Home' ? 0 : event.key === 'End' ? tabKas.length - 1 : (index + arah + tabKas.length) % tabKas.length;
    pilihTab(tabKas[tujuan].id);
  }

  const hariIni = () => { const d = new Date(); return new Date(d.getTime() - d.getTimezoneOffset() * 60000).toISOString().slice(0, 10); };
  const awalBulan = () => hariIni().slice(0, 8) + '01';

  let akun: Akun[] = [];
  let akunDimuat = false;
  $: akunAktif = akun.filter(a => a.aktif);
  let galat = '';
  let pesan = '';
  let sibuk = false;

  // Buku
  let pilihAkun: number | '' = '';
  let dari = awalBulan();
  let sampai = hariIni();
  type FilterBuku = { akun: number | ''; dari: string; sampai: string };
  let buku: (Buku & { filter: FilterBuku }) | undefined;
  const filterIsian = (): FilterBuku => ({ akun: pilihAkun, dari, sampai });

  // Input mutasi; kunci idempotensi dipakai ulang sampai berhasil agar kirim ulang tidak dobel.
  const isianBaru = (): IsianMutasi => ({ jenis: 'M', akun: pilihAkun, ke: '', tanggal: hariIni(), nominal: undefined, keterangan: '' });
  let isian = isianBaru();
  let kunci = crypto.randomUUID();

  // Akun
  type FormAkun = { id?: number; kode: string; nama: string; jenis: JenisAkun; no_rekening: string; atas_nama: string; saldo_awal: number; tgl_saldo_awal: string; default: boolean; aktif: boolean };
  let form: FormAkun | null = null;
  let kodeAkun: HTMLInputElement;
  let tombolFormAkun: HTMLButtonElement | null = null;

  // Tarik
  let tarikDari = awalBulan();
  let tarikSampai = hariIni();

  async function jalan(f: () => Promise<void>) {
    if (sibuk) return;
    sibuk = true; galat = ''; pesan = '';
    try { await f(); } catch (e) { galat = (e as Error).message; } finally { sibuk = false; }
  }

  async function muatAkun() {
    akun = (await api<{ akun: Akun[] }>('/kas/akun', { userId: user.id })).akun;
    if (!pilihAkun && akun.length) pilihAkun = (akun.find(a => a.default_untuk === 'KAS') ?? akun[0]).id;
    akunDimuat = true;
  }

  async function muatBuku(filter = buku?.filter ?? filterIsian()) {
    if (!filter.akun) { buku = undefined; return; }
    const hasil = await api<Buku>('/kas/buku' + query(filter), { userId: user.id });
    buku = { ...hasil, filter };
  }

  async function segarkan() {
    await muatAkun();
    await muatBuku();
  }

  function tampilkanBuku() {
    void jalan(async () => {
      if (!akunDimuat) await muatAkun();
      await muatBuku(filterIsian());
    });
  }

  void jalan(segarkan);

  function simpanMutasi() {
    const salah = cekMutasi(isian, hariIni());
    if (salah) { galat = salah; return; }
    const { path, body } = permintaanMutasi(isian);
    void jalan(async () => {
      try { await api(path, { method: 'POST', body, headers: { 'Idempotency-Key': kunci }, userId: user.id }); }
      catch (e) { if (e instanceof ApiError && [400, 404, 409, 422].includes(e.status)) kunci = crypto.randomUUID(); throw e; }
      kunci = crypto.randomUUID();
      isian = isianBaru();
      pesan = 'Mutasi dicatat.';
      await segarkan();
    });
  }

  function batal(m: Mutasi) {
    const alasan = prompt(`Batalkan mutasi #${m.id} (${m.keterangan})?\nAlasan:`)?.trim();
    if (!alasan) return;
    void jalan(async () => {
      await api(`/kas/mutasi/${m.id}/batal`, { method: 'POST', body: { alasan }, headers: { 'Idempotency-Key': crypto.randomUUID() }, userId: user.id });
      pesan = m.sumber === 'TRANSFER' ? 'Transfer dibatalkan (kedua sisi).' : 'Mutasi dibatalkan.';
      await segarkan();
    });
  }

  function tarik() {
    void jalan(async () => {
      const r = await api<Record<string, number>>('/kas/tarik', { method: 'POST', body: { dari: tarikDari, sampai: tarikSampai }, userId: user.id });
      pesan = `Tarik selesai: penjualan ${r.penjualan}, bayar supplier ${r.beli_bayar}, pinjaman ${r.pinjaman} mutasi baru/koreksi.`;
      await segarkan();
    });
  }

  async function ubah(event: MouseEvent, a?: Akun) {
    tombolFormAkun = event.currentTarget as HTMLButtonElement;
    form = a
      ? { id: a.id, kode: a.kode, nama: a.nama, jenis: a.jenis, no_rekening: a.no_rekening ?? '', atas_nama: a.atas_nama ?? '', saldo_awal: a.saldo_awal, tgl_saldo_awal: a.tgl_saldo_awal, default: a.default_untuk !== null, aktif: a.aktif }
      : { kode: '', nama: '', jenis: 'KAS', no_rekening: '', atas_nama: '', saldo_awal: 0, tgl_saldo_awal: awalBulan(), default: false, aktif: true };
    await tick();
    if (tab === 'akun') kodeAkun?.focus();
  }

  function tutupFormAkun() {
    form = null;
    if (tab === 'akun') tombolFormAkun?.focus();
    tombolFormAkun = null;
  }

  async function simpanAkun() {
    const f = form;
    if (!f || sibuk) return;
    if (!f.kode.trim() || !f.nama.trim() || !f.tgl_saldo_awal || !Number.isSafeInteger(f.saldo_awal)) { galat = 'Kode, nama, saldo awal, dan tanggal saldo awal wajib.'; return; }
    const body = { kode: f.kode, nama: f.nama, jenis: f.jenis, no_rekening: f.no_rekening || null, atas_nama: f.atas_nama || null,
      saldo_awal: f.saldo_awal, tgl_saldo_awal: f.tgl_saldo_awal, default_untuk: f.default ? f.jenis : null, aktif: f.aktif };
    await jalan(async () => {
      await api(f.id ? `/kas/akun/${f.id}` : '/kas/akun', { method: f.id ? 'PUT' : 'POST', body, userId: user.id });
      form = null;
      pesan = 'Akun disimpan.';
      await segarkan();
    });
    if (!form) {
      await tick();
      tutupFormAkun();
    }
  }
</script>

<div class="beli">
  <div class="beli-tab" role="tablist" aria-label="Kas &amp; Bank">
    {#each tabKas as t, i (t.id)}
      <button type="button" id={'kas-tab-' + t.id} role="tab" aria-selected={tab === t.id} aria-controls={'kas-panel-' + t.id}
        tabindex={tab === t.id ? 0 : -1} on:keydown={(event) => tombolTab(event, i)} on:click={() => pilihTab(t.id)}>
        <svelte:component this={t.ikon} size={16} aria-hidden="true" />{t.label}
      </button>
    {/each}
  </div>

  {#if galat}<p class="error" role="alert">{galat}</p>{/if}
  {#if pesan}<p class="success" role="status">{pesan}</p>{/if}

  <div class="panel beli-panel" id="kas-panel-akun" role="tabpanel" aria-labelledby="kas-tab-akun" hidden={tab !== 'akun'}>
    <header class="beli-baris-judul">
      <h2>Akun kas &amp; bank</h2>
      <button class="primary" type="button" disabled={sibuk} on:click={(event) => ubah(event)}>Tambah akun</button>
    </header>
    {#if form}
      <form class="beli-form kisi kas-form" aria-label={form.id ? 'Ubah akun' : 'Tambah akun'} on:submit|preventDefault={simpanAkun}>
        <label>Kode <input maxlength="20" required bind:this={kodeAkun} bind:value={form.kode} /></label>
        <label>Nama <input maxlength="100" required bind:value={form.nama} /></label>
        <label>Jenis <select bind:value={form.jenis}><option value="KAS">KAS</option><option value="BANK">BANK</option></select></label>
        <label>No. rekening <input maxlength="50" bind:value={form.no_rekening} /></label>
        <label>Atas nama <input maxlength="100" bind:value={form.atas_nama} /></label>
        <label>Saldo awal <input type="number" step="1" required bind:value={form.saldo_awal} /></label>
        <label>Per tanggal <input type="date" required bind:value={form.tgl_saldo_awal} /></label>
        <label class="check"><input type="checkbox" bind:checked={form.default} /> Default untuk {form.jenis === 'KAS' ? 'transaksi tunai' : 'transfer'}</label>
        <label class="check"><input type="checkbox" bind:checked={form.aktif} /> Aktif</label>
        <div class="kas-form-actions">
          <button class="primary" type="submit" disabled={sibuk}>Simpan</button>
          <button class="secondary" type="button" disabled={sibuk} on:click={tutupFormAkun}>Batal</button>
        </div>
      </form>
    {/if}
    {#if akunDimuat}
      <!-- svelte-ignore a11y_no_noninteractive_tabindex (Area tabel dapat digulir dengan keyboard.) -->
      <div class="beli-gulir" role="region" aria-label="Daftar akun" tabindex="0"><table class="beli-tabel">
        <thead><tr><th>Kode</th><th>Nama</th><th>Jenis</th><th>Rekening</th><th class="beli-angka">Saldo</th><th></th></tr></thead>
        <tbody>{#each akun as a (a.id)}
          <tr><th scope="row">{a.kode}</th>
            <td>{a.nama}{#if a.default_untuk} <span class="beli-status">default</span>{/if}{#if !a.aktif} <span class="beli-status gagal">nonaktif</span>{/if}</td>
            <td>{a.jenis}</td><td>{a.no_rekening ?? '—'}</td><td class="beli-angka">{rpBeli(a.saldo)}</td>
            <td><button class="secondary" type="button" aria-label={'Ubah ' + a.nama} disabled={sibuk} on:click={(event) => ubah(event, a)}>Ubah</button></td></tr>
        {:else}<tr><td class="beli-kosong" colspan="6">Belum ada akun. Tambahkan akun KAS dan BANK, lalu tandai masing-masing sebagai default.</td></tr>{/each}</tbody>
      </table></div>
    {:else if sibuk}
      <p class="kas-muted" role="status">Memuat akun…</p>
    {/if}
  </div>

  <div class="panel beli-panel" id="kas-panel-tarik" role="tabpanel" aria-labelledby="kas-tab-tarik" hidden={tab !== 'tarik'}>
    <h2>Tarik transaksi dari modul lain</h2>
    <p class="kas-muted">Menyalin penjualan tunai (per hari), pembayaran supplier, dan pencairan pinjaman ke akun default. Penarikan dapat diulang karena hanya selisih yang dicatat, termasuk koreksi bila transaksi asal diubah atau dihapus.</p>
    <form class="beli-filter kas-filter" on:submit|preventDefault={tarik}>
      <label>Dari <input type="date" required max={tarikSampai} bind:value={tarikDari} /></label>
      <label>Sampai <input type="date" required min={tarikDari} max={hariIni()} bind:value={tarikSampai} /></label>
      <button class="primary" type="submit" disabled={sibuk}>Tarik transaksi</button>
    </form>
  </div>

  <div class="panel beli-panel" id="kas-panel-mutasi" role="tabpanel" aria-labelledby="kas-tab-mutasi" hidden={tab !== 'mutasi'}>
    <h2>Catat mutasi</h2>
    <form class="beli-form kisi kas-form" on:submit|preventDefault={simpanMutasi}>
      <label>Jenis <select bind:value={isian.jenis}><option value="M">Kas masuk</option><option value="K">Kas keluar</option><option value="TRANSFER">Transfer antar akun</option></select></label>
      <label>{isian.jenis === 'TRANSFER' ? 'Dari akun' : 'Akun'} <select bind:value={isian.akun}>
        <option value="">Pilih</option>{#each akunAktif as a (a.id)}<option value={a.id}>{a.kode} — {a.nama}</option>{/each}</select></label>
      {#if isian.jenis === 'TRANSFER'}
        <label>Ke akun <select bind:value={isian.ke}><option value="">Pilih</option>{#each akunAktif as a (a.id)}<option value={a.id}>{a.kode} — {a.nama}</option>{/each}</select></label>
      {/if}
      <label>Tanggal <input type="date" required max={hariIni()} bind:value={isian.tanggal} /></label>
      <label>Nominal <input type="number" min="1" step="1" required bind:value={isian.nominal} /></label>
      <label class="kas-field-wide">Keterangan <input maxlength="250" required bind:value={isian.keterangan} /></label>
      <div class="kas-form-actions"><button class="primary" type="submit" disabled={sibuk}>Simpan</button></div>
    </form>
  </div>

  <div class="panel beli-panel" id="kas-panel-buku" role="tabpanel" aria-labelledby="kas-tab-buku" hidden={tab !== 'buku'}>
    <h2>Buku kas / bank</h2>
    <form class="beli-filter kas-filter" on:submit|preventDefault={tampilkanBuku}>
      <label>Akun <select bind:value={pilihAkun} disabled={!akun.length}>{#each akun as a (a.id)}<option value={a.id}>{a.kode} — {a.nama}</option>{:else}<option value="">Belum ada akun</option>{/each}</select></label>
      <label>Dari <input type="date" required bind:value={dari} /></label>
      <label>Sampai <input type="date" required min={dari} bind:value={sampai} /></label>
      <button class="primary" type="submit" disabled={sibuk || (akunDimuat && !akun.length)}>Tampilkan</button>
    </form>
    {#if buku}
      <!-- svelte-ignore a11y_no_noninteractive_tabindex (Area tabel dapat digulir dengan keyboard.) -->
      <div class="beli-gulir" role="region" aria-label="Rincian buku kas dan bank" tabindex="0"><table class="beli-tabel" aria-busy={sibuk}>
        <caption>{akun.find(a => a.id === buku?.filter.akun)?.nama} · {buku.filter.dari} s.d. {buku.filter.sampai}</caption>
        <thead><tr><th>Tanggal</th><th>Keterangan</th><th>Sumber</th><th class="beli-angka">Masuk</th><th class="beli-angka">Keluar</th><th class="beli-angka">Saldo</th><th></th></tr></thead>
        <tbody>
          <tr><td>{buku.filter.dari}</td><td colspan="4"><strong>Saldo awal</strong></td><td class="beli-angka">{rpBeli(buku.saldo_awal)}</td><td></td></tr>
          {#each buku.mutasi as m (m.id)}
            <tr><td>{m.tanggal}</td><td>{m.keterangan}{#if m.user}<small> · {m.user}</small>{/if}</td>
              <td>{labelSumber[m.sumber]}{#if m.ref}<small> {m.ref}</small>{/if}</td>
              <td class="beli-angka">{m.masuk ? rpBeli(m.masuk) : ''}</td><td class="beli-angka">{m.keluar ? rpBeli(m.keluar) : ''}</td>
              <td class="beli-angka">{rpBeli(m.saldo)}</td>
              <td>{#if bisaDibatal(m)}<button class="secondary" type="button" disabled={sibuk} on:click={() => batal(m)}>Batal</button>{:else if m.dibalik}<span class="beli-status gagal">dibatalkan</span>{/if}</td></tr>
          {:else}<tr><td class="beli-kosong" colspan="7">Belum ada mutasi pada periode ini.</td></tr>{/each}
        </tbody>
        <tfoot><tr><th colspan="5" scope="row">Saldo akhir</th><td class="beli-angka"><strong>{rpBeli(buku.saldo_akhir)}</strong></td><td></td></tr></tfoot>
      </table></div>
    {:else if sibuk}
      <p class="kas-muted" role="status">Memuat buku kas / bank…</p>
    {:else if akunDimuat && !akun.length}
      <p class="kas-muted">Belum ada akun. <button class="beli-teks-button" type="button" on:click={() => pilihTab('akun')}>Buka tab Akun</button> untuk menambah akun.</p>
    {/if}
  </div>
</div>

<style>
  /* Hallmark · macrostructure: Workbench · tone: operasional · anchor hue: hijau POS */
  .beli-tab button { border-bottom: 3px solid transparent; }
  .beli-tab button[aria-selected='true'] { border-bottom-color: var(--beli-focus); }
  .beli-tab button:hover { background: var(--beli-soft); }
  .beli-tab button:focus-visible { outline-offset: -3px; }
  .beli-gulir:focus-visible { outline: 2px solid var(--beli-focus); outline-offset: 2px; }
  .beli > :is(.error, .success) { margin: 0; }
  .success { padding: 12px 16px; border-radius: 10px; background: var(--beli-soft); color: var(--beli-focus); font-size: .875rem; }
  .kas-muted, caption { color: var(--beli-muted); font-size: .875rem; }
  .kas-muted { max-width: 68ch; }
  .kas-filter, .kas-form { margin-top: 16px; max-width: 760px; }
  .kas-filter :is(input, select, button) { min-height: 44px; }
  .kas-form-actions, .kas-field-wide { grid-column: 1 / -1; }
  .kas-form-actions { display: flex; gap: 8px; flex-wrap: wrap; }
  .kas-form label.check { min-height: 44px; }
  .kas-form label.check input { min-height: auto; }
  caption { padding: 8px 0; text-align: left; }
  tfoot { background: var(--beli-soft); }
  @media (max-width: 560px) {
    .kas-form.kisi { grid-template-columns: minmax(0, 1fr); }
    .kas-filter { display: grid; grid-template-columns: minmax(0, 1fr); }
    .kas-filter button { justify-self: start; }
  }
</style>
