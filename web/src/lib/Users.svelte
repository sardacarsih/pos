<script lang="ts">
  import { onMount } from 'svelte';
  import { api, query } from './api';

  interface Account { id: number; username: string; nama: string; role: 'ADMIN' | 'KASIR' | 'ANGGOTA'; nik: string | null; aktif: boolean; terkunci: boolean }
  interface Member { nik: string; nama: string; unit_nama: string }
  interface MemberPage { items: Member[]; ada_lagi: boolean }
  let accounts: Account[] = [], tab: 'sistem' | 'anggota' = 'sistem', search = '';
  let loading = true, busy = false, error = '', notice = '', create = false, formError = '', password = '';
  let username = '', name = '', role: 'ADMIN' | 'KASIR' = 'KASIR';
  let members: Member[] = [], selected: Member | null = null, memberSearch = '', submittedSearch = '', memberPage = 1, memberMore = false, memberLoading = false, memberError = '', requestId = 0;
  $: systemAccounts = accounts.filter(account => account.role === 'ADMIN' || account.role === 'KASIR');
  $: memberAccounts = accounts.filter(account => account.role === 'ANGGOTA');
  $: filtered = (tab === 'sistem' ? systemAccounts : memberAccounts).filter(account => [account.nama, account.username, account.nik || ''].some(value => value.toLowerCase().includes(search.trim().toLowerCase())));
  $: memberUsername = selected ? selected.nik.trim().toLowerCase().replace(/[^a-z0-9._-]+/g, '.').slice(0, 50) : '';

  onMount(loadAccounts);
  async function loadAccounts() {
    loading = true; error = '';
    try { accounts = await api<Account[]>('/admin/users'); }
    catch (e) { error = (e as Error).message; }
    finally { loading = false; }
  }
  function openCreate() {
    create = true; formError = ''; name = ''; username = ''; role = 'KASIR'; selected = null;
    members = []; memberSearch = ''; submittedSearch = ''; memberPage = 1; memberMore = false; memberError = '';
    if (tab === 'anggota') void findMembers();
  }
  function closeCreate() { if (busy) return; create = false; requestId++; }
  async function findMembers(next = false) {
    if (memberLoading && next) return;
    const current = ++requestId;
    if (!next) { submittedSearch = memberSearch.trim(); members = []; memberPage = 1; memberMore = false; }
    const page = next ? memberPage + 1 : 1;
    memberLoading = true; memberError = '';
    try {
      const result = await api<MemberPage>('/pelanggan' + query({ q: submittedSearch, page, size: 30, belum_akun: 'true' }));
      if (current !== requestId) return;
      members = next ? [...members, ...result.items] : result.items; memberPage = page; memberMore = result.ada_lagi;
    } catch (e) { if (current === requestId) memberError = (e as Error).message; }
    finally { if (current === requestId) memberLoading = false; }
  }
  async function addUser() {
    if (busy || (tab === 'anggota' && !selected)) return;
    busy = true; formError = '';
    try {
      const body = tab === 'anggota' && selected
        ? { username: memberUsername, nama: selected.nama, role: 'ANGGOTA', nik: selected.nik }
        : { username: username.trim().toLowerCase(), nama: name.trim(), role };
      const result = await api<{ password_sementara: string }>('/admin/users', { method: 'POST', body });
      password = result.password_sementara; create = false; requestId++;
      await loadAccounts();
    } catch (e) { formError = (e as Error).message; }
    finally { busy = false; }
  }
  async function resetPassword(account: Account) {
    if (busy) return; busy = true; error = ''; notice = '';
    try { const result = await api<{password_sementara: string}>('/admin/users/' + account.id + '/reset-password', { method: 'POST' }); password = result.password_sementara; }
    catch (e) { error = (e as Error).message; }
    finally { busy = false; }
  }
  async function toggleActive(account: Account) {
    if (busy) return; busy = true; error = ''; notice = '';
    try { await api('/admin/users/' + account.id + '/aktif', { method: 'PUT', body: { aktif: !account.aktif } }); notice = 'Status pengguna berhasil diperbarui.'; await loadAccounts(); }
    catch (e) { error = (e as Error).message; }
    finally { busy = false; }
  }
</script>

