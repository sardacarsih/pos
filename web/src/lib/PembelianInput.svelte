<script lang="ts">
  import { createEventDispatcher, onDestroy, tick } from 'svelte';
  import { BarcodeFormat, BrowserMultiFormatReader, type IScannerControls } from '@zxing/browser';
  import Camera from '@lucide/svelte/icons/camera';
  import ScanBarcode from '@lucide/svelte/icons/scan-barcode';
  import Trash2 from '@lucide/svelte/icons/trash-2';
  import { brutoBaris, cariBarang, cocokBarcode, formatJumlahBeli, hargaAwalBeli, hargaDasar, metodeBayar, payloadBeli,
    rpBeli, selisihHarga, stokMasuk, tambahQty, teksQtyDasar, totalBeli,
    type DrafBeli, type PilihanBarang, type ReferensiBeli, type SatuanBeli } from './pembelian';
  import { inputAngkaPembelian } from './inputAngkaPembelian';

  export let draf: DrafBeli;
  export let referensi: ReferensiBeli;
  export let online: boolean;
  export let menyiapkan = false;
  export let menyimpan = false;
  let scan = '';
  let galat = '';
  let galatScan = '';
  let pesan = '';
  let bukaPilihan = false;
  let pilihanAktif = -1;
  let scanInput: HTMLInputElement;
  let tabel: HTMLTableElement;
  let form: HTMLFormElement;
  let konfirmasi: HTMLDialogElement;
  let dialogKamera: HTMLDialogElement;
  let video: HTMLVideoElement;
  let stream: MediaStream | undefined;
  let kontrol: IScannerControls | undefined;
  let sesiKamera = 0;
  let statusKamera = '';
  let galatKamera = '';
  let dariKamera = false;
  let previewKey = 0;
  const dispatch = createEventDispatcher<{ simpan: DrafBeli; posting: DrafBeli; segarkan: void }>();

  $: pilihanKamera = dariKamera ? cocokBarcode(referensi.barang, scan) : [];
  $: pilihan = dariKamera ? { persis: pilihanKamera, hasil: pilihanKamera }
    : cariBarang(referensi.barang, scan);
  $: jumlah = totalBeli(draf.baris);
  $: dibayar = draf.jenis === 'TEMPO' ? (draf.uang_muka ?? 0) : (jumlah?.total ?? 0);
  $: supplierLama = draf.supplier_id && !referensi.supplier.some(s => s.kode === draf.supplier_id);
  $: barangAktif = new Map(referensi.barang.map(p => [p.id, p]));

  function simpan() { dispatch('simpan', draf); }

  function simpanInput(e: Event) {
    // Bind Svelte pada select diperbarui saat change, setelah event input.
    if (!(e.target instanceof HTMLSelectElement)) simpan();
  }

  onDestroy(hentikanKamera);

  function hentikanKamera() {
    sesiKamera += 1;
    kontrol?.stop();
    kontrol = undefined;
    stream?.getTracks().forEach(track => track.stop());
    stream = undefined;
  }

  function tutupKamera() {
    hentikanKamera();
    dialogKamera.close();
  }

  async function bukaKamera() {
    if (dialogKamera.open) return;
    const sesi = ++sesiKamera;
    bukaPilihan = false;
    galatKamera = '';
    statusKamera = 'Menyiapkan kamera…';
    dialogKamera.showModal();
    if (!navigator.mediaDevices?.getUserMedia) {
      statusKamera = '';
      galatKamera = 'Kamera memerlukan HTTPS atau localhost. Gunakan scanner atau ketik barcode.';
      return;
    }
    try {
      previewKey = sesi;
      await tick();
      if (sesi !== sesiKamera) return;
      const media = await navigator.mediaDevices.getUserMedia({
        audio: false, video: { facingMode: { ideal: 'environment' } }
      });
      if (sesi !== sesiKamera) {
        media.getTracks().forEach(track => track.stop());
        return;
      }
      stream = media;
      statusKamera = 'Arahkan kamera ke barcode barang.';
      const reader = new BrowserMultiFormatReader();
      const hasil = await reader.decodeFromStream(media, video, (result, _error, sesiKontrol) => {
        if (!result || sesi !== sesiKamera) return;
        kontrol = sesiKontrol;
        const barcode = result.getText().trim();
        // ZXing may return a leading-zero EAN-13 in its equivalent UPC-A form.
        const alias = result.getBarcodeFormat() === BarcodeFormat.UPC_A ? '0' + barcode : '';
        tutupKamera();
        void hasilKamera(cocokBarcode(referensi.barang, barcode).length || !alias ? barcode : alias);
      });
      if (sesi === sesiKamera) kontrol = hasil;
      else hasil.stop();
    } catch (e) {
      if (sesi !== sesiKamera) return;
      hentikanKamera();
      statusKamera = '';
      galatKamera = e instanceof DOMException && e.name === 'NotAllowedError'
        ? 'Izin kamera ditolak. Izinkan kamera di browser atau ketik barcode.'
        : 'Kamera tidak tersedia. Gunakan scanner atau ketik barcode.';
    }
  }

  async function hasilKamera(barcode: string) {
    const hasil = cocokBarcode(referensi.barang, barcode);
    if (hasil.length === 1) {
      await tambah(hasil[0]);
      return;
    }
    scan = barcode;
    dariKamera = true;
    galatScan = hasil.length ? '' : 'Barcode tidak terdaftar di katalog pembelian.';
    await tick();
    scanInput.focus();
    bukaPilihan = hasil.length > 1;
    pilihanAktif = -1;
  }

  function pindahKolom(e: KeyboardEvent, i: number, kolom?: string) {
    if (e.key !== 'Enter' || e.isComposing || e.ctrlKey || e.metaKey) return;
    e.preventDefault();
    if (!(e.currentTarget as HTMLInputElement).reportValidity()) return;
    if (kolom) tabel.querySelector<HTMLInputElement>(`[data-baris="${i}"] [data-kolom="${kolom}"]`)?.focus();
    else scanInput.focus();
  }

  async function tambah(p: PilihanBarang) {
    const { produk, satuan } = p;
    // Scan ulang barang + satuan yang sama menambah jumlah baris itu; satuan lain membuat baris baru.
    const ada = draf.baris.findIndex(r => r.product_id === produk.id && r.satuan === satuan.satuan
      && Number(r.isi) === Number(satuan.isi));
    if (ada < 0 && draf.baris.length >= 100) { galatScan = 'Pembelian maksimal 100 baris.'; return; }
    let fokus = ada;
    if (ada >= 0) {
      const qty = tambahQty(draf.baris[ada].qty);
      if (!qty) { galatScan = 'Jumlah melebihi 999.999.'; return; }
      draf.baris = draf.baris.map((r, n) => n === ada ? { ...r, qty } : r);
      pesan = `${produk.nama}, ${satuan.satuan}, jumlah menjadi ${qty}.`;
    } else {
      const hargaAwal = hargaAwalBeli(produk, satuan);
      draf.baris = [...draf.baris, {
        id: crypto.randomUUID(), product_id: produk.id, kode: produk.kode, nama: produk.nama,
        satuan: satuan.satuan, isi: satuan.isi, qty: '1', harga: hargaAwal > 0 ? hargaAwal : undefined, potongan: 0
      }];
      fokus = draf.baris.length - 1;
      pesan = `${produk.nama}, ${satuan.satuan}, ditambahkan.`;
    }
    scan = '';
    dariKamera = false;
    bukaPilihan = false;
    pilihanAktif = -1;
    galatScan = '';
    simpan();
    await tick();
    const input = tabel.querySelector<HTMLInputElement>(`[data-baris="${fokus}"] [data-kolom="qty"]`);
    input?.focus();
    input?.select();
  }

  function ketikScan(e: KeyboardEvent) {
    if (e.isComposing || e.ctrlKey || e.metaKey) return;
    if (e.key === 'Escape') {
      bukaPilihan = false;
      pilihanAktif = -1;
      return;
    }
    const hasil = pilihan.hasil;
    if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {
      if (!hasil.length) return;
      e.preventDefault();
      bukaPilihan = true;
      if (pilihanAktif < 0) pilihanAktif = e.key === 'ArrowDown' ? 0 : hasil.length - 1;
      else pilihanAktif = (pilihanAktif + hasil.length + (e.key === 'ArrowDown' ? 1 : -1)) % hasil.length;
      void tick().then(() => document.getElementById(`beli-pilihan-${pilihanAktif}`)?.scrollIntoView({ block: 'nearest' }));
      return;
    }
    if (e.key !== 'Enter') return;
    e.preventDefault();
    const pilih = bukaPilihan && pilihanAktif >= 0 ? hasil[pilihanAktif]
      : (hasil.length === 1 ? hasil[0] : undefined);
    if (pilih) void tambah(pilih);
    else if (hasil.length) {
      bukaPilihan = true;
      pilihanAktif = 0;
    } else galatScan = scan.trim() ? 'Barang tidak ditemukan di katalog pembelian.' : 'Scan barcode atau ketik nama barang.';
  }

  function hapus(i: number) {
    draf.baris = draf.baris.filter((_, index) => i !== index);
    simpan();
    scanInput.focus();
  }

  function pilihUlang(i: number, satuan: string) {
    const b = draf.baris[i];
    const produk = barangAktif.get(b.product_id);
    const opsi = produk?.satuan.find(s => s.satuan === satuan);
    if (!produk || !opsi) return;
    const harga = hargaAwalBeli(produk, opsi);
    draf.baris = draf.baris.map((row, n) => i === n
      ? { ...row, satuan, isi: opsi.isi, harga: harga > 0 ? harga : row.harga } : row);
    simpan();
  }

  const aktifSatuan = (item: { satuan: SatuanBeli[] }, b: { satuan: string; isi: string }) =>
    item.satuan.findIndex(s => s.satuan === b.satuan && Number(s.isi) === Number(b.isi));

  $: ringkasStok = stokMasuk(draf.baris, barangAktif);

  function tinjau() {
    if (menyimpan || konfirmasi.open) return;
    galat = '';
    if (!form.reportValidity()) return;
    try {
      payloadBeli(draf);
      if (supplierLama) throw new Error('Pilih supplier yang masih aktif.');
      konfirmasi.showModal();
    } catch (e) { galat = (e as Error).message; }
  }

  function shortcut(e: KeyboardEvent) {
    if (e.key === 'Enter' && (e.ctrlKey || e.metaKey) && !e.isComposing) {
      e.preventDefault();
      tinjau();
    }
  }

  function konfirmasiPosting() {
    konfirmasi.close();
    dispatch('posting', draf);
  }
