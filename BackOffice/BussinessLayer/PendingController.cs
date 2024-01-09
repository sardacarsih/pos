using BackOffice.DataLayer;
using BackOffice.Interface;
using BackOffice.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackOffice
{
    public class PendingController
    {

        static readonly IFakturPending repository;
        static PendingController()
        {
            repository = new FakturPending();
        }
        public  void InsertFaktur_Pending(DTOFakturPending faktur_header, List<DTODaftarBarangPending> ListItemsPenjualan)
        {
            repository.InsertFaktur_Pending(faktur_header, ListItemsPenjualan);
        }
        public void DeletePendingFaktur(string idtransaksi)
        {
            repository.DeletePendingFaktur(idtransaksi);
        }
        public List<DTOFakturPending> GetPenjualanPending()
        {
            return repository.GetPenjualanPending();
        }
        public List<DTODaftarBarangPending> GetDaftarBarangPending(string idtransaksi)
        {
            return repository.GetDaftarBarangPending(idtransaksi);
        }
    }
}
