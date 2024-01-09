using Oracle.ManagedDataAccess.Client;
using BackOffice.DataLayer;
using Dapper;
using static BackOffice.Model.DTOBackup;
using BackOffice.UC;

namespace BackOffice.BussinessLayer
{

    public class PosBackup
    {
        public List<FinAnggota> LoadAnggota()
        {
            using OracleConnection connection = new(global.connectionString);
            connection.Open();

            string query = @"SELECT
                            NIK,
                            NAMA_PELANGGAN,
                            ALAMAT,
                            KOTA,
                            KODE_POS,
                            NO_TELP,
                            LOKASI,
                            KELOMPOK,
                            LIMIT_HUTANG,
                            STATUS,
                            AKTIF,
                            UNIT_KERJA,
                            TMK,
                            ANGGOTA,
                            TGLNONAKTIF,
                            GAMBAR,
                            UPDATEAT FROM FIN_ANGGOTA";
            List<FinAnggota> anggotaList = connection.Query<FinAnggota>(query).ToList();

            return anggotaList;
        }
        public List<PosProduct> LoadBarang()
        {
            using OracleConnection connection = new(global.connectionString);
            connection.Open();

            string query = @"SELECT
                            KODE_ITEM,
                            PRODUCTNAME,
                            SATUAN,
                            BARCODE,
                            KATEGORI_ID,
                            PRICE,
                            BELI,
                            AKTIF,
                            HPP,
                            LASTUPDATE from POS_PRODUCT";
            List<PosProduct> BarangList = connection.Query<PosProduct>(query).ToList();

            return BarangList;
        }
        public List<PEMBELIANMASTER> LoadPembelian()
        {
            using OracleConnection connection = new(global.connectionString);
            connection.Open();

            string query = @"SELECT 
                            NO_TRANSAKSI,
                            TANGGAL,
                            SUPPLIER_ID,
                            BRUTO,
                            POTONGAN,
                            TOTAL,
                            TERMIN,
                            USERID FROM POS_PEMBELIAN";
            List<PEMBELIANMASTER> PembelianList = connection.Query<PEMBELIANMASTER>(query).ToList();

            return PembelianList;
        }
        public List<PEMBELIANDETAIL> LoadPembelianDetail()
        {
            using OracleConnection connection = new(global.connectionString);
            connection.Open();

            string query = @"SELECT
                            PURCHASE_ID,
                            NO_TRANSAKSI,
                            BARIS,
                            PRODUCT_ID,
                            KODE_BARANG,
                            NAMA_BARANG,
                            SATUAN,
                            QUANTITY,
                            HARGA_BELI,
                            BRUTO,
                            POTONGAN,
                            TOTAL FROM POS_PEMBELIANDETAIL";
            List<PEMBELIANDETAIL> PembelianDetailList = connection.Query<PEMBELIANDETAIL>(query).ToList();

            return PembelianDetailList;
        }
        public List<PENJUALANMASTER> LoadPosPenjualanData()
        {
            using OracleConnection connection = new(global.connectionString);
            connection.Open();

            string query = @"SELECT
                            NO_TRANSAKSI,
                            TANGGAL,
                            JAM,
                            KASIR,
                            ID_PELANGGAN,
                            NIK,
                            NAMA_PELANGGAN,
                            STATUS,
                            UNIT_KERJA,
                            BRUTO,
                            POTONGAN,
                            TOTAL,
                            JENIS_BAYAR,
                            KET_BAYAR,
                            PENDING,
                            TENOR,
                            ANGSURAN,
                            ISLUNAS FROM POS_PENJUALAN";
                List<PENJUALANMASTER> PenjualanList = connection.Query<PENJUALANMASTER>(query).ToList();

                return PenjualanList;
         }

        public  List<PosPenjualanDetail> LoadPosPenjualanDetails()
        {
            using OracleConnection connection = new(global.connectionString);
            connection.Open();
            string query = @"SELECT
                            ID_PENJUALAN,
                            NO_TRANSAKSI,
                            BARIS,
                            PRODUCT_ID,
                            KODE_BARANG,
                            BARCODE,
                            NAMA_BARANG,
                            SATUAN,
                            JUMLAH_BARANG,
                            HARGA_BARANG,
                            BRUTO,
                            POTONGAN,
                            TOTAL_HARGA,
                            HPP FROM POS_PENJUALAN_DETAIL";
            List<PosPenjualanDetail> posPenjualanDetailList = connection.Query<PosPenjualanDetail>(query).ToList();

            return posPenjualanDetailList;
        }
    }

