using BackOffice.Interface;
using BackOffice.Model;
using Oracle.ManagedDataAccess.Client;
using static BackOffice.DataLayer.PeriodeGlobal;

namespace BackOffice.DataLayer
{
    public class LaporanRepository : ILaporanRepository
    {
        public List<DTOPOS_KREDIT_PENJUALAN_MASTER> PiutangKreditBarangBelumLunas()
        {
            List<DTOPOS_KREDIT_PENJUALAN_MASTER> Master = new();

            string query = "SELECT NO_TRANSAKSI,TANGGAL,NIK,NAMA_PELANGGAN,STATUS,UNIT_KERJA,TOTAL,TENOR,ANGSURAN FROM POS_PENJUALAN WHERE TENOR>1 AND ISLUNAS='T'";

            using (OracleConnection connection = new(global.connectionString))
            {
                connection.Open();

                using OracleCommand command = new(query, connection);
                using OracleDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    DTOPOS_KREDIT_PENJUALAN_MASTER DaftarKreditBarang = new()
                    {
                        NO_TRANSAKSI = reader["NO_TRANSAKSI"].ToString(),
                        TANGGAL = Convert.ToDateTime(reader["TANGGAL"]),
                        NIK = reader["NIK"].ToString(),
                        NAMA_PELANGGAN = reader["NAMA_PELANGGAN"].ToString(),
                        STATUS = reader["STATUS"].ToString(),
                        UNIT_KERJA = reader["UNIT_KERJA"].ToString(),
                        TOTAL = Convert.ToDecimal(reader["TOTAL"]),
                        TENOR = Convert.ToInt32(reader["TENOR"]),
                        ANGSURAN = Convert.ToDecimal(reader["ANGSURAN"])
                    };

                    // Mengambil angsuran untuk transaksi
                    List<DTOPOS_KREDIT_ANGSURAN_DETAIL> angsuran = GetDaftarAngsuranBarang(reader["NO_TRANSAKSI"].ToString());
                    DaftarKreditBarang.DetailsAngsuran = angsuran;

                    // Mengambil produk untuk transaksi
                    List<DTOPOS_KREDIT_PENJUALAN_DETAIL> barang = GetDaftarBarang(reader["NO_TRANSAKSI"].ToString());
                    DaftarKreditBarang.DetailsBarang = barang;

                    // Menghitung jumlah ANGSURAN untuk ISTAGIH = 'T'
                    decimal totalAngsuran = angsuran.Where(d => d.ISTAGIH == "T").Sum(d => d.ANGSURAN);
                    int SISAWAKTUANGSURAN = angsuran.Count(d => d.ISTAGIH == "T");
                    DaftarKreditBarang.SISAWAKTU = SISAWAKTUANGSURAN;
                    DaftarKreditBarang.PIUTANG = totalAngsuran;

                    Master.Add(DaftarKreditBarang);
                }
            }

