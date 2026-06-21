using System.Security.Cryptography;
using System.Text;
using Dapper;
using Oracle.ManagedDataAccess.Client;

namespace Migrator
{
    /// <summary>
    /// Menjalankan migrasi &amp; mencatatnya di tabel POS_SCHEMA_MIGRATION.
    /// Setiap migrasi hanya diterapkan sekali (berdasarkan MIGRATION_ID).
    /// </summary>
    public sealed class MigrationRunner
    {
        private readonly string _connectionString;

        public MigrationRunner(string connectionString) => _connectionString = connectionString;

        private OracleConnection Open()
        {
            var con = new OracleConnection(_connectionString);
            con.Open();
            return con;
        }

        public void EnsureTrackingTable()
        {
            using var con = Open();
            con.Execute(
                "BEGIN\n" +
                "  EXECUTE IMMEDIATE 'CREATE TABLE POS_SCHEMA_MIGRATION (" +
                "MIGRATION_ID VARCHAR2(100) CONSTRAINT PK_POS_SCHEMA_MIGRATION PRIMARY KEY, " +
                "CHECKSUM VARCHAR2(64), " +
                "APPLIED_AT TIMESTAMP DEFAULT SYSTIMESTAMP NOT NULL)';\n" +
                "EXCEPTION WHEN OTHERS THEN IF SQLCODE != -955 THEN RAISE; END IF;\n" +
                "END;");
        }

        private Dictionary<string, string?> GetApplied()
        {
            using var con = Open();
            return con.Query<AppliedRow>(
                    "SELECT MIGRATION_ID, CHECKSUM FROM POS_SCHEMA_MIGRATION")
                .ToDictionary(r => r.MIGRATION_ID, r => r.CHECKSUM);
        }

        /// <summary>Terapkan migrasi yang belum tercatat. Return jumlah migrasi baru.</summary>
        public int Apply(IEnumerable<Migration> migrations)
        {
            EnsureTrackingTable();
            Dictionary<string, string?> applied = GetApplied();
            int count = 0;

            foreach (Migration m in migrations)
            {
                string checksum = Checksum(m);

                if (applied.TryGetValue(m.Id, out string? existing))
                {
                    if (existing is not null && existing != checksum)
                    {
                        Console.WriteLine($"  [WARN ] {m.Id} sudah diterapkan, tapi definisi berubah (checksum beda).");
                    }
                    else
                    {
                        Console.WriteLine($"  [skip ] {m.Id}");
                    }
                    continue;
                }

                Console.WriteLine($"  [apply] {m.Id} - {m.Description}");
                using (OracleConnection con = Open())
                {
                    foreach (string stmt in m.Statements)
                    {
                        using OracleCommand cmd = con.CreateCommand();
                        cmd.CommandText = stmt;
                        cmd.ExecuteNonQuery();
                    }
                    con.Execute(
                        "INSERT INTO POS_SCHEMA_MIGRATION (MIGRATION_ID, CHECKSUM) VALUES (:id, :cs)",
                        new { id = m.Id, cs = checksum });
                }
                count++;
            }

            return count;
        }

        private static string Checksum(Migration m)
        {
            var sb = new StringBuilder(m.Id);
            foreach (string s in m.Statements)
            {
                sb.Append('\n').Append(s);
            }
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
            return Convert.ToHexString(hash);
        }

        private sealed class AppliedRow
        {
            public string MIGRATION_ID { get; set; } = string.Empty;
            public string? CHECKSUM { get; set; }
        }
    }
}
