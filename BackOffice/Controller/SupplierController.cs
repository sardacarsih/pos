using BackOffice.DataLayer;
using BackOffice.Interface;
using BackOffice.Model;
using System.Collections.Generic;

namespace BackOffice.Controller
{
    public class SupplierController
    {
        static readonly ISupplier repository;

        static SupplierController()
        {
            repository = new SupplierRepository();
        }

        public List<DTOSupplier> GetAllSuppliers()
        {
            return repository.GetAllSuppliers();
        }

        public List<DTOSupplier> GetActiveSuppliers()
        {
            return repository.GetActiveSuppliers();
        }

        public int InsertSupplier(DTOSupplier supplier)
        {
            return repository.InsertSupplier(supplier);
        }

        public int UpdateSupplier(DTOSupplier supplier)
        {
            return repository.UpdateSupplier(supplier);
        }

        public int DeactivateSupplier(string kode)
        {
            return repository.DeactivateSupplier(kode);
        }

        public bool IsSupplierInUse(string kode)
        {
            return repository.IsSupplierInUse(kode);
        }

        public bool IsKodeExists(string kode)
        {
            return repository.IsKodeExists(kode);
        }
    }
}
