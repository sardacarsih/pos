using Penjualan.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Penjualan.Interface
{
    public interface IFakturPending
    {
        void InsertFaktur_Pending(DTOFakturPending faktur_header, List<DTODaftarBarangPending> ListItemsPenjualan);
        List<DTOFakturPending> GetPenjualanPending();
        List<DTODaftarBarangPending> GetDaftarBarangPending(string idtransaksi);
        void DeletePendingFaktur(string idtransaksi);
    }
}
