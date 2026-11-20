<script lang="ts">
  // Panel samping Ubah/Tambah produk: satu form (identitas, harga + margin, satuan, foto, status), simpan sekali.
  // State dan pemanggilan API tetap di Admin.svelte; komponen ini hanya tampilan, fokus, dan penjaga perubahan.
  import { onMount, tick } from 'svelte';
  import X from '@lucide/svelte/icons/x';
  import Trash2 from '@lucide/svelte/icons/trash-2';
  import Plus from '@lucide/svelte/icons/plus';
  import { adaPerubahan, inisial, ringkasMargin } from './katalog';
  import type { IsianProduk, SatuanIsian } from './katalog';
  import Photo from './Photo.svelte';
  import type { Data } from './types';

  type SatuanBaris = { satuan: string; isi?: number; harga?: number; barcode: string };

  export let edit: Data;
  export let categories: Data[];
  export let masterSatuan: Data[] = [];
  // Nilai terpilih yang belum ada di master (produk warisan) tetap ditampilkan agar tidak hilang diam-diam.
  $: opsiDasar = opsiSatuan(masterSatuan, edit.satuan);
  const opsiSatuan = (m: Data[], cur?: string) => { const n = m.map(x => x.nama as string); return cur && !n.includes(cur) ? [cur, ...n] : n; };
  export let satuanList: SatuanBaris[];
  export let satuanSiap: boolean;
  export let saving: boolean;
  export let error: string;
  export let terkunci = false; // dialog hapus sedang terbuka di atas panel ini
  export let onSimpan: () => void;
  export let onTutup: () => void;
  export let onHapus: () => void;
  export let onFoto: (file: File) => Promise<void>;
  export let onMuatUlangSatuan: () => void;

  const rp = (n: number) => new Intl.NumberFormat('id-ID', { style: 'currency', currency: 'IDR', maximumFractionDigits: 0 }).format(n);
  const persen = new Intl.NumberFormat('id-ID', { maximumFractionDigits: 1 });
  const baru = !edit.id;

  let panel: HTMLElement, form: HTMLFormElement, fotoKey = 0, konfirmasiBuang = false, fotoProses = false;
  let awal: { produk: IsianProduk; satuan: SatuanIsian[] } | null = null;
  const salin = () => JSON.parse(JSON.stringify({ produk: edit, satuan: satuanList }));
  // Produk baru langsung punya kondisi awal; produk lama menunggu satuan selesai dimuat agar tidak dianggap berubah.
  $: if (!awal && (baru || satuanSiap)) awal = salin();
  $: kotor = awal ? adaPerubahan(awal, { produk: edit as unknown as IsianProduk, satuan: satuanList }) : false;
  $: margin = ringkasMargin(edit.harga, edit.beli);

  onMount(() => {
    const pemicu = document.activeElement as HTMLElement | null;
    void tick().then(() => form?.querySelector<HTMLInputElement>(baru ? 'input[name=barcode]' : 'input[name=nama]')?.focus());
    return () => pemicu?.focus?.();
  });

  function mintaTutup() {
    if (saving) return;
    if (kotor) { konfirmasiBuang = true; return; }
    onTutup();
  }

  function keyboard(e: KeyboardEvent) {
    if (terkunci) return;
    if (e.key === 'Escape') { e.preventDefault(); if (konfirmasiBuang) konfirmasiBuang = false; else mintaTutup(); return; }
    if (e.key === 'Tab' && panel) {
      const f = [...panel.querySelectorAll<HTMLElement>('button, input, select, a[href]')].filter((el) => !(el as HTMLButtonElement).disabled && el.offsetParent !== null);
      if (!f.length) return;
      const [a, z] = [f[0], f[f.length - 1]];
      if (e.shiftKey && document.activeElement === a) { e.preventDefault(); z.focus(); }
      else if (!e.shiftKey && document.activeElement === z) { e.preventDefault(); a.focus(); }
    }
  }

  // Enter pindah ke isian berikutnya (pemindai barcode mengirim Enter; jangan menyimpan form setengah jadi); Ctrl+Enter menyimpan.
  function enterForm(e: KeyboardEvent) {
    if (e.key !== 'Enter') return;
    if (e.ctrlKey || e.metaKey) { e.preventDefault(); form.requestSubmit(); return; }
    const t = e.target as HTMLElement;
    if (t.tagName !== 'INPUT' || (t as HTMLInputElement).type === 'checkbox' || (t as HTMLInputElement).type === 'file') return;
    e.preventDefault();
    const f = [...form.querySelectorAll<HTMLElement>('input:not([type=file]):not([type=checkbox]), select')].filter((el) => !(el as HTMLInputElement).disabled);
    f[f.indexOf(t) + 1]?.focus();
  }

  async function pilihFoto(e: Event) {
    const el = e.currentTarget as HTMLInputElement, file = el.files?.[0];
    if (!file) return;
    fotoProses = true;
    try { await onFoto(file); fotoKey += 1; } finally { fotoProses = false; el.value = ''; }
  }
</script>

