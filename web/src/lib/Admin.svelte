<script lang="ts">
  import { statusHarga } from './beranda';
  import type { IsianProduk } from './katalog';
  import { badanProduk, badanToggleAktif, cekProdukBaru, inisial, paramKatalog, produkBaru } from './katalog';
  import IconButton from './IconButton.svelte';
  import ProdukDrawer from './ProdukDrawer.svelte';
  import Photo from './Photo.svelte';
  import Search from '@lucide/svelte/icons/search';
  import Pencil from '@lucide/svelte/icons/pencil';
  import ImagePlus from '@lucide/svelte/icons/image-plus';
  import Power from '@lucide/svelte/icons/power';
  import Trash2 from '@lucide/svelte/icons/trash-2';
  import X from '@lucide/svelte/icons/x';
  import ArrowUp from '@lucide/svelte/icons/arrow-up';
  import ArrowDown from '@lucide/svelte/icons/arrow-down';
  import ChevronRight from '@lucide/svelte/icons/chevron-right';
  import { tick } from 'svelte';
  import Plus from '@lucide/svelte/icons/plus';
  import { api, query } from './api';
  import type { Data } from './types';
  import Receipt from './Receipt.svelte';
  export let view: string;
  let error = '', message = '', loading = false, products: Data[] = [], categories: Data[] = [], chosen: string[] = [], periods: Data[] = [], report: Data = {}, member: Data | null = null, memberItems: Data[] = [], invoice: Data | null = null;
  let aktifFilter: '' | 'Y' | 'N' = '', katFilter = '', hapus: Data | null = null;
  let edit: Data | null = null, search = '', period = '', page = 1, more = false;
  let lastView = '', orderCategory = '', memberPage = 1, memberMore = false;
  let hargaTotal = 0, hargaRugi = 0, hargaTanpa = 0, saving = false;
  const rp = (n: number) => new Intl.NumberFormat('id-ID', { style: 'currency', currency: 'IDR', maximumFractionDigits: 0 }).format(n || 0);
  type KtTab = 'produk' | 'kategori' | 'satuan' | 'urut';
  let ktTab: KtTab = 'produk';
  const ktTabs: { id: KtTab; label: string }[] = [{ id: 'produk', label: 'Produk' }, { id: 'kategori', label: 'Kategori' }, { id: 'satuan', label: 'Satuan' }, { id: 'urut', label: 'Urutan produk' }];
  $: sub = view === 'produk' ? ktTab : view;
  $: if (sub !== lastView) { lastView = sub; products = []; categories = []; search = ''; page = 1; more = false; message = ''; katFilter = ''; orderCategory = ''; edit = null; hapus = null; if (typeof window !== 'undefined') load(); }
  async function pilihKtTab(target: KtTab) { ktTab = target; await tick(); document.getElementById('kt-tab-' + target)?.focus(); }
  function tombolKtTab(e: KeyboardEvent, i: number) { const arah = e.key === 'ArrowLeft' ? -1 : e.key === 'ArrowRight' ? 1 : 0; if (!arah && e.key !== 'Home' && e.key !== 'End') return; e.preventDefault(); const index = e.key === 'Home' ? 0 : e.key === 'End' ? ktTabs.length - 1 : (i + arah + ktTabs.length) % ktTabs.length; void pilihKtTab(ktTabs[index].id); }
  function escapeAdmin(e: KeyboardEvent) {
    if (e.key !== 'Escape') return;
    if (katHapus && !katHapus.proses) katHapus = null;
    else if (katEdit && !katSaving) katEdit = null;
    else if (hapus && !hapus.proses) hapus = null;
    else if (invoice) invoice = null;
    else if (member) member = null;
  }
  async function load(next = false) { loading = true; error = ''; try {
    if (sub === 'produk') { const p = next ? page + 1 : 1; const result = await api<Data>('/admin/produk' + query(paramKatalog(search, aktifFilter, katFilter, p))); products = next ? [...products, ...result.items] : result.items; page = p; more = result.ada_lagi; if (!categories.length) categories = await api('/admin/kategori'); if (!masterSatuan.length) masterSatuan = await api('/admin/satuan'); }
    if (sub === 'harga') { const p = next ? page + 1 : 1; const result = await api<Data>('/admin/produk' + query({ q: search, page: p, size: 100, harga_rendah: 'Y' })); products = next ? [...products, ...result.items] : result.items; page = p; more = result.ada_lagi; if (result.jumlah != null) { hargaTotal = result.jumlah; hargaRugi = result.rugi ?? 0; hargaTanpa = result.tanpa_hpp ?? 0; } categories = await api('/admin/kategori'); }
    if (sub === 'urut') { categories = await api('/admin/kategori'); const visible = await api<string[]>('/admin/kategori-tampil'); if (visible.length) categories = categories.filter(c => visible.includes(c.id)); if (!orderCategory || !categories.some(c => c.id === orderCategory)) orderCategory = categories[0]?.id || ''; products = []; more = false; if (orderCategory) { for (let p = 1; p <= 10; p++) { const result = await api<Data>('/admin/produk' + query({ kategori: orderCategory, aktif: 'Y', page: p, size: 100 })); products = [...products, ...result.items]; more = result.ada_lagi; if (!more) break; } } }
    if (sub === 'satuan') { masterSatuan = await api('/admin/satuan'); }
    if (sub === 'kategori') { categories = await api('/admin/kategori'); chosen = await api('/admin/kategori-tampil'); }
    if (sub === 'rekap') { const ps = await api<Data>('/admin/periode'); periods = ps.items; if (!period && (ps.sekarang || ps.items[0])) period = String(ps.sekarang || ps.items[0].periode); report = await api('/admin/rekap-anggota' + query({ periode: period })); }
  } catch (e) { error = (e as Error).message; } finally { loading = false; } }
  async function run(f: () => Promise<unknown>, notice = 'Berhasil disimpan.') { error = ''; try { await f(); message = notice; await load(); } catch (e) { error = (e as Error).message; } }
  let satuanFor = 0, satuanList: { satuan: string; isi?: number; harga?: number; barcode: string }[] = [];
  $: if (edit && edit.id && edit.id !== satuanFor) muatSatuan(edit.id);
  let newKey = '';
  function tambahProduk() { error = ''; message = ''; newKey = crypto.randomUUID(); edit = produkBaru(categories[0]?.id ?? ''); satuanFor = 0; satuanList = []; }
  let satuanSiap = false;
  async function muatSatuan(id: number) {
    const produk = edit;
    satuanFor = id;
    satuanList = [];
    satuanSiap = false;
    error = '';
    try {
      const r = await api<{ satuan: { satuan: string; isi: string; harga: string; barcode: string }[] }>('/admin/produk/' + id + '/satuan');
      if (edit !== produk) return;
      satuanList = r.satuan.map(s => ({ ...s, isi: Number(s.isi), harga: Number(s.harga), barcode: s.barcode ?? '' }));
      satuanSiap = true;
    } catch (e) {
      if (edit === produk) error = (e as Error).message;
    }
  }
  function badanSatuan() {
    return satuanList.map(s => ({ satuan: s.satuan.trim(), isi: s.isi, harga: s.harga, barcode: (s.barcode ?? '').trim() || null }));
  }
  async function saveProduct() { if (!edit || saving) return; const p = edit;
    if (!p.id) {
      const salah = cekProdukBaru(p as IsianProduk);
      if (salah) { error = salah; return; }
      saving = true;
      error = '';
      try {
        const body = { ...badanProduk(p as IsianProduk), satuan_lain: badanSatuan() };
        await api('/admin/produk', { method: 'POST', headers: { 'Idempotency-Key': newKey }, body });
        message = 'Produk ' + body.nama + ' ditambahkan.';
        edit = null;
        await load();
      } catch (e) {
        error = (e as Error).message;
      } finally {
        saving = false;
      }
      return;
    }
    saving = true; error = '';
    try {
      await api('/admin/produk/' + p.id, { method: 'PUT', body: { nama: p.nama, barcode: p.barcode, satuan: p.satuan, kategori_id: p.kategori_id, harga: Number(p.harga), beli: Number(p.beli), aktif: p.aktif } });
      if (satuanSiap) {
        try { await api('/admin/produk/' + p.id + '/satuan', { method: 'PUT', body: { satuan: badanSatuan() } }); }
        catch (e) { error = 'Data produk tersimpan, tetapi satuan gagal disimpan: ' + (e as Error).message; await load(); return; }
      }
      message = 'Produk ' + p.nama.trim() + ' disimpan.';
      tutupProduk();
      await load();
    } catch (e) { error = (e as Error).message; }
    finally { saving = false; }
  }
  function tutupProduk() { edit = null; satuanFor = 0; }
  async function unggahFoto(file: File) {
    if (!edit) return;
    await run(() => api('/admin/produk/' + edit!.id + '/foto', { method: 'PUT', body: file, headers: { 'Content-Type': 'application/octet-stream' } }), 'Foto disimpan.');
  }
  async function bukaHapus(p: Data) { hapus = { id: p.id, nama: p.nama, kode: p.kode, memuat: true, bisa: false, rincian: '', galat: '', proses: false }; try { const j = await api<Data>('/admin/produk/' + p.id + '/jejak'); hapus = { ...hapus, memuat: false, bisa: j.bisa_hapus, rincian: j.rincian }; } catch (e) { hapus = { ...hapus, memuat: false, galat: (e as Error).message }; } }
  async function konfirmHapus() { if (!hapus) return; const h = hapus; hapus = { ...h, proses: true, galat: '' }; try { await api('/admin/produk/' + h.id, { method: 'DELETE' }); products = products.filter(x => x.id !== h.id); message = `Produk "${h.nama}" dihapus permanen.`; hapus = null; } catch (e) { hapus = { ...h, proses: false, galat: (e as Error).message }; } }
  let katEdit: { id?: string; nama: string } | null = null, katSaving = false, katError = '';
  let katHapus: { id: string; nama: string; memuat: boolean; bisa: boolean; jumlah: number; galat: string; proses: boolean } | null = null;
  let masterSatuan: Data[] = [];
  let stEdit: { id?: string; nama: string } | null = null, stSaving = false, stError = '';
  let stHapus: { id: string; nama: string; memuat: boolean; bisa: boolean; jumlah: number; galat: string; proses: boolean } | null = null;
  function tambahSatuanMaster() { stError = ''; stEdit = { nama: '' }; }
  async function simpanSatuanMaster() {
    if (!stEdit || stSaving) return;
    const e = stEdit, nama = e.nama.trim().toUpperCase();
    if (!nama) { stError = 'Nama satuan wajib diisi.'; return; }
    stSaving = true; stError = '';
    try {
      if (e.id) await api('/admin/satuan/' + encodeURIComponent(e.id), { method: 'PUT', body: { nama } });
      else await api('/admin/satuan', { method: 'POST', body: { nama } });
      message = e.id ? `Satuan "${nama}" diubah.` : `Satuan "${nama}" ditambahkan.`;
      stEdit = null;
      await load();
    } catch (err) { stError = (err as Error).message; }
    finally { stSaving = false; }
  }
  async function bukaHapusSatuanMaster(c: Data) {
    stHapus = { id: c.id, nama: c.nama, memuat: true, bisa: false, jumlah: 0, galat: '', proses: false };
    try { const j = await api<Data>('/admin/satuan/' + encodeURIComponent(c.id) + '/jejak'); stHapus = { ...stHapus, memuat: false, bisa: j.bisa_hapus, jumlah: j.jumlah_produk }; }
    catch (e) { stHapus = { ...stHapus, memuat: false, galat: (e as Error).message }; }
  }
  async function konfirmHapusSatuanMaster() {
    if (!stHapus) return;
    const h = stHapus; stHapus = { ...h, proses: true, galat: '' };
    try {
      await api('/admin/satuan/' + encodeURIComponent(h.id), { method: 'DELETE' });
      message = `Satuan "${h.nama}" dihapus.`;
      stHapus = null;
      await load();
    } catch (e) { stHapus = { ...h, proses: false, galat: (e as Error).message }; }
  }
  function tambahKategori() { katError = ''; katEdit = { nama: '' }; }
  async function simpanKategori() {
    if (!katEdit || katSaving) return;
    const e = katEdit, nama = e.nama.trim();
    if (!nama) { katError = 'Nama kategori wajib diisi.'; return; }
    katSaving = true; katError = '';
    try {
      if (e.id) await api('/admin/kategori/' + encodeURIComponent(e.id), { method: 'PUT', body: { nama } });
      else await api('/admin/kategori', { method: 'POST', body: { nama } });
      message = e.id ? `Kategori "${nama}" diubah.` : `Kategori "${nama}" ditambahkan.`;
      katEdit = null;
      await load();
    } catch (err) { katError = (err as Error).message; }
    finally { katSaving = false; }
  }
  async function bukaHapusKategori(c: Data) {
    katHapus = { id: c.id, nama: c.nama, memuat: true, bisa: false, jumlah: 0, galat: '', proses: false };
    try { const j = await api<Data>('/admin/kategori/' + encodeURIComponent(c.id) + '/jejak'); katHapus = { ...katHapus, memuat: false, bisa: j.bisa_hapus, jumlah: j.jumlah_produk }; }
    catch (e) { katHapus = { ...katHapus, memuat: false, galat: (e as Error).message }; }
  }
  async function konfirmHapusKategori() {
    if (!katHapus) return;
    const h = katHapus; katHapus = { ...h, proses: true, galat: '' };
    try {
      await api('/admin/kategori/' + encodeURIComponent(h.id), { method: 'DELETE' });
      chosen = chosen.filter(id => id !== h.id);
      message = `Kategori "${h.nama}" dihapus.`;
      katHapus = null;
      await load();
    } catch (e) { katHapus = { ...h, proses: false, galat: (e as Error).message }; }
  }
  async function ubahFoto(e: Event, id: number) { const el = e.currentTarget as HTMLInputElement; await upload(e, id); el.value = ''; }
  async function upload(e: Event, id: number) { const file = (e.currentTarget as HTMLInputElement).files?.[0]; if (file) await run(() => api('/admin/produk/' + id + '/foto', {method:'PUT',body:file,headers:{'Content-Type':'application/octet-stream'}}), 'Foto disimpan.'); }
  function move(i: number, delta: number) { const j = i + delta; if (j < 0 || j >= products.length) return; [products[i], products[j]] = [products[j], products[i]]; products = [...products]; }
  async function openMember(m: Data, next = false) { try { member = m; const p = next ? memberPage + 1 : 1; const result = await api<Data>('/admin/rekap-anggota/' + encodeURIComponent(m.nik) + '/transaksi' + query({periode:period,page:p,size:100})); memberItems = next ? [...memberItems, ...result.items] : result.items; memberPage = p; memberMore = result.ada_lagi; } catch(e) { error = (e as Error).message; } }
  async function showInvoice(no: string) { try { invoice = await api('/admin/faktur/' + encodeURIComponent(no)); } catch(e) { error = (e as Error).message; } }
