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
            string headerQuery = @"SELECT B.NO_TRANSAKSI,B.TANGGAL,B.SUPPLIER_ID,S.NAMA,B.BRUTO,B.POTONGAN,B.TOTAL,B.TERMIN FROM POS_PEMBELIAN B
                            JOIN  POS_SUPPLIER  S ON S.KODE=B.SUPPLIER_ID
                            WHERE EXTRACT(MONTH FROM B.TANGGAL) = :p_bulan AND EXTRACT(YEAR FROM B.TANGGAL) = :p_tahun
                            ORDER BY B.TANGGAL";

            string detailQuery = @"SELECT D.NO_TRANSAKSI,D.BARIS,D.PRODUCT_ID,D.KODE_BARANG,D.NAMA_BARANG,D.SATUAN,D.QUANTITY,D.HARGA_BELI,D.BRUTO,D.POTONGAN,D.TOTAL
                            FROM POS_PEMBELIANDETAIL D
                            JOIN POS_PEMBELIAN B ON B.NO_TRANSAKSI=D.NO_TRANSAKSI
                            WHERE EXTRACT(MONTH FROM B.TANGGAL) = :p_bulan AND EXTRACT(YEAR FROM B.TANGGAL) = :p_tahun
                            ORDER BY D.NO_TRANSAKSI, D.BARIS";

            using OracleConnection connection = new(global.connectionString);
            connection.Open();

            // Query 1: Fetch all headers
            List<DTOFakturPembelian_Header> Master = new();
            using (OracleCommand command = new(headerQuery, connection))
            {
                command.Parameters.Add(":p_bulan", OracleDbType.Int32).Value = bulan;
                command.Parameters.Add(":p_tahun", OracleDbType.Int32).Value = tahun;
                using OracleDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    Master.Add(new DTOFakturPembelian_Header
                    {
                        NO_TRANSAKSI = reader["NO_TRANSAKSI"].ToString(),
                        TANGGAL = Convert.ToDateTime(reader["TANGGAL"]),
                        SUPPLIER_ID = reader["SUPPLIER_ID"].ToString(),
                        NAMA_SUPPLIER = reader["NAMA"].ToString(),
                        BRUTO = Convert.ToDecimal(reader["BRUTO"]),
                        POTONGAN = Convert.ToDecimal(reader["POTONGAN"]),
                        TOTAL = Convert.ToDecimal(reader["TOTAL"]),
                        TERMIN = Convert.ToInt32(reader["TERMIN"]),
                        Details = new List<DTOFakturPembelianDetail>()
                    });
                }
            }

            // Query 2: Fetch all details in one query
            List<DTOFakturPembelianDetail> allDetails = new();
            using (OracleCommand command = new(detailQuery, connection))
            {
                command.Parameters.Add(":p_bulan", OracleDbType.Int32).Value = bulan;
                command.Parameters.Add(":p_tahun", OracleDbType.Int32).Value = tahun;
                using OracleDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    allDetails.Add(new DTOFakturPembelianDetail
                    {
                        NO_TRANSAKSI = reader["NO_TRANSAKSI"].ToString(),
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
                    });
                }
            }

            // Group details by NO_TRANSAKSI and assign to headers
            var detailLookup = allDetails.GroupBy(d => d.NO_TRANSAKSI).ToDictionary(g => g.Key, g => g.ToList());
            foreach (var header in Master)
            {
                if (detailLookup.TryGetValue(header.NO_TRANSAKSI, out var details))
                    header.Details = details;
            }

            return Master;
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
                //update hpp dan harga jual - hanya jika harga beli dan harga jual > 0
                string updateharga = "UPDATE pos_product " +
                     "SET beli = :Beli, price = :Jual, lastupdate = SYSDATE " +
                     "WHERE productid = :ProductID";

                foreach (var product in ListItemPembelian)
                {
                    if (product.HARGA_BELI <= 0 || product.HARGA_JUAL <= 0)
                        continue;

                    var parameters = new DynamicParameters(product);
                    parameters.Add("Beli", product.HARGA_BELI, DbType.Decimal, ParameterDirection.Input);
                    parameters.Add("Jual", product.HARGA_JUAL, DbType.Decimal, ParameterDirection.Input);
                    parameters.Add("ProductID", product.PRODUCT_ID, DbType.Int32, ParameterDirection.Input);
                    conn.Execute(updateharga, parameters, transaction);
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
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
            OracleTransaction transaction = connection.BeginTransaction();

            try
            {
                // DELETE detail records first
                string deleteDetailQuery = "DELETE FROM POS_PEMBELIANDETAIL WHERE NO_TRANSAKSI = :nomor";
                using OracleCommand deleteDetailCommand = new(deleteDetailQuery, connection);
                deleteDetailCommand.Transaction = transaction;
                deleteDetailCommand.Parameters.Add("nomor", no_faktur);
                deleteDetailCommand.ExecuteNonQuery();

                // DELETE header record
                string deleteQuery = "DELETE FROM POS_PEMBELIAN WHERE NO_TRANSAKSI = :nomor";
                using OracleCommand deleteCommand = new(deleteQuery, connection);
                deleteCommand.Transaction = transaction;
                deleteCommand.Parameters.Add("nomor", no_faktur);
                deleteCommand.ExecuteNonQuery();

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }


        public void UpdateFaktur_Pembelian(DTOFakturPembelian_Header faktur_header, List<DTOFakturPembelianDetail> ListItemPembelian)
        {
            using OracleConnection conn = new(global.connectionString);
            conn.Open();
            OracleTransaction transaction = conn.BeginTransaction();

            try
            {
                // DELETE detail records first
                string deleteDetailQuery = "DELETE FROM POS_PEMBELIANDETAIL WHERE NO_TRANSAKSI = :nomor";
                using (OracleCommand deleteDetailCommand = new(deleteDetailQuery, conn))
                {
                    deleteDetailCommand.Transaction = transaction;
                    deleteDetailCommand.Parameters.Add("nomor", faktur_header.NO_TRANSAKSI);
                    deleteDetailCommand.ExecuteNonQuery();
                }

                // DELETE header record
                string deleteQuery = "DELETE FROM POS_PEMBELIAN WHERE NO_TRANSAKSI = :nomor";
                using (OracleCommand deleteCommand = new(deleteQuery, conn))
                {
                    deleteCommand.Transaction = transaction;
                    deleteCommand.Parameters.Add("nomor", faktur_header.NO_TRANSAKSI);
                    deleteCommand.ExecuteNonQuery();
                }

                // Insert master records
                string insertFakturBeli_Master = "INSERT INTO POS_PEMBELIAN (NO_TRANSAKSI, TANGGAL, SUPPLIER_ID, BRUTO, POTONGAN, TOTAL, TERMIN,USERID) " +
                                                "VALUES (:NO_TRANSAKSI, :TANGGAL, :SUPPLIER_ID,:BRUTO, :POTONGAN, :TOTAL, :TERMIN, :USERID) " +
                                                "RETURNING PURCHASE_ID INTO :PembelianId";

                var masterParameters = new DynamicParameters(faktur_header);
                masterParameters.Add("PembelianId", dbType: DbType.Int32, direction: ParameterDirection.Output);
                conn.Execute(insertFakturBeli_Master, masterParameters, transaction);

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

                // update hpp dan harga jual - hanya jika harga beli dan harga jual > 0
                string updateharga = "UPDATE pos_product " +
                     "SET beli = :Beli, price = :Jual, lastupdate = SYSDATE " +
                     "WHERE productid = :ProductID";

                foreach (var product in ListItemPembelian)
                {
                    if (product.HARGA_BELI <= 0 || product.HARGA_JUAL <= 0)
                        continue;

                    var parameters = new DynamicParameters(product);
                    parameters.Add("Beli", product.HARGA_BELI, DbType.Decimal, ParameterDirection.Input);
                    parameters.Add("Jual", product.HARGA_JUAL, DbType.Decimal, ParameterDirection.Input);
                    parameters.Add("ProductID", product.PRODUCT_ID, DbType.Int32, ParameterDirection.Input);
                    conn.Execute(updateharga, parameters, transaction);
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
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
