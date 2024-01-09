using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackOffice.Model
{
    public class DTOFakturPenjualan_Header_Angsur
    {
        public double Penjualan_ID { get; set; }
        public string nofaktur { get; set; }
        public DateTime tanggal { get; set; }
        public string jam { get; set; }
        public string kasir { get; set; }
        public int kode_pelanggan { get; set; }
        public string nik { get; set; }
        public string nama_pelanggan { get; set; }
        public string unit_kerja { get; set; }
        public string status { get; set; }
        public decimal Bruto { get; set; }
        public decimal Potongan { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal Uang_Muka { get; set; }
        public decimal Sisa { get; set; }
        public int Waktu_Angsuran { get; set; }
        public decimal Jumlah_Angsuran { get; set; }
        public decimal Sisa_Angsuran { get; set; }

    }
}
