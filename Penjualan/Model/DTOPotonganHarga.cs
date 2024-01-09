using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Penjualan.Model
{
    public class DTOPotonganHarga
    {
            public int ProductID { get; set; }
            public int MinQty { get; set; }
            public decimal Potongan { get; set; }
    }
}
