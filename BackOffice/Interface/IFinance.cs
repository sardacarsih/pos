using BackOffice.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackOffice.Interface
{
    public interface IFinance
    {
        void InsertFaktur_PinjamanTunai(DTOPinjaman PinjamanTunaiMaster, List<DTOSimulasiAngsuran> PinjamanTunaiDetail);
        void EditFakturPinjaman(DTOPinjaman PinjamanTunaiMaster);

        DataSet Daftar_Pinjaman(int p_bulan, int p_tahun);
        List<DTOPinjamanMaster> Daftar_Pinjaman_List(int p_bulan, int p_tahun);
        DataSet Tagihan_Pinjaman_dan_Kredit(int p_bulan, int p_tahun);
        List<DTOPinjamanMaster> DaftarPinjaman(int bulan, int tahun);
        List<DTOPinjamanDetail> GetDaftarAngsuran(string idtransaksi);
        List<FinBayarVia> PembayaranVia(string kode);
        List<FinJenisBayar> JenisBayar();
    }
}
