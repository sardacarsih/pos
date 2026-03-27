using BackOffice.Interface;
using BackOffice.Model;
using BackOffice.UC;
using Dapper;
using Oracle.ManagedDataAccess.Client;
using Serilog;
using System;
using System.Data;
using System.Linq;

namespace BackOffice.DataLayer
{
    public class Finance : IFinance
    {
        public List<DTOPinjamanMaster> DaftarPinjaman(int bulan, int tahun)
        {
            List<DTOPinjamanMaster> Master = new();

            string query = "SELECT NO_TRANSAKSI,TANGGAL,NIK,NAMA_PELANGGAN,STATUS,UNIT_KERJA,PINJAMAN,BUNGA,TENOR,ANGSURAN,ISLUNAS LUNAS FROM FIN_PINJAMAN WHERE EXTRACT(MONTH FROM TANGGAL) = :p_bulan AND EXTRACT(YEAR FROM TANGGAL) = :p_tahun";

            using (OracleConnection connection = new(global.connectionString))
            {
                connection.Open();

                using OracleCommand command = new(query, connection);
                command.Parameters.Add(":p_bulan", OracleDbType.Int32).Value = bulan;
                command.Parameters.Add(":p_tahun", OracleDbType.Int32).Value = tahun;
                using OracleDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    DTOPinjamanMaster DaftarPinjaman = new()
                    {
                        NO_TRANSAKSI = reader["NO_TRANSAKSI"].ToString(),
                        TANGGAL = Convert.ToDateTime(reader["TANGGAL"]),
                        NIK = reader["NIK"].ToString(),
                        NAMA_PELANGGAN = reader["NAMA_PELANGGAN"].ToString(),
                        STATUS = reader["STATUS"].ToString(),
                        UNIT_KERJA = reader["UNIT_KERJA"].ToString(),
                        PINJAMAN = Convert.ToDecimal(reader["PINJAMAN"]),
                        TENOR = Convert.ToInt32(reader["TENOR"]),
                        BUNGA = Convert.ToDecimal(reader["BUNGA"]),
                        ANGSURAN = Convert.ToDecimal(reader["ANGSURAN"]),
                        ISLUNAS = reader["LUNAS"].ToString()
                    };

                    // Fetch products for the order
                    List<DTOPinjamanDetail> idtransaksi = GetDaftarAngsuran(reader["NO_TRANSAKSI"].ToString());
                    DaftarPinjaman.Details = idtransaksi;

                    Master.Add(DaftarPinjaman);
                }
            }

            return Master;
        }
        public  List<DTOPinjamanDetail> GetDaftarAngsuran(string idtransaksi)
        {
            List<DTOPinjamanDetail> Detail = new();

            string query = "SELECT NO_TRANSAKSI,TANGGALJATUHTEMPO,ANGSURANKE,SALDOAWAL,POKOK,BUNGA,ANGSURAN,SALDOAKHIR,ISTAGIH TAGIH from FIN_PINJAMAN_DETAIL WHERE NO_TRANSAKSI = :idtransaksi";

            using (OracleConnection connection = new(global.connectionString))
            {
                connection.Open();

                using OracleCommand command = new(query, connection);
                command.Parameters.Add(":idtransaksi", OracleDbType.Varchar2).Value = idtransaksi;

                using OracleDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    DTOPinjamanDetail angsuran = new()
                    {
                        NO_TRANSAKSI = reader["NO_TRANSAKSI"].ToString(),
                        TANGGALJATUHTEMPO = Convert.ToDateTime(reader["TANGGALJATUHTEMPO"]),
                        ANGSURANKE = Convert.ToInt32(reader["ANGSURANKE"]),
                        SALDOAWAL = Convert.ToDecimal(reader["SALDOAWAL"]),
                        POKOK = Convert.ToDecimal(reader["POKOK"]),
                        BUNGA = Convert.ToDecimal(reader["BUNGA"]),
                        ANGSURAN = Convert.ToDecimal(reader["ANGSURAN"]),
                        SALDOAKHIR = Convert.ToDecimal(reader["SALDOAKHIR"]),
                        ISTAGIH = reader["TAGIH"].ToString()
                    };

                    Detail.Add(angsuran);
                }
            }

