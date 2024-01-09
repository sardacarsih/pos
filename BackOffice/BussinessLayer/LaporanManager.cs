using BackOffice.DataLayer;
using BackOffice.Interface;
using BackOffice.Model;
using DevExpress.XtraRichEdit.Import.Html;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackOffice.BussinessLayer
{
    public class LaporanManager
    {
        static readonly ILaporanRepository laporanDataLayer;
        static LaporanManager()
        {
            laporanDataLayer = new LaporanRepository();
        }

        public static List<DTOPOS_KREDIT_PENJUALAN_MASTER> GetPiutangKreditBarangBelumLunas()
        {
            return laporanDataLayer.PiutangKreditBarangBelumLunas();
        }

        public static List<DTOPinjamanMaster> GetPiutangPinjaman()
        {
            return laporanDataLayer.PiutangPinjaman();
        }
        public static List<DTOPOS_KREDIT_PENJUALAN_MASTER> PenjualanTunai(DateTime dari, DateTime sampai)
        {
            return laporanDataLayer.PenjualanTunai( dari, sampai);
        }
        public static List<DTOPOS_KREDIT_PENJUALAN_MASTER> PenjualanTempo(DateTime dari, DateTime sampai)
        {
            return laporanDataLayer.PenjualanTempo(dari, sampai);
        }
        public static List<DTOPOS_KREDIT_PENJUALAN_MASTER> PenjualanKredit(DateTime dari, DateTime sampai)
        {
            return laporanDataLayer.PenjualanKredit(dari, sampai);
        }

        public static List<FinTagihanWaserdaDtl> PenjualanWaserdaDetail(string p_nik, DateTime dari, DateTime sampai)
        {

            return laporanDataLayer.PenjualanWaserdaDetail(p_nik,dari, sampai);
        }
        public static List<DTOTagihanAngsuran> TagihanKreditBarang(string p_nik, int p_periode)
        {
            return laporanDataLayer.TagihanKreditBarang(  p_nik,   p_periode);
        }
        public static List<DTOTagihanAngsuran> TagihanPinjaman(string p_nik, int p_periode)
        {
            return laporanDataLayer.TagihanPinjaman(p_nik, p_periode);
        }
    }
}
