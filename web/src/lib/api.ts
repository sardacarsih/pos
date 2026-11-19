import type { User } from './types';
const prefix = '/api/v1';
const tokenKey = 'kopkar_web_tokens';
const izinOfflineKey = 'kopkar_web_pembelian_offline';
type Tokens = { access_token: string; refresh_token: string };
export class ApiError extends Error { constructor(message: string, public status = 0, public code = '') { super(message); } }
// Tanpa "ingat saya" token di sessionStorage (hilang saat tab ditutup); dengan centang di localStorage.
function wadah(): Storage | null { try { return sessionStorage.getItem(tokenKey) ? sessionStorage : localStorage.getItem(tokenKey) ? localStorage : null; } catch { return null; } }
function tokens(): Tokens | null { try { return JSON.parse(wadah()?.getItem(tokenKey) || 'null'); } catch { return null; } }
function milikAkun(t: Tokens | null, id: number): boolean {
  try {
    const payload = t?.access_token.split('.')[1]?.replace(/-/g, '+').replace(/_/g, '/');
    return !!payload && JSON.parse(atob(payload)).sub === id;
  } catch { return false; }
}
function saveTokens(t: Tokens, ingat?: boolean) {
  const target = ingat === undefined ? (wadah() ?? localStorage) : ingat ? localStorage : sessionStorage;
  (target === localStorage ? sessionStorage : localStorage).removeItem(tokenKey);
  target.setItem(tokenKey, JSON.stringify({ access_token: t.access_token, refresh_token: t.refresh_token }));
}
function kunciIzinOffline() {
  localStorage.removeItem(izinOfflineKey);
  sessionStorage.removeItem(izinOfflineKey);
}
export function clearSession() { kunciIzinOffline(); localStorage.removeItem(tokenKey); sessionStorage.removeItem(tokenKey); }
export function sesiPembelianOffline(): User | null {
  try {
    const token = tokens();
    if (!token) return null;
    const izin = JSON.parse(wadah()?.getItem(izinOfflineKey) || 'null') as { user: User; expires: number } | null;
    return izin && izin.expires > Date.now() && milikAkun(token, izin.user.id) ? izin.user : null;
  } catch { return null; }
}
function deviceId() { let id = localStorage.getItem('kopkar_web_device'); if (!id) { id = crypto.randomUUID(); localStorage.setItem('kopkar_web_device', id); } return id; }
let refreshing: Promise<boolean> | null = null;
async function refresh(oldAccess: string): Promise<boolean> {
  const work = async () => {
    const t = tokens(); if (!t) return false;
    if (t.access_token !== oldAccess) return true;
    let response: Response;
    try { response = await fetch(prefix + '/web/auth/refresh', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ refresh_token: t.refresh_token, device_id: deviceId() }), signal: AbortSignal.timeout(15_000) }); } catch { throw new ApiError('Tidak dapat terhubung ke server'); }
    if (!response.ok) {
      if (response.status === 401 && tokens()?.refresh_token === t.refresh_token) clearSession();
      if (response.status >= 500 || response.status === 429) {
        throw new ApiError('Server belum dapat memulihkan sesi', response.status);
      }
      return false;
    }
    const next = await response.json();
    if (tokens()?.refresh_token === t.refresh_token) saveTokens(next);
    return true;
  };
  if (!refreshing) {
    const operation: Promise<boolean> = navigator.locks ? new Promise<boolean>((resolve, reject) => { navigator.locks.request('kopkar-token-refresh', async () => { try { resolve(await work()); } catch (e) { reject(e); } }); }) : work();
    refreshing = operation.finally(() => { refreshing = null; });
  }
  return refreshing!;
}
export async function api<T = any>(path: string, options: { method?: string; body?: unknown; headers?: Record<string, string>; auth?: boolean; raw?: boolean; userId?: number } = {}): Promise<T> {
  const { method = 'GET', body, headers = {}, auth = true, raw = false, userId } = options;
  const request = async () => {
    const t = tokens(); const h = new Headers(headers);
    if (userId !== undefined && !milikAkun(t, userId)) throw new ApiError('Masuk kembali dengan akun pemilik data', 401);
    if (auth && t) h.set('Authorization', `Bearer ${t.access_token}`);
    if (body !== undefined && !(body instanceof Blob)) h.set('Content-Type', 'application/json');
    const signal = userId !== undefined || path === '/web/me' ? AbortSignal.timeout(15_000) : undefined;
    try { return await fetch(prefix + path, { method, headers: h, signal, body: body === undefined ? undefined : body instanceof Blob ? body : JSON.stringify(body) }); }
    catch { throw new ApiError('Tidak dapat terhubung ke server'); }
  };
  const oldAccess = tokens()?.access_token || '';
  let response = await request();
  if (response.status === 401 && auth && oldAccess && await refresh(oldAccess)) response = await request();
  if (!response.ok) { let error: any = {}; try { error = await response.json(); } catch {} throw new ApiError(error.pesan || `Permintaan gagal (${response.status})`, response.status, error.kode); }
  if (response.status === 204) return undefined as T;
  return (raw ? await response.blob() : await response.json()) as T;
}
async function profilTerverifikasi(): Promise<User> {
  const user = await api<User>('/web/me');
  kunciIzinOffline();
  if (!user.ganti_password && (user.role === 'ADMIN' || user.role === 'PEMBELIAN')) {
    wadah()?.setItem(izinOfflineKey, JSON.stringify({ user, expires: Date.now() + 8 * 60 * 60 * 1000 }));
  }
  return user;
}
export async function login(username: string, password: string, ingat = false): Promise<User> {
  const t = await api<Tokens>('/web/auth/login', { method: 'POST', auth: false, body: { username, password, device_id: deviceId(), ingat } });
  kunciIzinOffline();
  saveTokens(t, ingat);
  return profilTerverifikasi();
}
export async function restore(): Promise<User | null> {
  if (!tokens()) return null;
  try { return await profilTerverifikasi(); }
  catch (e) {
    if (e instanceof ApiError && e.status === 401) { clearSession(); return null; }
    throw e;
  }
}
export async function logout() {
  const t = tokens();
  clearSession();
  if (t) await api('/auth/logout', { method: 'POST', auth: false, body: { refresh_token: t.refresh_token } });
}
export const query = (v: Record<string, string | number | undefined>) => '?' + new URLSearchParams(Object.entries(v).filter(([, x]) => x !== undefined && x !== '').map(([k, x]) => [k, String(x)])).toString();
