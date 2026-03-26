using BackOffice.Model;
using System.Collections.Generic;

namespace BackOffice.Interface
{
    public interface ISupplier
    {
        List<DTOSupplier> GetAllSuppliers();
        List<DTOSupplier> GetActiveSuppliers();
        int InsertSupplier(DTOSupplier supplier);
        int UpdateSupplier(DTOSupplier supplier);
        int DeactivateSupplier(string kode);
        bool IsSupplierInUse(string kode);
        bool IsKodeExists(string kode);
    }
}
