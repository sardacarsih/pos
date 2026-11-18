<script lang="ts">
  import { tick } from 'svelte';
  import { api, ApiError, query } from './api';
  import { formatJumlahBeli, labelFaktur, metodeBayar, rpBeli, type DetailBeli, type FakturBeli,
    type Metode, type Pembayaran, type SaldoSupplier } from './pembelian';
  import type { User } from './types';
  import { inputAngkaPembelian } from './inputAngkaPembelian';
  import './pembelian.css';

  export let user: User;
  export let online: boolean;
  export let hutang = false;
  export let nomorAwal = '';
  type Daftar = { items: FakturBeli[]; ada_lagi: boolean; page: number; supplier?: SaldoSupplier[] };
  let daftar: FakturBeli[] = [];
  let supplier: SaldoSupplier[] = [];
  let pencarian = '';
  let dari = '';
  let sampai = '';
  let kodeSupplier = '';
  let terlambat = false;
  let page = 1;
  let lebih = false;
  let loading = false;
  let galat = '';
  let pilihan: DetailBeli | undefined;
  let dialog: HTMLDialogElement;
  let aksiDialog: HTMLDialogElement;
  let kembaliKe: HTMLElement | null = null;
  let galatDetail = '';
  let galatAksi = '';
  let pesanDetail = '';
  let aksi: 'bayar' | 'balik' | 'batal' = 'bayar';
  let balikBayar: Pembayaran | undefined;
  let sibuk = false;
  let nominal: number | undefined;
  let sesiAksi = 0;
  let tanggalBayar = '';
  let metode: Metode = 'TUNAI';
  let referensi = '';
  let alasan = '';
  let versiDaftar = 0;
  let versiDetail = 0;
  let sudahBuka = '';
  let filter = { q: '', supplier: '', dari: '', sampai: '', terlambat: '' };

  $: if (online) void muat(1);
  $: if (online && nomorAwal && nomorAwal !== sudahBuka && dialog) {
    sudahBuka = nomorAwal;
    void detail(nomorAwal);
  }

  async function muat(hal: number) {
    if (!online) return;
    const urutan = ++versiDaftar;
    loading = true;
    galat = '';
    try {
      const r = await api<Daftar>((hutang ? '/hutang-supplier' : '/pembelian') +
        query({ ...filter, page: hal }), { userId: user.id });
      if (urutan !== versiDaftar) return;
      daftar = hal === 1 ? r.items : [...daftar, ...r.items];
      page = hal;
      lebih = r.ada_lagi;
      supplier = r.supplier ?? [];
    } catch (e) { if (urutan === versiDaftar) galat = (e as Error).message; }
    finally { if (urutan === versiDaftar) loading = false; }
  }

  function cari() {
    filter = { q: pencarian, supplier: kodeSupplier, dari, sampai, terlambat: terlambat ? 'true' : '' };
    void muat(1);
  }

  async function detail(no: string, buka = true) {
    const urutan = ++versiDetail;
    if (buka) {
      kembaliKe = document.activeElement as HTMLElement | null;
      pilihan = undefined;
      pesanDetail = '';
      await tick();
      if (!dialog.open) dialog.showModal();
    }
    galatDetail = '';
    try {
      const data = await api<DetailBeli>('/pembelian/' + encodeURIComponent(no), { userId: user.id });
      if (urutan === versiDetail) pilihan = data;
    } catch (e) { if (urutan === versiDetail) galatDetail = (e as Error).message; }
  }

  function tutupDetail() {
    ++versiDetail;
    kembaliKe?.focus();
    kembaliKe = null;
  }

  function mulaiAksi(mode: typeof aksi, p?: Pembayaran) {
    if (!pilihan || !online) return;
    aksi = mode;
    sesiAksi += 1;
    balikBayar = p;
    nominal = pilihan.sisa ?? undefined;
    tanggalBayar = pilihan.tanggal_server;
    metode = 'TUNAI';
    referensi = '';
    alasan = '';
    galatAksi = '';
    aksiDialog.showModal();
  }

  async function simpanAksi() {
    if (!pilihan || !online || sibuk) return;
    const no = pilihan.no_transaksi;
    let path: string;
    let body: object;
    if (aksi === 'bayar') {
      if (!Number.isSafeInteger(nominal) || !nominal || nominal <= 0 || nominal > (pilihan.sisa ?? 0) || !tanggalBayar) {
        galatAksi = 'Periksa tanggal dan nominal pembayaran.';
        return;
      }
      path = '/hutang-supplier/' + encodeURIComponent(no) + '/pembayaran';
      body = { tanggal: tanggalBayar, metode, referensi: referensi.trim() || null, nominal };
    } else {
      if (!alasan.trim()) { galatAksi = 'Alasan wajib diisi.'; return; }
      path = aksi === 'balik'
        ? '/hutang-supplier/' + encodeURIComponent(no) + '/pembayaran/' + balikBayar!.id + '/balik'
        : '/pembelian/' + encodeURIComponent(no) + '/batal';
      body = { alasan: alasan.trim() };
    }

    sibuk = true;
    galatAksi = '';
    let simpanKey: string | undefined;
    try {
      // Retry a lost response using the same key, including after a page reload.
      const bytes = new TextEncoder().encode(path + JSON.stringify(body));
      const digest = new Uint8Array(await crypto.subtle.digest('SHA-256', bytes));
      simpanKey = 'kopkar-beli-aksi-' + user.id + '-' + Array.from(digest, b => b.toString(16).padStart(2, '0')).join('');
      const key = sessionStorage.getItem(simpanKey) ?? crypto.randomUUID();
      sessionStorage.setItem(simpanKey, key);
      await api(path, { method: 'POST', body, headers: { 'Idempotency-Key': key }, userId: user.id });
      sessionStorage.removeItem(simpanKey);
      aksiDialog.close();
      pesanDetail = aksi === 'bayar' ? 'Pembayaran dicatat.' : aksi === 'balik' ? 'Pembayaran dibalik.' : 'Faktur dibatalkan.';
      await Promise.all([detail(no, false), muat(1)]);
    } catch (e) {
      if (e instanceof ApiError && [400, 404, 409, 422].includes(e.status)) {
        if (simpanKey) sessionStorage.removeItem(simpanKey);
        galatAksi = e.message;
        await detail(no, false);
      } else if (e instanceof ApiError && (e.status === 0 || e.status >= 500)) {
        galatAksi = 'Hasil belum terkonfirmasi. Coba kirim ulang dengan isian yang sama.';
      } else galatAksi = (e as Error).message;
    } finally { sibuk = false; }
  }
