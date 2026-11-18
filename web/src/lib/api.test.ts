import { beforeEach, expect, it, vi } from 'vitest';
import { api, login, clearSession } from './api';
const storage = new Map<string,string>();
beforeEach(() => { storage.clear(); storage.set('kopkar_web_tokens', JSON.stringify({access_token:'old',refresh_token:'refresh'})); vi.stubGlobal('localStorage',{getItem:(k:string)=>storage.get(k)??null,setItem:(k:string,v:string)=>storage.set(k,v),removeItem:(k:string)=>storage.delete(k)}); const sess = new Map<string,string>(); vi.stubGlobal('sessionStorage',{getItem:(k:string)=>sess.get(k)??null,setItem:(k:string,v:string)=>sess.set(k,v),removeItem:(k:string)=>sess.delete(k)}); (globalThis as any).__sess = sess; vi.stubGlobal('navigator',{}); vi.stubGlobal('crypto',{randomUUID:()=> 'device'}); });
it('rotates the refresh token once for simultaneous unauthorized requests', async () => {
 let refreshes = 0;
 vi.stubGlobal('fetch',vi.fn(async (url: string, init: RequestInit) => { if (url.endsWith('/auth/refresh')) { refreshes++; await new Promise(r=>setTimeout(r,10)); return Response.json({access_token:'new',refresh_token:'new-refresh'}); } return (init.headers as Headers).get('Authorization') === 'Bearer old' ? new Response('',{status:401}) : Response.json({ok:true}); }));
 expect(await Promise.all([api('/me'),api('/profil')])).toEqual([{ok:true},{ok:true}]); expect(refreshes).toBe(1); expect(JSON.parse(storage.get('kopkar_web_tokens')!).refresh_token).toBe('new-refresh');
});

it.each([[false,'sess'],[true,'local']])('login ingat=%s menyimpan token di %s saja', async (ingat, where) => {
 storage.clear(); const sess = (globalThis as any).__sess as Map<string,string>; sess.clear();
 vi.stubGlobal('fetch',vi.fn(async (url: string) => url.endsWith('/auth/login') ? Response.json({access_token:'a',refresh_token:'r'}) : Response.json({}) ));
 await login('u','p',ingat);
 expect(storage.has('kopkar_web_tokens')).toBe(where === 'local'); expect(sess.has('kopkar_web_tokens')).toBe(where === 'sess');
 clearSession(); expect(storage.has('kopkar_web_tokens') || sess.has('kopkar_web_tokens')).toBe(false);
});
