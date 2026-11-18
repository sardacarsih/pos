import { describe, expect, it } from 'vitest';
import { delta, hariSingkat, persenTerpakai, proporsi, singkat, statusLimit, teksDelta, tinggiBatang } from './beranda';

describe('limit anggota', () => {
  it('status mengikuti batas 80% dan 100%', () => {
    expect(statusLimit(0, 0)).toBe('tak-dibatasi');
    expect(statusLimit(500, 1000)).toBe('aman');
    expect(statusLimit(799, 1000)).toBe('aman');
    expect(statusLimit(800, 1000)).toBe('hampir');
    expect(statusLimit(1000, 1000)).toBe('hampir');
    expect(statusLimit(1001, 1000)).toBe('terlampaui');
  });
  it('persen terpakai dibatasi 0..100 dan 0 untuk tanpa limit', () => {
    expect(persenTerpakai(250, 1000)).toBe(25);
    expect(persenTerpakai(5000, 1000)).toBe(100);
    expect(persenTerpakai(-5, 1000)).toBe(0);
    expect(persenTerpakai(100, 0)).toBe(0);
  });
});

describe('pembanding KPI', () => {
  it('menghitung naik, turun, sama, dan tanpa pembanding', () => {
    expect(delta(150, 100)).toEqual({ arah: 'naik', persen: 50 });
    expect(delta(50, 100)).toEqual({ arah: 'turun', persen: -50 });
    expect(delta(100, 100)).toEqual({ arah: 'tetap', persen: 0 });
    expect(delta(100, 0)).toEqual({ arah: 'tidak-ada', persen: 0 });
  });
  it('teks tidak menampilkan angka palsu bila tanpa pembanding', () => {
    expect(teksDelta(delta(100, 0))).toBe('—');
    expect(teksDelta(delta(150, 100))).toBe('▲ 50% vs kemarin');
    expect(teksDelta(delta(50, 100))).toBe('▼ 50% vs kemarin');
    expect(teksDelta(delta(100, 100))).toBe('sama dengan kemarin');
  });
});

describe('proporsi dan grafik', () => {
  it('proporsi berjumlah 100 atau 0/0', () => {
    expect(proporsi(1, 2)).toEqual([33, 67]);
    expect(proporsi(0, 0)).toEqual([0, 0]);
    expect(proporsi(5, 0)).toEqual([100, 0]);
  });
  it('tinggi batang relatif ke maksimum dan minimal terlihat', () => {
    const v = [0, 10, 100];
    expect(tinggiBatang(0, v)).toBe(0);
    expect(tinggiBatang(100, v)).toBe(100);
    expect(tinggiBatang(10, v)).toBe(10);
    expect(tinggiBatang(1, [1, 1000])).toBe(4);
    expect(tinggiBatang(5, [0, 0])).toBe(0);
  });
  it('rupiah ringkas', () => {
    expect(singkat(950)).toBe('950');
    expect(singkat(12500)).toBe('13 rb');
    expect(singkat(1250000)).toBe('1,3 jt');
    expect(singkat(2500000000)).toBe('2,5 M');
  });
  it('hari singkat dari tanggal ISO', () => {
    expect(hariSingkat('2026-11-18').length).toBeGreaterThan(0);
    expect(hariSingkat('bukan-tanggal')).toBe('');
  });
});
