using Dapper;
using Oracle.ManagedDataAccess.Client;
using Penjualan.Interface;
using Penjualan.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Penjualan.DataLayer
{
    public class FakturPending : IFakturPending
    {
        public void DeletePendingFaktur(string idtransaksi)
        {
            string query = @"DELETE FROM POS_PENDING WHERE NO_TRANSAKSI = :notransaksi";

            using OracleConnection connection = new(global.connectionString);
            connection.Open();

            using OracleCommand command = new(query, connection);
            command.Parameters.Add(new OracleParameter("notransaksi", idtransaksi));
            command.ExecuteNonQuery();
        }

        public List<DTOFakturPending> GetPenjualanPending()
        {
                List<DTOFakturPending> Master = new();

                string query = @"SELECT NO_TRANSAKSI,TANGGAL,JAM,KASIR FROM POS_PENDING ORDER BY TANGGAL";

                using (OracleConnection connection = new(global.connectionString))
                {
                    connection.Open();

                    using OracleCommand command = new(query, connection);

                    using OracleDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        DTOFakturPending DaftarPenjualanPending = new()
                        {
                            NO_TRANSAKSI = reader["NO_TRANSAKSI"].ToString(),
                            TANGGAL = Convert.ToDateTime(reader["TANGGAL"]),
                            JAM = reader["JAM"].ToString(),
                            KASIR = reader["KASIR"].ToString()
                        };
                    //// Mengambil produk untuk transaksi
                    List<DTODaftarBarangPending> barang = GetDaftarBarangPending(reader["NO_TRANSAKSI"].ToString());
                    DaftarPenjualanPending.Details = barang;
                    Master.Add(DaftarPenjualanPending);
                    }
                }

                return Master;
        }

        public List<DTODaftarBarangPending> GetDaftarBarangPending(string idtransaksi)
        {
            List<DTODaftarBarangPending> Detail = new();

            string query = @"SELECT NO_TRANSAKSI,BARIS,PRODUCT_ID,KODE_BARANG,BARCODE,
                              NAMA_BARANG,SATUAN,JUMLAH_BARANG,HPP,HARGA_BARANG,
                              BRUTO,POTONGAN,TOTAL_HARGA
                            FROM
                              POS_PENDING_DETAIL
                            WHERE
                              NO_TRANSAKSI = :idtransaksi";

            using (OracleConnection connection = new(global.connectionString))
            {
                connection.Open();

                using OracleCommand command = new(query, connection);
                command.Parameters.Add(":idtransaksi", OracleDbType.Varchar2).Value = idtransaksi;

                using OracleDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    DTODaftarBarangPending DaftarBarang = new()
                    {
                        NO_TRANSAKSI= idtransaksi,
                        BARIS = Convert.ToInt32(reader["BARIS"]),
                        PRODUCT_ID = Convert.ToInt32(reader["PRODUCT_ID"]),
                        KODE_BARANG = reader["KODE_BARANG"].ToString(),
                        BARCODE = reader["BARCODE"].ToString(),
                        NAMA_BARANG = reader["NAMA_BARANG"].ToString(),
                        SATUAN = reader["SATUAN"].ToString(),
                        JUMLAH_BARANG = Convert.ToDecimal(reader["JUMLAH_BARANG"]),
                        HARGA_BARANG = Convert.ToDecimal(reader["HARGA_BARANG"]),
                        HPP = Convert.ToDecimal(reader["HPP"]),
                        BRUTO = Convert.ToDecimal(reader["BRUTO"]),
                        POTONGAN = Convert.ToDecimal(reader["POTONGAN"]),
                        TOTAL_HARGA = Convert.ToDecimal(reader["TOTAL_HARGA"])
                    };

                    Detail.Add(DaftarBarang);
                }
            }

            return Detail;
        }

        public void InsertFaktur_Pending(DTOFakturPending faktur_header, List<DTODaftarBarangPending> ListItemsPenjualan)
        {
            using OracleConnection conn = new(global.connectionString);
            conn.Open();
            OracleTransaction transaction = conn.BeginTransaction();

            //try
            //{
                // Insert master records
                string insertFakturJual_Master = "INSERT INTO POS_PENDING (NO_TRANSAKSI, TANGGAL, JAM, KASIR) " +
                                                "VALUES (:NO_TRANSAKSI, :TANGGAL, :JAM, :KASIR) ";
                conn.Execute(insertFakturJual_Master, faktur_header, transaction);


                string insertFakturJual_Detail = "INSERT INTO POS_PENDING_DETAIL ( NO_TRANSAKSI, BARIS, PRODUCT_ID, KODE_BARANG, BARCODE, NAMA_BARANG, SATUAN, JUMLAH_BARANG,HARGA_BARANG, HPP, BRUTO, POTONGAN, TOTAL_HARGA) " +
                    "VALUES (:NO_TRANSAKSI, :BARIS, :PRODUCT_ID, :KODE_BARANG, :BARCODE, :NAMA_BARANG, :SATUAN, :JUMLAH_BARANG,:HARGA_BARANG, :HPP, :BRUTO, :POTONGAN, :TOTAL_HARGA)";

                foreach (var detail in ListItemsPenjualan)
                {
                    detail.NO_TRANSAKSI = faktur_header.NO_TRANSAKSI;
                    var detailParameters = new DynamicParameters(detail);
                    conn.Execute(insertFakturJual_Detail, detailParameters, transaction);
                }

                transaction.Commit();
            //}
            //catch (Exception ex)
            //{
            //    transaction.Rollback();
            //    // Handle or log the exception here
            //    throw ex;
            //}
        }
    }
}
