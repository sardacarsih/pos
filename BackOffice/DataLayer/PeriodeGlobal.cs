using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackOffice.DataLayer
{
    public class PeriodeGlobal
    {
        public interface IPeriode
        {
            string[] GetBulan();
            string[] GetRemise();
        }

        public class Periode : IPeriode
        {
            private string[] bulan = { "Januari", "Februari", "Maret", "April", "Mei", "Juni", "Juli", "Agustus", "September", "Oktober", "November", "Desember" };
            private string[] remise = { "Remise I", "Remise II", "Bulanan" };

            public string[] GetBulan()
            {
                return bulan;
            }

            public string[] GetRemise()
            {
                return remise;
            }
        }

    }
}
