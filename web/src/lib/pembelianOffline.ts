import { api, ApiError } from './api';
import { payloadBeli, type DrafBeli, type PayloadBeli, type ReferensiBeli } from './pembelian';

export type AntreanBeli = {
  id: string; draf: DrafBeli; payload: PayloadBeli; dibuat: string;
  status: 'MENUNGGU' | 'TERSINKRON' | 'PERBAIKI'; nomor?: string; galat?: string;
};
type Workspace = { draf?: DrafBeli; antrean: AntreanBeli[] };
const DB = 'kopkar-pembelian';
const STORE = 'akun';
const KATALOG = 'katalog';
const EVENT = 'kopkar-pembelian-berubah';

function buka(): Promise<IDBDatabase> {
  return new Promise((resolve, reject) => {
    const req = indexedDB.open(DB, 1);
    req.onupgradeneeded = () => {
      req.result.createObjectStore(STORE);
      req.result.createObjectStore(KATALOG);
    };
    req.onsuccess = () => resolve(req.result);
    req.onerror = () => reject(req.error);
    req.onblocked = () => reject(new Error('Tutup tab POS lain untuk menyiapkan penyimpanan pembelian.'));
  });
}

async function akses<T>(id: number, nama: string, ubah?: (data: T | undefined) => T): Promise<T | undefined> {
  const db = await buka();
  try {
    return await new Promise((resolve, reject) => {
      const tx = db.transaction(nama, ubah ? 'readwrite' : 'readonly');
      const store = tx.objectStore(nama);
      const read = store.get(id);
      let result: T | undefined;
      let gagal: unknown;
      read.onsuccess = () => {
        result = read.result;
        if (ubah) {
          try { result = ubah(result); store.put(result, id); }
          catch (e) { gagal = e; tx.abort(); }
        }
      };
      tx.oncomplete = () => resolve(result);
      tx.onabort = () => reject(gagal ?? tx.error ?? new Error('Penyimpanan pembelian gagal.'));
    });
  } finally { db.close(); }
}

async function workspace(id: number, ubah?: (data: Workspace) => void): Promise<Workspace> {
  return await akses<Workspace>(id, STORE, ubah && (lama => {
    const data = lama ?? { antrean: [] };
    ubah(data);
    return data;
  })) ?? { antrean: [] };
}

export async function bacaPembelian(id: number) {
  const [data, referensi] = await Promise.all([workspace(id), akses<ReferensiBeli>(id, KATALOG)]);
  return { ...data, referensi };
}

export async function bacaAntreanBeli(id: number) {
  return (await workspace(id)).antrean;
}

export async function simpanDrafBeli(id: number, draf: DrafBeli) {
  const data = structuredClone(draf);
  await workspace(id, w => {
    if (!w.antrean.some(a => a.id === data.id)) w.draf = data;
  });
}

export async function muatReferensiBeli(id: number) {
  const data = await api<ReferensiBeli>('/pembelian/referensi', { userId: id });
  await akses<ReferensiBeli>(id, KATALOG, () => data);
  return data;
}

function beriTahu(id: number) {
  window.dispatchEvent(new CustomEvent(EVENT, { detail: id }));
  if (typeof BroadcastChannel !== 'undefined') {
    const channel = new BroadcastChannel(EVENT);
    channel.postMessage(id);
    channel.close();
  }
}

export function ikutiAntrean(id: number, handler: () => void) {
  const lokal = (e: Event) => { if ((e as CustomEvent<number>).detail === id) handler(); };
  const channel = typeof BroadcastChannel === 'undefined' ? null : new BroadcastChannel(EVENT);
  if (channel) channel.onmessage = e => { if (e.data === id) handler(); };
  window.addEventListener(EVENT, lokal);
  return () => { window.removeEventListener(EVENT, lokal); channel?.close(); };
}

export async function antrekanBeli(id: number, draf: DrafBeli) {
  const data = structuredClone(draf);
  const payload = payloadBeli(data);
  await workspace(id, w => {
    const existing = w.antrean.find(a => a.id === data.id);
    if (existing) {
      if (JSON.stringify(existing.payload) !== JSON.stringify(payload)) {
        throw new Error('Draf ini sudah masuk antrean dari tab lain. Buka antrean untuk melihat transaksi.');
      }
      return;
    }
    if (data.ganti) {
      if (!w.antrean.some(a => a.id === data.ganti && a.status === 'PERBAIKI')) {
        throw new Error('Transaksi awal sudah diperbaiki dari tab lain.');
      }
      w.antrean = w.antrean.filter(a => a.id !== data.ganti);
    }
    w.antrean.push({ id: data.id, draf: data, payload, status: 'MENUNGGU', dibuat: new Date().toISOString() });
    if (w.draf?.id === data.id) delete w.draf;
  });
  beriTahu(id);
}

const sinkron = new Map<number, Promise<boolean>>();

export function sinkronPembelian(id: number): Promise<boolean> {
  const proses = sinkron.get(id);
  if (proses) return proses;
  if (!navigator.onLine) return Promise.resolve(false);
  const kirim = async () => {
    const { antrean } = await workspace(id);
    for (const a of antrean) {
      if (a.status !== 'MENUNGGU') continue;
      let hasil: Partial<AntreanBeli>;
      try {
        const r = await api<{ no_transaksi: string }>('/pembelian', {
          method: 'POST', body: a.payload, headers: { 'Idempotency-Key': a.id }, userId: id
        });
        if (!r.no_transaksi) return false;
        hasil = { status: 'TERSINKRON', nomor: r.no_transaksi, galat: undefined };
      } catch (e) {
        const definitif = e instanceof ApiError && [400, 404, 409, 422].includes(e.status);
        if (!definitif) return false;
        hasil = { status: 'PERBAIKI', galat: (e as ApiError).message };
      }
      await workspace(id, w => {
        const masih = w.antrean.find(x => x.id === a.id);
        if (masih?.status === 'MENUNGGU') Object.assign(masih, hasil);
        const lama = w.antrean.filter(x => x.status === 'TERSINKRON').slice(0, -30);
        if (lama.length) {
          const ids = new Set(lama.map(x => x.id));
          w.antrean = w.antrean.filter(x => !ids.has(x.id));
        }
      });
      beriTahu(id);
    }
    return true;
  };
  const kerja = Promise.resolve(navigator.locks
    ? navigator.locks.request(`kopkar-pembelian-${id}`, kirim)
    : kirim()).finally(() => sinkron.delete(id));
  sinkron.set(id, kerja);
  return kerja;
}
