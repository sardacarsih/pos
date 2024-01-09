using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackOffice.Model
{
    public class DTOPelanggan
    {
        public string ID_PELANGGAN { get; set; }
        public string NIK { get; set; }
        public string NAMA_PELANGGAN { get; set; }
        public string UNIT_KERJA { get; set; }
        public string UNITKERJA { get; set; }
        public string STATUS { get; set; }
        public decimal LIMIT_HUTANG { get; set; }
    }

}
