using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Penjualan.Model
{
    public class DTODaftarPenjualan
    {
        public string UNIT_KERJA { get; set; }
        public string NO_TRANSAKSI { get; set; }
        public DateTime TANGGAL { get; set; }
        public string NIK { get; set; }
        public string NAMA_PELANGGAN { get; set; }       
        public string STATUS { get; set; }
        public decimal BRUTO { get; set; }
        public decimal POTONGAN { get; set; }
        public decimal TOTAL { get; set; }
        public int TENOR { get; set; }
        public List<DTODaftarBarang> DetailsBarang { get; set; }
    }
}
