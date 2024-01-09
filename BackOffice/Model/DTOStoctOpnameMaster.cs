using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackOffice.Model
{
        public class DTOStoctOpnameMaster
    {
            public string BULAN { get; set; }
            public string NOMOR_SO { get; set; }
            public DateTime TANGGAL { get; set; }
        public double TOTAL { get; set; }
        public List<DTOStoctOpnameDetail> Details { get; set; } 
        }
}
