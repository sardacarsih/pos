using BackOffice.DataLayer;
using BackOffice.Interface;
using BackOffice.Model;

namespace BackOffice
{
    public class KASController
    {

        static readonly IKas repository;
        static KASController()
        {
            repository = new KASRepository();
        }
        
        
       
        public List<DTOTransaksiKAS> KAS_Transaksi(string idKas, DateTime startDate, DateTime endDate)
        {
            return repository.KAS_Transaksi(  idKas,   startDate,   endDate);        }
        
        public List<DTOTransaksiKAS> Edit_KAS_Transaksi(string p_nomorkas)
        {
            return repository.Edit_KAS_Transaksi(p_nomorkas);
        }
        public void Delete_KAS(string p_nomorkas)
        {
            repository.Delete_KAS(p_nomorkas);
        }
    }
}
