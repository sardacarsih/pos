<script lang="ts">
  import { api, ApiError } from './api';
  import { cekAjuan, labelStatus, LANGKAH, type Aturan, type BarisJadwal, type Pengajuan } from './pinjaman';
  import type { User } from './types';

  export let user: User;

  type Aktif = { no_transaksi: string; tanggal: string; pinjaman: number; tenor: number; angsuran: number; angsuran_lewat: number };
  let aturan: Aturan | undefined;
  let pengajuan: Pengajuan[] = [];
  let aktif: Aktif[] = [];
  let galat = '';
  let pesan = '';
  let sibuk = false;

  let jumlah: number | undefined;
  let tenor: number | undefined;
  let keperluan = '';
  let jadwal: BarisJadwal[] = [];
  let kunci = crypto.randomUUID();

  const rp = (n: number) => new Intl.NumberFormat('id-ID', { style: 'currency', currency: 'IDR', maximumFractionDigits: 0 }).format(n || 0);
  $: berjalan = pengajuan.find(p => LANGKAH.slice(0, 3).includes(p.status));
  $: bisaAju = !berjalan && !aktif.length;

  async function jalan(f: () => Promise<void>) {
    if (sibuk) return;
    sibuk = true; galat = ''; pesan = '';
    try { await f(); } catch (e) { galat = (e as Error).message; } finally { sibuk = false; }
  }

  async function muat() {
    const r = await api<{ aturan: Aturan; pengajuan: Pengajuan[]; pinjaman_aktif: Aktif[] }>('/saya/pinjaman', { userId: user.id });
    aturan = r.aturan; pengajuan = r.pengajuan; aktif = r.pinjaman_aktif;
    tenor ??= r.aturan.maks_tenor;
  }
  void jalan(muat);

  function simulasi() {
    if (!aturan) return;
    const salah = cekAjuan(jumlah, tenor, 'x', aturan);
    if (salah) { galat = salah; return; }
    void jalan(async () => {
      jadwal = (await api<{ jadwal: BarisJadwal[] }>('/saya/pinjaman/simulasi', { method: 'POST', body: { jumlah, tenor }, userId: user.id })).jadwal;
    });
  }

  function ajukan() {
    if (!aturan) return;
    const salah = cekAjuan(jumlah, tenor, keperluan, aturan);
    if (salah) { galat = salah; return; }
    void jalan(async () => {
      try { await api('/saya/pinjaman', { method: 'POST', body: { jumlah, tenor, keperluan: keperluan.trim() }, headers: { 'Idempotency-Key': kunci }, userId: user.id }); }
      catch (e) { if (e instanceof ApiError && [400, 409, 422].includes(e.status)) kunci = crypto.randomUUID(); throw e; }
      kunci = crypto.randomUUID();
      jumlah = undefined; keperluan = ''; jadwal = [];
      pesan = 'Pengajuan terkirim. Pantau statusnya di bawah.';
      await muat();
    });
  }

  function batal(p: Pengajuan) {
    if (!confirm(`Batalkan pengajuan ${rp(p.jumlah)}?`)) return;
    void jalan(async () => {
      await api(`/saya/pinjaman/${p.id}/batal`, { method: 'POST', headers: { 'Idempotency-Key': crypto.randomUUID() }, userId: user.id });
      pesan = 'Pengajuan dibatalkan.';
      await muat();
    });
  }
</script>

