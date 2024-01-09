using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackOffice.Model
{
    public class DTOStockData
    {
        public string IDX { get; set; }
        public string KODE_ITEM { get; set; }
        public string PRODUCTNAME { get; set; }
        public string SATUAN { get; set; }
        public decimal STOCKAWAL_QTY { get; set; }
        public decimal STOCKAWAL_HPP { get; set; }
        public decimal BELI_QTY { get; set; }
        public decimal BELI_HARGA_AVG { get; set; }
        public decimal JUAL_QTY { get; set; }
        public decimal RUSAK_QTY { get; set; }
        public decimal STOCK_OPNAME { get; set; }
        public decimal STOCK_AKHIR { get; set; }
        public decimal TOTAL_COST_AVG { get; set; }
        public decimal PERSEDIAAN { get; set; }
    }


}
