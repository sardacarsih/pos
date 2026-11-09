import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { Draft, User } from './types';
vi.mock('./api', () => ({ api: vi.fn(), ApiError: class ApiError extends Error { constructor(message: string, public status = 0) { super(message); } } }));
import { api, ApiError } from './api';
import { readDraft, saveDraft, submitDraft } from './checkout';
const user: User = { id: 7, role: 'KASIR', nama: 'Kasir', nik: null, default_nik: '00', ganti_password: false };
const draft: Draft = { key: 'd9428888-122b-4f8a-8e3c-20d3c7eab5f1', userId: 7, role: 'KASIR', nik: '00', tenor: 1, uang_bayar: 100000, baris: [{ product_id: 1, qty: 2 }] };
const storage = new Map<string,string>();
beforeEach(() => { storage.clear(); vi.resetAllMocks(); vi.stubGlobal('localStorage', { getItem: (k: string) => storage.get(k) ?? null, setItem: (k:string,v:string) => storage.set(k,v), removeItem: (k:string) => storage.delete(k) }); });
describe('checkout', () => {
 it('persists number and replays exactly the same payload after a network failure', async () => {
   saveDraft(user, draft);
   vi.mocked(api).mockResolvedValueOnce({ no_transaksi: 'BBM-1' }).mockRejectedValueOnce(new ApiError('tidak ada',404)).mockRejectedValueOnce(new ApiError('offline'));
   await expect(submitDraft(user, draft)).rejects.toThrow('offline');
   expect(readDraft(user)?.nomor).toBe('BBM-1');
   vi.mocked(api).mockRejectedValueOnce(new ApiError('tidak ada',404)).mockResolvedValueOnce({ no_transaksi: 'BBM-1' });
   await submitDraft(user, readDraft(user)!);
   const calls = vi.mocked(api).mock.calls.filter(([path]) => path === '/faktur');
   expect(calls).toHaveLength(2); expect(calls[0][1]).toEqual(calls[1][1]); expect(readDraft(user)).toBeNull();
 });
 it('reconciles an already saved invoice without sending it again', async () => {
   const pending = {...draft, nomor:'BBM-1'}; saveDraft(user,pending); vi.mocked(api).mockResolvedValueOnce({ no_transaksi:'BBM-1' });
   await submitDraft(user,pending); expect(api).toHaveBeenCalledTimes(1); expect(readDraft(user)).toBeNull();
 });
 it('keeps uncertain failures but clears definite validation rejections', async () => {
   const pending = {...draft, nomor:'BBM-1'}; saveDraft(user,pending); vi.mocked(api).mockRejectedValueOnce(new ApiError('not found',404)).mockRejectedValueOnce(new ApiError('limit terlampaui',400));
   await expect(submitDraft(user,pending)).rejects.toThrow('limit'); expect(readDraft(user)).toBeNull();
 });
 it('isolates drafts by user and role', () => { saveDraft(user,draft); expect(readDraft({...user,id:8})).toBeNull(); expect(readDraft({...user,role:'ANGGOTA'})).toBeNull(); });
});
