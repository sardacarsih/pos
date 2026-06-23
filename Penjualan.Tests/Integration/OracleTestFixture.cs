using System.Globalization;
using Oracle.ManagedDataAccess.Client;

namespace Penjualan.Tests.Integration
{
    /// <summary>
    /// Shared fixture for the credit-limit DB integration tests. It is opt-in: tests run
    /// only when the <c>POS_TEST_ORACLE</c> environment variable points at a *disposable*
    /// Oracle schema. Otherwise <see cref="Available"/> is false and the tests skip.
    ///
    /// The fixture creates the minimal tables the credit-limit SQL touches
    /// (FIN_ANGGOTA, POS_PERIODE, POS_PENJUALAN). To avoid clobbering a real database it
    /// refuses to run if any of those tables already exist, and on dispose it drops only
    /// the tables it created.
    /// </summary>
    public sealed class OracleTestFixture : IDisposable
    {
        private static readonly string[] ManagedTables = { "POS_PENJUALAN", "POS_PERIODE", "FIN_ANGGOTA" };

        public string? ConnectionString { get; }
        public bool Available { get; }
        public string SkipReason { get; } = string.Empty;

        public OracleTestFixture()
        {
            ConnectionString = Environment.GetEnvironmentVariable("POS_TEST_ORACLE");
            if (string.IsNullOrWhiteSpace(ConnectionString))
            {
                SkipReason = "Set POS_TEST_ORACLE to a disposable Oracle schema to run credit-limit DB integration tests.";
                return;
            }

            try
            {
                using var conn = Open();
                foreach (var table in ManagedTables)
                {
                    if (TableExists(conn, table))
                        throw new InvalidOperationException(
                            $"Table {table} already exists — point POS_TEST_ORACLE at an empty/throwaway schema.");
                }
                CreateSchema(conn);
                Available = true;
            }
            catch (Exception ex)
            {
                Available = false;
                SkipReason = "Oracle test schema unavailable or not clean: " + ex.Message;
            }
        }

        public OracleConnection Open()
        {
            var conn = new OracleConnection(ConnectionString);
            conn.Open();
            return conn;
        }

        /// <summary>Removes all seeded rows so each test starts from a clean slate.</summary>
        public void ClearData()
        {
            using var conn = Open();
            foreach (var table in ManagedTables)
                Exec(conn, $"DELETE FROM {table}");
        }

        public void SeedMember(string nik, decimal limit)
        {
            using var conn = Open();
            Exec(conn, "INSERT INTO FIN_ANGGOTA (NIK, LIMIT_HUTANG) VALUES (:nik, :lim)",
                ("nik", nik), ("lim", limit));
        }

        /// <summary>Seeds a POS_PERIODE row. Dates are stored as 'DD-MON-YYYY' strings, as in production.</summary>
        public void SeedPeriode(int periode, DateTime remiseFrom, DateTime remiseTo, DateTime bulananFrom, DateTime bulananTo)
        {
            using var conn = Open();
            Exec(conn,
                @"INSERT INTO POS_PERIODE (PERIODE, R1DARI, R1SAMPAI, R2DARI, R2SAMPAI, BDARI, BSAMPAI)
                  VALUES (:p, :r1d, :r1s, :r2d, :r2s, :bd, :bs)",
                ("p", periode),
                ("r1d", Fmt(remiseFrom)), ("r1s", Fmt(remiseTo)),
                ("r2d", Fmt(remiseFrom)), ("r2s", Fmt(remiseTo)),
                ("bd", Fmt(bulananFrom)), ("bs", Fmt(bulananTo)));
        }

        public void SeedSale(string nik, DateTime tanggal, decimal total)
        {
            using var conn = Open();
            using var cmd = new OracleCommand(
                "INSERT INTO POS_PENJUALAN (NIK, TANGGAL, TOTAL) VALUES (:nik, :tgl, :total)", conn);
            cmd.Parameters.Add("nik", OracleDbType.Varchar2).Value = nik;
            cmd.Parameters.Add("tgl", OracleDbType.Date).Value = tanggal;
            cmd.Parameters.Add("total", OracleDbType.Decimal).Value = total;
            cmd.ExecuteNonQuery();
        }

        private static string Fmt(DateTime d) =>
            d.ToString("dd-MMM-yyyy", CultureInfo.InvariantCulture).ToUpperInvariant();

        private void CreateSchema(OracleConnection conn)
        {
            Exec(conn, "CREATE TABLE FIN_ANGGOTA (NIK VARCHAR2(20) PRIMARY KEY, LIMIT_HUTANG NUMBER)");
            Exec(conn, @"CREATE TABLE POS_PERIODE (
                PERIODE NUMBER PRIMARY KEY,
                R1DARI VARCHAR2(20), R1SAMPAI VARCHAR2(20),
                R2DARI VARCHAR2(20), R2SAMPAI VARCHAR2(20),
                BDARI VARCHAR2(20), BSAMPAI VARCHAR2(20))");
            Exec(conn, "CREATE TABLE POS_PENJUALAN (NIK VARCHAR2(20), TANGGAL DATE, TOTAL NUMBER)");
        }

        private static bool TableExists(OracleConnection conn, string table)
        {
            using var cmd = new OracleCommand(
                "SELECT COUNT(*) FROM USER_TABLES WHERE TABLE_NAME = :t", conn);
            cmd.Parameters.Add("t", OracleDbType.Varchar2).Value = table;
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        private static void Exec(OracleConnection conn, string sql, params (string Name, object Value)[] args)
        {
            using var cmd = new OracleCommand(sql, conn);
            foreach (var (name, value) in args)
                cmd.Parameters.Add(name, value);
            cmd.ExecuteNonQuery();
        }

        public void Dispose()
        {
            if (string.IsNullOrWhiteSpace(ConnectionString) || !Available)
                return;

            try
            {
                using var conn = Open();
                foreach (var table in ManagedTables) // already in drop-safe order
                {
                    try { Exec(conn, $"DROP TABLE {table} CASCADE CONSTRAINTS"); }
                    catch { /* best-effort cleanup */ }
                }
            }
            catch { /* ignore */ }
        }
    }
}
