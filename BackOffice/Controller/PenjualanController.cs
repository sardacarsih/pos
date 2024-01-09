using BackOffice.DataLayer;
using BackOffice.Interface;
using BackOffice.Model;

namespace BackOffice
{
    public class PenjualanController
    {

        static readonly IFakturPenjualan repository;
        static PenjualanController()
        {
            repository = new FakturPenjualan();
        }

        public List<DTODaftarPenjualan> GetPenjualan(DateTime date1, DateTime date2)
        {
            return repository.GetPenjualan(date1, date2);
        }
        public List<DTOLabaRugiAnalisa> AnalisaLabaRugi(DateTime date1, DateTime date2)
        {
            return repository.AnalisaLabaRugi(date1, date2);
        }
        public List<DTOLabaRugi> GetLabaRugi(int year)
        {
            return repository.GetLabaRugi(year);
        }
        public List<DTODaftarBarang> GetDaftarBarang(string idtransaksi)
        {
            return repository.GetDaftarBarang(idtransaksi);
        }
        public void HapusFakturPenjualan(string idtransaksi)
        {
            repository.HapusFakturPenjualan(idtransaksi);
        }
        public void HapusFakturPenjualanAngsuran(string idtransaksi)
        {
            repository.HapusFakturPenjualanAngsuran(idtransaksi);
        }
        public void UpdateFakturPenjualan(DTOFakturPenjualanHeader faktur_header, List<DTODaftarBarang> ListItemsPenjualan)
        {
            repository.UpdateFakturPenjualan(faktur_header, ListItemsPenjualan);
        }

    }
}
