using BackOffice.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackOffice.Interface
{
    public interface IPersediaan
    {
        List<DTOKartuStok> KartuStokBarang(string kode, DateTime startdate, DateTime enddate);
        DataTable FillDictionaryBarang_FromDatabase();
    }
}
