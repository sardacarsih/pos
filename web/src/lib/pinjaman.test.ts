import { describe, expect, it } from 'vitest';
import { aksiTersedia, cekAjuan, type Pengajuan } from './pinjaman';

const p = (x: Partial<Pengajuan>): Pengajuan => ({
  id: 1, nik: 'N1', nama: 'Ani', jumlah: 1000, tenor: 6, bunga: 1.6, angsuran: 100, keperluan: 'x', status: 'DIAJUKAN', tgl_aju: '',
  verif_nama: null, verif_tgl: null, verif_catatan: null, setuju_nama: null, setuju_tgl: null, setuju_catatan: null,
  tolak_nama: null, tolak_tgl: null, alasan: null, cair_nama: null, cair_tgl: null, no_transaksi: null,
  antrian: null, verif_user: null, setuju_user: null, ...x
});

describe('aksi pengurus', () => {
  it('mengikuti status dan hak tahap', () => {
    expect(aksiTersedia(p({}), ['VERIFIKASI'], 9, false)).toEqual(['verifikasi', 'tolak']);
    expect(aksiTersedia(p({}), ['CAIR'], 9, false)).toEqual([]);
    expect(aksiTersedia(p({ status: 'DIVERIFIKASI' }), ['SETUJU'], 9, false)).toEqual(['setujui', 'tolak']);
    expect(aksiTersedia(p({ status: 'DICAIRKAN' }), ['VERIFIKASI', 'SETUJU', 'CAIR'], 9, true)).toEqual([]);
  });
  it('pengurus yang sudah memegang tahap sebelumnya tidak ditawari tahap berikut', () => {
    expect(aksiTersedia(p({ status: 'DIVERIFIKASI', verif_user: 9 }), ['SETUJU'], 9, false)).toEqual(['tolak']);
    expect(aksiTersedia(p({ status: 'DISETUJUI', setuju_user: 9 }), ['CAIR'], 9, true)).toEqual([]);
  });
  it('pencairan hanya untuk kepala antrian', () => {
    expect(aksiTersedia(p({ status: 'DISETUJUI' }), ['CAIR'], 9, false)).toEqual([]);
    expect(aksiTersedia(p({ status: 'DISETUJUI' }), ['CAIR'], 9, true)).toEqual(['cairkan']);
  });
});

describe('validasi pengajuan', () => {
  const a = { bunga: 1.6, maks_tenor: 12, maks_pinjaman: 5_000_000 };
  it('jumlah, plafon, tenor, keperluan', () => {
    expect(cekAjuan(undefined, 6, 'x', a)).toMatch(/Jumlah/);
    expect(cekAjuan(6_000_000, 6, 'x', a)).toMatch(/maksimal/);
    expect(cekAjuan(1_000_000, 13, 'x', a)).toMatch(/Tenor/);
    expect(cekAjuan(1_000_000, 6, ' ', a)).toMatch(/Keperluan/);
    expect(cekAjuan(1_000_000, 6, 'Renovasi', a)).toBe('');
    expect(cekAjuan(99_000_000, 6, 'x', { ...a, maks_pinjaman: 0 })).toBe('');
  });
});
