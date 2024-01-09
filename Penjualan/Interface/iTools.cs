using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Penjualan.Interface
{
    public interface iTools
    {
        bool GetRemiseStatus(int p_periode, int remise);
        string TerbilangIndonesia(double nilai);
        DataTable AnggotaAktif();
        DataTable PelangganAktif();

    }

}
