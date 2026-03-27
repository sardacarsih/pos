using Penjualan.DataLayer;
using Penjualan.Interface;
using Penjualan.Model;

namespace Penjualan.BusinessLayer
{
    public class POS_Services
    {
        static readonly IFakturPenjualan repository;
        static POS_Services()
        {
            repository = new FakturPenjualan();
        }

        public static void InsertFaktur_Penjualan(DTOFakturPenjualanHeader faktur_header, List<DTOFakturPenjualanDetail> ListItemsPenjualan, CreditLimitCheck? creditCheck = null, string? pendingNoTransaksi = null)
        {
            repository.InsertFaktur_Penjualan(faktur_header, ListItemsPenjualan, creditCheck, pendingNoTransaksi);
        }
        public static void InsertFaktur_Penjualan_Angsuran(DTOFakturPenjualanHeader faktur_header, List<DTOFakturPenjualanDetail> ListItemsPenjualan, List<DTOAngsuranKreditBarang> DaftarWaktuTagihan, CreditLimitCheck? creditCheck = null, string? pendingNoTransaksi = null)
        {
            repository.InsertFaktur_Penjualan_Angsuran(faktur_header, ListItemsPenjualan, DaftarWaktuTagihan, creditCheck, pendingNoTransaksi);
        }
        public static string GenerateTransactionNumber(DateTime date)
        {
            return repository.GenerateTransactionNumber(date);
        }
        public static void UpdateFakturPenjualan(DTOFakturPenjualanHeader faktur_header)
        {
            repository.UpdateFakturPenjualan(faktur_header);
        }
        public static DTOProductInfo RetrieveProductInfo(string barcode)
        {
            return repository.RetrieveProductInfo(barcode);
        }
        public static decimal GetStocItem(string kodeBarang, DateTime startDate, DateTime endDate)
        {
            return repository.GetStocItem(kodeBarang, startDate, endDate); 
        }
        public static bool GetSettingKontrol_qty_Saldo()
        {
            return repository.GetSettingKontrol_qty_Saldo();
        }
        public static DTOPeriodeDates? GetTanggalByPeriode(int periode)
        {
            return repository.GetTanggalByPeriode(periode);
        }
        public static DTOPotonganHarga? GetPotonganByKodeItem(string kodeItem)
        {
            return repository.GetPotonganByKodeItem(kodeItem);
        }
        public static List<DTOPelanggan> GetPelangganAktif()
        {
            return repository.GetPelangganAktif();
        }
        public static decimal CheckingJumlahHutang(string nik, DateTime dari, DateTime sampai)
        {
            return repository.CheckingJumlahHutang(nik, dari, sampai);
        }
    }
}
