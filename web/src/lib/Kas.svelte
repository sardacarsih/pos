<script lang="ts">
  import { api, ApiError, query } from './api';
  import { rpBeli } from './pembelian';
  import { bisaDibatal, cekMutasi, labelSumber, permintaanMutasi, type Akun, type Buku, type IsianMutasi, type JenisAkun, type Mutasi } from './kas';
  import type { User } from './types';
  import './pembelian.css';

  export let user: User;

  const hariIni = () => { const d = new Date(); return new Date(d.getTime() - d.getTimezoneOffset() * 60000).toISOString().slice(0, 10); };
  const awalBulan = () => hariIni().slice(0, 8) + '01';

  let akun: Akun[] = [];
  let galat = '';
  let pesan = '';
  let sibuk = false;

  // Buku
  let pilihAkun: number | '' = '';
  let dari = awalBulan();
  let sampai = hariIni();
  let buku: Buku | undefined;

  // Input mutasi; kunci idempotensi dipakai ulang sampai berhasil agar kirim ulang tidak dobel.
  const isianBaru = (): IsianMutasi => ({ jenis: 'M', akun: pilihAkun, ke: '', tanggal: hariIni(), nominal: undefined, keterangan: '' });
  let isian = isianBaru();
  let kunci = crypto.randomUUID();

  // Akun
  type FormAkun = { id?: number; kode: string; nama: string; jenis: JenisAkun; no_rekening: string; atas_nama: string; saldo_awal: number; tgl_saldo_awal: string; default: boolean; aktif: boolean };
  let form: FormAkun | null = null;

  // Tarik
  let tarikDari = awalBulan();
  let tarikSampai = hariIni();

  const aktif = () => akun.filter(a => a.aktif);

  async function jalan(f: () => Promise<void>) {
    if (sibuk) return;
    sibuk = true; galat = ''; pesan = '';
    try { await f(); } catch (e) { galat = (e as Error).message; } finally { sibuk = false; }
  }

  async function muatAkun() {
    akun = (await api<{ akun: Akun[] }>('/kas/akun', { userId: user.id })).akun;
    if (!pilihAkun && akun.length) pilihAkun = (akun.find(a => a.default_untuk === 'KAS') ?? akun[0]).id;
  }

  async function muatBuku() {
    if (!pilihAkun) { buku = undefined; return; }
    buku = await api<Buku>('/kas/buku' + query({ akun: pilihAkun, dari, sampai }), { userId: user.id });
  }

  const segarkan = () => Promise.all([muatAkun(), muatBuku()]).then(() => {});

  void jalan(async () => { await muatAkun(); await muatBuku(); });

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

  function ubah(a?: Akun) {
    form = a
      ? { id: a.id, kode: a.kode, nama: a.nama, jenis: a.jenis, no_rekening: a.no_rekening ?? '', atas_nama: a.atas_nama ?? '', saldo_awal: a.saldo_awal, tgl_saldo_awal: a.tgl_saldo_awal, default: a.default_untuk !== null, aktif: a.aktif }
      : { kode: '', nama: '', jenis: 'KAS', no_rekening: '', atas_nama: '', saldo_awal: 0, tgl_saldo_awal: awalBulan(), default: false, aktif: true };
  }

  function simpanAkun() {
    const f = form;
    if (!f) return;
    if (!f.kode.trim() || !f.nama.trim() || !f.tgl_saldo_awal || !Number.isSafeInteger(f.saldo_awal)) { galat = 'Kode, nama, saldo awal, dan tanggal saldo awal wajib.'; return; }
    const body = { kode: f.kode, nama: f.nama, jenis: f.jenis, no_rekening: f.no_rekening || null, atas_nama: f.atas_nama || null,
      saldo_awal: f.saldo_awal, tgl_saldo_awal: f.tgl_saldo_awal, default_untuk: f.default ? f.jenis : null, aktif: f.aktif };
    void jalan(async () => {
      await api(f.id ? `/kas/akun/${f.id}` : '/kas/akun', { method: f.id ? 'PUT' : 'POST', body, userId: user.id });
      form = null;
      pesan = 'Akun disimpan.';
      await segarkan();
    });
  }
</script>

