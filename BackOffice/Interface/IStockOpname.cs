using BackOffice.Model;
using BackOffice.UC;

namespace BackOffice.Interface
{
    public interface IStockOpname
    {
        List<DTOStoctOpnameMaster> DaftarStockOpname(int tahun);
        void HapusStockOpname(string no_faktur);
        void EditStockOpname(string no_faktur);
        void CetakStockOpname(string no_faktur);
        bool CheckStockOpname(string kodeBarang, DateTime SODate);
        DTOStockDataItem GetStockData(string kodeBarang, DateTime startDate, DateTime endDate);
        void Insert_StockOpname(List<TransactionStockOpname> StockOpname_List);
        void UpdateTransactionNumber(string transactionNumber);
        List<DTOStockData> GetStockAkhir(DateTime startDate, DateTime endDate,int opsistock);
        List<DTOStockData> GetProductStockInfo(DateTime startDate, DateTime endDate);
        List<DTODaftarBarang> GetDaftarPembelianBarang(string idtransaksi);

        void Insert_BarangRusak(List<TransactionBarangRusak> BarangRusak_List);

    }
}
