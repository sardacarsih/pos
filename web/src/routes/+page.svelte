<script lang="ts">
  import Asterisk from '@lucide/svelte/icons/asterisk';
  import ArrowRight from '@lucide/svelte/icons/arrow-right';
  import { onMount, tick } from 'svelte';
  import { beforeNavigate } from '$app/navigation';
  import AdminOpname from '$lib/AdminOpname.svelte';
  import { api, login, logout, restore, clearSession } from '$lib/api';
  import type { User } from '$lib/types';
  import Shop from '$lib/Shop.svelte'; import General from '$lib/General.svelte'; import Admin from '$lib/Admin.svelte'; import Users from '$lib/Users.svelte'; import TagihanRekap from '$lib/TagihanRekap.svelte';
  import Sidebar from '$lib/Sidebar.svelte'; import BottomNav from '$lib/BottomNav.svelte';
  import { menuUntuk, labelView, type Badge } from '$lib/nav';
  import Topbar from '$lib/Topbar.svelte';
  import { jejak as hitungJejak, statusPeriode, judulDokumen, NAMA_PERAN, type RespPeriode, type StatusPeriode } from '$lib/topbar';
  import '../app.css';
  let user: User | null = null, username = '', password = '', newPassword = '', busy = false, ingat = false, loading = true, error = '', view = 'dashboard', online = true, dark = false;
  // Navigasi: menu per peran, preferensi sidebar diciutkan (tersimpan), rail otomatis di layar sedang, lencana admin.
  let ciut = false, layarSedang = false, badge: Badge = {}, mainEl: HTMLElement | undefined, lastView = 'dashboard', periode: StatusPeriode | null = null, tergulir = false;
  $: menu = menuUntuk(user?.role ?? 'KASIR');
  $: rail = ciut || layarSedang;
  // Pindah halaman: fokus ke konten (pembaca layar/keyboard), bukan tertinggal di menu.
  $: if (view !== lastView) { lastView = view; void tick().then(() => mainEl?.focus({ preventScroll: true })); }
  // Lencana disegarkan tiap pindah halaman (cache server 60 dtk) dan tiap 5 menit; gagal = tanpa lencana.
  async function muatBadge(_: string) { if (user?.role !== 'ADMIN' || user.ganti_password) return; try { const r = await api<{ stok_habis: number | null; harga_rendah: number | null }>('/admin/menu-badge'); badge = { stok_habis: r.stok_habis ?? undefined, harga_rendah: r.harga_rendah ?? undefined }; } catch { /* lencana opsional */ } }
  // Status periode untuk chip topbar (semua peran); gagal = chip tidak tampil.
  async function muatPeriode(_: string) { if (!user || user.ganti_password) return; try { periode = statusPeriode(await api<RespPeriode>('/periode/status')); } catch { periode = null; } }
  $: if (user) { void muatBadge(view); void muatPeriode(view); }
  function toggleRail() { ciut = !ciut; try { localStorage.setItem('kopkar_sidebar', ciut ? 'rail' : 'lebar'); } catch { /* penyimpanan tidak tersedia */ } }
  onMount(() => { online = navigator.onLine; dark = localStorage.getItem('kopkar_theme') === 'dark'; try { ciut = localStorage.getItem('kopkar_sidebar') === 'rail'; } catch { /* abaikan */ } const mq = window.matchMedia('(max-width: 950px)'); const ubahMq = () => layarSedang = mq.matches; ubahMq(); mq.addEventListener('change', ubahMq); const jam = setInterval(() => { void muatBadge(view); void muatPeriode(view); }, 300000); window.addEventListener('online', connectivity); window.addEventListener('offline', connectivity); void (async () => { try { user = await restore(); } catch(e) { error = (e as Error).message; } finally { loading = false; } })(); return () => { clearInterval(jam); mq.removeEventListener('change', ubahMq); window.removeEventListener('online', connectivity); window.removeEventListener('offline', connectivity); }; });
  function connectivity() { online = navigator.onLine; }
  async function signIn() { busy = true; error = ''; try { user = await login(username, password, ingat); password = ''; view = 'dashboard'; } catch(e) { error = (e as Error).message; } finally { busy = false; } }
  async function changePassword() { busy = true; error = ''; try { await api('/auth/ganti-password',{method:'POST',body:{password_lama:password,password_baru:newPassword}}); clearSession(); user = null; password = newPassword = ''; error = 'Password berhasil diganti. Silakan login kembali.'; } catch(e) { error = (e as Error).message; } finally { busy = false; } }
  let opname: AdminOpname | undefined;
  function pilihView(tujuan: string) {
    if (tujuan === view || !opname || opname.bolehKeluar()) view = tujuan;
  }
  beforeNavigate(({ willUnload, cancel }) => {
    if (!willUnload && opname && !opname.bolehKeluar()) cancel();
  });
  async function signOut() { if (opname && !opname.bolehKeluar()) return; try { await logout(); } catch {} finally { user = null; view = 'dashboard'; badge = {}; periode = null; } }
  function toggleTheme() { dark = !dark; localStorage.setItem('kopkar_theme', dark ? 'dark' : 'light'); }
