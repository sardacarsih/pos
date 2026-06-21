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
            public decimal JUMLAHSISTEM { get; set; }
            public decimal JUMLAHFISIK { get; set; }
            public decimal SELISIH { get; set; }
            public decimal HPP { get; set; }
            public decimal TOTAL { get; set; }
    }
}
