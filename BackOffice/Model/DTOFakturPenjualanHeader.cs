using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackOffice.Model
{
    public class DTOFakturPenjualanHeader
    {
        public string NO_TRANSAKSI { get; set; }
        public DateTime TANGGAL { get; set; }
        public string JAM { get; set; }
        public string KASIR { get; set; }
        public double ID_PELANGGAN { get; set; }
        public string NIK { get; set; }
        public string NAMA_PELANGGAN { get; set; }
        public string STATUS { get; set; }
        public string UNIT_KERJA { get; set; }
        public decimal BRUTO { get; set; }
        public decimal POTONGAN { get; set; }
        public decimal TOTAL { get; set; }
        public string JENIS_BAYAR { get; set; }
        public string KET_BAYAR { get; set; }
        public int TENOR { get; set; }
        public decimal ANGSURAN { get; set; }
        public string PENDING { get; set; }
    }
}
