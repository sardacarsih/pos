import { angkaQty, bacaJumlahBeli, bacaRupiahBeli, formatJumlahBeli, rpBeli } from './pembelian';

type Rupiah = {
  jenis: 'rupiah';
  nilai: number | undefined;
  ubah: (nilai: number | undefined) => void;
  min?: number;
  max?: number;
};

type Jumlah = {
  jenis: 'jumlah';
  nilai: string;
  ubah: (nilai: string) => void;
};

type Opsi = Rupiah | Jumlah;

export function inputAngkaPembelian(input: HTMLInputElement, opsi: Opsi) {
  let nilaiTerakhir: Opsi['nilai'] = opsi.nilai;
  let valid = true;
  let terformat = false;

  function validasi(pesan = '') {
    valid = !pesan;
    input.setCustomValidity(pesan);
    input.title = pesan;
    if (pesan) input.setAttribute('aria-invalid', 'true');
    else input.removeAttribute('aria-invalid');
  }

  function periksaBatas(n: number, aturan: Rupiah) {
    if (n < (aturan.min ?? 0)) {
      validasi('Masukkan nominal minimal ' + rpBeli(aturan.min ?? 0) + '.');
    } else if (aturan.max !== undefined && n > aturan.max) {
      validasi('Nominal tidak boleh melebihi ' + rpBeli(aturan.max) + '.');
    } else {
      validasi();
    }
  }

  function tampilkan() {
    const nilai = opsi.nilai;
    if (nilai === '' || nilai === undefined) {
      input.value = '';
      terformat = false;
      validasi();
    } else if (opsi.jenis === 'jumlah') {
      const qty = angkaQty(opsi.nilai);
      input.value = qty ? formatJumlahBeli(qty) : opsi.nilai;
      terformat = !!qty && input.value.includes('.');
      validasi(qty ? '' : 'Periksa jumlah barang yang tersimpan dalam draf.');
    } else {
      const format = formatJumlahBeli(opsi.nilai);
      input.value = format === '—' ? '' : format;
      terformat = input.value !== '';
      if (opsi.nilai !== undefined) periksaBatas(opsi.nilai, opsi);
    }
  }

  function formatRupiah(n: number, kursor: number) {
    const digitDiKanan = input.value.slice(kursor).replace(/\D/g, '').length;
    const format = formatJumlahBeli(n);
    let posisi = format.length;
    let tersisa = digitDiKanan;
    while (posisi > 0 && tersisa > 0) {
      posisi -= 1;
      if (/\d/.test(format[posisi])) tersisa -= 1;
    }
    input.value = format;
    terformat = true;
    input.setSelectionRange(posisi, posisi);
  }

  function masukan(e: Event) {
    if (e instanceof InputEvent && e.isComposing) return;
    const teks = input.value;
    const editDigit = e instanceof InputEvent &&
      (e.inputType.startsWith('delete') || (e.inputType === 'insertText' && /^\d+$/.test(e.data ?? '')));
    if (opsi.jenis === 'jumlah') {
      const qty = teks.trim() === '' ? '' : bacaJumlahBeli(
        terformat && editDigit ? teks.replaceAll('.', '') : teks
      );
      if (qty === null) {
        terformat = false;
        validasi('Gunakan titik untuk ribuan dan koma untuk maksimal tiga angka desimal.');
        return;
      }
      terformat = teks.includes('.');
      validasi();
      nilaiTerakhir = qty;
      opsi.ubah(qty);
      return;
    }
    if (teks.trim() === '') {
      input.value = '';
      terformat = false;
      validasi();
      nilaiTerakhir = undefined;
      opsi.ubah(undefined);
      return;
    }
    const n = bacaRupiahBeli(terformat && editDigit ? teks.replaceAll('.', '') : teks);
    if (n === null) {
      terformat = false;
      validasi('Masukkan nominal rupiah bulat, misalnya 12.500.');
      return;
    }
    periksaBatas(n, opsi);
    formatRupiah(n, input.selectionStart ?? teks.length);
    if (!valid) return;
    nilaiTerakhir = n;
    opsi.ubah(n);
  }

  function sebelumMasukan(e: InputEvent) {
    if (opsi.jenis !== 'rupiah' || !terformat || !e.cancelable || e.isComposing) return;
    const kursor = input.selectionStart;
    if (kursor === null || kursor !== input.selectionEnd) return;
    const mundur = e.inputType === 'deleteContentBackward' && input.value[kursor - 1] === '.';
    const maju = e.inputType === 'deleteContentForward' && input.value[kursor] === '.';
    if (!mundur && !maju) return;
    e.preventDefault();
    input.setRangeText('', mundur ? kursor - 2 : kursor, mundur ? kursor : kursor + 2, 'end');
    input.dispatchEvent(new InputEvent('input', { bubbles: true, inputType: e.inputType }));
  }

  function tempel(e: ClipboardEvent) {
    if (opsi.jenis !== 'rupiah' || !e.clipboardData) return;
    const teks = e.clipboardData.getData('text').trim();
    if (!teks) return;
    e.preventDefault();
    const n = bacaRupiahBeli(teks);
    if (n === null) {
      validasi('Tempel nominal rupiah bulat, misalnya Rp 12.500.');
      return;
    }
    const mulai = input.selectionStart ?? 0;
    const akhir = input.selectionEnd ?? input.value.length;
    input.setRangeText(String(n), mulai, akhir, 'end');
    input.dispatchEvent(new InputEvent('input', { bubbles: true, inputType: 'insertText', data: String(n) }));
  }

  function blur() {
    if (opsi.jenis === 'jumlah' && valid && nilaiTerakhir) {
      input.value = formatJumlahBeli(nilaiTerakhir);
      terformat = input.value.includes('.');
    }
  }

  function selesaiKomposisi() {
    input.dispatchEvent(new Event('input', { bubbles: true }));
  }

  tampilkan();
  input.addEventListener('input', masukan);
  input.addEventListener('compositionend', selesaiKomposisi);
  input.addEventListener('beforeinput', sebelumMasukan);
  input.addEventListener('paste', tempel);
  input.addEventListener('blur', blur);

  return {
    update(baru: Opsi) {
      opsi = baru;
      if (baru.nilai !== nilaiTerakhir) {
        nilaiTerakhir = baru.nilai;
        tampilkan();
      }
    },
    destroy() {
      input.removeEventListener('input', masukan);
      input.removeEventListener('compositionend', selesaiKomposisi);
      input.removeEventListener('beforeinput', sebelumMasukan);
      input.removeEventListener('paste', tempel);
      input.removeEventListener('blur', blur);
    }
  };
}
