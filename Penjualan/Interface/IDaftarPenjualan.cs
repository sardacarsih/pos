using Penjualan.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Penjualan.Interface
{
    public interface IDaftarPenjualan
    {
        List<DTODaftarPenjualan> GetPenjualan(DateTime date1,DateTime date2);       
        List<DTODaftarBarang> GetDaftarBarang(string idtransaksi);
    }
}
