using BackOffice.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackOffice.Interface
{
    public interface IAnggota
    {
        List<DTOAnggota> GetAnggotaData();
        List<DTOAnggotaAktif> GetAnggotaAktif();
        IEnumerable<DTOAnggota> GetMembersByStatusAndType(string isAnggota);
        void InsertAnggota(DTOAnggota anggota);
        void UpdateAnggota(DTOAnggota anggota);
        string GenerateNikAnggota(DateTime date);
        void SimpanNomorAnggota(string lastnomor);

    }
}
