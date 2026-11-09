<script lang="ts">
 import { onMount, onDestroy } from 'svelte';
 import { api } from './api';
 export let path: string; export let alt = ''; export let fallback = '';
 let url = '';
 onMount(async () => { try { const blob = await api<Blob>(path, {raw:true}); url = URL.createObjectURL(blob); } catch {} });
 onDestroy(() => { if (url) URL.revokeObjectURL(url); });
</script>
{#if url}<img class="loaded-photo" src={url} {alt}/>{:else}{fallback}{/if}
<style>.loaded-photo{width:100%;height:100%;object-fit:cover;border-radius:inherit}</style>
