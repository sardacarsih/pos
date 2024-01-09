using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackOffice.Model
{
    public class DTOStockDataItem
    {
        public string KodeBarang { get; set; }
        public decimal StockQty { get; set; }
        public decimal BeliQty { get; set; }
        public decimal JualQty { get; set; }
        public decimal StockOpname { get; set; }
        public decimal StockAkhir { get; set; }
    }
}