</script>

<div class="beli">
  {#if !online}
    <section class="panel beli-panel"><h2>{hutang ? 'Saldo hutang supplier' : 'Riwayat pembelian'}</h2>
      <p>Sambungkan ke server untuk melihat data terbaru.</p>
    </section>
  {:else}
    {#if hutang && supplier.length}
      <section class="panel beli-panel">
        <h2>Saldo per supplier</h2>
        <div class="beli-gulir"><table class="beli-tabel">
          <thead><tr><th>Supplier</th><th class="beli-angka">Faktur terbuka</th><th class="beli-angka">Sisa hutang</th><th class="beli-angka">Terlambat</th></tr></thead>
          <tbody>{#each supplier as s (s.supplier_id)}
            <tr><th scope="row"><button class="beli-teks-button" type="button" on:click={() => { kodeSupplier = s.supplier_id; cari(); }}>{s.supplier_nama}</button></th>
              <td class="beli-angka">{formatJumlahBeli(s.faktur)}</td><td class="beli-angka">{rpBeli(s.saldo)}</td><td class="beli-angka">{rpBeli(s.terlambat)}</td></tr>
          {/each}</tbody>
        </table></div>
      </section>
    {/if}
    <section class="panel beli-panel">
      <h2>{hutang ? 'Faktur belum lunas' : 'Riwayat pembelian'}</h2>
      <form class="beli-filter" on:submit|preventDefault={cari}>
        <label>Cari faktur atau supplier <input type="search" maxlength="100" bind:value={pencarian} /></label>
        {#if hutang}
          <label>Supplier <select bind:value={kodeSupplier}>
            <option value="">Semua supplier</option>
            {#each supplier as s}<option value={s.supplier_id}>{s.supplier_nama}</option>{/each}
          </select></label>
          <label class="check"><input type="checkbox" bind:checked={terlambat} /> Terlambat saja</label>
        {:else}
          <label>Diterima dari <input type="date" bind:value={dari} /></label>
          <label>Sampai <input type="date" min={dari} bind:value={sampai} /></label>
        {/if}
        <button class="secondary" type="submit" disabled={loading}>Cari</button>
      </form>
      {#if galat}<p class="error" role="alert">{galat}</p>{/if}
      <div class="beli-gulir"><table class="beli-tabel" aria-busy={loading}>
        <thead><tr><th>Faktur</th><th>Supplier</th><th>{hutang ? 'Jatuh tempo' : 'Penerimaan'}</th>
          <th class="beli-angka">{hutang ? 'Sisa hutang' : 'Total faktur'}</th><th>Status</th></tr></thead>
        <tbody>
          {#each daftar as f (f.asal + ':' + f.no_transaksi)}
            <tr><th scope="row"><button type="button" class="beli-teks-button" on:click={() => detail(f.no_transaksi)}>{f.no_faktur ?? f.no_transaksi}</button>
              {#if f.no_faktur}<small>{f.no_transaksi}</small>{/if}</th>
              <td>{f.supplier_nama ?? '—'}</td><td>{(hutang ? f.jatuh_tempo : f.tanggal_terima) ?? '—'}</td>
              <td class="beli-angka">{rpBeli(hutang ? f.sisa : f.total)}</td>
              <td><span class="beli-status" class:gagal={f.status === 'TERLAMBAT' || f.status === 'BATAL'}
                class:waspada={f.status === 'TERBUKA'}>{labelFaktur[f.status]}</span></td>
            </tr>
          {:else}<tr><td colspan="5" class="beli-kosong">{loading ? 'Memuat faktur…' : 'Tidak ada faktur yang cocok.'}</td></tr>{/each}
        </tbody>
      </table></div>
      {#if lebih}<button type="button" class="secondary" disabled={loading} on:click={() => muat(page + 1)}>Muat lebih banyak</button>{/if}
    </section>
  {/if}

  <dialog class="modal beli-dialog beli-detail" bind:this={dialog} on:close={tutupDetail} aria-labelledby="beli-detail-judul">
    <div class="beli-baris-judul"><h2 id="beli-detail-judul">Faktur {pilihan?.no_transaksi ?? ''}</h2>
      <button type="button" class="secondary" on:click={() => dialog.close()}>Tutup</button>
    </div>
    {#if pesanDetail}<p role="status">{pesanDetail}</p>{/if}
    {#if galatDetail}<p class="error" role="alert">{galatDetail}</p>{/if}
    {#if pilihan}
      <dl class="beli-meta">
        <div><dt>Supplier</dt><dd>{pilihan.supplier_nama ?? '—'}</dd></div>
        <div><dt>No. faktur supplier</dt><dd>{pilihan.no_faktur ?? '—'}</dd></div>
        <div><dt>Diterima</dt><dd>{pilihan.tanggal_terima}</dd></div>
        <div><dt>Tanggal faktur</dt><dd>{pilihan.tanggal_faktur ?? '—'}</dd></div>
        <div><dt>Jatuh tempo</dt><dd>{pilihan.jatuh_tempo ?? '—'}</dd></div>
        <div><dt>Dibuat oleh</dt><dd>{pilihan.operator ?? '—'}</dd></div>
        <div><dt>Status</dt><dd>{labelFaktur[pilihan.status]}</dd></div>
        {#if pilihan.batal_at}<div><dt>Pembatalan</dt><dd>{pilihan.batal_at} · {pilihan.batal_nama} · {pilihan.batal_alasan}</dd></div>{/if}
      </dl>
      <div class="beli-gulir"><table class="beli-tabel">
        <thead><tr><th>Barang</th><th class="beli-angka">Jumlah</th><th class="beli-angka">Harga beli</th><th class="beli-angka">Potongan</th><th class="beli-angka">Subtotal</th></tr></thead>
        <tbody>{#each pilihan.baris as b}
          <tr><th scope="row">{b.nama ?? b.kode}<small>{b.kode}</small></th>
            <td class="beli-angka">{formatJumlahBeli(b.qty)} {b.satuan}{#if Number(b.isi) !== 1}<small>{formatJumlahBeli(b.isi)} {b.satuan_dasar} per {b.satuan}</small>{/if}</td>
            <td class="beli-angka">{rpBeli(b.harga)}</td><td class="beli-angka">{rpBeli(b.potongan)}</td><td class="beli-angka">{rpBeli(b.total)}</td></tr>
        {/each}</tbody>
      </table></div>
      <dl class="beli-total">
        <div><dt>Total faktur</dt><dd>{rpBeli(pilihan.total)}</dd></div>
        {#if pilihan.asal === 'WEB'}
          <div><dt>Sudah dibayar</dt><dd>{rpBeli(pilihan.sudah_bayar)}</dd></div>
          <div class="beli-total-utama"><dt>Sisa hutang</dt><dd>{rpBeli(pilihan.sisa)}</dd></div>
        {/if}
      </dl>
      {#if user.role === 'ADMIN' && pilihan.asal === 'WEB'}
        <h3>Riwayat pembayaran</h3>
        {#if pilihan.pembayaran.length}
          <div class="beli-gulir"><table class="beli-tabel">
            <thead><tr><th>Tanggal / petugas</th><th>Pembayaran</th><th class="beli-angka">Nominal</th><th>Tindakan</th></tr></thead>
            <tbody>{#each pilihan.pembayaran as p (p.id)}
              <tr><td>{p.tanggal}<small>{p.operator} · {p.created_at}</small></td>
                <td>{p.balik_id ? 'Pembalikan #' + p.balik_id : metodeBayar[p.metode] + ' #' + p.id}
                  {#if p.referensi}<small>{p.referensi}</small>{/if}{#if p.alasan}<small>{p.alasan}</small>{/if}</td>
                <td class="beli-angka">{rpBeli(p.nominal)}</td><td>
                  {#if p.dibalik}<small>Sudah dibalik</small>
                  {:else if !p.balik_id && pilihan.status !== 'BATAL'}
                    <button type="button" class="beli-teks-button" disabled={!online} on:click={() => mulaiAksi('balik', p)}>Balik</button>
                  {/if}
                </td></tr>
            {/each}</tbody>
          </table></div>
        {:else}<p>Belum ada pembayaran.</p>{/if}
        {#if pilihan.status !== 'BATAL'}
          <div class="beli-aksi">
            <button type="button" class="secondary" disabled={!online || pilihan.sudah_bayar !== 0} on:click={() => mulaiAksi('batal')}>Batalkan faktur</button>
            {#if (pilihan.sisa ?? 0) > 0}<button type="button" class="primary" disabled={!online} on:click={() => mulaiAksi('bayar')}>Catat pembayaran</button>{/if}
          </div>
          {#if pilihan.sudah_bayar !== 0}<p class="beli-petunjuk">Balik seluruh pembayaran sebelum membatalkan faktur.</p>{/if}
        {/if}
      {/if}
    {:else if !galatDetail}
      <p role="status">Memuat detail faktur…</p>
    {/if}
  </dialog>

  <dialog class="modal beli-dialog" bind:this={aksiDialog} aria-labelledby="beli-aksi-judul" on:cancel={(e) => { if (sibuk) e.preventDefault(); }}>
    <h2 id="beli-aksi-judul">{aksi === 'bayar' ? 'Catat pembayaran' : aksi === 'balik' ? 'Balik pembayaran' : 'Batalkan faktur'}</h2>
    <form class="beli-form" on:submit|preventDefault={simpanAksi}>
      {#if aksi === 'bayar'}
        <p>Sisa hutang {rpBeli(pilihan?.sisa)}</p>
        <label>Tanggal pembayaran <input type="date" min={pilihan?.tanggal_faktur ?? undefined} max={pilihan?.tanggal_server} bind:value={tanggalBayar} required /></label>
        <label>Nominal
          {#key sesiAksi}
            <input class="beli-angka" inputmode="numeric" required use:inputAngkaPembelian={{
              jenis: 'rupiah', nilai: nominal, min: 1, max: pilihan?.sisa ?? undefined, ubah: nilai => nominal = nilai
            }} />
          {/key}
        </label>
        <label>Metode pembayaran
          <select aria-label="Metode pembayaran" bind:value={metode}>{#each Object.entries(metodeBayar) as [kode, nama]}<option value={kode}>{nama}</option>{/each}</select>
        </label>
        <label>Referensi pembayaran <input maxlength="100" bind:value={referensi} /></label>
      {:else}
        <p>{aksi === 'batal' ? 'Stok yang masuk dari faktur ini akan dikoreksi.' : 'Saldo faktur akan disesuaikan sebesar ' + rpBeli(balikBayar?.nominal) + '.'}</p>
        <label>Alasan <input maxlength="250" bind:value={alasan} required /></label>
      {/if}
      {#if galatAksi}<p class="error" role="alert">{galatAksi}</p>{/if}
      <div class="beli-aksi"><button class="secondary" type="button" disabled={sibuk} on:click={() => aksiDialog.close()}>Kembali</button>
        <button class="primary" type="submit" disabled={sibuk || !online}>{sibuk ? 'Menyimpan…' : 'Konfirmasi'}</button>
      </div>
    </form>
  </dialog>
</div>