<svelte:window on:keydown={keyboard} />

<div class="pd-scrim" role="presentation" on:click={mintaTutup}></div>
<div class="pd" bind:this={panel} role="dialog" aria-modal="true" aria-labelledby="judul-produk">
  <header class="pd-head">
    <div>
      <h2 id="judul-produk">{baru ? 'Tambah produk' : 'Ubah produk'}</h2>
      {#if !baru}<small class="muted">{edit.kode} · {edit.kategori}</small>{/if}
    </div>
    {#if kotor}<span class="pd-kotor" role="status">Belum disimpan</span>{/if}
    <button type="button" aria-label="Tutup" class="icon" disabled={saving} on:click={mintaTutup}><X size={20} aria-hidden="true" /></button>
  </header>

  <!-- svelte-ignore a11y_no_noninteractive_element_interactions -->
  <form bind:this={form} class="pd-form" on:submit|preventDefault={onSimpan} on:keydown={enterForm}>
    <div class="pd-body">
      {#if error}<div class="error" role="alert">{error}{#if !baru && !satuanSiap}<button type="button" class="secondary" on:click={onMuatUlangSatuan}>Muat ulang satuan</button>{/if}</div>{/if}

      <fieldset disabled={saving}>
        <legend>Identitas</legend>
        <label>Barcode<input name="barcode" bind:value={edit.barcode} inputmode="numeric" autocomplete="off" /></label>
        <label>Nama<input name="nama" bind:value={edit.nama} required autocomplete="off" /></label>
        <div class="form-grid">
          <label>Satuan dasar<select bind:value={edit.satuan} required><option value="" disabled>Pilih satuan</option>{#each opsiDasar as n}<option value={n}>{n}</option>{/each}</select></label>
          <label>Kategori<select bind:value={edit.kategori_id}>{#each categories as c}<option value={c.id}>{c.nama}</option>{/each}</select></label>
        </div>
      </fieldset>

      <fieldset disabled={saving}>
        <legend>Harga</legend>
        <div class="form-grid">
          <label>Harga jual<input type="number" bind:value={edit.harga} min="0" required inputmode="numeric" /></label>
          <label>Harga beli<input type="number" bind:value={edit.beli} min="0" required inputmode="numeric" /></label>
        </div>
        <p class="pd-margin {margin.tipe}" role="status" aria-live="polite">
          {#if margin.tipe === 'tanpa-hpp'}HPP belum diisi, margin belum bisa dihitung.
          {:else if margin.tipe === 'rugi'}Rugi {rp(Math.abs(margin.margin ?? 0))} per unit: harga jual di bawah HPP.
          {:else if margin.tipe === 'impas'}Impas: harga jual sama dengan HPP.
          {:else}Margin {rp(margin.margin ?? 0)}{#if margin.persen !== null}{' '}({persen.format(margin.persen)}%){/if} per unit.{/if}
        </p>
      </fieldset>

      <fieldset class="kt-satuan" disabled={saving}>
        <legend>Satuan jual tambahan</legend>
        <p class="muted">Isi adalah jumlah {edit.satuan || 'satuan dasar'} per satuan jual. Stok dihitung dalam satuan dasar.</p>
        {#if !baru && !satuanSiap && !error}<p class="muted">Memuat satuan…</p>{/if}
        {#if baru || satuanSiap}
          {#each satuanList as s, i (s)}
            <div class="pd-satuan form-grid" role="group" aria-label="Satuan tambahan {i + 1}">
              <label>Satuan<select bind:value={s.satuan} required><option value="" disabled>Pilih satuan</option>{#each opsiSatuan(masterSatuan, s.satuan) as n}<option value={n}>{n}</option>{/each}</select></label>
              <label>Isi<input type="number" bind:value={s.isi} min="1.0001" max="1000000" step="0.0001" required /></label>
              <label>Harga jual<input type="number" bind:value={s.harga} min="0.01" step="0.01" required /></label>
              <label>Barcode (opsional)<input bind:value={s.barcode} maxlength="50" /></label>
              <button type="button" class="secondary" on:click={() => (satuanList = satuanList.filter((_, j) => j !== i))}>Hapus satuan</button>
            </div>
          {/each}
          <button type="button" class="secondary" disabled={satuanList.length >= 10}
            on:click={() => (satuanList = [...satuanList, { satuan: '', barcode: '' }])}><Plus size={16} aria-hidden="true" /> Tambah satuan</button>
        {/if}
      </fieldset>

      {#if !baru}
        <fieldset disabled={saving}>
          <legend>Foto</legend>
          <div class="pd-foto">
            <div class="kt-ava" aria-hidden="true">
              {#if edit.foto_versi || fotoKey}{#key fotoKey}<Photo path={'/produk/' + edit.id + '/foto'} alt="" fallback={inisial(edit.nama)} />{/key}{:else}{inisial(edit.nama)}{/if}
            </div>
            <label class="pd-file">JPEG/PNG, maks. 1 MB
              <input type="file" accept="image/jpeg,image/png" disabled={fotoProses} on:change={pilihFoto} aria-label="Foto produk" />
            </label>
          </div>
        </fieldset>

        <fieldset class="pd-bahaya" disabled={saving}>
          <legend>Status dan zona berbahaya</legend>
          <label class="check"><input type="checkbox" bind:checked={edit.aktif} /> Produk aktif (tampil di kasir)</label>
          <p class="muted">Produk yang pernah dipakai transaksi tidak bisa dihapus; nonaktifkan saja.</p>
          <button type="button" class="btn-danger" on:click={onHapus}><Trash2 size={16} aria-hidden="true" /> Hapus permanen</button>
        </fieldset>
      {/if}
    </div>

    <footer class="pd-foot">
      {#if konfirmasiBuang}
        <div class="pd-buang" role="alert">
          <span>Buang perubahan yang belum disimpan?</span>
          <button type="button" class="secondary" on:click={() => (konfirmasiBuang = false)}>Lanjut mengedit</button>
          <button type="button" class="btn-danger" on:click={onTutup}>Buang</button>
        </div>
      {:else}
        <button type="button" class="secondary" disabled={saving} on:click={mintaTutup}>Batal</button>
        <button class="primary" disabled={saving} title="Ctrl+Enter">{saving ? 'Menyimpan…' : 'Simpan'}</button>
      {/if}
    </footer>
  </form>
</div>

<style>
  .pd-scrim { position: fixed; inset: 0; background: #071f1355; z-index: 20; }
  .pd { position: fixed; top: 0; right: 0; bottom: 0; width: min(100%, 560px); z-index: 21; display: flex; flex-direction: column; background: #fff; color: #1e2b22; box-shadow: -12px 0 40px #071f1333; }
  :global(.dark) .pd { background: #172a21; color: #e6f5ea; }
  .pd-head { display: flex; align-items: center; gap: 12px; padding: 18px 22px; border-bottom: 1px solid #dce6de; }
  :global(.dark) .pd-head { border-color: #385043; }
  .pd-head > div { flex: 1; min-width: 0; }
  .pd-head h2 { margin: 0; }
  .pd-kotor { background: #f4efda; color: #725b16; border-radius: 99px; padding: 4px 10px; font-size: .72rem; font-weight: 800; }
  .pd-form { display: flex; flex-direction: column; flex: 1; min-height: 0; }
  .pd-body { flex: 1; overflow: auto; padding: 8px 22px 22px; }
  fieldset { border: 0; padding: 0; margin: 0; min-width: 0; }
  legend { font-family: Poppins, sans-serif; font-weight: 700; font-size: .78rem; letter-spacing: .08em; text-transform: uppercase; color: #238046; padding: 18px 0 0; }
  :global(.dark) legend { color: #7fdc9a; }
  .pd-margin { margin: 4px 0 0; padding: 9px 12px; border-radius: 10px; font-size: .85rem; font-weight: 700; background: #e9f4e8; color: #166a32; }
  .pd-margin.rugi { background: #fceae7; color: #a32c25; }
  .pd-margin.tanpa-hpp, .pd-margin.impas { background: #f4efda; color: #725b16; }
  :global(.dark) .pd-margin { background: #1f3a2a; color: #9fe0b4; }
  :global(.dark) .pd-margin.rugi { background: #4a2523; color: #f6b6b0; }
  :global(.dark) .pd-margin.tanpa-hpp, :global(.dark) .pd-margin.impas { background: #3d3517; color: #e7d38a; }
  .pd-satuan { padding: 12px; border: 1px solid #dce6de; border-radius: 12px; margin-bottom: 10px; gap: 0 14px; }
  :global(.dark) .pd-satuan { border-color: #385043; }
  .pd-satuan label { margin: 8px 0; min-width: 0; }
  .pd-satuan button { justify-self: start; align-self: end; margin-bottom: 8px; }
  .pd-foto { display: flex; align-items: center; gap: 14px; margin-top: 10px; }
  .pd-foto .kt-ava { flex: none; }
  .pd-file { margin: 0; flex: 1; }
  .pd-bahaya { margin-top: 8px; border-top: 1px solid #f0c7c2; }
  .pd-bahaya legend { color: #a32c25; }
  :global(.dark) .pd-bahaya { border-color: #6b3a36; }
  :global(.dark) .pd-bahaya legend { color: #f0a39d; }
  .pd-foot { display: flex; justify-content: flex-end; gap: 10px; padding: 14px 22px; border-top: 1px solid #dce6de; background: inherit; }
  :global(.dark) .pd-foot { border-color: #385043; }
  .pd-foot button { min-height: 44px; }
  .pd-buang { display: flex; align-items: center; gap: 10px; flex-wrap: wrap; justify-content: flex-end; width: 100%; font-weight: 700; font-size: .85rem; }
  .pd-buang span { flex: 1; min-width: 180px; }
  @media (max-width: 640px) { .pd { width: 100%; } .pd-head, .pd-foot { padding-inline: 16px; } .pd-body { padding-inline: 16px; } }
</style>
