using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackOffice.Model
{
    public class DTOLabaRugiAnalisa
    {
        public DateTime TANGGAL { get; set; }
        public string NO_TRANSAKSI { get; set; }
        public string KODE_BARANG { get; set; }
        public string NAMA_BARANG { get; set; }
        public decimal QTY { get; set; }
        public decimal PENJUALAN { get; set; }
        public decimal HPP { get; set; }
        public decimal LABA { get; set; }
        public decimal PERSEN { get; set; }

    }
}
