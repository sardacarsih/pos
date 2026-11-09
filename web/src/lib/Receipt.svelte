<script lang="ts">
  import type { Data } from './types';
  export let invoice: Data;
  export let close: () => void;
  const rp = (n: number) => new Intl.NumberFormat('id-ID', { style: 'currency', currency: 'IDR', maximumFractionDigits: 0 }).format(n || 0);
</script>
<div class="overlay"><section class="modal receipt"><div class="row no-print"><h2>Rincian faktur</h2><button class="icon" on:click={close} aria-label="Tutup">×</button></div>
  <div class="receipt-head"><strong>KOPKAR KUSUMA LESTARI</strong><p>{invoice.no_transaksi} · {invoice.tanggal} {invoice.jam}</p><p>{invoice.nama_pelanggan} · {invoice.jenis_bayar}</p></div>
  {#if String(invoice.no_transaksi).startsWith('DEMO-')}<div class="notice">Simulasi: transaksi ini tidak tersimpan.</div>{/if}
  {#each invoice.baris || [] as line}<div class="receipt-line"><span>{line.nama}<small>{line.qty} {line.satuan} × {rp(line.harga)} {line.potongan ? ` · Potongan ${rp(line.potongan)}` : ''}</small></span><strong>{rp(line.total)}</strong></div>{/each}
  <div class="receipt-line total"><span>Total akhir</span><strong>{rp(invoice.total)}</strong></div>
  {#if invoice.uang_bayar != null}<div class="receipt-line"><span>Bayar / Kembali</span><span>{rp(invoice.uang_bayar)} / {rp(invoice.kembalian)}</span></div>{/if}
  {#if invoice.jadwal?.length}<h3>Jadwal angsuran</h3>{#each invoice.jadwal as j}<div class="receipt-line"><span>{j.ke}. {j.jatuh_tempo}</span><strong>{rp(j.angsuran)}</strong></div>{/each}{/if}
  <button class="primary wide no-print" on:click={() => window.print()}>Cetak struk</button>
</section></div>
