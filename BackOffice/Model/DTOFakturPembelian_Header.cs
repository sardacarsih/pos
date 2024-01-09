using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackOffice.Model
{
    public class DTOFakturPembelian_Header
    {
        public double PURCHASE_ID { get; set; }
        public string NO_TRANSAKSI { get; set; }
        public DateTime TANGGAL { get; set; }
        public string SUPPLIER_ID { get; set; }
        public string NAMA_SUPPLIER { get; set; }
        public decimal BRUTO { get; set; }
        public decimal POTONGAN { get; set; }
        public decimal TOTAL { get; set; }
        public int TERMIN { get; set; }
        public string USERID { get; set; }
        public List<DTOFakturPembelianDetail> Details { get; set; }
    }
}
