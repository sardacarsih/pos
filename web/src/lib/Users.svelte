<script lang="ts">
  import AlertCircle from '@lucide/svelte/icons/alert-circle';
  import ArrowLeft from '@lucide/svelte/icons/arrow-left';
  import ChevronRight from '@lucide/svelte/icons/chevron-right';
  import CircleCheck from '@lucide/svelte/icons/circle-check';
  import CircleMinus from '@lucide/svelte/icons/circle-minus';
  import KeyRound from '@lucide/svelte/icons/key-round';
  import ListFilter from '@lucide/svelte/icons/list-filter';
  import LoaderCircle from '@lucide/svelte/icons/loader-circle';
  import Lock from '@lucide/svelte/icons/lock';
  import Power from '@lucide/svelte/icons/power';
  import RefreshCw from '@lucide/svelte/icons/refresh-cw';
  import Search from '@lucide/svelte/icons/search';
  import Shield from '@lucide/svelte/icons/shield';
  import UserPlus from '@lucide/svelte/icons/user-plus';
  import Users from '@lucide/svelte/icons/users';
  import X from '@lucide/svelte/icons/x';
  import { onMount, tick } from 'svelte';
  import { api, query } from './api';
  import { inisial } from './topbar';

  export let currentUserId: number;

  type Tab = 'sistem' | 'anggota';
  type Status = '' | 'aktif' | 'nonaktif' | 'terkunci';
  interface Account { id: number; username: string; nama: string; role: 'ADMIN' | 'KASIR' | 'ANGGOTA'; nik: string | null; aktif: boolean; terkunci: boolean }
  interface Member { nik: string; nama: string; unit_nama: string }
  interface MemberPage { items: Member[]; ada_lagi: boolean }
  interface TemporaryPassword { password_sementara: string }
  type AccountAction = { kind: 'confirm'; action: 'reset' | 'status'; account: Account };
  type Dialog = { kind: 'create' } | AccountAction | { kind: 'password'; username: string; value: string };

  const roles = { ADMIN: 'Admin', KASIR: 'Kasir', ANGGOTA: 'Anggota' };
  let accounts: Account[] = [];
  let tab: Tab = 'sistem', status: Status = '', search = '';
  let loading = true, busy = false, listError = '', notice = '';
  let dialog: Dialog | null = null, dialogError = '';
  let dialogEl: HTMLDialogElement, searchInput: HTMLInputElement;
  let previouslyFocused: HTMLElement | null = null;
  let username = '', name = '', role: 'ADMIN' | 'KASIR' = 'KASIR';
  let members: Member[] = [], selected: Member | null = null, memberSearch = '', submittedSearch = '';
  let memberPage = 1, memberMore = false, memberLoading = false, memberError = '', retryNext = false, requestId = 0;

  $: systemAccounts = accounts.filter(account => account.role === 'ADMIN' || account.role === 'KASIR');
  $: memberAccounts = accounts.filter(account => account.role === 'ANGGOTA');
  $: tabAccounts = tab === 'sistem' ? systemAccounts : memberAccounts;
  $: term = search.trim().toLowerCase();
  $: filtered = tabAccounts.filter(account => {
    const matchesSearch = [account.nama, account.username, account.nik ?? ''].some(value => value.toLowerCase().includes(term));
    return matchesSearch && (
      status === '' ||
      (status === 'aktif' && account.aktif) ||
      (status === 'nonaktif' && !account.aktif) ||
      (status === 'terkunci' && account.terkunci)
    );
  });
  $: memberUsername = selected ? selected.nik.trim().toLowerCase().replace(/[^a-z0-9._-]+/g, '.').slice(0, 50) : '';
  $: confirmationLabel = dialog?.kind === 'confirm'
    ? dialog.action === 'reset' ? 'Reset password' : dialog.account.aktif ? 'Nonaktifkan' : 'Aktifkan'
    : '';
  $: title = dialog?.kind === 'password' ? 'Password sementara'
    : dialog?.kind === 'confirm' ? confirmationLabel + '?'
    : tab === 'sistem' ? 'Tambah pengguna sistem' : 'Tambah akun anggota';

  onMount(() => { void loadAccounts(); });

  async function loadAccounts() {
    listError = '';
    loading = true;
    try {
      accounts = await api<Account[]>('/admin/users');
    } catch (e) {
      listError = (e as Error).message;
    } finally {
      loading = false;
    }
  }

  function clearFilters() {
    search = '';
    status = '';
  }

  function selectTab(next: Tab) {
    if (tab === next) return;
    tab = next;
    clearFilters();
  }

  async function focusDialog() {
    await tick();
    dialogEl.querySelector<HTMLElement>('[data-initial-focus]')?.focus();
  }

  async function present(next: Dialog, opener?: EventTarget | null) {
    if (!dialogEl.open) previouslyFocused = opener as HTMLElement | null;
    dialog = next;
    dialogError = '';
    await tick();
    if (!dialogEl.open) dialogEl.showModal();
    await focusDialog();
  }

  function closeDialog() {
    requestId++;
    memberLoading = false;
    dialogEl.close();
    dialog = null;
    const target = previouslyFocused;
    previouslyFocused = null;
    void tick().then(() => {
      if (target?.isConnected && !target.matches(':disabled')) target.focus();
      else searchInput.focus();
    });
  }

  function cancelDialog(event?: Event) {
    event?.preventDefault();
    if (!busy && dialog?.kind !== 'password') closeDialog();
  }

  function openCreate(event: MouseEvent) {
    name = ''; username = ''; role = 'KASIR'; selected = null;
    members = []; memberSearch = ''; submittedSearch = ''; memberPage = 1;
    memberMore = false; memberError = ''; retryNext = false;
    void present({ kind: 'create' }, event.currentTarget);
    if (tab === 'anggota') void findMembers();
  }

  async function findMembers(next = false) {
    if (memberLoading) return;
    const current = ++requestId;
    if (!next) {
      submittedSearch = memberSearch.trim();
      members = [];
      memberPage = 1;
      memberMore = false;
    }
    const page = next ? memberPage + 1 : 1;
    memberLoading = true;
    memberError = '';
    retryNext = next;
    try {
      const result = await api<MemberPage>('/pelanggan' + query({ q: submittedSearch, page, size: 30, belum_akun: 'true' }));
      if (current !== requestId) return;
      members = next ? [...members, ...result.items] : result.items;
      memberPage = page;
      memberMore = result.ada_lagi;
    } catch (e) {
      if (current === requestId) memberError = (e as Error).message;
    } finally {
      if (current === requestId) memberLoading = false;
    }
  }

  async function addUser() {
    if (busy || (tab === 'anggota' && !selected)) return;
    busy = true;
    dialogError = '';
    notice = '';
    try {
      const body = tab === 'anggota' && selected
        ? { username: memberUsername, nama: selected.nama, role: 'ANGGOTA', nik: selected.nik }
        : { username: username.trim().toLowerCase(), nama: name.trim(), role };
      const result = await api<TemporaryPassword>('/admin/users', { method: 'POST', body });
      await present({ kind: 'password', username: body.username, value: result.password_sementara });
      await loadAccounts();
    } catch (e) {
      dialogError = (e as Error).message;
    } finally {
      busy = false;
    }
  }

  function openAction(event: MouseEvent, account: Account, action: AccountAction['action']) {
    if (busy || (action === 'status' && account.aktif && account.id === currentUserId)) return;
    void present({ kind: 'confirm', action, account }, event.currentTarget);
  }

  async function runAction() {
    if (busy || dialog?.kind !== 'confirm') return;
    const { account, action } = dialog;
    busy = true;
    dialogError = '';
    notice = '';
    try {
      if (action === 'reset') {
        const result = await api<TemporaryPassword>('/admin/users/' + account.id + '/reset-password', { method: 'POST' });
        accounts = accounts.map(item => item.id === account.id ? { ...item, terkunci: false } : item);
        await present({ kind: 'password', username: account.username, value: result.password_sementara });
        await loadAccounts();
      } else {
        const aktif = !account.aktif;
        await api('/admin/users/' + account.id + '/aktif', { method: 'PUT', body: { aktif } });
        accounts = accounts.map(item => item.id === account.id ? { ...item, aktif } : item);
        notice = aktif ? 'Akun ' + account.username + ' diaktifkan.' : 'Akun ' + account.username + ' dinonaktifkan.';
        closeDialog();
      }
    } catch (e) {
      dialogError = (e as Error).message;
    } finally {
      busy = false;
    }
  }
