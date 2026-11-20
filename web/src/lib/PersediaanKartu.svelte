<script lang="ts">
  import ChevronLeft from '@lucide/svelte/icons/chevron-left';
  import ChevronRight from '@lucide/svelte/icons/chevron-right';
  import { api, query } from './api';
  import { labelJenisMutasi, rentangKartu } from './persediaan';

  interface Barang { id: number; kode: string; nama: string; satuan: string; stok: number }
  interface Mutasi { tanggal: string; jenis: string; ref: string; qty: string; saldo: string }
  interface Kartu {
    produk: { id: number; kode: string; nama: string; satuan: string };
    dari: string; sampai: string; saldo_awal: string; saldo_akhir: string;
    items: Mutasi[]; ada_lagi: boolean; page: number;
  }

  const angka = new Intl.NumberFormat('id-ID', { maximumFractionDigits: 4 });
  const kini = new Date();
  const hariIni = `${kini.getFullYear()}-${String(kini.getMonth() + 1).padStart(2, '0')}-${String(kini.getDate()).padStart(2, '0')}`;
  const tanda = (s: string) => { const n = Number(s); return (n > 0 ? '+' : '') + angka.format(n); };

  let cari = '', hasil: Barang[] = [], mencari = false, sudahCari = false;
  let produk: Barang | null = null;
  let dari = rentangKartu(null, null, hariIni).dari, sampai = hariIni;
  let kartu: Kartu | null = null, page = 1, memuat = false, galat = '';

  async function cariProduk() {
    mencari = true; galat = '';
    try {
      const r = await api<{ items: Barang[] }>('/admin/stok' + query({ q: cari, page: 1, size: 20 }));
      hasil = r.items; sudahCari = true;
    } catch (e) { galat = (e as Error).message; }
    finally { mencari = false; }
  }

  function pilih(b: Barang) {
    produk = b; hasil = []; sudahCari = false; cari = '';
    void muat(1);
  }

  async function muat(p: number) {
    if (!produk || memuat) return;
    memuat = true; galat = '';
    try {
      const r = rentangKartu(dari, sampai, hariIni);
      dari = r.dari; sampai = r.sampai;
      kartu = await api<Kartu>('/admin/persediaan/kartu' + query({ produk_id: produk.id, dari, sampai, page: p, size: 30 }));
      page = p;
    } catch (e) { galat = (e as Error).message; }
    finally { memuat = false; }
  }
</script>

<section class="panel sedia-panel">
  <h2>Kartu stok</h2>
  <p class="sedia-petunjuk">Mutasi pembelian, penjualan, opname, barang rusak, dan penyesuaian per produk. Saldo akhir selalu sama dengan stok sistem.</p>
  {#if galat}<div class="error" role="alert">{galat}</div>{/if}
  {#if !produk}
    <form class="search" on:submit|preventDefault={cariProduk}>
      <label class="sr-only" for="sedia-kartu-cari">Cari produk</label>
      <input id="sedia-kartu-cari" bind:value={cari} placeholder="Cari nama, kode, atau barcode"/>
      <button disabled={mencari}>{mencari ? 'Mencari…' : 'Cari'}</button>
    </form>
    {#if hasil.length}
      <ul class="sedia-pilih" role="listbox" aria-label="Hasil pencarian produk" style="position:static">
        {#each hasil as b (b.id)}
          <li role="option" aria-selected="false"><button type="button" on:click={() => pilih(b)}>
            <strong>{b.nama}</strong><span>{b.kode} · stok {angka.format(b.stok)} {b.satuan}</span>
          </button></li>
        {/each}
      </ul>
    {:else if sudahCari && !mencari}
      <p class="muted">Tidak ada produk aktif yang cocok.</p>
    {/if}
  {:else}
    <div class="sedia-baris-judul">
      <p style="margin:0"><strong>{kartu?.produk.nama ?? produk.nama}</strong> <small class="muted">{produk.kode} · {produk.satuan}</small></p>
      <button type="button" class="secondary" on:click={() => { produk = null; kartu = null; }}>Ganti produk</button>
    </div>
    <form class="sedia-form kisi" on:submit|preventDefault={() => muat(1)}>
      <label>Dari tanggal<input type="date" bind:value={dari} min={dari.slice(0, 4) + '-01-01'} max={hariIni}/></label>
      <label>Sampai tanggal<input type="date" bind:value={sampai} min={dari.slice(0, 4) + '-01-01'} max={hariIni}/></label>
      <button class="primary" disabled={memuat}>{memuat ? 'Memuat…' : 'Tampilkan'}</button>
    </form>
    {#if kartu}
      <dl class="sedia-saldo">
        <div><dt>Saldo awal ({kartu.dari})</dt><dd>{angka.format(Number(kartu.saldo_awal))} {kartu.produk.satuan}</dd></div>
        <div><dt>Saldo akhir ({kartu.sampai})</dt><dd>{angka.format(Number(kartu.saldo_akhir))} {kartu.produk.satuan}</dd></div>
      </dl>
      <div class="sedia-gulir"><table class="sedia-tabel">
        <thead><tr><th>Tanggal</th><th>Jenis</th><th>Referensi</th><th class="sedia-angka">Qty</th><th class="sedia-angka">Saldo</th></tr></thead>
        <tbody>
          {#each kartu.items as m, i (i)}
            <tr><td>{m.tanggal}</td><td>{labelJenisMutasi(m.jenis)}</td><td>{m.ref}</td>
              <td class="sedia-angka">{tanda(m.qty)}</td><td class="sedia-angka">{angka.format(Number(m.saldo))}</td></tr>
          {:else}
            <tr><td colspan="5" class="sedia-kosong">Tidak ada mutasi pada rentang ini.</td></tr>
          {/each}
        </tbody>
      </table></div>
      <div class="sedia-aksi">
        <button type="button" class="secondary sedia-ikon-button" disabled={page <= 1 || memuat} on:click={() => muat(page - 1)}>
          <ChevronLeft size={16} aria-hidden="true"/>Sebelumnya
        </button>
        <span class="sedia-petunjuk">Halaman {page}</span>
        <button type="button" class="secondary sedia-ikon-button" disabled={!kartu.ada_lagi || memuat} on:click={() => muat(page + 1)}>
          Berikutnya<ChevronRight size={16} aria-hidden="true"/>
        </button>
      </div>
    {/if}
  {/if}
</section>