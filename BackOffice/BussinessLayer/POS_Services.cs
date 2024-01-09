using BackOffice.DataLayer;
using BackOffice.Interface;
using BackOffice.Model;
using System.Data;

namespace BackOffice.BussinessLayer
{
    public class POS_Services
    {
        static readonly IFakturPenjualan repository;
        static POS_Services()
        {
            repository = new FakturPenjualan();
        }

        public static DataSet Tagihan_Periode(int p_indextagihan, DateTime p_tglAngsuran, DateTime p_daritanggal, DateTime p_sampaitanggal)
        {
            return repository.Tagihan_Periode(p_indextagihan, p_tglAngsuran, p_daritanggal, p_sampaitanggal);
        }
        public static void Tutup_Buku(int p_periode, int p_remise, DateTime p_tglAngsuran, DateTime p_daritanggal, DateTime p_sampaitanggal)
        {
             repository.Tutup_Buku(p_periode,p_remise, p_tglAngsuran, p_daritanggal,p_sampaitanggal);
        }

        public static DataSet Tagihan_ALL(int p_periode, int p_remise)
        {
            return repository.Tagihan_ALL( p_periode,  p_remise);
        }
        public static DataSet Penjualan_Tunai(int p_periode, int p_remise)
        {
            return repository.Penjualan_Tunai(p_periode, p_remise);
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
        public static DTOProductInfo RetrieveProductInfo(string barcode)
        {
            return repository.RetrieveProductInfo(barcode);
        }

    }
}
