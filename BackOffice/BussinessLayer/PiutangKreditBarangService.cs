using BackOffice.DataLayer;
using System.Collections.Generic;
using BackOffice.Model;

namespace BackOffice.BusinessLayer
{
    public class PiutangKreditBarangService
    {
        private readonly PiutangKreditBarangRepository _repository;

        public PiutangKreditBarangService(string connectionString)
        {
            _repository = new PiutangKreditBarangRepository(connectionString);
        }

        public List<DTOPOS_KREDIT_PENJUALAN_MASTER> GetDaftarKreditBarangBelumLunas()
        {
            // You can add any additional business logic here before or after retrieving data from the repository
            return _repository.GetDaftarKreditBarangBelumLunas();
        }
    }
}
