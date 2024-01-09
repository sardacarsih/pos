using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackOffice.Model
{
    public class DTOProductInfo
    {
        public int ProductId { get; set; }
        public string KodeItem { get; set; }
        public string ProductName { get; set; }
        public string Satuan { get; set; }
        public decimal Price { get; set; }
        public decimal Hpp { get; set; }
    }
}
