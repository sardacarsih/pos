<script lang="ts">
  import { tick } from 'svelte';
  import NotebookText from '@lucide/svelte/icons/notebook-text';
  import PackageX from '@lucide/svelte/icons/package-x';
  import ClipboardCheck from '@lucide/svelte/icons/clipboard-check';
  import SlidersHorizontal from '@lucide/svelte/icons/sliders-horizontal';
  import Coins from '@lucide/svelte/icons/coins';
  import List from '@lucide/svelte/icons/list';
  import { TAB_SEDIA, type TabSedia } from './persediaan';
  import AdminOpname from './AdminOpname.svelte';
  import PersediaanKartu from './PersediaanKartu.svelte';
  import PersediaanHabis from './PersediaanHabis.svelte';
  import PersediaanSesuaikan from './PersediaanSesuaikan.svelte';
  import PersediaanValuasi from './PersediaanValuasi.svelte';
  import PersediaanValuasiItem from './PersediaanValuasiItem.svelte';
  import './persediaan.css';

  export let tabAwal: TabSedia = 'kartu';
  let tab: TabSedia = tabAwal;
  let opnameRef: AdminOpname | undefined;
  const IKON_TAB: Record<TabSedia, any> = {
    kartu: NotebookText, habis: PackageX, opname: ClipboardCheck, sesuai: SlidersHorizontal, valuasi: Coins, 'valuasi-item': List
  };

  let tabAwalTerakhir = tabAwal;
  $: if (tabAwal !== tabAwalTerakhir) { tabAwalTerakhir = tabAwal; void pilihTab(tabAwal); }

  export function bolehKeluar(): boolean {
    return opnameRef?.bolehKeluar() ?? true;
  }

  async function pilihTab(target: TabSedia) {
    if (target === tab) return;
    if (tab === 'opname' && opnameRef && !opnameRef.bolehKeluar()) return;
    tab = target;
    await tick();
    document.getElementById('sedia-tab-' + target)?.focus();
  }

  function tombolTab(e: KeyboardEvent, i: number) {
    const arah = e.key === 'ArrowLeft' ? -1 : e.key === 'ArrowRight' ? 1 : 0;
    if (!arah && e.key !== 'Home' && e.key !== 'End') return;
    e.preventDefault();
    const index = e.key === 'Home' ? 0 : e.key === 'End' ? TAB_SEDIA.length - 1 : (i + arah + TAB_SEDIA.length) % TAB_SEDIA.length;
    void pilihTab(TAB_SEDIA[index].id);
  }
</script>

<div class="sedia">
  <div class="sedia-tab" role="tablist" aria-label="Persediaan">
    {#each TAB_SEDIA as t, i (t.id)}
      <button type="button" id={'sedia-tab-' + t.id} role="tab" aria-selected={tab === t.id} aria-controls={'sedia-panel-' + t.id}
        tabindex={tab === t.id ? 0 : -1} on:keydown={(e) => tombolTab(e, i)} on:click={() => pilihTab(t.id)}>
        <svelte:component this={IKON_TAB[t.id]} size={16} aria-hidden="true"/>{t.label}
      </button>
    {/each}
  </div>
  {#each TAB_SEDIA as t (t.id)}
    <div id={'sedia-panel-' + t.id} role="tabpanel" aria-labelledby={'sedia-tab-' + t.id} hidden={tab !== t.id}>
      {#if tab === t.id}
        {#if tab === 'kartu'}
          <PersediaanKartu />
        {:else if tab === 'habis'}
          <PersediaanHabis />
        {:else if tab === 'opname'}
          <AdminOpname bind:this={opnameRef} />
        {:else if tab === 'sesuai'}
          <PersediaanSesuaikan />
        {:else if tab === 'valuasi'}
          <PersediaanValuasi />
        {:else if tab === 'valuasi-item'}
          <PersediaanValuasiItem />
        {/if}
      {/if}
    </div>
  {/each}
</div>
