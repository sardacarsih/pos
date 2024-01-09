using BackOffice.Interface;
using BackOffice.Model;
using Dapper;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace BackOffice.DataLayer
{
    public class PembelianRepository : IPembelian
    {
        public List<DTOFakturPembelian_Header> DaftarPembelian(int bulan, int tahun)
        {
            List<DTOFakturPembelian_Header> Master = new();

            string query = @"SELECT B.NO_TRANSAKSI,B.TANGGAL,B.SUPPLIER_ID,S.NAMA,B.BRUTO,B.POTONGAN,B.TOTAL,B.TERMIN FROM POS_PEMBELIAN B
                            JOIN  POS_SUPPLIER  S ON S.KODE=B.SUPPLIER_ID
                            WHERE EXTRACT(MONTH FROM B.TANGGAL) = :p_bulan AND EXTRACT(YEAR FROM B.TANGGAL) = :p_tahun
                            ORDER BY B.TANGGAL";

            using (OracleConnection connection = new(global.connectionString))
            {
                connection.Open();

                using OracleCommand command = new(query, connection);
                command.Parameters.Add(":p_bulan", OracleDbType.Int32).Value = bulan;
                command.Parameters.Add(":p_tahun", OracleDbType.Int32).Value = tahun;
                using OracleDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    DTOFakturPembelian_Header DaftarPembelian = new()
                    {
                        NO_TRANSAKSI = reader["NO_TRANSAKSI"].ToString(),
                        TANGGAL = Convert.ToDateTime(reader["TANGGAL"]),
                        SUPPLIER_ID = reader["SUPPLIER_ID"].ToString(),
                        NAMA_SUPPLIER = reader["NAMA"].ToString(),
                        BRUTO = Convert.ToDecimal(reader["BRUTO"]),
                        POTONGAN = Convert.ToDecimal(reader["POTONGAN"]),
                        TOTAL = Convert.ToDecimal(reader["TOTAL"]),
                        TERMIN = Convert.ToInt32(reader["TERMIN"])
                    };

                    // Fetch products for the order
                    List<DTOFakturPembelianDetail> idtransaksi = GetItemBarang(reader["NO_TRANSAKSI"].ToString());
                    DaftarPembelian.Details = idtransaksi;

                    Master.Add(DaftarPembelian);
                }
            }

            return Master;
        }

        private List<DTOFakturPembelianDetail> GetItemBarang(string idtransaksi)
        {
            List<DTOFakturPembelianDetail> Detail = new();

            string query = "SELECT BARIS,PRODUCT_ID,KODE_BARANG,NAMA_BARANG,SATUAN,quantity,HARGA_BELI,BRUTO,POTONGAN,TOTAL FROM  POS_PEMBELIANDETAIL WHERE NO_TRANSAKSI = :idtransaksi ORDER BY BARIS";

            using (OracleConnection connection = new(global.connectionString))
            {
                connection.Open();

                using OracleCommand command = new(query, connection);
                command.Parameters.Add(":idtransaksi", OracleDbType.Varchar2).Value = idtransaksi;

                using OracleDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    DTOFakturPembelianDetail DaftarBarang = new()
                    {
                        NO_TRANSAKSI = idtransaksi,
                        BARIS = Convert.ToInt32(reader["BARIS"]),
                        PRODUCT_ID = Convert.IsDBNull(reader["PRODUCT_ID"]) ? 0 : Convert.ToInt32(reader["PRODUCT_ID"]),
                        KODE_BARANG = reader["KODE_BARANG"].ToString(),
                        NAMA_BARANG = reader["NAMA_BARANG"].ToString(),
                        SATUAN = reader["SATUAN"].ToString(),
                        QUANTITY = Convert.IsDBNull(reader["QUANTITY"]) ? 0 : Convert.ToInt32(reader["QUANTITY"]),
                        HARGA_BELI = Convert.IsDBNull(reader["HARGA_BELI"]) ? 0 : Convert.ToDecimal(reader["HARGA_BELI"]),
                        BRUTO = Convert.IsDBNull(reader["BRUTO"]) ? 0 : Convert.ToDecimal(reader["BRUTO"]),
                        POTONGAN = Convert.IsDBNull(reader["POTONGAN"]) ? 0 : Convert.ToDecimal(reader["POTONGAN"]),
                        TOTAL = Convert.IsDBNull(reader["TOTAL"]) ? 0 : Convert.ToDecimal(reader["TOTAL"])
                    };

                    Detail.Add(DaftarBarang);
                }

            }

            return Detail;
        }

        public List<DTODaftarBarang> GetDaftarBarang(string idtransaksi)
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
                        NO_TRANSAKSI = idtransaksi,
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

        public List<DTOSupplier> GetSuppliers()
        {
            using OracleConnection connection = new(global.connectionString);
            string query = "SELECT KODE,NAMA FROM POS_SUPPLIER WHERE AKTIF='Y' ORDER BY NAMA";
            return connection.Query<DTOSupplier>(query).AsList();
        }

        public void InsertFaktur_Pembelian(DTOFakturPembelian_Header faktur_header, List<DTOFakturPembelianDetail> ListItemPembelian)
        {
            using OracleConnection conn = new(global.connectionString);
            conn.Open();
            OracleTransaction transaction = conn.BeginTransaction();

            try
            {
                // Insert master records
                string insertFakturBeli_Master = "INSERT INTO POS_PEMBELIAN (NO_TRANSAKSI, TANGGAL, SUPPLIER_ID, BRUTO, POTONGAN, TOTAL, TERMIN,USERID) " +
                                                "VALUES (:NO_TRANSAKSI, :TANGGAL, :SUPPLIER_ID,:BRUTO, :POTONGAN, :TOTAL, :TERMIN, :USERID) " +
                                                "RETURNING PURCHASE_ID INTO :PembelianId";

                var masterParameters = new DynamicParameters(faktur_header);
                masterParameters.Add("PembelianId", dbType: DbType.Int32, direction: ParameterDirection.Output);

                int rowsAffected = conn.Execute(insertFakturBeli_Master, masterParameters, transaction);

                Int32 newPembelianId = masterParameters.Get<Int32>("PembelianId");

                string insertFakturBeli_Detail = "INSERT INTO POS_PEMBELIANDETAIL (PURCHASE_ID, NO_TRANSAKSI, BARIS, PRODUCT_ID, KODE_BARANG, NAMA_BARANG, SATUAN,QUANTITY,HARGA_BELI,  BRUTO, POTONGAN, TOTAL) " +
                    "VALUES (:PURCHASE_ID, :NO_TRANSAKSI, :BARIS, :PRODUCT_ID, :KODE_BARANG,  :NAMA_BARANG, :SATUAN, :QUANTITY,:HARGA_BELI, :BRUTO, :POTONGAN, :TOTAL)";

                foreach (var detail in ListItemPembelian)
                {
                    detail.PURCHASE_ID = newPembelianId;
                    detail.NO_TRANSAKSI = faktur_header.NO_TRANSAKSI;

                    var detailParameters = new DynamicParameters(detail);
                    conn.Execute(insertFakturBeli_Detail, detailParameters, transaction);
                }
                //update hpp dan harga jual
                string updateharga = "UPDATE pos_product " +
                     "SET beli = :Beli, price = :Jual, lastupdate = SYSDATE " +
                     "WHERE productid = :ProductID";

                foreach (var product in ListItemPembelian)
                {
                    var parameters = new DynamicParameters(product);
                    parameters.Add("Beli", product.HARGA_BELI, DbType.Decimal, ParameterDirection.Input);
                    parameters.Add("Jual", product.HARGA_JUAL, DbType.Decimal, ParameterDirection.Input);
                    parameters.Add("ProductID", product.PRODUCT_ID, DbType.Int32, ParameterDirection.Input);
                    conn.Execute(updateharga, parameters);
                }

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                // Handle or log the exception here
                throw ex;
            }
        }

        public void UpdateTransactionNumber(string transactionNumber)
        {
            using OracleConnection connection = new(global.connectionString);
            connection.Open();

            // Check if record exists
            string checkQuery = "SELECT COUNT(*) FROM nomor_transaksi WHERE kode = 'PEMBELIAN'";
            using OracleCommand checkCommand = new(checkQuery, connection);
            int count = Convert.ToInt32(checkCommand.ExecuteScalar());

            if (count == 0)
            {
                // Insert new record
                string insertQuery = "INSERT INTO nomor_transaksi (kode, nomor) VALUES ('PEMBELIAN', :nomor)";
                using OracleCommand insertCommand = new(insertQuery, connection);
                insertCommand.Parameters.Add("nomor", transactionNumber);
                insertCommand.ExecuteNonQuery();
            }
            else
            {

                // Update existing record
                string updateQuery = "UPDATE nomor_transaksi SET nomor = :nomor WHERE kode = 'PEMBELIAN'";
                using OracleCommand updateCommand = new(updateQuery, connection);
                updateCommand.Parameters.Add("nomor", transactionNumber);
                updateCommand.ExecuteNonQuery();

            }
        }

        public void HapusPembelian(string no_faktur)
        {
            using OracleConnection connection = new(global.connectionString);
            connection.Open();

            // DELETE existing record
            string deleteQuery = "DELETE FROM POS_PEMBELIAN WHERE NO_TRANSAKSI = :nomor";

            using OracleCommand deleteCommand = new(deleteQuery, connection);
            deleteCommand.Parameters.Add("nomor", no_faktur);
            deleteCommand.ExecuteNonQuery();
        }


        public void EditPembelian(string no_faktur)
        {
            throw new NotImplementedException();
        }

        public void CetakPembelian(string no_faktur)
        {
            throw new NotImplementedException();
        }

        public List<DTODaftarBarang> GetDaftarPembelianBarang(string idtransaksi)
        {
            List<DTODaftarBarang> Detail = new();

            string query = @"SELECT B.BARIS,B.PRODUCT_ID,B.KODE_BARANG,B.NAMA_BARANG,B.SATUAN,B.QUANTITY,B.HARGA_BELI,M.PRICE HARGA_JUAL,B.BRUTO,B.POTONGAN,B.TOTAL FROM  POS_PEMBELIANDETAIL B
JOIN POS_PRODUCT M ON M.PRODUCTID=B.PRODUCT_ID
WHERE B.NO_TRANSAKSI = :idtransaksi ORDER BY B.BARIS";

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
                        BARIS = Convert.ToInt32(reader["BARIS"]),
                        PRODUCTID = Convert.ToInt32(reader["PRODUCT_ID"]),
                        KODE_BARANG = reader["KODE_BARANG"].ToString(),
                        NAMA_BARANG = reader["NAMA_BARANG"].ToString(),
                        SATUAN = reader["SATUAN"].ToString(),
                        JUMLAH_BARANG = Convert.ToInt32(reader["QUANTITY"]),
                        HPP = Convert.ToDecimal(reader["HARGA_BELI"]),
                        HARGA_BARANG = Convert.ToDecimal(reader["HARGA_JUAL"]),
                        BRUTO = Convert.ToDecimal(reader["BRUTO"]),
                        POTONGAN = Convert.ToDecimal(reader["POTONGAN"]),
                        TOTAL_HARGA = Convert.ToDecimal(reader["TOTAL"])
                    };

                    Detail.Add(DaftarBarang);
                }
            }

            return Detail;
        }
    }
}
