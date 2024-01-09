using BackOffice.Interface;
using BackOffice.Model;
using Dapper;
using DevExpress.XtraEditors;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace BackOffice.DataLayer
{
    public class AnggotaRepository : IAnggota
    {
        public List<DTOAnggota> GetAnggotaData()
        {
            List<DTOAnggota> anggotaList = new();

            using (OracleConnection connection = new (global.connectionString))
            {
                string sqlQuery = "SELECT A.NIK, A.NAMA_PELANGGAN, A.STATUS,A.TMK, A.UNIT_KERJA AS KODE_UNIT, K.NAMA AS UNIT_KERJA, " +
                                  "A.LIMIT_HUTANG, A.AKTIF, A.ANGGOTA, A.GAMBAR " +
                                  "FROM FIN_ANGGOTA A " +
                                  "JOIN FIN_UNITKERJA K ON K.KODE = A.UNIT_KERJA "+
                                  "WHERE A.AKTIF='Y'";

                using OracleCommand command = new(sqlQuery, connection);
                connection.Open();
                using OracleDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    DTOAnggota anggota = new()
                    {
                        NIK = reader["NIK"].ToString(),
                        NAMA_PELANGGAN = reader["NAMA_PELANGGAN"].ToString(),
                        STATUS = reader["STATUS"].ToString(),
                        KODE_UNIT = reader["KODE_UNIT"].ToString(),
                        UNIT_KERJA = reader["UNIT_KERJA"].ToString(),
                        LIMIT_HUTANG = Convert.ToDecimal(reader["LIMIT_HUTANG"]),
                        AKTIF = reader["AKTIF"].ToString(),
                        ANGGOTA = reader["ANGGOTA"].ToString()
                    };
                    if (reader["TMK"] != DBNull.Value)
                        anggota.TMK = Convert.ToDateTime(reader["TMK"]);
                    else
                        anggota.TMK = null;
                    if (reader["GAMBAR"] != DBNull.Value)
                        anggota.GAMBAR = (byte[])reader["GAMBAR"];
                    else
                        anggota.GAMBAR = GetDefaultImage();

                    anggotaList.Add(anggota);
                }
            }

            return anggotaList;
        }

        private static byte[] GetDefaultImage()
        {
            string imagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Icons", "defaultimage.png");
            byte[] defaultImage = File.ReadAllBytes(imagePath);
            return defaultImage;
        }

        public void UpdateAnggota(DTOAnggota anggota)
        {
            using OracleConnection connection = new (global.connectionString);
            string updateQuery = "UPDATE FIN_ANGGOTA SET NAMA_PELANGGAN = :NamaPelanggan, TMK = :TMK, GAMBAR = :Gambar, STATUS = :Status, UNIT_KERJA = :KodeUnit, AKTIF = :Aktif, ANGGOTA = :Anggota, LIMIT_HUTANG = :LimitHutang WHERE NIK = :NIK";

            OracleCommand command = new(updateQuery, connection);

            command.Parameters.Add("NamaPelanggan", OracleDbType.Varchar2).Value = anggota.NAMA_PELANGGAN;
            command.Parameters.Add("TMK", OracleDbType.Date).Value = anggota.TMK;
            command.Parameters.Add("Gambar", OracleDbType.Blob).Value = anggota.GAMBAR;
            command.Parameters.Add("Status", OracleDbType.Varchar2).Value = anggota.STATUS;
            command.Parameters.Add("KodeUnit", OracleDbType.Varchar2).Value = anggota.KODE_UNIT;
            command.Parameters.Add("Aktif", OracleDbType.Varchar2).Value = anggota.AKTIF;
            command.Parameters.Add("Anggota", OracleDbType.Varchar2).Value = anggota.ANGGOTA;
            command.Parameters.Add("LimitHutang", OracleDbType.Decimal).Value = anggota.LIMIT_HUTANG;
            command.Parameters.Add("NIK", OracleDbType.Varchar2).Value = anggota.NIK;


            try
            {
                connection.Open();
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                // Handle any exceptions that occur during the update process
                XtraMessageBox.Show("Error updating Anggota: " + ex.Message);
            }
        }
        public void InsertAnggota(DTOAnggota anggota)
        {
            using OracleConnection connection = new(global.connectionString);
            string insertQuery = "INSERT INTO FIN_ANGGOTA (NIK, NAMA_PELANGGAN, TMK, GAMBAR, STATUS, UNIT_KERJA, AKTIF, ANGGOTA, LIMIT_HUTANG,UPDATEAT) " +
                                 "VALUES (:NIK, :NamaPelanggan, :TMK, :Gambar, :Status, :KodeUnit, :Aktif, :Anggota, :LimitHutang,:UpdateAt)";

            OracleCommand command = new(insertQuery, connection);

            command.Parameters.Add("NIK", OracleDbType.Varchar2).Value = anggota.NIK;
            command.Parameters.Add("NamaPelanggan", OracleDbType.Varchar2).Value = anggota.NAMA_PELANGGAN;
            command.Parameters.Add("TMK", OracleDbType.Date).Value = anggota.TMK;
            command.Parameters.Add("Gambar", OracleDbType.Blob).Value = anggota.GAMBAR;
            command.Parameters.Add("Status", OracleDbType.Varchar2).Value = anggota.STATUS;
            command.Parameters.Add("KodeUnit", OracleDbType.Varchar2).Value = anggota.KODE_UNIT;
            command.Parameters.Add("Aktif", OracleDbType.Varchar2).Value = anggota.AKTIF;
            command.Parameters.Add("Anggota", OracleDbType.Varchar2).Value = anggota.ANGGOTA;
            command.Parameters.Add("LimitHutang", OracleDbType.Decimal).Value = anggota.LIMIT_HUTANG;
            command.Parameters.Add("UpdateAt", OracleDbType.Varchar2).Value = DateTime.Today.ToString("dd-MM-yyyy");

            try
            {
                connection.Open();
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                // Handle any exceptions that occur during the insert process
                XtraMessageBox.Show("Error inserting Anggota: " + ex.Message);
            }
        }

        public IEnumerable<DTOAnggota> GetMembersByStatusAndType(string isAnggota)
        {
            using OracleConnection dbConnection = new(global.connectionString);
            dbConnection.Open();

            string sqlQuery = @"
                SELECT A.NIK, A.NAMA_PELANGGAN, A.STATUS, A.TMK, A.UNIT_KERJA AS KODE_UNIT,
                       K.NAMA AS UNIT_KERJA, A.LIMIT_HUTANG, A.AKTIF, A.ANGGOTA, A.GAMBAR
                FROM FIN_ANGGOTA A
                JOIN FIN_UNITKERJA K ON K.KODE = A.UNIT_KERJA
                WHERE A.ANGGOTA = :isAnggota";

            var parameters = new
            {
                isAnggota
            };

            IEnumerable<DTOAnggota> members = dbConnection.Query<DTOAnggota>(sqlQuery, parameters);

            return members;
        }

        public string GenerateNikAnggota(DateTime date)
        {
            // Mendapatkan tahun dari parameter tanggal
            int currentYear = date.Year % 100;

            // Ambil nomor transaksi terakhir dari database untuk tahun saat ini
            string selectQuery = $"SELECT nomor FROM nomor_transaksi WHERE kode = 'ANGGOTA' AND nomor LIKE 'A-{currentYear}%' ORDER BY nomor DESC FETCH FIRST 1 ROWS ONLY";
            using OracleConnection connection = new(global.connectionString);
            connection.Open();
            using OracleCommand selectCommand = new(selectQuery, connection);
            string lastTransactionNumber = selectCommand.ExecuteScalar()?.ToString();

            // Buat nomor transaksi baru untuk tahun saat ini
            string newTransactionNumber;
            if (string.IsNullOrEmpty(lastTransactionNumber))
            {
                newTransactionNumber = $"A-{currentYear.ToString("D2")}-001"; // Jika belum ada nomor transaksi sebelumnya


            }
            else
            {
                int lastNumber = int.Parse(lastTransactionNumber.Substring(lastTransactionNumber.Length - 3));
                int newNumber = lastNumber + 1;
                newTransactionNumber = $"A-{currentYear.ToString("D2")}-{newNumber.ToString("D3")}"; // Format nomor transaksi dengan leading zero
            }

            return newTransactionNumber;
        }

        public void SimpanNomorAnggota(string lastnomor)
        {
            using OracleConnection connection = new(global.connectionString);
            connection.Open();

            // Check if record exists
            string checkQuery = "SELECT COUNT(*) FROM nomor_transaksi WHERE kode = 'ANGGOTA'";
            using OracleCommand checkCommand = new(checkQuery, connection);
            int count = Convert.ToInt32(checkCommand.ExecuteScalar());

            if (count == 0)
            {
                // Insert new record
                string insertQuery = "INSERT INTO nomor_transaksi (kode, nomor) VALUES ('ANGGOTA', :nomor)";
                using OracleCommand insertCommand = new(insertQuery, connection);
                insertCommand.Parameters.Add("nomor", lastnomor);
                insertCommand.ExecuteNonQuery();
            }
            else
            {
                
                    // Update existing record
                    string updateQuery = "UPDATE nomor_transaksi SET nomor = :nomor WHERE kode = 'ANGGOTA'";
                    using OracleCommand updateCommand = new(updateQuery, connection);
                    updateCommand.Parameters.Add("nomor", lastnomor);
                    updateCommand.ExecuteNonQuery();
               
            }
        }

        public List<DTOAnggotaAktif> GetAnggotaAktif()
        {
            string query = @"SELECT A.ID_PELANGGAN, A.NIK, A.NAMA_PELANGGAN, A.UNIT_KERJA,K.NAMA UNITKERJA, A.STATUS, A.LIMIT_HUTANG FROM FIN_ANGGOTA A
                            JOIN FIN_UNITKERJA K ON K.KODE=A.UNIT_KERJA
                            WHERE A.AKTIF='Y' AND A.ANGGOTA='Y' ORDER BY A.NAMA_PELANGGAN";

            using OracleConnection connection = new(global.connectionString);

            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            List<DTOAnggotaAktif> AnggotaList = connection.Query<DTOAnggotaAktif>(query).ToList();

            connection.Close();

            return AnggotaList;
        }
    }
}
