using BackOffice.Model;
using Oracle.ManagedDataAccess.Client;
using System.Collections.Generic;

namespace BackOffice.DataLayer
{
    public class PiutangKreditBarangRepository
    {
        private readonly string _connectionString;

        public PiutangKreditBarangRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public List<DTOPOS_KREDIT_PENJUALAN_MASTER> GetDaftarKreditBarangBelumLunas()
        {
            List<DTOPOS_KREDIT_PENJUALAN_MASTER> Master = new();

            string query = "SELECT NO_TRANSAKSI,TANGGAL,NIK,NAMA_PELANGGAN,STATUS,UNIT_KERJA,TOTAL,TENOR,ANGSURAN FROM POS_PENJUALAN WHERE TENOR>1 AND ISLUNAS='T'";

            using (OracleConnection connection = new(_connectionString))
            {
                connection.Open();

                using OracleCommand command = new(query, connection);
                using OracleDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    DTOPOS_KREDIT_PENJUALAN_MASTER DaftarKreditBarang = new()
                    {
                        NO_TRANSAKSI = reader["NO_TRANSAKSI"].ToString(),
                        TANGGAL = Convert.ToDateTime(reader["TANGGAL"]),
                        NIK = reader["NIK"].ToString(),
                        NAMA_PELANGGAN = reader["NAMA_PELANGGAN"].ToString(),
                        STATUS = reader["STATUS"].ToString(),
                        UNIT_KERJA = reader["UNIT_KERJA"].ToString(),
                        TOTAL = Convert.ToDecimal(reader["TOTAL"]),
                        TENOR = Convert.ToInt32(reader["TENOR"]),
                        ANGSURAN = Convert.ToDecimal(reader["ANGSURAN"])
                    };

                    // Fetch products for the order
                    List<DTOPOS_KREDIT_ANGSURAN_DETAIL> angsuran = GetDaftarAngsuran(reader["NO_TRANSAKSI"].ToString());
                    DaftarKreditBarang.DetailsAngsuran = angsuran;

                    List<DTOPOS_KREDIT_PENJUALAN_DETAIL> barang = GetDaftarBarang(reader["NO_TRANSAKSI"].ToString());
                    DaftarKreditBarang.DetailsBarang = barang;

                    // Calculate the sum of ANGSURAN for ISTAGIH = 'T'
                    decimal totalAngsuran = angsuran.Where(d => d.ISTAGIH == "T").Sum(d => d.ANGSURAN);
                    int SISAWAKTUANGSURAN = angsuran.Count(d => d.ISTAGIH == "T");
                    DaftarKreditBarang.SISAWAKTU = SISAWAKTUANGSURAN;
                    DaftarKreditBarang.PIUTANG = totalAngsuran;

                    Master.Add(DaftarKreditBarang);
                }
            }

            return Master;
        }

        private List<DTOPOS_KREDIT_PENJUALAN_DETAIL> GetDaftarBarang(string idtransaksi)
        {
            List<DTOPOS_KREDIT_PENJUALAN_DETAIL> Detail = new();

            string query = "SELECT BARIS,NAMA_BARANG,SATUAN,JUMLAH_BARANG,HARGA_BARANG,POTONGAN,TOTAL_HARGA FROM  POS_PENJUALAN_DETAIL WHERE NO_TRANSAKSI = :idtransaksi ORDER BY BARIS";

            using (OracleConnection connection = new(_connectionString))
            {
                connection.Open();

                using OracleCommand command = new(query, connection);
                command.Parameters.Add(":idtransaksi", OracleDbType.Varchar2).Value = idtransaksi;

                using OracleDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    DTOPOS_KREDIT_PENJUALAN_DETAIL DaftarBarang = new()
                    {
                        BARIS = Convert.ToInt32(reader["BARIS"]),
                        NAMA_BARANG = reader["NAMA_BARANG"].ToString(),
                        SATUAN = reader["SATUAN"].ToString(),
                        JUMLAH_BARANG = Convert.ToInt32(reader["JUMLAH_BARANG"]),
                        HARGA_BARANG = Convert.ToDecimal(reader["HARGA_BARANG"]),
                        POTONGAN = Convert.ToDecimal(reader["POTONGAN"]),
                        TOTAL_HARGA = Convert.ToDecimal(reader["TOTAL_HARGA"])
                    };

                    Detail.Add(DaftarBarang);
                }
            }

            return Detail;
        }

        private List<DTOPOS_KREDIT_ANGSURAN_DETAIL> GetDaftarAngsuran(string idtransaksi)
        {
            List<DTOPOS_KREDIT_ANGSURAN_DETAIL> Detail = new();

            string query = "SELECT NO_TRANSAKSI,TANGGALJATUHTEMPO,ANGSURANKE,SALDOAWAL,ANGSURAN,SALDOAKHIR,ISTAGIH TAGIH from POS_KREDIT_ANGSURAN WHERE NO_TRANSAKSI = :idtransaksi ORDER BY ANGSURANKE";

            using (OracleConnection connection = new(_connectionString))
            {
                connection.Open();

                using OracleCommand command = new(query, connection);
                command.Parameters.Add(":idtransaksi", OracleDbType.Varchar2).Value = idtransaksi;

                using OracleDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    DTOPOS_KREDIT_ANGSURAN_DETAIL angsuranBarang = new()
                    {
                        NO_TRANSAKSI = reader["NO_TRANSAKSI"].ToString(),
                        TANGGALJATUHTEMPO = Convert.ToDateTime(reader["TANGGALJATUHTEMPO"]),
                        ANGSURANKE = Convert.ToInt32(reader["ANGSURANKE"]),
                        SALDOAWAL = Convert.ToDecimal(reader["SALDOAWAL"]),
                        ANGSURAN = Convert.ToDecimal(reader["ANGSURAN"]),
                        SALDOAKHIR = Convert.ToDecimal(reader["SALDOAKHIR"]),
                        ISTAGIH = reader["TAGIH"].ToString()
                    };

                    Detail.Add(angsuranBarang);
                }
            }

            return Detail;
        }
    }
}
