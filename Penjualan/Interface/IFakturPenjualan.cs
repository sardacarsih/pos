using DevExpress.Utils.About;
using Penjualan.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Penjualan.Interface
{
    public interface IFakturPenjualan
    {

        void InsertFaktur_Penjualan(DTOFakturPenjualanHeader faktur_header, List<DTOFakturPenjualanDetail> ListItemsPenjualan);
        void InsertFaktur_Penjualan_Angsuran(DTOFakturPenjualanHeader faktur_header, List<DTOFakturPenjualanDetail> ListItemsPenjualan, List<DTOAngsuranKreditBarang> DaftarWaktuTagihan);
        void UpdateTransactionNumber(string transactionNumber);
        void UpdateFakturPenjualan(DTOFakturPenjualanHeader faktur_header);
        DTOProductInfo RetrieveProductInfo(string barcode);
        decimal GetStocItem(string kodeBarang, DateTime startDate, DateTime endDate);
        
     }
}
