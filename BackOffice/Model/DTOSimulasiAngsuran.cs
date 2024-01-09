using System;
using System.Linq;

namespace BackOffice.Model
{
    public class DTOSimulasiAngsuran
    {
        public int Periode { get; set; }
        public string NO_TRANSAKSI { get; set; }
        public DateTime TanggalJatuhTempo { get; set; }
        public int AngsuranKe { get; set; }
        public decimal SaldoAwal { get; set; }
        public decimal Pokok { get; set; }
        public decimal Bunga { get; set; }
        public decimal Angsuran { get; set; }
        public decimal SaldoAkhir { get; set; }
    }
}
