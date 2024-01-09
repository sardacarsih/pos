using BackOffice.DataLayer;
using BackOffice.Interface;
using BackOffice.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackOffice.BussinessLayer
{
    public class Finance_Services
    {

        static readonly IFinance repository;
        static Finance_Services()
        {
            repository = new Finance();
        }

        public static void InsertFaktur_PinjamanTunai(DTOPinjaman PinjamanTunaiMaster, List<DTOSimulasiAngsuran> PinjamanTunaiDetail)
        {
            repository.InsertFaktur_PinjamanTunai(PinjamanTunaiMaster, PinjamanTunaiDetail);
        }

        public static void EditFakturPinjaman(DTOPinjaman PinjamanTunaiMaster)
        {
            repository.EditFakturPinjaman(PinjamanTunaiMaster);
        }
        public static DataSet Tagihan_Pinjaman_dan_Kredit(int p_bulan, int p_tahun)
        {
            return repository.Tagihan_Pinjaman_dan_Kredit(p_bulan, p_tahun);
        }
        public static List<DTOPinjamanMaster> DaftarPinjaman(int p_bulan, int p_tahun)
        {
            return repository.DaftarPinjaman(p_bulan, p_tahun);
        }
        public static List<DTOPinjamanDetail> GetDaftarAngsuran(string idtransaksi)
        {
            return repository.GetDaftarAngsuran(idtransaksi);
        }
        public static List<FinBayarVia> PembayaranVia(string kode) { return repository.PembayaranVia( kode);}
        public static List<FinJenisBayar> JenisBayar() { return repository.JenisBayar();}

    }
}
