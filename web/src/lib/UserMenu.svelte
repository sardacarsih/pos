<script lang="ts">
  import { createEventDispatcher, tick } from 'svelte';
  import ChevronDown from '@lucide/svelte/icons/chevron-down';
  import UserRound from '@lucide/svelte/icons/user-round';
  import Sun from '@lucide/svelte/icons/sun';
  import Moon from '@lucide/svelte/icons/moon';
  import LogOut from '@lucide/svelte/icons/log-out';
  import { indeksMenu, inisial } from './topbar';

  export let nama: string;
  export let peran: string;
  export let dark = false;

  const dispatch = createEventDispatcher<{ profil: void; tema: void; keluar: void }>();
  const idMenu = 'menu-pengguna';
  let terbuka = false, akar: HTMLElement, tombol: HTMLButtonElement, daftar: HTMLElement;

  const butir = () => [...daftar.querySelectorAll<HTMLElement>('[role="menuitem"]')];

  async function buka(dariAkhir = false) {
    terbuka = true;
    await tick();
    const b = butir();
    (dariAkhir ? b[b.length - 1] : b[0])?.focus();
  }
  function tutup(kembaliKeTombol = true) { terbuka = false; if (kembaliKeTombol) tombol?.focus(); }

  // Tombol pemicu: Enter/Space/↓ membuka (fokus item pertama), ↑ membuka (fokus item terakhir).
  function tekanTombol(e: KeyboardEvent) {
    if (e.key === 'ArrowDown' || e.key === 'Enter' || e.key === ' ') { e.preventDefault(); void buka(); }
    else if (e.key === 'ArrowUp') { e.preventDefault(); void buka(true); }
  }
  // Di dalam menu: panah/Home/End berpindah (berputar), Esc menutup dan mengembalikan fokus, Tab menutup.
  function tekanMenu(e: KeyboardEvent) {
    if (e.key === 'Escape') { e.preventDefault(); tutup(); return; }
    if (e.key === 'Tab') { tutup(false); return; }
    if (['ArrowDown', 'ArrowUp', 'Home', 'End'].includes(e.key)) {
      e.preventDefault();
      const b = butir(), i = b.indexOf(document.activeElement as HTMLElement);
      b[indeksMenu(i < 0 ? 0 : i, b.length, e.key)]?.focus();
    }
  }
  function pilih(aksi: 'profil' | 'tema' | 'keluar') {
    // Tema: tetap di halaman, jadi fokus kembali ke tombol; Profil/Keluar memindahkan halaman.
    tutup(aksi === 'tema');
    if (aksi === 'profil') dispatch('profil'); else if (aksi === 'tema') dispatch('tema'); else dispatch('keluar');
  }
  function klikLuar(e: MouseEvent) { if (terbuka && akar && !akar.contains(e.target as Node)) tutup(false); }
</script>

<svelte:window on:click={klikLuar} />

<div class="um" bind:this={akar}>
  <button
    class="um-btn"
    bind:this={tombol}
    aria-haspopup="menu"
    aria-expanded={terbuka}
    aria-controls={terbuka ? idMenu : undefined}
    aria-label={`Menu pengguna: ${nama}, ${peran}`}
    on:click={() => (terbuka ? tutup() : void buka())}
    on:keydown={tekanTombol}
  >
    <span class="um-avatar" aria-hidden="true">{inisial(nama)}</span>
    <span class="um-nama" aria-hidden="true"><strong>{nama}</strong><small>{peran}</small></span>
    <ChevronDown size={16} class="um-chev" aria-hidden="true" />
  </button>

  {#if terbuka}
    <div class="um-menu" id={idMenu} role="menu" aria-label="Menu pengguna" tabindex="-1" bind:this={daftar} on:keydown={tekanMenu}>
      <div class="um-head" role="presentation"><strong>{nama}</strong><small>{peran}</small></div>
      <button class="um-item" role="menuitem" tabindex="-1" on:click={() => pilih('profil')}><UserRound size={18} aria-hidden="true" /><span>Profil</span></button>
      <button class="um-item" role="menuitem" tabindex="-1" on:click={() => pilih('tema')}>
        {#if dark}<Sun size={18} aria-hidden="true" />{:else}<Moon size={18} aria-hidden="true" />{/if}
        <span>{dark ? 'Mode terang' : 'Mode gelap'}</span>
      </button>
      <button class="um-item um-keluar" role="menuitem" tabindex="-1" on:click={() => pilih('keluar')}><LogOut size={18} aria-hidden="true" /><span>Keluar</span></button>
    </div>
  {/if}
</div>
