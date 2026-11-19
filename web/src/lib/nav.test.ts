import { describe, expect, it } from 'vitest';
import { MAKS_BOTTOM_NAV, VIEW_DIKENAL, ariaBadge, itemUtama, jumlahBadge, labelBadge, labelView, menuUntuk, semuaItem } from './nav';
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
    expect(m.kelompok.map(k => k.judul)).toEqual(['Ringkasan', 'Pantau', 'Katalog & stok', 'Anggota & keuangan', 'Pengelolaan', 'Akun']);
    expect(semuaItem(m).filter(i => i.badge).map(i => i.id)).toEqual(['stok', 'harga']);
  });
  it('kasir dan anggota tidak melihat menu admin', () => {
    for (const p of ['KASIR', 'ANGGOTA'] as Role[]) {
      const ids = semuaItem(menuUntuk(p)).map(i => i.id);
      for (const admin of ['users', 'produk', 'stok', 'opname', 'harga', 'kategori', 'urut', 'rekap', 'tagihan-rekap']) expect(ids).not.toContain(admin);
    }
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
    const stok = semuaItem(menuUntuk('ADMIN')).find(i => i.id === 'stok')!;
    const beranda = semuaItem(menuUntuk('ADMIN')).find(i => i.id === 'dashboard')!;
    expect(jumlahBadge(stok, { stok_habis: 12, harga_rendah: 3 })).toBe(12);
    expect(jumlahBadge(beranda, { stok_habis: 12 })).toBe(0);
    expect(jumlahBadge(stok, {})).toBe(0);
    expect(ariaBadge(stok, 12)).toBe('Stok habis, 12 produk perlu perhatian');
    expect(ariaBadge(stok, 0)).toBe('Stok habis');
  });
});
