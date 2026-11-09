import { beforeEach, expect, it, vi } from 'vitest';
import { api } from './api';
const storage = new Map<string,string>();
beforeEach(() => { storage.clear(); storage.set('kopkar_web_tokens', JSON.stringify({access_token:'old',refresh_token:'refresh'})); vi.stubGlobal('localStorage',{getItem:(k:string)=>storage.get(k)??null,setItem:(k:string,v:string)=>storage.set(k,v),removeItem:(k:string)=>storage.delete(k)}); vi.stubGlobal('navigator',{}); vi.stubGlobal('crypto',{randomUUID:()=> 'device'}); });
it('rotates the refresh token once for simultaneous unauthorized requests', async () => {
 let refreshes = 0;
 vi.stubGlobal('fetch',vi.fn(async (url: string, init: RequestInit) => { if (url.endsWith('/auth/refresh')) { refreshes++; await new Promise(r=>setTimeout(r,10)); return Response.json({access_token:'new',refresh_token:'new-refresh'}); } return (init.headers as Headers).get('Authorization') === 'Bearer old' ? new Response('',{status:401}) : Response.json({ok:true}); }));
 expect(await Promise.all([api('/me'),api('/profil')])).toEqual([{ok:true},{ok:true}]); expect(refreshes).toBe(1); expect(JSON.parse(storage.get('kopkar_web_tokens')!).refresh_token).toBe('new-refresh');
});
