using Penjualan.BusinessLayer;
using Penjualan.Model;
using Xunit;

namespace Penjualan.Tests
{
    public class PeriodeResolverTests
    {
        private static DateTime D(int y, int m, int d) => new(y, m, d);

        // Non-calendar periods: bulanan 26th..25th, remise I 26th..10th, remise II 11th..25th.
        private static PeriodeRow Nov(bool r1Tutup = false, bool r2Tutup = false, bool bTutup = false) => new()
        {
            Periode = 202611,
            R1Dari = D(2026, 10, 26), R1Sampai = D(2026, 11, 10),
            R2Dari = D(2026, 11, 11), R2Sampai = D(2026, 11, 25),
            BDari = D(2026, 10, 26), BSampai = D(2026, 11, 25),
            Remise1Tutup = r1Tutup, Remise2Tutup = r2Tutup, BulananTutup = bTutup
        };

        private static PeriodeRow Des() => new()
        {
            Periode = 202612,
            R1Dari = D(2026, 11, 26), R1Sampai = D(2026, 12, 10),
            R2Dari = D(2026, 12, 11), R2Sampai = D(2026, 12, 25),
            BDari = D(2026, 11, 26), BSampai = D(2026, 12, 25)
        };

        [Fact]
        public void CreditWindow_Bulanan_UsesRangeCoveringDate_NotCalendarMonth()
        {
            var rows = new[] { Nov(), Des() };
            Assert.Equal((D(2026, 10, 26), D(2026, 11, 25)), PeriodeResolver.CreditWindow(rows, D(2026, 11, 7), "BULANAN"));
            // 27-Nov is calendar November but belongs to the December period.
            Assert.Equal((D(2026, 11, 26), D(2026, 12, 25)), PeriodeResolver.CreditWindow(rows, D(2026, 11, 27), "BULANAN"));
        }

        [Fact]
        public void CreditWindow_Remise_UsesFullRemiseSpan()
        {
            var rows = new[] { Nov(), Des() };
            Assert.Equal((D(2026, 10, 26), D(2026, 11, 25)), PeriodeResolver.CreditWindow(rows, D(2026, 11, 20), "REMISE"));
        }

        [Fact]
        public void CreditWindow_IgnoresTimeOfDay()
        {
            var rows = new[] { Nov() };
            Assert.NotNull(PeriodeResolver.CreditWindow(rows, new DateTime(2026, 11, 25, 23, 59, 0), "BULANAN"));
        }

        [Fact]
        public void FindRemise_PicksSegmentByRange()
        {
            var rows = new[] { Nov() };
            Assert.Same(rows[0], PeriodeResolver.FindRemise(rows, D(2026, 10, 30), out int s1));
            Assert.Equal(1, s1);
            Assert.Same(rows[0], PeriodeResolver.FindRemise(rows, D(2026, 11, 11), out int s2));
            Assert.Equal(2, s2);
        }

        [Fact]
        public void DateNotCovered_IsRejected_AndHasNoWindow()
        {
            var rows = new[] { Nov() };
            Assert.Null(PeriodeResolver.CreditWindow(rows, D(2026, 11, 26), "BULANAN"));
            Assert.Contains("belum masuk periode", PeriodeResolver.AlasanTolak(rows, D(2026, 11, 26), null));
        }

        [Fact]
        public void RowWithNullDates_IsNotConfigured()
        {
            var rows = new[] { new PeriodeRow { Periode = 202611 } };
            Assert.Null(PeriodeResolver.CreditWindow(rows, D(2026, 11, 7), "REMISE"));
            Assert.NotNull(PeriodeResolver.AlasanTolak(rows, D(2026, 11, 7), null));
        }

        [Fact]
        public void OpenPeriod_IsAllowed()
        {
            Assert.Null(PeriodeResolver.AlasanTolak(new[] { Nov() }, D(2026, 11, 7), "BULANAN"));
        }

        [Fact]
        public void ClosedRemiseSegment_IsRejected_OtherSegmentStillOpen()
        {
            var rows = new[] { Nov(r1Tutup: true) };
            Assert.Contains("Remise 1", PeriodeResolver.AlasanTolak(rows, D(2026, 11, 5), null));
            Assert.Null(PeriodeResolver.AlasanTolak(rows, D(2026, 11, 15), null));
        }

        [Fact]
        public void ClosedBulanan_RejectsOnlyBulananMembers()
        {
            var rows = new[] { Nov(bTutup: true) };
            Assert.Contains("bulanan", PeriodeResolver.AlasanTolak(rows, D(2026, 11, 7), "BULANAN"));
            Assert.Null(PeriodeResolver.AlasanTolak(rows, D(2026, 11, 7), "REMISE"));
            Assert.Null(PeriodeResolver.AlasanTolak(rows, D(2026, 11, 7), null));
        }
    }
}
