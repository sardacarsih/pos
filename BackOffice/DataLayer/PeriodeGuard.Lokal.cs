using Oracle.ManagedDataAccess.Client;

namespace Kopkar.Shared
{
    // Bagian PeriodeGuard yang memakai koneksi aplikasi ini (BackOffice global); sisanya di Shared/PeriodeGuard.cs.
    public static partial class PeriodeGuard
    {
        public static PeriodeCek Cek(DateTime tanggal)
        {
            using OracleConnection conn = new(BackOffice.DataLayer.global.connectionString);
            conn.Open();
            return Cek(conn, null, tanggal);
        }

        public static int? PeriodeUntukTanggal(DateTime tanggal)
        {
            using OracleConnection conn = new(BackOffice.DataLayer.global.connectionString);
            conn.Open();
            return PeriodeUntukTanggal(conn, null, tanggal);
        }
    }
}
