using BackOffice.Interface;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackOffice.DataLayer
{
    public class TutupBukuRepository : ITutupBuku
    {
        public List<string> GetDuplicateNiks(int p_periode, int p_remise, DateTime p_daritanggal, DateTime p_daritanggalr2, DateTime p_sampaitanggal)
        {
                List<string> duplicateNiks = new();
            using (OracleConnection connection = new(global.connectionString))
            {
                connection.Open();

                using OracleCommand command = new();
                command.Connection = connection;
                command.CommandType = CommandType.Text;

                if (p_remise == 1)
                {
                    // Retrieve data for the header
                    command.CommandText = @"
                        SELECT NIK
                        FROM POS_PENJUALAN J
                        JOIN FIN_UNITKERJA U ON U.KODE = J.UNIT_KERJA
                        WHERE J.TENOR = 1 AND STATUS <> 'BULANAN' AND jenis_bayar = 'KREDIT' AND PENDING = 'T' AND TANGGAL BETWEEN :daritanggal AND :sampaitanggal
                        GROUP BY NIK
                        HAVING COUNT(NIK) > 1";

                    command.Parameters.Add("daritanggal", OracleDbType.Date).Value = p_daritanggal;
                    command.Parameters.Add("sampaitanggal", OracleDbType.Date).Value = p_sampaitanggal;
                }
                else
                {
                    // Retrieve data for KHT WASERDA HEADER or BULANAN WASERDA HEADER
                    command.CommandText = @"
                        SELECT NIK
                        FROM POS_PENJUALAN J
                        JOIN FIN_UNITKERJA U ON U.KODE = J.UNIT_KERJA
                        WHERE J.TENOR = 1 AND jenis_bayar = 'KREDIT' AND PENDING = 'T'
                            AND ((STATUS <> 'BULANAN' AND TANGGAL BETWEEN :daritanggalr2 AND :sampaitanggal)
                            OR (STATUS = 'BULANAN' AND TANGGAL BETWEEN :daritanggal AND :sampaitanggal)) 
                        GROUP BY NIK
                        HAVING COUNT(NIK) > 1";

                    command.Parameters.Add("daritanggalr2", OracleDbType.Date).Value = p_daritanggalr2;
                    command.Parameters.Add("daritanggal", OracleDbType.Date).Value = p_daritanggal;
                    command.Parameters.Add("sampaitanggal", OracleDbType.Date).Value = p_sampaitanggal;
                }

                using OracleDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    string duplicateNik = reader.GetString(0);
                    duplicateNiks.Add(duplicateNik);
                }
            }
            return duplicateNiks;
        }
    }
}
