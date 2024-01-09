using Penjualan.DataLayer;
using Penjualan.Interface;
using Penjualan.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Penjualan.BusinessLayer
{
    public class PenjualanController
    {

        static readonly IDaftarPenjualan repository;
        static PenjualanController()
        {
            repository = new DaftarPenjualan();
        }

        public List<DTODaftarPenjualan> GetPenjualan(DateTime date1, DateTime date2)
        {
            return repository.GetPenjualan(date1, date2);
        }
        
        public List<DTODaftarBarang> GetDaftarBarang(string idtransaksi)
        {
            return repository.GetDaftarBarang(idtransaksi);
        }

    }
}
