using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackOffice.Model
{
    public class DTOPRODUCTS
    {
        public Int32 PRODUCTID { get; set; }
        public string KODE_ITEM { get; set; }
        public string BARCODE { get; set; }
        public string PRODUCTNAME { get; set; }
        public string SATUAN { get; set; }
        public decimal PRICE { get; set; }
        public string KATEGORI { get; set; }
        public string KATEGORI_ID { get; set; }
        public decimal BELI { get; set; }

        // Calculated property for profit margin
        public decimal MARGIN
        {
            get { return PRICE - BELI; }
        }
        public char AKTIF { get; set; }
    }

}
