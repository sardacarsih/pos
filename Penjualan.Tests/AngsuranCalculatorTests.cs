using Penjualan.BusinessLayer;
using Penjualan.Model;
using Xunit;

namespace Penjualan.Tests
{
    public class AngsuranCalculatorTests
    {
        private const string NoTransaksi = "W-26-000123";

        [Fact]
        public void SingleTenor_ProducesOnePaidOffRow()
        {
            var rows = AngsuranCalculator.Calculate(NoTransaksi, new DateTime(2026, 6, 10), 100_000m, 1);

            var row = Assert.Single(rows);
            Assert.Equal(1, row.ANGSURANKE);
            Assert.Equal(202606, row.PERIODE);
            Assert.Equal(new DateTime(2026, 6, 30), row.TANGGALJATUHTEMPO); // due = last day of month
            Assert.Equal(100_000m, row.SALDOAWAL);
            Assert.Equal(100_000m, row.ANGSURAN);
            Assert.Equal(0m, row.SALDOAKHIR);
            Assert.Equal(NoTransaksi, row.NO_TRANSAKSI);
        }

        [Fact]
        public void EvenSplit_AllInstallmentsEqual()
        {
            var rows = AngsuranCalculator.Calculate(NoTransaksi, new DateTime(2026, 6, 10), 400_000m, 4);

            Assert.Equal(4, rows.Count);
            Assert.All(rows, r => Assert.Equal(100_000m, r.ANGSURAN));
            Assert.Equal(400_000m, rows.Sum(r => r.ANGSURAN));
            Assert.Equal(0m, rows[^1].SALDOAKHIR);
        }

        [Fact]
        public void NonDivisible_LastInstallmentAbsorbsRemainder()
        {
            // 100_000 / 3 -> floor = 33_333 per month; last row takes the leftover.
            var rows = AngsuranCalculator.Calculate(NoTransaksi, new DateTime(2026, 6, 10), 100_000m, 3);

            Assert.Equal(3, rows.Count);
            Assert.Equal(33_333m, rows[0].ANGSURAN);
            Assert.Equal(33_333m, rows[1].ANGSURAN);
            Assert.Equal(33_334m, rows[2].ANGSURAN);           // remainder absorbed here
            Assert.Equal(100_000m, rows.Sum(r => r.ANGSURAN)); // nothing lost to rounding
            Assert.Equal(0m, rows[^1].SALDOAKHIR);
        }

        [Fact]
        public void DueDates_RollOverMonthAndYear()
        {
            var rows = AngsuranCalculator.Calculate(NoTransaksi, new DateTime(2026, 11, 15), 300_000m, 3);

            Assert.Equal(new DateTime(2026, 11, 30), rows[0].TANGGALJATUHTEMPO);
            Assert.Equal(new DateTime(2026, 12, 31), rows[1].TANGGALJATUHTEMPO);
            Assert.Equal(new DateTime(2027, 1, 31), rows[2].TANGGALJATUHTEMPO); // crosses into next year

            Assert.Equal(202611, rows[0].PERIODE);
            Assert.Equal(202612, rows[1].PERIODE);
            Assert.Equal(202701, rows[2].PERIODE);
        }

        [Fact]
        public void DueDate_ClampsToShortMonth()
        {
            // Buying on Jan 31 — February has no 31st; due date must clamp to Feb 28.
            var rows = AngsuranCalculator.Calculate(NoTransaksi, new DateTime(2026, 1, 31), 200_000m, 2);

            Assert.Equal(new DateTime(2026, 1, 31), rows[0].TANGGALJATUHTEMPO);
            Assert.Equal(new DateTime(2026, 2, 28), rows[1].TANGGALJATUHTEMPO);
        }

        [Fact]
        public void SaldoChain_IsContinuousAndStartsAtPrincipal()
        {
            var rows = AngsuranCalculator.Calculate(NoTransaksi, new DateTime(2026, 6, 10), 100_000m, 3);

            Assert.Equal(100_000m, rows[0].SALDOAWAL); // opening balance = principal
            for (int i = 0; i < rows.Count - 1; i++)
            {
                // Each row's closing balance feeds the next row's opening balance.
                Assert.Equal(rows[i].SALDOAKHIR, rows[i + 1].SALDOAWAL);
                Assert.Equal(rows[i].SALDOAWAL - rows[i].ANGSURAN, rows[i].SALDOAKHIR);
            }
            Assert.Equal(0m, rows[^1].SALDOAKHIR); // fully paid off
        }

        [Theory]
        [InlineData(100_000, 1)]
        [InlineData(100_000, 3)]
        [InlineData(250_000, 6)]
        [InlineData(1_000_000, 12)]
        [InlineData(99_999, 7)]
        [InlineData(123_457, 11)]
        public void InstallmentsAlwaysSumToPrincipal(int jumlah, int tenor)
        {
            var rows = AngsuranCalculator.Calculate(NoTransaksi, new DateTime(2026, 6, 10), jumlah, tenor);

            Assert.Equal(tenor, rows.Count);
            Assert.Equal((decimal)jumlah, rows.Sum(r => r.ANGSURAN)); // no money created or lost
            Assert.Equal(0m, rows[^1].SALDOAKHIR);

            // ANGSURANKE runs 1..tenor in order, and NO_TRANSAKSI is stamped on every row.
            for (int i = 0; i < rows.Count; i++)
            {
                Assert.Equal(i + 1, rows[i].ANGSURANKE);
                Assert.Equal(NoTransaksi, rows[i].NO_TRANSAKSI);
            }
        }
    }
}