            return Master;
        }
        List<DTOPOS_KREDIT_PENJUALAN_DETAIL> GetDaftarBarang(string idtransaksi)
        {
            List<DTOPOS_KREDIT_PENJUALAN_DETAIL> Detail = new();

            string query = "SELECT BARIS,NAMA_BARANG,SATUAN,JUMLAH_BARANG,HARGA_BARANG,POTONGAN,TOTAL_HARGA FROM  POS_PENJUALAN_DETAIL WHERE NO_TRANSAKSI = :idtransaksi ORDER BY BARIS";

            using (OracleConnection connection = new(global.connectionString))
            {
                connection.Open();

                using OracleCommand command = new(query, connection);
                command.Parameters.Add(":idtransaksi", OracleDbType.Varchar2).Value = idtransaksi;

                using OracleDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    DTOPOS_KREDIT_PENJUALAN_DETAIL DaftarBarang = new()
                    {
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

        /// <summary>
        /// Mengambil daftar detail angsuran untuk ID transaksi tertentu.
        /// </summary>
        /// <param name="idtransaksi">ID transaksi.</param>
        /// <returns>Daftar objek DTOPOS_KREDIT_ANGSURAN_DETAIL yang mewakili detail angsuran.</returns>
        List<DTOPOS_KREDIT_ANGSURAN_DETAIL> GetDaftarAngsuranBarang(string idtransaksi)
        {
            List<DTOPOS_KREDIT_ANGSURAN_DETAIL> Detail = new();

            string query = "SELECT NO_TRANSAKSI,TANGGALJATUHTEMPO,ANGSURANKE,SALDOAWAL,ANGSURAN,SALDOAKHIR,ISTAGIH TAGIH from POS_KREDIT_ANGSURAN WHERE NO_TRANSAKSI = :idtransaksi ORDER BY ANGSURANKE";

            using (OracleConnection connection = new(global.connectionString))
            {
                connection.Open();

                using OracleCommand command = new(query, connection);
                command.Parameters.Add(":idtransaksi", OracleDbType.Varchar2).Value = idtransaksi;

                using OracleDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    DTOPOS_KREDIT_ANGSURAN_DETAIL angsuranBarang = new()
                    {
                        NO_TRANSAKSI = reader["NO_TRANSAKSI"].ToString(),
                        TANGGALJATUHTEMPO = Convert.ToDateTime(reader["TANGGALJATUHTEMPO"]),
                        ANGSURANKE = Convert.ToInt32(reader["ANGSURANKE"]),
                        SALDOAWAL = Convert.ToDecimal(reader["SALDOAWAL"]),
                        ANGSURAN = Convert.ToDecimal(reader["ANGSURAN"]),
                        SALDOAKHIR = Convert.ToDecimal(reader["SALDOAKHIR"]),
                        ISTAGIH = reader["TAGIH"].ToString()
                    };

                    Detail.Add(angsuranBarang);
                }
            }

            return Detail;
        }

        List<DTOPOS_KREDIT_ANGSURAN_DETAIL> GetDaftarAngsuranPinjaman(string idtransaksi)
        {
            List<DTOPOS_KREDIT_ANGSURAN_DETAIL> Detail = new();

            string query = "SELECT NO_TRANSAKSI,TANGGALJATUHTEMPO,ANGSURANKE,SALDOAWAL,ANGSURAN,SALDOAKHIR,ISTAGIH TAGIH from FIN_PINJAMAN_DETAIL WHERE NO_TRANSAKSI = :idtransaksi ORDER BY ANGSURANKE";

            using (OracleConnection connection = new(global.connectionString))
            {
                connection.Open();

                using OracleCommand command = new(query, connection);
                command.Parameters.Add(":idtransaksi", OracleDbType.Varchar2).Value = idtransaksi;

                using OracleDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    DTOPOS_KREDIT_ANGSURAN_DETAIL angsuranBarang = new()
                    {
                        NO_TRANSAKSI = reader["NO_TRANSAKSI"].ToString(),
                        TANGGALJATUHTEMPO = Convert.ToDateTime(reader["TANGGALJATUHTEMPO"]),
                        ANGSURANKE = Convert.ToInt32(reader["ANGSURANKE"]),
                        SALDOAWAL = Convert.ToDecimal(reader["SALDOAWAL"]),
                        ANGSURAN = Convert.ToDecimal(reader["ANGSURAN"]),
                        SALDOAKHIR = Convert.ToDecimal(reader["SALDOAKHIR"]),
                        ISTAGIH = reader["TAGIH"].ToString()
                    };

                    Detail.Add(angsuranBarang);
                }
            }

            return Detail;
        }
        public List<FinTagihanWaserdaDtl> PenjualanWaserdaDetail(string p_nik, DateTime dari, DateTime sampai)
        {
            List<FinTagihanWaserdaDtl> Master = new List<FinTagihanWaserdaDtl>();

            try
            {
                using (OracleConnection connection = new OracleConnection(global.connectionString))
                {
                    connection.Open();

                    string query = @"SELECT 
                                m.no_transaksi,
                                m.tanggal,
                                m.jam,
                                m.nik,
                                d.baris,
                                d.nama_barang,
                                d.satuan,
                                d.jumlah_barang,
                                d.harga_barang,
                                d.bruto,
                                d.potongan,
                                d.total_harga
                            FROM 
                                pos_penjualan m
                            JOIN 
                                pos_penjualan_detail d ON d.no_transaksi = m.no_transaksi
                            WHERE M.TENOR=1 AND  m.nik = :nik AND m.tanggal BETWEEN :start_date AND :end_date";

                    using (OracleCommand command = new OracleCommand(query, connection))
                    {
                        command.Parameters.Add(":nik", OracleDbType.Varchar2).Value = p_nik;
                        command.Parameters.Add(":start_date", OracleDbType.Date).Value = dari;
                        command.Parameters.Add(":end_date", OracleDbType.Date).Value = sampai;

                        using (OracleDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                FinTagihanWaserdaDtl detail = new FinTagihanWaserdaDtl
                                {
                                    NoTransaksi = reader["no_transaksi"].ToString(),
                                    Tanggal = Convert.ToDateTime(reader["tanggal"]),
                                    Jam = reader["jam"].ToString(),
                                    Nik = reader["nik"].ToString(),
                                    Baris = Convert.ToInt32(reader["baris"]),
                                    NamaBarang = reader["nama_barang"].ToString(),
                                    Satuan = reader["satuan"].ToString(),
                                    JumlahBarang = Convert.ToInt32(reader["jumlah_barang"]),
                                    HargaBarang = Convert.ToDecimal(reader["harga_barang"]),
                                    Bruto = Convert.ToDecimal(reader["bruto"]),
                                    Potongan = Convert.ToDecimal(reader["potongan"]),
                                    TotalHarga = Convert.ToDecimal(reader["total_harga"])
                                };

                                Master.Add(detail);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle the exception (log, rethrow, etc.)
                Console.WriteLine($"Error: {ex.Message}");
                // Optionally, you might want to throw the exception again or log it.
                throw;
            }

            return Master;
        }



        public List<DTOPOS_KREDIT_PENJUALAN_MASTER> PenjualanTunai(DateTime dari, DateTime sampai)
        {
            List<DTOPOS_KREDIT_PENJUALAN_MASTER> Master = new List<DTOPOS_KREDIT_PENJUALAN_MASTER>();
            string query = "SELECT NO_TRANSAKSI, TANGGAL, BRUTO, POTONGAN, TOTAL FROM POS_PENJUALAN WHERE JENIS_BAYAR='TUNAI' AND TANGGAL BETWEEN :dari AND :sampai";

            using (OracleConnection connection = new OracleConnection(global.connectionString))
            {
                connection.Open();

                using OracleCommand command = new(query, connection);
                command.Parameters.Add(":dari", OracleDbType.Date).Value = dari;
                command.Parameters.Add(":sampai", OracleDbType.Date).Value = sampai;

                using (OracleDataReader reader = command.ExecuteReader())
                {
                    // Retrieve the list of products for all transactions
                    List<DTOPOS_KREDIT_PENJUALAN_DETAIL> barang = GetDaftarBarangForTransactions(dari, sampai);

                    // Loop through the reader to create DTOPOS_KREDIT_PENJUALAN_MASTER objects
                    while (reader.Read())
                    {
                        string noTransaksi = reader["NO_TRANSAKSI"].ToString();

                        // Retrieve the list of products (barang) for the current transaction (NO_TRANSAKSI)
                        List<DTOPOS_KREDIT_PENJUALAN_DETAIL> barangForTransaksi = barang
                            .Where(b => b.NO_TRANSAKSI == noTransaksi)
                            .ToList();

                        DTOPOS_KREDIT_PENJUALAN_MASTER penjualantunai = new()
                        {
                            NO_TRANSAKSI = noTransaksi,
                            TANGGAL = Convert.ToDateTime(reader["TANGGAL"]),
                            BRUTO = Convert.ToDecimal(reader["BRUTO"]),
                            POTONGAN = Convert.ToDecimal(reader["POTONGAN"]),
                            TOTAL = Convert.ToDecimal(reader["TOTAL"]),
                            DetailsBarang = barangForTransaksi
                        };
                        Master.Add(penjualantunai);
                    }
                }
            }

            return Master;
        }
        public List<DTOPinjamanMaster> PiutangPinjaman()
        {

            List<DTOPinjamanMaster> Master = new();

            string query = "SELECT NO_TRANSAKSI,TANGGAL,NIK,NAMA_PELANGGAN,STATUS,UNIT_KERJA,PINJAMAN,BUNGA,TENOR,ANGSURAN FROM FIN_PINJAMAN WHERE ISLUNAS='T'";

            using (OracleConnection connection = new(global.connectionString))
            {
                connection.Open();

                using OracleCommand command = new(query, connection);
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
                        ANGSURAN = Convert.ToDecimal(reader["ANGSURAN"])
                    };

                    // Fetch products for the order
                    List<DTOPinjamanDetail> daftarangsuranlist = GetDaftarAngsuran(reader["NO_TRANSAKSI"].ToString());
                    DaftarPinjaman.Details = daftarangsuranlist;

                    // Calculate the sum of ANGSURAN for ISTAGIH = 'T'
                    decimal totalAngsuran = daftarangsuranlist.Where(d => d.ISTAGIH == "T").Sum(d => d.ANGSURAN);
                    int SISAWAKTUANGSURAN = daftarangsuranlist.Count(d => d.ISTAGIH == "T");
                    DaftarPinjaman.SISAWAKTU = SISAWAKTUANGSURAN;
                    DaftarPinjaman.PIUTANG = totalAngsuran;


                    Master.Add(DaftarPinjaman);
                }
            }

            return Master;
        }
        private static List<DTOPinjamanDetail> GetDaftarAngsuran(string idtransaksi)
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

        public List<DTOPOS_KREDIT_PENJUALAN_MASTER> PenjualanTempo(DateTime dari, DateTime sampai)
        {
            List<DTOPOS_KREDIT_PENJUALAN_MASTER> Master = new List<DTOPOS_KREDIT_PENJUALAN_MASTER>();
            string query = "SELECT NO_TRANSAKSI, TANGGAL,NAMA_PELANGGAN, BRUTO, POTONGAN, TOTAL FROM POS_PENJUALAN WHERE JENIS_BAYAR='KREDIT' AND TENOR=1 AND TANGGAL BETWEEN :dari AND :sampai";

            using (OracleConnection connection = new OracleConnection(global.connectionString))
            {
                connection.Open();

                using OracleCommand command = new(query, connection);
                command.Parameters.Add(":dari", OracleDbType.Date).Value = dari;
                command.Parameters.Add(":sampai", OracleDbType.Date).Value = sampai;

                using OracleDataReader reader = command.ExecuteReader();
                // Retrieve the list of products for all transactions
                List<DTOPOS_KREDIT_PENJUALAN_DETAIL> barang = GetDaftarBarangForTransactions(dari, sampai);

                // Loop through the reader to create DTOPOS_KREDIT_PENJUALAN_MASTER objects
                while (reader.Read())
                {
                    string noTransaksi = reader["NO_TRANSAKSI"].ToString();

                    // Retrieve the list of products (barang) for the current transaction (NO_TRANSAKSI)
                    List<DTOPOS_KREDIT_PENJUALAN_DETAIL> barangForTransaksi = barang
                        .Where(b => b.NO_TRANSAKSI == noTransaksi)
                        .ToList();

                    DTOPOS_KREDIT_PENJUALAN_MASTER penjualantunai = new()
                    {
                        NO_TRANSAKSI = noTransaksi,
                        NAMA_PELANGGAN = reader["NAMA_PELANGGAN"].ToString(),
                        TANGGAL = Convert.ToDateTime(reader["TANGGAL"]),
                        BRUTO = Convert.ToDecimal(reader["BRUTO"]),
                        POTONGAN = Convert.ToDecimal(reader["POTONGAN"]),
                        TOTAL = Convert.ToDecimal(reader["TOTAL"]),
                        DetailsBarang = barangForTransaksi
                    };
                    Master.Add(penjualantunai);
                }
            }

            return Master;
        }

        public List<DTOPOS_KREDIT_PENJUALAN_MASTER> PenjualanKredit(DateTime dari, DateTime sampai)
        {
            List<DTOPOS_KREDIT_PENJUALAN_MASTER> Master = new();
            string query = "SELECT NO_TRANSAKSI, TANGGAL,NAMA_PELANGGAN, BRUTO, POTONGAN, TOTAL,TENOR FROM POS_PENJUALAN WHERE JENIS_BAYAR='KREDIT' AND TENOR>1 AND TANGGAL BETWEEN :dari AND :sampai";

            using (OracleConnection connection = new(global.connectionString))
            {
                connection.Open();

                using OracleCommand command = new(query, connection);
                command.Parameters.Add(":dari", OracleDbType.Date).Value = dari;
                command.Parameters.Add(":sampai", OracleDbType.Date).Value = sampai;

                using (OracleDataReader reader = command.ExecuteReader())
                {
                    // Retrieve the list of products for all transactions
                    List<DTOPOS_KREDIT_PENJUALAN_DETAIL> barang = GetDaftarBarangForTransactions(dari, sampai);

                    // Loop through the reader to create DTOPOS_KREDIT_PENJUALAN_MASTER objects
                    while (reader.Read())
                    {
                        string noTransaksi = reader["NO_TRANSAKSI"].ToString();

                        // Retrieve the list of products (barang) for the current transaction (NO_TRANSAKSI)
                        List<DTOPOS_KREDIT_PENJUALAN_DETAIL> barangForTransaksi = barang
                            .Where(b => b.NO_TRANSAKSI == noTransaksi)
                            .ToList();

                        DTOPOS_KREDIT_PENJUALAN_MASTER penjualantunai = new()
                        {
                            NO_TRANSAKSI = noTransaksi,
                            TANGGAL = Convert.ToDateTime(reader["TANGGAL"]),
                            NAMA_PELANGGAN = reader["NAMA_PELANGGAN"].ToString(),
                            BRUTO = Convert.ToDecimal(reader["BRUTO"]),
                            POTONGAN = Convert.ToDecimal(reader["POTONGAN"]),
                            TOTAL = Convert.ToDecimal(reader["TOTAL"]),
                            TENOR = Convert.ToInt16(reader["TENOR"]),
                            DetailsBarang = barangForTransaksi
                        };
                        Master.Add(penjualantunai);
                    }
                }
            }

            return Master;
        }

        private static List<DTOPOS_KREDIT_PENJUALAN_DETAIL> GetDaftarBarangForTransactions(DateTime dari, DateTime sampai)
        {
            List<DTOPOS_KREDIT_PENJUALAN_DETAIL> Detail = new();

            string query = @"SELECT D.BARIS,D.NAMA_BARANG,D.SATUAN,D.JUMLAH_BARANG,D.HARGA_BARANG,D.POTONGAN,D.TOTAL_HARGA,D.NO_TRANSAKSI FROM  POS_PENJUALAN_DETAIL D
                            JOIN POS_PENJUALAN M ON M.NO_TRANSAKSI=D.NO_TRANSAKSI
                            WHERE M.TANGGAL BETWEEN :dari AND :sampai ORDER BY D.BARIS";

            using (OracleConnection connection = new(global.connectionString))
            {
                connection.Open();

                using OracleCommand command = new(query, connection);
                command.Parameters.Add(":dari", OracleDbType.Date).Value = dari;
                command.Parameters.Add(":sampai", OracleDbType.Date).Value = sampai;
                using OracleDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    DTOPOS_KREDIT_PENJUALAN_DETAIL DaftarBarang = new()
                    {
                        BARIS = Convert.ToInt32(reader["BARIS"]),
                        NAMA_BARANG = reader["NAMA_BARANG"].ToString(),
                        SATUAN = reader["SATUAN"].ToString(),
                        JUMLAH_BARANG = Convert.ToInt32(reader["JUMLAH_BARANG"]),
                        HARGA_BARANG = Convert.ToDecimal(reader["HARGA_BARANG"]),
                        POTONGAN = Convert.ToDecimal(reader["POTONGAN"]),
                        TOTAL_HARGA = Convert.ToDecimal(reader["TOTAL_HARGA"]),
                        NO_TRANSAKSI = reader["NO_TRANSAKSI"].ToString(),
                    };

                    Detail.Add(DaftarBarang);
                }
            }

            return Detail;
        }

        public List<DTOTagihanAngsuran> TagihanKreditBarang(string p_nik, int p_periode)
        {
            List<DTOTagihanAngsuran> Master = new();

            string query = "SELECT * FROM FIN_TAGIHAN_KREDIT WHERE NIK = :nik AND PERIODE = :periode";
            using (OracleConnection connection = new(global.connectionString))
            {
                connection.Open();
                using OracleCommand command = new(query, connection);
                command.Parameters.Add(new OracleParameter("nik", p_nik));
                command.Parameters.Add(new OracleParameter("periode", p_periode));

                using OracleDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    DTOTagihanAngsuran DaftarKreditBarang = new()
                    {                        
                        NIK = reader["NIK"].ToString(),
                        NAMA_PELANGGAN = reader["NAMA_PELANGGAN"].ToString(),
                        STATUS = reader["STATUS"].ToString(),
                        UNIT_KERJA = reader["UNIT_KERJA"].ToString(),
                        TOTAL = Convert.ToDecimal(reader["TOTAL"]),
                        NO_TRANSAKSI = reader["NO_TRANSAKSI"].ToString(),
                        TGLKREDIT = Convert.ToDateTime(reader["TGLKREDIT"]),
                        KE = Convert.ToInt16(reader["KE"]),
                        DARI = Convert.ToInt16(reader["DARI"]),
                    };

                    // Mengambil angsuran untuk transaksi
                    List<DTOPOS_KREDIT_ANGSURAN_DETAIL> angsuran = GetDaftarAngsuranBarang(reader["NO_TRANSAKSI"].ToString());
                    DaftarKreditBarang.Angsuran = angsuran;

                    // Mengambil produk untuk transaksi
                    List<DTOPOS_KREDIT_PENJUALAN_DETAIL> barang =GetDaftarBarang(reader["NO_TRANSAKSI"].ToString());
                    DaftarKreditBarang.DaftarBarang = barang;

                    Master.Add(DaftarKreditBarang);
                }
            }

            return Master;
        }


