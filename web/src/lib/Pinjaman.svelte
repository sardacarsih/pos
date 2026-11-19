<script lang="ts">
  import { api, ApiError, query } from './api';
  import { rpBeli } from './pembelian';
  import { aksiTersedia, labelStatus, labelTahap, TAHAP, type Aksi, type Antrian, type Aturan, type BarisJadwal, type Pengajuan, type Status, type Tahap } from './pinjaman';
  import type { User } from './types';
  import './pembelian.css';

  export let user: User;

  type Detail = Pengajuan & { jadwal: BarisJadwal[]; jumlah_pinjaman_lalu: number };
  type HakUser = { id: number; username: string; nama: string; tahap: Tahap[] };

  let tab: 'pengajuan' | 'antrian' | 'pengaturan' = 'pengajuan';
  let galat = '';
  let pesan = '';
  let sibuk = false;

  let status: Status | '' = '';
  let cari = '';
  let items: Pengajuan[] = [];
  let hak: Tahap[] = [];
  let antrian: Antrian | undefined;
  let detail: Detail | undefined;
  let users: HakUser[] = [];
  let aturan: Aturan | undefined;
  let plafonBaru: number | undefined;
  let maksBaru: number | undefined;

  $: kepala = antrian?.items[0]?.id;
  const label: Record<Aksi, string> = { verifikasi: 'Verifikasi', setujui: 'Setujui', tolak: 'Tolak', cairkan: 'Cairkan' };

  async function jalan(f: () => Promise<void>) {
    if (sibuk) return;
    sibuk = true; galat = ''; pesan = '';
    try { await f(); } catch (e) { galat = (e as Error).message; } finally { sibuk = false; }
  }

  async function muatDaftar() {
    const r = await api<{ items: Pengajuan[]; hak: Tahap[] }>('/pinjaman' + query({ status: status || undefined, q: cari || undefined }), { userId: user.id });
    items = r.items; hak = r.hak;
  }
  async function muatAntrian() {
    antrian = await api<Antrian>('/pinjaman/antrian', { userId: user.id });
    plafonBaru = antrian.plafon ?? undefined;
  }
  async function muatPengaturan() {
    const r = await api<{ users: HakUser[]; aturan: Aturan }>('/pinjaman/pengaturan', { userId: user.id });
    users = r.users; aturan = r.aturan; maksBaru = r.aturan.maks_pinjaman;
  }
  const segarkan = () => Promise.all([muatDaftar(), muatAntrian().catch(e => { galat = (e as Error).message; })]).then(() => {});

  void jalan(segarkan);

  function buka(p: Pengajuan) {
    void jalan(async () => { detail = await api<Detail>(`/pinjaman/${p.id}`, { userId: user.id }); });
  }

  function proses(p: Pengajuan, aksi: Aksi) {
    const tanya = aksi === 'tolak' ? 'Alasan penolakan (wajib):' : `${label[aksi]} pengajuan #${p.id} ${p.nama} Rp ${p.jumlah.toLocaleString('id-ID')}?\nCatatan (opsional):`;
    const isi = prompt(tanya);
    if (isi === null) return;
    if (aksi === 'tolak' && !isi.trim()) { galat = 'Alasan penolakan wajib diisi.'; return; }
    void jalan(async () => {
      const r = await api<{ hasil: string }>(`/pinjaman/${p.id}/${aksi}`, {
        method: 'POST', body: { catatan: isi.trim() || null }, headers: { 'Idempotency-Key': crypto.randomUUID() }, userId: user.id
      });
      pesan = aksi === 'cairkan' ? `Dicairkan sebagai ${r.hasil}. Catat pengeluaran kas lewat menu Kas & Bank → Tarik transaksi.` : `Pengajuan #${p.id}: ${label[aksi].toLowerCase()} berhasil.`;
      detail = undefined;
      await segarkan();
    });
  }

  function simpanPlafon() {
    const a = antrian;
    if (!a || !Number.isSafeInteger(plafonBaru) || (plafonBaru ?? -1) < 0) { galat = 'Plafon harus bilangan bulat ≥ 0.'; return; }
    void jalan(async () => {
      await api(`/pinjaman/plafon/${a.periode}`, { method: 'PUT', body: { plafon: plafonBaru }, userId: user.id });
      pesan = 'Plafon dana disimpan.';
      await muatAntrian();
    });
  }

  function simpanMaks() {
    if (!Number.isSafeInteger(maksBaru) || (maksBaru ?? -1) < 0) { galat = 'Plafon maksimal harus bilangan bulat ≥ 0.'; return; }
    void jalan(async () => {
      await api('/pinjaman/pengaturan/maks', { method: 'PUT', body: { maks_pinjaman: maksBaru }, userId: user.id });
      pesan = 'Plafon maksimal per anggota disimpan.';
      await muatPengaturan();
    });
  }

  function ubahHak(u: HakUser, t: Tahap, on: boolean) {
    const tahap = on ? [...u.tahap, t] : u.tahap.filter(x => x !== t);
    void jalan(async () => {
      try { await api('/pinjaman/pengaturan/hak', { method: 'PUT', body: { user_id: u.id, tahap }, userId: user.id }); }
      catch (e) { if (e instanceof ApiError) await muatPengaturan(); throw e; }
      await Promise.all([muatPengaturan(), muatDaftar()]);
    });
  }

  function pindah(t: typeof tab) {
    tab = t;
    if (t === 'pengaturan') void jalan(muatPengaturan);
  }