{#if galat}<div class="error" role="alert">{galat}</div>{/if}{#if pesan}<div class="notice" role="status">{pesan}</div>{/if}

{#each aktif as a (a.no_transaksi)}
  <section class="panel"><p class="eyebrow">PINJAMAN AKTIF</p><h2>{rp(a.pinjaman)}</h2>
    <p>{a.no_transaksi} · cair {a.tanggal} · angsuran {rp(Math.round(a.angsuran))}/bln · {a.angsuran_lewat} dari {a.tenor} angsuran sudah jatuh tempo</p></section>
{/each}

{#if berjalan}
  <section class="panel"><p class="eyebrow">PENGAJUAN BERJALAN</p><h2>{rp(berjalan.jumlah)} · {berjalan.tenor} bulan</h2>
    <ol class="langkah">{#each LANGKAH as s, i}
      <li class:selesai={LANGKAH.indexOf(berjalan.status) >= i}>{labelStatus[s]}</li>
    {/each}</ol>
    {#if berjalan.antrian}<p>Nomor antrian pencairan: <strong>{berjalan.antrian}</strong></p>{/if}
    <p class="muted">Perkiraan angsuran {rp(Math.round(berjalan.angsuran))}/bulan. {berjalan.keperluan}</p>
    {#if berjalan.status === 'DIAJUKAN'}<button class="secondary" disabled={sibuk} on:click={() => batal(berjalan)}>Batalkan pengajuan</button>{/if}
  </section>
{/if}

{#if bisaAju && aturan}
  <section class="panel"><p class="eyebrow">AJUKAN PINJAMAN</p><h2>Pinjaman tunai</h2>
    <p class="muted">Bunga {aturan.bunga}%/bulan · tenor maksimal {aturan.maks_tenor} bulan{aturan.maks_pinjaman ? ` · maksimal ${rp(aturan.maks_pinjaman)}` : ''}. Angsuran dipotong dari gaji.</p>
    <form on:submit|preventDefault={ajukan}>
      <label>Jumlah pinjaman<input type="number" min="1" step="1" required bind:value={jumlah} on:change={() => jadwal = []}/></label>
      <label>Tenor (bulan)<select bind:value={tenor} on:change={() => jadwal = []}>{#each Array.from({ length: aturan.maks_tenor }, (_, i) => i + 1) as t}<option value={t}>{t}</option>{/each}</select></label>
      <label>Keperluan<input maxlength="200" required bind:value={keperluan}/></label>
      <button type="button" class="secondary" disabled={sibuk} on:click={simulasi}>Hitung angsuran</button>
      <button class="primary" disabled={sibuk}>Kirim pengajuan</button>
    </form>
    {#if jadwal.length}
      <table class="jadwal"><thead><tr><th>Ke</th><th>Jatuh tempo</th><th>Angsuran</th><th>Sisa</th></tr></thead>
        <tbody>{#each jadwal as b (b.ke)}<tr><td>{b.ke}</td><td>{b.jatuh_tempo}</td><td>{rp(Math.round(b.angsuran))}</td><td>{rp(Math.round(b.saldo_akhir))}</td></tr>{/each}</tbody></table>
    {/if}
  </section>
{/if}

<section class="panel"><p class="eyebrow">RIWAYAT</p><h2>Pengajuan saya</h2>
  {#each pengajuan as p (p.id)}
    <div class="list-button horizontal"><span><strong>{rp(p.jumlah)} · {p.tenor} bln</strong><small>{p.tgl_aju?.slice(0, 10)} · {labelStatus[p.status]}{p.no_transaksi ? ` · ${p.no_transaksi}` : ''}{p.alasan ? ` · ${p.alasan}` : ''}</small></span></div>
  {:else}<p class="empty">Belum ada pengajuan.</p>{/each}
</section>

<style>
  .langkah { display: flex; flex-wrap: wrap; gap: .5rem; padding: 0; list-style: none; }
  .langkah li { padding: .25rem .6rem; border-radius: 999px; border: 1px solid currentColor; opacity: .5; font-size: .85rem; }
  .langkah li.selesai { opacity: 1; font-weight: 600; }
  .jadwal { width: 100%; margin-top: 1rem; border-collapse: collapse; font-size: .9rem; }
  .jadwal th, .jadwal td { padding: .3rem; text-align: right; border-bottom: 1px solid color-mix(in srgb, currentColor 15%, transparent); }
  .jadwal th:first-child, .jadwal td:first-child { text-align: left; }
</style>
