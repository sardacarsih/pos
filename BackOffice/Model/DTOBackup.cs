using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackOffice.Model
{
    public class DTOBackup
    {
        public class FinAnggota
        {
            public string NIK { get; set; }
            public string NAMA_PELANGGAN { get; set; }
            public string ALAMAT { get; set; }
            public string KOTA { get; set; }
            public string KODE_POS { get; set; }
            public string NO_TELP { get; set; }
            public string LOKASI { get; set; }
            public string KELOMPOK { get; set; }
            public decimal LIMIT_HUTANG { get; set; } = 0;
            public string STATUS { get; set; }
            public string AKTIF { get; set; }
            public string UNIT_KERJA { get; set; }
            public DateTime TMK { get; set; }
            public string ANGGOTA { get; set; } = "Y";
            public DateTime TGLNONAKTIF { get; set; }
            public byte[] GAMBAR { get; set; }
            public string UPDATEAT { get; set; }
        }
        public class PosProduct
        {
            public string KODE_ITEM { get; set; }
            public string PRODUCTNAME { get; set; }
            public string SATUAN { get; set; }
            public string BARCODE { get; set; }
            public string KATEGORI_ID { get; set; }
            public decimal PRICE { get; set; }     
            public decimal BELI { get; set; }
            public string AKTIF { get; set; }
            public decimal HPP { get; set; }
            public string LASTUPDATE { get; set; }
        }

        public class PEMBELIANMASTER
        {
            public string NO_TRANSAKSI { get; set; }
            public DateTime TANGGAL { get; set; }
            public string SUPPLIER_ID { get; set; }
            public decimal BRUTO { get; set; }
            public decimal POTONGAN { get; set; }
            public decimal TOTAL { get; set; }
            public int TERMIN { get; set; }
            public string USERID { get; set; }
        }
        public class PEMBELIANDETAIL
        {
            public decimal PURCHASE_ID { get; set; }
            public string NO_TRANSAKSI { get; set; }
            public decimal BARIS { get; set; }
            public decimal PRODUCT_ID { get; set; }
            public string KODE_BARANG { get; set; }
            public string NAMA_BARANG { get; set; }
            public string SATUAN { get; set; }
            public decimal QUANTITY { get; set; }
            public decimal HARGA_BELI { get; set; }
            public decimal BRUTO { get; set; }
            public decimal POTONGAN { get; set; }
            public decimal TOTAL { get; set; }
        }
        public class PENJUALANMASTER
        {
            public string NO_TRANSAKSI { get; set; }
            public DateTime TANGGAL { get; set; }
            public string JAM { get; set; }
            public string KASIR { get; set; }
            public decimal ID_PELANGGAN { get; set; }
            public string NIK { get; set; }
            public string NAMA_PELANGGAN { get; set; }
            public string STATUS { get; set; }
            public string UNIT_KERJA { get; set; }
            public decimal BRUTO { get; set; }
            public decimal POTONGAN { get; set; }
            public decimal TOTAL { get; set; }
            public string JENIS_BAYAR { get; set; }
            public string KET_BAYAR { get; set; }
            public string PENDING { get; set; } = "T";
            public decimal TENOR { get; set; } = 1;
            public decimal ANGSURAN { get; set; } = 0;
            public string ISLUNAS { get; set; } = "T";
        }

        public class PosPenjualanDetail
        {
            public decimal ID_PENJUALAN { get; set; }
            public string NO_TRANSAKSI { get; set; }
            public int BARIS { get; set; }
            public int PRODUCT_ID { get; set; }
            public string KODE_BARANG { get; set; }
            public string BARCODE { get; set; }
            public string NAMA_BARANG { get; set; }
            public string SATUAN { get; set; }
            public int JUMLAH_BARANG { get; set; }
            public decimal HARGA_BARANG { get; set; }
            public decimal BRUTO { get; set; }
            public decimal POTONGAN { get; set; }
            public decimal TOTAL_HARGA { get; set; }
            public decimal HPP { get; set; }
        }

        public class MergedData
        {
            public List<FinAnggota> AnggotaList { get; set; }
            public List<PosProduct> BarangList { get; set; }
            public List<PEMBELIANMASTER> PembelianList { get; set; }
            public List<PEMBELIANDETAIL> PembelianDetailList { get; set; }
            public List<PENJUALANMASTER> PenjualanList { get; set; }
            public List<PosPenjualanDetail> PenjualanDetailList { get; set; }
        }
    }
}