<div class="beli">
  {#if galat}<p class="error" role="alert">{galat}</p>{/if}
  {#if pesan}<p class="success" role="status">{pesan}</p>{/if}

  <section class="panel beli-panel">
    <h2>Akun kas &amp; bank</h2>
    <div class="beli-gulir"><table class="beli-tabel">
      <thead><tr><th>Kode</th><th>Nama</th><th>Jenis</th><th>Rekening</th><th>Saldo</th><th></th></tr></thead>
      <tbody>{#each akun as a (a.id)}
        <tr><th scope="row">{a.kode}</th>
          <td>{a.nama}{#if a.default_untuk} <span class="beli-status">default</span>{/if}{#if !a.aktif} <span class="beli-status gagal">nonaktif</span>{/if}</td>
          <td>{a.jenis}</td><td>{a.no_rekening ?? '—'}</td><td class="beli-angka">{rpBeli(a.saldo)}</td>
          <td><button class="secondary" type="button" on:click={() => ubah(a)}>Ubah</button></td></tr>
      {:else}<tr><td colspan="6">Belum ada akun. Tambahkan akun KAS dan BANK, lalu tandai masing-masing sebagai default.</td></tr>{/each}</tbody>
    </table></div>
    {#if form}
      <form class="beli-filter" on:submit|preventDefault={simpanAkun}>
        <label>Kode <input maxlength="20" required bind:value={form.kode} /></label>
        <label>Nama <input maxlength="100" required bind:value={form.nama} /></label>
        <label>Jenis <select bind:value={form.jenis}><option value="KAS">KAS</option><option value="BANK">BANK</option></select></label>
        <label>No. rekening <input maxlength="50" bind:value={form.no_rekening} /></label>
        <label>Atas nama <input maxlength="100" bind:value={form.atas_nama} /></label>
        <label>Saldo awal <input type="number" step="1" required bind:value={form.saldo_awal} /></label>
        <label>Per tanggal <input type="date" required bind:value={form.tgl_saldo_awal} /></label>
        <label class="check"><input type="checkbox" bind:checked={form.default} /> Default untuk {form.jenis === 'KAS' ? 'transaksi tunai' : 'transfer'}</label>
        <label class="check"><input type="checkbox" bind:checked={form.aktif} /> Aktif</label>
        <button type="submit" disabled={sibuk}>Simpan</button>
        <button class="secondary" type="button" on:click={() => form = null}>Batal</button>
      </form>
    {:else}<button class="secondary" type="button" on:click={() => ubah()}>Tambah akun</button>{/if}
  </section>

  <section class="panel beli-panel">
    <h2>Tarik transaksi dari modul lain</h2>
    <p>Menyalin penjualan tunai (per hari), pembayaran supplier, dan pencairan pinjaman ke akun default. Aman diulang: hanya selisih yang dicatat, termasuk koreksi bila transaksi asal diubah atau dihapus.</p>
    <form class="beli-filter" on:submit|preventDefault={tarik}>
      <label>Dari <input type="date" required max={tarikSampai} bind:value={tarikDari} /></label>
      <label>Sampai <input type="date" required min={tarikDari} max={hariIni()} bind:value={tarikSampai} /></label>
      <button type="submit" disabled={sibuk}>Tarik transaksi</button>
    </form>
  </section>

  <section class="panel beli-panel">
    <h2>Catat mutasi</h2>
    <form class="beli-filter" on:submit|preventDefault={simpanMutasi}>
      <label>Jenis <select bind:value={isian.jenis}><option value="M">Kas masuk</option><option value="K">Kas keluar</option><option value="TRANSFER">Transfer antar akun</option></select></label>
      <label>{isian.jenis === 'TRANSFER' ? 'Dari akun' : 'Akun'} <select bind:value={isian.akun}>
        <option value="">Pilih</option>{#each aktif() as a (a.id)}<option value={a.id}>{a.kode} — {a.nama}</option>{/each}</select></label>
      {#if isian.jenis === 'TRANSFER'}
        <label>Ke akun <select bind:value={isian.ke}><option value="">Pilih</option>{#each aktif() as a (a.id)}<option value={a.id}>{a.kode} — {a.nama}</option>{/each}</select></label>
      {/if}
      <label>Tanggal <input type="date" required max={hariIni()} bind:value={isian.tanggal} /></label>
      <label>Nominal <input type="number" min="1" step="1" required bind:value={isian.nominal} /></label>
      <label>Keterangan <input maxlength="250" required bind:value={isian.keterangan} /></label>
      <button type="submit" disabled={sibuk}>Simpan</button>
    </form>
  </section>

  <section class="panel beli-panel">
    <h2>Buku kas / bank</h2>
    <form class="beli-filter" on:submit|preventDefault={() => jalan(muatBuku)}>
      <label>Akun <select bind:value={pilihAkun}>{#each akun as a (a.id)}<option value={a.id}>{a.kode} — {a.nama}</option>{/each}</select></label>
      <label>Dari <input type="date" required bind:value={dari} /></label>
      <label>Sampai <input type="date" required min={dari} bind:value={sampai} /></label>
      <button class="secondary" type="submit" disabled={sibuk}>Tampilkan</button>
    </form>
    {#if buku}
      <div class="beli-gulir"><table class="beli-tabel" aria-busy={sibuk}>
        <thead><tr><th>Tanggal</th><th>Keterangan</th><th>Sumber</th><th>Masuk</th><th>Keluar</th><th>Saldo</th><th></th></tr></thead>
        <tbody>
          <tr><td>{dari}</td><td colspan="4"><strong>Saldo awal</strong></td><td class="beli-angka">{rpBeli(buku.saldo_awal)}</td><td></td></tr>
          {#each buku.mutasi as m (m.id)}
            <tr><td>{m.tanggal}</td><td>{m.keterangan}{#if m.user}<small> · {m.user}</small>{/if}</td>
              <td>{labelSumber[m.sumber]}{#if m.ref}<small> {m.ref}</small>{/if}</td>
              <td class="beli-angka">{m.masuk ? rpBeli(m.masuk) : ''}</td><td class="beli-angka">{m.keluar ? rpBeli(m.keluar) : ''}</td>
              <td class="beli-angka">{rpBeli(m.saldo)}</td>
              <td>{#if bisaDibatal(m)}<button class="secondary" type="button" disabled={sibuk} on:click={() => batal(m)}>Batal</button>{:else if m.dibalik}<span class="beli-status gagal">dibatalkan</span>{/if}</td></tr>
          {/each}
          <tr><td colspan="5"><strong>Saldo akhir</strong></td><td class="beli-angka"><strong>{rpBeli(buku.saldo_akhir)}</strong></td><td></td></tr>
        </tbody>
      </table></div>
    {/if}
  </section>
</div>
