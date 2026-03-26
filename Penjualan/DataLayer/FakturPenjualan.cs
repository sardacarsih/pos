using Dapper;
using DevExpress.Utils.About;
using Oracle.ManagedDataAccess.Client;
using Penjualan.Interface;
using Penjualan.Model;
using System.Data;
using System.Text.RegularExpressions;

namespace Penjualan.DataLayer
{
    public class FakturPenjualan : IFakturPenjualan
    {
        public bool GetSettingKontrol_qty_Saldo()
        {
            bool hasValue = false;

            using (OracleConnection connection = new OracleConnection(Global.connectionString))
            {
                connection.Open();

                string sqlQuery = "SELECT JUMLAH FROM FIN_SETTINGS WHERE CONFIG ='KONTROL_SALDO_QTY'";
                using OracleCommand command = new(sqlQuery, connection);
                object result = command.ExecuteScalar();

                // Check if the result is null, treat it as 0
                int rowCount = result != null ? Convert.ToInt32(result) : 0;

                // If rowCount is greater than 0, it means a matching record exists
                hasValue = rowCount > 0;
            }

            return hasValue;
        }

        public decimal GetStocItem(string kodeBarang, DateTime startDate, DateTime endDate)
        {
            using OracleConnection connection = new(Global.connectionString);
            connection.Open();

            string sqlQuery = @"
                SELECT COALESCE(stock.quantity, 0) + COALESCE(purchases.quantity, 0)
                    + COALESCE(STOCKOPNAME.jumlah_barang, 0) - (COALESCE(sales.jumlah_barang, 0)
                    + COALESCE(RUSAK.jumlah_barang, 0)) AS StockAkhir
                FROM
                    (SELECT kode_item FROM POS_PRODUCT WHERE kode_item = :p_product_code) all_data
                LEFT JOIN
                    (SELECT kode_barang, quantity FROM pos_stock 
                     WHERE tanggal = :start_date AND KODE_BARANG = :p_product_code) stock 
                     ON all_data.kode_item = stock.kode_barang
                LEFT JOIN
                    (SELECT 
                        d.kode_barang,
                        SUM(d.quantity) AS quantity,
                        AVG(d.harga_beli) AS harga_beli
                    FROM pos_pembeliandetail d
                    JOIN pos_pembelian m ON m.purchase_id = d.purchase_id
                    WHERE m.tanggal BETWEEN :start_date AND :end_date AND d.kode_barang = :p_product_code
                    GROUP BY d.kode_barang) purchases ON all_data.kode_item = purchases.kode_barang
                LEFT JOIN
                    (SELECT 
                        d.kode_barang,
                        SUM(d.jumlah_barang) AS jumlah_barang
                    FROM pos_penjualan_detail d
                    JOIN pos_penjualan m ON m.no_transaksi = d.no_transaksi
                    WHERE m.tanggal BETWEEN :start_date AND :end_date AND d.kode_barang = :p_product_code
                    GROUP BY d.kode_barang) sales ON all_data.kode_item = sales.kode_barang
                LEFT JOIN
                    (SELECT 
                        KODE_BARANG,
                        SUM(JUMLAHFISIK) AS jumlah_barang
                    FROM POS_BARANGRUSAK
                    WHERE TANGGAL BETWEEN :start_date AND :end_date AND KODE_BARANG = :p_product_code
                    GROUP BY KODE_BARANG) RUSAK ON all_data.kode_item = RUSAK.kode_barang
                LEFT JOIN
                    (SELECT 
                        KODE_BARANG,
                        SUM(SELISIH) AS jumlah_barang
                    FROM POS_STOCKOPNAME
                    WHERE TANGGAL BETWEEN :start_date AND :end_date AND KODE_BARANG = :p_product_code
                    GROUP BY KODE_BARANG) STOCKOPNAME ON all_data.kode_item = STOCKOPNAME.kode_barang";

            var parameters = new
            {
                p_product_code = kodeBarang,
                start_date = startDate,
                end_date = endDate
            };

            var result = connection.QueryFirstOrDefault<decimal>(sqlQuery, parameters, commandType: CommandType.Text);

            return result;
        }

