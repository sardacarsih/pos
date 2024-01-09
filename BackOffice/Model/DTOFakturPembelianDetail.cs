namespace BackOffice.Model
{
    public class DTOFakturPembelianDetail
    {
        public double PURCHASE_ID { get; set; }
        public string NO_TRANSAKSI { get; set; }
        public int BARIS { get; set; }
        public Double PRODUCT_ID { get; set; }
        public string KODE_BARANG { get; set; }
        public string NAMA_BARANG { get; set; }
        public string SATUAN { get; set; }
        public decimal QUANTITY { get; set; }
        public decimal HARGA_BELI { get; set; }
        public decimal HARGA_JUAL { get; set; }
        public decimal BRUTO { get; set; }
        public decimal POTONGAN { get; set; }
        public decimal TOTAL { get; set; }
    }
}