        public List<DTOTagihanAngsuran> TagihanPinjaman(string p_nik, int p_periode)
        {
            List<DTOTagihanAngsuran> Master = new();

            string query = "SELECT * FROM FIN_TAGIHAN_PINJAMAN WHERE NIK = :nik AND PERIODE = :periode";
            using (OracleConnection connection = new(global.connectionString))
            {
                connection.Open();
                using OracleCommand command = new(query, connection);
                command.Parameters.Add(new OracleParameter("nik", p_nik));
                command.Parameters.Add(new OracleParameter("periode", p_periode));

                using OracleDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    DTOTagihanAngsuran DaftarKreditBarang = new()
                    {
                        NIK = reader["NIK"].ToString(),
                        NAMA_PELANGGAN = reader["NAMA_PELANGGAN"].ToString(),
                        STATUS = reader["STATUS"].ToString(),
                        UNIT_KERJA = reader["UNIT_KERJA"].ToString(),
                        TOTAL = Convert.ToDecimal(reader["TOTAL"]),
                        NO_TRANSAKSI = reader["NO_TRANSAKSI"].ToString(),
                        TGLKREDIT = Convert.ToDateTime(reader["TGLPINJAM"]),
                        KE = Convert.ToInt16(reader["KE"]),
                        DARI = Convert.ToInt16(reader["DARI"]),
                    };

                    // Mengambil angsuran untuk transaksi
                    List<DTOPOS_KREDIT_ANGSURAN_DETAIL> angsuran = GetDaftarAngsuranPinjaman(reader["NO_TRANSAKSI"].ToString());
                    DaftarKreditBarang.Angsuran = angsuran;

                    Master.Add(DaftarKreditBarang);
                }
            }

            return Master;
        }
    }
}
