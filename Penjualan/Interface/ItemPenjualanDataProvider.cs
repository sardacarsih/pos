using DevExpress.XtraGrid.Views.Grid;
using Penjualan.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Penjualan.Interface
{
    public class ItemPenjualanDataProvider : IItemPenjualanDataProvider
    {
        public List<DTOFakturPenjualanDetail> GetItemPenjualanData(GridView gridView)
        {
            List<DTOFakturPenjualanDetail> ListItemsPenjualan = new();

            for (int i = 0; i < gridView.RowCount; i++)
            {
                DTOFakturPenjualanDetail data = new()
                {
                    BARIS = (int)gridView.GetRowCellValue(i, "No"),
                    PRODUCT_ID = (int)gridView.GetRowCellValue(i, "ProductId"),
                    KODE_BARANG = gridView.GetRowCellValue(i, "Kode_Item").ToString(),
                    BARCODE = gridView.GetRowCellValue(i, "Barcode").ToString(),
                    NAMA_BARANG = gridView.GetRowCellValue(i, "ProductName").ToString(),
                    SATUAN = gridView.GetRowCellValue(i, "Satuan").ToString(),
                    JUMLAH_BARANG = (decimal)gridView.GetRowCellValue(i, "Qty"),
                    HARGA_BARANG = (decimal)gridView.GetRowCellValue(i, "Price"),
                    BRUTO = (decimal)gridView.GetRowCellValue(i, "Bruto"),
                    POTONGAN = (decimal)gridView.GetRowCellValue(i, "Potongan"),
                    TOTAL_HARGA = (decimal)gridView.GetRowCellValue(i, "Total"),
                };
                ListItemsPenjualan.Add(data);
            }

            return ListItemsPenjualan;
        }
    }

}
