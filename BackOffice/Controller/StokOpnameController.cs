using BackOffice.DataLayer;
using BackOffice.Interface;
using BackOffice.Model;
using BackOffice.UC;
using DevExpress.Office.Utils;
using DevExpress.XtraRichEdit.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static BackOffice.Model.DTOStoctOpnameDetail;

namespace BackOffice.Controller
{
    internal class StokOpnameController
    {

        static readonly IStockOpname repository;
        static StokOpnameController()
        {
            repository = new StockOpnamenRepository();
        }
        public List<DTODaftarBarang> GetDaftarPembelianBarang(string no_transaksi)
        {
            return repository.GetDaftarPembelianBarang(no_transaksi);
        }
        public List<DTOStoctOpnameMaster> DaftarStockOpname(int tahun)
        {
            return repository.DaftarStockOpname(tahun);
        }
        public bool CheckStockOpname(string kodeBarang, DateTime SODate)
        {
            return repository.CheckStockOpname(kodeBarang, SODate);
        }
        public DTOStockDataItem GetStockData(string kodeBarang, DateTime startDate, DateTime endDate)
        {
            return repository.GetStockData( kodeBarang,  startDate,  endDate);
        }
        public List<DTOStockData> GetProductStockInfo(DateTime startDate, DateTime endDate)
        {
            return repository.GetProductStockInfo( startDate, endDate);
        }
        public void Insert_StockOpname(List<TransactionStockOpname> StockOpname_List)
        {
            repository.Insert_StockOpname(StockOpname_List);
        }
        public void UpdateTransactionNumber(string transactionNumber)
        {
             repository.UpdateTransactionNumber(transactionNumber);
        }
        public void HapusStockOpname(string no_faktur_pembeian)
        {
            repository.HapusStockOpname(no_faktur_pembeian);
        }

        public void Insert_BarangRusak(List<TransactionBarangRusak> BarangRusak_List)
        {
            repository.Insert_BarangRusak(BarangRusak_List);
        }
    }
}
