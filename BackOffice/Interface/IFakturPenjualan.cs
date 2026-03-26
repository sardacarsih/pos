using BackOffice.Model;
using DevExpress.XtraMap.Native;
using System.Data;

namespace BackOffice.Interface
{
    public interface IFakturPenjualan
    {
        void InsertFaktur_Penjualan(DTOFakturPenjualanHeader faktur_header, List<DTOFakturPenjualanDetail> ListItemsPenjualan);
        void InsertFaktur_Penjualan_Angsuran(DTOFakturPenjualanHeader faktur_header, List<DTOFakturPenjualanDetail> ListItemsPenjualan, List<DTOAngsuranKreditBarang> DaftarWaktuTagihan);
        void UpdateTransactionNumber(string transactionNumber);
        

        DataSet Tagihan_Periode(int p_indextagihan, DateTime p_tglAngsuran, DateTime p_daritanggal, DateTime p_sampaitanggal);
        DataSet Tagihan_ALL(int p_periode, int p_remise);
        DataSet Penjualan_Tunai(int p_periode, int p_remise);
        void Tutup_Buku(int p_periode, int p_remise, DateTime p_tglAngsuran, DateTime p_daritanggal, DateTime p_sampaitanggal);
        DTOProductInfo RetrieveProductInfo(string barcode);
        bool GetSettingKontrol_qty_Saldo();
        decimal GetStocItem(string kodeBarang, DateTime startDate, DateTime endDate);
        List<DTOLabaRugiAnalisa> AnalisaLabaRugi(DateTime date1, DateTime date2);
        List<DTOLabaRugi> GetLabaRugi(int year);
        List<DTODaftarPenjualan> GetPenjualan(DateTime date1, DateTime date2);
        List<DTODaftarBarang> GetDaftarBarang(string idtransaksi);
        void HapusFakturPenjualan(string notransaksi);
        void HapusFakturPenjualanAngsuran(string notransaksi);
        void UpdateFakturPenjualan(DTOFakturPenjualanHeader faktur_header, List<DTODaftarBarang> ListItemsPenjualan);

    }
}

