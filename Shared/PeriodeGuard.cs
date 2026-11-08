using Oracle.ManagedDataAccess.Client;

namespace Kopkar.Shared
{
    public enum PeriodeKode { Terbuka, Tutup, TidakAda, TidakValid }

    public sealed record PeriodeCek(PeriodeKode Kode, string Pesan)
    {
        public bool Terbuka => Kode == PeriodeKode.Terbuka;

        public string Judul => Kode switch
        {
            PeriodeKode.Terbuka => "Periode terbuka",
            PeriodeKode.Tutup => "Periode tertutup",
            PeriodeKode.TidakAda => "Periode belum diatur",
            _ => "Periode belum valid",
        };
    }

    /// <summary>
    /// Periode penjualan harus terbuka: tanggal berada dalam rentang bulanan (BDARI..BSAMPAI) sebuah baris
    /// POS_PERIODE, flag remise untuk tanggal itu bukan 'Y', dan PERIODE &lt;= MAX(PERIODE). Fail closed.
    /// Dipakai bersama Penjualan dan BackOffice (linked source, lihat Shared/); koneksi dilempar dari pemanggil.
    /// Aturan yang sama dengan rust-api (db/faktur.rs cek_periode). Limit hutang bergantung pada periode,
    /// jadi periode bermasalah memblokir transaksi.
    /// </summary>
    public static partial class PeriodeGuard
    {
        public const string HubungiAdmin = "Hubungi admin untuk mengatur periode.";

        private const string Tgl = "'DD-MON-YYYY', 'NLS_DATE_LANGUAGE = ENGLISH'";

        /// <summary>Remise: dalam R1 -> 1, dalam R2 -> 2, selain itu aturan lama (tanggal &lt;= 15 -> 1).</summary>
        public static int PilihRemise(DateTime tanggal, DateTime? r1Dari, DateTime? r1Sampai, DateTime? r2Dari, DateTime? r2Sampai)
        {
            static bool Dalam(DateTime t, DateTime? a, DateTime? b) => a.HasValue && b.HasValue && t.Date >= a.Value.Date && t.Date <= b.Value.Date;
            if (Dalam(tanggal, r1Dari, r1Sampai)) return 1;
            if (Dalam(tanggal, r2Dari, r2Sampai)) return 2;
            return tanggal.Day <= 15 ? 1 : 2;
        }

        public static PeriodeCek Evaluasi(int periode, string? flagTutup, int? maxPeriode)
        {
            if (string.Equals(flagTutup?.Trim(), "Y", StringComparison.OrdinalIgnoreCase))
                return new PeriodeCek(PeriodeKode.Tutup, $"Periode {periode} telah ditutup.");
            if (maxPeriode.HasValue && periode > maxPeriode.Value)
                return new PeriodeCek(PeriodeKode.TidakValid, "Input transaksi dibatasi pada periode yang valid.");
            return new PeriodeCek(PeriodeKode.Terbuka, string.Empty);
        }

        public static PeriodeCek Cek(OracleConnection conn, OracleTransaction? transaction, DateTime tanggal)
        {
            string sql = $@"SELECT PERIODE, REMISE1, REMISE2,
                                   TO_DATE(R1DARI, {Tgl}), TO_DATE(R1SAMPAI, {Tgl}),
                                   TO_DATE(R2DARI, {Tgl}), TO_DATE(R2SAMPAI, {Tgl})
                            FROM POS_PERIODE
                            WHERE :tgl BETWEEN TO_DATE(BDARI, {Tgl}) AND TO_DATE(BSAMPAI, {Tgl})
                            ORDER BY PERIODE DESC FETCH FIRST 1 ROWS ONLY";
            int periode;
            string? tutup;
            using (var cmd = new OracleCommand(sql, conn))
            {
                if (transaction != null) cmd.Transaction = transaction;
                cmd.Parameters.Add("tgl", OracleDbType.Date).Value = tanggal.Date;
                using var r = cmd.ExecuteReader();
                if (!r.Read())
                    return new PeriodeCek(PeriodeKode.TidakAda, $"Tanggal {tanggal:yyyy-MM-dd} di luar rentang periode yang dikonfigurasi.");

                DateTime? D(int i) => r.IsDBNull(i) ? null : r.GetDateTime(i);
                periode = r.GetInt32(0);
                int remise = PilihRemise(tanggal, D(3), D(4), D(5), D(6));
                int kolom = remise == 1 ? 1 : 2;
                tutup = r.IsDBNull(kolom) ? null : r.GetString(kolom);
            }

            int? max;
            using (var cmd = new OracleCommand("SELECT MAX(PERIODE) FROM POS_PERIODE", conn))
            {
                if (transaction != null) cmd.Transaction = transaction;
                object? o = cmd.ExecuteScalar();
                max = o == null || o == DBNull.Value ? null : Convert.ToInt32(o);
            }
            return Evaluasi(periode, tutup, max);
        }

        /// <summary>
        /// Periode yang rentang bulanannya (BDARI..BSAMPAI) memuat tanggal (terbesar bila tumpang tindih); null bila tidak ada.
        /// Dasar pencarian periode untuk status buka/tutup maupun window limit hutang — bukan bulan kalender.
        /// </summary>
        public static int? PeriodeUntukTanggal(OracleConnection conn, OracleTransaction? transaction, DateTime tanggal)
        {
            string sql = $@"SELECT PERIODE FROM POS_PERIODE
                            WHERE :tgl BETWEEN TO_DATE(BDARI, {Tgl}) AND TO_DATE(BSAMPAI, {Tgl})
                            ORDER BY PERIODE DESC FETCH FIRST 1 ROWS ONLY";
            using var cmd = new OracleCommand(sql, conn);
            if (transaction != null) cmd.Transaction = transaction;
            cmd.Parameters.Add("tgl", OracleDbType.Date).Value = tanggal.Date;
            object? o = cmd.ExecuteScalar();
            return o == null || o == DBNull.Value ? null : Convert.ToInt32(o);
        }

        /// <summary>Lempar exception bila periode tidak terbuka (dipakai saat simpan faktur, dalam transaksi yang sama).</summary>
        public static void Pastikan(OracleConnection conn, OracleTransaction? transaction, DateTime tanggal)
        {
            var cek = Cek(conn, transaction, tanggal);
            if (!cek.Terbuka)
                throw new InvalidOperationException($"{cek.Judul}: {cek.Pesan} Transaksi tidak dapat dilakukan. {HubungiAdmin}");
        }
    }
}
