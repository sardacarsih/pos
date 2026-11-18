export type Metode = 'TUNAI' | 'TRANSFER' | 'LAINNYA';
export type Jenis = 'TUNAI' | 'TEMPO';
export type Supplier = { kode: string; nama: string };
export type SatuanBeli = { satuan: string; isi: string; barcode: string };
export type BarangBeli = { id: number; kode: string; nama: string; beli: string; satuan: SatuanBeli[] };
export type ReferensiBeli = { tanggal_server: string; supplier: Supplier[]; barang: BarangBeli[] };
export type PilihanBarang = { produk: BarangBeli; satuan: SatuanBeli };
export type BarisBeli = {
  id: string; product_id: number; kode: string; nama: string; satuan: string; isi: string;
  qty: string; harga: number | undefined; potongan: number | undefined;
};
export type DrafBeli = {
  id: string; ganti?: string; supplier_id: string; no_faktur: string; tanggal_faktur: string;
  tanggal_terima: string; jenis: Jenis; jatuh_tempo: string; uang_muka: number | undefined;
  metode: Metode; referensi: string; baris: BarisBeli[];
};

export function hariJakarta(): string {
  const parts = new Intl.DateTimeFormat('en', {
    timeZone: 'Asia/Jakarta', year: 'numeric', month: '2-digit', day: '2-digit'
  }).formatToParts();
  const part = (name: string) => parts.find(p => p.type === name)?.value;
  return `${part('year')}-${part('month')}-${part('day')}`;
}

export function drafBaru(hari = hariJakarta()): DrafBeli {
  return {
    id: crypto.randomUUID(), supplier_id: '', no_faktur: '', tanggal_faktur: hari, tanggal_terima: hari,
    jenis: 'TUNAI', jatuh_tempo: '', uang_muka: 0, metode: 'TUNAI', referensi: '', baris: []
  };
}

export function cariBarang(barang: BarangBeli[], masukan: string) {
  const q = masukan.trim();
  if (!q) return { persis: [] as PilihanBarang[], hasil: [] as PilihanBarang[] };
  const norm = q.toLocaleLowerCase('id');
  const persis: PilihanBarang[] = [];
  const hasil: PilihanBarang[] = [];
  for (const p of barang) {
    for (const [index, s] of p.satuan.entries()) {
      if (s.barcode === q || (index === 0 && p.kode.toLocaleLowerCase('id') === norm)) {
        persis.push({ produk: p, satuan: s });
      } else if (hasil.length < 20 && (
        p.nama.toLocaleLowerCase('id').includes(norm) || p.kode.toLocaleLowerCase('id').includes(norm) ||
        s.barcode.includes(q)
      )) {
        hasil.push({ produk: p, satuan: s });
      }
    }
  }
  return { persis, hasil: persis.length ? persis : hasil };
}

export function cocokBarcode(barang: BarangBeli[], masukan: string): PilihanBarang[] {
  const barcode = masukan.trim();
  if (!barcode) return [];
  return barang.flatMap(produk => produk.satuan
    .filter(satuan => satuan.barcode === barcode)
    .map(satuan => ({ produk, satuan })));
}

const MAKS_RUPIAH = 9_000_000_000_000_000;

export function angkaQty(qty: string): string | null {
  const q = qty.trim().replace(',', '.');
  if (!/^\d{1,6}(\.\d{1,3})?$/.test(q) || Number(q) <= 0 || Number(q) > 999_999) return null;
  return q;
}

function nilaiRupiah(n: number | undefined): n is number {
  return n !== undefined && Number.isSafeInteger(n) && n >= 0 && n <= MAKS_RUPIAH;
}

const bulatIndonesia = /^(?:\d+|\d{1,3}(?:\.\d{3})+)$/;

export function bacaRupiahBeli(teks: string): number | null {
  const bersih = teks.trim().replace(/^rp\s*/i, '');
  if (!bulatIndonesia.test(bersih)) return null;
  const angka = Number(bersih.replaceAll('.', ''));
  return nilaiRupiah(angka) ? angka : null;
}

export function bacaJumlahBeli(teks: string): string | null {
  const [bulat, desimal, ...lebih] = teks.trim().split(',');
  if (lebih.length || !bulatIndonesia.test(bulat) || (desimal !== undefined && !/^\d{1,3}$/.test(desimal))) {
    return null;
  }
  const utama = bulat.replaceAll('.', '').replace(/^0+(?=\d)/, '');
  return angkaQty(desimal === undefined ? utama : `${utama}.${desimal}`);
}

export function formatJumlahBeli(nilai: number | string | null | undefined): string {
  if (nilai == null || nilai === '') return '—';
  const cocok = /^(-?)(\d+)(?:[.,](\d+))?$/.exec(String(nilai));
  if (!cocok) return '—';
  const [, tanda, bulat, desimal = ''] = cocok;
  const bagian = bulat.replace(/\B(?=(\d{3})+(?!\d))/g, '.');
  const pecahan = desimal.replace(/0+$/, '');
  return tanda + bagian + (pecahan ? ',' + pecahan : '');
}

