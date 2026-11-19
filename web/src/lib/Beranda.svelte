<script lang="ts">
  import ArrowRight from '@lucide/svelte/icons/arrow-right';
  import ChevronRight from '@lucide/svelte/icons/chevron-right';
  import IdCard from '@lucide/svelte/icons/id-card';
  import { createEventDispatcher, onMount } from 'svelte';
  import { api, query } from './api';
  import type { User, Data } from './types';
  import Receipt from './Receipt.svelte';
  import { delta, teksDelta, proporsi, statusLimit, persenTerpakai, tinggiBatang, singkat, hariSingkat, rentangPreset, validasiRentang, persenJenis, MAKS_HARI_GRAFIK, type Preset, type Rentang } from './beranda';

  export let user: User;
  const dispatch = createEventDispatcher<{ go: string }>();

  type Perhatian = { tingkat: 'bahaya' | 'peringatan'; judul: string; teks: string; tuju: string | null };

  let data: Data = {}, loading = true, error = '', invoice: Data | null = null, stokHabis: number | null = null;
  // Rincian penjualan (admin): rentang yang dipilih + hasilnya dimuat terpisah dari Beranda.
  let preset: Preset = 'hari', rincian: Data | null = null, rincianLoading = false, rincianError = '', rincianId = 0, bebasDari = '', bebasSampai = '';

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
      if (user.role === 'ADMIN') {
        // Stok habis berat dihitung server: dimuat terpisah agar Beranda tidak menunggu.
        api<Data>('/admin/dashboard/stok-habis').then(r => stokHabis = r.jumlah).catch(() => stokHabis = null);
        if (preset !== 'bebas') muatRincian({ ...(rentangPreset(preset, data.tanggal, data.periode_dari, data.periode_sampai) ?? rentangPreset('hari', data.tanggal)!) }); else if (rincian) muatRincian({ dari: rincian.dari, sampai: rincian.sampai });
      }
    } catch (e) { error = (e as Error).message; } finally { loading = false; }
  }
  onMount(load);

  async function muatRincian(r: Rentang) {
    const id = ++rincianId; rincianLoading = true; rincianError = '';
    try { const hasil = await api<Data>('/admin/penjualan/rincian' + query({ dari: r.dari, sampai: r.sampai })); if (id === rincianId) rincian = hasil; }
    catch (e) { if (id === rincianId) rincianError = (e as Error).message; }
    finally { if (id === rincianId) rincianLoading = false; }
  }
  function pilih(p: Preset) {
    preset = p; rincianError = '';
    if (p === 'bebas') { bebasDari = rincian?.dari || data.tanggal; bebasSampai = rincian?.sampai || data.tanggal; return; }
    const r = rentangPreset(p, data.tanggal, data.periode_dari, data.periode_sampai);
    if (r) muatRincian(r);
  }
  function terapkanBebas() {
    const r = { dari: bebasDari, sampai: bebasSampai };
    const galat = validasiRentang(r, data.tanggal);
    if (galat) { rincianError = galat; return; }
    muatRincian(r);
  }

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
  $: adaPeriode = !!rentangPreset('periode', data.tanggal || '', data.periode_dari, data.periode_sampai);
  $: rc = rincian?.ringkasan;
  $: harian = (rincian?.harian || []) as Data[];
  $: totalHarian = harian.map(h => h.tunai + h.kredit + h.lain);
  $: adaLain = (rc?.lain?.total || 0) > 0 || (rc?.lain?.jumlah || 0) > 0;
  $: perhatian = (user.role === 'ADMIN' && data.tanggal ? [
    !terbuka ? { tingkat: 'bahaya', judul: 'Periode tidak terbuka', teks: pesanPeriode, tuju: null } : null,
    (data.user?.terkunci || 0) > 0 ? { tingkat: 'peringatan', judul: `${data.user.terkunci} akun terkunci`, teks: 'Buka kunci di menu Pengguna.', tuju: 'users' } : null,
    (data.perhatian?.harga_rendah || 0) > 0 ? { tingkat: 'peringatan', judul: `${data.perhatian.harga_rendah} produk harga jual ≤ HPP`, teks: 'Periksa dan naikkan harga jual.', tuju: 'harga' } : null,
    (stokHabis || 0) > 0 ? { tingkat: 'peringatan', judul: `${stokHabis} produk stok habis`, teks: 'Lakukan opname atau pembelian.', tuju: 'stok' } : null
  ].filter(Boolean) : []) as Perhatian[];
  const labelPreset: [Preset, string][] = [['hari', 'Hari ini'], ['tujuh', '7 hari'], ['periode', 'Periode berjalan'], ['bebas', 'Pilih tanggal']];
  const labelHarian = (tgl: string, n: number) => n <= 7 ? hariSingkat(tgl) : tgl.slice(8);
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
      <button class="bd-cta" disabled={!terbuka} on:click={() => dispatch('go', 'belanja')}>Mulai jual <ArrowRight size={20} aria-hidden="true"/></button>
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
        <button class="bd-cta" disabled={!terbuka} on:click={() => dispatch('go', 'belanja')}>Belanja mandiri <ArrowRight size={20} aria-hidden="true"/></button>
        <button class="bd-cta alt" on:click={() => dispatch('go', 'kartu')}>Kartu anggota <IdCard size={20} aria-hidden="true"/></button>
      </div>
    {:else}
      <section aria-label="Perlu perhatian">
        <h3 class="bd-h">Perlu perhatian</h3>
        {#if perhatian.length}
          <div class="bd-attn">
            {#each perhatian as p}
              {#if p.tuju}
                <button class="bd-card attn {p.tingkat}" on:click={() => dispatch('go', p.tuju || '')}><strong>{p.judul}</strong><span>{p.teks}</span><ChevronRight size={18} aria-hidden="true"/></button>
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

      <section class="bd-card" aria-label="Rincian penjualan" aria-busy={rincianLoading}>
        <h3 class="bd-h">Rincian penjualan</h3>
        <div class="bd-range" role="radiogroup" aria-label="Rentang waktu">
          {#each labelPreset as [k, l]}
            <button class="bd-pill" class:on={preset === k} role="radio" aria-checked={preset === k} disabled={k === 'periode' && !adaPeriode} on:click={() => pilih(k)}>{l}</button>
          {/each}
        </div>
        {#if preset === 'bebas'}
          <form class="bd-dates" on:submit|preventDefault={terapkanBebas}>
            <label>Dari<input type="date" bind:value={bebasDari} max={data.tanggal} required/></label>
            <label>Sampai<input type="date" bind:value={bebasSampai} max={data.tanggal} required/></label>
            <button class="secondary">Terapkan</button>
          </form>
        {/if}
        {#if rincianError}<div class="bd-alert bad" role="alert">{rincianError}</div>{/if}
        {#if rincianLoading && !rincian}<div class="bd-skel" aria-label="Memuat rincian"><i></i><i></i></div>{/if}
        {#if rincian && rc}
          <p class="muted bd-range-info">{tglPendek(rincian.dari)}{rincian.hari > 1 ? ` – ${tglPendek(rincian.sampai)}` : ''} · {rincian.hari} hari · {rc.total.jumlah} transaksi · <strong>{rp(rc.total.total)}</strong>{rincianLoading ? ' · memuat…' : ''}</p>
          {#if rc.total.jumlah === 0}
            <p class="empty">Belum ada transaksi pada rentang ini.</p>
          {:else}
            <div class="bd-jenis">
              <div class="bd-jenis-kartu a"><span>Tunai</span><strong>{rp(rc.tunai.total)}</strong><small>{rc.tunai.jumlah} transaksi · {persenJenis(rc.tunai.total, rc.total.total)}%</small></div>
              <div class="bd-jenis-kartu b"><span>Kredit</span><strong>{rp(rc.kredit.total)}</strong><small>{rc.kredit.jumlah} transaksi · {persenJenis(rc.kredit.total, rc.total.total)}%</small></div>
              {#if adaLain}<div class="bd-jenis-kartu l"><span>Lainnya</span><strong>{rp(rc.lain.total)}</strong><small>{rc.lain.jumlah} transaksi · {persenJenis(rc.lain.total, rc.total.total)}%</small></div>{/if}
            </div>
            <div class="bd-split" role="img" aria-label={`Tunai ${persenJenis(rc.tunai.total, rc.total.total)}%, kredit ${persenJenis(rc.kredit.total, rc.total.total)}%`}><i class="a" style={`width:${rc.total.total ? (rc.tunai.total / rc.total.total) * 100 : 0}%`}></i><i class="b" style={`width:${rc.total.total ? (rc.kredit.total / rc.total.total) * 100 : 0}%`}></i><i class="l" style={`width:${rc.total.total ? (rc.lain.total / rc.total.total) * 100 : 0}%`}></i></div>

            {#if harian.length > 1 && harian.length <= MAKS_HARI_GRAFIK}
              <h4 class="bd-h2">Tren harian</h4>
              <div class="bd-stack" role="img" aria-label={'Penjualan harian tunai/kredit: ' + harian.map(h => `${tglPendek(h.tanggal)} tunai ${rp(h.tunai)}, kredit ${rp(h.kredit)}`).join('; ')}>
                {#each harian as h, i}
                  <div class="bd-scol">
                    <div class="bd-sbar">{#if totalHarian[i] > 0}<div class="bd-sfill" style={`height:${tinggiBatang(totalHarian[i], totalHarian)}%`} title={`${tglPendek(h.tanggal)}: ${rp(totalHarian[i])}`}><i class="l" style={`flex:${h.lain}`}></i><i class="b" style={`flex:${h.kredit}`}></i><i class="a" style={`flex:${h.tunai}`}></i></div>{/if}</div>
                    <span>{labelHarian(h.tanggal, harian.length)}</span>
                  </div>
                {/each}
              </div>
              <small class="bd-legend"><b class="a"></b>Tunai<b class="b"></b>Kredit{#if adaLain}<b class="l"></b>Lainnya{/if}</small>
            {:else if harian.length > MAKS_HARI_GRAFIK}
              <p class="muted">Grafik harian ditampilkan untuk rentang ≤ {MAKS_HARI_GRAFIK} hari.</p>
            {/if}

            <h4 class="bd-h2">Per kasir</h4>
            <div class="bd-tablewrap">
              <table class="bd-table">
                <caption class="sr-only">Penjualan per kasir, tunai dan kredit</caption>
                <thead><tr><th scope="col">Kasir</th><th scope="col" class="n">Tunai</th><th scope="col" class="n">Kredit</th>{#if adaLain}<th scope="col" class="n">Lainnya</th>{/if}<th scope="col" class="n">Total</th><th scope="col" class="n">Trx</th></tr></thead>
                <tbody>
                  {#each rincian.per_kasir as k}
                    <tr>
                      <th scope="row" data-l="Kasir">{k.nama || k.kasir}{#if k.nama}<small>{k.kasir}</small>{/if}</th>
                      <td class="n" data-l="Tunai">{rp(k.tunai.total)}<small>{k.tunai.jumlah} trx</small></td>
                      <td class="n" data-l="Kredit">{rp(k.kredit.total)}<small>{k.kredit.jumlah} trx</small></td>
                      {#if adaLain}<td class="n" data-l="Lainnya">{rp(k.lain.total)}<small>{k.lain.jumlah} trx</small></td>{/if}
                      <td class="n tot" data-l="Total">{rp(k.total.total)}</td>
                      <td class="n" data-l="Trx">{k.total.jumlah}</td>
                    </tr>
                  {/each}
                </tbody>
                <tfoot>
                  <tr>
                    <th scope="row" data-l="Kasir">Total</th>
                    <td class="n" data-l="Tunai">{rp(rc.tunai.total)}<small>{rc.tunai.jumlah} trx</small></td>
                    <td class="n" data-l="Kredit">{rp(rc.kredit.total)}<small>{rc.kredit.jumlah} trx</small></td>
                    {#if adaLain}<td class="n" data-l="Lainnya">{rp(rc.lain.total)}<small>{rc.lain.jumlah} trx</small></td>{/if}
                    <td class="n tot" data-l="Total">{rp(rc.total.total)}</td>
                    <td class="n" data-l="Trx">{rc.total.jumlah}</td>
                  </tr>
                </tfoot>
              </table>
            </div>
            {#if rincian.per_kasir.length >= 50}<p class="muted">Menampilkan 50 kasir dengan penjualan terbesar.</p>{/if}
          {/if}
        {/if}
      </section>
    {/if}

    {#if user.role === 'ADMIN'}
      <div class="bd-two">
        <section class="bd-card"><h3 class="bd-h">Barang terlaris hari ini</h3>
          {#each data.terlaris || [] as t}<div class="bd-row"><span><strong>{t.nama || t.kode}</strong><small>{t.kode} · terjual {t.qty}</small></span><strong>{rp(t.total)}</strong></div>{:else}<p class="empty">Belum ada penjualan hari ini.</p>{/each}
        </section>
        <section class="bd-card"><h3 class="bd-h">Pengguna mobile</h3>
          <div class="bd-pills"><span>Admin {data.user?.admin ?? 0}</span><span>Kasir {data.user?.kasir ?? 0}</span><span>Anggota {data.user?.anggota ?? 0}</span><span class="mut">Nonaktif {data.user?.nonaktif ?? 0}</span><span class:warn={(data.user?.terkunci || 0) > 0}>Terkunci {data.user?.terkunci ?? 0}</span></div>
        </section>
      </div>
    {:else}
      <section class="bd-card">
        <div class="bd-row-head"><h3 class="bd-h">Transaksi terakhir</h3>{#if user.role === 'ANGGOTA'}<button class="bd-link" on:click={() => dispatch('go', 'riwayat')}>Lihat semua <ArrowRight size={14} class="chev" aria-hidden="true"/></button>{:else}<button class="bd-link" on:click={() => dispatch('go', 'riwayat')}>Riwayat <ArrowRight size={14} class="chev" aria-hidden="true"/></button>{/if}</div>
        {#each data.terakhir || [] as t}
          <button class="bd-row btn" on:click={() => detail(t.no_transaksi)}>
            <span><strong>{t.nama_pelanggan || t.no_transaksi}</strong><small>{t.no_transaksi} · {t.tanggal ? `${tglPendek(t.tanggal)} ` : ''}{t.jam || ''}{t.jenis_bayar ? ` · ${t.jenis_bayar}` : ''}{t.tenor > 1 ? ` · ${t.tenor}x angsuran` : ''}</small></span>
            <strong>{rp(t.total)} <ChevronRight size={18} aria-hidden="true"/></strong>
          </button>
        {:else}
          <p class="empty">{user.role === 'KASIR' ? 'Belum ada transaksi hari ini. Tekan "Mulai jual" untuk memulai.' : 'Belum ada transaksi pada periode ini.'}</p>
        {/each}
      </section>
    {/if}
  {/if}
</div>
{#if invoice}<Receipt {invoice} close={() => invoice = null}/>{/if}
