using BackOffice.DataLayer;
using BackOffice.Interface;
using BackOffice.Model;
using System.Data;

namespace BackOffice.Controller
{
    public class PersediaanController
    {

        static readonly IPersediaan repository;
        static PersediaanController()
        {
            repository = new Persediaan();
        }

#pragma warning disable CRRSP08 // A misspelled word has been found
        public List<DTOKartuStok> KartuStokBarang(string p_kode, DateTime p_startdate, DateTime p_enddate)
#pragma warning restore CRRSP08 // A misspelled word has been found
        {
            return repository.KartuStokBarang(  p_kode,   p_startdate,   p_enddate);
        }
        public DataTable FillDictionaryFromDatabase()
        {
            return repository.FillDictionaryBarang_FromDatabase();
        }
    }
}
