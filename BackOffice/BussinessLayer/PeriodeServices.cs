
using BackOffice.DataLayer;
using static BackOffice.DataLayer.PeriodeGlobal;

namespace BackOffice.BussinessLayer
{
    public class PeriodeServices
    {
        static readonly IPeriode repository;

        static PeriodeServices()
        {
            repository = new Periode();
        }

        public static string[] GetBulan()
        {
            return repository.GetBulan();
        }

        public static string[] GetRemise()
        {
            return repository.GetRemise();
        }
    }
}
