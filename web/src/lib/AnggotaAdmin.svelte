<script lang="ts">
  import { onMount, tick } from 'svelte';
  import Building2 from '@lucide/svelte/icons/building-2';
  import ChevronLeft from '@lucide/svelte/icons/chevron-left';
  import ChevronRight from '@lucide/svelte/icons/chevron-right';
  import History from '@lucide/svelte/icons/history';
  import ListFilter from '@lucide/svelte/icons/list-filter';
  import Pencil from '@lucide/svelte/icons/pencil';
  import Plus from '@lucide/svelte/icons/plus';
  import Power from '@lucide/svelte/icons/power';
  import RotateCw from '@lucide/svelte/icons/rotate-cw';
  import Save from '@lucide/svelte/icons/save';
  import Search from '@lucide/svelte/icons/search';
  import Trash2 from '@lucide/svelte/icons/trash-2';
  import UserMinus from '@lucide/svelte/icons/user-minus';
  import UserPlus from '@lucide/svelte/icons/user-plus';
  import Users from '@lucide/svelte/icons/users';
  import X from '@lucide/svelte/icons/x';
  import IconButton from './IconButton.svelte';
  import { ApiError, api, query } from './api';

  interface Anggota {
    id: number;
    nik: string | null;
    nama: string | null;
    unit: string | null;
    aktif: string | null;
    anggota: string | null;
    status: string | null;
    limit_hutang: number;
    dapat_gabung: number;
    tmk: string | null;
  }
  interface Profil {
    nama: string | null;
    alamat: string | null;
    kota: string | null;
    kode_pos: string | null;
    telepon: string | null;
    limit_hutang: number | undefined;
  }
  interface Detail { anggota: Anggota; profil: Profil }
  interface Halaman { data: Anggota[]; lanjut: boolean; registrasi: boolean; riwayat: boolean; audit_tmk: boolean }
  interface Unit { kode: string; nama: string | null; potong_shu: string | null }
  type ModeDetail = 'ubah' | 'aktif';

  const endpoint = '/admin/anggota';
  const rupiah = new Intl.NumberFormat('id-ID', { style: 'currency', currency: 'IDR', maximumFractionDigits: 0 });

  function labelLimit(nilai: number | undefined) {
    return nilai === undefined ? '—' : nilai === 0 ? 'Tanpa batas' : rupiah.format(nilai);
  }
  const endpointUnit = '/admin/unit-kerja';
  const tabs = [{ id: 'anggota', label: 'Anggota', icon: Users }, { id: 'unit', label: 'Unit Kerja', icon: Building2 }, { id: 'riwayat', label: 'Riwayat anggota', icon: History }] as const;
  let tab: (typeof tabs)[number]['id'] = 'anggota';
  let formUnit: { kode?: string; nama: string; potong_shu: boolean } | null = null;
  let hapusUnit: Unit | null = null;
  const kosong = { q: '', unit: '', aktif: '', anggota: '', status: '' };
  let filter = { ...kosong }, diterapkan = { ...kosong };
  let hasil: Halaman = { data: [], lanjut: false, registrasi: false, riwayat: false, audit_tmk: false }, halaman = 1;
  $: tabTerlihat = tabs.filter(t => t.id !== 'riwayat' || hasil.riwayat);
  const labelKejadian: Record<string, string> = {
    MASUK: 'Masuk', MUTASI: 'Perubahan unit atau tagihan', NONAKTIF: 'Nonaktif sementara',
    AKTIF: 'Aktif kembali', BERHENTI: 'Berhenti', GABUNG_KEMBALI: 'Gabung kembali'
  };
  const labelPerubahan = { berhenti: 'Berhentikan anggota', gabung_kembali: 'Gabung kembali' };
  type Aksi = keyof typeof labelPerubahan;
  type Riwayat = {
    id: number; nik: string | null; nama: string | null; jenis: string; admin_nama: string; alasan: string; waktu: string;
    unit_dari: string | null; unit_ke: string | null; status_dari: string | null; status_ke: string | null;
    aktif_dari: string | null; aktif_ke: string | null; anggota_dari: string | null; anggota_ke: string | null;
    tmk_dari: string | null; tmk_ke: string | null;
  };
  let mutasi: { a: Anggota; aksi: Aksi; alasan: string; tanggal: string } | null = null;
  let editMutasi: { unit: string; status: string; alasan: string } | null = null;
  $: berubahTagihan = !!(editMutasi && detail &&
    (editMutasi.unit !== (detail.anggota.unit ?? '') || editMutasi.status !== (detail.anggota.status ?? '')));
  let alasanAktif = '';
  let filterRiwayat = { dari: '', sampai: '', jenis: '', q: '' };
  let kriteriaRiwayat = { ...filterRiwayat };
  let anggotaRiwayat: Anggota | null = null;
  let riwayat: { data: Riwayat[]; lanjut: boolean } = { data: [], lanjut: false };
  let halamanRiwayat = 1;
  let galatRiwayat = '';
  let unit: Unit[] = [], detail: Detail | null = null, edit: Profil | null = null;
  let sibuk = false, galat = '', pesan = '';
  let galatAnggota = '', galatUnit = '';
  let ulangAnggota = { halaman: 1, filter: { ...kosong } };
  let filterTerbuka = false, cariUnit = '';
  let dialog: HTMLDialogElement;
  let pemicu: HTMLElement | null = null;
  let fokusKembali = false;
  let permintaanDetail: { id: number; mode: ModeDetail } | null = null;
  let konfirmasiAktif = false;
  let registrasi: { nama: string; unit: string; status: string; tmk: string; limit_hutang: number | undefined } | null = null;
  type KolomRegistrasi = keyof NonNullable<typeof registrasi>;
  let galatRegistrasi: Partial<Record<KolomRegistrasi, string>> = {};
  const kolomDariKode: Record<string, KolomRegistrasi> = {
    ANGGOTA_NAMA: 'nama', ANGGOTA_DUPLIKAT: 'nama', ANGGOTA_UNIT: 'unit',
    ANGGOTA_STATUS: 'status', ANGGOTA_TMK: 'tmk', ANGGOTA_LIMIT: 'limit_hutang'
  };
  const tanggalJakarta = new Intl.DateTimeFormat('sv-SE', { timeZone: 'Asia/Jakarta', dateStyle: 'short' });
  const tanggalTampil = new Intl.DateTimeFormat('id-ID', { timeZone: 'UTC', dateStyle: 'long' });
  function tanggalMasuk(tanggal: string | null) {
    if (!tanggal) return '—';
    const nilai = new Date(tanggal + 'T00:00:00Z');
    return Number.isNaN(nilai.valueOf()) ? tanggal : tanggalTampil.format(nilai);
  }
  let batasTanggalMasuk = '';

  $: unitTerlihat = unit.filter((u) => {
    const q = cariUnit.trim().toLocaleLowerCase('id');
    return u.kode.toLocaleLowerCase('id').includes(q) || (u.nama ?? '').toLocaleLowerCase('id').includes(q);
  });
  $: jumlahFilter = [diterapkan.unit, diterapkan.aktif, diterapkan.anggota, diterapkan.status].filter(Boolean).length;
  $: judulPanel = formUnit ? (formUnit.kode ? 'Ubah unit kerja' : 'Tambah unit kerja')
    : hapusUnit ? 'Hapus unit kerja'
    : mutasi ? labelPerubahan[mutasi.aksi]
    : registrasi ? 'Daftarkan anggota'
    : permintaanDetail?.mode === 'ubah' ? 'Ubah profil anggota'
    : 'Aktifkan anggota';

  async function tampilkanPanel(e: MouseEvent) {
    pemicu = e.currentTarget as HTMLElement;
    galat = pesan = '';
    await tick();
    dialog.showModal();
    fokusPanel();
  }

  function fokusPanel() {
    void tick().then(() => dialog.querySelector<HTMLElement>('h3')?.focus());
  }

  function tutupPanel() {
    dialog.close();
    detail = edit = formUnit = hapusUnit = registrasi = null;
    mutasi = null;
    editMutasi = null;
    alasanAktif = '';
    konfirmasiAktif = false;
    permintaanDetail = null;
    galat = '';
    fokusKembali = true;
    if (!sibuk) kembalikanFokus();
  }

  function kembalikanFokus() {
    fokusKembali = false;
    const target = pemicu;
    pemicu = null;
    void tick().then(() => {
      if (target?.isConnected && !target.matches(':disabled')) target.focus();
      else document.getElementById('anggota-tab-' + tab)?.focus();
    });
  }

  function batalPanel(e: Event) {
    e.preventDefault();
    if (!sibuk) tutupPanel();
  }

  function pilihTab(tujuan: typeof tab) {
    if (sibuk || tab === tujuan) return;
    tab = tujuan;
    galat = pesan = '';
    if (tujuan === 'riwayat') void jalankan(() => muatRiwayat(1, filterRiwayat));
  }

  function tombolTab(e: KeyboardEvent, index: number) {
    const arah: Record<string, number> = { ArrowLeft: index - 1, ArrowRight: index + 1, Home: 0, End: tabTerlihat.length - 1 };
    const berikutnya = arah[e.key];
    if (berikutnya === undefined || sibuk) return;
    e.preventDefault();
    const tujuan = tabTerlihat[(berikutnya + tabTerlihat.length) % tabTerlihat.length].id;
    pilihTab(tujuan);
    document.getElementById('anggota-tab-' + tujuan)?.focus();
  }

  async function muatUnit() {
    galatUnit = '';
    try { unit = await api<Unit[]>(endpointUnit); }
    catch (e) { galatUnit = (e as Error).message; }
  }

  async function muatRiwayat(nomor: number, kriteria = kriteriaRiwayat) {
    galatRiwayat = '';
    try {
      riwayat = await api(endpoint + '/riwayat' + query({ ...kriteria, halaman: nomor, id: anggotaRiwayat?.id }));
      halamanRiwayat = nomor;
      kriteriaRiwayat = { ...kriteria };
    } catch (e) {
      galatRiwayat = (e as Error).message;
      riwayat = { data: [], lanjut: false };
    }
  }

  function bukaRiwayat(a: Anggota) {
    anggotaRiwayat = a;
    filterRiwayat = { dari: '', sampai: '', jenis: '', q: '' };
    pilihTab('riwayat');
    void tick().then(() => document.getElementById('anggota-tab-riwayat')?.focus());
  }

  function bukaMutasi(e: MouseEvent, a: Anggota, aksi: Aksi) {
    mutasi = { a, aksi, alasan: '', tanggal: tanggalJakarta.format(new Date()) };
    void tampilkanPanel(e);
  }

  function simpanMutasi() {
    const data = mutasi;
    if (!data) return;
    const gabung = data.aksi === 'gabung_kembali';
    if (gabung && !hasil.audit_tmk) return;
    if (gabung && data.tanggal !== tanggalJakarta.format(new Date())) {
      mutasi = { ...data, tanggal: tanggalJakarta.format(new Date()) };
      galat = 'Tanggal efektif berubah. Periksa tanggal baru sebelum menyimpan.';
      return;
    }
    const { unit, status, aktif, anggota } = data.a;
    return jalankan(async () => {
      await api(endpoint + '/' + data.a.id + '/mutasi', {
        method: 'POST', body: {
          aksi: data.aksi, awal: { unit, status, aktif, anggota }, alasan: data.alasan,
          ...(gabung ? { tanggal_efektif: data.tanggal, tmk_lama: data.a.tmk } : {})
        }
      });
      tutupPanel();
      pesan = data.aksi === 'berhenti' ? 'Keanggotaan telah diakhiri.' : 'Keanggotaan diaktifkan kembali.';
      await muat(halaman);
    });
  }

  function bukaUnit(e: MouseEvent, data?: Unit) {
    formUnit = { kode: data?.kode, nama: data?.nama ?? '', potong_shu: data?.potong_shu === 'Y' };
    void tampilkanPanel(e);
  }

  function bukaHapus(e: MouseEvent, data: Unit) {
    hapusUnit = data;
    void tampilkanPanel(e);
  }

  function simpanUnit() {
    const data = formUnit;
    if (!data) return;
    return jalankan(async () => {
      await api<Unit>(endpointUnit + (data.kode ? '/' + encodeURIComponent(data.kode) : ''), {
        method: data.kode ? 'PATCH' : 'POST',
        body: { nama: data.nama, potong_shu: data.potong_shu }
      });
      tutupPanel();
      pesan = 'Unit kerja tersimpan.';
      await muatUnit();
    });
  }

  function konfirmasiHapusUnit() {
    const dipilih = hapusUnit;
    if (!dipilih) return;
    return jalankan(async () => {
      await api(endpointUnit + '/' + encodeURIComponent(dipilih.kode), { method: 'DELETE' });
      tutupPanel();
      pesan = 'Unit kerja dihapus.';
      if (filter.unit === dipilih.kode) filter = { ...filter, unit: '' };
      if (registrasi?.unit === dipilih.kode) registrasi = { ...registrasi, unit: '' };
      await muatUnit();
      if (diterapkan.unit === dipilih.kode) await muat(1, { ...diterapkan, unit: '' });
    });
  }

  function bukaRegistrasi(e: MouseEvent) {
    batasTanggalMasuk = tanggalJakarta.format(new Date());
    registrasi = { nama: '', unit: '', status: 'BULANAN', tmk: batasTanggalMasuk, limit_hutang: undefined };
    galatRegistrasi = {};
    void tampilkanPanel(e);
  }

  async function fokusGalatRegistrasi() {
    const pertama = Object.keys(galatRegistrasi)[0];
    if (pertama) {
      await tick();
      document.getElementById('registrasi-' + pertama)?.focus();
    }
  }

  function daftarBaru() {
    const data = registrasi;
    if (!data || sibuk) return;
    galat = '';
    galatRegistrasi = {};
    batasTanggalMasuk = tanggalJakarta.format(new Date());
    const nama = data.nama.trim();
    if (!nama || new TextEncoder().encode(nama).length > 50 || /\p{Cc}/u.test(data.nama)) {
      galatRegistrasi.nama = 'Nama wajib diisi, maksimal 50 byte, dan tidak boleh mengandung karakter kontrol.';
    }
    if (!unit.some(u => u.kode === data.unit)) galatRegistrasi.unit = 'Pilih unit kerja yang tersedia.';
    if (data.status !== 'BULANAN' && data.status !== 'KHT') galatRegistrasi.status = 'Pilih kategori tagihan BULANAN atau KHT.';
    if (!data.tmk || data.tmk < '0001-01-01' || data.tmk > batasTanggalMasuk) {
      galatRegistrasi.tmk = 'Tanggal masuk wajib valid dan paling lambat hari ini (WIB).';
    }
    if (data.limit_hutang === undefined || !Number.isSafeInteger(data.limit_hutang) || data.limit_hutang < 0) {
      galatRegistrasi.limit_hutang = 'Isi limit dengan rupiah bulat dari 0 hingga 9.007.199.254.740.991.';
    }
    if (Object.keys(galatRegistrasi).length) return fokusGalatRegistrasi();
    return jalankan(async () => {
      let nik: string;
      try {
        ({ nik } = await api<{ nik: string }>(endpoint, { method: 'POST', body: { ...data, nama } }));
      } catch (e) {
        const kolom = e instanceof ApiError ? kolomDariKode[e.code] : undefined;
        if (!kolom || !(e instanceof ApiError)) throw e;
        galatRegistrasi = { [kolom]: e.message };
        if (kolom === 'unit') await muatUnit();
        return;
      }
      tutupPanel();
      pesan = 'Anggota berhasil didaftarkan dengan NIK ' + nik + '.';
      await muat(halaman);
    }).then(fokusGalatRegistrasi);
  }

  function labelFlag(nilai: string | null) {
    return nilai === 'Y' ? 'Ya' : nilai === 'T' ? 'Tidak' : '—';
  }

  async function jalankan(kerja: () => Promise<void>) {
    if (sibuk) return;
    sibuk = true;
    galat = pesan = '';
    try { await kerja(); }
    catch (e) { galat = (e as Error).message; }
    finally {
      sibuk = false;
      if (fokusKembali) kembalikanFokus();
    }
  }

  async function muat(tujuan: number, kriteria = diterapkan) {
    ulangAnggota = { halaman: tujuan, filter: { ...kriteria } };
    galatAnggota = '';
    try {
      hasil = await api<Halaman>(endpoint + query({ ...kriteria, halaman: tujuan }));
      halaman = tujuan;
      diterapkan = { ...kriteria };
    } catch (e) { galatAnggota = (e as Error).message; }
  }

  onMount(() => {
    void jalankan(async () => { await Promise.all([muat(1), muatUnit()]); });
  });

  function buka(id: number, mode: ModeDetail) {
    permintaanDetail = { id, mode };
    return jalankan(async () => {
      detail = await api<Detail>(endpoint + '/' + id);
      edit = mode === 'ubah' ? { ...detail.profil } : null;
      editMutasi = edit && hasil.riwayat && detail.anggota.anggota === 'Y'
        ? { unit: detail.anggota.unit ?? '', status: detail.anggota.status ?? '', alasan: '' }
        : null;
      konfirmasiAktif = mode === 'aktif' && bolehAktifkan(detail.anggota);
      if (mode === 'aktif' && !konfirmasiAktif) {
        detail = null;
        throw new Error('Status aktif anggota ini tidak dapat diubah.');
      }
      fokusPanel();
    });
  }

  function bukaAnggota(e: MouseEvent, id: number, mode: ModeDetail) {
    permintaanDetail = { id, mode };
    void tampilkanPanel(e).then(() => buka(id, mode));
  }

  function bolehAktifkan(a: Anggota) {
    return a.anggota === 'Y' && a.aktif === 'T';
  }

  function simpan() {
    const dipilih = detail, perubahan = edit;
    if (!dipilih || !perubahan || perubahan.limit_hutang === undefined) return;
    const { unit, status, aktif, anggota } = dipilih.anggota;
    const mutasiProfil = berubahTagihan && editMutasi
      ? { awal: { unit, status, aktif, anggota }, ...editMutasi }
      : undefined;
    if (mutasiProfil && !mutasiProfil.alasan.trim()) return;
    return jalankan(async () => {
      await api<Profil>(endpoint + '/' + dipilih.anggota.id, {
        method: 'PATCH',
        body: { awal: dipilih.profil, profil: perubahan, mutasi: mutasiProfil }
      });
      tutupPanel();
      pesan = 'Profil anggota tersimpan.';
      await muat(halaman);
    });
  }

  function ubahAktif() {
    const dipilih = detail;
    if (!dipilih || !konfirmasiAktif) return;
    return jalankan(async () => {
      await api<{ aktif: boolean }>(endpoint + '/' + dipilih.anggota.id + '/aktif', {
        method: 'PUT', body: { aktif: true, alasan: hasil.riwayat ? alasanAktif : undefined }
      });
      tutupPanel();
      pesan = 'Anggota diaktifkan.';
      await muat(halaman);
    });
  }

  function resetFilter() {
    filter = { ...kosong, q: filter.q };
    void jalankan(() => muat(1, filter));
  }
