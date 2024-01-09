using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackOffice.Model
{
   
        public class DTOStoctOpnameDetail
    {
            public string NOMOR_SO { get; set; }
            public string KODE_BARANG { get; set; }
            public string PRODUCTNAME { get; set; }
            public int JUMLAHSISTEM { get; set; }
            public int JUMLAHFISIK { get; set; }
            public int SELISIH { get; set; }
            public double HPP { get; set; }
        public double TOTAL { get; set; }
    }
}
