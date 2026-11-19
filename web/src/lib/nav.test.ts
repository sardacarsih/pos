import { describe, expect, it } from 'vitest';
import { MAKS_BOTTOM_NAV, MENU_OFFLINE, VIEW_DIKENAL, ariaBadge, itemUtama, jumlahBadge, labelBadge, labelView, menuUntuk, semuaItem } from './nav';
import type { Role } from './types';

const PERAN: Role[] = ['KASIR', 'ANGGOTA', 'ADMIN'];

describe('struktur menu', () => {
  for (const p of PERAN) {
    const m = menuUntuk(p);
    const semua = semuaItem(m);
    it(`${p}: id unik, kelompok tidak kosong, dan semua id dikenali router`, () => {
      expect(new Set(semua.map(i => i.id)).size).toBe(semua.length);
      expect(m.kelompok.every(k => k.item.length > 0)).toBe(true);
      expect(semua.every(i => (VIEW_DIKENAL as readonly string[]).includes(i.id))).toBe(true);
    });
    it(`${p}: Beranda selalu item pertama dan Profil tersedia`, () => {
      expect(semua[0].id).toBe('dashboard');
      expect(semua.some(i => i.id === 'profil')).toBe(true);
    });
    it(`${p}: bottom-nav (utama + Lainnya) tidak melebihi ${MAKS_BOTTOM_NAV} dan semua idnya ada di menu`, () => {
      expect(m.utama.length + 1).toBeLessThanOrEqual(MAKS_BOTTOM_NAV);
      expect(itemUtama(m).length).toBe(m.utama.length);
    });
  }
  it('menu admin dikelompokkan dan lencana hanya pada item pantau', () => {
    const m = menuUntuk('ADMIN');
    expect(m.kelompok.map(k => k.judul)).toEqual(['Ringkasan', 'Pantau', 'Katalog', 'Pengadaan', 'Anggota & keuangan', 'Pengelolaan', 'Akun']);
    expect(semuaItem(m).filter(i => i.badge).map(i => i.id)).toEqual(['persediaan', 'harga']);
  });
  it('kasir dan anggota tidak melihat menu admin', () => {
    for (const p of ['KASIR', 'ANGGOTA'] as Role[]) {
      const ids = semuaItem(menuUntuk(p)).map(i => i.id);
      for (const admin of ['users', 'produk', 'persediaan', 'harga', 'rekap', 'tagihan-rekap', 'pembelian', 'kas']) expect(ids).not.toContain(admin);
    }
  });
  it('Stok habis dan Stock opname menjadi tab di Persediaan; menu lama dihapus', () => {
    const admin = semuaItem(menuUntuk('ADMIN')).map(i => i.id);
    for (const pindah of ['kategori', 'urut', 'supplier', 'hutang-supplier', 'stok', 'opname']) expect(admin).not.toContain(pindah);
    const sedia = semuaItem(menuUntuk('ADMIN')).find(i => i.id === 'persediaan')!;
    expect(sedia).toEqual({ id: 'persediaan', label: 'Persediaan', pendek: 'Stok', ikon: 'stokHabis', badge: 'stok_habis' });
    expect((VIEW_DIKENAL as readonly string[])).toContain('persediaan');
    expect((VIEW_DIKENAL as readonly string[])).not.toContain('stok');
    expect((VIEW_DIKENAL as readonly string[])).not.toContain('opname');
    expect(menuUntuk('ADMIN').utama).toEqual(['dashboard', 'persediaan', 'rekap']);
    expect(semuaItem(menuUntuk('PEMBELIAN')).map(i => i.id)).toEqual(['pembelian']);
    expect(itemUtama(menuUntuk('PEMBELIAN')).map(i => i.id)).toEqual(['pembelian']);
    expect(semuaItem(MENU_OFFLINE).map(i => i.id)).toEqual(['pembelian']);
  });
  it('label view mengikuti menu peran', () => {
    expect(labelView(menuUntuk('KASIR'), 'belanja')).toBe('Kasir');
    expect(labelView(menuUntuk('ANGGOTA'), 'belanja')).toBe('Belanja');
    expect(labelView(menuUntuk('ADMIN'), 'tidak-ada')).toBe('');
  });
});

describe('lencana', () => {
  it('label: kosong untuk 0, angka apa adanya, 99+ untuk >= 100', () => {
    expect(labelBadge(0)).toBe('');
    expect(labelBadge(-3)).toBe('');
    expect(labelBadge(undefined)).toBe('');
    expect(labelBadge(Number.NaN)).toBe('');
    expect(labelBadge(5)).toBe('5');
    expect(labelBadge(99)).toBe('99');
    expect(labelBadge(100)).toBe('99+');
    expect(labelBadge(2500)).toBe('99+');
  });
  it('jumlah dan teks aksesibel mengikuti jenis lencana item', () => {
    const sedia = semuaItem(menuUntuk('ADMIN')).find(i => i.id === 'persediaan')!;
    const beranda = semuaItem(menuUntuk('ADMIN')).find(i => i.id === 'dashboard')!;
    expect(jumlahBadge(sedia, { stok_habis: 12, harga_rendah: 3 })).toBe(12);
    expect(jumlahBadge(beranda, { stok_habis: 12 })).toBe(0);
    expect(jumlahBadge(sedia, {})).toBe(0);
    expect(ariaBadge(sedia, 12)).toBe('Persediaan, 12 produk perlu perhatian');
    expect(ariaBadge(sedia, 0)).toBe('Persediaan');
  });
});