<section class="panel">
  <div class="section-heading"><div><p class="eyebrow">ADMINISTRASI</p><h2>Pengguna</h2></div><button class="primary" on:click={openCreate} disabled={busy}>＋ {tab === 'sistem' ? 'User sistem' : 'Akun anggota'}</button></div>
  <div class="user-tabs" aria-label="Jenis pengguna">
    <button class:active={tab === 'sistem'} aria-pressed={tab === 'sistem'} on:click={() => { tab = 'sistem'; search = ''; }}>Sistem <span>{systemAccounts.length}</span></button>
    <button class:active={tab === 'anggota'} aria-pressed={tab === 'anggota'} on:click={() => { tab = 'anggota'; search = ''; }}>Anggota <span>{memberAccounts.length}</span></button>
  </div>
  <label class="user-search">Cari pengguna<input bind:value={search} placeholder="Cari nama, username, atau NIK"/></label>
  {#if error}<div class="error" role="alert">{error} <button on:click={loadAccounts}>Coba lagi</button></div>{/if}
  {#if notice}<div class="notice">{notice}</div>{/if}
  {#if loading}<p class="muted" aria-live="polite">Memuat pengguna…</p>
  {:else if !error}
    {#each filtered as account (account.id)}
      <div class="admin-row"><div><strong>{account.nama}</strong><small>{account.username} · {account.role} {account.nik || ''} · {account.aktif ? 'Aktif' : 'Nonaktif'} {account.terkunci ? '· Terkunci' : ''}</small></div><div class="actions"><button on:click={() => resetPassword(account)} disabled={busy}>Reset password</button><button on:click={() => toggleActive(account)} disabled={busy}>{account.aktif ? 'Nonaktifkan' : 'Aktifkan'}</button></div></div>
    {:else}<p class="empty">{search.trim() ? 'Tidak ada pengguna yang cocok.' : tab === 'sistem' ? 'Belum ada user sistem.' : 'Belum ada akun anggota.'}</p>{/each}
  {/if}
</section>

{#if create}
<div class="overlay"><div class="modal" role="dialog" aria-modal="true" aria-labelledby="create-user-title">
  <div class="row"><h2 id="create-user-title">{tab === 'sistem' ? 'User sistem baru' : 'Akun anggota baru'}</h2><button class="icon" aria-label="Tutup" on:click={closeCreate} disabled={busy}>×</button></div>
  {#if formError}<div class="error" role="alert">{formError}</div>{/if}
  {#if tab === 'sistem'}
    <form on:submit|preventDefault={addUser}><label>Peran<select bind:value={role}><option value="KASIR">KASIR</option><option value="ADMIN">ADMIN</option></select></label><label>Nama<input bind:value={name} required maxlength="100"/></label><label>Username<input bind:value={username} required minlength="3" maxlength="50"/></label><button class="primary" disabled={busy}>{busy ? 'Menyimpan…' : 'Buat pengguna'}</button></form>
  {:else if !selected}
    <p class="muted">Pilih anggota aktif yang belum memiliki akun.</p>
    <form class="search" on:submit|preventDefault={() => findMembers()}><input aria-label="Cari anggota" bind:value={memberSearch} placeholder="Cari nama atau NIK"/><button disabled={memberLoading}>Cari</button></form>
    {#if memberError}<div class="error" role="alert">{memberError} <button on:click={() => findMembers()}>Coba lagi</button></div>{/if}
    {#if memberLoading}<p class="muted" aria-live="polite">Memuat anggota…</p>{/if}
    <div class="scroll-list">{#each members as member (member.nik)}<button class="list-button" on:click={() => {selected = member; formError = '';}}><strong>{member.nama}</strong><small>{member.nik} · {member.unit_nama || 'Unit kerja tidak tersedia'}</small></button>{:else}{#if !memberLoading && !memberError}<p class="empty">Tidak ada anggota yang cocok atau belum memiliki akun.</p>{/if}{/each}
      {#if memberMore}<button class="secondary wide" on:click={() => findMembers(true)} disabled={memberLoading}>Muat lebih banyak</button>{/if}</div>
  {:else}
    <form on:submit|preventDefault={addUser}><label>Nama anggota<input value={selected.nama} readonly/></label><label>NIK<input value={selected.nik} readonly/></label><label>Username<input value={memberUsername} readonly/></label><p class="muted">Username dibentuk dari NIK anggota dan tidak dapat diubah.</p><div class="actions"><button type="button" class="secondary" on:click={() => selected = null} disabled={busy}>Pilih anggota lain</button><button class="primary" disabled={busy}>{busy ? 'Menyimpan…' : 'Buat akun anggota'}</button></div></form>
  {/if}
</div></div>
{/if}
{#if password}<div class="overlay"><div class="modal" role="dialog" aria-modal="true" aria-labelledby="password-title"><h2 id="password-title">Password sementara</h2><p>Catat password ini. Password hanya ditampilkan sekali dan wajib diganti saat login pertama.</p><div class="secret">{password}</div><button class="primary" on:click={() => password = ''}>Sudah dicatat</button></div></div>{/if}

<style>
  .user-tabs { display: flex; gap: 8px; border-bottom: 1px solid #e2ebe2; }
  .user-tabs button { padding: 12px 16px; color: #728479; font-weight: 700; border-bottom: 3px solid transparent; }
  .user-tabs button.active { color: #147d37; border-color: #147d37; }
  .user-tabs span { background: #e9f4e8; border-radius: 99px; padding: 3px 8px; margin-left: 4px; font-size: .75rem; }
  .user-search { max-width: 420px; }
  .error button { text-decoration: underline; font-weight: 700; }
  @media(max-width: 650px) { .section-heading { align-items: flex-start; flex-wrap: wrap; } .user-tabs button { flex: 1; } }
</style>