</script>
<svelte:window on:keydown={escapeAdmin} />
{#if error && !edit}<div class="error" role="alert">{error}</div>{/if}{#if message}<div class="notice">{message}</div>{/if}{#if loading}<p class="muted">Memuat data…</p>{/if}
<section class="panel"><div class="section-heading"><div><p class="eyebrow">ADMINISTRASI</p><h2>{view === 'produk' ? 'Katalog produk' : view === 'harga' ? 'Harga jual ≤ HPP' : 'Rekap anggota'}</h2></div></div>
{#if view === 'produk'}<div class="kt-tab" role="tablist" aria-label="Katalog produk">{#each ktTabs as t, i (t.id)}<button type="button" id={'kt-tab-' + t.id} role="tab" aria-selected={ktTab === t.id} aria-controls={'kt-panel-' + t.id} tabindex={ktTab === t.id ? 0 : -1} on:keydown={(e) => tombolKtTab(e, i)} on:click={() => pilihKtTab(t.id)}>{t.label}</button>{/each}</div>{/if}
{#if sub === 'produk' || sub === 'urut'}<div id={view === 'produk' ? 'kt-panel-' + sub : undefined} role={view === 'produk' ? 'tabpanel' : undefined} aria-labelledby={view === 'produk' ? 'kt-tab-' + sub : undefined}>{#if sub === 'produk'}<div class="kt-bar"><form class="kt-search" on:submit|preventDefault={() => load()}><label><span class="sr-only">Cari produk</span><Search size={18} aria-hidden="true"/><input type="search" bind:value={search} placeholder="Cari nama, kode, atau barcode"/></label><button class="primary" style="min-height:44px">Cari</button></form><div class="kt-filter" role="group" aria-label="Filter produk">{#each [['', 'Semua'], ['Y', 'Aktif'], ['N', 'Nonaktif']] as [v, t]}<button type="button" class="kt-chip" aria-pressed={aktifFilter === v} on:click={() => { aktifFilter = v as '' | 'Y' | 'N'; load(); }}>{t}</button>{/each}<select aria-label="Kategori" bind:value={katFilter} on:change={() => load()}><option value="">Semua kategori</option>{#each categories as c}<option value={c.id}>{c.nama}</option>{/each}</select></div><button type="button" class="primary" style="min-height:44px" on:click={tambahProduk}>Tambah produk</button></div>{:else}<label>Kategori<select bind:value={orderCategory} on:change={() => load()}>{#each categories as c}<option value={c.id}>{c.nama}</option>{/each}</select></label><p class="muted">Urutkan seluruh produk aktif dalam kategori ini.</p>{#if more}<div class="error">Kategori memiliki lebih dari 1.000 produk dan tidak dapat diurutkan di sini.</div>{/if}{/if}{#if sub === 'produk'}{#each products as p (p.id)}<div class="kt-row" class:off={!p.aktif}><div class="kt-ava" aria-hidden="true">{#if p.foto_versi}{#key p.id + ':' + p.foto_versi}<Photo path={'/produk/' + p.id + '/foto'} alt="" fallback={inisial(p.nama)}/>{/key}{:else}{inisial(p.nama)}{/if}</div><div class="kt-main"><strong>{p.nama}</strong><small><span class="kt-pill" class:off={!p.aktif}>{p.aktif ? 'Aktif' : 'Nonaktif'}</span>{p.kode} · {p.kategori} · {p.satuan}</small><span class="kt-price">{rp(p.harga)}</span></div><div class="kt-act"><IconButton icon={Pencil} label="Ubah {p.nama}" hint="Ubah" on:click={() => edit = {...p}}/><label class="ibtn" title="Ganti foto" aria-label="Ganti foto {p.nama}"><ImagePlus size={18} aria-hidden="true"/><input type="file" accept="image/jpeg,image/png" on:change={(e) => ubahFoto(e, p.id)}/></label><IconButton icon={Power} label="{p.aktif ? 'Nonaktifkan' : 'Aktifkan'} {p.nama}" hint={p.aktif ? 'Nonaktifkan' : 'Aktifkan'} on:click={() => run(() => api('/admin/produk/' + p.id, { method: 'PUT', body: badanToggleAktif(p as never) }), p.aktif ? 'Produk dinonaktifkan.' : 'Produk diaktifkan.')}/><IconButton icon={Trash2} danger label="Hapus permanen {p.nama}" hint="Hapus permanen" on:click={() => bukaHapus(p)}/></div></div>{:else}{#if !loading}<p class="muted">Tidak ada produk yang cocok.</p>{/if}{/each}{:else}{#each products as p, i}<div class="admin-row"><div><strong>{p.nama}</strong><small>{p.kode} · {p.kategori} · {rp(p.harga)} · {p.aktif ? 'Aktif' : 'Nonaktif'}</small></div><div class="actions"><button aria-label="Naikkan" on:click={() => move(i,-1)} disabled={i === 0}><ArrowUp size={16} aria-hidden="true"/></button><button aria-label="Turunkan" on:click={() => move(i,1)} disabled={i === products.length-1}><ArrowDown size={16} aria-hidden="true"/></button></div></div>{/each}{/if}{#if more && sub === 'produk'}<button class="secondary" on:click={() => load(true)}>Muat lebih banyak</button>{/if}{#if sub === 'urut' && products.length && !more}<button class="primary" on:click={() => run(() => api('/admin/produk-urut',{method:'PUT',body:{produk_ids:products.map(p => p.id)}}))}>Simpan urutan</button>{/if}</div>
{:else if sub === 'harga'}<form class="search" on:submit|preventDefault={() => load()}><input bind:value={search} placeholder="Cari produk"/><button>Cari</button></form><p class="muted">{hargaRugi} produk harga jual ≤ HPP · {hargaTanpa} produk belum ada harga beli (HPP belum diisi). Yang rugi tampil lebih dulu.</p>{#each products as p}{@const sh = statusHarga(p.harga, p.beli)}<div class="admin-row"><div><strong>{p.nama}</strong><small>{p.kode} · {p.kategori} · jual {rp(p.harga)} · {#if sh.tipe === 'tanpa-hpp'}<span class="tanpa-hpp" style="color: #8a5a00; font-weight: 700">HPP belum diisi</span>{:else}beli {rp(p.beli)} · margin <span class="margin-neg" style="color: var(--danger, #b3261e)">{rp(sh.margin)}</span>{/if}</small></div><div class="actions"><button on:click={() => edit = {...p}}>Ubah</button></div></div>{:else}{#if !loading}<p class="muted">Tidak ada produk dengan harga jual ≤ HPP.</p>{/if}{/each}{#if more}<button class="secondary" on:click={() => load(true)}>Muat lebih banyak</button>{/if}
{:else if sub === 'satuan'}<div id="kt-panel-satuan" role="tabpanel" aria-labelledby="kt-tab-satuan"><div class="kt-bar"><p class="muted" style="margin:0">Kelola satuan produk. Semua satuan produk dipilih dari daftar ini. Satuan yang masih dipakai produk tidak dapat dihapus.</p><button type="button" class="primary" style="min-height:44px" on:click={tambahSatuanMaster}><Plus size={18} aria-hidden="true"/> Tambah satuan</button></div>{#each masterSatuan as c (c.id)}<div class="admin-row"><div><strong>{c.nama}</strong></div><div style="display:flex;gap:5px"><IconButton icon={Pencil} label="Ubah {c.nama}" hint="Ubah" on:click={() => { stError = ''; stEdit = { id: c.id, nama: c.nama }; }}/><IconButton icon={Trash2} danger label="Hapus {c.nama}" hint="Hapus" on:click={() => bukaHapusSatuanMaster(c)}/></div></div>{:else}{#if !loading}<p class="muted">Belum ada satuan.</p>{/if}{/each}</div>
{:else if sub === 'kategori'}<div id="kt-panel-kategori" role="tabpanel" aria-labelledby="kt-tab-kategori"><div class="kt-bar"><p class="muted" style="margin:0">Kelola kategori produk. Kategori yang masih dipakai produk tidak dapat dihapus.</p><button type="button" class="primary" style="min-height:44px" on:click={tambahKategori}><Plus size={18} aria-hidden="true"/> Tambah kategori</button></div>{#each categories as c (c.id)}<div class="admin-row"><div><strong>{c.nama}</strong><small>Kode {c.id}</small></div><div style="display:flex;gap:5px"><IconButton icon={Pencil} label="Ubah {c.nama}" hint="Ubah" on:click={() => { katError = ''; katEdit = { id: c.id, nama: c.nama }; }}/><IconButton icon={Trash2} danger label="Hapus {c.nama}" hint="Hapus" on:click={() => bukaHapusKategori(c)}/></div></div>{:else}{#if !loading}<p class="muted">Belum ada kategori.</p>{/if}{/each}<h3>Kategori tampil di kasir</h3><p class="muted">Jika tidak ada kategori yang dipilih, semua kategori ditampilkan.</p>{#each categories as c}<label class="check"><input type="checkbox" value={c.id} bind:group={chosen}/>{c.nama}</label>{/each}<button class="primary" on:click={() => run(() => api('/admin/kategori-tampil',{method:'PUT',body:{kategori_ids:chosen}}))}>Simpan kategori tampil</button></div>
{:else if sub === 'rekap'}<label>Periode<select bind:value={period} on:change={() => load()}>{#each periods as p}<option value={String(p.periode)}>{p.periode} · {p.dari} – {p.sampai}</option>{/each}</select></label><p>Total: <strong>{rp(report.total)}</strong> · {report.jumlah_transaksi || 0} transaksi</p>{#each report.items || [] as m}<button class="list-button horizontal" on:click={() => openMember(m)}><span><strong>{m.nama}</strong><small>{m.nik} · {m.unit} · {m.jumlah} transaksi</small></span><strong>{rp(m.total)} <ChevronRight size={16} class="chev" aria-hidden="true"/></strong></button>{/each}{/if}</section>
{#if edit}
  <ProdukDrawer {edit} {categories} {masterSatuan} bind:satuanList {satuanSiap} {saving} {error} terkunci={!!hapus}
    onSimpan={saveProduct} onTutup={tutupProduk} onHapus={() => edit && bukaHapus(edit)}
    onFoto={unggahFoto} onMuatUlangSatuan={() => edit && muatSatuan(edit.id)} />
{/if}
{#if hapus}<div class="overlay" role="presentation"><div class="modal kt-dlg" role="alertdialog" aria-modal="true" aria-labelledby="kt-hapus-judul"><h2 id="kt-hapus-judul">Hapus permanen?</h2><p><strong>{hapus.nama}</strong><small>{hapus.kode}</small></p>{#if hapus.memuat}<p class="muted">Memeriksa riwayat produk…</p>{:else if hapus.galat}<div class="error" role="alert">{hapus.galat}</div>{:else if !hapus.bisa}<div class="error" role="alert">Tidak bisa dihapus: produk sudah punya riwayat ({hapus.rincian}). Nonaktifkan saja agar tidak muncul di kasir.</div>{:else}<p>Produk belum pernah dipakai di transaksi. Data dihapus dan <strong>tidak dapat dibatalkan</strong>.</p>{/if}<div class="acts"><button type="button" class="secondary" style="min-height:44px" on:click={() => hapus = null}>{hapus.bisa && !hapus.galat ? 'Batal' : 'Tutup'}</button>{#if hapus.bisa && !hapus.memuat}<button type="button" class="btn-danger" disabled={hapus.proses} on:click={konfirmHapus}>{hapus.proses ? 'Menghapus…' : 'Hapus permanen'}</button>{/if}</div></div></div>{/if}
{#if stEdit}<div class="overlay"><div class="modal" role="dialog" aria-modal="true" aria-labelledby="judul-satuan"><div class="row"><h2 id="judul-satuan">{stEdit.id ? 'Ubah satuan' : 'Tambah satuan'}</h2><button type="button" aria-label="Tutup" class="icon" disabled={stSaving} on:click={() => stEdit = null}><X size={20} aria-hidden="true"/></button></div>{#if stError}<div class="error" role="alert">{stError}</div>{/if}<form on:submit|preventDefault={simpanSatuanMaster}><label>Nama satuan<input bind:value={stEdit.nama} maxlength={20} required/></label>{#if stEdit.id}<p class="muted">Produk yang memakai satuan ini ikut berganti nama. Riwayat transaksi tidak berubah.</p>{/if}<button class="primary" disabled={stSaving}>{stSaving ? 'Menyimpan…' : 'Simpan'}</button></form></div></div>{/if}
{#if stHapus}<div class="overlay" role="presentation"><div class="modal kt-dlg" role="alertdialog" aria-modal="true" aria-labelledby="kt-hapus-satuan-judul"><h2 id="kt-hapus-satuan-judul">Hapus satuan?</h2><p><strong>{stHapus.nama}</strong></p>{#if stHapus.memuat}<p class="muted">Memeriksa pemakaian satuan…</p>{:else if stHapus.galat}<div class="error" role="alert">{stHapus.galat}</div>{:else if !stHapus.bisa}<div class="error" role="alert">Tidak bisa dihapus: satuan masih dipakai {stHapus.jumlah} produk. Ganti satuan produk itu dulu.</div>{:else}<p>Satuan belum dipakai produk. Data dihapus dan <strong>tidak dapat dibatalkan</strong>.</p>{/if}<div class="acts"><button type="button" class="secondary" style="min-height:44px" on:click={() => stHapus = null}>{stHapus.bisa && !stHapus.galat ? 'Batal' : 'Tutup'}</button>{#if stHapus.bisa && !stHapus.memuat}<button type="button" class="btn-danger" disabled={stHapus.proses} on:click={konfirmHapusSatuanMaster}>{stHapus.proses ? 'Menghapus…' : 'Hapus satuan'}</button>{/if}</div></div></div>{/if}
{#if katEdit}<div class="overlay"><div class="modal" role="dialog" aria-modal="true" aria-labelledby="judul-kategori"><div class="row"><h2 id="judul-kategori">{katEdit.id ? 'Ubah kategori' : 'Tambah kategori'}</h2><button type="button" aria-label="Tutup" class="icon" disabled={katSaving} on:click={() => katEdit = null}><X size={20} aria-hidden="true"/></button></div>{#if katError}<div class="error" role="alert">{katError}</div>{/if}<form on:submit|preventDefault={simpanKategori}><label>Nama kategori<input bind:value={katEdit.nama} maxlength={50} required/></label><button class="primary" disabled={katSaving}>{katSaving ? 'Menyimpan…' : 'Simpan'}</button></form></div></div>{/if}
{#if katHapus}<div class="overlay" role="presentation"><div class="modal kt-dlg" role="alertdialog" aria-modal="true" aria-labelledby="kt-hapus-kategori-judul"><h2 id="kt-hapus-kategori-judul">Hapus kategori?</h2><p><strong>{katHapus.nama}</strong><small>Kode {katHapus.id}</small></p>{#if katHapus.memuat}<p class="muted">Memeriksa pemakaian kategori…</p>{:else if katHapus.galat}<div class="error" role="alert">{katHapus.galat}</div>{:else if !katHapus.bisa}<div class="error" role="alert">Tidak bisa dihapus: kategori masih dipakai {katHapus.jumlah} produk. Pindahkan produk ke kategori lain dulu.</div>{:else}<p>Kategori belum dipakai produk. Data dihapus dan <strong>tidak dapat dibatalkan</strong>.</p>{/if}<div class="acts"><button type="button" class="secondary" style="min-height:44px" on:click={() => katHapus = null}>{katHapus.bisa && !katHapus.galat ? 'Batal' : 'Tutup'}</button>{#if katHapus.bisa && !katHapus.memuat}<button type="button" class="btn-danger" disabled={katHapus.proses} on:click={konfirmHapusKategori}>{katHapus.proses ? 'Menghapus…' : 'Hapus kategori'}</button>{/if}</div></div></div>{/if}
{#if member}<div class="overlay"><div class="modal" role="dialog" aria-modal="true" aria-labelledby="judul-member"><div class="row"><h2 id="judul-member">{member.nama}</h2><button aria-label="Tutup" class="icon" on:click={() => member = null}><X size={20} aria-hidden="true"/></button></div><p>{member.nik} · {rp(member.total)}</p>{#each memberItems as t}<button class="list-button horizontal" on:click={() => showInvoice(t.no_transaksi)}><span>{t.no_transaksi}<small>{t.tanggal}</small></span><strong>{rp(t.total)} <ChevronRight size={16} class="chev" aria-hidden="true"/></strong></button>{:else}<p class="empty">Tidak ada transaksi.</p>{/each}{#if memberMore}<button class="secondary wide" on:click={() => openMember(member!, true)}>Muat lebih banyak</button>{/if}</div></div>{/if}
{#if invoice}<Receipt {invoice} close={() => invoice = null}/>{/if}
