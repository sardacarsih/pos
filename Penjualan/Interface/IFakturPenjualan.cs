using Penjualan.Model;

namespace Penjualan.Interface
{
    public interface IFakturPenjualan
    {
        void InsertFaktur_Penjualan(DTOFakturPenjualanHeader faktur_header, List<DTOFakturPenjualanDetail> ListItemsPenjualan, CreditLimitCheck? creditCheck = null, string? pendingNoTransaksi = null);
        void InsertFaktur_Penjualan_Angsuran(DTOFakturPenjualanHeader faktur_header, List<DTOFakturPenjualanDetail> ListItemsPenjualan, List<DTOAngsuranKreditBarang> DaftarWaktuTagihan, CreditLimitCheck? creditCheck = null, string? pendingNoTransaksi = null);
        string GenerateTransactionNumber(DateTime date);
        void UpdateFakturPenjualan(DTOFakturPenjualanHeader faktur_header);
        DTOProductInfo RetrieveProductInfo(string barcode);
        decimal GetStocItem(string kodeBarang, DateTime startDate, DateTime endDate);
        bool GetSettingKontrol_qty_Saldo();
        DTOPeriodeDates? GetTanggalByPeriode(int periode);
        DTOPotonganHarga? GetPotonganByKodeItem(string kodeItem);
        List<DTOPelanggan> GetPelangganAktif();
        decimal CheckingJumlahHutang(string nik, DateTime dari, DateTime sampai);
    }
}
