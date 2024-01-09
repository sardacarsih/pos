using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Penjualan.Model
{
    public class DTOPRODUCT_WSTOCK
    {
        public Int32 PRODUCTID { get; set; }
        public string BARCODE { get; set; }
        public string KODE_ITEM { get; set; }
        public string PRODUCTNAME { get; set; }
        public string SATUAN { get; set; }
        public decimal PRICE { get; set; }
        public decimal BELI { get; set; }
        public decimal STOCK { get; set; }
    }
}
