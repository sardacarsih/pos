using BackOffice.Model;
using BackOffice.UC;

namespace BackOffice.Interface
{
    public interface IStockOpname
    {
        List<DTOStoctOpnameMaster> DaftarStockOpname(int tahun);
        void HapusStockOpname(string no_faktur);
        bool CheckStockOpname(string kodeBarang, DateTime SODate);
        DTOStockDataItem GetStockData(string kodeBarang, DateTime startDate, DateTime endDate);
        string SaveStockOpname(DateTime transactionDate, IReadOnlyCollection<TransactionStockOpname> items);
        void UpdateTransactionNumber(string transactionNumber);
        List<DTOStockData> GetProductStockInfo(DateTime startDate, DateTime endDate);

        string Insert_BarangRusak(DateTime transactionDate, IReadOnlyCollection<TransactionBarangRusak> items);
    }
}
