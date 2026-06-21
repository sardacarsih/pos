using BackOffice.Interface;
using BackOffice.Model;
using BackOffice.UC;
using Dapper;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace BackOffice.DataLayer
{
    public class StockOpnamenRepository : IStockOpname
    {
        // Stock Opname and Barang Rusak share a single running number sequence.
        private const string TransactionCode = "STOCK_OPNAME";

        // Legacy single-row mirror of the latest allocated number. Kept for any
        // external readers; POS_TRANSACTION_COUNTER is the authoritative source.
        private const string MergeLegacyNumberSql = @"
            MERGE INTO nomor_transaksi target
            USING (
                SELECT 'STOCK_OPNAME' AS kode, :nomor AS nomor
                FROM dual
            ) source
            ON (target.kode = source.kode)
            WHEN MATCHED THEN
                UPDATE SET target.nomor = source.nomor
            WHEN NOT MATCHED THEN
                INSERT (kode, nomor) VALUES (source.kode, source.nomor)";

        public string SaveStockOpname(
            DateTime transactionDate,
            IReadOnlyCollection<TransactionStockOpname> items)
        {
            if (!StockOpnameValidator.TryValidate(transactionDate, items, out string validationError))
            {
                throw new ArgumentException(validationError, nameof(items));
            }

            using OracleConnection conn = new(global.connectionString);
            conn.Open();
            using OracleTransaction transaction = conn.BeginTransaction();

            try
            {
                string transactionNumber = AllocateTransactionNumber(
                    conn,
                    transaction,
                    transactionDate.Year);

                const string insertStockOpname = @"
                    INSERT INTO POS_STOCKOPNAME
                    (NOMOR_SO, TANGGAL, KODE_BARANG, JUMLAHSISTEM, JUMLAHFISIK, SELISIH, HPP, TOTAL)
                    VALUES
                    (:p_NOMOR_SO, :p_TANGGAL, :p_KODE_BARANG, :p_JUMLAHSISTEM, :p_JUMLAHFISIK, :p_SELISIH, :p_HPP, :p_TOTAL)";

                // Parameter names differ from the column names, so map explicitly.
                conn.Execute(insertStockOpname,
                    items.Select(opname => new
                    {
                        p_NOMOR_SO = transactionNumber,
                        p_TANGGAL = transactionDate.Date,
                        p_KODE_BARANG = opname.Kode_Item.Trim(),
                        p_JUMLAHSISTEM = opname.QtySystem,
                        p_JUMLAHFISIK = opname.QtyFisik,
                        p_SELISIH = opname.Selisih,
                        p_HPP = opname.Hpp,
                        p_TOTAL = opname.Total
                    }),
                    transaction);

                conn.Execute(MergeLegacyNumberSql, new { nomor = transactionNumber }, transaction);

                transaction.Commit();
                return transactionNumber;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public string Insert_BarangRusak(
            DateTime transactionDate,
            IReadOnlyCollection<TransactionBarangRusak> items)
        {
            ValidateBarangRusak(transactionDate, items);

            using OracleConnection conn = new(global.connectionString);
            conn.Open();
            using OracleTransaction transaction = conn.BeginTransaction();

            try
            {
                string transactionNumber = AllocateTransactionNumber(
                    conn,
                    transaction,
                    transactionDate.Year);

                const string insertBarangRusak = @"
                    INSERT INTO POS_BARANGRUSAK
                    (NOMOR_SO, TANGGAL, KODE_BARANG, JUMLAHFISIK, HPP, TOTAL, KETERANGAN)
                    VALUES
                    (:p_NOMOR_SO, :p_TANGGAL, :p_KODE_BARANG, :p_JUMLAHFISIK, :p_HPP, :p_TOTAL, :p_KETERANGAN)";

                conn.Execute(insertBarangRusak,
                    items.Select(rusak => new
                    {
                        p_NOMOR_SO = transactionNumber,
                        p_TANGGAL = transactionDate.Date,
                        p_KODE_BARANG = rusak.Kode_Item.Trim(),
                        p_JUMLAHFISIK = rusak.QtyFisik,
                        p_HPP = rusak.Hpp,
                        p_TOTAL = rusak.Total,
                        p_KETERANGAN = rusak.Keterangan
                    }),
                    transaction);

                conn.Execute(MergeLegacyNumberSql, new { nomor = transactionNumber }, transaction);

                transaction.Commit();
                return transactionNumber;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        private static void ValidateBarangRusak(
            DateTime transactionDate,
            IReadOnlyCollection<TransactionBarangRusak> items)
        {
            if (transactionDate == default)
            {
                throw new ArgumentException("Tanggal Barang Rusak tidak valid.", nameof(transactionDate));
            }

            if (items is null || items.Count == 0)
            {
                throw new ArgumentException("Daftar Barang Rusak masih kosong.", nameof(items));
            }

            foreach (TransactionBarangRusak item in items)
            {
                if (string.IsNullOrWhiteSpace(item.Kode_Item))
                {
                    throw new ArgumentException("Terdapat baris dengan kode barang kosong.", nameof(items));
                }

                if (item.QtyFisik <= 0)
                {
                    throw new ArgumentException(
                        $"Qty Fisik {item.ProductName} harus lebih besar dari nol.", nameof(items));
                }

                if (item.Hpp < 0)
                {
                    throw new ArgumentException(
                        $"HPP {item.ProductName} tidak boleh negatif.", nameof(items));
                }
            }
        }

        private static string AllocateTransactionNumber(
            OracleConnection connection,
            OracleTransaction transaction,
            int transactionYear)
        {
            const string updateCounter = @"
                UPDATE POS_TRANSACTION_COUNTER
                SET LAST_NUMBER = LAST_NUMBER + 1
                WHERE TRANSACTION_CODE = :code
                  AND TRANSACTION_YEAR = :year";

            var parameters = new
            {
                code = TransactionCode,
                year = transactionYear
            };

            int affectedRows = connection.Execute(updateCounter, parameters, transaction);
            if (affectedRows == 0)
            {
                const string insertCounter = @"
                    INSERT INTO POS_TRANSACTION_COUNTER
                        (TRANSACTION_CODE, TRANSACTION_YEAR, LAST_NUMBER)
                    VALUES (:code, :year, 1)";

                try
                {
                    connection.Execute(insertCounter, parameters, transaction);
                }
                catch (OracleException ex) when (ex.Number == 1)
                {
                    connection.Execute(updateCounter, parameters, transaction);
                }
            }

            const string selectCounter = @"
                SELECT LAST_NUMBER
                FROM POS_TRANSACTION_COUNTER
                WHERE TRANSACTION_CODE = :code
                  AND TRANSACTION_YEAR = :year";

            int sequenceNumber = connection.QuerySingle<int>(
                selectCounter,
                parameters,
                transaction);

            return $"SO-{transactionYear % 100:D2}-{sequenceNumber:D6}";
        }

        public void UpdateTransactionNumber(string transactionNumber)
        {
            using OracleConnection connection = new(global.connectionString);
            connection.Open();

            connection.Execute(MergeLegacyNumberSql, new { nomor = transactionNumber });
        }

        public void HapusStockOpname(string no_faktur)
        {
            using OracleConnection connection = new(global.connectionString);
            connection.Open();
            using OracleTransaction transaction = connection.BeginTransaction();

            try
            {
                const string deleteQuery =
                    "DELETE FROM POS_STOCKOPNAME WHERE NOMOR_SO = :nomor";

                int affectedRows = connection.Execute(
                    deleteQuery,
                    new { nomor = no_faktur },
                    transaction);

                if (affectedRows == 0)
                {
                    throw new InvalidOperationException(
                        $"Stock Opname {no_faktur} tidak ditemukan.");
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public bool CheckStockOpname(string kodeBarang, DateTime SODate)
        {
            using OracleConnection connection = new(global.connectionString);
            connection.Open();

            const string sql =
                "SELECT COUNT(1) FROM POS_STOCKOPNAME WHERE KODE_BARANG = :p_kode_barang AND tanggal >= :p_tanggal";

            int count = connection.ExecuteScalar<int>(
                sql,
                new { p_kode_barang = kodeBarang, p_tanggal = SODate });

            return count > 0;
        }

        public DTOStockDataItem GetStockData(string kodeBarang, DateTime startDate, DateTime endDate)
        {
            using OracleConnection connection = new(global.connectionString);
            connection.Open();

            DTOStockData row = connection.QueryFirstOrDefault<DTOStockData>(
                BuildStockInfoQuery(singleItem: true),
                new
                {
                    start_date = startDate.Date,
                    end_date = endDate.Date,
                    p_kode_barang = kodeBarang
                });

            if (row is null)
            {
                return null;
            }

            return new DTOStockDataItem
            {
                KodeBarang = row.KODE_ITEM,
                StockQty = row.STOCKAWAL_QTY,
                BeliQty = row.BELI_QTY,
                JualQty = row.JUAL_QTY,
                StockOpname = row.STOCK_OPNAME,
                StockAkhir = row.STOCK_AKHIR,
                TotalCostAvg = row.TOTAL_COST_AVG
            };
        }

        public List<DTOStockData> GetProductStockInfo(DateTime startDate, DateTime endDate)
        {
            using IDbConnection dbConnection = new OracleConnection(global.connectionString);
            dbConnection.Open();

            return dbConnection.Query<DTOStockData>(
                BuildStockInfoQuery(singleItem: false),
                new
                {
                    start_date = startDate.Date,
                    end_date = endDate.Date
                }).AsList();
        }

        // Single source of truth for the running-stock calculation. Both the
        // single-item lookup (GetStockData) and the all-items report
        // (GetProductStockInfo) use this so the Qty System and HPP shown when an
        // operator adds one item match what bulk import / year-end closing use.
        private static string BuildStockInfoQuery(bool singleItem)
        {
            string openingFilter = singleItem ? " AND kode_barang = :p_kode_barang" : string.Empty;
            string detailFilter = singleItem ? " AND detail.kode_barang = :p_kode_barang" : string.Empty;
            string flatFilter = singleItem ? " AND kode_barang = :p_kode_barang" : string.Empty;
            string productFilter = singleItem ? "WHERE product.kode_item = :p_kode_barang" : string.Empty;

            return $@"
                SELECT
                    product.kode_item,
                    product.productname,
                    product.satuan,
                    NVL(opening.quantity, 0) AS stock_qty,
                    NVL(opening.hpp, 0) AS stock_hpp,
                    NVL(purchases.quantity, 0) AS beli_qty,
                    NVL(purchases.harga_beli, 0) AS beli_harga_avg,
                    CASE
                        WHEN NVL(opening.quantity, 0) + NVL(purchases.quantity, 0) = 0
                            THEN NVL(purchases.harga_beli, NVL(opening.hpp, 0))
                        ELSE ROUND(
                            (
                                NVL(opening.quantity, 0) * NVL(opening.hpp, 0)
                                + NVL(purchases.quantity, 0) * NVL(purchases.harga_beli, 0)
                            )
                            / (NVL(opening.quantity, 0) + NVL(purchases.quantity, 0)),
                            2)
                    END AS total_cost_avg,
                    NVL(sales.quantity, 0) AS jual_qty,
                    NVL(opname.quantity, 0) AS stock_opname,
                    NVL(opname.item_count, 0) AS stock_opname_count,
                    NVL(opening.quantity, 0)
                        + NVL(purchases.quantity, 0)
                        + NVL(opname.quantity, 0)
                        - NVL(sales.quantity, 0)
                        - NVL(damaged.quantity, 0) AS stock_akhir
                FROM POS_PRODUCT product
                LEFT JOIN (
                    SELECT kode_barang, SUM(quantity) AS quantity, MAX(hpp) AS hpp
                    FROM POS_STOCK
                    WHERE tanggal = :start_date{openingFilter}
                    GROUP BY kode_barang
                ) opening ON opening.kode_barang = product.kode_item
                LEFT JOIN (
                    SELECT detail.kode_barang,
                           SUM(detail.quantity) AS quantity,
                           AVG(detail.harga_beli) AS harga_beli
                    FROM POS_PEMBELIANDETAIL detail
                    JOIN POS_PEMBELIAN header
                      ON header.purchase_id = detail.purchase_id
                    WHERE header.tanggal BETWEEN :start_date AND :end_date{detailFilter}
                    GROUP BY detail.kode_barang
                ) purchases ON purchases.kode_barang = product.kode_item
                LEFT JOIN (
                    SELECT detail.kode_barang, SUM(detail.jumlah_barang) AS quantity
                    FROM POS_PENJUALAN_DETAIL detail
                    JOIN POS_PENJUALAN header
                      ON header.no_transaksi = detail.no_transaksi
                    WHERE header.tanggal BETWEEN :start_date AND :end_date{detailFilter}
                    GROUP BY detail.kode_barang
                ) sales ON sales.kode_barang = product.kode_item
                LEFT JOIN (
                    SELECT kode_barang, SUM(jumlahfisik) AS quantity
                    FROM POS_BARANGRUSAK
                    WHERE tanggal BETWEEN :start_date AND :end_date{flatFilter}
                    GROUP BY kode_barang
                ) damaged ON damaged.kode_barang = product.kode_item
                LEFT JOIN (
                    SELECT kode_barang,
                           SUM(selisih) AS quantity,
                           COUNT(*) AS item_count
                    FROM POS_STOCKOPNAME
                    WHERE tanggal BETWEEN :start_date AND :end_date{flatFilter}
                    GROUP BY kode_barang
                ) opname ON opname.kode_barang = product.kode_item
                {productFilter}
                ORDER BY product.productname";
        }

        public List<DTOStoctOpnameMaster> DaftarStockOpname(int tahun)
        {
            using IDbConnection dbConnection = new OracleConnection(global.connectionString);
            dbConnection.Open();

            string masterQuery = "SELECT DISTINCT TO_CHAR(tanggal, 'Month') AS Bulan, Nomor_SO, Tanggal,sum(selisih*hpp)total FROM pos_stockopname  WHERE EXTRACT(YEAR FROM tanggal) = :Tahun group by Nomor_SO,tanggal ORDER BY Nomor_SO";

            var masterResults = dbConnection.Query<DTOStoctOpnameMaster>(masterQuery, new { Tahun = tahun }).AsList();

            foreach (var masterdetailResult in masterResults)
            {
                // Load details for each master record using the new query
                string detailQuery = "SELECT S.NOMOR_SO, S.KODE_BARANG, B.PRODUCTNAME, S.JUMLAHSISTEM, S.JUMLAHFISIK, S.SELISIH , S.HPP, S.TOTAL " +
                                     "FROM pos_stockopname S " +
                                     "JOIN POS_PRODUCT B ON B.KODE_ITEM = S.KODE_BARANG " +
                                     "WHERE EXTRACT(YEAR FROM S.tanggal) = :Tahun AND S.NOMOR_SO = :NomorSO ORDER BY B.PRODUCTNAME";

                var detailResults = dbConnection.Query<DTOStoctOpnameDetail>(detailQuery, new { Tahun = tahun, NomorSO = masterdetailResult.NOMOR_SO }).AsList();

                // Optionally, associate the details with the master record
                masterdetailResult.Details = detailResults;
            }

            return masterResults;
        }
    }
}
