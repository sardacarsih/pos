namespace BackOffice.Model
{
    public class DTOPenerimaanPembayaran
    {
        public int PERIODE { get; set; }
        public int REMISE { get; set; }
        public string NIK { get; set; }
        public string NAMA_PELANGGAN { get; set; }
        public string UNIT_KERJA { get; set; }
        public decimal TAGIHAN { get; set; }

        private decimal bayar;
        public decimal BAYAR
        {
            get { return bayar; }
            set
            {
                if (bayar != value)
                {
                    bayar = value;
                    IsModified = true;
                }
            }
        }

        private decimal sisa;
        public decimal SISA
        {
            get { return sisa; }
            set
            {
                if (sisa != value)
                {
                    sisa = value;
                    IsModified = true;
                }
            }
        }
        private bool isModified;
        public bool IsModified
        {
            get { return isModified; }
            set { isModified = value; }
        }
        public List<DTOPenerimaanPembayaranDetail> Details { get; set; }
    }

}
