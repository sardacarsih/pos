using BackOffice.DataLayer;
using BackOffice.Interface;
using BackOffice.Model;
using DevExpress.Office.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackOffice.Controller
{
    internal class PembelianController
    {

        static readonly IPembelian repository;
        static PembelianController()
        {
            repository = new PembelianRepository();
        }

        public List<DTOSupplier> GetSuppliers()
        {
            return repository.GetSuppliers();
        }
        public List<DTODaftarBarang> GetDaftarPembelianBarang(string no_transaksi)
        {
            return repository.GetDaftarPembelianBarang(no_transaksi);
        }
        public List<DTOFakturPembelian_Header> DaftarPembelian(int bulan, int tahun)
        {
            return repository.DaftarPembelian( bulan,  tahun);
        }
        public void InsertFaktur_Pembelian(DTOFakturPembelian_Header faktur_header, List<DTOFakturPembelianDetail> ListItemPembelian)
        {
            repository.InsertFaktur_Pembelian(faktur_header, ListItemPembelian);
        }
        public void UpdateFaktur_Pembelian(DTOFakturPembelian_Header faktur_header, List<DTOFakturPembelianDetail> ListItemPembelian)
        {
            repository.UpdateFaktur_Pembelian(faktur_header, ListItemPembelian);
        }
        public void UpdateTransactionNumber(string transactionNumber)
        {
             repository.UpdateTransactionNumber(transactionNumber);
        }
        public void HapusPembelian(string no_faktur_pembeian)
        {
            repository.HapusPembelian(no_faktur_pembeian);
        }
    }
}
