<script lang="ts">
  import { onMount, tick } from 'svelte';
  import CloudOff from '@lucide/svelte/icons/cloud-off';
  import RefreshCw from '@lucide/svelte/icons/refresh-cw';
  import type { User } from './types';
  import { drafBaru, hariJakarta, rpBeli, totalBeli, type DrafBeli, type ReferensiBeli } from './pembelian';
  import { antrekanBeli, bacaAntreanBeli, bacaPembelian, ikutiAntrean, muatReferensiBeli, simpanDrafBeli, sinkronPembelian,
    type AntreanBeli } from './pembelianOffline';
  import PembelianInput from './PembelianInput.svelte';
  import PembelianRiwayat from './PembelianRiwayat.svelte';
  import './pembelian.css';

  export let user: User;
  export let online: boolean;
  type Tab = 'input' | 'antrean' | 'riwayat';
  const tabs: { id: Tab; label: string }[] = [
    { id: 'input', label: 'Input pembelian' }, { id: 'antrean', label: 'Antrean' }, { id: 'riwayat', label: 'Riwayat' }
  ];
  let tab: Tab = 'input';
  let draf = drafBaru();
  let referensi: ReferensiBeli | undefined;
  let antrean: AntreanBeli[] = [];
  let membuka = true;
  let menyiapkan = false;
  let menyimpan = false;
  let mengirim = false;
  let galat = '';
  let pesan = '';
  let editPilihan: AntreanBeli | undefined;
  let konfirmasiEdit: HTMLDialogElement;
  let nomorPilihan = '';
  let tersambung = false;
  let tanggalDrafBaru = true;

  $: perluDikirim = antrean.filter(a => a.status !== 'TERSINKRON').length;
  $: if (online && !tersambung && !membuka) { tersambung = true; void segarkan(); }
  $: if (!online) tersambung = false;

  onMount(() => {
    const berhenti = ikutiAntrean(user.id, () => { void bacaAntrean(); });
    void (async () => {
      try {
        const lokal = await bacaPembelian(user.id);
        if (lokal.draf) { draf = lokal.draf; tanggalDrafBaru = false; }
        referensi = lokal.referensi;
        antrean = lokal.antrean;
      } catch (e) { galat = (e as Error).message; }
      finally { membuka = false; }
    })();
    return berhenti;
  });

  async function bacaAntrean() {
    try { antrean = await bacaAntreanBeli(user.id); }
    catch (e) { galat = (e as Error).message; }
  }

  async function segarkan() {
    if (menyiapkan) return;
    menyiapkan = true;
    try {
      referensi = await muatReferensiBeli(user.id);
      if (tanggalDrafBaru) draf = drafBaru(referensi.tanggal_server);
      galat = '';
    } catch {
      if (!referensi) galat = 'Katalog belum tersedia di perangkat ini. Sambungkan ke server untuk mengunduh katalog.';
      else galat = 'Katalog lokal sedang digunakan. Pembaruan katalog belum berhasil.';
    } finally { menyiapkan = false; }
  }

  async function simpan(d: DrafBeli) {
    tanggalDrafBaru = false;
    draf = d;
    try { await simpanDrafBeli(user.id, d); }
    catch (e) { galat = 'Draf belum tersimpan. ' + (e as Error).message; }
  }

  async function posting(d: DrafBeli) {
    if (menyimpan) return;
    menyimpan = true;
    galat = '';
    try {
      await antrekanBeli(user.id, d);
      pesan = `Faktur ${d.no_faktur} tersimpan di antrean perangkat ini.`;
      draf = drafBaru(hariJakarta());
      tanggalDrafBaru = true;
      await pilihTab('antrean');
      await bacaAntrean();
      if (online) await kirim();
    } catch (e) { galat = (e as Error).message; }
    finally { menyimpan = false; }
  }

  async function kirim() {
    if (mengirim) return;
    mengirim = true;
    galat = '';
    try {
      const selesai = await sinkronPembelian(user.id);
      await bacaAntrean();
      if (!selesai) pesan = 'Server belum bisa menerima antrean. Pengiriman akan dicoba lagi saat terhubung.';
      else if (!antrean.some(a => a.status === 'MENUNGGU')) pesan = '';
    } catch (e) { galat = (e as Error).message; }
    finally { mengirim = false; }
  }

  function bukaPerbaiki(a: AntreanBeli) {
    editPilihan = a;
    if (tanggalDrafBaru) void perbaiki();
    else konfirmasiEdit.showModal();
  }

  async function perbaiki() {
    if (!editPilihan) return;
    if (konfirmasiEdit.open) konfirmasiEdit.close();
    const d = { ...structuredClone(editPilihan.draf), id: crypto.randomUUID(), ganti: editPilihan.id };
    galat = '';
    await simpan(d);
    await pilihTab('input');
    if (online) void segarkan();
  }

  async function pilihTab(target: Tab) {
    nomorPilihan = '';
    tab = target;
    await tick();
    document.getElementById('beli-tab-' + target)?.focus();
  }

  function tombolTab(e: KeyboardEvent, i: number) {
    const arah = e.key === 'ArrowLeft' ? -1 : e.key === 'ArrowRight' ? 1 : 0;
    if (!arah && e.key !== 'Home' && e.key !== 'End') return;
    e.preventDefault();
    const index = e.key === 'Home' ? 0 : e.key === 'End' ? tabs.length - 1 : (i + arah + tabs.length) % tabs.length;
    void pilihTab(tabs[index].id);
  }

  function lihat(no: string) {
    nomorPilihan = no;
    tab = 'riwayat';
  }
