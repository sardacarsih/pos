using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackOffice.Model
{
    public class DTOFakturPending
    {
        public string NO_TRANSAKSI { get; set; }
        public DateTime TANGGAL { get; set; }
        public string JAM { get; set; }
        public string KASIR { get; set; }
        public List<DTODaftarBarangPending> Details { get; set; }
    }
}
