/// <reference lib="webworker" />
import { base, build, version } from '$service-worker';

const worker = self as unknown as ServiceWorkerGlobalScope;
const prefix = 'kopkar-shell-';
const name = prefix + version;
const shell = base + '/';
const assets = new Set(build);

worker.addEventListener('install', event => {
  event.waitUntil(caches.open(name).then(cache => cache.addAll([
    ...build.map(path => new Request(path, { cache: 'reload' })),
    new Request(shell, { cache: 'reload' })
  ])));
});

worker.addEventListener('activate', event => {
  event.waitUntil(caches.keys().then(keys => Promise.all(
    keys.filter(key => key.startsWith(prefix) && key !== name).map(key => caches.delete(key))
  )).then(() => worker.clients.claim()));
});

worker.addEventListener('fetch', event => {
  const request = event.request;
  const url = new URL(request.url);
  if (request.method !== 'GET' || url.origin !== worker.location.origin) return;
  if (request.mode === 'navigate' && (url.pathname === shell || url.pathname === shell + 'index.html')) {
    event.respondWith((async () => {
      try {
        const response = await fetch(request);
        if (response.status < 500) return response;
        return (await (await caches.open(name)).match(shell)) ?? response;
      } catch {
        return (await (await caches.open(name)).match(shell)) ?? Response.error();
      }
    })());
  } else if (assets.has(url.pathname)) {
    event.respondWith(caches.open(name).then(async cache => (await cache.match(request)) ?? fetch(request)));
  }
});
