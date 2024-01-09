using BackOffice.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackOffice.Interface
{
    public interface IKas
    {
        List<DTOTransaksiKAS> KAS_Transaksi(string idKas, DateTime startDate, DateTime endDate);
        List<DTOTransaksiKAS> Edit_KAS_Transaksi(string p_nomorkas);
        void Delete_KAS(string p_nomorkas);


    }
}