            return Detail;
        }
        public DataSet Daftar_Pinjaman(int p_bulan, int p_tahun)
        {
            using OracleConnection conn = new(global.connectionString);
            using OracleCommand _command = new("FINANCE.DAFTAR_PINJAMAN", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            //conn.Open();
            _command.Parameters.Add("Pinjaman_Master", OracleDbType.RefCursor).Direction = ParameterDirection.Output;
            _command.Parameters.Add("Pinjaman_Detail", OracleDbType.RefCursor).Direction = ParameterDirection.Output;
           
            _command.Parameters.Add(":p_bulan", OracleDbType.Int16).Value = p_bulan;
            _command.Parameters.Add(":p_tahun", OracleDbType.Int16).Value = p_tahun;

            OracleCommandBuilder sqlcmdbuilder = new();
            OracleDataAdapter sqlAdapter = new();
            sqlcmdbuilder.DataAdapter = sqlAdapter;
            sqlAdapter.SelectCommand = _command;
            DataSet _ds = new();
            _ds.Clear();
            //Get the data in disconnected mode
            sqlAdapter.Fill(_ds);

            // Assuming you have a DataSet named "myDataSet" and an index of 0 for the DataTable you want to rename
            _ds.Tables[0].TableName = "Pinjaman_Master";
            _ds.Tables[1].TableName = "Pinjaman_Detail";
            // return dataset result
            return _ds;
        }

        public List<DTOPinjamanMaster> Daftar_Pinjaman_List(int p_bulan, int p_tahun)
        {
            List<DTOPinjamanMaster> result = new();

            using (OracleConnection conn = new(global.connectionString))
            {
                using (OracleCommand _command = new OracleCommand("FINANCE.DAFTAR_PINJAMAN", conn))
                {
                    _command.CommandType = CommandType.StoredProcedure;

                    _command.Parameters.Add(new OracleParameter("Pinjaman_Master", OracleDbType.RefCursor, ParameterDirection.Output));
                    _command.Parameters.Add(new OracleParameter("Pinjaman_Detail", OracleDbType.RefCursor, ParameterDirection.Output));
                    _command.Parameters.Add(new OracleParameter("p_bulan", OracleDbType.Int32)).Value = p_bulan;
                    _command.Parameters.Add(new OracleParameter("p_tahun", OracleDbType.Int32)).Value = p_tahun;

                    conn.Open();

                    using (OracleDataReader reader = _command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                DTOPinjamanMaster pinjamanMaster = new DTOPinjamanMaster()
                                {
                                    NO_TRANSAKSI = reader["NO_TRANSAKSI"].ToString(),
                                    TANGGAL = Convert.ToDateTime(reader["TANGGAL"]),
                                    NIK = reader["NIK"].ToString(),
                                    NAMA_PELANGGAN = reader["NAMA_PELANGGAN"].ToString(),
                                    STATUS = reader["STATUS"].ToString(),
                                    UNIT_KERJA = reader["UNIT_KERJA"].ToString(),
                                    PINJAMAN = Convert.ToDecimal(reader["PINJAMAN"]),
                                    TENOR = Convert.ToInt32(reader["TENOR"]),
                                    BUNGA = Convert.ToDecimal(reader["BUNGA"]),
                                    ANGSURAN = Convert.ToDecimal(reader["ANGSURAN"]),
                                    ISLUNAS = reader["LUNAS"].ToString()
                                };

                                result.Add(pinjamanMaster);
                            }

                            reader.NextResult();

                            while (reader.Read())
                            {
                                DTOPinjamanDetail pinjamanDetail = new DTOPinjamanDetail()
                                {
                                    NO_TRANSAKSI = reader["NO_TRANSAKSI"].ToString(),
                                    TANGGALJATUHTEMPO = Convert.ToDateTime(reader["TANGGALJATUHTEMPO"]),
                                    ANGSURANKE = Convert.ToInt32(reader["ANGSURANKE"]),
                                    SALDOAWAL = Convert.ToDecimal(reader["SALDOAWAL"]),
                                    POKOK = Convert.ToDecimal(reader["POKOK"]),
                                    BUNGA = Convert.ToDecimal(reader["BUNGA"]),
                                    ANGSURAN = Convert.ToDecimal(reader["ANGSURAN"]),
                                    SALDOAKHIR = Convert.ToDecimal(reader["SALDOAKHIR"]),
                                    ISTAGIH = reader["TAGIH"].ToString()
                                };

                                string idPinjaman = reader["NO_TRANSAKSI"].ToString();
                                DTOPinjamanMaster pinjamanMaster = result.Find(pm => pm.NO_TRANSAKSI == idPinjaman);
                                pinjamanMaster?.Details.Add(pinjamanDetail);
                            }
                        }
                    }
                }
            }

            return result;
        }


        public void InsertFaktur_PinjamanTunai(DTOPinjaman pinjaman, List<DTOSimulasiAngsuran> details)
        {
            using OracleConnection conn = new(global.connectionString);
            conn.Open();

            using OracleTransaction transaction = conn.BeginTransaction();
            try
            {
                string insertMasterQuery = "INSERT INTO FIN_PINJAMAN (NO_TRANSAKSI, TANGGAL, KASIR, ID_PELANGGAN, NIK, NAMA_PELANGGAN, STATUS, UNIT_KERJA, PINJAMAN, TENOR, BUNGA, ANGSURAN, KETERANGAN) " +
                                           "VALUES (:NO_TRANSAKSI, :TANGGAL, :KASIR, :ID_PELANGGAN, :NIK, :NAMA_PELANGGAN, :STATUS, :UNIT_KERJA, :PINJAMAN, :TENOR, :BUNGA, :ANGSURAN, :KETERANGAN)";
                conn.Execute(insertMasterQuery, pinjaman, transaction);

                string insertDetailQuery = "INSERT INTO FIN_PINJAMAN_DETAIL (PERIODE,NO_TRANSAKSI, TANGGALJATUHTEMPO, ANGSURANKE, SALDOAWAL, POKOK, BUNGA, ANGSURAN, SALDOAKHIR) " +
                                           "VALUES (:PERIODE,:NO_TRANSAKSI, :TANGGALJATUHTEMPO, :ANGSURANKE, :SALDOAWAL, :POKOK, :BUNGA, :ANGSURAN, :SALDOAKHIR)";
                conn.Execute(insertDetailQuery, details, transaction);

                transaction.Commit();
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw; // Rethrow the exception to be handled by the caller
            }
        }

        public DataSet Tagihan_Pinjaman_dan_Kredit(int p_bulan, int p_tahun)
        {
            using OracleConnection conn = new(global.connectionString);
            using OracleCommand _command = new("FINANCE.DAFTAR_TAGIHAN_PINJAMAN_KREDIT", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            //conn.Open();
            _command.Parameters.Add("pinjaman", OracleDbType.RefCursor).Direction = ParameterDirection.Output;
            _command.Parameters.Add("kredit", OracleDbType.RefCursor).Direction = ParameterDirection.Output;

            _command.Parameters.Add(":p_bulan", OracleDbType.Int16).Value = p_bulan;
            _command.Parameters.Add(":p_tahun", OracleDbType.Int16).Value = p_tahun;

            OracleCommandBuilder sqlcmdbuilder = new();
            OracleDataAdapter sqlAdapter = new();
            sqlcmdbuilder.DataAdapter = sqlAdapter;
            sqlAdapter.SelectCommand = _command;
            DataSet _ds = new();
            _ds.Clear();
            //Get the data in disconnected mode
            sqlAdapter.Fill(_ds);

            // Assuming you have a DataSet named "myDataSet" and an index of 0 for the DataTable you want to rename
            _ds.Tables[0].TableName = "pinjaman";
            _ds.Tables[1].TableName = "kredit";
            // return dataset result
            return _ds;
        }

        public void EditFakturPinjaman(DTOPinjaman PinjamanTunaiMaster)
        {
            using OracleConnection connection = new(global.connectionString);

            connection.Execute(@"UPDATE FIN_PINJAMAN
                        SET NIK = :newNik, ID_PELANGGAN = :newID,
                            NAMA_PELANGGAN = :newNamaPelanggan,
                            STATUS = :newStatus,
                            UNIT_KERJA = :newUnitKerja
                        WHERE NO_TRANSAKSI = :transactionNumber",
                new
                {
                    newNik = PinjamanTunaiMaster.NIK,
                    newID = PinjamanTunaiMaster.ID_PELANGGAN,
                    newNamaPelanggan = PinjamanTunaiMaster.NAMA_PELANGGAN,
                    newStatus = PinjamanTunaiMaster.STATUS,
                    newUnitKerja = PinjamanTunaiMaster.UNIT_KERJA,
                    transactionNumber = PinjamanTunaiMaster.NO_TRANSAKSI
                });
        }

        public List<FinBayarVia> PembayaranVia(string kode  )
        {
            // Define the SQL query
            string query = "SELECT ID, KODE,NAMA,NOMOR_REK, ACKODE FROM FIN_BAYAR_VIA where KATEGORI=:kode order by id";

            try
            {
                // Execute the query using Dapper
                using (IDbConnection dbConnection = new OracleConnection(global.connectionString))
                {
                    dbConnection.Open();

                    IEnumerable<FinBayarVia> result = dbConnection.Query<FinBayarVia>(query,new {kode=kode});

                    // Return the result for further processing
                    return result.ToList();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to retrieve payment via list");
                // Handle the exception as needed
                return new List<FinBayarVia>(); // Or throw the exception if appropriate
            }
        }

        public List<FinJenisBayar> JenisBayar()
        {
            // Define the SQL query
            string query = "SELECT ID, NAMA, ACKODE FROM FIN_JENISBAYAR order by id";

            try
            {
                // Execute the query using Dapper
                using (IDbConnection dbConnection = new OracleConnection(global.connectionString))
                {
                    dbConnection.Open();

                    IEnumerable<FinJenisBayar> result = dbConnection.Query<FinJenisBayar>(query);

                    // Return the result for further processing
                    return result.ToList();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to retrieve payment type list");
                // Handle the exception as needed
                return new List<FinJenisBayar>(); // Or throw the exception if appropriate
            }
        }

    }
}
