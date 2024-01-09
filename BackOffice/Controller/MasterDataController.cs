using BackOffice.DataLayer;
using BackOffice.Interface;
using BackOffice.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackOffice.Controller
{
    public class MasterDataController
    {

        static readonly IMasterData repository;
        static MasterDataController()
        {
            repository = new MasterData();
        }

        public  List<DTOUnitKerja> GetUnitKerja()
        {
            return repository.GetUnitKerja();
        }
        public  List<DTOAnggota> GetAllAnggota()
        {
            return repository.GetAllAnggota();
        }
        public List<DTOSatuan> GetSatuan()
        {
            return repository.GetSatuan();
        }
        public List<DTOKategori> GetKategori()
        {
            return repository.GetKategori();
        }
        public  List<DTOUnitKerja> GetUnitKerjaFromTransaksi(int p_periode, int p_remise)
        {
            return repository.GetUnitKerjaFromTransaksi(  p_periode,   p_remise);
        }
        public List<DTOPRODUCTS> GetBarang(string isaktif)
        {
            return repository.GetBarang(isaktif);
        }
        public List<DTODiskon> GetDiskon()
        {
            return repository.GetDiskon();
        }
        public void DeleteProduct(int productID)
        {
            repository.DeleteProduct(productID);
        }
    }
}