        public void InsertFaktur_Penjualan(DTOFakturPenjualanHeader faktur_header, List<DTOFakturPenjualanDetail> ListItemsPenjualan, CreditLimitCheck? creditCheck = null)
        {
            using OracleConnection conn = new(Global.connectionString);
            conn.Open();
            OracleTransaction transaction = conn.BeginTransaction();

            try
            {
                // Validate credit limit inside the same transaction
                if (creditCheck != null && creditCheck.Limit != 0)
                {
                    ValidateCreditLimit(conn, transaction, creditCheck);
                }

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
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
        }


        public void InsertFaktur_Penjualan_Angsuran(DTOFakturPenjualanHeader faktur_header, List<DTOFakturPenjualanDetail> ListItemsPenjualan, List<DTOAngsuranKreditBarang> DaftarWaktuTagihan, CreditLimitCheck? creditCheck = null)
        {
            using OracleConnection conn = new(Global.connectionString);
            conn.Open();
            OracleTransaction transaction = conn.BeginTransaction();

            try
            {
                // Validate credit limit inside the same transaction
                if (creditCheck != null && creditCheck.Limit != 0)
                {
                    ValidateCreditLimit(conn, transaction, creditCheck);
                }

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

        public DTOProductInfo RetrieveProductInfo(string barcode)
        {
            DTOProductInfo productInfo = new ();

            using (OracleConnection connection = new(Global.connectionString))
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

        public void UpdateFakturPenjualan(DTOFakturPenjualanHeader faktur_header)
        {
            using OracleConnection connection = new(Global.connectionString);

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

        public string GenerateTransactionNumber(DateTime date)
        {
            int currentYear = date.Year % 100;
            string prefix = $"W-{currentYear:D2}";

            using OracleConnection connection = new(Global.connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                // Lock the row for atomic read-and-update
                string selectQuery = "SELECT nomor FROM nomor_transaksi WHERE kode = 'PENJUALAN' FOR UPDATE";
                using OracleCommand selectCommand = new(selectQuery, connection);
                selectCommand.Transaction = transaction;
                string? lastNumber = selectCommand.ExecuteScalar()?.ToString();

                string newTransactionNumber;

                if (string.IsNullOrEmpty(lastNumber) || !lastNumber.StartsWith(prefix))
                {
                    // New year or no record — start at 1
                    newTransactionNumber = $"{prefix}-000001";
                }
                else
                {
                    int lastSeq = lastNumber.Length >= 6
                        ? int.Parse(lastNumber.Substring(lastNumber.Length - 6))
                        : 0;
                    newTransactionNumber = $"{prefix}-{(lastSeq + 1):D6}";
                }

                // Upsert the new transaction number
                string mergeQuery = @"
                    MERGE INTO nomor_transaksi t
                    USING (SELECT 'PENJUALAN' AS kode FROM DUAL) s
                    ON (t.kode = s.kode)
                    WHEN MATCHED THEN UPDATE SET nomor = :nomor
                    WHEN NOT MATCHED THEN INSERT (kode, nomor) VALUES ('PENJUALAN', :nomor)";

                using OracleCommand mergeCommand = new(mergeQuery, connection);
                mergeCommand.Transaction = transaction;
                mergeCommand.Parameters.Add("nomor", OracleDbType.Varchar2).Value = newTransactionNumber;
                mergeCommand.ExecuteNonQuery();

                transaction.Commit();
                return newTransactionNumber;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        private static void ValidateCreditLimit(OracleConnection conn, OracleTransaction transaction, CreditLimitCheck creditCheck)
        {
            string query = @"
                SELECT NVL(SUM(TOTAL), 0)
                FROM POS_PENJUALAN
                WHERE NIK = :Nik
                AND TANGGAL BETWEEN :Dari AND :Sampai";

            using var cmd = new OracleCommand(query, conn);
            cmd.Transaction = transaction;
            cmd.Parameters.Add("Nik", OracleDbType.Varchar2).Value = creditCheck.NIK;
            cmd.Parameters.Add("Dari", OracleDbType.Date).Value = creditCheck.PeriodFrom;
            cmd.Parameters.Add("Sampai", OracleDbType.Date).Value = creditCheck.PeriodTo;

            decimal currentDebt = Convert.ToDecimal(cmd.ExecuteScalar());
            decimal totalAfterInvoice = currentDebt + creditCheck.InvoiceAmount;

            if (totalAfterInvoice > creditCheck.Limit)
            {
                throw new CreditLimitExceededException(currentDebt, creditCheck.InvoiceAmount, creditCheck.Limit);
            }
        }

        public DTOPeriodeDates? GetTanggalByPeriode(int periode)
        {
            string query = @"
                SELECT
                    TO_DATE(R1DARI, 'DD-MON-YYYY', 'NLS_DATE_LANGUAGE = ENGLISH') AS R1DARI,
                    TO_DATE(R1SAMPAI, 'DD-MON-YYYY', 'NLS_DATE_LANGUAGE = ENGLISH') AS R1SAMPAI,
                    TO_DATE(R2DARI, 'DD-MON-YYYY', 'NLS_DATE_LANGUAGE = ENGLISH') AS R2DARI,
                    TO_DATE(R2SAMPAI, 'DD-MON-YYYY', 'NLS_DATE_LANGUAGE = ENGLISH') AS R2SAMPAI,
                    TO_DATE(BDARI, 'DD-MON-YYYY', 'NLS_DATE_LANGUAGE = ENGLISH') AS BDARI,
                    TO_DATE(BSAMPAI, 'DD-MON-YYYY', 'NLS_DATE_LANGUAGE = ENGLISH') AS BSAMPAI
                FROM POS_PERIODE
                WHERE PERIODE = :periode";

            using OracleConnection connection = new(Global.connectionString);
            using OracleCommand cmd = new(query, connection);
            cmd.Parameters.Add(":periode", OracleDbType.Int32).Value = periode;

            connection.Open();
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new DTOPeriodeDates
                {
                    Remise1Dari = reader.IsDBNull(reader.GetOrdinal("R1DARI")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("R1DARI")),
                    Remise1Sampai = reader.IsDBNull(reader.GetOrdinal("R1SAMPAI")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("R1SAMPAI")),
                    Remise2Dari = reader.IsDBNull(reader.GetOrdinal("R2DARI")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("R2DARI")),
                    Remise2Sampai = reader.IsDBNull(reader.GetOrdinal("R2SAMPAI")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("R2SAMPAI")),
                    BulananDari = reader.IsDBNull(reader.GetOrdinal("BDARI")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("BDARI")),
                    BulananSampai = reader.IsDBNull(reader.GetOrdinal("BSAMPAI")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("BSAMPAI"))
                };
            }
            return null;
        }

        public DTOPotonganHarga? GetPotonganByKodeItem(string kodeItem)
        {
            string query = "SELECT MINQTY, POTONGAN FROM POS_POTONGANBERDASARKANQTY WHERE KODE_ITEM = :kodebrg";

            using OracleConnection connection = new(Global.connectionString);
            connection.Open();
            using OracleCommand command = new(query, connection);
            command.Parameters.Add("kodebrg", OracleDbType.Varchar2).Value = kodeItem;
            using var reader = command.ExecuteReader();

            if (reader.Read())
            {
                return new DTOPotonganHarga
                {
                    MinQty = Convert.ToInt32(reader.GetDecimal(0)),
                    Potongan = reader.GetDecimal(1)
                };
            }
            return null;
        }

        public List<DTOPelanggan> GetPelangganAktif()
        {
            string query = @"SELECT A.ID_PELANGGAN, A.NIK, A.NAMA_PELANGGAN, A.UNIT_KERJA, K.NAMA UNITKERJA, A.STATUS, A.LIMIT_HUTANG
                            FROM FIN_ANGGOTA A
                            JOIN FIN_UNITKERJA K ON K.KODE=A.UNIT_KERJA
                            WHERE A.AKTIF='Y' ORDER BY A.NAMA_PELANGGAN";

            using OracleConnection connection = new(Global.connectionString);
            connection.Open();
            return connection.Query<DTOPelanggan>(query).ToList();
        }

        public decimal CheckingJumlahHutang(string nik, string status, DateTime dari, DateTime sampai)
        {
            using var connection = new OracleConnection(Global.connectionString);
            connection.Open();

            using var cmd = new OracleCommand(@"
                SELECT NVL(SUM(TOTAL), 0)
                FROM POS_PENJUALAN
                WHERE NIK = :Nik
                AND TANGGAL BETWEEN :Dari AND :Sampai", connection);

            cmd.Parameters.Add("Nik", OracleDbType.Varchar2).Value = nik;
            cmd.Parameters.Add("Dari", OracleDbType.Date).Value = dari;
            cmd.Parameters.Add("Sampai", OracleDbType.Date).Value = sampai;

            return Convert.ToDecimal(cmd.ExecuteScalar());
        }
    }
}
