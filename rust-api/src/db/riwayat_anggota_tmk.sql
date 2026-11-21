-- Jalankan terpisah setelah perubahan skema audit disetujui.
ALTER TABLE MOB_ANGGOTA_RIWAYAT ADD (TMK_DARI DATE, TMK_KE DATE);
