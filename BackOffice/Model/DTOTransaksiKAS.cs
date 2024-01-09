using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackOffice.Model
{
    public class DTOTransaksiKAS
    {
        public string NOMOR { get; set; }
        public DateTime TANGGAL { get; set; }
        public string KETERANGAN { get; set; }
        public decimal DEBET { get; set; }
        public decimal KREDIT { get; set; }
        public decimal SALDO { get; set; }
    }
}
