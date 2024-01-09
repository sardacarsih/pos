using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackOffice.Model
{
    public class DTOPOS_KREDIT_PENJUALAN_MASTER
    {
        public string NO_TRANSAKSI { get; set; }
        public DateTime TANGGAL { get; set; }
        public string JAM { get; set; }
        public string KASIR { get; set; }
        public int ID_PELANGGAN { get; set; }
        public string NIK { get; set; }
        public string NAMA_PELANGGAN { get; set; }
        public string STATUS { get; set; }
        public string UNIT_KERJA { get; set; }
        public decimal BRUTO { get; set; }
        public decimal POTONGAN { get; set; }
        public decimal TOTAL { get; set; }
        public decimal UANG_MUKA { get; set; }
        public decimal SISA { get; set; }
        public int TENOR { get; set; }
        public decimal ANGSURAN { get; set; }
        public decimal SISA_ANGSURAN { get; set; }
        public decimal PELUNASAN { get; set; }
        public string LUNAS { get; set; }
        public decimal PIUTANG { get; set; }
        public int SISAWAKTU { get; set; }


        public List<DTOPOS_KREDIT_PENJUALAN_DETAIL> DetailsBarang { get; set; }
        public List<DTOPOS_KREDIT_ANGSURAN_DETAIL> DetailsAngsuran { get; set; }
    }

}
