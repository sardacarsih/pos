using Dapper;
using DevExpress.Utils.About;
using Oracle.ManagedDataAccess.Client;
using Penjualan.BusinessLayer;
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

        public void InsertFaktur_Penjualan(DTOFakturPenjualanHeader faktur_header, List<DTOFakturPenjualanDetail> ListItemsPenjualan, CreditLimitCheck? creditCheck = null, string? pendingNoTransaksi = null)
        {
            using OracleConnection conn = new(Global.connectionString);
            conn.Open();
            OracleTransaction transaction = conn.BeginTransaction();

            try
            {
                ValidatePeriodeTerbuka(conn, transaction, faktur_header.TANGGAL, faktur_header.STATUS);

                // Validate credit limit inside the same transaction
                if (creditCheck != null)
                {
                    ValidateCreditLimit(conn, transaction, creditCheck);
                }

                // Insert master records
                string insertFakturJual_Master = "INSERT INTO POS_PENJUALAN (NO_TRANSAKSI, TANGGAL, JAM, KASIR, NAMA_KASIR, ID_PELANGGAN, NIK, NAMA_PELANGGAN, UNIT_KERJA, STATUS, BRUTO, POTONGAN, TOTAL, JENIS_BAYAR, KET_BAYAR, TENOR, ANGSURAN,PENDING) " +
                                                "VALUES (:NO_TRANSAKSI, :TANGGAL, :JAM, :KASIR, :NAMA_KASIR, :ID_PELANGGAN, :NIK, :NAMA_PELANGGAN, :UNIT_KERJA, :STATUS, :BRUTO, :POTONGAN, :TOTAL, :JENIS_BAYAR, :KET_BAYAR, :TENOR, :ANGSURAN,:PENDING) " +
                                                "RETURNING ID_PENJUALAN INTO :penjualanId";

                var masterParameters = new DynamicParameters(faktur_header);
                masterParameters.Add("penjualanId", dbType: DbType.Int32, direction: ParameterDirection.Output);

                conn.Execute(insertFakturJual_Master, masterParameters, transaction);

                Int32 newPenjualanId = masterParameters.Get<Int32>("penjualanId");

                string insertFakturJual_Detail = "INSERT INTO POS_PENJUALAN_DETAIL (ID_PENJUALAN, NO_TRANSAKSI, BARIS, PRODUCT_ID, KODE_BARANG, BARCODE, NAMA_BARANG, SATUAN, HPP,HARGA_BARANG, JUMLAH_BARANG, BRUTO, POTONGAN, TOTAL_HARGA) " +
                    "VALUES (:ID_PENJUALAN, :NO_TRANSAKSI, :BARIS, :PRODUCT_ID, :KODE_BARANG, :BARCODE, :NAMA_BARANG, :SATUAN, :HPP,:HARGA_BARANG, :JUMLAH_BARANG, :BRUTO, :POTONGAN, :TOTAL_HARGA)";

                foreach (var detail in ListItemsPenjualan)
                {
                    detail.ID_PENJUALAN = newPenjualanId;
                    detail.NO_TRANSAKSI = faktur_header.NO_TRANSAKSI;
                }
                conn.Execute(insertFakturJual_Detail, ListItemsPenjualan, transaction);

                // Delete pending record within the same transaction
                if (!string.IsNullOrEmpty(pendingNoTransaksi))
                {
                    conn.Execute("DELETE FROM POS_PENDING_DETAIL WHERE NO_TRANSAKSI = :notransaksi", new { notransaksi = pendingNoTransaksi }, transaction);
                    conn.Execute("DELETE FROM POS_PENDING WHERE NO_TRANSAKSI = :notransaksi", new { notransaksi = pendingNoTransaksi }, transaction);
                }

                transaction.Commit();
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
        }


        public void InsertFaktur_Penjualan_Angsuran(DTOFakturPenjualanHeader faktur_header, List<DTOFakturPenjualanDetail> ListItemsPenjualan, List<DTOAngsuranKreditBarang> DaftarWaktuTagihan, CreditLimitCheck? creditCheck = null, string? pendingNoTransaksi = null)
        {
            using OracleConnection conn = new(Global.connectionString);
            conn.Open();
            OracleTransaction transaction = conn.BeginTransaction();

            try
            {
                ValidatePeriodeTerbuka(conn, transaction, faktur_header.TANGGAL, faktur_header.STATUS);

                // Validate credit limit inside the same transaction
                if (creditCheck != null)
                {
                    ValidateCreditLimit(conn, transaction, creditCheck);
                }

                // Insert master record
                string insertFakturJual_Master = "INSERT INTO POS_PENJUALAN (NO_TRANSAKSI, TANGGAL, JAM, KASIR, NAMA_KASIR, ID_PELANGGAN, NIK, NAMA_PELANGGAN, UNIT_KERJA, STATUS, BRUTO, POTONGAN, TOTAL, JENIS_BAYAR, KET_BAYAR, TENOR, ANGSURAN) VALUES " +
                                                "(:NO_TRANSAKSI, :TANGGAL, :JAM, :KASIR, :NAMA_KASIR, :ID_PELANGGAN, :NIK, :NAMA_PELANGGAN, :UNIT_KERJA, :STATUS, :BRUTO, :POTONGAN, :TOTAL, :JENIS_BAYAR, :KET_BAYAR, :TENOR, :ANGSURAN) RETURNING ID_PENJUALAN INTO :penjualanId";

                var masterParameters = new DynamicParameters(faktur_header);
                masterParameters.Add("penjualanId", dbType: DbType.Int32, direction: ParameterDirection.Output);

                conn.Execute(insertFakturJual_Master, masterParameters, transaction);

                Int32 newPenjualanId = masterParameters.Get<Int32>("penjualanId");

                // Insert sales details
                string insertFakturJual_Detail = "INSERT INTO POS_PENJUALAN_DETAIL (ID_PENJUALAN, NO_TRANSAKSI, BARIS, PRODUCT_ID, KODE_BARANG, BARCODE, NAMA_BARANG, SATUAN, HPP,HARGA_BARANG, JUMLAH_BARANG, BRUTO, POTONGAN, TOTAL_HARGA) " +
                                                 "VALUES (:ID_PENJUALAN, :NO_TRANSAKSI, :BARIS, :PRODUCT_ID, :KODE_BARANG, :BARCODE, :NAMA_BARANG, :SATUAN, :HPP,:HARGA_BARANG, :JUMLAH_BARANG, :BRUTO, :POTONGAN, :TOTAL_HARGA)";

                foreach (var detail in ListItemsPenjualan)
                {
                    detail.ID_PENJUALAN = newPenjualanId;
                    detail.NO_TRANSAKSI = faktur_header.NO_TRANSAKSI;
                }
                conn.Execute(insertFakturJual_Detail, ListItemsPenjualan, transaction);

                // Insert credit installment information
                string insertTagihanKredit = "INSERT INTO POS_KREDIT_ANGSURAN (PERIODE, NO_TRANSAKSI, TANGGALJATUHTEMPO, ANGSURANKE, SALDOAWAL,ANGSURAN,SALDOAKHIR) " +
                                             "VALUES (:PERIODE, :NO_TRANSAKSI, :TANGGALJATUHTEMPO, :ANGSURANKE, :SALDOAWAL,:ANGSURAN,:SALDOAKHIR)";

                foreach (var detail in DaftarWaktuTagihan)
                {
                    detail.NO_TRANSAKSI = faktur_header.NO_TRANSAKSI;
                }
                conn.Execute(insertTagihanKredit, DaftarWaktuTagihan, transaction);

                // Delete pending record within the same transaction
                if (!string.IsNullOrEmpty(pendingNoTransaksi))
                {
                    conn.Execute("DELETE FROM POS_PENDING_DETAIL WHERE NO_TRANSAKSI = :notransaksi", new { notransaksi = pendingNoTransaksi }, transaction);
                    conn.Execute("DELETE FROM POS_PENDING WHERE NO_TRANSAKSI = :notransaksi", new { notransaksi = pendingNoTransaksi }, transaction);
                }

                transaction.Commit();
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
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

        // Load the POS_PERIODE rows around the date (previous, same and next yyyyMM). The period a
        // date belongs to is then picked by configured range (PeriodeResolver), not calendar month,
        // so non-calendar periods (e.g. 26th..25th) resolve to the right row.
        internal static List<PeriodeRow> LoadPeriodeSekitar(OracleConnection conn, OracleTransaction? transaction, DateTime date)
        {
            static int Ym(DateTime d) => d.Year * 100 + d.Month;

            const string sql = @"
                SELECT PERIODE,
                       TO_DATE(R1DARI,   'DD-MON-YYYY', 'NLS_DATE_LANGUAGE = ENGLISH') R1DARI,
                       TO_DATE(R1SAMPAI, 'DD-MON-YYYY', 'NLS_DATE_LANGUAGE = ENGLISH') R1SAMPAI,
                       TO_DATE(R2DARI,   'DD-MON-YYYY', 'NLS_DATE_LANGUAGE = ENGLISH') R2DARI,
                       TO_DATE(R2SAMPAI, 'DD-MON-YYYY', 'NLS_DATE_LANGUAGE = ENGLISH') R2SAMPAI,
                       TO_DATE(BDARI,    'DD-MON-YYYY', 'NLS_DATE_LANGUAGE = ENGLISH') BDARI,
                       TO_DATE(BSAMPAI,  'DD-MON-YYYY', 'NLS_DATE_LANGUAGE = ENGLISH') BSAMPAI,
                       REMISE1, REMISE2, BULANAN
                FROM POS_PERIODE
                WHERE PERIODE IN (:p0, :p1, :p2)
                ORDER BY PERIODE";

            using var cmd = new OracleCommand(sql, conn) { BindByName = true };
            if (transaction != null) cmd.Transaction = transaction;
            cmd.Parameters.Add("p0", OracleDbType.Int32).Value = Ym(date.AddMonths(-1));
            cmd.Parameters.Add("p1", OracleDbType.Int32).Value = Ym(date);
            cmd.Parameters.Add("p2", OracleDbType.Int32).Value = Ym(date.AddMonths(1));

            static DateTime? D(OracleDataReader r, string col) =>
                r.IsDBNull(r.GetOrdinal(col)) ? null : r.GetDateTime(r.GetOrdinal(col));
            static bool Y(OracleDataReader r, string col) =>
                string.Equals(r[col]?.ToString()?.Trim(), "Y", StringComparison.OrdinalIgnoreCase);

            var rows = new List<PeriodeRow>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                rows.Add(new PeriodeRow
                {
                    Periode = Convert.ToInt32(reader["PERIODE"]),
                    R1Dari = D(reader, "R1DARI"),
                    R1Sampai = D(reader, "R1SAMPAI"),
                    R2Dari = D(reader, "R2DARI"),
                    R2Sampai = D(reader, "R2SAMPAI"),
                    BDari = D(reader, "BDARI"),
                    BSampai = D(reader, "BSAMPAI"),
                    Remise1Tutup = Y(reader, "REMISE1"),
                    Remise2Tutup = Y(reader, "REMISE2"),
                    BulananTutup = Y(reader, "BULANAN")
                });
            }
            return rows;
        }

        // Rejects a faktur whose date has no configured period or falls in a closed one.
        internal static void ValidatePeriodeTerbuka(OracleConnection conn, OracleTransaction transaction, DateTime tanggal, string? status)
        {
            var alasan = PeriodeResolver.AlasanTolak(LoadPeriodeSekitar(conn, transaction, tanggal), tanggal, status);
            if (alasan != null)
                throw new PeriodeTidakValidException(alasan);
        }

        // Credit window for the member, or throws when the date is not covered by any period.
        private static (DateTime Dari, DateTime Sampai) ResolveCreditWindow(
            OracleConnection conn, OracleTransaction? transaction, DateTime date, string? status)
        {
            var window = PeriodeResolver.CreditWindow(LoadPeriodeSekitar(conn, transaction, date), date, status);
            return window ?? throw new PeriodeTidakValidException(
                $"Tanggal {date:dd-MM-yyyy} belum masuk periode manapun di Setting Periode. Hubungi admin.");
        }

        // Sum the member's credit spend (POS_PENJUALAN.TOTAL) within a period window (whole days).
        private static decimal SumPeriodSpend(
            OracleConnection conn, OracleTransaction? transaction, string nik, DateTime dari, DateTime sampai)
        {
            using var cmd = new OracleCommand(
                "SELECT NVL(SUM(TOTAL), 0) FROM POS_PENJUALAN WHERE NIK = :Nik AND TANGGAL >= :Dari AND TANGGAL < :SampaiPlus1", conn);
            if (transaction != null) cmd.Transaction = transaction;
            cmd.Parameters.Add("Nik", OracleDbType.Varchar2).Value = nik;
            cmd.Parameters.Add("Dari", OracleDbType.Date).Value = dari.Date;
            cmd.Parameters.Add("SampaiPlus1", OracleDbType.Date).Value = sampai.Date.AddDays(1);
            return Convert.ToDecimal(cmd.ExecuteScalar());
        }

        // Non-locking read of the member's credit spend in the current period, for the UI
        // pre-check. The authoritative, transaction-scoped check lives in ValidateCreditLimit.
        public decimal GetPeriodCreditSpend(string nik, string status, DateTime date)
        {
            using var connection = new OracleConnection(Global.connectionString);
            connection.Open();

            var window = ResolveCreditWindow(connection, null, date, status);
            return SumPeriodSpend(connection, null, nik, window.Dari, window.Sampai);
        }

        // Non-locking UI check: null when a transaction on the date is allowed, else the reason.
        public string? CekPeriodeTransaksi(DateTime tanggal, string? status)
        {
            using var connection = new OracleConnection(Global.connectionString);
            connection.Open();
            return PeriodeResolver.AlasanTolak(LoadPeriodeSekitar(connection, null, tanggal), tanggal, status);
        }

        internal static void ValidateCreditLimit(OracleConnection conn, OracleTransaction transaction, CreditLimitCheck creditCheck)
        {
            // Lock the member row first. This serializes concurrent sales for the same
            // member (so the limit holds even on a first purchase) and lets us read the
            // authoritative limit from the DB instead of trusting the client value.
            decimal limit;
            using (var limitCmd = new OracleCommand(
                "SELECT NVL(LIMIT_HUTANG, 0) FROM FIN_ANGGOTA WHERE NIK = :Nik FOR UPDATE", conn))
            {
                limitCmd.Transaction = transaction;
                limitCmd.Parameters.Add("Nik", OracleDbType.Varchar2).Value = creditCheck.NIK;
                object? result = limitCmd.ExecuteScalar();
                if (result == null)
                    return; // member not found — nothing to enforce
                limit = Convert.ToDecimal(result);
            }

            // A limit of 0 means "unlimited" (no ceiling enforced).
            if (limit == 0)
                return;

            // Per-period spending cap: existing credit spend in the period + this invoice
            // must not exceed the limit. The cap resets each period. An unconfigured period
            // rejects the sale instead of silently skipping the check.
            var window = ResolveCreditWindow(conn, transaction, creditCheck.TransactionDate, creditCheck.STATUS);

            decimal periodSpend = SumPeriodSpend(conn, transaction, creditCheck.NIK, window.Dari, window.Sampai);

            if (CreditLimitPolicy.IsExceeded(periodSpend, creditCheck.InvoiceAmount, limit))
            {
                throw new CreditLimitExceededException(periodSpend, creditCheck.InvoiceAmount, limit);
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

    }
}
