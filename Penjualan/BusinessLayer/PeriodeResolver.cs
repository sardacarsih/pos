using Penjualan.Model;

namespace Penjualan.BusinessLayer
{
    /// <summary>
    /// Pure period lookup over POS_PERIODE rows (no DB/UI, unit tested). A date belongs to the
    /// row whose configured range covers it — never to its calendar month — so non-calendar
    /// periods (e.g. 26th..25th) resolve correctly.
    /// </summary>
    public static class PeriodeResolver
    {
        private static bool Covers(DateTime? dari, DateTime? sampai, DateTime tgl) =>
            dari.HasValue && sampai.HasValue && tgl >= dari.Value.Date && tgl <= sampai.Value.Date;

        private static bool IsBulanan(string? status) =>
            string.Equals(status?.Trim(), "BULANAN", StringComparison.OrdinalIgnoreCase);

        /// <summary>Row whose Remise I or II range covers <paramref name="tgl"/>; segmen = 1 or 2 (0 if none).</summary>
        public static PeriodeRow? FindRemise(IEnumerable<PeriodeRow> rows, DateTime tgl, out int segmen)
        {
            var d = tgl.Date;
            foreach (var r in rows)
            {
                if (Covers(r.R1Dari, r.R1Sampai, d)) { segmen = 1; return r; }
                if (Covers(r.R2Dari, r.R2Sampai, d)) { segmen = 2; return r; }
            }
            segmen = 0;
            return null;
        }

        /// <summary>Row whose bulanan range covers <paramref name="tgl"/>.</summary>
        public static PeriodeRow? FindBulanan(IEnumerable<PeriodeRow> rows, DateTime tgl)
        {
            var d = tgl.Date;
            return rows.FirstOrDefault(r => Covers(r.BDari, r.BSampai, d));
        }

        /// <summary>
        /// Credit-limit window for a member: BULANAN → BDari..BSampai, otherwise the full remise
        /// span R1Dari..R2Sampai of the row covering the date. Null when not configured.
        /// </summary>
        public static (DateTime Dari, DateTime Sampai)? CreditWindow(IEnumerable<PeriodeRow> rows, DateTime tgl, string? status)
        {
            if (IsBulanan(status))
            {
                var b = FindBulanan(rows, tgl);
                return b == null ? null : (b.BDari!.Value.Date, b.BSampai!.Value.Date);
            }

            var r = FindRemise(rows, tgl, out _);
            if (r?.R1Dari == null || r.R2Sampai == null) return null;
            return (r.R1Dari.Value.Date, r.R2Sampai.Value.Date);
        }

        /// <summary>
        /// Null when a transaction on <paramref name="tgl"/> is allowed; otherwise the rejection
        /// reason. A null <paramref name="status"/> (no customer chosen yet) checks remise only.
        /// </summary>
        public static string? AlasanTolak(IEnumerable<PeriodeRow> rows, DateTime tgl, string? status)
        {
            var list = rows as IList<PeriodeRow> ?? rows.ToList();
            string tglStr = tgl.ToString("dd-MM-yyyy");

            var remise = FindRemise(list, tgl, out int segmen);
            if (remise == null)
                return $"Tanggal {tglStr} belum masuk periode manapun di Setting Periode. Hubungi admin.";
            if ((segmen == 1 && remise.Remise1Tutup) || (segmen == 2 && remise.Remise2Tutup))
                return $"Remise {segmen} periode {remise.Periode} sudah ditutup, transaksi tidak dapat dilakukan.";

            if (IsBulanan(status))
            {
                var bulanan = FindBulanan(list, tgl);
                if (bulanan == null)
                    return $"Tanggal {tglStr} belum masuk periode bulanan di Setting Periode. Hubungi admin.";
                if (bulanan.BulananTutup)
                    return $"Periode bulanan {bulanan.Periode} sudah ditutup, transaksi tidak dapat dilakukan.";
            }

            return null;
        }
    }
}
