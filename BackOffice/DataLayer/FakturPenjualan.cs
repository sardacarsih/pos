using BackOffice.Interface;
using BackOffice.Model;
using Dapper;
using DevExpress.XtraMap.Native;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.Text.RegularExpressions;

namespace BackOffice.DataLayer
{
    public class FakturPenjualan : IFakturPenjualan
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

        public List<DTODaftarBarang> GetDaftarBarang(string idtransaksi)
        {
            List<DTODaftarBarang> Detail = new();

            string query = "SELECT BARIS,PRODUCT_ID,BARCODE,KODE_BARANG,NAMA_BARANG,SATUAN,JUMLAH_BARANG,HARGA_BARANG,POTONGAN,BRUTO,TOTAL_HARGA FROM  POS_PENJUALAN_DETAIL WHERE NO_TRANSAKSI = :idtransaksi ORDER BY BARIS";

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
                        PRODUCTID = Convert.ToInt32(reader["PRODUCT_ID"]),
                        BARCODE = reader["BARCODE"].ToString(),
                        KODE_BARANG = reader["KODE_BARANG"].ToString(),
                        NAMA_BARANG = reader["NAMA_BARANG"].ToString(),
                        SATUAN = reader["SATUAN"].ToString(),
                        JUMLAH_BARANG = Convert.ToInt32(reader["JUMLAH_BARANG"]),
                        HARGA_BARANG = Convert.ToDecimal(reader["HARGA_BARANG"]),
                        BRUTO = Convert.ToDecimal(reader["BRUTO"]),
                        POTONGAN = Convert.ToDecimal(reader["POTONGAN"]),
                        TOTAL_HARGA = Convert.ToDecimal(reader["TOTAL_HARGA"])
                    };

