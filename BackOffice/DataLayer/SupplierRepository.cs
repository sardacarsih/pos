using BackOffice.Interface;
using BackOffice.Model;
using Dapper;
using Oracle.ManagedDataAccess.Client;

namespace BackOffice.DataLayer
{
    public class SupplierRepository : ISupplier
    {
        public List<DTOSupplier> GetAllSuppliers()
        {
            using OracleConnection connection = new(global.connectionString);
            string query = "SELECT KODE, NAMA, AKTIF FROM POS_SUPPLIER ORDER BY NAMA";
            return connection.Query<DTOSupplier>(query).AsList();
        }

        public List<DTOSupplier> GetActiveSuppliers()
        {
            using OracleConnection connection = new(global.connectionString);
            string query = "SELECT KODE, NAMA, AKTIF FROM POS_SUPPLIER WHERE AKTIF = 'Y' ORDER BY NAMA";
            return connection.Query<DTOSupplier>(query).AsList();
        }

        public int InsertSupplier(DTOSupplier supplier)
        {
            using OracleConnection connection = new(global.connectionString);
            string query = "INSERT INTO POS_SUPPLIER (KODE, NAMA, AKTIF) VALUES (:KODE, :NAMA, :AKTIF)";
            return connection.Execute(query, supplier);
        }

        public int UpdateSupplier(DTOSupplier supplier)
        {
            using OracleConnection connection = new(global.connectionString);
            string query = "UPDATE POS_SUPPLIER SET NAMA = :NAMA WHERE KODE = :KODE";
            return connection.Execute(query, supplier);
        }

        public int DeactivateSupplier(string kode)
        {
            using OracleConnection connection = new(global.connectionString);
            string query = "UPDATE POS_SUPPLIER SET AKTIF = 'N' WHERE KODE = :kode";
            return connection.Execute(query, new { kode });
        }

        public bool IsSupplierInUse(string kode)
        {
            using OracleConnection connection = new(global.connectionString);
            string query = "SELECT COUNT(*) FROM POS_PEMBELIAN WHERE SUPPLIER_ID = :kode";
            int count = connection.ExecuteScalar<int>(query, new { kode });
            return count > 0;
        }

        public bool IsKodeExists(string kode)
        {
            using OracleConnection connection = new(global.connectionString);
            string query = "SELECT COUNT(*) FROM POS_SUPPLIER WHERE KODE = :kode";
            int count = connection.ExecuteScalar<int>(query, new { kode });
            return count > 0;
        }
    }
}
