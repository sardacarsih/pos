import { api, ApiError } from './api';
import type { Draft, User } from './types';
const keyFor = (u: User) => `kopkar_checkout_${u.role}_${u.id}`;
export function readDraft(u: User): Draft | null { try { return JSON.parse(localStorage.getItem(keyFor(u)) || 'null'); } catch { throw new Error('Data checkout tersimpan tidak dapat dibaca. Hubungi administrator.'); } }
export function saveDraft(u: User, d: Draft) { localStorage.setItem(keyFor(u), JSON.stringify(d)); }
export function clearDraft(u: User) { localStorage.removeItem(keyFor(u)); }
export async function submitDraft(u: User, draft: Draft) {
  const member = u.role === 'ANGGOTA'; const headers = { 'Idempotency-Key': draft.key };
  if (!draft.nomor) { const n = await api<{ no_transaksi: string }>(member ? '/saya/belanja/nomor' : '/transaksi/nomor', { method: 'POST', headers }); draft = { ...draft, nomor: n.no_transaksi }; saveDraft(u, draft); }
  if (!member) { try { const found = await api('/faktur/' + encodeURIComponent(draft.nomor!)); clearDraft(u); return found; } catch (e) { if (!(e instanceof ApiError) || e.status !== 404) throw e; } }
  const body = { no_transaksi: draft.nomor, nik: draft.nik, tenor: draft.tenor, ...(draft.uang_bayar === undefined ? {} : { uang_bayar: draft.uang_bayar }), baris: draft.baris };
  try { const result = await api(member ? '/saya/belanja' : '/faktur', { method: 'POST', headers, body }); clearDraft(u); return result; } catch (e) { if (e instanceof ApiError && [400, 403, 422].includes(e.status)) clearDraft(u); throw e; }
}
