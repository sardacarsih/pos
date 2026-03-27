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
        

        private List<DTOFakturPembelianDetail> GetItemBarang(string idtransaksi)
        {
            List<DTOFakturPembelianDetail> Detail = new();

            string query = "SELECT BARIS,PRODUCT_ID,KODE_BARANG,NAMA_BARANG,SATUAN,quantity,HARGA_BELI,BRUTO,POTONGAN,TOTAL FROM  POS_PEMBELIANDETAIL WHERE NO_TRANSAKSI = :idtransaksi ORDER BY BARIS";

            using (OracleConnection connection = new(global.connectionString))
            {
                connection.Open();

                using OracleCommand command = new(query, connection);
                command.Parameters.Add(":idtransaksi", OracleDbType.Varchar2).Value = idtransaksi;

                using OracleDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    DTOFakturPembelianDetail DaftarBarang = new()
                    {
                        NO_TRANSAKSI= idtransaksi,
                        BARIS = Convert.ToInt32(reader["BARIS"]),
                        PRODUCT_ID = Convert.ToInt32(reader["PRODUCT_ID"]),
                        KODE_BARANG = reader["KODE_BARANG"].ToString(),
                        NAMA_BARANG = reader["NAMA_BARANG"].ToString(),
                        SATUAN = reader["SATUAN"].ToString(),
                        QUANTITY = Convert.ToInt32(reader["QUANTITY"]),
                        HARGA_BELI = Convert.ToDecimal(reader["HARGA_BELI"]),
                        BRUTO = Convert.ToDecimal(reader["BRUTO"]),
                        POTONGAN = Convert.ToDecimal(reader["POTONGAN"]),
                        TOTAL = Convert.ToDecimal(reader["TOTAL"])
                    };

                    Detail.Add(DaftarBarang);
                }
            }

            return Detail;
        }

        public List<DTODaftarBarang> GetDaftarBarang(string idtransaksi)
        {
            List<DTODaftarBarang> Detail = new();

            string query = "SELECT BARIS,NAMA_BARANG,SATUAN,JUMLAH_BARANG,HARGA_BARANG,POTONGAN,TOTAL_HARGA FROM  POS_PENJUALAN_DETAIL WHERE NO_TRANSAKSI = :idtransaksi ORDER BY BARIS";

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


        public void Insert_StockOpname(List<TransactionStockOpname> StockOpname_List)
        {
            using OracleConnection conn = new(global.connectionString);
            conn.Open();
            OracleTransaction transaction = conn.BeginTransaction();

            try
            {
                string insert_Stock_Opname = @"
                INSERT INTO POS_STOCKOPNAME 
                (NOMOR_SO, TANGGAL, KODE_BARANG, JUMLAHSISTEM, JUMLAHFISIK, SELISIH, HPP,TOTAL) 
                VALUES 
                (:p_NOMOR_SO, :p_TANGGAL, :p_KODE_BARANG, :p_JUMLAHSISTEM, :p_JUMLAHFISIK, :p_SELISIH, :p_HPP, :p_TOTAL)";

                // Adjust the Dapper call to use parameters explicitly
                //karena nama pada database dan object ga sama persis
                conn.Execute(insert_Stock_Opname,
                    StockOpname_List.Select(opname => new {
                        p_NOMOR_SO = opname.Nomor_SO,
                        p_TANGGAL = opname.Tanggal,
                        p_KODE_BARANG = opname.Kode_Item,
                        p_JUMLAHSISTEM = opname.QtySystem,
                        p_JUMLAHFISIK = opname.QtyFisik,
                        p_SELISIH = opname.Selisih,
                        p_HPP = opname.Hpp,
                        p_TOTAL=opname.Total
                    }),
                    transaction);

                transaction.Commit();
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
        }

        public void UpdateTransactionNumber(string transactionNumber)
        {
            using OracleConnection connection = new(global.connectionString);
            connection.Open();

            // Check if record exists
            string checkQuery = "SELECT COUNT(*) FROM nomor_transaksi WHERE kode = 'STOCK_OPNAME'";
            using OracleCommand checkCommand = new(checkQuery, connection);
            int count = Convert.ToInt32(checkCommand.ExecuteScalar());

            if (count == 0)
            {
                // Insert new record
                string insertQuery = "INSERT INTO nomor_transaksi (kode, nomor) VALUES ('STOCK_OPNAME', :nomor)";
                using OracleCommand insertCommand = new(insertQuery, connection);
                insertCommand.Parameters.Add("nomor", transactionNumber);
                insertCommand.ExecuteNonQuery();
            }
            else
            {

                // Update existing record
                string updateQuery = "UPDATE nomor_transaksi SET nomor = :nomor WHERE kode = 'STOCK_OPNAME'";
                using OracleCommand updateCommand = new(updateQuery, connection);
                updateCommand.Parameters.Add("nomor", transactionNumber);
                updateCommand.ExecuteNonQuery();

            }
        }

        public void HapusStockOpname(string no_faktur)
        {
            using OracleConnection connection = new(global.connectionString);
            connection.Open();

            // DELETE existing record
            string deleteQuery = "DELETE FROM POS_PEMBELIAN WHERE NO_TRANSAKSI = :nomor";

            using OracleCommand deleteCommand = new(deleteQuery, connection);
            deleteCommand.Parameters.Add("nomor", no_faktur);
            deleteCommand.ExecuteNonQuery();
        }


        public void EditStockOpname(string no_faktur)
        {
            throw new NotImplementedException();
        }

        public void CetakStockOpname(string no_faktur)
        {
            throw new NotImplementedException();
        }

        public List<DTODaftarBarang> GetDaftarPembelianBarang(string idtransaksi)
        {
            List<DTODaftarBarang> Detail = new();

            string query = @"SELECT B.BARIS,B.PRODUCT_ID,B.KODE_BARANG,B.NAMA_BARANG,B.SATUAN,B.QUANTITY,B.HARGA_BELI,M.PRICE HARGA_JUAL,B.BRUTO,B.POTONGAN,B.TOTAL FROM  POS_PEMBELIANDETAIL B
JOIN POS_PRODUCT M ON M.PRODUCTID=B.PRODUCT_ID
WHERE B.NO_TRANSAKSI = :idtransaksi ORDER BY B.BARIS";

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
                        BARIS = Convert.ToInt32(reader["BARIS"]),
                        PRODUCTID = Convert.ToInt32(reader["PRODUCT_ID"]),
                        KODE_BARANG = reader["KODE_BARANG"].ToString(),
                        NAMA_BARANG = reader["NAMA_BARANG"].ToString(),
                        SATUAN = reader["SATUAN"].ToString(),
                        JUMLAH_BARANG = Convert.ToInt32(reader["QUANTITY"]),
                        HPP = Convert.ToDecimal(reader["HARGA_BELI"]),
                        HARGA_BARANG = Convert.ToDecimal(reader["HARGA_JUAL"]),
                        BRUTO = Convert.ToDecimal(reader["BRUTO"]),
                        POTONGAN = Convert.ToDecimal(reader["POTONGAN"]),
                        TOTAL_HARGA = Convert.ToDecimal(reader["TOTAL"])
                    };

                    Detail.Add(DaftarBarang);
                }
            }

            return Detail;
        }
        public DTOStockDataItem GetStockData(string kodeBarang, DateTime startDate, DateTime endDate)
        {
            using OracleConnection connection = new(global.connectionString);
            connection.Open();

            string sqlQuery = @"
                SELECT 
                    all_data.kode_item AS KodeBarang,
                    NVL(stock.quantity, 0) AS StockQty,
                    NVL(purchases.quantity, 0) AS BeliQty,
                    NVL(sales.jumlah_barang, 0) AS JualQty,
                    NVL(STOCKOPNAME.jumlah_barang, 0) AS StockOpname,
                    COALESCE(stock.quantity, 0) + COALESCE(purchases.quantity, 0) + COALESCE(STOCKOPNAME.jumlah_barang, 0) - (COALESCE(sales.jumlah_barang, 0)+ COALESCE(RUSAK.jumlah_barang, 0)) AS StockAkhir
                FROM
                    (SELECT kode_item FROM POS_PRODUCT WHERE kode_item=:p_kode_barang) all_data
                LEFT JOIN
                    (SELECT kode_barang,quantity FROM pos_stock WHERE tanggal = :start_date AND KODE_BARANG=:p_kode_barang) stock ON all_data.kode_item = stock.kode_barang
                LEFT JOIN
                    (SELECT 
                        d.kode_barang,
                        SUM(d.quantity) AS quantity,
                        AVG(d.harga_beli) AS harga_beli
                    FROM pos_pembeliandetail d
                    JOIN pos_pembelian m ON m.purchase_id = d.purchase_id
                    WHERE m.tanggal BETWEEN :start_date AND :end_date AND d.kode_barang = :p_kode_barang
                    GROUP BY d.kode_barang) purchases ON all_data.kode_item = purchases.kode_barang
                LEFT JOIN
                    (SELECT 
                        d.kode_barang,
                        SUM(d.jumlah_barang) AS jumlah_barang
                    FROM pos_penjualan_detail d
                    JOIN pos_penjualan m ON m.no_transaksi = d.no_transaksi
                    WHERE m.tanggal BETWEEN :start_date AND :end_date AND d.kode_barang = :p_kode_barang
                    GROUP BY d.kode_barang) sales ON all_data.kode_item = sales.kode_barang
                LEFT JOIN
                    (SELECT 
                        KODE_BARANG,
                        SUM(JUMLAHFISIK) AS jumlah_barang
                    FROM POS_BARANGRUSAK
                    WHERE TANGGAL BETWEEN :start_date AND :end_date AND KODE_BARANG = :p_kode_barang
                    GROUP BY KODE_BARANG) RUSAK ON all_data.kode_item = RUSAK.kode_barang
                LEFT JOIN
                    (SELECT 
                        KODE_BARANG,
                        SUM(SELISIH) AS jumlah_barang
                    FROM POS_STOCKOPNAME
                    WHERE TANGGAL BETWEEN :start_date AND :end_date AND KODE_BARANG = :p_kode_barang
                    GROUP BY KODE_BARANG) STOCKOPNAME ON all_data.kode_item = STOCKOPNAME.kode_barang";

            var parameters = new
            {
                p_kode_barang = kodeBarang,
                start_date = startDate,
                end_date = endDate
            };

            var result = connection.QueryFirstOrDefault<DTOStockDataItem>(sqlQuery, parameters, commandType: CommandType.Text);

            return result;
        }

        public bool CheckStockOpname(string kodeBarang, DateTime SODate)
        {
            using OracleConnection connection = new(global.connectionString);
            connection.Open();

            using OracleCommand command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(1) FROM POS_STOCKOPNAME WHERE KODE_BARANG = :p_kode_barang AND tanggal >= :p_tanggal";
            command.Parameters.Add(new OracleParameter("p_kode_barang", OracleDbType.Varchar2)).Value = kodeBarang;
            command.Parameters.Add(new OracleParameter("p_tanggal", OracleDbType.Date)).Value = SODate;

            int count = Convert.ToInt32(command.ExecuteScalar());
            return count > 0;
        }
        public List<DTOStockData> GetStockAkhir(DateTime startDate, DateTime endDate, int opsistock)
        {
            List<DTOStockData> stockList;

            using (IDbConnection dbConnection = new OracleConnection(global.connectionString))
            {
                dbConnection.Open();

                // Initial query without WHERE clause
                string sqlQuery = @"
                SELECT 
                    all_data.kode_ITEM,
                    all_data.productname,
                    all_data.SATUAN,
                    NVL(stock.quantity, 0) AS stock_qty,
                    NVL(stock.hpp, 0) AS stock_hpp,
                    NVL(purchases.quantity, 0) AS beli_qty,
                    ROUND(NVL(purchases.harga_beli, 0), 2) AS beli_harga_AVG,
                    NVL(ROUND(((stock.quantity * stock.hpp) + NVL((purchases.quantity * purchases.harga_beli), 0)) / (stock.quantity + NVL(purchases.quantity, 0)), 2), 0) AS TOTAL_COST_AVG,
                    NVL(sales.jumlah_barang, 0) AS jual_qty,
                    NVL(STOCKOPNAME.jumlah_barang, 0) AS stock_opname,
                    COALESCE(stock.quantity, 0) + COALESCE(purchases.quantity, 0) + COALESCE(STOCKOPNAME.jumlah_barang, 0) - COALESCE(sales.jumlah_barang, 0) AS stock_akhir
                FROM
                    (SELECT kode_item, productname, SATUAN FROM POS_PRODUCT ) all_data
                LEFT JOIN
                    (SELECT kode_barang, quantity, hpp FROM pos_stock WHERE tanggal BETWEEN :StartDate AND :EndDate) stock ON all_data.kode_item = stock.kode_barang
                LEFT JOIN
                    (SELECT 
                        d.kode_barang,
                        SUM(d.quantity) AS quantity,
                        AVG(d.harga_beli) AS harga_beli
                    FROM pos_pembeliandetail d
                    JOIN pos_pembelian m ON m.purchase_id = d.purchase_id
                    WHERE m.tanggal BETWEEN :StartDate AND :EndDate
                    GROUP BY d.kode_barang) purchases ON all_data.kode_item = purchases.kode_barang
                LEFT JOIN
                    (SELECT 
                        d.kode_barang,
                        SUM(d.jumlah_barang) AS jumlah_barang
                    FROM pos_penjualan_detail d
                    JOIN pos_penjualan m ON m.no_transaksi = d.no_transaksi
                    WHERE m.tanggal BETWEEN :StartDate AND :EndDate
                    GROUP BY d.kode_barang) sales ON all_data.kode_item = sales.kode_barang
                LEFT JOIN
                    (SELECT 
                        KODE_BARANG,
                        SUM(SELISIH) AS jumlah_barang
                    FROM  POS_STOCKOPNAME
                    WHERE TANGGAL BETWEEN :StartDate AND :EndDate
                    GROUP BY KODE_BARANG) STOCKOPNAME  ON all_data.kode_item = STOCKOPNAME.kode_barang
            ";

                // Dynamic WHERE clause based on opsistock
                //opsistock 0=semua stock || -1 stock minus only
                if (opsistock == 0)
                {
                    sqlQuery += " WHERE COALESCE(stock.quantity, 0) + COALESCE(purchases.quantity, 0) + COALESCE(STOCKOPNAME.jumlah_barang, 0) - COALESCE(sales.jumlah_barang, 0) <> 0";
                }
                else if (opsistock < 0)
                {
                    sqlQuery += " WHERE COALESCE(stock.quantity, 0) + COALESCE(purchases.quantity, 0) + COALESCE(STOCKOPNAME.jumlah_barang, 0) - COALESCE(sales.jumlah_barang, 0) < 0";
                }

                // Order by clause
                sqlQuery += " ORDER BY all_data.PRODUCTNAME ASC";

                var parameters = new { StartDate = startDate, EndDate = endDate };

                stockList = dbConnection.Query<DTOStockData>(sqlQuery, parameters).AsList();
            }

            return stockList;
        }

        public List<DTOStockData> GetProductStockInfo(DateTime startDate, DateTime endDate)
        {
            using IDbConnection dbConnection = new OracleConnection(global.connectionString);
            // Ensure the connection is open
            dbConnection.Open();

            // Execute the Oracle function using Dapper
            var result = dbConnection.Query<DTOStockData>(
                sql: "SELECT * FROM GET_PRODUCT_STOCK_INFO_PIPELINE(:p_start_date, :p_end_date)",
                param: new { p_start_date = startDate, p_end_date = endDate }
            );

            // Optionally, you can handle the result or perform additional actions

            return result.ToList();
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

        public void Insert_BarangRusak(List<TransactionBarangRusak> BarangRusak_List)
        {
            using OracleConnection conn = new(global.connectionString);
            conn.Open();
            OracleTransaction transaction = conn.BeginTransaction();

            try
            {
                string insert_Barang_Rusak = @"
                INSERT INTO POS_BARANGRUSAK
                (NOMOR_SO, TANGGAL, KODE_BARANG, JUMLAHFISIK, HPP,TOTAL, KETERANGAN) 
                VALUES 
                (:p_NOMOR_SO, :p_TANGGAL, :p_KODE_BARANG, :p_JUMLAHFISIK, :p_HPP, :p_TOTAL, :p_KETERANGAN)";

                // Adjust the Dapper call to use parameters explicitly
                //karena nama pada database dan object ga sama persis
                conn.Execute(insert_Barang_Rusak,
                    BarangRusak_List.Select(Rusak => new {
                        p_NOMOR_SO = Rusak.Nomor_SO,
                        p_TANGGAL = Rusak.Tanggal,
                        p_KODE_BARANG = Rusak.Kode_Item,
                        p_JUMLAHFISIK = Rusak.QtyFisik,
                        p_HPP = Rusak.Hpp,
                        p_TOTAL = Rusak.Total,
                        p_KETERANGAN = Rusak.Keterangan
                    }),
                    transaction);

                transaction.Commit();
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}