    public class RestoreData
    {
        public void DeleteAndInsertMasterDetailData_Penjualan(List<PENJUALANMASTER> masterList, List<PosPenjualanDetail> detailList)
        {
            using OracleConnection dbConnection = new(global.connectionString);
            dbConnection.Open();
            {
                using var transaction = dbConnection.BeginTransaction();
                //try
                //{
                    // Delete existing records in the master table
                    string deleteMasterQuery = "DELETE FROM POS_PENJUALAN";
                    dbConnection.Execute(deleteMasterQuery, transaction);

                    // Insert new master data
                    foreach (var masterData in masterList)
                    {
                        string masterQuery = @"
                            INSERT INTO POS_PENJUALAN
                            ( NO_TRANSAKSI, TANGGAL, JAM, KASIR, ID_PELANGGAN, NIK, NAMA_PELANGGAN, STATUS, 
                            UNIT_KERJA, BRUTO, POTONGAN, TOTAL, JENIS_BAYAR, KET_BAYAR, PENDING, TENOR, ANGSURAN, ISLUNAS)
                            VALUES
                            (:NO_TRANSAKSI, :TANGGAL, :JAM, :KASIR, :ID_PELANGGAN, :NIK, :NAMA_PELANGGAN, :STATUS, 
                            :UNIT_KERJA, :BRUTO, :POTONGAN, :TOTAL, :JENIS_BAYAR, :KET_BAYAR, :PENDING, :TENOR, :ANGSURAN, :ISLUNAS)";

                        dbConnection.Execute(masterQuery, masterData, transaction);
                    }
                    // Insert detail data
                    foreach (var detailData in detailList)
                    {
                        string detailQuery = @"
                            INSERT INTO POS_PENJUALAN_DETAIL
                            (ID_PENJUALAN, NO_TRANSAKSI, BARIS, PRODUCT_ID, KODE_BARANG, BARCODE, NAMA_BARANG, 
                            SATUAN, JUMLAH_BARANG, HARGA_BARANG, BRUTO, POTONGAN, TOTAL_HARGA, HPP)
                            VALUES
                            (:ID_PENJUALAN, :NO_TRANSAKSI, :BARIS, :PRODUCT_ID, :KODE_BARANG, :BARCODE, :NAMA_BARANG, 
                            :SATUAN, :JUMLAH_BARANG, :HARGA_BARANG, :BRUTO, :POTONGAN, :TOTAL_HARGA, :HPP)";

                        dbConnection.Execute(detailQuery, detailData, transaction);
                    }

                    // Commit the transaction
                    transaction.Commit();
                //}
                //catch (Exception ex)
                //{
                //    // Handle exception
                //    Console.WriteLine($"Error: {ex.Message}");
                //    transaction.Rollback();
                //}
            }
        }

        public void RestoreAnggota(List<FinAnggota> anggotaList)
        {
            using OracleConnection dbConnection = new(global.connectionString);
            dbConnection.Open();
            {
                using var transaction = dbConnection.BeginTransaction();
                //try
                //{
                // Delete existing records in the master table
                //string deleteMasterQuery = "DELETE FROM POS_PENJUALAN";
                //dbConnection.Execute(deleteMasterQuery, transaction);

                // Insert new master data
                foreach (var masterData in anggotaList)
                {
                    string insertQuery = @"
                    INSERT INTO FIN_ANGGOTA 
                        (NIK, NAMA_PELANGGAN, ALAMAT, KOTA, KODE_POS, NO_TELP, LOKASI, KELOMPOK, LIMIT_HUTANG, 
                         STATUS, AKTIF, UNIT_KERJA, TMK, ANGGOTA, TGLNONAKTIF, GAMBAR, UPDATEAT) 
                    VALUES 
                        (:NIK, :NAMA_PELANGGAN, :ALAMAT, :KOTA, :KODE_POS, :NO_TELP, :LOKASI, :KELOMPOK, :LIMIT_HUTANG, 
                         :STATUS, :AKTIF, :UNIT_KERJA, :TMK, :ANGGOTA, :TGLNONAKTIF, :GAMBAR, :UPDATEAT)";

                    dbConnection.Execute(insertQuery, masterData, transaction);
                }
                

                // Commit the transaction
                transaction.Commit();
                //}
                //catch (Exception ex)
                //{
                //    // Handle exception
                //    Console.WriteLine($"Error: {ex.Message}");
                //    transaction.Rollback();
                //}
            }
        }
        public void RestoreBarang(List<PosProduct> barangList)
        {
            using OracleConnection dbConnection = new(global.connectionString);
            dbConnection.Open();
            {
                using var transaction = dbConnection.BeginTransaction();
                //try
                //{
                // Delete existing records in the master table
                //string deleteMasterQuery = "DELETE FROM POS_PENJUALAN";
                //dbConnection.Execute(deleteMasterQuery, transaction);

                // Insert new master data
                foreach (var masterData in barangList)
                {
                    string insertQuery = @"INSERT INTO POS_PRODUCT(KODE_ITEM, PRODUCTNAME, SATUAN, BARCODE, KATEGORI_ID, PRICE, BELI, AKTIF, HPP, LASTUPDATE) " +
                             "VALUES (:KODE_ITEM, :PRODUCTNAME, :SATUAN, :BARCODE, :KATEGORI_ID, :PRICE, :BELI, :AKTIF, :HPP, :LASTUPDATE)";

                    dbConnection.Execute(insertQuery, masterData, transaction);
                }


                // Commit the transaction
                transaction.Commit();
                //}
                //catch (Exception ex)
                //{
                //    // Handle exception
                //    Console.WriteLine($"Error: {ex.Message}");
                //    transaction.Rollback();
                //}
            }
        }
    }
}
