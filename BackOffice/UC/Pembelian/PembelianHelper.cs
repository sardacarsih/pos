using BackOffice.BussinessLayer;
using BackOffice.Model;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;
using System.ComponentModel;

namespace BackOffice.UC
{
    internal static class PembelianHelper
    {
        public static void HandleCellValueChanged(
            GridView gridView,
            DevExpress.XtraGrid.Views.Base.CellValueChangedEventArgs e,
            BindingList<TransactionDataBeli> transactionDataList,
            Action hitungTotal,
            Control focusTarget)
        {
            try
            {
                if (e.Column.FieldName == "Qty")
                {
                    if (e.RowHandle >= 0)
                    {
                        TransactionDataBeli data = (TransactionDataBeli)gridView.GetRow(e.RowHandle);
                        data.UpdateTotal();
                        gridView.PostEditor();
                        hitungTotal();
                        gridView.RefreshData();
                    }
                }
                else if (e.Column.FieldName == "Total")
                {
                    object cellValue = e.Value;
                    if (cellValue != null)
                    {
                        if (!decimal.TryParse(cellValue.ToString(), out decimal total))
                            gridView.SetColumnError(e.Column, "Input hanya angka.");
                        else if (total <= 0)
                            gridView.SetColumnError(e.Column, "Total harus positif.");
                        else
                            gridView.SetColumnError(e.Column, null);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CellValueChanged error: {ex.Message}");
            }
            finally
            {
                focusTarget.Focus();
            }
        }

        public static void HandleGridKeyDown(
            KeyEventArgs e,
            GridControl gridControl,
            GridView gridView,
            BindingList<TransactionDataBeli> transactionDataList,
            Action hitungTotal,
            Control focusTarget)
        {
            if (e.KeyCode == Keys.F2)
            {
                gridView.CloseEditor();
                gridView.UpdateCurrentRow();
                focusTarget.Focus();
                e.Handled = true;
            }
            if (e.KeyCode == Keys.Delete)
            {
                GridView view = gridControl.FocusedView as GridView;
                int selectedRowHandle = view.FocusedRowHandle;

                if (selectedRowHandle >= 0 && selectedRowHandle < transactionDataList.Count)
                {
                    DialogResult result = MessageBox.Show("Apakah anda yakin akan menghapus baris ini?", "Delete Row", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (result == DialogResult.Yes)
                    {
                        transactionDataList.RemoveAt(selectedRowHandle);
                    }
                }
                hitungTotal();
            }
        }

        public static void ValidateEditor(
            GridView gridView,
            DevExpress.XtraEditors.Controls.BaseContainerValidateEditorEventArgs e)
        {
            if (gridView.FocusedColumn.FieldName == "Qty" && e.Value != null)
            {
                string qtyString = e.Value.ToString();

                if (qtyString.Length > 5)
                {
                    e.Valid = false;
                    e.ErrorText = "Qty ERROR.";
                    return;
                }

                if (!decimal.TryParse(qtyString, out decimal qty))
                {
                    e.Valid = false;
                    e.ErrorText = "Input hanya angka.";
                    return;
                }

                if (qty < 0)
                {
                    e.Valid = false;
                    e.ErrorText = "Qty tidak boleh negatif.";
                }
            }
            else if (gridView.FocusedColumn.FieldName == "Total" && e.Value != null)
            {
                if (!decimal.TryParse(e.Value.ToString(), out decimal jumlah))
                {
                    e.Valid = false;
                    e.ErrorText = "Input hanya angka.";
                    return;
                }

                if (jumlah <= 0)
                {
                    e.Valid = false;
                    e.ErrorText = "Jumlah harus positif.";
                }
            }
        }

        public static void UpdateRowNumbers(GridView gridView)
        {
            for (int i = 0; i < gridView.RowCount; i++)
            {
                gridView.SetRowCellValue(i, gridView.Columns["No"], i + 1);
            }
        }

        public static decimal SubtotalCalc(TextEdit txtqty, TextEdit txthargabeli, TextEdit txtpotongan, TextEdit txttotal)
        {
            if (!decimal.TryParse(txtqty.Text, out decimal qty)) qty = 0;
            if (!decimal.TryParse(txthargabeli.Text, out decimal harga)) harga = 0;
            if (!decimal.TryParse(txtpotongan.Text, out decimal potongan)) potongan = 0;
            var total = (qty * harga) - potongan;
            txttotal.Text = total.ToString();
            return total;
        }

        public static List<DTOFakturPembelianDetail> GetItemPembelianData(BindingList<TransactionDataBeli> transactionDataList)
        {
            List<DTOFakturPembelianDetail> ListItemsPembelian = new();

            for (int i = 0; i < transactionDataList.Count; i++)
            {
                TransactionDataBeli data = transactionDataList[i];
                DTOFakturPembelianDetail detail = new()
                {
                    BARIS = i + 1,
                    PRODUCT_ID = data.ProductId,
                    KODE_BARANG = data.Kode_Item,
                    NAMA_BARANG = data.ProductName,
                    SATUAN = data.Satuan,
                    QUANTITY = data.Qty,
                    HARGA_BELI = data.Hpp,
                    HARGA_JUAL = data.Price,
                    BRUTO = data.Qty * data.Hpp,
                    POTONGAN = data.Potongan,
                    TOTAL = data.Total
                };
                ListItemsPembelian.Add(detail);
            }
            return ListItemsPembelian;
        }

        public static void DisableColumnSorting(GridView gridView)
        {
            string[] columns = { "No", "ProductName", "Satuan", "Qty", "Hpp", "Bruto", "Potongan", "Total" };
            foreach (var col in columns)
            {
                gridView.Columns[col].OptionsColumn.AllowSort = DevExpress.Utils.DefaultBoolean.False;
            }
        }

        public static TransactionDataBeli ValidateAndCreateProduct(
            ref int productid,
            TextEdit barcodeTextBox,
            TextEdit txtItemBarang,
            TextEdit txtnamabarang,
            TextEdit txtsatuan,
            TextEdit txtqty,
            TextEdit txthargabeli,
            TextEdit texthargajual,
            TextEdit txtpotongan)
        {
            if (!decimal.TryParse(txtqty.Text, out decimal qty) || qty <= 0 || string.IsNullOrEmpty(txtItemBarang.Text))
            {
                XtraMessageBox.Show("Qty dan harga harus diisi", "Info", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
            if (!decimal.TryParse(texthargajual.Text, out decimal hargaJual)) hargaJual = 0;
            if (!decimal.TryParse(txthargabeli.Text, out decimal hargaBeli)) hargaBeli = 0;
            if (hargaJual <= hargaBeli)
            {
                XtraMessageBox.Show("Harga Jual <=  Harga Pokok Pembelian", "Info", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
            if (!decimal.TryParse(txtpotongan.Text, out decimal potongan)) potongan = 0;

            var newProduct = new TransactionDataBeli
            {
                ProductId = productid,
                Barcode = barcodeTextBox.Text,
                Kode_Item = txtItemBarang.Text,
                ProductName = txtnamabarang.Text,
                Satuan = txtsatuan.Text,
                Qty = qty,
                Hpp = hargaBeli,
                Price = hargaJual,
                Potongan = potongan,
            };
            newProduct.UpdateTotal();
            productid = 0;
            return newProduct;
        }

        public static void ClearInputFields(
            TextEdit barcodeTextBox,
            TextEdit txtItemBarang,
            TextEdit txtnamabarang,
            TextEdit txtsatuan,
            TextEdit txtqty,
            TextEdit txthargabeli,
            TextEdit txtpotongan,
            TextEdit texthpplama,
            TextEdit texthargajual,
            TextEdit txttotal = null)
        {
            barcodeTextBox.Text = string.Empty;
            txtItemBarang.Text = string.Empty;
            txtnamabarang.Text = string.Empty;
            txtsatuan.Text = string.Empty;
            txtqty.Text = "0";
            txthargabeli.Text = "0";
            txtpotongan.Text = "0";
            texthpplama.Text = "0";
            texthargajual.Text = "0";
            if (txttotal != null) txttotal.Text = "0";
            barcodeTextBox.Focus();
        }
    }
}
