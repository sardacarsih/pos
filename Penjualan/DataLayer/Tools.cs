using Oracle.ManagedDataAccess.Client;
using Penjualan.Interface;
using System;
using System.Data;
using System.Linq;

namespace Penjualan
{
    public class Tools : iTools
    {
        public DataTable AnggotaAktif()
        {
            string query = "SELECT A.ID_PELANGGAN,A.NIK,A.NAMA_PELANGGAN,K.NAMA UNIT_KERJA,A.STATUS,A.LIMIT_HUTANG FROM FIN_ANGGOTA A JOIN FIN_UNITKERJA K ON K.KODE=A.UNIT_KERJA WHERE A.AKTIF='Y' AND A.ANGGOTA='Y' ORDER BY A.NAMA_PELANGGAN";
            using OracleConnection connection = new(global.connectionString);
            using OracleCommand _command = new(query, connection)
            {
                CommandType = CommandType.Text
            };
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }
            OracleDataReader dr;
            dr = _command.ExecuteReader();
            DataTable _dt = new();
            _dt.Load(dr);
            dr.Close();
            connection.Close();
            return _dt;
        }

        public bool GetRemiseStatus(int p_periode, int remise)
        {
            bool isclosed = false;
            string query;
            if (remise == 1)
            {
                query = "SELECT REMISE1 FROM POS_PERIODE WHERE PERIODE=:periode";
            }
            else
            {
                query = "SELECT REMISE2 FROM POS_PERIODE WHERE PERIODE=:periode";
            }

            using (OracleConnection conn = new (global.connectionString))
            {
                conn.Open();

                using (OracleCommand cmd = new (query, conn))
                {
                    cmd.Parameters.Add("periode", p_periode);

                    object result = cmd.ExecuteScalar();

                    if (!string.IsNullOrEmpty(result?.ToString()) && result.ToString().Trim().Equals("Y", StringComparison.OrdinalIgnoreCase))
                    {
                        isclosed = true;
                    }
                }

                conn.Close();
            }

            return isclosed;
        }

        public DataTable PelangganAktif()
        {
            string query = "SELECT A.ID_PELANGGAN,A.NIK,A.NAMA_PELANGGAN,K.NAMA UNIT_KERJA,A.STATUS,A.LIMIT_HUTANG FROM FIN_ANGGOTA A JOIN FIN_UNITKERJA K ON K.KODE=A.UNIT_KERJA WHERE A.AKTIF='Y'  ORDER BY A.NAMA_PELANGGAN";
            using OracleConnection connection = new(global.connectionString);
            using OracleCommand _command = new(query, connection)
            {
                CommandType = CommandType.Text
            };
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }
            OracleDataReader dr;
            dr = _command.ExecuteReader();
            DataTable _dt = new();
            _dt.Load(dr);
            dr.Close();
            connection.Close();
            return _dt;
        }

        public  string TerbilangIndonesia(double nilai)
        {
            string[] angka = { "Rupiah", "satu", "dua", "tiga", "empat", "lima",
        "enam", "tujuh", "delapan", "sembilan", "sepuluh",
        "sebelas", "dua belas", "tiga belas", "empat belas", "lima belas",
        "enam belas", "tujuh belas", "delapan belas", "sembilan belas"  };

            if (nilai < 20)
            {
                return angka[(int)nilai];
            }
            else if (nilai < 100)
            {
                return angka[(int)nilai / 10] + " puluh " + angka[(int)nilai % 10];
            }
            else if (nilai < 200)
            {
                return "seratus " + TerbilangIndonesia(nilai % 100);
            }
            else if (nilai < 1000)
            {
                return angka[(int)nilai / 100] + " ratus " + TerbilangIndonesia(nilai % 100);
            }
            else if (nilai < 2000)
            {
                return "seribu " + TerbilangIndonesia(nilai % 1000);
            }
            else if (nilai < 1000000)
            {
                return TerbilangIndonesia(nilai / 1000) + " ribu " + TerbilangIndonesia(nilai % 1000);
            }
            else if (nilai < 1000000000)
            {
                return TerbilangIndonesia(nilai / 1000000) + " juta " + TerbilangIndonesia(nilai % 1000000);
            }
            else if (nilai < 1000000000000)
            {
                return TerbilangIndonesia(nilai / 1000000000) + " milyar " + TerbilangIndonesia(nilai % 1000000000);
            }
            else if (nilai < 1000000000000000)
            {
                return TerbilangIndonesia(nilai / 1000000000000) + " triliun " + TerbilangIndonesia(nilai % 1000000000000);
            }
            else
            {
                return "Angka terlalu besar";
            }
        }
    }
    
}
