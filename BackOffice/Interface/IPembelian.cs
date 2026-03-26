using BackOffice.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackOffice.Interface
{
    public interface IPembelian
    {
        List<DTOSupplier> GetSuppliers();
        List<DTOFakturPembelian_Header> DaftarPembelian(int bulan, int tahun);
        void HapusPembelian(string no_faktur);
        void EditPembelian(string no_faktur);
        void CetakPembelian(string no_faktur);
        void InsertFaktur_Pembelian(DTOFakturPembelian_Header faktur_header, List<DTOFakturPembelianDetail> ListItemPembelian);
        void UpdateFaktur_Pembelian(DTOFakturPembelian_Header faktur_header, List<DTOFakturPembelianDetail> ListItemPembelian);
        void UpdateTransactionNumber(string transactionNumber);
        List<DTODaftarBarang> GetDaftarPembelianBarang(string idtransaksi);

    }
}
