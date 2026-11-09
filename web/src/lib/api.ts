import type { User } from './types';
const prefix = '/api/v1';
const tokenKey = 'kopkar_web_tokens';
type Tokens = { access_token: string; refresh_token: string };
export class ApiError extends Error { constructor(message: string, public status = 0, public code = '') { super(message); } }
function tokens(): Tokens | null { try { return JSON.parse(localStorage.getItem(tokenKey) || 'null'); } catch { return null; } }
function saveTokens(t: Tokens) { localStorage.setItem(tokenKey, JSON.stringify({ access_token: t.access_token, refresh_token: t.refresh_token })); }
function deviceId() { let id = localStorage.getItem('kopkar_web_device'); if (!id) { id = crypto.randomUUID(); localStorage.setItem('kopkar_web_device', id); } return id; }
let refreshing: Promise<boolean> | null = null;
async function refresh(oldAccess: string): Promise<boolean> {
  const work = async () => {
    const t = tokens(); if (!t) return false;
    if (t.access_token !== oldAccess) return true;
    let response: Response;
    try { response = await fetch(prefix + '/auth/refresh', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ refresh_token: t.refresh_token, device_id: deviceId() }) }); } catch { throw new ApiError('Tidak dapat terhubung ke server'); }
    if (!response.ok) { if (response.status === 401) localStorage.removeItem(tokenKey); return false; }
    saveTokens(await response.json()); return true;
  };
  if (!refreshing) {
    const operation: Promise<boolean> = navigator.locks ? new Promise<boolean>((resolve, reject) => { navigator.locks.request('kopkar-token-refresh', async () => { try { resolve(await work()); } catch (e) { reject(e); } }); }) : work();
    refreshing = operation.finally(() => { refreshing = null; });
  }
  return refreshing!;
}
export async function api<T = any>(path: string, options: { method?: string; body?: unknown; headers?: Record<string, string>; auth?: boolean; raw?: boolean } = {}): Promise<T> {
  const { method = 'GET', body, headers = {}, auth = true, raw = false } = options;
  const request = async () => {
    const t = tokens(); const h = new Headers(headers);
    if (auth && t) h.set('Authorization', `Bearer ${t.access_token}`);
    if (body !== undefined && !(body instanceof Blob)) h.set('Content-Type', 'application/json');
    try { return await fetch(prefix + path, { method, headers: h, body: body === undefined ? undefined : body instanceof Blob ? body : JSON.stringify(body) }); }
    catch { throw new ApiError('Tidak dapat terhubung ke server'); }
  };
  const oldAccess = tokens()?.access_token || '';
  let response = await request();
  if (response.status === 401 && auth && oldAccess && await refresh(oldAccess)) response = await request();
  if (!response.ok) { let error: any = {}; try { error = await response.json(); } catch {} throw new ApiError(error.pesan || `Permintaan gagal (${response.status})`, response.status, error.kode); }
  if (response.status === 204) return undefined as T;
  return (raw ? await response.blob() : await response.json()) as T;
}
export async function login(username: string, password: string): Promise<User> { const t = await api<Tokens>('/auth/login', { method: 'POST', auth: false, body: { username, password, device_id: deviceId() } }); saveTokens(t); return api<User>('/me'); }
export async function restore(): Promise<User | null> { if (!tokens()) return null; try { return await api<User>('/me'); } catch (e) { if (e instanceof ApiError && e.status === 401) return null; throw e; } }
export async function logout() { const t = tokens(); try { if (t) await api('/auth/logout', { method: 'POST', auth: false, body: { refresh_token: t.refresh_token } }); } finally { localStorage.removeItem(tokenKey); } }
export function clearSession() { localStorage.removeItem(tokenKey); }
export const query = (v: Record<string, string | number | undefined>) => '?' + new URLSearchParams(Object.entries(v).filter(([, x]) => x !== undefined && x !== '').map(([k, x]) => [k, String(x)])).toString();