</script>

<div class="beli">
  {#if !online}
    <div class="beli-offline" role="status"><CloudOff size={20} aria-hidden="true" />
      <div><strong>Mode offline</strong><small>Input dan antrean disimpan di perangkat ini. Stok masuk setelah sinkron.</small></div>
    </div>
  {/if}
  <div class="beli-tab" role="tablist" aria-label="Pembelian">
    {#each tabs as t, i (t.id)}
      <button type="button" id={'beli-tab-' + t.id} role="tab" aria-selected={tab === t.id} aria-controls={'beli-panel-' + t.id}
        tabindex={tab === t.id ? 0 : -1} on:keydown={(e) => tombolTab(e, i)} on:click={() => pilihTab(t.id)}>
        {t.label}{#if t.id === 'antrean' && perluDikirim}<span>{perluDikirim}</span>{/if}
      </button>
    {/each}
  </div>
  {#if galat}<p class="error" role="alert">{galat}</p>{/if}
  {#if pesan}<p class="beli-petunjuk" role="status">{pesan}</p>{/if}
  {#each tabs as t (t.id)}
    <div id={'beli-panel-' + t.id} role="tabpanel" aria-labelledby={'beli-tab-' + t.id} hidden={tab !== t.id}>
    {#if tab === t.id}
      {#if tab === 'input'}
        {#if referensi}
          {#key draf.id}
            <PembelianInput {draf} {referensi} {online} {menyiapkan} {menyimpan}
              on:simpan={(e) => simpan(e.detail)} on:posting={(e) => posting(e.detail)} on:segarkan={segarkan} />
          {/key}
        {:else}
          <section class="panel beli-panel">
            <h2>Katalog pembelian</h2>
            <p>{membuka || menyiapkan ? 'Menyiapkan katalog…' : 'Unduh katalog barang dan supplier saat terhubung untuk mulai menginput.'}</p>
            <button type="button" class="secondary" disabled={!online || membuka || menyiapkan} on:click={segarkan}>Unduh katalog</button>
          </section>
        {/if}
      {:else if tab === 'antrean'}
        <section class="panel beli-panel">
          <div class="beli-baris-judul"><h2>Antrean di perangkat ini</h2>
            <button type="button" class="secondary beli-ikon-button" on:click={kirim} disabled={!online || mengirim || !antrean.some(a => a.status === 'MENUNGGU')}>
              <RefreshCw size={16} aria-hidden="true" />{mengirim ? 'Mengirim…' : 'Coba lagi'}
            </button>
          </div>
          <div class="beli-gulir"><table class="beli-tabel">
            <thead><tr><th>Faktur supplier</th><th class="beli-angka">Total</th><th>Status</th><th>Tindakan</th></tr></thead>
            <tbody>
              {#each [...antrean].reverse() as a (a.id)}
                <tr><th scope="row">{a.draf.no_faktur}<small>{referensi?.supplier.find(s => s.kode === a.draf.supplier_id)?.nama ?? a.draf.supplier_id} · {a.draf.tanggal_terima}</small></th>
                  <td class="beli-angka">{rpBeli(totalBeli(a.draf.baris)?.total)}</td>
                  <td><span class="beli-status" class:waspada={a.status === 'MENUNGGU'} class:gagal={a.status === 'PERBAIKI'}>
                    {a.status === 'MENUNGGU' ? 'Menunggu' : a.status === 'TERSINKRON' ? 'Tersinkron' : 'Perlu diperbaiki'}
                  </span>{#if a.galat}<small>{a.galat}</small>{/if}{#if a.nomor}<small>{a.nomor}</small>{/if}</td>
                  <td>{#if a.status === 'PERBAIKI'}
                    <button class="secondary" type="button" on:click={() => bukaPerbaiki(a)}>Perbaiki</button>
                  {:else if a.nomor && online}
                    <button class="beli-teks-button" type="button" on:click={() => lihat(a.nomor!)}>Lihat faktur</button>
                  {/if}</td>
                </tr>
              {:else}<tr><td colspan="4" class="beli-kosong">Belum ada pembelian di antrean ini.</td></tr>{/each}
            </tbody>
          </table></div>
        </section>
      {:else}
        <PembelianRiwayat {user} {online} nomorAwal={nomorPilihan} />
      {/if}
    {/if}
    </div>
  {/each}
  <dialog class="modal beli-dialog" bind:this={konfirmasiEdit} aria-labelledby="beli-ganti-draf">
    <h2 id="beli-ganti-draf">Ganti draf aktif?</h2>
    <p>Draf yang sedang diinput akan diganti dengan salinan faktur {editPilihan?.draf.no_faktur}.</p>
    <div class="beli-aksi">
      <button class="secondary" type="button" on:click={() => konfirmasiEdit.close()}>Kembali</button>
      <button class="primary" type="button" on:click={perbaiki}>Ganti draf</button>
    </div>
  </dialog>
</div>
