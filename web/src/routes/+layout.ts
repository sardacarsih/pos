import { browser, dev, version } from '$app/environment';
import { base } from '$app/paths';

export const ssr = false;
export const prerender = false;

if (browser && !dev && 'serviceWorker' in navigator) {
  // The CDN caches JavaScript, so each deployment needs its own worker URL.
  void navigator.serviceWorker.register(`${base}/service-worker.js?v=${encodeURIComponent(version)}`, {
    updateViaCache: 'none'
  }).catch((error) => console.warn('Service worker POS Web belum terdaftar', error));
}