                    Detail.Add(DaftarBarang);
                }
            }

            return Detail;
        }

        public void HapusFakturPenjualan(string notransaksi)
        {
            using OracleConnection connection = new(global.connectionString);
            connection.Open();

            // DELETE FAKTUR
            string DeleteQuery = "DELETE FROM POS_PENJUALAN WHERE NO_TRANSAKSI=:notransaksi";
            using OracleCommand DeletedCommand = new(DeleteQuery, connection);
            DeletedCommand.Parameters.Add("notransaksi", notransaksi);
            DeletedCommand.ExecuteNonQuery();
        }
        public void UpdateFakturPenjualan(DTOFakturPenjualanHeader faktur_header, List<DTODaftarBarang> ListItemsPenjualan)
        {
            using OracleConnection connection = new(global.connectionString);

            connection.Execute(@"UPDATE POS_PENJUALAN
                        SET NIK = :newNik, ID_PELANGGAN = :newID,
                            NAMA_PELANGGAN = :newNamaPelanggan,
                            STATUS = :newStatus,
                            UNIT_KERJA = :newUnitKerja
                        WHERE NO_TRANSAKSI = :transactionNumber",
                new
                {
                    newNik = faktur_header.NIK,
                    newID = faktur_header.ID_PELANGGAN,
                    newNamaPelanggan = faktur_header.NAMA_PELANGGAN,
                    newStatus = faktur_header.STATUS,
                    newUnitKerja = faktur_header.UNIT_KERJA,
                    transactionNumber = faktur_header.NO_TRANSAKSI
                });
        }

        public void HapusFakturPenjualanAngsuran(string notransaksi)
        {
            using OracleConnection connection = new(global.connectionString);
            connection.Open();

            // DELETE ANGSURAN KREDIT
            string DeleteQuery = "DELETE FROM POS_KREDIT_ANGSURAN WHERE NO_TRANSAKSI=:notransaksi";
            using OracleCommand DeletedCommand = new(DeleteQuery, connection);
            DeletedCommand.Parameters.Add("notransaksi", notransaksi);
            DeletedCommand.ExecuteNonQuery();
        }
        /// <summary>
        /// BATAS
        /// </summary>
        /// <param name="faktur_header"></param>
        /// <param name="ListItemsPenjualan"></param>
        public void InsertFaktur_Penjualan(DTOFakturPenjualanHeader faktur_header, List<DTOFakturPenjualanDetail> ListItemsPenjualan)
        {
            using OracleConnection conn = new(global.connectionString);
            conn.Open();
            OracleTransaction transaction = conn.BeginTransaction();

            try
            {
                // Insert master records
                string insertFakturJual_Master = "INSERT INTO POS_PENJUALAN (NO_TRANSAKSI, TANGGAL, JAM, KASIR, ID_PELANGGAN, NIK, NAMA_PELANGGAN, UNIT_KERJA, STATUS, BRUTO, POTONGAN, TOTAL, JENIS_BAYAR, KET_BAYAR, TENOR, ANGSURAN,PENDING) " +
                                                "VALUES (:NO_TRANSAKSI, :TANGGAL, :JAM, :KASIR, :ID_PELANGGAN, :NIK, :NAMA_PELANGGAN, :UNIT_KERJA, :STATUS, :BRUTO, :POTONGAN, :TOTAL, :JENIS_BAYAR, :KET_BAYAR, :TENOR, :ANGSURAN,:PENDING) " +
                                                "RETURNING ID_PENJUALAN INTO :penjualanId";

                var masterParameters = new DynamicParameters(faktur_header);
                masterParameters.Add("penjualanId", dbType: DbType.Int32, direction: ParameterDirection.Output);

                int rowsAffected = conn.Execute(insertFakturJual_Master, masterParameters, transaction);

                Int32 newPenjualanId = masterParameters.Get<Int32>("penjualanId");

                string insertFakturJual_Detail = "INSERT INTO POS_PENJUALAN_DETAIL (ID_PENJUALAN, NO_TRANSAKSI, BARIS, PRODUCT_ID, KODE_BARANG, BARCODE, NAMA_BARANG, SATUAN, HPP,HARGA_BARANG, JUMLAH_BARANG, BRUTO, POTONGAN, TOTAL_HARGA) " +
                    "VALUES (:ID_PENJUALAN, :NO_TRANSAKSI, :BARIS, :PRODUCT_ID, :KODE_BARANG, :BARCODE, :NAMA_BARANG, :SATUAN, :HPP,:HARGA_BARANG, :JUMLAH_BARANG, :BRUTO, :POTONGAN, :TOTAL_HARGA)";

                foreach (var detail in ListItemsPenjualan)
                {
                    detail.ID_PENJUALAN = newPenjualanId;
                    detail.NO_TRANSAKSI = faktur_header.NO_TRANSAKSI;

                    var detailParameters = new DynamicParameters(detail);
                    conn.Execute(insertFakturJual_Detail, detailParameters, transaction);
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


        public void InsertFaktur_Penjualan_Angsuran(DTOFakturPenjualanHeader faktur_header, List<DTOFakturPenjualanDetail> ListItemsPenjualan, List<DTOAngsuranKreditBarang> DaftarWaktuTagihan)
        {
            using OracleConnection conn = new(global.connectionString);
            conn.Open();
            OracleTransaction transaction = conn.BeginTransaction();

            try
            {
                // Insert master record
                string insertFakturJual_Master = "INSERT INTO POS_PENJUALAN (NO_TRANSAKSI, TANGGAL, JAM, KASIR, ID_PELANGGAN, NIK, NAMA_PELANGGAN, UNIT_KERJA, STATUS, BRUTO, POTONGAN, TOTAL, JENIS_BAYAR, KET_BAYAR, TENOR, ANGSURAN) VALUES " +
                                                "(:NO_TRANSAKSI, :TANGGAL, :JAM, :KASIR, :ID_PELANGGAN, :NIK, :NAMA_PELANGGAN, :UNIT_KERJA, :STATUS, :BRUTO, :POTONGAN, :TOTAL, :JENIS_BAYAR, :KET_BAYAR, :TENOR, :ANGSURAN) RETURNING ID_PENJUALAN INTO :penjualanId";

                var masterParameters = new DynamicParameters(faktur_header);
                masterParameters.Add("penjualanId", dbType: DbType.Int32, direction: ParameterDirection.Output);

                int rowsAffected = conn.Execute(insertFakturJual_Master, masterParameters, transaction);

                Int32 newPenjualanId = masterParameters.Get<Int32>("penjualanId");

                // Insert sales details
                string insertFakturJual_Detail = "INSERT INTO POS_PENJUALAN_DETAIL (ID_PENJUALAN, NO_TRANSAKSI, BARIS, PRODUCT_ID, KODE_BARANG, BARCODE, NAMA_BARANG, SATUAN, HPP,HARGA_BARANG, JUMLAH_BARANG, BRUTO, POTONGAN, TOTAL_HARGA) " +
                                                 "VALUES (:ID_PENJUALAN, :NO_TRANSAKSI, :BARIS, :PRODUCT_ID, :KODE_BARANG, :BARCODE, :NAMA_BARANG, :SATUAN, :HPP,:HARGA_BARANG, :JUMLAH_BARANG, :BRUTO, :POTONGAN, :TOTAL_HARGA)";

                foreach (var detail in ListItemsPenjualan)
                {
                    detail.ID_PENJUALAN = newPenjualanId;
                    detail.NO_TRANSAKSI = faktur_header.NO_TRANSAKSI;
                    var detailParameters = new DynamicParameters(detail);
                    conn.Execute(insertFakturJual_Detail, detailParameters, transaction);
                }

                // Insert credit installment information
                string insertTagihanKredit = "INSERT INTO POS_KREDIT_ANGSURAN (PERIODE, NO_TRANSAKSI, TANGGALJATUHTEMPO, ANGSURANKE, SALDOAWAL,ANGSURAN,SALDOAKHIR) " +
                                             "VALUES (:PERIODE, :NO_TRANSAKSI, :TANGGALJATUHTEMPO, :ANGSURANKE, :SALDOAWAL,:ANGSURAN,:SALDOAKHIR)";

                foreach (var detail in DaftarWaktuTagihan)
                {
                    detail.NO_TRANSAKSI = faktur_header.NO_TRANSAKSI;
                    var detailParameters = new DynamicParameters(detail);
                    conn.Execute(insertTagihanKredit, detailParameters, transaction);
                }

                transaction.Commit();
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw; // Rethrow the exception to be handled by the caller
            }
        }

        

        public void UpdateTransactionNumber(string transactionNumber)
        {
            int nomorbaru = 0;
            int nomorexist = 0;
            using OracleConnection connection = new(global.connectionString);
            connection.Open();

            // Check if record exists
            string checkQuery = "SELECT COUNT(*) FROM nomor_transaksi WHERE kode = 'PENJUALAN'";
            using OracleCommand checkCommand = new(checkQuery, connection);
            int count = Convert.ToInt32(checkCommand.ExecuteScalar());

            if (count == 0)
            {
                // Insert new record
                string insertQuery = "INSERT INTO nomor_transaksi (kode, nomor) VALUES ('PENJUALAN', :nomor)";
                using OracleCommand insertCommand = new(insertQuery, connection);
                insertCommand.Parameters.Add("nomor", transactionNumber);
                insertCommand.ExecuteNonQuery();
            }
            else
            {
                // Check existing transaction number
                string selectQuery = "SELECT nomor FROM nomor_transaksi WHERE kode = 'PENJUALAN'";
                using OracleCommand selectCommand = new(selectQuery, connection);
                string existingTransactionNumber = selectCommand.ExecuteScalar()?.ToString();

                // Remove non-numeric characters from the transactionNumber
                string numericTransactionExist = Regex.Replace(existingTransactionNumber, @"[^\d]", "");
                nomorexist = int.Parse(numericTransactionExist);
                string numericTransactionNew = Regex.Replace(transactionNumber, @"[^\d]", "");
                nomorbaru = int.Parse(numericTransactionNew);


                if (nomorbaru > nomorexist)
                {
                    // Update existing record
                    string updateQuery = "UPDATE nomor_transaksi SET nomor = :nomor WHERE kode = 'PENJUALAN'";
                    using OracleCommand updateCommand = new(updateQuery, connection);
                    updateCommand.Parameters.Add("nomor", transactionNumber);
                    updateCommand.ExecuteNonQuery();
                }
                // If existingTransactionNumber is already the same as transactionNumber, no update is needed.


            }
        }

        public DataSet Tagihan_Periode(int p_indextagihan, DateTime p_tglAngsuran, DateTime p_daritanggal, DateTime p_sampaitanggal)
        {
            using OracleConnection conn = new(global.connectionString);
            using OracleCommand _command = new("TAGIHAN.TAGIHAN_PERIODE", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            //conn.Open();
            _command.Parameters.Add("rekap", OracleDbType.RefCursor).Direction = ParameterDirection.Output;
            _command.Parameters.Add("tagihan_waserda", OracleDbType.RefCursor).Direction = ParameterDirection.Output;
            _command.Parameters.Add("tagihan_angsuran", OracleDbType.RefCursor).Direction = ParameterDirection.Output;

            _command.Parameters.Add(":p_indextagihan", OracleDbType.Int16).Value = p_indextagihan;
            _command.Parameters.Add(":p_tglAngsuran", OracleDbType.Date).Value = p_tglAngsuran;
            _command.Parameters.Add(":p_daritanggal", OracleDbType.Date).Value = p_daritanggal;
            _command.Parameters.Add(":p_sampaitanggal", OracleDbType.Date).Value = p_sampaitanggal;

            OracleCommandBuilder sqlcmdbuilder = new ();
            OracleDataAdapter sqlAdapter = new ();
            sqlcmdbuilder.DataAdapter = sqlAdapter;
            sqlAdapter.SelectCommand = _command;
            DataSet _ds = new DataSet();
            _ds.Clear();
            //Get the data in disconnected mode
            sqlAdapter.Fill(_ds);

            // Assuming you have a DataSet named "myDataSet" and an index of 0 for the DataTable you want to rename
            _ds.Tables[0].TableName = "rekap";
            _ds.Tables[1].TableName = "tagihan_waserda";
            _ds.Tables[2].TableName = "tagihan_angsuran";
            // return dataset result
            return _ds;
        }

        public DataSet Tagihan_ALL(int p_periode, int p_remise)
        {
            using OracleConnection conn = new(global.connectionString);
            using OracleCommand _command = new("FINANCE.TAGIHAN_ALL", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            //conn.Open();
            _command.Parameters.Add("rekap_UK", OracleDbType.RefCursor).Direction = ParameterDirection.Output;
            _command.Parameters.Add("rekap_NIK", OracleDbType.RefCursor).Direction = ParameterDirection.Output;
           // _command.Parameters.Add("tagihan_waserda", OracleDbType.RefCursor).Direction = ParameterDirection.Output;

            _command.Parameters.Add(":p_periode", OracleDbType.Int16).Value = p_periode;
            _command.Parameters.Add(":p_remise", OracleDbType.Int16).Value = p_remise;

            OracleCommandBuilder sqlcmdbuilder = new();
            OracleDataAdapter sqlAdapter = new();
            sqlcmdbuilder.DataAdapter = sqlAdapter;
            sqlAdapter.SelectCommand = _command;
            DataSet _ds = new();
            _ds.Clear();
            //Get the data in disconnected mode
            sqlAdapter.Fill(_ds);

            // Assuming you have a DataSet named "myDataSet" and an index of 0 for the DataTable you want to rename
            _ds.Tables[0].TableName = "rekap_UK";
            _ds.Tables[1].TableName = "rekap_NIK";
            //_ds.Tables[2].TableName = "tagihan_waserda";
            // return dataset result
            return _ds;
        }

        public void Tutup_Buku(int p_periode, int p_remise, DateTime p_tglAngsuran, DateTime p_daritanggal, DateTime p_sampaitanggal)
        {
            using OracleConnection conn = new(global.connectionString);
            using OracleCommand _command = new("TUTUP_BUKU.TUTUP_BUKU_PERIODE", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            conn.Open();
            _command.Parameters.Add(":p_periode", OracleDbType.Int16).Value = p_periode;
            _command.Parameters.Add(":p_remise", OracleDbType.Int16).Value = p_remise;
            _command.Parameters.Add(":p_tglAngsuran", OracleDbType.Date).Value = p_tglAngsuran;
            _command.Parameters.Add(":p_daritanggal", OracleDbType.Date).Value = p_daritanggal;
            _command.Parameters.Add(":p_sampaitanggal", OracleDbType.Date).Value = p_sampaitanggal;
            _command.ExecuteNonQuery();
        }

        public DataSet Penjualan_Tunai(int p_periode, int p_remise)
        {
            using OracleConnection conn = new(global.connectionString);
            using OracleCommand _command = new("FINANCE.TAGIHAN_ALL", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            //conn.Open();
            _command.Parameters.Add("rekap_UK", OracleDbType.RefCursor).Direction = ParameterDirection.Output;
            _command.Parameters.Add("rekap_NIK", OracleDbType.RefCursor).Direction = ParameterDirection.Output;
            _command.Parameters.Add("tagihan_waserda", OracleDbType.RefCursor).Direction = ParameterDirection.Output;

            _command.Parameters.Add(":p_periode", OracleDbType.Int16).Value = p_periode;
            _command.Parameters.Add(":p_remise", OracleDbType.Int16).Value = p_remise;

            OracleCommandBuilder sqlcmdbuilder = new();
            OracleDataAdapter sqlAdapter = new();
            sqlcmdbuilder.DataAdapter = sqlAdapter;
            sqlAdapter.SelectCommand = _command;
            DataSet _ds = new();
            _ds.Clear();
            //Get the data in disconnected mode
            sqlAdapter.Fill(_ds);

            // Assuming you have a DataSet named "myDataSet" and an index of 0 for the DataTable you want to rename
            _ds.Tables[0].TableName = "rekap_UK";
            _ds.Tables[1].TableName = "rekap_NIK";
            _ds.Tables[2].TableName = "tagihan_waserda";
            // return dataset result
            return _ds;
        }

        public DTOProductInfo RetrieveProductInfo(string barcode)
        {
            DTOProductInfo productInfo = new();

            using (OracleConnection connection = new(global.connectionString))
            {
                connection.Open();

                string sql = "SELECT PRODUCTID, KODE_ITEM, PRODUCTNAME, SATUAN, PRICE, BELI FROM POS_PRODUCT WHERE BARCODE = :barcode";
                using (OracleCommand command = new(sql, connection))
                {
                    command.Parameters.Add("barcode", OracleDbType.Varchar2).Value = barcode;

                    using OracleDataReader reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        productInfo.ProductId = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                        productInfo.KodeItem = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
                        productInfo.ProductName = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);
                        productInfo.Satuan = reader.IsDBNull(3) ? string.Empty : reader.GetString(3);
                        productInfo.Price = reader.IsDBNull(4) ? 0.0m : reader.GetDecimal(4);
                        productInfo.Hpp = reader.IsDBNull(5) ? 0.0m : reader.GetDecimal(5);
                    }
                }

                connection.Close();
            }

            return productInfo;
        }

        public List<DTOLabaRugi> GetLabaRugi(int year)
        {
            using IDbConnection dbConnection = new OracleConnection(global.connectionString);
            dbConnection.Open();

            string sqlQuery = @"
               SELECT
                        EXTRACT(MONTH FROM m.tanggal) AS BulanInt,
                        TO_CHAR(m.tanggal, 'Month') AS Bulan,
                        m.no_transaksi,
                        m.nik,
                        m.nama_pelanggan,
                        SUM(d.total_harga) AS penjualan,
                        SUM(D.JUMLAH_BARANG*d.hpp) AS hpp,
                        SUM(d.total_harga) - SUM(D.JUMLAH_BARANG*d.hpp) AS laba
                    FROM
                        pos_penjualan m
                    JOIN
                        pos_penjualan_detail d ON d.no_transaksi = m.no_transaksi
                    WHERE
                        EXTRACT(YEAR FROM m.tanggal) = :Year
                    GROUP BY
                        EXTRACT(MONTH FROM m.tanggal),
                        m.no_transaksi,
                        m.nik,
                        m.nama_pelanggan,
                        TO_CHAR(m.tanggal, 'Month')
                    ORDER BY
                        EXTRACT(MONTH FROM m.tanggal), m.no_transaksi";

            var parameters = new { Year = year };
            List<DTOLabaRugi> LabaRugi = dbConnection.Query<DTOLabaRugi>(sqlQuery, parameters).AsList();

            return LabaRugi;
        }

        public List<DTOLabaRugiAnalisa> AnalisaLabaRugi(DateTime date1, DateTime date2)
        {
            using IDbConnection dbConnection = new OracleConnection(global.connectionString);
            dbConnection.Open();

            string sqlQuery = @"
               SELECT
                     m.no_transaksi,
                       m.tanggal,   
                        D.KODE_BARANG,D.NAMA_BARANG,
                        SUM(D.JUMLAH_BARANG) AS QTY,
                        SUM(d.total_harga) AS penjualan,
                        SUM(d.hpp) AS hpp,
                        SUM(d.total_harga) - SUM(d.hpp*D.JUMLAH_BARANG) AS laba,
                        ROUND(((SUM(d.total_harga) - SUM(d.hpp*D.JUMLAH_BARANG)) / NULLIF(SUM(d.total_harga), 0)),2) AS PERSEN
                    FROM
                        pos_penjualan m
                    JOIN
                        pos_penjualan_detail d ON d.no_transaksi = m.no_transaksi
                    WHERE
                       M.TANGGAL BETWEEN :DARI AND :SAMPAI
                    GROUP BY
                        m.tanggal, m.no_transaksi,D.KODE_BARANG,D.NAMA_BARANG";

            var parameters = new { DARI = date1, SAMPAI = date2 };
            List<DTOLabaRugiAnalisa> LabaRugi = dbConnection.Query<DTOLabaRugiAnalisa>(sqlQuery, parameters).AsList();

            return LabaRugi;
        }
    }
}
