using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackOffice.Model
{
    public class DTOPOS_KREDIT_ANGSURAN_DETAIL
    {
        public string NO_TRANSAKSI { get; set; }
        public DateTime TANGGALJATUHTEMPO { get; set; }
        public int ANGSURANKE { get; set; }
        public decimal SALDOAWAL { get; set; }
        public decimal ANGSURAN { get; set; }
        public decimal SALDOAKHIR { get; set; }
        public string ISTAGIH { get; set; }
    }
}
