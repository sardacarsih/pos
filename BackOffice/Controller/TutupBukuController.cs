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
    public class TutupBukuController
    {
        static readonly ITutupBuku repository;
        static TutupBukuController()
        {
            repository = new TutupBukuRepository();
        }

        public List<string> GetDuplicateNiks(int p_periode, int p_remise, DateTime p_daritanggal, DateTime p_daritanggalr2, DateTime p_sampaitanggal)
        {
            return repository.GetDuplicateNiks(  p_periode,   p_remise,   p_daritanggal,   p_daritanggalr2,   p_sampaitanggal);
        }
    }
}
