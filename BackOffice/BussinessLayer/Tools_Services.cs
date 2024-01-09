using BackOffice.DataLayer;
using BackOffice.Interface;
using BackOffice.Model;
using System.Data;

namespace BackOffice.BussinessLayer
{
    public class Tools_Services
    { 
        static readonly iTools repository;
        static Tools_Services()
        {
            repository = new Tools();
        }

        public static bool GetRemiseStatus(int p_periode, int p_remise)
        {
            return repository.GetRemiseStatus( p_periode,  p_remise);
        }
        public static string TerbilangIndonesia(double nilai)
        {
            return repository.TerbilangIndonesia(nilai);
        }

        public static DataTable PelangganAktif()
        {
            return repository.PelangganAktif();
        }
        public static DataTable AnggotaAktif()
        {
            return repository.AnggotaAktif();
        }
    }
}
