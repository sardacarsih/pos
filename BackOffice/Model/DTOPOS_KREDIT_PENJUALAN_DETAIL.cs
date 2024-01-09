using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackOffice.Model
{
    public class DTOPOS_KREDIT_PENJUALAN_DETAIL
    {
        public int BARIS { get; set; }
        public int PRODUCT_ID { get; set; }
        public string KODE_BARANG { get; set; }
        public string BARCODE { get; set; }
        public string NAMA_BARANG { get; set; }
        public string SATUAN { get; set; }
        public int JUMLAH_BARANG { get; set; }
        public decimal HARGA_BARANG { get; set; }
        public decimal BRUTO { get; set; }
        public decimal POTONGAN { get; set; }
        public decimal TOTAL_HARGA { get; set; }
        public string NO_TRANSAKSI { get; set; }
    }

}
