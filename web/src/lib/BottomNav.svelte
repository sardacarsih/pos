<script lang="ts">
  import { createEventDispatcher, tick } from 'svelte';
  import Ellipsis from '@lucide/svelte/icons/ellipsis';
  import X from '@lucide/svelte/icons/x';
  import Sun from '@lucide/svelte/icons/sun';
  import Moon from '@lucide/svelte/icons/moon';
  import LogOut from '@lucide/svelte/icons/log-out';
  import { IKON } from './icons';
  import { ariaBadge, itemUtama, jumlahBadge, labelBadge, type Badge, type Menu } from './nav';

  export let menu: Menu;
  export let view: string;
  export let badge: Badge = {};
  export let dark = false;

  const dispatch = createEventDispatcher<{ pilih: string; tema: void; keluar: void }>();
  let terbuka = false, lembar: HTMLElement, pemicu: HTMLButtonElement;

  $: utama = itemUtama(menu);
  $: lainnyaAktif = !menu.utama.includes(view);
  // Titik lencana di "Lainnya" bila ada item di luar bottom-nav yang punya lencana.
  $: lainnyaBadge = menu.kelompok.flatMap(k => k.item).some(i => !menu.utama.includes(i.id) && jumlahBadge(i, badge) > 0);

  async function buka() { terbuka = true; await tick(); lembar?.querySelector<HTMLElement>('button')?.focus(); }
  function tutup() { terbuka = false; pemicu?.focus(); }
  function pilih(id: string) { terbuka = false; dispatch('pilih', id); }
  // Fokus terkunci di dalam lembar; Esc menutup dan mengembalikan fokus ke tombol "Lainnya".
  function tombol(e: KeyboardEvent) {
    if (e.key === 'Escape') { e.stopPropagation(); tutup(); return; }
    if (e.key !== 'Tab') return;
    const f = [...lembar.querySelectorAll<HTMLElement>('button:not([disabled])')];
    if (!f.length) return;
    const a = document.activeElement;
    if (e.shiftKey && a === f[0]) { e.preventDefault(); f[f.length - 1].focus(); }
    else if (!e.shiftKey && a === f[f.length - 1]) { e.preventDefault(); f[0].focus(); }
  }
</script>

<nav class="bn" aria-label="Menu utama">
  {#each utama as i}
    {@const n = jumlahBadge(i, badge)}
    <button class="bn-item" class:on={view === i.id} aria-current={view === i.id ? 'page' : undefined} aria-label={ariaBadge(i, n)} on:click={() => dispatch('pilih', i.id)}>
      <span class="bn-ico"><svelte:component this={IKON[i.ikon]} size={22} aria-hidden="true" />{#if n > 0}<b class="bn-dot" aria-hidden="true">{labelBadge(n)}</b>{/if}</span>
      <small>{i.pendek ?? i.label}</small>
    </button>
  {/each}
  <button class="bn-item" class:on={lainnyaAktif} bind:this={pemicu} aria-haspopup="dialog" aria-expanded={terbuka} on:click={buka}>
    <span class="bn-ico"><Ellipsis size={22} aria-hidden="true" />{#if lainnyaBadge}<b class="bn-dot titik" aria-hidden="true"></b>{/if}</span>
    <small>Lainnya</small>
  </button>
</nav>

{#if terbuka}
  <div class="bn-scrim" role="presentation" on:click|self={tutup}>
    <div class="bn-sheet" role="dialog" aria-modal="true" aria-label="Semua menu" tabindex="-1" bind:this={lembar} on:keydown={tombol}>
      <div class="bn-sheet-head">
        <h2>Menu</h2>
        <button class="bn-close" aria-label="Tutup menu" on:click={tutup}><X size={22} aria-hidden="true" /></button>
      </div>
      {#each menu.kelompok as k}
        <section class="bn-group">
          {#if k.judul}<h3>{k.judul}</h3>{/if}
          <div class="bn-grid">
            {#each k.item as i}
              {@const n = jumlahBadge(i, badge)}
              <button class="bn-tile" class:on={view === i.id} aria-current={view === i.id ? 'page' : undefined} aria-label={ariaBadge(i, n)} on:click={() => pilih(i.id)}>
                <span class="bn-ico"><svelte:component this={IKON[i.ikon]} size={22} aria-hidden="true" /></span>
                <span>{i.label}</span>
                {#if n > 0}<span class="bn-pill" aria-hidden="true">{labelBadge(n)}</span>{/if}
              </button>
            {/each}
          </div>
        </section>
      {/each}
      <section class="bn-group">
        <h3>Pengaturan</h3>
        <div class="bn-grid">
          <button class="bn-tile" on:click={() => { terbuka = false; dispatch('tema'); }}>
            <span class="bn-ico">{#if dark}<Sun size={22} aria-hidden="true" />{:else}<Moon size={22} aria-hidden="true" />{/if}</span>
            <span>{dark ? 'Mode terang' : 'Mode gelap'}</span>
          </button>
          <button class="bn-tile" on:click={() => { terbuka = false; dispatch('keluar'); }}>
            <span class="bn-ico"><LogOut size={22} aria-hidden="true" /></span>
            <span>Keluar</span>
          </button>
        </div>
      </section>
    </div>
  </div>
{/if}