</script>

<section class="directory" aria-label="Anggota Koperasi">
  <div class="tab" role="tablist" aria-label="Pengelolaan anggota koperasi">
    {#each tabTerlihat as t, i (t.id)}
      <button type="button" role="tab" id={'anggota-tab-' + t.id}
        aria-controls={'anggota-panel-' + t.id} aria-selected={tab === t.id}
        aria-disabled={sibuk} tabindex={tab === t.id ? 0 : -1}
        on:click={() => pilihTab(t.id)} on:keydown={(e) => tombolTab(e, i)}>
        <svelte:component this={t.icon} size={18} aria-hidden="true" />
        {t.label}
      </button>
    {/each}
  </div>
  {#if pesan}<p class="notice feedback" role="status">{pesan}</p>{/if}

  <div class="tabpanel" role="tabpanel" id="anggota-panel-anggota" aria-labelledby="anggota-tab-anggota" hidden={tab !== 'anggota'} tabindex="0">
    <form class="toolbar" on:submit|preventDefault={() => jalankan(() => muat(1, filter))}>
      <label class="search">
        <Search size={18} aria-hidden="true" />
        <input type="search" aria-label="Cari nama atau NIK" placeholder="Cari nama atau NIK" bind:value={filter.q} maxlength="100" />
      </label>
      <button class="secondary action" disabled={sibuk}>Cari</button>
      <button type="button" class="secondary action" aria-expanded={filterTerbuka}
        aria-controls="filter-anggota" on:click={() => filterTerbuka = !filterTerbuka}>
        <ListFilter size={18} aria-hidden="true" /> Filter{jumlahFilter ? ' (' + jumlahFilter + ')' : ''}
      </button>
      {#if hasil.registrasi}
        <button type="button" class="primary action add" disabled={sibuk || !unit.length} on:click={bukaRegistrasi}>
          <Plus size={18} aria-hidden="true" /> Daftarkan anggota
        </button>
      {/if}
      <div class="filters" id="filter-anggota" hidden={!filterTerbuka}>
        <label>Unit kerja<select bind:value={filter.unit}><option value="">Semua unit</option>
          {#each unit as u (u.kode)}<option value={u.kode}>{u.nama ?? u.kode}</option>{/each}
        </select></label>
        <label>Status aktif<select bind:value={filter.aktif}><option value="">Semua</option><option value="Y">Aktif</option><option value="T">Nonaktif</option></select></label>
        <label>Keanggotaan<select bind:value={filter.anggota}><option value="">Semua</option><option value="Y">Anggota</option><option value="T">Nonanggota</option></select></label>
        <label>Status tagihan<select bind:value={filter.status}><option value="">Semua</option><option>BULANAN</option><option>KHT</option></select></label>
        <div class="button-group">
          <button class="primary action" disabled={sibuk}>Terapkan</button>
          <button type="button" class="secondary action" disabled={sibuk} on:click={resetFilter}>Reset filter</button>
        </div>
      </div>
    </form>

    {#if galatAnggota}
      <div class="feedback failure" role="alert">
        <span>Daftar anggota gagal dimuat. {galatAnggota}</span>
        <button type="button" class="secondary action" disabled={sibuk} on:click={() => jalankan(() => muat(ulangAnggota.halaman, ulangAnggota.filter))}>
          <RotateCw size={16} aria-hidden="true" /> Coba lagi
        </button>
      </div>
    {/if}
    {#if galatUnit}
      <div class="feedback failure" role="alert">
        <span>Pilihan unit kerja gagal dimuat. {galatUnit}</span>
        <button type="button" class="secondary action" disabled={sibuk} on:click={() => jalankan(muatUnit)}>
          <RotateCw size={16} aria-hidden="true" /> Coba lagi
        </button>
      </div>
    {/if}
    <div class="tabel" aria-busy={sibuk}>
      {#if hasil.data.length}
        <table aria-label="Daftar anggota">
          <thead><tr><th scope="col">Nama / NIK</th><th scope="col">Unit kerja</th><th scope="col">Status aktif</th><th scope="col">Keanggotaan</th><th scope="col">Tagihan</th><th scope="col" class="limit">Limit hutang</th><th scope="col" class="action-cell">Aksi</th></tr></thead>
          <tbody>
            {#each hasil.data as a (a.id)}
              <tr>
                <th scope="row"><strong>{a.nama ?? 'Tanpa nama'}</strong><small>{a.nik ?? '—'}</small></th>
                <td>{unit.find(u => u.kode === a.unit)?.nama ?? a.unit ?? '—'}</td>
                <td><span class="badge" class:positive={a.aktif === 'Y'}>{a.aktif === 'Y' ? 'Aktif' : a.aktif === 'T' ? 'Nonaktif' : '—'}</span></td>
                <td><span class="badge">{a.anggota === 'Y' ? 'Anggota' : a.anggota === 'T' ? 'Nonanggota' : '—'}</span></td>
                <td>{a.status ?? '—'}</td>
                <td class="limit">{labelLimit(a.limit_hutang)}</td>
                <td><div class="row-actions">
                  <IconButton icon={Pencil} label={'Ubah profil ' + (a.nama ?? a.nik ?? a.id)} hint="Ubah profil" disabled={sibuk} on:click={(e) => bukaAnggota(e, a.id, 'ubah')} />
                  {#if bolehAktifkan(a)}
                    <IconButton icon={Power} label={'Aktifkan ' + (a.nama ?? a.nik ?? a.id)}
                      hint="Aktifkan" disabled={sibuk} on:click={(e) => bukaAnggota(e, a.id, 'aktif')} />
                  {/if}
                  {#if hasil.riwayat}
                    {#if a.anggota === 'Y'}
                      <IconButton icon={UserMinus} danger label={'Berhentikan anggota ' + (a.nama ?? a.id)} hint="Berhentikan anggota" disabled={sibuk} on:click={(e) => bukaMutasi(e, a, 'berhenti')} />
                    {:else if a.dapat_gabung === 1}
                      <IconButton icon={UserPlus} label={'Gabung kembali ' + (a.nama ?? a.id)} hint="Gabung kembali" disabled={sibuk} on:click={(e) => bukaMutasi(e, a, 'gabung_kembali')} />
                    {/if}
                    <IconButton icon={History} label={'Riwayat ' + (a.nama ?? a.id)} hint="Riwayat anggota" disabled={sibuk} on:click={() => bukaRiwayat(a)} />
                  {/if}
                </div></td>
              </tr>
            {/each}
          </tbody>
        </table>
      {:else if sibuk}
        <p class="empty" role="status">Memuat daftar anggota…</p>
      {:else if !galatAnggota}
        <p class="empty">Tidak ada anggota yang cocok.</p>
      {/if}
    </div>
    {#if hasil.data.length || halaman > 1}
      <nav class="pagination" aria-label="Halaman anggota">
        <span aria-live="polite">Halaman {halaman}</span>
        <div class="button-group">
          <IconButton icon={ChevronLeft} label="Halaman sebelumnya" disabled={sibuk || halaman === 1} on:click={() => jalankan(() => muat(halaman - 1))} />
          <IconButton icon={ChevronRight} label="Halaman berikutnya" disabled={sibuk || !hasil.lanjut} on:click={() => jalankan(() => muat(halaman + 1))} />
        </div>
      </nav>
    {/if}
  </div>

  <div class="tabpanel" role="tabpanel" id="anggota-panel-unit" aria-labelledby="anggota-tab-unit" hidden={tab !== 'unit'} tabindex="0">
    <div class="toolbar">
      <label class="search">
        <Search size={18} aria-hidden="true" />
        <input type="search" aria-label="Cari nama atau kode unit" placeholder="Cari nama atau kode unit" bind:value={cariUnit} />
      </label>
      <IconButton icon={RotateCw} label="Muat ulang unit kerja" disabled={sibuk} on:click={() => jalankan(muatUnit)} />
      <button type="button" class="primary action add" disabled={sibuk} on:click={(e) => bukaUnit(e)}>
        <Plus size={18} aria-hidden="true" /> Tambah unit kerja
      </button>
    </div>
    {#if galatUnit}
      <div class="feedback failure" role="alert">
        <span>Daftar unit kerja gagal dimuat. {galatUnit}</span>
        <button type="button" class="secondary action" disabled={sibuk} on:click={() => jalankan(muatUnit)}>
          <RotateCw size={16} aria-hidden="true" /> Coba lagi
        </button>
      </div>
    {/if}
    <div class="tabel" aria-busy={sibuk}>
      {#if unitTerlihat.length}
        <table aria-label="Daftar unit kerja">
          <thead><tr><th scope="col">Kode</th><th scope="col">Nama unit kerja</th><th scope="col">Potong SHU</th><th scope="col" class="action-cell">Aksi</th></tr></thead>
          <tbody>
            {#each unitTerlihat as u (u.kode)}
              <tr>
                <td>{u.kode}</td><th scope="row"><strong>{u.nama ?? '—'}</strong></th>
                <td><span class="badge" class:positive={u.potong_shu === 'Y'}>{labelFlag(u.potong_shu)}</span></td>
                <td><div class="row-actions">
                  <IconButton icon={Pencil} label={'Ubah unit ' + (u.nama ?? u.kode)} hint="Ubah unit" disabled={sibuk} on:click={(e) => bukaUnit(e, u)} />
                  <IconButton icon={Trash2} danger label={'Hapus unit ' + (u.nama ?? u.kode)} hint="Hapus unit" disabled={sibuk} on:click={(e) => bukaHapus(e, u)} />
                </div></td>
              </tr>
            {/each}
          </tbody>
        </table>
      {:else if sibuk}
        <p class="empty" role="status">Memuat unit kerja…</p>
      {:else if !galatUnit}
        <p class="empty">{cariUnit.trim() ? 'Tidak ada unit kerja yang cocok.' : 'Belum ada unit kerja.'}</p>
      {/if}
    </div>
  </div>
  {#if hasil.riwayat}
    <div class="tabpanel" role="tabpanel" id="anggota-panel-riwayat" aria-labelledby="anggota-tab-riwayat" hidden={tab !== 'riwayat'} tabindex="0">
      <form class="toolbar history-filter" on:submit|preventDefault={() => jalankan(() => muatRiwayat(1, filterRiwayat))}>
        {#if anggotaRiwayat}
          <span>{anggotaRiwayat.nama ?? anggotaRiwayat.nik ?? anggotaRiwayat.id}</span>
          <button type="button" class="secondary action" disabled={sibuk} on:click={() => { anggotaRiwayat = null; void jalankan(() => muatRiwayat(1, filterRiwayat)); }}>Semua anggota</button>
        {/if}
        <label>Nama atau NIK<input type="search" maxlength="100" bind:value={filterRiwayat.q} /></label>
        <label>Dari tanggal<input type="date" bind:value={filterRiwayat.dari} /></label>
        <label>Sampai tanggal<input type="date" min={filterRiwayat.dari || undefined} bind:value={filterRiwayat.sampai} /></label>
        <label>Kejadian<select bind:value={filterRiwayat.jenis}>
          <option value="">Semua kejadian</option>
          {#each Object.entries(labelKejadian) as [kode, label] (kode)}<option value={kode}>{label}</option>{/each}
        </select></label>
        <button class="primary action" disabled={sibuk}><Search size={18} aria-hidden="true" /> Cari</button>
      </form>
      {#if galatRiwayat}<p class="feedback failure" role="alert">{galatRiwayat}</p>{/if}
      <div class="tabel" aria-busy={sibuk}>
        {#if riwayat.data.length}
          <table aria-label="Riwayat perubahan anggota">
            <thead><tr><th scope="col">Waktu (WIB)</th><th scope="col">Nama / NIK</th><th scope="col">Kejadian</th><th scope="col">Perubahan</th><th scope="col">Admin / Alasan</th></tr></thead>
            <tbody>
              {#each riwayat.data as r (r.id)}
                <tr>
                  <td>{r.waktu}</td>
                  <th scope="row">{r.nama ?? '—'}<small>{r.nik ?? '—'}</small></th>
                  <td>{labelKejadian[r.jenis] ?? r.jenis}</td>
                  <td>
                    {#if r.unit_dari !== r.unit_ke}<div>Unit {r.unit_dari ?? '—'} → {r.unit_ke ?? '—'}</div>{/if}
                    {#if r.status_dari !== r.status_ke}<div>Tagihan {r.status_dari ?? '—'} → {r.status_ke ?? '—'}</div>{/if}
                    {#if r.aktif_dari !== r.aktif_ke}<div>Aktif: {labelFlag(r.aktif_dari)} → {labelFlag(r.aktif_ke)}</div>{/if}
                    {#if r.anggota_dari !== r.anggota_ke}<div>Anggota: {labelFlag(r.anggota_dari)} → {labelFlag(r.anggota_ke)}</div>{/if}
                    {#if r.tmk_ke}<div>Tanggal masuk: {tanggalMasuk(r.tmk_dari)} → {tanggalMasuk(r.tmk_ke)}</div>{/if}
                  </td>
                  <td class="history-reason">{r.admin_nama}<small>{r.alasan}</small></td>
                </tr>
              {/each}
            </tbody>
          </table>
        {:else if sibuk}
          <p class="empty" role="status">Memuat riwayat…</p>
        {:else if !galatRiwayat}
          <p class="empty">Tidak ada riwayat untuk filter ini.</p>
        {/if}
      </div>
      {#if riwayat.data.length || halamanRiwayat > 1}
        <nav class="pagination" aria-label="Halaman riwayat anggota">
          <span aria-live="polite">Halaman {halamanRiwayat}</span>
          <div class="button-group">
            <IconButton icon={ChevronLeft} label="Riwayat sebelumnya" disabled={sibuk || halamanRiwayat === 1} on:click={() => jalankan(() => muatRiwayat(halamanRiwayat - 1))} />
            <IconButton icon={ChevronRight} label="Riwayat berikutnya" disabled={sibuk || !riwayat.lanjut} on:click={() => jalankan(() => muatRiwayat(halamanRiwayat + 1))} />
          </div>
        </nav>
      {/if}
    </div>
  {/if}
</section>

<dialog bind:this={dialog} aria-labelledby="judul-panel-anggota" on:cancel={batalPanel}>
  <header class="drawer-header">
    <h3 id="judul-panel-anggota" tabindex="-1">{judulPanel}</h3>
    <IconButton icon={X} label="Tutup panel" disabled={sibuk} on:click={tutupPanel} />
  </header>
  <div class="drawer-body" aria-busy={sibuk}>
    {#if galat}<p class="feedback failure" role="alert">{galat}</p>{/if}
    {#if formUnit}
      <form on:submit|preventDefault={simpanUnit}>
        <fieldset disabled={sibuk}>
          {#if formUnit.kode}<label>Kode<input value={formUnit.kode} readonly /></label>{/if}
          <label>Nama unit kerja<input bind:value={formUnit.nama} maxlength="50" required /></label>
          <label class="checkbox"><input type="checkbox" bind:checked={formUnit.potong_shu} /> Potong SHU</label>
          <div class="drawer-actions">
            <button type="button" class="secondary action" on:click={tutupPanel}>Batal</button>
            <button class="primary action"><Save size={18} aria-hidden="true" /> Simpan</button>
          </div>
        </fieldset>
      </form>
    {:else if hapusUnit}
      <p>Hapus <strong>{hapusUnit.nama ?? hapusUnit.kode}</strong> dari daftar unit kerja?</p>
      <div class="drawer-actions">
        <button type="button" class="secondary action" disabled={sibuk} on:click={tutupPanel}>Batal</button>
        <button type="button" class="danger-action action" disabled={sibuk} on:click={konfirmasiHapusUnit}><Trash2 size={18} aria-hidden="true" /> Hapus unit</button>
      </div>
    {:else if mutasi}
      <p><strong>{mutasi.a.nama ?? 'Anggota'}</strong> {mutasi.a.nik ?? ''}</p>
      <form on:submit|preventDefault={simpanMutasi}>
        <fieldset disabled={sibuk}>
          {#if mutasi.aksi === 'berhenti'}
            <p>Keanggotaan berakhir dan akun anggota yang tertaut akan dinonaktifkan.</p>
          {:else}
            <p>Keanggotaan dipulihkan dengan NIK yang sama. Akses login dapat dipulihkan melalui menu Pengguna.</p>
            <p>Tanggal masuk lama: <strong>{tanggalMasuk(mutasi.a.tmk)}</strong><br />
              Tanggal masuk yang akan diakui: <strong>{tanggalMasuk(mutasi.tanggal)}</strong></p>
            {#if !hasil.audit_tmk}<p role="status">Gabung kembali tersedia setelah pencatatan tanggal masuk diaktifkan.</p>{/if}
          {/if}
          <label>Alasan<input bind:value={mutasi.alasan} maxlength="500" required /></label>
          <div class="drawer-actions">
            <button type="button" class="secondary action" on:click={tutupPanel}>Batal</button>
            <button class="action" class:primary={mutasi.aksi !== 'berhenti'} class:danger-action={mutasi.aksi === 'berhenti'}
              disabled={!mutasi.alasan.trim() || (mutasi.aksi === 'gabung_kembali' && !hasil.audit_tmk)}>
              {labelPerubahan[mutasi.aksi]}
            </button>
          </div>
        </fieldset>
      </form>
    {:else if registrasi}
      <form novalidate on:submit|preventDefault={daftarBaru}>
        <fieldset disabled={sibuk}>
          <label>Nama<input id="registrasi-nama" bind:value={registrasi.nama} required maxlength="50"
            aria-invalid={!!galatRegistrasi.nama} aria-describedby={galatRegistrasi.nama ? 'galat-reg-nama' : undefined} /></label>
          {#if galatRegistrasi.nama}<small class="field-error" id="galat-reg-nama">{galatRegistrasi.nama}</small>{/if}
          <label>Unit kerja<select id="registrasi-unit" bind:value={registrasi.unit} required
              aria-invalid={!!galatRegistrasi.unit} aria-describedby={galatRegistrasi.unit ? 'galat-reg-unit' : undefined}>
            <option value="">Pilih unit</option>
            {#each unit as u (u.kode)}<option value={u.kode}>{u.nama ?? u.kode}</option>{/each}
          </select></label>
          {#if galatRegistrasi.unit}<small class="field-error" id="galat-reg-unit">{galatRegistrasi.unit}</small>{/if}
          <label>Status tagihan<select id="registrasi-status" bind:value={registrasi.status} required
              aria-invalid={!!galatRegistrasi.status} aria-describedby={galatRegistrasi.status ? 'galat-reg-status' : undefined}>
            <option>BULANAN</option><option>KHT</option>
          </select></label>
          {#if galatRegistrasi.status}<small class="field-error" id="galat-reg-status">{galatRegistrasi.status}</small>{/if}
          <label>Tanggal masuk<input id="registrasi-tmk" type="date" bind:value={registrasi.tmk}
            min="0001-01-01" max={batasTanggalMasuk} required
            aria-invalid={!!galatRegistrasi.tmk} aria-describedby={galatRegistrasi.tmk ? 'galat-reg-tmk' : undefined} /></label>
          {#if galatRegistrasi.tmk}<small class="field-error" id="galat-reg-tmk">{galatRegistrasi.tmk}</small>{/if}
          <label>Limit hutang (Rp)<input id="registrasi-limit_hutang" type="number" bind:value={registrasi.limit_hutang}
            required min="0" max={Number.MAX_SAFE_INTEGER} step="1" aria-invalid={!!galatRegistrasi.limit_hutang}
            aria-describedby={galatRegistrasi.limit_hutang ? 'petunjuk-limit-baru galat-reg-limit' : 'petunjuk-limit-baru'} /></label>
          <small id="petunjuk-limit-baru">0 = tanpa batas</small>
          {#if galatRegistrasi.limit_hutang}<small class="field-error" id="galat-reg-limit">{galatRegistrasi.limit_hutang}</small>{/if}
          <div class="drawer-actions">
            <button type="button" class="secondary action" on:click={tutupPanel}>Batal</button>
            <button class="primary action"><Plus size={18} aria-hidden="true" /> Daftarkan</button>
          </div>
        </fieldset>
      </form>
    {:else if detail}
      {#if edit}
        <form on:submit|preventDefault={simpan}>
          <fieldset disabled={sibuk}>
            <label>Nama<input bind:value={edit.nama} required maxlength="50" /></label>
            <label>Tanggal masuk yang diakui<input value={tanggalMasuk(detail.anggota.tmk)} readonly /></label>
            <label>Alamat<input bind:value={edit.alamat} maxlength="100" /></label>
            <div class="field-pair">
              <label>Kota<input bind:value={edit.kota} maxlength="50" /></label>
              <label>Kode pos<input bind:value={edit.kode_pos} maxlength="10" /></label>
            </div>
            <label>Telepon<input type="tel" bind:value={edit.telepon} maxlength="20" /></label>
            {#if editMutasi}
              <div class="field-pair">
                <label>Unit kerja<select bind:value={editMutasi.unit} required={berubahTagihan}>
                  <option value="">Pilih unit</option>
                  {#if detail.anggota.unit && !unit.some(u => u.kode === detail?.anggota.unit)}
                    <option value={detail.anggota.unit}>{detail.anggota.unit}</option>
                  {/if}
                  {#each unit as u (u.kode)}<option value={u.kode}>{u.nama ?? u.kode}</option>{/each}
                </select></label>
                <label>Kategori tagihan<select bind:value={editMutasi.status} required={berubahTagihan}>
                  <option value="">Pilih kategori</option><option>BULANAN</option><option>KHT</option>
                </select></label>
              </div>
              {#if berubahTagihan}
                <label>Alasan perubahan unit atau tagihan<input bind:value={editMutasi.alasan} maxlength="500" required /></label>
                <small>Perubahan berlaku saat disimpan. Rekap yang membaca data anggota terkini dapat menampilkan unit atau kategori baru untuk periode lama.</small>
              {/if}
            {/if}
            <div>
              <label>Limit hutang per periode (Rp)
                <input type="number" bind:value={edit.limit_hutang} required min="0" max={Number.MAX_SAFE_INTEGER}
                  step="1" aria-describedby="petunjuk-limit-hutang" />
              </label>
              <small id="petunjuk-limit-hutang">0 = tanpa batas</small>
            </div>
            <div class="drawer-actions">
              {#if galat}<button type="button" class="secondary action" on:click={() => detail && buka(detail.anggota.id, 'ubah')}><RotateCw size={16} aria-hidden="true" /> Muat ulang detail</button>{/if}
              <button type="button" class="secondary action" on:click={tutupPanel}>Batal</button>
              <button class="primary action" disabled={berubahTagihan && !editMutasi?.alasan.trim()}><Save size={18} aria-hidden="true" /> Simpan</button>
            </div>
          </fieldset>
        </form>
      {:else if konfirmasiAktif}
        <p>Aktifkan <strong>{detail.profil.nama ?? detail.anggota.nik ?? 'anggota ini'}</strong>?</p>
        {#if hasil.riwayat}<label>Alasan<input bind:value={alasanAktif} maxlength="500" disabled={sibuk} /></label>{/if}
        <div class="drawer-actions">
          {#if galat}<button type="button" class="secondary action" disabled={sibuk} on:click={() => detail && buka(detail.anggota.id, 'aktif')}><RotateCw size={16} aria-hidden="true" /> Muat ulang detail</button>{/if}
          <button type="button" class="secondary action" disabled={sibuk} on:click={tutupPanel}>Batal</button>
          <button type="button" class="primary action" disabled={sibuk || (hasil.riwayat && !alasanAktif.trim())} on:click={ubahAktif}><Power size={18} aria-hidden="true" /> Aktifkan</button>
        </div>
      {/if}
    {:else if sibuk}
      <p role="status">Memuat detail anggota…</p>
    {:else if galat && permintaanDetail}
      <button type="button" class="secondary action" on:click={() => permintaanDetail && buka(permintaanDetail.id, permintaanDetail.mode)}>
        <RotateCw size={16} aria-hidden="true" /> Coba lagi
      </button>
    {/if}
  </div>
</dialog>

<style>
  .directory, dialog {
    --directory-surface: #fff;
    --directory-subtle: #f4f8f5;
    --directory-line: #dce7df;
    --directory-ink: #20332a;
    --directory-muted: #52685b;
    --directory-active: #e3f2e8;
    --directory-accent: #176337;
    color: var(--directory-ink);
    background: var(--directory-surface);
  }
  :global(.dark) .directory, :global(.dark) dialog,
  :global([data-theme='dark']) .directory, :global([data-theme='dark']) dialog {
    --directory-surface: #182a21;
    --directory-subtle: #20362a;
    --directory-line: #3a5145;
    --directory-ink: #e6f3ea;
    --directory-muted: #b6c9bd;
    --directory-active: #294836;
    --directory-accent: #9ae6b5;
  }
  .directory { border: 1px solid var(--directory-line); border-radius: 16px; min-width: 0; }
  .tab { display: flex; flex-wrap: wrap; gap: .25rem; border-bottom: 1px solid var(--directory-line); padding-inline: 1rem; }
  .tab button { display: inline-flex; align-items: center; gap: .5rem; min-height: 48px; font-weight: 700; color: var(--directory-muted); border-bottom: 3px solid transparent; }
  .tab [aria-selected='true'] { color: var(--directory-accent); border-bottom-color: currentColor; }
  .tabpanel { min-width: 0; }
  .toolbar { display: flex; flex-wrap: wrap; align-items: center; gap: .5rem; padding: 1rem; }
  .action, .button-group, .row-actions { display: inline-flex; align-items: center; gap: .5rem; }
  .action { justify-content: center; min-height: 44px; }
  .add { margin-inline-start: auto; }
  .search { display: flex; align-items: center; gap: .5rem; flex: 1 1 14rem; max-width: 26rem; padding-inline-start: .75rem; color: var(--directory-muted); border: 1px solid var(--directory-line); border-radius: 10px; }
  .search:focus-within { outline: 2px solid var(--directory-accent); outline-offset: 1px; }
  .search input { flex: 1; min-width: 0; min-height: 42px; padding-inline: 0 .75rem; border: 0; outline: 0; background: transparent; color: var(--directory-ink); }
  .filters { flex-basis: 100%; padding: .85rem; border: 1px solid var(--directory-line); border-radius: 10px; background: var(--directory-subtle); }
  .filters:not([hidden]) { display: flex; flex-wrap: wrap; align-items: end; gap: .75rem; }
  .filters label { flex: 1 1 8rem; }
  .tabel { overflow-x: auto; }
  table { width: 100%; border-collapse: collapse; }
  th, td { padding: .7rem 1rem; text-align: start; border-bottom: 1px solid var(--directory-line); }
  thead th { font-size: .82rem; white-space: nowrap; color: var(--directory-muted); background: var(--directory-subtle); }
  tbody th { font-weight: 600; min-width: 10rem; }
  tbody tr:hover { background: var(--directory-subtle); }
  small { display: block; margin-top: .18rem; font-weight: 400; color: var(--directory-muted); }
  .action-cell, .row-actions { white-space: nowrap; }
  .action-cell { text-align: end; }
  .limit { text-align: end; white-space: nowrap; font-variant-numeric: tabular-nums; }
  .history-filter { align-items: end; }
  tr td.history-reason { text-align: start; overflow-wrap: anywhere; }
  tr td:last-child { text-align: end; }
  .badge { display: inline-block; white-space: nowrap; padding: .2rem .6rem; border-radius: 999px; background: var(--directory-subtle); color: var(--directory-muted); font-size: .82rem; }
  .badge.positive { color: var(--directory-accent); background: var(--directory-active); font-weight: 650; }
  .pagination { display: flex; justify-content: space-between; align-items: center; gap: .75rem; padding: .5rem 1rem; color: var(--directory-muted); }
  .empty, .feedback { margin: 0 1rem 1rem; padding: .75rem; }
  .empty { color: var(--directory-muted); text-align: center; }
  .failure { color: #912720; background: #fcebe8; border-radius: 8px; }
  .field-error { margin-top: -.65rem; color: #c0443d; font-weight: 650; }
  :global(.dark) .field-error { color: #ffaaa4; }
  .failure .action { margin-inline-start: .5rem; }
  dialog { position: fixed; inset: 0 0 0 auto; width: min(100%, 32rem); height: 100dvh; max-width: none; max-height: none; padding: 0; margin: 0; border: 0; box-shadow: -12px 0 40px #101e1533; }
  dialog::backdrop { background: #13241b66; }
  .drawer-header { position: sticky; top: 0; z-index: 1; display: flex; justify-content: space-between; align-items: center; gap: .5rem; padding: .85rem 1.25rem; border-bottom: 1px solid var(--directory-line); background: var(--directory-surface); }
  .drawer-header h3 { margin: 0; font-size: 1.15rem; }
  .drawer-body { padding: 1.25rem; }
  .drawer-body .feedback { margin-inline: 0; }
  .drawer-actions { display: flex; flex-wrap: wrap; justify-content: flex-end; gap: .5rem; margin-top: 1rem; padding-top: 1rem; border-top: 1px solid var(--directory-line); }
  .danger-action { color: #fff; background: #a82d26; border-radius: 8px; padding-inline: 1rem; }
  fieldset { display: grid; gap: 1rem; margin: 0; padding: 0; border: 0; min-width: 0; }
  label { display: grid; gap: .3rem; }
  .checkbox { display: flex; align-items: center; gap: .5rem; }
  .field-pair { display: grid; grid-template-columns: 1fr 1fr; gap: .75rem; }
  .field-pair input { width: 100%; min-width: 0; }
  @media (max-width: 600px) {
    .search { max-width: none; }
    .add { margin-inline-start: 0; }
    dialog { width: 100%; }
    th, td { padding-inline: .65rem; }
  }
</style>
