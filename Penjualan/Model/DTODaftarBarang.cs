using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Penjualan.Model
{
    public class DTODaftarBarang
    {
       
        public string NO_TRANSAKSI { get; set; }
        public int BARIS { get; set; }
        public string NAMA_BARANG { get; set; }
        public string SATUAN { get; set; }
        public decimal JUMLAH_BARANG { get; set; }
        public decimal HPP { get; set; }
        public decimal HARGA_BARANG { get; set; }
        public decimal BRUTO { get; set; }
        public decimal POTONGAN { get; set; }
        public decimal TOTAL_HARGA { get; set; }
    }
}
