<script lang="ts">
  // Tombol ikon 44px: label wajib (aria-label + title), varian bahaya untuk aksi merusak.
  import type { Component } from 'svelte';
  export let icon: Component<any>; // eslint-disable-line @typescript-eslint/no-explicit-any
  export let label: string;
  export let danger = false;
  export let disabled = false;
  export let hint = '';
</script>

<button type="button" class="ibtn" class:danger {disabled} aria-label={label} title={hint || label} on:click><svelte:component this={icon} size={18} aria-hidden="true" /></button>
