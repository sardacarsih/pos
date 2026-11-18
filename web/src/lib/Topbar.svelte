<script lang="ts">
  import { createEventDispatcher } from 'svelte';
  import WifiOff from '@lucide/svelte/icons/wifi-off';
  import UserMenu from './UserMenu.svelte';
  import type { StatusPeriode } from './topbar';

  /** Label halaman aktif; dirender sebagai satu-satunya <h1> halaman. */
  export let judul: string;
  /** Breadcrumb di atas judul (mis. ["Admin", "Katalog & stok"]). */
  export let jejak: string[] = [];
  export let nama: string;
  export let peran: string;
  export let dark = false;
  export let online = true;
  export let periode: StatusPeriode | null = null;
  /** true bila halaman sudah tergulir: tampilkan bayangan. */
  export let tergulir = false;

  const dispatch = createEventDispatcher<{ profil: void; tema: void; keluar: void }>();
</script>

<header class="tb" class:tergulir>
  <div class="tb-title">
    {#if jejak.length}
      <nav class="tb-crumb" aria-label="Jejak halaman"><ol>{#each jejak as j}<li>{j}</li>{/each}</ol></nav>
    {/if}
    <h1 id="judul-halaman">{judul}</h1>
  </div>

  <div class="tb-end">
    <!-- Wilayah status selalu ada di DOM agar perubahan teksnya diumumkan pembaca layar. -->
    <span class="sr-only" role="status">{online ? '' : 'Koneksi terputus. Transaksi memerlukan koneksi internet.'}</span>
    {#if !online}
      <span class="tb-chip warn" aria-hidden="true" title="Koneksi terputus. Transaksi memerlukan koneksi internet."><WifiOff size={14} /> Offline<span class="tb-lebar"> · transaksi memerlukan internet</span></span>
    {/if}
    {#if periode}
      <span class="tb-chip tb-periode {periode.tingkat}" title={periode.judul}>{periode.label}</span>
    {/if}
    <UserMenu {nama} {peran} {dark} on:profil={() => dispatch('profil')} on:tema={() => dispatch('tema')} on:keluar={() => dispatch('keluar')} />
  </div>
</header>