</script>

<div class="beli">
  {#if galat}<p class="error" role="alert">{galat}</p>{/if}
  {#if pesan}<p class="success" role="status">{pesan}</p>{/if}

  <div class="beli-filter" role="tablist">
    {#each [['pengajuan', 'Pengajuan'], ['antrian', 'Antrian dana'], ['pengaturan', 'Pengaturan']] as [id, nama]}
      <button type="button" role="tab" aria-selected={tab === id} class:primary={tab === id} class:secondary={tab !== id} on:click={() => pindah(id as typeof tab)}>{nama}</button>
    {/each}
  </div>

  {#if tab === 'pengajuan'}
    <section class="panel beli-panel">
      <h2>Pengajuan pinjaman</h2>
      {#if !hak.length}<p>Akun Anda belum diberi hak tahap pinjaman. Atur di tab Pengaturan.</p>{/if}
      <form class="beli-filter" on:submit|preventDefault={() => jalan(muatDaftar)}>
        <label>Status <select bind:value={status}><option value="">Semua</option>{#each Object.entries(labelStatus) as [k, v]}<option value={k}>{v}</option>{/each}</select></label>
        <label>Cari <input placeholder="Nama / NIK" bind:value={cari} /></label>
        <button class="secondary" type="submit" disabled={sibuk}>Tampilkan</button>
      </form>
      <div class="beli-gulir"><table class="beli-tabel" aria-busy={sibuk}>
        <thead><tr><th>#</th><th>Anggota</th><th>Jumlah</th><th>Tenor</th><th>Angsuran</th><th>Status</th><th></th></tr></thead>
        <tbody>{#each items as p (p.id)}
          <tr><th scope="row"><button class="secondary" type="button" on:click={() => buka(p)}>{p.id}</button></th>
            <td>{p.nama}<small> · {p.nik}</small><br /><small>{p.keperluan}</small></td>
            <td class="beli-angka">{rpBeli(p.jumlah)}</td><td>{p.tenor} bln</td><td class="beli-angka">{rpBeli(Math.round(p.angsuran))}</td>
            <td><span class="beli-status" class:gagal={p.status === 'DITOLAK' || p.status === 'DIBATALKAN'}>{labelStatus[p.status]}{p.antrian ? ` #${p.antrian}` : ''}</span>
              {#if p.no_transaksi}<small> {p.no_transaksi}</small>{/if}</td>
            <td>{#each aksiTersedia(p, hak, user.id, p.id === kepala) as a}
              <button class:primary={a !== 'tolak'} class:secondary={a === 'tolak'} type="button" disabled={sibuk} on:click={() => proses(p, a)}>{label[a]}</button>
            {/each}</td></tr>
        {:else}<tr><td colspan="7">Tidak ada pengajuan.</td></tr>{/each}</tbody>
      </table></div>
    </section>

    {#if detail}
      <section class="panel beli-panel">
        <h2>Pengajuan #{detail.id} — {detail.nama}</h2>
        <p>Rp {rpBeli(detail.jumlah)} · {detail.tenor} bulan · bunga {detail.bunga}%/bln · {detail.jumlah_pinjaman_lalu} pinjaman sebelumnya</p>
        <ul>
          <li>Diajukan {detail.tgl_aju?.slice(0, 16).replace('T', ' ')}: {detail.keperluan}</li>
          {#if detail.verif_nama}<li>Diverifikasi {detail.verif_nama} {detail.verif_tgl?.slice(0, 16).replace('T', ' ')}{detail.verif_catatan ? ` — ${detail.verif_catatan}` : ''}</li>{/if}
          {#if detail.setuju_nama}<li>Disetujui {detail.setuju_nama} {detail.setuju_tgl?.slice(0, 16).replace('T', ' ')}{detail.setuju_catatan ? ` — ${detail.setuju_catatan}` : ''}</li>{/if}
          {#if detail.tolak_nama}<li>Ditolak {detail.tolak_nama}: {detail.alasan}</li>{/if}
          {#if detail.cair_nama}<li>Dicairkan {detail.cair_nama} {detail.cair_tgl?.slice(0, 16).replace('T', ' ')} — {detail.no_transaksi}</li>{/if}
        </ul>
        <div class="beli-gulir"><table class="beli-tabel">
          <thead><tr><th>Ke</th><th>Jatuh tempo</th><th>Pokok</th><th>Bunga</th><th>Angsuran</th><th>Sisa</th></tr></thead>
          <tbody>{#each detail.jadwal as b (b.ke)}
            <tr><td>{b.ke}</td><td>{b.jatuh_tempo}</td><td class="beli-angka">{rpBeli(Math.round(b.pokok))}</td><td class="beli-angka">{rpBeli(Math.round(b.bunga))}</td>
              <td class="beli-angka">{rpBeli(Math.round(b.angsuran))}</td><td class="beli-angka">{rpBeli(Math.round(b.saldo_akhir))}</td></tr>
          {/each}</tbody>
        </table></div>
        <p><small>Jadwal simulasi per hari ini; jadwal final dihitung saat pencairan.</small></p>
        <button class="secondary" type="button" on:click={() => detail = undefined}>Tutup</button>
      </section>
    {/if}

  {:else if tab === 'antrian'}
    <section class="panel beli-panel">
      <h2>Antrian dana periode {antrian?.periode ?? '—'}</h2>
      {#if antrian}
        <p>Plafon {antrian.plafon == null ? 'belum diatur' : rpBeli(antrian.plafon)} · terpakai {rpBeli(antrian.terpakai)} · sisa <strong>{antrian.sisa == null ? '—' : rpBeli(antrian.sisa)}</strong>
          <small>(termasuk pinjaman yang dibuat dari BackOffice, {antrian.dari} s.d. {antrian.sampai})</small></p>
        <form class="beli-filter" on:submit|preventDefault={simpanPlafon}>
          <label>Plafon dana periode ini <input type="number" min="0" step="1" required bind:value={plafonBaru} /></label>
          <button class="primary" type="submit" disabled={sibuk}>Simpan plafon</button>
        </form>
        <div class="beli-gulir"><table class="beli-tabel">
          <thead><tr><th>Urutan</th><th>Anggota</th><th>Jumlah</th><th>Disetujui</th><th></th></tr></thead>
          <tbody>{#each antrian.items as p (p.id)}
            <tr><td>{p.antrian}</td><td>{p.nama}<small> · {p.nik}</small></td><td class="beli-angka">{rpBeli(p.jumlah)}</td>
              <td>{p.setuju_tgl?.slice(0, 16).replace('T', ' ')}<small> · {p.setuju_nama}</small></td>
              <td>{#if p.id === kepala}
                {#if antrian.sisa != null && p.jumlah > antrian.sisa}<span class="beli-status gagal">dana belum cukup</span>{/if}
                {#each aksiTersedia(p, hak, user.id, true).filter(a => a === 'cairkan') as a}
                  <button class="primary" type="button" disabled={sibuk || antrian.sisa == null || p.jumlah > antrian.sisa} on:click={() => proses(p, a)}>Cairkan</button>
                {/each}
              {:else}<small>menunggu</small>{/if}</td></tr>
          {:else}<tr><td colspan="5">Antrian kosong.</td></tr>{/each}</tbody>
        </table></div>
      {/if}
    </section>

  {:else}
    <section class="panel beli-panel">
      <h2>Aturan pinjaman</h2>
      {#if aturan}
        <p>Bunga {aturan.bunga}%/bulan dan tenor maksimal {aturan.maks_tenor} bulan mengikuti pengaturan BackOffice.</p>
        <form class="beli-filter" on:submit|preventDefault={simpanMaks}>
          <label>Plafon maksimal per anggota (0 = tanpa batas) <input type="number" min="0" step="1" required bind:value={maksBaru} /></label>
          <button class="primary" type="submit" disabled={sibuk}>Simpan</button>
        </form>
      {/if}
    </section>
    <section class="panel beli-panel">
      <h2>Hak tahap pengurus</h2>
      <p>Satu pengajuan wajib diverifikasi, disetujui, dan dicairkan oleh orang yang berbeda.</p>
      <div class="beli-gulir"><table class="beli-tabel">
        <thead><tr><th>Pengguna</th>{#each TAHAP as t}<th>{labelTahap[t]}</th>{/each}</tr></thead>
        <tbody>{#each users as u (u.id)}
          <tr><th scope="row">{u.nama}<small> · {u.username}</small></th>
            {#each TAHAP as t}<td><input type="checkbox" aria-label="{labelTahap[t]} untuk {u.nama}" checked={u.tahap.includes(t)} disabled={sibuk}
              on:change={(e) => ubahHak(u, t, e.currentTarget.checked)} /></td>{/each}</tr>
        {/each}</tbody>
      </table></div>
    </section>
  {/if}
</div>
