using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackOffice.Model
{
    public class DTORekapPenjualanByNik
    {
        public string UNIT_KERJA { get; set; }
        public string NIK { get; set; }
        public string NAMA_PELANGGAN { get; set; }
        public decimal TOTAL { get; set; }
    }
}
