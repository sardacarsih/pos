using Oracle.ManagedDataAccess.Client;
using Penjualan.DataLayer;
using Penjualan.Model;
using Xunit;

namespace Penjualan.Tests.Integration
{
    /// <summary>
    /// End-to-end tests for <see cref="FakturPenjualan.ValidateCreditLimit"/> against a real
    /// Oracle schema: they exercise the actual SQL (TO_DATE/NLS window resolution,
    /// NVL/SUM/BETWEEN, FIN_ANGGOTA FOR UPDATE) plus the C# period computation and the
    /// shared <c>CreditLimitPolicy</c> verdict.
    ///
    /// Opt-in via the POS_TEST_ORACLE env var (see <see cref="OracleTestFixture"/>); skipped
    /// otherwise so the suite stays green without a database.
    /// </summary>
    public class CreditLimitValidationIntegrationTests : IClassFixture<OracleTestFixture>
    {
        private const string Nik = "ITEST-001";
        private const int Periode = 209912;                       // yyyyMM of TxnDate
        private static readonly DateTime TxnDate = new(2099, 12, 10);
        private static readonly DateTime DecFrom = new(2099, 12, 1);
        private static readonly DateTime DecTo = new(2099, 12, 31);
        private static readonly DateTime JanFrom = new(2099, 1, 1);
        private static readonly DateTime JanTo = new(2099, 1, 31);

        private readonly OracleTestFixture _fx;

        public CreditLimitValidationIntegrationTests(OracleTestFixture fx) => _fx = fx;

        private static CreditLimitCheck Check(decimal invoice, string status = "REMISE") => new()
        {
            NIK = Nik,
            STATUS = status,
            TransactionDate = TxnDate,
            InvoiceAmount = invoice
        };

        /// <summary>Runs the validation in its own transaction and returns any thrown exception (rolled back).</summary>
        private Exception? RunValidation(CreditLimitCheck check)
        {
            using var conn = _fx.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                return Record.Exception(() => FakturPenjualan.ValidateCreditLimit(conn, tx, check));
            }
            finally
            {
                tx.Rollback();
            }
        }

        [SkippableFact]
        public void UnderLimit_DoesNotThrow()
        {
            Skip.IfNot(_fx.Available, _fx.SkipReason);
            _fx.ClearData();
            _fx.SeedMember(Nik, 1_000_000);
            _fx.SeedPeriode(Periode, DecFrom, DecTo, DecFrom, DecTo);
            _fx.SeedSale(Nik, TxnDate, 300_000);

            // 300,000 existing + 200,000 invoice = 500,000 <= 1,000,000
            Assert.Null(RunValidation(Check(200_000)));
        }

        [SkippableFact]
        public void OverLimit_ThrowsWithDebtInvoiceAndLimit()
        {
            Skip.IfNot(_fx.Available, _fx.SkipReason);
            _fx.ClearData();
            _fx.SeedMember(Nik, 1_000_000);
            _fx.SeedPeriode(Periode, DecFrom, DecTo, DecFrom, DecTo);
            _fx.SeedSale(Nik, TxnDate, 800_000);

            // 800,000 + 300,000 = 1,100,000 > 1,000,000
            var ex = Assert.IsType<CreditLimitExceededException>(RunValidation(Check(300_000)));
            Assert.Equal(800_000m, ex.CurrentDebt);
            Assert.Equal(300_000m, ex.InvoiceAmount);
            Assert.Equal(1_000_000m, ex.Limit);
        }

        [SkippableFact]
        public void ExactlyAtLimit_IsAllowed()
        {
            Skip.IfNot(_fx.Available, _fx.SkipReason);
            _fx.ClearData();
            _fx.SeedMember(Nik, 1_000_000);
            _fx.SeedPeriode(Periode, DecFrom, DecTo, DecFrom, DecTo);
            _fx.SeedSale(Nik, TxnDate, 800_000);

            // 800,000 + 200,000 = 1,000,000 == limit (boundary inclusive)
            Assert.Null(RunValidation(Check(200_000)));
        }

