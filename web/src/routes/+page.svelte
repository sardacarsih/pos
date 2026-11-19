<script lang="ts">
  import { base } from '$app/paths';
  import ArrowRight from '@lucide/svelte/icons/arrow-right';
  import UserRound from '@lucide/svelte/icons/user-round';
  import Lock from '@lucide/svelte/icons/lock';
  import Eye from '@lucide/svelte/icons/eye';
  import EyeOff from '@lucide/svelte/icons/eye-off';
  import { onMount, tick } from 'svelte';
  import { beforeNavigate } from '$app/navigation';
  import Persediaan from '$lib/Persediaan.svelte';
  import type { TabSedia } from '$lib/persediaan';
  import { api, ApiError, login, logout, restore, clearSession, sesiPembelianOffline } from '$lib/api';
  import Pembelian from '$lib/Pembelian.svelte';
  import Kas from '$lib/Kas.svelte';
  import Pinjaman from '$lib/Pinjaman.svelte';
  import PinjamanSaya from '$lib/PinjamanSaya.svelte';
  import { sinkronPembelian } from '$lib/pembelianOffline';
  import type { User } from '$lib/types';
  import Shop from '$lib/Shop.svelte'; import General from '$lib/General.svelte'; import Admin from '$lib/Admin.svelte'; import Users from '$lib/Users.svelte'; import TagihanRekap from '$lib/TagihanRekap.svelte';
  import Sidebar from '$lib/Sidebar.svelte'; import BottomNav from '$lib/BottomNav.svelte';
  import { menuUntuk, labelView, MENU_OFFLINE, type Badge } from '$lib/nav';
  import Topbar from '$lib/Topbar.svelte';
  import { jejak as hitungJejak, statusPeriode, judulDokumen, NAMA_PERAN, type RespPeriode, type StatusPeriode } from '$lib/topbar';
  import '../app.css';
  let user: User | null = null, username = '', password = '', newPassword = '', busy = false, ingat = false, lihat = false, loading = true, error = '', view = 'dashboard', online = true, dark = false;
  let modeLokal = false, memeriksaSesi = false;
  const bisaBeli = (u: User | null) => u && !u.ganti_password && ['ADMIN', 'PEMBELIAN'].includes(u.role);
  const halamanAwal = (u: User | null) => u?.role === 'PEMBELIAN' ? 'pembelian' : 'dashboard';
  const tanpaServer = (e: unknown) => e instanceof ApiError && (e.status === 0 || e.status >= 500);
  // Navigasi: menu per peran, preferensi sidebar diciutkan (tersimpan), rail otomatis di layar sedang, lencana admin.
  let ciut = false, layarSedang = false, badge: Badge = {}, mainEl: HTMLElement | undefined, lastView = 'dashboard', periode: StatusPeriode | null = null, tergulir = false;
  $: menu = modeLokal ? MENU_OFFLINE : menuUntuk(user?.role ?? 'KASIR');
  $: terhubung = online && !modeLokal;
  $: rail = ciut || layarSedang;
  // Pindah halaman: fokus ke konten (pembaca layar/keyboard), bukan tertinggal di menu.
  $: if (view !== lastView) { lastView = view; void tick().then(() => mainEl?.focus({ preventScroll: true })); }
  // Lencana disegarkan tiap pindah halaman (cache server 60 dtk) dan tiap 5 menit; gagal = tanpa lencana.
  async function muatBadge(_: string) { if (user?.role !== 'ADMIN' || user.ganti_password) return; try { const r = await api<{ stok_habis: number | null; harga_rendah: number | null }>('/admin/menu-badge'); badge = { stok_habis: r.stok_habis ?? undefined, harga_rendah: r.harga_rendah ?? undefined }; } catch { /* lencana opsional */ } }
  // Status periode untuk chip topbar (semua peran); gagal = chip tidak tampil.
  async function muatPeriode(_: string) { if (!user || user.ganti_password) return; try { periode = statusPeriode(await api<RespPeriode>('/periode/status')); } catch { periode = null; } }
  $: if (user) { void muatBadge(view); void muatPeriode(view); }
  function toggleRail() { ciut = !ciut; try { localStorage.setItem('kopkar_sidebar', ciut ? 'rail' : 'lebar'); } catch { /* penyimpanan tidak tersedia */ } }
  onMount(() => {
    online = navigator.onLine;
    dark = localStorage.getItem('kopkar_theme') === 'dark';
    try { ciut = localStorage.getItem('kopkar_sidebar') === 'rail'; } catch { /* abaikan */ }
    const mq = window.matchMedia('(max-width: 950px)');
    const ubahMq = () => layarSedang = mq.matches;
    ubahMq();
    mq.addEventListener('change', ubahMq);
    const jam = setInterval(() => { void muatBadge(view); void muatPeriode(view); }, 300000);
    const sinkron = setInterval(connectivity, 30_000);
    window.addEventListener('online', connectivity);
    window.addEventListener('offline', connectivity);
    window.addEventListener('focus', connectivity);
    void mulai();
    return () => {
      clearInterval(jam);
      clearInterval(sinkron);
      mq.removeEventListener('change', ubahMq);
      window.removeEventListener('online', connectivity);
      window.removeEventListener('offline', connectivity);
      window.removeEventListener('focus', connectivity);
    };
  });

  async function mulai() {
    try {
      if (!online && sesiPembelianOffline()) { bukaLokal(); return; }
      user = await restore();
      view = halamanAwal(user);
      if (bisaBeli(user) && user) void sinkronPembelian(user.id).catch(() => false);
    } catch (e) {
      if (tanpaServer(e) && sesiPembelianOffline()) bukaLokal();
      else error = (e as Error).message;
    } finally { loading = false; }
  }

  function bukaLokal() {
    user = sesiPembelianOffline();
    modeLokal = !!user;
    view = 'pembelian';
    if (!user) error = 'Sesi offline sudah berakhir. Sambungkan ke server untuk masuk kembali.';
  }

  function connectivity() {
    online = navigator.onLine;
    if (loading || !bisaBeli(user) || !user || memeriksaSesi) return;
    if (!online) {
      if (view === 'pembelian' || modeLokal) bukaLokal();
      return;
    }
    void verifikasiDanSinkron(user.id);
  }

  async function verifikasiDanSinkron(id: number) {
    memeriksaSesi = true;
    try {
      const profil = await restore();
      if (user?.id !== id) return;
      if (!profil || profil.id !== id) {
        user = null;
        modeLokal = false;
        error = 'Masuk kembali untuk melanjutkan.';
        return;
      }
      const aksesBerubah = profil.role !== user.role || profil.ganti_password !== user.ganti_password;
      if (aksesBerubah) { user = profil; view = halamanAwal(profil); }
      modeLokal = false;
      if (bisaBeli(profil)) await sinkronPembelian(id).catch(() => false);
    } catch (e) {
      if (user?.id === id && tanpaServer(e) && (view === 'pembelian' || modeLokal)) bukaLokal();
    } finally { memeriksaSesi = false; }
  }

  async function signIn() {
    busy = true;
    error = '';
    try {
      user = await login(username, password, ingat);
      password = '';
      modeLokal = false;
      view = halamanAwal(user);
      if (bisaBeli(user)) void sinkronPembelian(user.id).catch(() => false);
    } catch(e) { error = (e as Error).message; }
    finally { busy = false; }
  }
  async function changePassword() { busy = true; error = ''; try { await api('/auth/ganti-password',{method:'POST',body:{password_lama:password,password_baru:newPassword}}); clearSession(); user = null; password = newPassword = ''; error = 'Password berhasil diganti. Silakan login kembali.'; } catch(e) { error = (e as Error).message; } finally { busy = false; } }
  let persediaan: Persediaan | undefined;
  let tabAwal: TabSedia = 'kartu';
  function pilihView(tujuan: string) {
    if (modeLokal && tujuan !== 'pembelian') return;
    if (tujuan === 'stok' || tujuan === 'opname') { tabAwal = tujuan === 'stok' ? 'habis' : 'opname'; tujuan = 'persediaan'; }
    if (tujuan !== view && persediaan && !persediaan.bolehKeluar()) return;
    if (tujuan === 'pembelian' && !online && bisaBeli(user)) { bukaLokal(); return; }
    view = tujuan;
  }
  beforeNavigate(({ willUnload, cancel }) => {
    if (!willUnload && persediaan && !persediaan.bolehKeluar()) cancel();
  });
  async function signOut() {
    if (persediaan && !persediaan.bolehKeluar()) return;
    user = null; modeLokal = false; view = 'dashboard'; badge = {}; periode = null;
    try { await logout(); } catch { /* Sesi lokal sudah dikunci. */ }
  }
  function toggleTheme() { dark = !dark; localStorage.setItem('kopkar_theme', dark ? 'dark' : 'light'); }
  const logoKopkar = base + '/kopkar-icon.png';