export function brutoBaris(b: BarisBeli): number | null {
  const q = angkaQty(b.qty);
  if (!q || !nilaiRupiah(b.harga) || b.harga === 0) return null;
  const [bulat, pecahan = ''] = q.split('.');
  const mili = BigInt(bulat) * 1000n + BigInt(pecahan.padEnd(3, '0'));
  const bruto = (mili * BigInt(b.harga) + 500n) / 1000n;
  return bruto > 0n && bruto <= BigInt(MAKS_RUPIAH) ? Number(bruto) : null;
}

export function totalBeli(baris: BarisBeli[]) {
  let bruto = 0;
  let potongan = 0;
  for (const b of baris) {
    const kotor = brutoBaris(b);
    const diskon = b.potongan ?? 0;
    if (kotor === null || !nilaiRupiah(diskon) || diskon > kotor) return null;
    bruto += kotor;
    potongan += diskon;
    if (bruto > MAKS_RUPIAH) return null;
  }
  return { bruto, potongan, total: bruto - potongan };
}

export function payloadBeli(d: DrafBeli) {
  const total = totalBeli(d.baris);
  if (!d.supplier_id || !d.no_faktur.trim() || !d.tanggal_faktur || !d.tanggal_terima) {
    throw new Error('Lengkapi supplier, nomor faktur, dan tanggal.');
  }
  if (d.baris.length > 100 || !total || total.total <= 0) {
    throw new Error('Isi 1–100 baris barang dengan jumlah, harga, dan total yang valid.');
  }
  const tempo = d.jenis === 'TEMPO';
  const dp = d.uang_muka ?? 0;
  if (tempo && (!d.jatuh_tempo || d.jatuh_tempo < d.tanggal_faktur || !nilaiRupiah(dp) || dp >= total.total)) {
    throw new Error('Periksa tanggal jatuh tempo dan uang muka.');
  }
  const bayar = !tempo || dp > 0;
  return {
    supplier_id: d.supplier_id, no_faktur: d.no_faktur.trim(), tanggal_faktur: d.tanggal_faktur,
    tanggal_terima: d.tanggal_terima, jenis: d.jenis, jatuh_tempo: tempo ? d.jatuh_tempo : null,
    uang_muka: tempo ? dp : 0, metode: bayar ? d.metode : null, referensi: bayar ? d.referensi.trim() || null : null,
    baris: d.baris.map(b => ({
      product_id: b.product_id, satuan: b.satuan, isi: b.isi, qty: angkaQty(b.qty)!,
      harga: b.harga!, potongan: b.potongan ?? 0
    }))
  };
}

export type PayloadBeli = ReturnType<typeof payloadBeli>;
export type FakturBeli = {
  no_transaksi: string; supplier_id: string | null; supplier_nama: string | null; no_faktur: string | null;
  tanggal_faktur: string | null; tanggal_terima: string; jatuh_tempo: string | null; jenis: Jenis | null;
  bruto: string; potongan: string; total: string; operator: string | null; posted_at: string | null;
  batal_at: string | null; batal_nama: string | null; batal_alasan: string | null;
  sudah_bayar: number | null; sisa: number | null; asal: 'WEB' | 'BACKOFFICE';
  status: 'TERBUKA' | 'TERLAMBAT' | 'LUNAS' | 'BATAL' | 'SALDO_BELUM_DILACAK';
};
export type Pembayaran = {
  id: number; tanggal: string; metode: Metode; referensi: string | null; nominal: number; balik_id: number | null;
  alasan: string | null; operator: string; created_at: string; dibalik: boolean;
};
export type DetailBeli = FakturBeli & {
  tanggal_server: string;
  baris: { kode: string | null; nama: string | null; satuan: string | null; satuan_dasar: string | null;
    qty: string | null; isi: string; harga: string | null; potongan: string; total: string }[];
  pembayaran: Pembayaran[];
};
export type SaldoSupplier = { supplier_id: string; supplier_nama: string; saldo: string; terlambat: string; faktur: number };

export const labelFaktur: Record<FakturBeli['status'], string> = {
  TERBUKA: 'Terbuka', TERLAMBAT: 'Terlambat', LUNAS: 'Lunas', BATAL: 'Dibatalkan', SALDO_BELUM_DILACAK: 'Saldo belum dilacak'
};
export const metodeBayar: Record<Metode, string> = { TUNAI: 'Tunai', TRANSFER: 'Transfer', LAINNYA: 'Lainnya' };
export function rpBeli(n: number | string | null | undefined): string {
  const nilai = formatJumlahBeli(n);
  if (nilai === '—') return nilai;
  return nilai.startsWith('-') ? '-Rp ' + nilai.slice(1) : 'Rp ' + nilai;
}
