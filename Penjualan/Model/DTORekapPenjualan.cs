using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Penjualan.Model
{
    public class DTORekapPenjualan
    {
        public string Jenis { get; set; }
        public DateTime Tanggal { get; set; }
        public decimal Jumlah { get; set; }
    }
}
