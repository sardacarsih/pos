import { describe, expect, it } from 'vitest';
import { indeksMenu, inisial, jejak, judulDokumen, namaPeriode, statusPeriode } from './topbar';
import { menuUntuk } from './nav';

describe('breadcrumb', () => {
  it('admin: peran lalu kelompok menu', () => {
    const m = menuUntuk('ADMIN');
    expect(jejak(m, 'opname', 'ADMIN')).toEqual(['Admin', 'Katalog & stok']);
    expect(jejak(m, 'stok', 'ADMIN')).toEqual(['Admin', 'Pantau']);
    expect(jejak(m, 'users', 'ADMIN')).toEqual(['Admin', 'Pengelolaan']);
    expect(jejak(m, 'profil', 'ADMIN')).toEqual(['Admin', 'Akun']);
  });
  it('kelompok Ringkasan dan kelompok tanpa judul tidak ditampilkan', () => {
    expect(jejak(menuUntuk('ADMIN'), 'dashboard', 'ADMIN')).toEqual(['Admin']);
    expect(jejak(menuUntuk('KASIR'), 'riwayat', 'KASIR')).toEqual(['Kasir']);
    expect(jejak(menuUntuk('ANGGOTA'), 'kartu', 'ANGGOTA')).toEqual(['Anggota']);
  });
  it('view tak dikenal hanya menampilkan peran', () => {
    expect(jejak(menuUntuk('KASIR'), 'tidak-ada', 'KASIR')).toEqual(['Kasir']);
  });
});

describe('status periode', () => {
  it('terbuka: label memuat nama periode dan rentang tanggal sebagai judul', () => {
    const s = statusPeriode({ terbuka: true, periode: 202611, dari: '2026-11-01', sampai: '2026-11-30' })!;
    expect(s).toEqual({ label: 'Periode Nov 2026 · Terbuka', tingkat: 'ok', judul: '2026-11-01 s/d 2026-11-30' });
    expect(statusPeriode({ terbuka: true })!.label).toBe('Periode terbuka');
  });
  it('tutup: bahaya dengan pesan server; tidak ada/valid: peringatan', () => {
    expect(statusPeriode({ terbuka: false, kode: 'PERIODE_TUTUP', pesan: 'Periode sudah ditutup' })).toEqual({ label: 'Periode tutup', tingkat: 'bahaya', judul: 'Periode sudah ditutup' });
    expect(statusPeriode({ terbuka: false, kode: 'PERIODE_TIDAK_ADA' })!.tingkat).toBe('peringatan');
    expect(statusPeriode({ terbuka: false, kode: 'PERIODE_TIDAK_VALID' })!.label).toBe('Periode belum valid');
  });
  it('respons tak dikenali tidak menghasilkan chip', () => {
    expect(statusPeriode(null)).toBeNull();
    expect(statusPeriode(undefined)).toBeNull();
    expect(statusPeriode({} as never)).toBeNull();
  });
  it('nama periode', () => {
    expect(namaPeriode(202601)).toBe('Jan 2026');
    expect(namaPeriode(202612)).toBe('Des 2026');
    expect(namaPeriode(202613)).toBe('202613');
  });
});

describe('avatar dan judul', () => {
  it('inisial', () => {
    expect(inisial('Dadang Admin')).toBe('DA');
    expect(inisial('  siti   aminah  rahma ')).toBe('SA');
    expect(inisial('Rina')).toBe('R');
    expect(inisial('')).toBe('?');
    expect(inisial('   ')).toBe('?');
  });
  it('judul dokumen', () => {
    expect(judulDokumen('Stok habis')).toBe('Stok habis · Kopkar POS');
    expect(judulDokumen('')).toBe('Kopkar POS');
  });
});

describe('navigasi keyboard menu', () => {
  it('panah berputar, Home/End melompat, tombol lain diam', () => {
    expect(indeksMenu(0, 3, 'ArrowDown')).toBe(1);
    expect(indeksMenu(2, 3, 'ArrowDown')).toBe(0);
    expect(indeksMenu(0, 3, 'ArrowUp')).toBe(2);
    expect(indeksMenu(1, 3, 'Home')).toBe(0);
    expect(indeksMenu(0, 3, 'End')).toBe(2);
    expect(indeksMenu(1, 3, 'a')).toBe(1);
    expect(indeksMenu(0, 0, 'ArrowDown')).toBe(0);
  });
});