</script>

<section class="users-page" aria-label="Pengelolaan pengguna">
  <div class="directory">
    <div class="directory-heading">
      <div>
        <h2>Daftar akun</h2>
        <p>Kelola akses admin, kasir, dan anggota.</p>
      </div>
      <button class="button primary-action" on:click={openCreate} disabled={busy || loading}>
        <UserPlus size={18} aria-hidden="true"/>
        {tab === 'sistem' ? 'Tambah pengguna' : 'Tambah akun anggota'}
      </button>
    </div>

    <div class="account-tabs" role="group" aria-label="Jenis pengguna">
      <button type="button" aria-pressed={tab === 'sistem'} on:click={() => selectTab('sistem')}>
        <Shield size={17} aria-hidden="true"/> Sistem
        {#if !loading && (!listError || accounts.length)}<span class="count">{systemAccounts.length}</span>{/if}
      </button>
      <button type="button" aria-pressed={tab === 'anggota'} on:click={() => selectTab('anggota')}>
        <Users size={17} aria-hidden="true"/> Anggota
        {#if !loading && (!listError || accounts.length)}<span class="count">{memberAccounts.length}</span>{/if}
      </button>
    </div>

    <div class="account-toolbar">
      <div class="field-icon search-field">
        <Search size={17} aria-hidden="true"/>
        <input type="search" aria-label="Cari berdasarkan nama, username, atau NIK" placeholder="Nama, username, atau NIK" bind:value={search} bind:this={searchInput}/>
      </div>
      <div class="field-icon">
        <ListFilter size={17} aria-hidden="true"/>
        <select aria-label="Filter status akun" bind:value={status}>
          <option value="">Semua status</option>
          <option value="aktif">Aktif</option>
          <option value="nonaktif">Nonaktif</option>
          <option value="terkunci">Terkunci</option>
        </select>
      </div>
    </div>

    <div class="feedback">
      {#if listError}
        <div class="alert" role="alert">
          <AlertCircle size={18} aria-hidden="true"/>
          <span>{listError}</span>
          <button class="button secondary-action" on:click={loadAccounts} disabled={busy}>
            <RefreshCw size={16} aria-hidden="true"/> Coba lagi
          </button>
        </div>
      {/if}
      <p class="sr-only" role="status">{notice}</p>
      {#if notice}<p class="success" aria-hidden="true"><CircleCheck size={17}/> {notice}</p>{/if}
    </div>

    {#if loading && !accounts.length}
      <div class="directory-state" role="status"><LoaderCircle size={24} aria-hidden="true"/> Memuat pengguna…</div>
    {:else if filtered.length}
      <!-- svelte-ignore a11y_no_redundant_roles (Responsive display changes can remove implicit table semantics.) -->
      <table class="account-table" role="table" aria-label={tab === 'sistem' ? 'Akun sistem' : 'Akun anggota'}>
        <!-- svelte-ignore a11y_no_redundant_roles -->
        <thead role="rowgroup">
          <tr>
            <th scope="col" role="columnheader">Pengguna</th>
            <th scope="col" role="columnheader">Peran</th>
            <th scope="col" role="columnheader">Status</th>
            <th scope="col" role="columnheader">Tindakan</th>
          </tr>
        </thead>
        <!-- svelte-ignore a11y_no_redundant_roles -->
        <tbody role="rowgroup">
          {#each filtered as account (account.id)}
            <!-- svelte-ignore a11y_no_redundant_roles -->
            <tr role="row">
              <th scope="row" role="rowheader">
                <div class="identity">
                  <span class="initials" aria-hidden="true">{inisial(account.nama)}</span>
                  <div>
                    <strong>{account.nama}</strong>
                    <span class="account-meta">{account.username}</span>
                    {#if account.nik}<span class="account-meta">NIK {account.nik}</span>{/if}
                  </div>
                </div>
              </th>
              <td class="account-role" role="cell">{roles[account.role]}</td>
              <td class="account-status" role="cell">
                <div class="statuses">
                  <span class="status" class:is-active={account.aktif}>
                    {#if account.aktif}<CircleCheck size={14} aria-hidden="true"/>{:else}<CircleMinus size={14} aria-hidden="true"/>{/if}
                    {account.aktif ? 'Aktif' : 'Nonaktif'}
                  </span>
                  {#if account.terkunci}<span class="status is-locked"><Lock size={14} aria-hidden="true"/> Terkunci</span>{/if}
                </div>
              </td>
              <td class="account-actions" role="cell">
                <div class="row-actions">
                  <button class="button secondary-action" aria-label={'Reset password ' + account.username} on:click={(event) => openAction(event, account, 'reset')} disabled={busy || loading}>
                    <KeyRound size={16} aria-hidden="true"/> Reset password
                  </button>
                  <button class="button secondary-action" aria-label={(account.aktif ? 'Nonaktifkan ' : 'Aktifkan ') + account.username}
                    aria-describedby={account.aktif && account.id === currentUserId ? 'own-account-note' : undefined}
                    on:click={(event) => openAction(event, account, 'status')} disabled={busy || loading || (account.aktif && account.id === currentUserId)}>
                    <Power size={16} aria-hidden="true"/> {account.aktif ? 'Nonaktifkan' : 'Aktifkan'}
                  </button>
                </div>
                {#if account.aktif && account.id === currentUserId}<small class="own-account" id="own-account-note">Akun yang Anda gunakan</small>{/if}
              </td>
            </tr>
          {/each}
        </tbody>
      </table>
    {:else if !listError}
      <div class="directory-state">
        {#if term || status}
          <Search size={24} aria-hidden="true"/>
          <h3>Tidak ada akun yang cocok</h3>
          <button class="button secondary-action" on:click={() => { clearFilters(); searchInput.focus(); }}>Hapus filter</button>
        {:else}
          <Users size={24} aria-hidden="true"/>
          <h3>{tab === 'sistem' ? 'Belum ada akun sistem' : 'Belum ada akun anggota'}</h3>
        {/if}
      </div>
    {/if}
  </div>


  <dialog class="account-dialog" aria-labelledby="account-dialog-title" bind:this={dialogEl} on:cancel={cancelDialog}>
    {#if dialog}
      <div class="dialog-heading">
        <h2 id="account-dialog-title" tabindex="-1" data-initial-focus={dialog.kind === 'password' ? '' : undefined}>{title}</h2>
        {#if dialog.kind !== 'password'}
          <button type="button" class="button icon-action" aria-label="Tutup dialog" on:click={() => cancelDialog()} disabled={busy}>
            <X size={20} aria-hidden="true"/>
          </button>
        {/if}
      </div>
      {#if dialogError}<p class="alert" role="alert"><AlertCircle size={18} aria-hidden="true"/> {dialogError}</p>{/if}

      {#if dialog.kind === 'create'}
        {#if tab === 'sistem'}
          <form class="account-form" on:submit|preventDefault={addUser}>
            <label>Nama <input data-initial-focus bind:value={name} required maxlength="100"/></label>
            <label>Username <input bind:value={username} required minlength="3" maxlength="50" autocapitalize="none" spellcheck="false"/></label>
            <label>Peran <select bind:value={role}><option value="KASIR">Kasir</option><option value="ADMIN">Admin</option></select></label>
            <div class="dialog-actions">
              <button type="button" class="button secondary-action" on:click={() => cancelDialog()} disabled={busy}>Batal</button>
              <button class="button primary-action" disabled={busy}><UserPlus size={17} aria-hidden="true"/> {busy ? 'Menyimpan…' : 'Buat pengguna'}</button>
            </div>
          </form>
        {:else if selected}
          <form class="account-form" on:submit|preventDefault={addUser}>
            <div class="selected-member" tabindex="-1" role="group" aria-label="Anggota terpilih" data-initial-focus>
              <strong>{selected.nama}</strong>
              <span class="account-meta">NIK {selected.nik}</span>
              {#if selected.unit_nama}<span class="account-meta">{selected.unit_nama}</span>{/if}
            </div>
            <label>Username <input value={memberUsername} readonly/></label>
            <p class="help">Username dibuat dari NIK anggota.</p>
            <div class="dialog-actions">
              <button type="button" class="button secondary-action" on:click={() => { selected = null; dialogError = ''; void focusDialog(); }} disabled={busy}>
                <ArrowLeft size={17} aria-hidden="true"/> Pilih anggota lain
              </button>
              <button class="button primary-action" disabled={busy}><UserPlus size={17} aria-hidden="true"/> {busy ? 'Menyimpan…' : 'Buat akun anggota'}</button>
            </div>
          </form>
        {:else}
          <p class="help">Pilih anggota aktif yang belum memiliki akun.</p>
          <form class="member-search" on:submit|preventDefault={() => findMembers()}>
            <div class="field-icon search-field">
              <Search size={17} aria-hidden="true"/>
              <input type="search" aria-label="Cari anggota berdasarkan nama atau NIK" placeholder="Nama atau NIK" bind:value={memberSearch} data-initial-focus/>
            </div>
            <button class="button secondary-action" disabled={memberLoading}>Cari</button>
          </form>
          {#if memberError}
            <div class="alert" role="alert">
              <AlertCircle size={18} aria-hidden="true"/> <span>{memberError}</span>
              <button class="button secondary-action" on:click={() => findMembers(retryNext)}><RefreshCw size={16} aria-hidden="true"/> Coba lagi</button>
            </div>
          {/if}
          <div class="member-list" aria-label="Anggota yang dapat dipilih" role="group">
            {#each members as member (member.nik)}
              <button class="member-option" on:click={() => { selected = member; dialogError = ''; void focusDialog(); }} disabled={memberLoading}>
                <span><strong>{member.nama}</strong><small>{member.nik}{member.unit_nama ? ' · ' + member.unit_nama : ''}</small></span>
                <ChevronRight size={18} aria-hidden="true"/>
              </button>
            {:else}
              {#if !memberLoading && !memberError}<p class="help">Tidak ada anggota yang dapat dipilih.</p>{/if}
            {/each}
          </div>
          {#if memberLoading}<p class="member-loading" role="status"><LoaderCircle size={17} aria-hidden="true"/> Memuat anggota…</p>{/if}
          {#if memberMore && !memberError}<button class="button secondary-action more-members" on:click={() => findMembers(true)} disabled={memberLoading}>Muat lebih banyak</button>{/if}
        {/if}
      {:else if dialog.kind === 'confirm'}
        <div class="selected-member">
          <strong>{dialog.account.nama}</strong>
          <span class="account-meta">{dialog.account.username}</span>
        </div>
        <p class="dialog-copy">
          {#if dialog.action === 'reset'}
            Sesi login akun ini akan dicabut. Pengguna perlu masuk dengan password sementara dan menggantinya.
          {:else if dialog.account.aktif}
            Sesi login akun ini akan dicabut. Pengguna kehilangan akses sampai akun diaktifkan kembali.
          {:else}
            Pengguna akan dapat masuk kembali jika akunnya tidak terkunci.
          {/if}
        </p>
        <div class="dialog-actions">
          <button type="button" class="button secondary-action" data-initial-focus on:click={() => cancelDialog()} disabled={busy}>Batal</button>
          <button type="button" class="button primary-action" class:danger-action={dialog.action === 'status' && dialog.account.aktif} on:click={runAction} disabled={busy}>
            {#if dialog.action === 'reset'}<KeyRound size={17} aria-hidden="true"/>{:else}<Power size={17} aria-hidden="true"/>{/if}
            {busy ? 'Memproses…' : confirmationLabel}
          </button>
        </div>
      {:else}
        <p class="dialog-copy">Catat password untuk <strong>{dialog.username}</strong>. Password ini hanya ditampilkan sekali dan wajib diganti saat login berikutnya.</p>
        <div class="temporary-password"><KeyRound size={18} aria-hidden="true"/> <code>{dialog.value}</code></div>
        <div class="dialog-actions"><button class="button primary-action" on:click={closeDialog}><CircleCheck size={17} aria-hidden="true"/> Sudah dicatat</button></div>
      {/if}
    {/if}
  </dialog>
</section>

<style>
  /* Hallmark · macrostructure: directory table and responsive account cards */
  /* Hallmark · pre-emit critique: P5 H5 E4 S5 R5 V4 */
  .users-page {
    container-type: inline-size;
    --surface: #fff;
    --subtle: #f4f8f4;
    --ink: #203329;
    --muted-ink: #50665a;
    --line: #dce7df;
    --control-line: #adbdaf;
    --accent: #146e38;
    --accent-text: #fff;
    --accent-soft: #e7f3ea;
    --accent-ink: #175d34;
    --focus: #157b43;
    --warning-soft: #fff2d5;
    --warning-ink: #725014;
    --danger-soft: #fceae7;
    --danger-ink: #922e26;
    --danger-fill: #9a3029;
    --danger-text: #fff;
    --backdrop: #0b2017b3;
    --space-xs: 4px;
    --space-sm: 8px;
    --space-md: 12px;
    --space-lg: 16px;
    --space-xl: 24px;
    --space-2xl: 32px;
    color: var(--ink);
  }

  :global(.dark) .users-page {
    --surface: #172a21;
    --subtle: #12231b;
    --ink: #edf8ef;
    --muted-ink: #adc3b4;
    --line: #34493c;
    --control-line: #61796a;
    --accent: #7bdfa1;
    --accent-text: #12291b;
    --accent-soft: #203c2d;
    --accent-ink: #9ae6b5;
    --focus: #a7ebbd;
    --warning-soft: #3b301b;
    --warning-ink: #f4d48a;
    --danger-soft: #452720;
    --danger-ink: #ffc2b7;
    --danger-fill: #f2a397;
    --danger-text: #38140f;
  }

  .directory { background: var(--surface); border: 1px solid var(--line); border-radius: 16px; }
  .directory-heading, .dialog-heading {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: var(--space-lg);
  }
  .directory-heading { padding: var(--space-xl); }
  .directory-heading h2, .dialog-heading h2 { overflow-wrap: anywhere; }
  .directory-heading p { margin: var(--space-xs) 0 0; color: var(--muted-ink); font-size: .9rem; }

  .button, .account-tabs button {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    gap: var(--space-sm);
    min-height: 44px;
    padding: 0 var(--space-md);
    font-size: .82rem;
    font-weight: 700;
    line-height: 1;
    white-space: nowrap;
  }
  .button { border: 1px solid transparent; border-radius: 8px; }
  .primary-action { background: var(--accent); color: var(--accent-text); }
  .danger-action { background: var(--danger-fill); color: var(--danger-text); }
  .secondary-action { color: var(--ink); background: var(--surface); border-color: var(--control-line); }
  .button:enabled:hover { box-shadow: inset 0 0 0 1px currentColor; }
  .button:enabled:active { box-shadow: inset 0 0 0 2px currentColor; }
  .button:disabled { opacity: .62; }
  .users-page :is(button, input, select):focus-visible, .selected-member:focus-visible {
    outline: 2px solid var(--focus);
    outline-offset: 2px;
  }
  .button :global(svg), .status :global(svg), .alert :global(svg), .success :global(svg),
  .temporary-password :global(svg), .member-option :global(svg) { flex-shrink: 0; }

  .account-tabs {
    display: flex;
    gap: var(--space-md);
    padding-inline: var(--space-xl);
    border-bottom: 1px solid var(--line);
  }
  .account-tabs button { color: var(--muted-ink); border-bottom: 3px solid transparent; }
  .account-tabs button[aria-pressed='true'] { color: var(--accent-ink); border-bottom-color: var(--accent); }
  .account-tabs button:hover, .account-tabs button:active { background: var(--subtle); }
  .count {
    min-width: 24px;
    padding: var(--space-xs) var(--space-sm);
    background: var(--subtle);
    border-radius: 99px;
    font-size: .72rem;
    text-align: center;
    font-variant-numeric: tabular-nums;
  }
  .account-tabs button[aria-pressed='true'] .count { background: var(--accent-soft); }
  .account-toolbar {
    display: flex;
    align-items: center;
    gap: var(--space-md);
    padding: var(--space-lg) var(--space-xl);
  }
  .field-icon { position: relative; min-width: 0; }
  .search-field { flex: 1; max-width: 420px; }
  .field-icon > :global(svg) {
    position: absolute;
    inset-inline-start: var(--space-md);
    top: var(--space-md);
    color: var(--muted-ink);
    pointer-events: none;
  }
  .users-page :is(input, select) {
    min-width: 0;
    min-height: 44px;
    margin: 0;
    border: 1px solid var(--control-line);
    border-radius: 8px;
    background: var(--surface);
    color: var(--ink);
    font-size: .875rem;
  }
  .users-page :is(input, select):hover { border-color: var(--accent-ink); }
  .users-page input::placeholder { color: var(--muted-ink); }
  .field-icon :is(input, select) { padding-inline-start: 36px; }
  .field-icon select { padding-inline-end: var(--space-md); }
  .feedback { padding-inline: var(--space-xl); }
  .alert, .success {
    display: flex;
    align-items: center;
    flex-wrap: wrap;
    gap: var(--space-sm);
    margin: 0 0 var(--space-lg);
    padding: var(--space-sm) var(--space-md);
    border-radius: 8px;
    font-size: .875rem;
    overflow-wrap: anywhere;
  }
  .alert { background: var(--danger-soft); color: var(--danger-ink); }
  .success { background: var(--accent-soft); color: var(--accent-ink); }


  .account-table { width: 100%; border-collapse: collapse; text-align: left; font-size: .875rem; }
  .account-table th, .account-table td { padding: var(--space-md); }
  .account-table th:first-child { padding-inline-start: var(--space-xl); }
  .account-table :is(th, td):last-child { padding-inline-end: var(--space-xl); }
  .account-table thead th {
    background: var(--subtle);
    color: var(--muted-ink);
    border-block: 1px solid var(--line);
    font-size: .78rem;
  }
  .account-table tbody tr + tr { border-top: 1px solid var(--line); }
  .identity { display: flex; align-items: center; gap: var(--space-md); min-width: 0; }
  .identity > div { min-width: 0; overflow-wrap: anywhere; }
  .initials {
    display: grid;
    place-items: center;
    flex: 0 0 40px;
    block-size: 40px;
    border-radius: 10px;
    background: var(--accent-soft);
    color: var(--accent-ink);
    font-weight: 800;
  }
  .account-meta, .own-account {
    display: block;
    color: var(--muted-ink);
    font-size: .78rem;
    font-weight: 500;
    overflow-wrap: anywhere;
  }
  .account-meta { margin-top: var(--space-xs); }
  .own-account { margin-top: var(--space-sm); }
  .statuses, .row-actions { display: flex; flex-wrap: wrap; align-items: center; gap: var(--space-sm); }
  .row-actions { flex-wrap: nowrap; }
  .status {
    display: inline-flex;
    align-items: center;
    gap: var(--space-xs);
    padding: var(--space-xs) var(--space-sm);
    border-radius: 99px;
    background: var(--subtle);
    color: var(--muted-ink);
    font-size: .74rem;
    font-weight: 700;
    white-space: nowrap;
  }
  .is-active { background: var(--accent-soft); color: var(--accent-ink); }
  .is-locked { background: var(--warning-soft); color: var(--warning-ink); }
  .directory-state {
    display: grid;
    justify-items: center;
    gap: var(--space-md);
    border-top: 1px solid var(--line);
    padding: var(--space-2xl) var(--space-lg);
    color: var(--muted-ink);
    text-align: center;
    font-size: .9rem;
  }
  .directory-state h3 { margin: 0; color: var(--ink); font-size: 1rem; }

  .account-dialog {
    width: min(520px, calc(100% - 32px));
    max-height: calc(100dvh - 32px);
    padding: var(--space-xl);
    border: 1px solid var(--line);
    border-radius: 16px;
    color: var(--ink);
    background: var(--surface);
    overflow: auto;
    overscroll-behavior: contain;
  }
  .account-dialog::backdrop { background: var(--backdrop); }
  .dialog-heading { gap: var(--space-sm); margin-bottom: var(--space-lg); }
  .dialog-heading h2 { font-size: 1.25rem; }
  .icon-action { flex: 0 0 44px; padding: 0; }
  .account-form { display: grid; gap: var(--space-md); }
  .account-form label { display: grid; gap: var(--space-xs); margin: 0; color: var(--ink); font-size: .875rem; font-weight: 700; }
  .account-form input[readonly] { background: var(--subtle); }
  .help, .dialog-copy { color: var(--muted-ink); font-size: .875rem; margin: 0 0 var(--space-md); overflow-wrap: anywhere; }
  .account-form .help { margin: 0; }
  .dialog-copy strong { color: var(--ink); }
  .dialog-actions { display: flex; flex-wrap: wrap; justify-content: flex-end; gap: var(--space-sm); margin-top: var(--space-md); }
  .member-search { display: flex; align-items: center; gap: var(--space-sm); margin-bottom: var(--space-md); }
  .member-list { max-height: min(276px, 40dvh); overflow: auto; }
  .member-option {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: var(--space-md);
    width: 100%;
    min-height: 44px;
    padding: var(--space-md);
    border-bottom: 1px solid var(--line);
    text-align: left;
    overflow-wrap: anywhere;
  }
  .member-option:enabled:hover, .member-option:enabled:active { background: var(--subtle); }
  .users-page .member-option:focus-visible { outline-offset: -2px; }
  .member-option small { color: var(--muted-ink); margin-top: var(--space-xs); }
  .member-loading { display: flex; align-items: center; gap: var(--space-sm); margin: var(--space-sm) 0 0; color: var(--muted-ink); }
  .more-members { margin-top: var(--space-md); width: 100%; }
  .selected-member {
    border: 1px solid var(--line);
    border-radius: 8px;
    padding: var(--space-md);
    background: var(--subtle);
    overflow-wrap: anywhere;
  }
  .selected-member + .dialog-copy { margin-top: var(--space-md); }
  .temporary-password {
    display: flex;
    gap: var(--space-md);
    align-items: center;
    padding: var(--space-md);
    border-radius: 8px;
    background: var(--accent-soft);
    color: var(--accent-ink);
    overflow-wrap: anywhere;
  }
  .temporary-password code { font-size: 1.2rem; font-weight: 700; user-select: all; min-width: 0; }

  @container (max-width: 730px) {
    .directory-heading { padding: var(--space-lg); }
    .account-toolbar, .feedback, .account-tabs { padding-inline: var(--space-lg); }
    .account-table thead {
      position: absolute;
      inline-size: 1px;
      block-size: 1px;
      overflow: hidden;
      clip-path: inset(50%);
    }
    .account-table, .account-table tbody { display: block; }
    .account-table tbody { padding: 0 var(--space-lg) var(--space-lg); }
    .account-table tbody tr {
      display: grid;
      grid-template-columns: minmax(0, auto) minmax(0, 1fr);
      align-items: center;
      gap: var(--space-sm) var(--space-md);
      padding: var(--space-md);
      border: 1px solid var(--line);
      border-radius: 12px;
    }
    .account-table tbody tr + tr { margin-top: var(--space-sm); }
    .account-table tr :is(th, td) { padding: 0; min-width: 0; }
    .account-table tr :is(th:first-child, td:last-child) { padding: 0; grid-column: 1 / -1; }
    .account-role { color: var(--muted-ink); font-size: .8rem; font-weight: 700; }
    .account-actions { border-top: 1px solid var(--line); }
    .row-actions { flex-wrap: wrap; padding-top: var(--space-sm); }
  }

  @container (max-width: 420px) {
    .directory-heading { flex-direction: column; align-items: flex-start; }
    .account-toolbar { flex-direction: column; align-items: stretch; }
    .account-tabs { gap: var(--space-xs); }
    .account-tabs button { flex: 1; padding-inline: var(--space-sm); }
    .row-actions > button { flex: 1; }
  }

  @media (max-width: 414px) {
    .account-dialog { padding: var(--space-lg); }
    .dialog-actions > button { flex: 1; }
  }
</style>
