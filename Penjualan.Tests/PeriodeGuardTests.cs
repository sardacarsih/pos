using Kopkar.Shared;
using Penjualan.DataLayer;
using Xunit;

namespace Penjualan.Tests
{
    public class PeriodeGuardTests
    {
        private static readonly DateTime R1Dari = new(2026, 10, 26), R1Sampai = new(2026, 11, 15);
        private static readonly DateTime R2Dari = new(2026, 11, 16), R2Sampai = new(2026, 11, 25);

        [Theory]
        [InlineData(2026, 10, 28, 1)] // hari > 15 tapi masih di R1 (aturan lama salah)
        [InlineData(2026, 11, 10, 1)]
        [InlineData(2026, 11, 20, 2)]
        public void Remise_mengikuti_rentang(int y, int m, int d, int expected) =>
            Assert.Equal(expected, PeriodeGuard.PilihRemise(new DateTime(y, m, d), R1Dari, R1Sampai, R2Dari, R2Sampai));

        [Theory]
        [InlineData(10, 1)]
        [InlineData(20, 2)]
        public void Remise_fallback_aturan_hari_bila_rentang_kosong(int day, int expected) =>
            Assert.Equal(expected, PeriodeGuard.PilihRemise(new DateTime(2026, 11, day), null, null, null, null));

        [Fact]
        public void Evaluasi_terbuka_bila_flag_bukan_Y_dan_tidak_melebihi_max()
        {
            Assert.True(PeriodeGuard.Evaluasi(202611, null, 202612).Terbuka);
            Assert.True(PeriodeGuard.Evaluasi(202611, " T ", 202611).Terbuka);
        }

        [Fact]
        public void Evaluasi_tutup_bila_flag_Y()
        {
            var r = PeriodeGuard.Evaluasi(202611, " y ", 202611);
            Assert.Equal(PeriodeKode.Tutup, r.Kode);
            Assert.False(r.Terbuka);
        }

        [Fact]
        public void Evaluasi_tidak_valid_bila_melebihi_max()
        {
            Assert.Equal(PeriodeKode.TidakValid, PeriodeGuard.Evaluasi(202701, null, 202612).Kode);
        }
    }
}
