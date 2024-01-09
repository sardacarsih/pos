using Oracle.ManagedDataAccess.Client;
using Penjualan.Interface;
using Penjualan.Model;
using System;
using System.Linq;

namespace Penjualan.DataLayer
{
    public class DaftarPenjualan : IDaftarPenjualan
    {
        public List<DTODaftarPenjualan> GetPenjualan(DateTime date1, DateTime date2)
        {
            List<DTODaftarPenjualan> Master = new();

            string query = @"SELECT U.NAMA UNIT_KERJA,J.NO_TRANSAKSI,J.TANGGAL,J.NIK,J.NAMA_PELANGGAN,J.STATUS,J.BRUTO,J.POTONGAN,J.TOTAL,J.TENOR FROM POS_PENJUALAN J
                                JOIN FIN_UNITKERJA U ON U.KODE=J.UNIT_KERJA
                                WHERE J.TANGGAL BETWEEN :1 AND:2 AND J.PENDING='T' ORDER BY J.TANGGAL,J.NIK";

            using (OracleConnection connection = new(global.connectionString))
            {
                connection.Open();

                using OracleCommand command = new(query, connection);
                command.Parameters.Add(":1", OracleDbType.Date).Value = date1;
                command.Parameters.Add(":2", OracleDbType.Date).Value = date2;

                using OracleDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {                    
                    DTODaftarPenjualan DaftarPenjualan = new()
                    {
                        UNIT_KERJA = reader["UNIT_KERJA"].ToString(),
                        NO_TRANSAKSI = reader["NO_TRANSAKSI"].ToString(),
                        TANGGAL = Convert.ToDateTime(reader["TANGGAL"]),
                        NIK = reader["NIK"].ToString(),
                        NAMA_PELANGGAN = reader["NAMA_PELANGGAN"].ToString(),
                        STATUS = reader["STATUS"].ToString(),
                        BRUTO = Convert.ToDecimal(reader["BRUTO"]),
                        POTONGAN = Convert.ToDecimal(reader["POTONGAN"]),
                        TOTAL = Convert.ToDecimal(reader["TOTAL"]),
                        TENOR = Convert.ToInt32(reader["TENOR"])
                    };
                    //// Mengambil produk untuk transaksi
                    //List<DTODaftarBarang> barang = GetDaftarBarang(reader["NO_TRANSAKSI"].ToString());
                    //DaftarPenjualan.DetailsBarang = barang;
                    Master.Add(DaftarPenjualan);
                }
            }

            return Master;
        }

        public  List<DTODaftarBarang> GetDaftarBarang(string idtransaksi)
        {
            List<DTODaftarBarang> Detail = new();

            string query = "SELECT BARIS,NAMA_BARANG,SATUAN,JUMLAH_BARANG,HARGA_BARANG,POTONGAN,TOTAL_HARGA FROM  POS_PENJUALAN_DETAIL WHERE NO_TRANSAKSI = :idtransaksi ORDER BY BARIS";

            using (OracleConnection connection = new(global.connectionString))
            {
                connection.Open();

                using OracleCommand command = new(query, connection);
                command.Parameters.Add(":idtransaksi", OracleDbType.Varchar2).Value = idtransaksi;

                using OracleDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    DTODaftarBarang DaftarBarang = new()
                    {
                        NO_TRANSAKSI= idtransaksi,
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

        
    }
}
