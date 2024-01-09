using BackOffice.Model;
using BackOffice.UC;
using DevExpress.XtraSpreadsheet.Model;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackOffice.DataLayer
{
    public class RekapTerimaBayarDetal
    {
        public static List<DTOPenerimaanPembayaran> Load_Pembayaran(int p_periode, int p_remise, string p_unitkerja)
        {
            List<DTOPenerimaanPembayaran> pembayaranList = new();
            using (OracleConnection connection = new(global.connectionString))
            {
                connection.Open();

                string query = "SELECT b.*, a.nama_pelanggan, a.unit_kerja " +
                               "FROM fin_terima_pembayaran b " +
                               "JOIN fin_anggota a ON a.nik = b.nik " +
                               "WHERE b.periode = :p AND b.remise = :r AND a.UNIT_KERJA = :u";

                using (OracleCommand command = new(query, connection))
                {
                    command.Parameters.Add(new OracleParameter("p", p_periode));
                    command.Parameters.Add(new OracleParameter("r", p_remise));
                    command.Parameters.Add(new OracleParameter("u", p_unitkerja));

                    using OracleDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        DTOPenerimaanPembayaran rekaptagihan_bynik = new()
                        {
                            PERIODE = reader.GetInt32(reader.GetOrdinal("periode")),
                            REMISE = reader.GetInt32(reader.GetOrdinal("remise")),
                            NIK = reader.GetString(reader.GetOrdinal("nik")),
                            NAMA_PELANGGAN = reader.GetString(reader.GetOrdinal("nama_pelanggan")),
                            UNIT_KERJA = reader.GetString(reader.GetOrdinal("unit_kerja")),
                            TAGIHAN = reader.GetDecimal(reader.GetOrdinal("tagihan")),
                            BAYAR = reader.GetDecimal(reader.GetOrdinal("bayar")),
                            SISA = reader.GetDecimal(reader.GetOrdinal("sisa"))
                        };
                        // Fetch products for the order
                       // List<DTOPenerimaanPembayaranDetail> totaltagihanwaserda = GetDataFromQuery(reader["nik"].ToString());
                        //rekaptagihan_bynik.Details= totaltagihanwaserda;
                        pembayaranList.Add(rekaptagihan_bynik);
                    }
                }

                connection.Close();
            }

            // Reset IsModified flag after loading new data
            foreach (DTOPenerimaanPembayaran item in pembayaranList)
            {
                item.IsModified = false;
            }

            return pembayaranList; // Return the populated pembayaranList
        }
        public List<DTOPenerimaanPembayaranDetail> GetDataFromQuery(string p_periodelalu_KHT, string p_remiselalu_KHT, string p_periodelalu_BULANAN, string p_remiselalu_BULANAN, string p_periode, string p_remise, string unitKerja)
        {
            List<DTOPenerimaanPembayaranDetail> results = new List<DTOPenerimaanPembayaranDetail>();

            using (OracleConnection connection = new OracleConnection(global.connectionString))
            {
                connection.Open();

                using (OracleCommand command = connection.CreateCommand())
                {
                    // Set up your query
                    command.CommandText = @"
                    SELECT NIK, SISA AS SISALALU, 0 AS SIMPANAN, 0 AS KREDIT, 0 AS PINJAMAN, 0 AS WASERDA
                    FROM FIN_TERIMA_PEMBAYARAN
                    WHERE PERIODE = :p_periodelalu_KHT AND REMISE = :p_remiselalu_KHT AND SISA > 0 AND UNIT_KERJA=:UK

                    UNION ALL 

                    SELECT NIK, SISA AS SISALALU, 0 AS SIMPANAN, 0 AS KREDIT, 0 AS PINJAMAN, 0 AS WASERDA
                    FROM FIN_TERIMA_PEMBAYARAN
                    WHERE PERIODE = :p_periodelalu_BULANAN AND REMISE = :p_remiselalu_BULANAN AND SISA > 0 AND UNIT_KERJA=:UK

                    UNION ALL 

                    SELECT NIK, 0 AS SISALALU, JUMLAH AS SIMPANAN, 0 AS KREDIT, 0 AS PINJAMAN, 0 AS WASERDA
                    FROM FIN_TAGIHAN_SPSW
                    WHERE PERIODE = :p_periode AND UNIT_KERJA=:UK

                    UNION ALL 

                    SELECT NIK, 0 AS SISALALU, 0 AS SIMPANAN, TOTAL AS KREDIT, 0 AS PINJAMAN, 0 AS WASERDA
                    FROM FIN_TAGIHAN_KREDIT
                    WHERE PERIODE = :p_periode AND UNIT_KERJA=:UK

                    UNION ALL 

                    SELECT NIK, 0 AS SISALALU, 0 AS SIMPANAN, 0 AS KREDIT, TOTAL AS PINJAMAN, 0 AS WASERDA
                    FROM FIN_TAGIHAN_PINJAMAN
                    WHERE PERIODE = :p_periode AND UNIT_KERJA=:UK

                    UNION ALL 

                    SELECT NIK, 0 AS SISALALU, 0 AS SIMPANAN, 0 AS KREDIT, 0 AS PINJAMAN, TOTAL AS WASERDA
                    FROM FIN_TAGIHAN_WASERDA
                    WHERE PERIODE = :p_periode AND REMISE = :p_remise AND UNIT_KERJA=:UK";

                    // Set up query parameters
                    command.Parameters.Add("p_periodelalu_KHT", OracleDbType.Varchar2).Value = p_periodelalu_KHT;
                    command.Parameters.Add("p_remiselalu_KHT", OracleDbType.Varchar2).Value = p_remiselalu_KHT;
                    command.Parameters.Add("p_periodelalu_BULANAN", OracleDbType.Varchar2).Value = p_periodelalu_BULANAN;
                    command.Parameters.Add("p_remiselalu_BULANAN", OracleDbType.Varchar2).Value = p_remiselalu_BULANAN;
                    command.Parameters.Add("p_periode", OracleDbType.Varchar2).Value = p_periode;
                    command.Parameters.Add("p_remise", OracleDbType.Varchar2).Value = p_remise;
                    command.Parameters.Add("UK", OracleDbType.Varchar2).Value = unitKerja;

                    // Execute the query
                    using (OracleDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            DTOPenerimaanPembayaranDetail data = new DTOPenerimaanPembayaranDetail
                            {
                                NIK = reader.GetString(0),
                                SISALALU = reader.GetDecimal(1),
                                SWAJIB = reader.GetDecimal(2),
                                KREDIT = reader.GetDecimal(3),
                                PINJAMAN = reader.GetDecimal(4),
                                WASERDA = reader.GetDecimal(5)
                            };

                            results.Add(data);
                        }
                    }
                }
            }

            return results;
        }
    }
}
