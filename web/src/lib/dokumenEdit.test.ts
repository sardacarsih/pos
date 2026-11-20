import { describe, expect, it } from 'vitest';
import { bolehSimpan, kunciBaru, teksQty, type BarisEdit } from './dokumenEdit';

const b = (qty: string): BarisEdit => ({ produk_id: 1, kode: 'K', nama: 'Tepung', satuan: 'Kg', qty, keterangan: '' });

describe('bolehSimpan', () => {
  it('menolak daftar kosong, angka tidak valid, nol bila tidak diizinkan, dan alasan kosong', () => {
    expect(bolehSimpan([], 'x', true, true)).toMatch(/kosong/);
    expect(bolehSimpan([b('')], 'x', true, true)).toMatch(/Tepung/);
    expect(bolehSimpan([b('abc')], 'x', true, true)).toMatch(/valid/);
    expect(bolehSimpan([b('0')], 'x', true, false)).toMatch(/lebih dari 0/);
    expect(bolehSimpan([b('0')], 'x', true, true)).toBeNull();
    expect(bolehSimpan([b('2,5')], '  ', true, true)).toBe('Alasan wajib diisi.');
    expect(bolehSimpan([b('2,5')], '', false, true)).toBeNull();
  });
});

describe('kunciBaru', () => {
  it('memakai kunci lama selama isi sama dan membuat kunci baru saat isi berubah', () => {
    let n = 0;
    const buat = () => 'k' + ++n;
    const [k1, s1] = kunciBaru('awal', '', 'a', buat);
    expect([k1, s1]).toEqual(['k1', 'a']);
    expect(kunciBaru(k1, s1, 'a', buat)).toEqual(['k1', 'a']);
    expect(kunciBaru(k1, s1, 'b', buat)).toEqual(['k2', 'b']);
  });
});

describe('teksQty', () => {
  it('mengubah angka API menjadi teks isian', () => {
    expect(teksQty(3)).toBe('3');
    expect(teksQty('2.5000')).toBe('2.5');
    expect(teksQty('x')).toBe('');
  });
});
