using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackOffice.Model
{
    public class FinTagihanWaserdaDtl
    {
        public string NoTransaksi { get; set; }
        public DateTime Tanggal { get; set; }
        public string Jam { get; set; }
        public string Nik { get; set; }
        public int Baris { get; set; }
        public string NamaBarang { get; set; }
        public string Satuan { get; set; }
        public int JumlahBarang { get; set; }
        public decimal HargaBarang { get; set; }
        public decimal Bruto { get; set; }
        public decimal Potongan { get; set; }
        public decimal TotalHarga { get; set; }

        // Add any other properties if needed
    }


}
