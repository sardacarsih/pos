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
        public decimal GetStocItem(string kodeBarang, DateTime startDate, DateTime endDate)
        {
            using OracleConnection connection = new(global.connectionString);
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

        public void InsertFaktur_Penjualan(DTOFakturPenjualanHeader faktur_header, List<DTOFakturPenjualanDetail> ListItemsPenjualan)
        {
            using OracleConnection conn = new(global.connectionString);
            conn.Open();
            OracleTransaction transaction = conn.BeginTransaction();

            try
            {
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
            catch (Exception ex)
            {
                transaction.Rollback();
                // Handle or log the exception here
                throw ex;
            }
        }


        public void InsertFaktur_Penjualan_Angsuran(DTOFakturPenjualanHeader faktur_header, List<DTOFakturPenjualanDetail> ListItemsPenjualan, List<DTOAngsuranKreditBarang> DaftarWaktuTagihan)
        {
            using OracleConnection conn = new(global.connectionString);
            conn.Open();
            OracleTransaction transaction = conn.BeginTransaction();

            try
            {
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

            using (OracleConnection connection = new(global.connectionString))
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
            using OracleConnection connection = new(global.connectionString);

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

        public void UpdateTransactionNumber(string transactionNumber)
        {
            int nomorbaru = 0;
            int nomorexist=0;
            using OracleConnection connection = new(global.connectionString);
            connection.Open();

            // Check if record exists
            string checkQuery = "SELECT COUNT(*) FROM nomor_transaksi WHERE kode = 'PENJUALAN'";
            using OracleCommand checkCommand = new(checkQuery, connection);
            int count = Convert.ToInt32(checkCommand.ExecuteScalar());

            if (count == 0)
            {
                // Insert new record
                string insertQuery = "INSERT INTO nomor_transaksi (kode, nomor) VALUES ('PENJUALAN', :nomor)";
                using OracleCommand insertCommand = new(insertQuery, connection);
                insertCommand.Parameters.Add("nomor", transactionNumber);
                insertCommand.ExecuteNonQuery();
            }
            else
            {
                // Check existing transaction number
                string selectQuery = "SELECT nomor FROM nomor_transaksi WHERE kode = 'PENJUALAN'";
                using OracleCommand selectCommand = new(selectQuery, connection);
                string existingTransactionNumber = selectCommand.ExecuteScalar()?.ToString();

                // Remove non-numeric characters from the transactionNumber
                string numericTransactionExist = Regex.Replace(existingTransactionNumber, @"[^\d]", "");
                nomorexist = int.Parse(numericTransactionExist);
                string numericTransactionNew = Regex.Replace(transactionNumber, @"[^\d]", "");
                nomorbaru = int.Parse(numericTransactionNew);

               
                if (nomorbaru > nomorexist)
                {
                    // Update existing record
                    string updateQuery = "UPDATE nomor_transaksi SET nomor = :nomor WHERE kode = 'PENJUALAN'";
                    using OracleCommand updateCommand = new(updateQuery, connection);
                    updateCommand.Parameters.Add("nomor", transactionNumber);
                    updateCommand.ExecuteNonQuery();
                }
                // If existingTransactionNumber is already the same as transactionNumber, no update is needed.


            }
        }

    }
}
