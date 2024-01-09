using BackOffice.Interface;
using BackOffice.Model;
using Dapper;
using Oracle.ManagedDataAccess.Client;

namespace BackOffice.DataLayer
{
    public class MasterData : IMasterData
    {
        public void DeleteProduct(int productID)
        {
            using OracleConnection connection = new(global.connectionString);
            string deleteQuery = "DELETE FROM POS_PRODUCT WHERE productID = :productID";
            connection.Execute(deleteQuery, new { productID });
        }

        public List<DTOAnggota> GetAllAnggota()
        {
            using OracleConnection connection = new(global.connectionString);
            string query = "SELECT ID_PELANGGAN, NIK, NAMA_PELANGGAN, ALAMAT, KOTA, KODE_POS, NO_TELP, LOKASI, KELOMPOK, LIMIT_HUTANG, STATUS, AKTIF, UNIT_KERJA, TMK, ANGGOTA, BARCODE, TGLNONAKTIF FROM FIN_ANGGOTA";
            return connection.Query<DTOAnggota>(query).AsList();
        }

        public List<DTOPRODUCTS> GetBarang(string isaktif)
        {
            using (OracleConnection connection = new OracleConnection(global.connectionString))
            {
                string query = "SELECT productid, kategori_id, Kode_item, barcode, productname, satuan, beli, price, price-beli AS margin, aktif FROM pos_product WHERE aktif = :isaktif ORDER BY productname";
                return connection.Query<DTOPRODUCTS>(query, new { isaktif }).AsList();
            }
        }


        public List<DTODiskon> GetDiskon()
        {
            using OracleConnection connection = new(global.connectionString);
            string query = "SELECT KODE_ITEM,MINQTY,POTONGAN FROM POS_POTONGANBERDASARKANQTY";
            return connection.Query<DTODiskon>(query).AsList();
        }

        public List<DTODiskon> GetDiskonbykode(string Kode_item)
        {
            using OracleConnection connection = new(global.connectionString);
            string query = "SELECT ID_PELANGGAN, NIK, NAMA_PELANGGAN, ALAMAT, KOTA, KODE_POS, NO_TELP, LOKASI, KELOMPOK, LIMIT_HUTANG, STATUS, AKTIF, UNIT_KERJA, TMK, ANGGOTA, BARCODE, TGLNONAKTIF FROM FIN_ANGGOTA";
            return connection.Query<DTODiskon>(query).AsList();
        }

        public List<DTOKategori> GetKategori()
        {
            using OracleConnection connection = new(global.connectionString);
            string query = "SELECT kode,KATEGORI FROM POS_KATEGORI ORDER BY KATEGORI";
            return connection.Query<DTOKategori>(query).AsList();
        }

        public List<DTOSatuan> GetSatuan()
        {
            using OracleConnection connection = new(global.connectionString);
            string query = "SELECT SATUAN FROM POS_SATUAN ORDER BY SATUAN";
            return connection.Query<DTOSatuan>(query).AsList();
        }

        public List<DTOUnitKerja> GetUnitKerja()
        {
            using OracleConnection connection = new(global.connectionString);
            string sqlQuery = "SELECT KODE, NAMA, SW_POT_SHU FROM FIN_UNITKERJA";
            return connection.Query<DTOUnitKerja>(sqlQuery).AsList();
        }

        public List<DTOUnitKerja> GetUnitKerjaFromTransaksi(int p_periode, int p_remise)
        {
            using OracleConnection connection = new(global.connectionString);
            connection.Open();
            using OracleCommand command = connection.CreateCommand();
            string query = @"
                WITH TRANSAKSI AS (
                    SELECT DISTINCT nik
                    FROM fin_terima_pembayaran
                    WHERE PERIODE = :P_PERIODE AND REMISE = :P_REMISE
                )
                SELECT DISTINCT A.UNIT_KERJA KODE, U.NAMA UNIT_KERJA
                FROM TRANSAKSI T
                JOIN FIN_ANGGOTA A ON A.NIK = T.NIK
                JOIN FIN_UNITKERJA U ON U.KODE = A.UNIT_KERJA
                ORDER BY A.UNIT_KERJA";

            command.CommandText = query;

            // Set the parameter values
            command.Parameters.Add(new OracleParameter(":P_PERIODE", p_periode));
            command.Parameters.Add(new OracleParameter(":P_REMISE", p_remise));

            using OracleDataReader reader = command.ExecuteReader();
            List<DTOUnitKerja> dtoUnitKerjaList = new();

            while (reader.Read())
            {
                string kodeKerja = reader["KODE"].ToString();
                string namaUnitKerja = reader["UNIT_KERJA"].ToString();

                DTOUnitKerja dtoUnitKerja = new()
                {
                    KODE = kodeKerja,
                    NAMA = namaUnitKerja
                };

                dtoUnitKerjaList.Add(dtoUnitKerja);
            }

            return dtoUnitKerjaList;
        }

    }
}
