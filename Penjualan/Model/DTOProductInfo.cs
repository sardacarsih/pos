using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Penjualan.Model
{
    public class DTOProductInfo
    {
        public int ProductId { get; set; }
        public string KodeItem { get; set; }
        public string ProductName { get; set; }
        public string Satuan { get; set; }
        public decimal Price { get; set; }
        public decimal Hpp { get; set; }
        /// <summary>Jumlah satuan dasar per satuan jual ini (1 = satuan dasar).</summary>
        public decimal Isi { get; set; } = 1m;
    }
}
