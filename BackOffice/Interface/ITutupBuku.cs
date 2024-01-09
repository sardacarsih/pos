using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackOffice.Interface
{
    public interface ITutupBuku
    {
        List<string> GetDuplicateNiks(int p_periode, int p_remise, DateTime p_daritanggal, DateTime p_daritanggalr2, DateTime p_sampaitanggal);
    }
}
