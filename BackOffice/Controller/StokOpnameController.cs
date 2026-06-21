using BackOffice.DataLayer;
using BackOffice.Interface;
using BackOffice.Model;
using BackOffice.UC;
using System;
using System.Collections.Generic;

namespace BackOffice.Controller
{
    internal class StokOpnameController
    {

        static readonly IStockOpname repository;
        static StokOpnameController()
        {
            repository = new StockOpnamenRepository();
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
            return repository.GetStockData(kodeBarang, startDate, endDate);
        }
        public List<DTOStockData> GetProductStockInfo(DateTime startDate, DateTime endDate)
        {
            return repository.GetProductStockInfo(startDate, endDate);
        }
        public string SaveStockOpname(DateTime transactionDate, IReadOnlyCollection<TransactionStockOpname> items)
        {
            return repository.SaveStockOpname(transactionDate, items);
        }
        public void UpdateTransactionNumber(string transactionNumber)
        {
            repository.UpdateTransactionNumber(transactionNumber);
        }
        public void HapusStockOpname(string no_faktur)
        {
            repository.HapusStockOpname(no_faktur);
        }

        public string Insert_BarangRusak(DateTime transactionDate, IReadOnlyCollection<TransactionBarangRusak> items)
        {
            return repository.Insert_BarangRusak(transactionDate, items);
        }
    }
}
