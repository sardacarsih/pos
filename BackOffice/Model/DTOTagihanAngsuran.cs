using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackOffice.Model
{
    public  class DTOTagihanAngsuran
    {
            public string NIK { get; set; }
            public string NAMA_PELANGGAN { get; set; }
            public string STATUS { get; set; }
            public string UNIT_KERJA { get; set; }
            public decimal TOTAL { get; set; }
            public string NO_TRANSAKSI { get; set; }
            public DateTime TGLKREDIT { get; set; }
            public int KE { get; set; }
            public int DARI { get; set; }
            public List<DTOPOS_KREDIT_ANGSURAN_DETAIL> Angsuran { get; set; }
            public List<DTOPOS_KREDIT_PENJUALAN_DETAIL> DaftarBarang { get; set; }
    }


}
