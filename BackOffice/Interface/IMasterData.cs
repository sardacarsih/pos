using BackOffice.Model;
using System;
using System.Collections.Generic;
using System.IO.Packaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackOffice.Interface
{
    public interface IMasterData
    {
        List<DTOAnggota> GetAllAnggota();
        List<DTOUnitKerja> GetUnitKerja();
        List<DTOKategori> GetKategori();
        List<DTOSatuan> GetSatuan();
        List<DTOPRODUCTS> GetBarang(string isaktif);
        List<DTODiskon> GetDiskon();
        List<DTODiskon> GetDiskonbykode(string Kode_item);
        List<DTOUnitKerja> GetUnitKerjaFromTransaksi(int p_periode, int p_remise);
        void DeleteProduct(int productid);
    }
}
