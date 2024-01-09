using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackOffice.Model
{
    public class DTOKartuStok
    {
        public string Keterangan { get; set; }
        public DateTime Tanggal { get; set; }
        public decimal Masuk { get; set; }
        public decimal Keluar { get; set; }
        public decimal Saldo { get; set; }
    }

}