</script>

<!-- svelte-ignore a11y_no_noninteractive_element_interactions (Ctrl+Enter berlaku dari setiap kolom form) -->
<form aria-label="Input pembelian" bind:this={form} on:input={simpanInput} on:change={simpan} on:keydown={shortcut} on:submit|preventDefault={tinjau}>
  <div class="beli-layout">
    <div class="beli-kolom">
      <section class="panel beli-panel">
        <h2>Faktur supplier</h2>
        <div class="beli-form kisi">
          <label>Supplier
            <select aria-label="Supplier" bind:value={draf.supplier_id} required>
              <option value="">Pilih supplier</option>
              {#if supplierLama}<option value={draf.supplier_id}>{draf.supplier_id} · tidak tersedia</option>{/if}
              {#each referensi.supplier as s (s.kode)}<option value={s.kode}>{s.nama} · {s.kode}</option>{/each}
            </select>
          </label>
          <label>No. faktur supplier <input bind:value={draf.no_faktur} maxlength="100" required autocomplete="off" /></label>
          <label>Tanggal faktur <input type="date" bind:value={draf.tanggal_faktur} required /></label>
          <label>Tanggal penerimaan <input type="date" bind:value={draf.tanggal_terima} required /></label>
        </div>
      </section>

      <section class="panel beli-panel">
        <div class="beli-baris-judul">
          <h2>Barang diterima</h2>
          {#if online}
            <button type="button" class="beli-teks-button" disabled={menyiapkan} on:click={() => dispatch('segarkan')}>
              {menyiapkan ? 'Memuat katalog…' : 'Perbarui katalog'}
            </button>
          {/if}
        </div>
        <div class="beli-scan">
          <div class="beli-baris-judul">
            <label for="beli-scan">Barcode atau nama barang</label>
            <button class="beli-teks-button beli-ikon-button" type="button" on:click={bukaKamera}>
              <Camera size={18} aria-hidden="true" /> Scan dengan kamera
            </button>
          </div>
          <div class="beli-scan-input">
            <ScanBarcode size={24} aria-hidden="true" />
            <input id="beli-scan" bind:this={scanInput} bind:value={scan} role="combobox" aria-autocomplete="list"
              aria-expanded={bukaPilihan && pilihan.hasil.length > 0} aria-controls="beli-pilihan"
              aria-activedescendant={bukaPilihan && pilihanAktif >= 0 ? 'beli-pilihan-' + pilihanAktif : undefined}
              autocomplete="off" placeholder="Scan barcode atau ketik nama / kode"
              on:input={() => { dariKamera = false; bukaPilihan = true; pilihanAktif = -1; galatScan = ''; }}
              on:keydown={ketikScan} on:blur={() => { bukaPilihan = false; }} />
          </div>
          <ul id="beli-pilihan" role="listbox" aria-label="Pilihan barang" hidden={!bukaPilihan || !pilihan.hasil.length}>
            {#each pilihan.hasil as p, i}
              <li role="option" id={'beli-pilihan-' + i} aria-selected={i === pilihanAktif}>
                <button type="button" tabindex="-1" on:pointerdown|preventDefault={() => {}} on:click={() => tambah(p)}>
                  <strong>{p.produk.nama}</strong>
                  <span>{p.produk.kode} · {p.satuan.satuan}{Number(p.satuan.isi) !== 1 ? ' × ' + formatJumlahBeli(p.satuan.isi) : ''}</span>
                </button>
              </li>
            {/each}
          </ul>
        </div>
        {#if galatScan}<p class="error" role="alert">{galatScan}</p>{/if}
        <p class="beli-petunjuk"><kbd>↑</kbd> <kbd>↓</kbd> Pilih barang <kbd>Enter</kbd> Lanjut <kbd>Ctrl</kbd>+<kbd>Enter</kbd> Tinjau faktur</p>
        <div class="sr-only" role="status">{pesan}</div>
        <div class="beli-gulir">
          <table class="beli-tabel beli-grid-barang" bind:this={tabel}>
            <thead><tr><th>Barang / satuan</th><th>Satuan</th><th class="beli-angka">Jumlah</th><th class="beli-angka">Harga beli / satuan</th><th class="beli-angka">Potongan</th><th class="beli-angka">Subtotal</th><th><span class="sr-only">Tindakan</span></th></tr></thead>
            <tbody>
              {#each draf.baris as b, i (b.id)}
                {@const bruto = brutoBaris(b)}
                {@const item = barangAktif.get(b.product_id)}
                <tr data-baris={i}>
                  <th scope="row">
                    <strong>{b.nama}</strong><small>{b.kode}</small>
                    {#if item}
                      {@const cocok = item.satuan.some(s => s.satuan === b.satuan && Number(s.isi) === Number(b.isi))}
                      {@const aktif = item.satuan.findIndex(s => s.satuan === b.satuan && Number(s.isi) === Number(b.isi))}
                      {@const stokTxt = teksQtyDasar(b)}
                      {@const hd = hargaDasar(b.harga, b.isi)}
                      {#if !cocok}<small class="beli-peringatan">Satuan berubah di katalog. Pilih satuan dan periksa harga.</small>
                      {:else if stokTxt}
                        <small class="beli-konversi" aria-live="polite">
                          <span>{#if Number(b.isi) !== 1}<span class="beli-rumus">{formatJumlahBeli(b.qty.replace('.', ','))} {b.satuan} × {formatJumlahBeli(b.isi)} =</span>{/if}<strong>{formatJumlahBeli(stokTxt)} {item.satuan[0].satuan}</strong> masuk stok</span>
                          {#if hd !== null && Number(b.isi) !== 1}<span>{rpBeli(hd)} / {item.satuan[0].satuan}</span>{/if}
                        </small>
                      {/if}
                    {:else}<small class="beli-peringatan">Barang sudah tidak tersedia</small>{/if}
                  </th>
                  <td data-label="Satuan">
                    {#if item && (item.satuan.length > 1 || aktifSatuan(item, b) < 0)}
                      <select aria-label={'Satuan ' + b.nama} data-kolom="satuan" value={aktifSatuan(item, b) < 0 ? '' : b.satuan}
                        on:change={(e) => pilihUlang(i, e.currentTarget.value)}>
                        {#if aktifSatuan(item, b) < 0}<option value="">Pilih satuan</option>{/if}
                        {#each item.satuan as s, n}<option value={s.satuan}>{s.satuan} {n === 0 ? '(dasar)' : '× ' + formatJumlahBeli(s.isi)}</option>{/each}
                      </select>
                    {:else}{b.satuan}{/if}
                  </td>
                  <td data-label="Jumlah"><input class="beli-angka" aria-label={'Jumlah ' + b.nama} inputmode="decimal" data-kolom="qty" required
                    use:inputAngkaPembelian={{ jenis: 'jumlah', nilai: b.qty, ubah: nilai => b.qty = nilai }}
                    on:keydown={(e) => pindahKolom(e, i, 'harga')} /><small>{b.satuan}</small></td>
                  <td data-label="Harga beli"><input class="beli-angka" aria-label={'Harga beli per ' + b.satuan + ' ' + b.nama} inputmode="numeric" data-kolom="harga" required
                    use:inputAngkaPembelian={{ jenis: 'rupiah', nilai: b.harga, min: 1, ubah: nilai => b.harga = nilai }}
                    on:keydown={(e) => pindahKolom(e, i, 'potongan')} /><small>per {b.satuan}</small>
                    {#if item}
                      {@const opsi = item.satuan.find(s => s.satuan === b.satuan && Number(s.isi) === Number(b.isi))}
                      {@const awal = opsi ? hargaAwalBeli(item, opsi) : 0}
                      {@const selisih = opsi ? selisihHarga(item, opsi, b.harga) : null}
                      {#if awal > 0 && b.harga !== awal}<small>Terakhir {rpBeli(awal)}</small>{/if}
                      {#if selisih !== null}
                        <small class="beli-peringatan" role="status">{selisih > 0 ? '+' : ''}{Math.round(selisih * 100)}% dari harga terakhir. Periksa satuan dan harga.</small>
                      {/if}
                    {/if}</td>
                  <td data-label="Potongan"><input class="beli-angka" aria-label={'Potongan ' + b.nama} inputmode="numeric" data-kolom="potongan"
                    use:inputAngkaPembelian={{ jenis: 'rupiah', nilai: b.potongan, ubah: nilai => b.potongan = nilai }}
                    on:keydown={(e) => pindahKolom(e, i)} /></td>
                  <td class="beli-angka" data-label="Subtotal">{rpBeli(bruto === null ? null : bruto - (b.potongan ?? 0))}</td>
                  <td><button type="button" class="beli-hapus" aria-label={'Hapus ' + b.nama} on:click={() => hapus(i)}><Trash2 size={17} aria-hidden="true" /></button></td>
                </tr>
              {:else}
                <tr><td colspan="7" class="beli-kosong">Scan barcode atau cari barang untuk menambah baris.</td></tr>
              {/each}
            </tbody>
          </table>
        </div>
      </section>
    </div>

    <section class="panel beli-panel beli-ringkasan">
      <h2>Pembayaran</h2>
      <div class="beli-form">
        <label>Jenis pembayaran
          <select aria-label="Jenis pembayaran" bind:value={draf.jenis}><option value="TUNAI">Tunai</option><option value="TEMPO">Tempo</option></select>
        </label>
        {#if draf.jenis === 'TEMPO'}
          <label>Jatuh tempo <input type="date" min={draf.tanggal_faktur} bind:value={draf.jatuh_tempo} required /></label>
          <label>Uang muka <input class="beli-angka" inputmode="numeric"
            use:inputAngkaPembelian={{ jenis: 'rupiah', nilai: draf.uang_muka, ubah: nilai => draf.uang_muka = nilai }} />
          </label>
        {/if}
        {#if draf.jenis === 'TUNAI' || dibayar > 0}
          <label>Metode pembayaran
            <select aria-label="Metode pembayaran" bind:value={draf.metode}>{#each Object.entries(metodeBayar) as [kode, nama]}<option value={kode}>{nama}</option>{/each}</select>
          </label>
          <label>Referensi pembayaran <input maxlength="100" bind:value={draf.referensi} /></label>
        {/if}
      </div>
      <dl class="beli-total">
        <div><dt>Bruto</dt><dd>{rpBeli(jumlah?.bruto)}</dd></div>
        <div><dt>Potongan</dt><dd>{rpBeli(jumlah?.potongan)}</dd></div>
        <div class="beli-total-utama"><dt>Total faktur</dt><dd>{rpBeli(jumlah?.total)}</dd></div>
        <div><dt>Sisa hutang</dt><dd>{rpBeli(jumlah ? Math.max(0, jumlah.total - dibayar) : null)}</dd></div>
      </dl>
      {#if galat}<p class="error" role="alert">{galat}</p>{/if}
      <button type="button" class="primary wide" disabled={menyimpan} on:click={tinjau}>Tinjau faktur</button>
    </section>
  </div>
</form>

<dialog class="modal beli-dialog" bind:this={konfirmasi} aria-labelledby="beli-konfirmasi">
  <h2 id="beli-konfirmasi">Konfirmasi pembelian</h2>
  <p>{referensi.supplier.find(s => s.kode === draf.supplier_id)?.nama} · {draf.no_faktur} · {draf.baris.length} baris</p>
  <ul class="beli-tinjau" aria-label="Barang diterima">
    {#each draf.baris as b (b.id)}
      {@const stokTxt = teksQtyDasar(b)}
      {@const dasar = barangAktif.get(b.product_id)?.satuan[0]?.satuan}
      <li><span>{b.nama}</span>
        <strong>{formatJumlahBeli(b.qty.replace('.', ','))} {b.satuan}{#if Number(b.isi) !== 1 && stokTxt && dasar} (= {formatJumlahBeli(stokTxt)} {dasar}){/if}</strong></li>
    {/each}
  </ul>
  {#if ringkasStok.length}
    <p class="beli-stok-total">Stok masuk: {ringkasStok.map(s => `${formatJumlahBeli(s.qty)} ${s.satuan}`).join(' · ')}</p>
  {/if}
  <dl class="beli-total">
    <div><dt>Total faktur</dt><dd>{rpBeli(jumlah?.total)}</dd></div>
    <div><dt>Dibayar</dt><dd>{rpBeli(dibayar)}</dd></div>
    <div><dt>Sisa hutang</dt><dd>{rpBeli(jumlah ? jumlah.total - dibayar : null)}</dd></div>
  </dl>
  {#if !online}<p class="notice">Stok dan hutang dicatat setelah transaksi tersinkron.</p>{/if}
  <div class="beli-aksi"><button class="secondary" type="button" on:click={() => konfirmasi.close()}>Kembali</button>
    <button class="primary" type="button" on:click={konfirmasiPosting}>{online ? 'Simpan pembelian' : 'Masukkan antrean'}</button>
  </div>
</dialog>

<dialog class="modal beli-dialog" bind:this={dialogKamera} aria-labelledby="beli-kamera-judul"
  on:cancel|preventDefault={tutupKamera}>
  <div class="beli-baris-judul">
    <h2 id="beli-kamera-judul">Scan barcode barang</h2>
    <button class="secondary" type="button" on:click={tutupKamera}>Tutup kamera</button>
  </div>
  {#key previewKey}
    <video class="beli-kamera" bind:this={video} aria-label="Pratinjau kamera barcode" muted playsinline></video>
  {/key}
  {#if statusKamera}<p role="status">{statusKamera}</p>{/if}
  {#if galatKamera}<p class="error" role="alert">{galatKamera}</p>{/if}
</dialog>
