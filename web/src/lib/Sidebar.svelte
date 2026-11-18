<script lang="ts">
  import { createEventDispatcher } from 'svelte';
  import Asterisk from '@lucide/svelte/icons/asterisk';
  import PanelLeftClose from '@lucide/svelte/icons/panel-left-close';
  import PanelLeftOpen from '@lucide/svelte/icons/panel-left-open';
  import Sun from '@lucide/svelte/icons/sun';
  import Moon from '@lucide/svelte/icons/moon';
  import LogOut from '@lucide/svelte/icons/log-out';
  import { IKON } from './icons';
  import { ariaBadge, jumlahBadge, labelBadge, type Badge, type Menu } from './nav';

  export let menu: Menu;
  export let view: string;
  export let badge: Badge = {};
  export let dark = false;
  /** true = rail ikon (diciutkan pengguna atau otomatis di layar sedang). */
  export let rail = false;
  /** Tombol ciutkan hanya tampil bila rail tidak dipaksa oleh lebar layar. */
  export let bisaDiciutkan = true;

  const dispatch = createEventDispatcher<{ pilih: string; tema: void; keluar: void; ciut: void }>();
</script>

<aside class="sb" class:rail>
  <div class="sb-brand">
    <span class="brand-mark"><Asterisk size={22} aria-hidden="true" /></span>
    <span class="sb-brand-text">KOPKAR <small>KUSUMA LESTARI</small></span>
  </div>

  <nav class="sb-nav" aria-label="Menu utama">
    {#each menu.kelompok as k}
      <div class="sb-group">
        {#if k.judul}<p class="sb-label">{k.judul}</p>{/if}
        <ul>
          {#each k.item as i}
            {@const n = jumlahBadge(i, badge)}
            <li>
              <button
                class="sb-item"
                class:on={view === i.id}
                aria-current={view === i.id ? 'page' : undefined}
                aria-label={ariaBadge(i, n)}
                title={rail ? i.label : undefined}
                on:click={() => dispatch('pilih', i.id)}
              >
                <span class="sb-ico">
                  <svelte:component this={IKON[i.ikon]} size={20} aria-hidden="true" />
                  {#if n > 0}<b class="sb-dot" aria-hidden="true">{labelBadge(n)}</b>{/if}
                </span>
                <span class="sb-text">{i.label}</span>
                {#if n > 0}<span class="sb-badge" aria-hidden="true">{labelBadge(n)}</span>{/if}
              </button>
            </li>
          {/each}
        </ul>
      </div>
    {/each}
  </nav>

  <div class="sb-foot">
    <button class="sb-item" title={rail ? (dark ? 'Mode terang' : 'Mode gelap') : undefined} on:click={() => dispatch('tema')}>
      <span class="sb-ico">{#if dark}<Sun size={20} aria-hidden="true" />{:else}<Moon size={20} aria-hidden="true" />{/if}</span>
      <span class="sb-text">{dark ? 'Mode terang' : 'Mode gelap'}</span>
    </button>
    <button class="sb-item" title={rail ? 'Keluar' : undefined} on:click={() => dispatch('keluar')}>
      <span class="sb-ico"><LogOut size={20} aria-hidden="true" /></span>
      <span class="sb-text">Keluar</span>
    </button>
    {#if bisaDiciutkan}
      <button class="sb-item sb-toggle" aria-expanded={!rail} aria-label={rail ? 'Lebarkan menu' : 'Ciutkan menu'} title={rail ? 'Lebarkan menu' : undefined} on:click={() => dispatch('ciut')}>
        <span class="sb-ico">{#if rail}<PanelLeftOpen size={20} aria-hidden="true" />{:else}<PanelLeftClose size={20} aria-hidden="true" />{/if}</span>
        <span class="sb-text">Ciutkan menu</span>
      </button>
    {/if}
    <div class="sb-note">KOPERASI KARYAWAN<br /><strong>Kusuma Lestari</strong></div>
  </div>
</aside>
