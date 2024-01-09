using BackOffice.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackOffice.Interface
{
    public interface ILaporanRepository
    {
        List<FinTagihanWaserdaDtl> PenjualanWaserdaDetail(string p_nik,DateTime dari, DateTime sampai);
        List<DTOPOS_KREDIT_PENJUALAN_MASTER> PenjualanTunai(DateTime dari, DateTime sampai);
        List<DTOPOS_KREDIT_PENJUALAN_MASTER> PenjualanTempo(DateTime dari, DateTime sampai);
        List<DTOPOS_KREDIT_PENJUALAN_MASTER> PenjualanKredit(DateTime dari, DateTime sampai);
        List<DTOPOS_KREDIT_PENJUALAN_MASTER> PiutangKreditBarangBelumLunas();        
        List<DTOPinjamanMaster> PiutangPinjaman();
        List<DTOTagihanAngsuran> TagihanPinjaman(string p_nik, int p_periode);
        List<DTOTagihanAngsuran> TagihanKreditBarang(string p_nik, int p_periode);
    }
}
