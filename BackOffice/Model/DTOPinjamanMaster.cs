using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackOffice.Model
{
    public class DTOPinjamanMaster
    {
        public string NO_TRANSAKSI { get; set; }
        public DateTime TANGGAL { get; set; }
        public string NIK { get; set; }
        public string NAMA_PELANGGAN { get; set; }
        public string STATUS { get; set; }
        public string UNIT_KERJA { get; set; }
        public decimal PINJAMAN { get; set; }
        public int TENOR { get; set; }
        public decimal BUNGA { get; set; }
        public decimal ANGSURAN { get; set; }
        public string ISLUNAS { get; set; }
        public decimal PIUTANG { get; set; }
        public int SISAWAKTU { get; set; }

        public List<DTOPinjamanDetail> Details { get; set; }

    }
}
