// Token warna v2: hijau perkebunan, selaras dengan halaman login. `grad` = gradien tombol utama/chip terpilih,
// `accent` = hijau untuk teks/ikon aksen (kontras >= 4,5:1 di atas putih/kartu).
export const light = {
  mode: 'terang',
  bg: '#EEF7EA', surface: '#FFFFFF', surface2: '#F4F6F5', text: '#1E2B22', muted: '#5B665E',
  outline: '#E1E6E2', primary: '#1F8A35', grad: ['#43B22E', '#0D6A2A'], onPrimary: '#FFFFFF',
  primaryCont: '#E3F1E0', onPrimaryCont: '#0B5A2B', accent: '#0E7A2E',
  error: '#B3261E', errorCont: '#F9DEDC', onErrorCont: '#410E0B',
  warn: '#8A5A00', warnCont: '#FFEBC2', ok: '#1F7A3A', okCont: '#DDF1E3', scrim: '#00000066',
  foto: '#00000000', fotoOverlay: 0.18,
};
export const dark = {
  mode: 'gelap',
  bg: '#10241D', surface: '#17211B', surface2: '#223028', text: '#E6F5EA', muted: '#9DB0A3',
  outline: '#33443A', primary: '#3E9E34', grad: ['#3E9E34', '#145C27'], onPrimary: '#FFFFFF',
  primaryCont: '#1F3A2A', onPrimaryCont: '#C6EFD6', accent: '#8BE0A6',
  error: '#F2B8B5', errorCont: '#601410', onErrorCont: '#F9DEDC',
  warn: '#F2C265', warnCont: '#4A3300', ok: '#8BE0A6', okCont: '#17402A', scrim: '#000000AA',
  foto: '#0B2237', fotoOverlay: 0.62,
};
export const rp = (n) => 'Rp ' + String(Math.abs(n)).replace(/\B(?=(\d{3})+(?!\d))/g, '.');