</script>
<svelte:window on:scroll={() => tergulir = window.scrollY > 4}/>
<svelte:head><title>{user ? judulDokumen(labelView(menu, view)) : 'Kopkar POS'}</title><meta name="description" content="Aplikasi koperasi Kopkar Kusuma Lestari"/></svelte:head>
<div class:dark class="app">
{#if loading}<div class="center muted">Menyiapkan aplikasi…</div>
{:else if !user}<main class="login-page"><div class="login-story"><div class="brand inverse"><span class="brand-mark"><Asterisk size={22} aria-hidden="true"/></span><span>KOPKAR <small>KUSUMA LESTARI</small></span></div><div class="story-text"><p class="eyebrow light">BERSAMA TUMBUH LEBIH BAIK</p><h1>Lebih dekat<br/>dengan <em>koperasi.</em></h1><p>Akses layanan, transaksi, dan kebutuhan sehari-hari dalam satu tempat.</p></div><div class="story-footer">KOPKAR KUSUMA LESTARI <span>●</span> LAYANAN DIGITAL</div></div><div class="login-side"><div class="login-card"><div class="login-icon"><Asterisk size={26} aria-hidden="true"/></div><p class="eyebrow">SELAMAT DATANG</p><h2>Masuk ke akun</h2><p class="muted">Gunakan akun Kopkar Anda untuk melanjutkan.</p>{#if error}<div class="error" role="alert">{error}</div>{/if}<form on:submit|preventDefault={signIn}><label>Username<input bind:value={username} autocomplete="username" required placeholder="Masukkan username"/></label><label>Password<input type="password" bind:value={password} autocomplete="current-password" required placeholder="Masukkan password"/></label><label class="check"><input type="checkbox" bind:checked={ingat}/> Ingat saya di perangkat ini</label><button class="primary wide" disabled={busy}>{busy ? 'Memproses…' : 'Masuk'} <span><ArrowRight size={18} aria-hidden="true"/></span></button></form><p class="login-help">Mengalami kendala? Hubungi administrator koperasi.</p></div></div></main>
{:else if user.ganti_password}<div class="center"><section class="panel password-card"><p class="eyebrow">KEAMANAN AKUN</p><h2>Buat password baru</h2><p>Anda perlu mengganti password sementara sebelum melanjutkan.</p>{#if error}<div class="error">{error}</div>{/if}<form on:submit|preventDefault={changePassword}><label>Password saat ini<input type="password" bind:value={password} required/></label><label>Password baru<input type="password" bind:value={newPassword} required/></label><button class="primary wide" disabled={busy}>Ganti password</button></form><button on:click={signOut}>Keluar</button></section></div>
{:else}<div class="shell" class:rail={rail}><a class="skip" href="#konten">Lewati ke konten</a><Sidebar {menu} {view} {badge} {dark} {rail} bisaDiciutkan={!layarSedang} on:pilih={(e) => pilihView(e.detail)} on:tema={toggleTheme} on:keluar={signOut} on:ciut={toggleRail}/><div class="main-area"><Topbar judul={labelView(menu, view)} jejak={hitungJejak(menu, view, user.role)} nama={user.nama} peran={NAMA_PERAN[user.role]} {dark} {online} {periode} {tergulir} on:profil={() => pilihView('profil')} on:tema={toggleTheme} on:keluar={signOut}/><main class="content" id="konten" tabindex="-1" aria-labelledby="judul-halaman" bind:this={mainEl}>{#if view === 'belanja'}<Shop {user}/>{:else if user.role === 'ADMIN' && view === 'users'}<Users currentUserId={user.id}/>{:else if user.role === 'ADMIN' && view === 'tagihan-rekap'}<TagihanRekap/>{:else if user.role === 'ADMIN' && view === 'opname'}<AdminOpname bind:this={opname}/>{:else if user.role === 'ADMIN' && ['produk','stok','harga','kategori','urut','rekap'].includes(view)}<Admin {view}/>{:else}<General {user} {view} on:go={(e) => view = e.detail}/>{/if}</main></div><BottomNav {menu} {view} {badge} {dark} on:pilih={(e) => pilihView(e.detail)} on:tema={toggleTheme} on:keluar={signOut}/></div>{/if}
</div>
