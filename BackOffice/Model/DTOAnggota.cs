using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackOffice.Model
{
    public class DTOAnggota
    {
        public string NIK { get; set; }
        public string NAMA_PELANGGAN { get; set; }
        public string STATUS { get; set; }
        public DateTime? TMK { get; set; }
        public string KODE_UNIT { get; set; }
        public string UNIT_KERJA { get; set; }
        public decimal LIMIT_HUTANG { get; set; }  
        public string AKTIF { get; set; }
        public string ANGGOTA { get; set; }
        public byte[] GAMBAR { get; set; }


    }
}
