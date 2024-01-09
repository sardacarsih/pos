using BackOffice.DataLayer;
using BackOffice.Interface;
using BackOffice.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackOffice.Controller
{
    public class AnggotaController
    {

        static readonly IAnggota repository;
        static AnggotaController()
        {
            repository = new AnggotaRepository();
        }

        public List<DTOAnggota> GetAnggotaData()
#pragma warning restore CRRSP08 // A misspelled word has been found
        {
            return repository.GetAnggotaData();
        }
        public List<DTOAnggotaAktif> GetAnggotaAktif()
#pragma warning restore CRRSP08 // A misspelled word has been found
        {
            return repository.GetAnggotaAktif();
        }
        public IEnumerable<DTOAnggota> GetMembersByStatusAndType(string isAnggota)
#pragma warning restore CRRSP08 // A misspelled word has been found
        {
            return repository.GetMembersByStatusAndType(isAnggota);
        }
        public void InsertAnggota(DTOAnggota anggota)
        {
            repository.InsertAnggota(anggota);
        }
        public void UpdateAnggota(DTOAnggota anggota)
        {
            repository.UpdateAnggota(anggota);
        }
        public string GenerateNikAnggota( DateTime date)
        {
            return repository.GenerateNikAnggota(date);
        }
        public void SimpanNomorAnggota(string lastnomor)
        {
            repository.SimpanNomorAnggota(lastnomor);
        }
    }
}
