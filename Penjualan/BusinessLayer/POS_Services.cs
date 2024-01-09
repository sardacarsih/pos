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

        public static void InsertFaktur_Penjualan(DTOFakturPenjualanHeader faktur_header, List<DTOFakturPenjualanDetail> ListItemsPenjualan)
        {
            repository.InsertFaktur_Penjualan(faktur_header, ListItemsPenjualan);
        }
        public static void InsertFaktur_Penjualan_Angsuran(DTOFakturPenjualanHeader faktur_header, List<DTOFakturPenjualanDetail> ListItemsPenjualan, List<DTOAngsuranKreditBarang> DaftarWaktuTagihan)
        {
            repository.InsertFaktur_Penjualan_Angsuran(faktur_header, ListItemsPenjualan, DaftarWaktuTagihan);
        }
        public static void UpdateTransactionNumber(string transactionNumber)
        {
            repository.UpdateTransactionNumber(transactionNumber);
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
    }
}
