using DevExpress.XtraGrid.Views.Grid;
using Penjualan.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Penjualan.Interface
{
    public interface IItemPenjualanDataProvider
    {
        List<DTOFakturPenjualanDetail> GetItemPenjualanData(GridView gridView);
    }

}