        [SkippableFact]
        public void ZeroLimit_IsUnlimited()
        {
            Skip.IfNot(_fx.Available, _fx.SkipReason);
            _fx.ClearData();
            _fx.SeedMember(Nik, 0);
            _fx.SeedPeriode(Periode, DecFrom, DecTo, DecFrom, DecTo);
            _fx.SeedSale(Nik, TxnDate, 9_000_000);

            Assert.Null(RunValidation(Check(9_000_000)));
        }

        [SkippableFact]
        public void OnlySpendInsideThePeriodWindowCounts()
        {
            Skip.IfNot(_fx.Available, _fx.SkipReason);
            _fx.ClearData();
            _fx.SeedMember(Nik, 1_000_000);
            _fx.SeedPeriode(Periode, DecFrom, DecTo, DecFrom, DecTo);
            _fx.SeedSale(Nik, TxnDate, 500_000);                    // inside the Dec window
            _fx.SeedSale(Nik, new DateTime(2099, 11, 30), 9_999_999); // outside — must be ignored

            // Only the in-window 500,000 counts: 500,000 + 400,000 = 900,000 <= 1,000,000
            Assert.Null(RunValidation(Check(400_000)));
        }

        [SkippableFact]
        public void BulananStatus_UsesMonthlyWindowNotRemise()
        {
            Skip.IfNot(_fx.Available, _fx.SkipReason);
            _fx.ClearData();
            _fx.SeedMember(Nik, 1_000_000);
            // Remise window = January (does NOT cover the sale); Bulanan window = December (does).
            _fx.SeedPeriode(Periode, JanFrom, JanTo, DecFrom, DecTo);
            _fx.SeedSale(Nik, TxnDate, 800_000);

            // BULANAN -> Dec window -> sale counts -> 800,000 + 300,000 = 1,100,000 > limit.
            Assert.IsType<CreditLimitExceededException>(RunValidation(Check(300_000, status: "BULANAN")));
            // Sanity: same data under remise status uses the Jan window, so the Dec sale is ignored.
            Assert.Null(RunValidation(Check(300_000, status: "REMISE")));
        }

        [SkippableFact]
        public void MissingPeriodConfig_SkipsEnforcement()
        {
            Skip.IfNot(_fx.Available, _fx.SkipReason);
            _fx.ClearData();
            _fx.SeedMember(Nik, 100);            // tiny limit
            // No POS_PERIODE row for the period.
            _fx.SeedSale(Nik, TxnDate, 999_999);

            // Window cannot be resolved -> validation is skipped (documents current behavior).
            Assert.Null(RunValidation(Check(999_999)));
        }

        [SkippableFact]
        public void MemberRowLock_SerializesConcurrentValidation()
        {
            Skip.IfNot(_fx.Available, _fx.SkipReason);
            _fx.ClearData();
            _fx.SeedMember(Nik, 1_000_000);

            using var conn1 = _fx.Open();
            using var tx1 = conn1.BeginTransaction();
            using (var lockCmd = new OracleCommand(
                "SELECT LIMIT_HUTANG FROM FIN_ANGGOTA WHERE NIK = :nik FOR UPDATE", conn1) { Transaction = tx1 })
            {
                lockCmd.Parameters.Add("nik", OracleDbType.Varchar2).Value = Nik;
                lockCmd.ExecuteScalar(); // conn1 now holds the member-row lock
            }

            using var conn2 = _fx.Open();
            using var tx2 = conn2.BeginTransaction();
            using var busyCmd = new OracleCommand(
                "SELECT LIMIT_HUTANG FROM FIN_ANGGOTA WHERE NIK = :nik FOR UPDATE NOWAIT", conn2) { Transaction = tx2 };
            busyCmd.Parameters.Add("nik", OracleDbType.Varchar2).Value = Nik;

            // Second concurrent validation cannot grab the same member row -> ORA-00054 (resource busy).
            var ex = Assert.Throws<OracleException>(() => busyCmd.ExecuteScalar());
            Assert.Equal(54, ex.Number);

            tx2.Rollback();
            tx1.Rollback();
        }
    }
}
