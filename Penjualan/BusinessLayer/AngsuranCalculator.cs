using Penjualan.Model;

namespace Penjualan.BusinessLayer
{
    /// <summary>
    /// Canonical credit-installment (angsuran) schedule generator shared by the
    /// payment and installment sales screens. Splits the amount evenly with the last
    /// installment absorbing any rounding remainder, and sets PERIODE (yyyyMM of the
    /// due date) so the schedule is collectible per period.
    /// </summary>
    public static class AngsuranCalculator
    {
        public static List<DTOAngsuranKreditBarang> Calculate(string nomortransaksi, DateTime tanggalBelanja, decimal jumlahBelanja, int waktuangsuran)
        {
            List<DTOAngsuranKreditBarang> listAngsuran = new();
            decimal saldoAwal = jumlahBelanja;
            decimal P = Math.Floor(saldoAwal / waktuangsuran);

            for (int i = 1; i <= waktuangsuran; i++)
            {
                DateTime bulanBerikutnya = tanggalBelanja.AddMonths(i - 1);
                DateTime tanggalJatuhTempo = new(bulanBerikutnya.Year, bulanBerikutnya.Month, DateTime.DaysInMonth(bulanBerikutnya.Year, bulanBerikutnya.Month));
                int p_periode = int.Parse(tanggalJatuhTempo.ToString("yyyyMM"));

                decimal angsuranBulanIni = (i == waktuangsuran) ? saldoAwal : P;
                decimal saldoAkhir = saldoAwal - angsuranBulanIni;

                DTOAngsuranKreditBarang angsuran = new()
                {
                    PERIODE = p_periode,
                    NO_TRANSAKSI = nomortransaksi,
                    TANGGALJATUHTEMPO = tanggalJatuhTempo,
                    ANGSURANKE = i,
                    SALDOAWAL = Math.Round(saldoAwal, 2),
                    ANGSURAN = Math.Round(angsuranBulanIni, 2),
                    SALDOAKHIR = Math.Round(saldoAkhir, 2)
                };

                listAngsuran.Add(angsuran);
                saldoAwal = saldoAkhir;
            }

            return listAngsuran;
        }
    }
}
