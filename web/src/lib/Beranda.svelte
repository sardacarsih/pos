<script lang="ts">
  import { createEventDispatcher, onMount } from 'svelte';
  import { api } from './api';
  import type { User, Data } from './types';
  import Receipt from './Receipt.svelte';
  import { delta, teksDelta, proporsi, statusLimit, persenTerpakai, tinggiBatang, singkat, hariSingkat } from './beranda';

  export let user: User;
  const dispatch = createEventDispatcher<{ go: string }>();

  type Perhatian = { tingkat: 'bahaya' | 'peringatan'; judul: string; teks: string; tuju: string | null };

  let data: Data = {}, loading = true, error = '', invoice: Data | null = null, stokHabis: number | null = null;

  const rp = (n: number) => new Intl.NumberFormat('id-ID', { style: 'currency', currency: 'IDR', maximumFractionDigits: 0 }).format(n || 0);
  const tglPanjang = (t?: string) => {
    const d = new Date(`${t}T00:00:00`);
    return !t || Number.isNaN(d.getTime()) ? '' : new Intl.DateTimeFormat('id-ID', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' }).format(d);
  };
  const tglPendek = (t?: string) => {
    const d = new Date(`${t}T00:00:00`);
    return !t || Number.isNaN(d.getTime()) ? '' : new Intl.DateTimeFormat('id-ID', { day: 'numeric', month: 'short' }).format(d);
  };

  async function load() {
    loading = true; error = ''; stokHabis = null;
    try {
      data = await api(user.role === 'KASIR' ? '/kasir/dashboard' : user.role === 'ANGGOTA' ? '/saya/dashboard' : '/admin/dashboard');
      // Stok habis berat dihitung server: dimuat terpisah agar Beranda tidak menunggu.
      if (user.role === 'ADMIN') api<Data>('/admin/dashboard/stok-habis').then(r => stokHabis = r.jumlah).catch(() => stokHabis = null);
    } catch (e) { error = (e as Error).message; } finally { loading = false; }
  }
  onMount(load);

  async function detail(no: string) {
    try { invoice = await api((user.role === 'KASIR' ? '/faktur/' : user.role === 'ADMIN' ? '/admin/faktur/' : '/saya/transaksi/') + encodeURIComponent(no)); } catch (e) { error = (e as Error).message; }
  }

  $: nama = user.nama.split(' ')[0];
  $: r = data.ringkasan || {};
  $: terbuka = data.periode_terbuka !== false;
  $: pesanPeriode = data.periode_pesan || data.pesan || 'Periode belum terbuka.';
  $: rata = r.jumlah ? Math.round((r.total || 0) / r.jumlah) : 0;
  $: split = proporsi(r.tunai || 0, r.kredit || 0);
  $: dTotal = delta(r.total || 0, data.kemarin?.total || 0);
  $: dJumlah = delta(r.jumlah || 0, data.kemarin?.jumlah || 0);
  $: tren = (data.tren || []) as Data[];
  $: nilaiTren = tren.map(t => t.total as number);
  $: limit = user.role === 'ANGGOTA' && data.ringkasan ? { status: statusLimit(r.belanja_periode, r.limit), persen: persenTerpakai(r.belanja_periode, r.limit) } : null;
  $: periodeChip = data.periode_dari ? `${tglPendek(data.periode_dari)} – ${tglPendek(data.periode_sampai)}` : data.ringkasan?.dari ? `${tglPendek(data.ringkasan.dari)} – ${tglPendek(data.ringkasan.sampai)}` : '';
  $: perhatian = (user.role === 'ADMIN' && data.tanggal ? [
    !terbuka ? { tingkat: 'bahaya', judul: 'Periode tidak terbuka', teks: pesanPeriode, tuju: null } : null,
    (data.user?.terkunci || 0) > 0 ? { tingkat: 'peringatan', judul: `${data.user.terkunci} akun terkunci`, teks: 'Buka kunci di menu Pengguna.', tuju: 'users' } : null,
    (data.perhatian?.harga_rendah || 0) > 0 ? { tingkat: 'peringatan', judul: `${data.perhatian.harga_rendah} produk harga jual ≤ HPP`, teks: 'Periksa dan naikkan harga jual.', tuju: 'harga' } : null,
    (stokHabis || 0) > 0 ? { tingkat: 'peringatan', judul: `${stokHabis} produk stok habis`, teks: 'Lakukan opname atau pembelian.', tuju: 'stok' } : null
  ].filter(Boolean) : []) as Perhatian[];
</script>

<div class="bd">
  {#if error}<div class="bd-alert bad" role="alert"><strong>Gagal memuat Beranda</strong><span>{error}</span><button class="bd-link" on:click={load}>Coba lagi</button></div>{/if}

  {#if loading && !data.tanggal}
    <div class="bd-skel" aria-busy="true" aria-label="Memuat Beranda"><i></i><i></i><i></i><i></i></div>
  {:else if data.tanggal}
    <header class="bd-head">
      <div>
        <p class="eyebrow">{user.role === 'ADMIN' ? 'ADMIN' : user.role === 'KASIR' ? 'KASIR' : 'ANGGOTA'}</p>
        <h2>Halo, {nama}</h2>
        <p class="muted">{tglPanjang(data.tanggal)}</p>
      </div>
      <div class="bd-head-side">
        <span class="bd-chip" class:ok={terbuka} class:bad={!terbuka}>{terbuka ? 'Periode terbuka' : 'Periode tidak terbuka'}{periodeChip ? ` · ${periodeChip}` : ''}</span>
        <button class="bd-link" on:click={load} disabled={loading}>{loading ? 'Memuat…' : 'Muat ulang'}</button>
      </div>
    </header>

    {#if !terbuka && user.role !== 'ADMIN'}
      <div class="bd-alert bad" role="status"><strong>{user.role === 'KASIR' ? 'Penjualan diblokir' : 'Belanja belum bisa dilakukan'}</strong><span>{pesanPeriode}</span><small>Hubungi admin untuk mengatur periode.</small></div>
    {/if}
    {#if data.belanja_demo}<div class="bd-alert warn" role="status"><strong>Mode demo</strong><span>Belanja mandiri hanya simulasi dan tidak tersimpan.</span></div>{/if}

    {#if user.role === 'KASIR'}
      <button class="bd-cta" disabled={!terbuka} on:click={() => dispatch('go', 'belanja')}>Mulai jual <span aria-hidden="true">→</span></button>
      <section class="bd-grid" aria-label="Ringkasan hari ini">
        <div class="bd-kpi hero"><span>Total penjualan hari ini</span><strong>{rp(r.total)}</strong></div>
        <div class="bd-kpi"><span>Transaksi</span><strong>{r.jumlah ?? 0}</strong></div>
        <div class="bd-kpi"><span>Rata-rata per transaksi</span><strong>{rp(rata)}</strong></div>
        <div class="bd-kpi wide">
          <span>Tunai vs kredit</span>
          <div class="bd-split" role="img" aria-label={`Tunai ${split[0]}%, kredit ${split[1]}%`}><i class="a" style={`width:${split[0]}%`}></i><i class="b" style={`width:${split[1]}%`}></i></div>
          <small class="bd-legend"><b class="a"></b>Tunai {rp(r.tunai)}<b class="b"></b>Kredit {rp(r.kredit)}</small>
        </div>
      </section>
    {:else if user.role === 'ANGGOTA'}
      {#if limit}
        <section class="bd-limit" class:hampir={limit.status === 'hampir'} class:lewat={limit.status === 'terlampaui'} aria-label="Limit belanja periode ini">
          <p class="eyebrow light">LIMIT BELANJA · PERIODE {r.periode}</p>
          {#if limit.status === 'tak-dibatasi'}
            <strong class="bd-big">Tidak dibatasi</strong>
            <p>Belanja periode ini {rp(r.belanja_periode)}</p>
          {:else}
            <span class="bd-sub">Sisa limit</span>
            <strong class="bd-big">{rp(Math.max(0, r.sisa_limit ?? 0))}</strong>
            <div class="bd-bar" role="progressbar" aria-valuemin="0" aria-valuemax="100" aria-valuenow={limit.persen} aria-label="Limit terpakai"><i style={`width:${limit.persen}%`}></i></div>
            <p>{rp(r.belanja_periode)} terpakai dari {rp(r.limit)} ({limit.persen}%)
              {#if limit.status === 'hampir'} · <b>Hampir habis</b>{:else if limit.status === 'terlampaui'} · <b>Limit terlampaui</b>{/if}</p>
          {/if}
        </section>
      {:else}
        <div class="bd-alert warn" role="status"><strong>Data limit belum tersedia</strong><span>Tanggal hari ini di luar periode yang diatur.</span></div>
      {/if}
      <div class="bd-actions">
        <button class="bd-cta" disabled={!terbuka} on:click={() => dispatch('go', 'belanja')}>Belanja mandiri <span aria-hidden="true">→</span></button>
        <button class="bd-cta alt" on:click={() => dispatch('go', 'kartu')}>Kartu anggota <span aria-hidden="true">▣</span></button>
      </div>
    {:else}
      <section aria-label="Perlu perhatian">
        <h3 class="bd-h">Perlu perhatian</h3>
        {#if perhatian.length}
          <div class="bd-attn">
            {#each perhatian as p}
              {#if p.tuju}
                <button class="bd-card attn {p.tingkat}" on:click={() => dispatch('go', p.tuju || '')}><strong>{p.judul}</strong><span>{p.teks}</span><b aria-hidden="true">→</b></button>
              {:else}
                <div class="bd-card attn {p.tingkat}" role="status"><strong>{p.judul}</strong><span>{p.teks}</span></div>
              {/if}
            {/each}
          </div>
        {:else}
          <p class="bd-ok" role="status">✓ Tidak ada yang perlu perhatian saat ini.</p>
        {/if}
      </section>
      <section class="bd-grid" aria-label="Ringkasan hari ini">
        <div class="bd-kpi hero"><span>Total penjualan hari ini (semua kasir)</span><strong>{rp(r.total)}</strong><em class={dTotal.arah}>{teksDelta(dTotal)}</em></div>
        <div class="bd-kpi"><span>Transaksi</span><strong>{r.jumlah ?? 0}</strong><em class={dJumlah.arah}>{teksDelta(dJumlah)}</em></div>
        <div class="bd-kpi"><span>Rata-rata per transaksi</span><strong>{rp(rata)}</strong></div>
        <div class="bd-kpi wide">
          <span>Tunai vs kredit</span>
          <div class="bd-split" role="img" aria-label={`Tunai ${split[0]}%, kredit ${split[1]}%`}><i class="a" style={`width:${split[0]}%`}></i><i class="b" style={`width:${split[1]}%`}></i></div>
          <small class="bd-legend"><b class="a"></b>Tunai {rp(r.tunai)}<b class="b"></b>Kredit {rp(r.kredit)}</small>
        </div>
      </section>
      {#if tren.length}
        <section class="bd-card" aria-label="Tren penjualan 7 hari">
          <h3 class="bd-h">Tren penjualan 7 hari</h3>
          <div class="bd-chart" role="img" aria-label={'Penjualan harian: ' + tren.map(t => `${tglPendek(t.tanggal)} ${rp(t.total)}`).join(', ')}>
            {#each tren as t, i}
              <div class="bd-col" class:today={i === tren.length - 1}>
                <small>{singkat(t.total)}</small>
                <div class="bd-barv"><i style={`height:${tinggiBatang(t.total, nilaiTren)}%`}></i></div>
                <span>{hariSingkat(t.tanggal)}</span>
              </div>
            {/each}
          </div>
        </section>
      {/if}
    {/if}

    {#if user.role === 'ADMIN'}
      <div class="bd-two">
        <section class="bd-card"><h3 class="bd-h">Penjualan per kasir</h3>
          {#each data.per_kasir || [] as k}<div class="bd-row"><span><strong>{k.nama ? `${k.nama} (${k.kasir})` : k.kasir}</strong><small>{k.jumlah} transaksi</small></span><strong>{rp(k.total)}</strong></div>{:else}<p class="empty">Belum ada transaksi hari ini.</p>{/each}
        </section>
        <section class="bd-card"><h3 class="bd-h">Barang terlaris hari ini</h3>
          {#each data.terlaris || [] as t}<div class="bd-row"><span><strong>{t.nama || t.kode}</strong><small>{t.kode} · terjual {t.qty}</small></span><strong>{rp(t.total)}</strong></div>{:else}<p class="empty">Belum ada penjualan hari ini.</p>{/each}
        </section>
      </div>
      <section class="bd-card"><h3 class="bd-h">Pengguna mobile</h3>
        <div class="bd-pills"><span>Admin {data.user?.admin ?? 0}</span><span>Kasir {data.user?.kasir ?? 0}</span><span>Anggota {data.user?.anggota ?? 0}</span><span class="mut">Nonaktif {data.user?.nonaktif ?? 0}</span><span class:warn={(data.user?.terkunci || 0) > 0}>Terkunci {data.user?.terkunci ?? 0}</span></div>
      </section>
    {:else}
      <section class="bd-card">
        <div class="bd-row-head"><h3 class="bd-h">Transaksi terakhir</h3>{#if user.role === 'ANGGOTA'}<button class="bd-link" on:click={() => dispatch('go', 'riwayat')}>Lihat semua →</button>{:else}<button class="bd-link" on:click={() => dispatch('go', 'riwayat')}>Riwayat →</button>{/if}</div>
        {#each data.terakhir || [] as t}
          <button class="bd-row btn" on:click={() => detail(t.no_transaksi)}>
            <span><strong>{t.nama_pelanggan || t.no_transaksi}</strong><small>{t.no_transaksi} · {t.tanggal ? `${tglPendek(t.tanggal)} ` : ''}{t.jam || ''}{t.jenis_bayar ? ` · ${t.jenis_bayar}` : ''}{t.tenor > 1 ? ` · ${t.tenor}x angsuran` : ''}</small></span>
            <strong>{rp(t.total)} <b aria-hidden="true">→</b></strong>
          </button>
        {:else}
          <p class="empty">{user.role === 'KASIR' ? 'Belum ada transaksi hari ini. Tekan "Mulai jual" untuk memulai.' : 'Belum ada transaksi pada periode ini.'}</p>
        {/each}
      </section>
    {/if}
  {/if}
</div>
{#if invoice}<Receipt {invoice} close={() => invoice = null}/>{/if}