</script>
<svelte:window on:scroll={() => tergulir = window.scrollY > 4}/>
<svelte:head><title>{user ? judulDokumen(labelView(menu, view)) : 'Kopkar POS'}</title><meta name="description" content="Aplikasi koperasi Kopkar Kusuma Lestari"/></svelte:head>
<div class:dark class="app">
{#if loading}<div class="center muted">Menyiapkan aplikasi…</div>
{:else if !user}<main class="login-page"><div class="login-story"><div class="brand inverse"><img src={logoKopkar} width="42" height="42" alt=""/><span>KOPKAR <small>KUSUMA LESTARI</small></span></div><div class="story-text"><p class="eyebrow light">BERSAMA TUMBUH LEBIH BAIK</p><h1>Lebih dekat<br/>dengan <em>koperasi.</em></h1><p>Akses layanan, transaksi, dan kebutuhan sehari-hari dalam satu tempat.</p></div><div class="story-footer">KOPKAR KUSUMA LESTARI <span>●</span> LAYANAN DIGITAL</div></div><div class="login-side"><div class="login-card"><img class="login-icon" src={logoKopkar} width="42" height="42" alt="Kopkar Kusuma Lestari"/><p class="eyebrow">SELAMAT DATANG</p><h2>Masuk ke akun</h2><p class="muted">Gunakan akun Kopkar Anda untuk melanjutkan.</p>{#if error}<div class="error" role="alert">{error}</div>{/if}<form on:submit|preventDefault={signIn}><label>Username<span class="field"><UserRound size={18} aria-hidden="true"/><input bind:value={username} autocomplete="username" required placeholder="Masukkan username"/></span></label><label>Password<span class="field"><Lock size={18} aria-hidden="true"/><input type={lihat ? 'text' : 'password'} bind:value={password} autocomplete="current-password" required placeholder="Masukkan password"/><button type="button" class="eye" aria-label={lihat ? 'Sembunyikan password' : 'Tampilkan password'} on:click={() => lihat = !lihat}>{#if lihat}<EyeOff size={18} aria-hidden="true"/>{:else}<Eye size={18} aria-hidden="true"/>{/if}</button></span></label><label class="check"><input type="checkbox" bind:checked={ingat}/> Ingat saya di perangkat ini</label><button class="primary wide" disabled={busy}>{busy ? 'Memproses…' : 'Masuk'} <span><ArrowRight size={18} aria-hidden="true"/></span></button></form><p class="login-help">Mengalami kendala? Hubungi administrator koperasi.</p></div></div></main>
{:else if user.ganti_password}<div class="center"><section class="panel password-card"><p class="eyebrow">KEAMANAN AKUN</p><h2>Buat password baru</h2><p>Anda perlu mengganti password sementara sebelum melanjutkan.</p>{#if error}<div class="error">{error}</div>{/if}<form on:submit|preventDefault={changePassword}><label>Password saat ini<input type="password" bind:value={password} required/></label><label>Password baru<input type="password" bind:value={newPassword} required/></label><button class="primary wide" disabled={busy}>Ganti password</button></form><button on:click={signOut}>Keluar</button></section></div>
{:else}<div class="shell" class:rail={rail}><a class="skip" href="#konten">Lewati ke konten</a><Sidebar {menu} {view} {badge} {dark} {rail} bisaDiciutkan={!layarSedang} on:pilih={(e) => pilihView(e.detail)} on:tema={toggleTheme} on:keluar={signOut} on:ciut={toggleRail}/>
  <div class="main-area">
    <Topbar judul={labelView(menu, view)} jejak={hitungJejak(menu, view, user.role)}
      nama={user.nama} peran={NAMA_PERAN[user.role]} {dark} online={terhubung} {periode} {tergulir}
      pembelianOffline={view === 'pembelian'} tampilProfil={!modeLokal && user.role !== 'PEMBELIAN'}
      on:profil={() => pilihView('profil')} on:tema={toggleTheme} on:keluar={signOut}/>
    <main class="content" id="konten" tabindex="-1" aria-labelledby="judul-halaman" bind:this={mainEl}>
      {#if view === 'pembelian' && bisaBeli(user)}
        {#key user.id}<Pembelian {user} online={terhubung}/>{/key}
      {:else if view === 'kas' && user.role === 'ADMIN'}
        {#key user.id}<Kas {user}/>{/key}
      {:else if view === 'pinjaman' && user.role === 'ADMIN'}
        {#key user.id}<Pinjaman {user}/>{/key}
      {:else if view === 'pinjaman' && user.role === 'ANGGOTA'}
        {#key user.id}<PinjamanSaya {user}/>{/key}
      {:else if view === 'belanja'}<Shop {user}/>
      {:else if user.role === 'ADMIN' && view === 'users'}<Users currentUserId={user.id}/>
      {:else if user.role === 'ADMIN' && view === 'tagihan-rekap'}<TagihanRekap/>
      {:else if user.role === 'ADMIN' && view === 'persediaan'}<Persediaan bind:this={persediaan} {tabAwal}/>
      {:else if user.role === 'ADMIN' && ['produk','harga','rekap'].includes(view)}<Admin {view}/>
      {:else}<General {user} {view} on:go={(e) => pilihView(e.detail)}/>
      {/if}
    </main>
  </div>
  <BottomNav {menu} {view} {badge} {dark} on:pilih={(e) => pilihView(e.detail)} on:tema={toggleTheme} on:keluar={signOut}/>
</div>{/if}
</div>
