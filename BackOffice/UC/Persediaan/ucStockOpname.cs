using BackOffice.BussinessLayer;
using BackOffice.Controller;
using BackOffice.Model;
using DevExpress.Data;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraSplashScreen;
using System.ComponentModel;

namespace BackOffice.UC;

public partial class ucStockOpname : UserControl
{
    private readonly BindingList<TransactionStockOpname> _transactionDataList = [];
    private readonly StokOpnameController _controller = new();
    private bool _isInitializing;
    private bool _isSaving;
    private bool _minusStockImported;
    private bool _uncountedStockImported;
    private int _productId;

    private static ucStockOpname? _instance;

    public static ucStockOpname Instance => _instance ??= new ucStockOpname();

    public ucStockOpname()
    {
        InitializeComponent();
        gridControl1.DataSource = _transactionDataList;
    }

    private void barcodeTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            LoadProduct();
            e.Handled = true;
            return;
        }

        if (e.KeyCode == Keys.F2)
        {
            FocusLastPhysicalQuantity();
            e.Handled = true;
            return;
        }

        if (e.KeyCode == Keys.F5)
        {
            blbisimpan.PerformClick();
            e.Handled = true;
        }
    }

    private void LoadProduct()
    {
        if (!TryGetTransactionDate(out DateTime selectedDate))
        {
            ShowWarning("Tanggal Stock Opname tidak valid.");
            return;
        }

        string barcode = barcodeTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(barcode))
        {
            ShowWarning("Barcode tidak boleh kosong.");
            barcodeTextBox.Focus();
            return;
        }

        DTOProductInfo productInfo;
        try
        {
            productInfo = POS_Services.RetrieveProductInfo(barcode);
        }
        catch (Exception ex)
        {
            ShowError($"Informasi produk tidak dapat diambil.\n{ex.Message}");
            return;
        }

        if (productInfo.ProductId == 0)
        {
            using ProductForm productForm = new()
            {
                StartPosition = FormStartPosition.CenterScreen
            };
            productForm.SetSearchPanelValue(barcode);

            if (productForm.ShowDialog() != DialogResult.OK)
            {
                ResetItemFields(clearBarcode: false);
                barcodeTextBox.Focus();
                return;
            }

            productInfo = new DTOProductInfo
            {
                ProductId = productForm.ProductId,
                KodeItem = productForm.Kode_Item,
                ProductName = productForm.ProductName,
                Satuan = productForm.Satuan,
                Hpp = productForm.Hpp
            };
            barcode = productForm.Barcode;
        }

        if (productInfo.ProductId == 0 || string.IsNullOrWhiteSpace(productInfo.KodeItem))
        {
            ShowWarning("Produk yang dipilih tidak valid.");
            ResetItemFields();
            return;
        }

        try
        {
            DateTime startDate = new(selectedDate.Year, 1, 1);
            DTOStockDataItem? stock = _controller.GetStockData(
                productInfo.KodeItem,
                startDate,
                selectedDate);

            _productId = productInfo.ProductId;
            barcodeTextBox.Text = barcode;
            txtItemBarang.Text = productInfo.KodeItem;
            txtnamabarang.Text = productInfo.ProductName;
            txtsatuan.Text = productInfo.Satuan;
            // Value the item with the same weighted average the bulk import / year-end
            // closing use; fall back to product-master HPP only when there is no
            // movement history (TotalCostAvg = 0).
            decimal hpp = stock is { TotalCostAvg: > 0m } ? stock.TotalCostAvg : productInfo.Hpp;
            txthpp.Text = hpp.ToString("F2");
            txtqtysystem.Text = (stock?.StockAkhir ?? 0m).ToString("F2");
            txtqtyfisik.SelectAll();
            txtqtyfisik.Focus();
        }
        catch (Exception ex)
        {
            ShowError($"Stok sistem tidak dapat dihitung.\n{ex.Message}");
            ResetItemFields();
        }
    }

    private void NewTransaction()
    {
        _isInitializing = true;
        try
        {
            detanggal.EditValue = DateTime.Today;
            _transactionDataList.Clear();
            _minusStockImported = false;
            _uncountedStockImported = false;
            detanggal.Enabled = true;
            UpdateTemporaryTransactionNumber(DateTime.Today);
            ResetItemFields();
            UpdateBulkActionAvailability();
        }
        finally
        {
            _isInitializing = false;
        }
    }

    private void UpdateTemporaryTransactionNumber(DateTime date)
    {
        txtnotransaksi.Text = $"SO-{date.Year % 100:D2}-(otomatis)";
    }

    private void gridView1_RowCountChanged(object sender, EventArgs e)
    {
        for (int index = 0; index < _transactionDataList.Count; index++)
        {
            _transactionDataList[index].No = index + 1;
        }

        gridView1.RefreshData();
    }

    private void gridView1_CellValueChanged(
        object sender,
        DevExpress.XtraGrid.Views.Base.CellValueChangedEventArgs e)
    {
        if (e.Column.FieldName != nameof(TransactionStockOpname.QtyFisik))
        {
            return;
        }

        if (!decimal.TryParse(e.Value?.ToString(), out decimal quantity) || quantity < 0)
        {
            gridView1.SetColumnError(e.Column, "Qty Fisik harus berupa angka nol atau positif.");
            return;
        }

        gridView1.SetColumnError(e.Column, null);
        barcodeTextBox.Focus();
    }

    private void gridView1_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.F2)
        {
            gridView1.CloseEditor();
            gridView1.UpdateCurrentRow();
            barcodeTextBox.Focus();
            e.Handled = true;
            return;
        }

        if (e.KeyCode != Keys.Delete)
        {
            return;
        }

        if (gridView1.GetFocusedRow() is not TransactionStockOpname selectedItem)
        {
            return;
        }

        DialogResult result = XtraMessageBox.Show(
            $"Hapus {selectedItem.Kode_Item} - {selectedItem.ProductName} dari daftar?",
            "Hapus Baris",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result != DialogResult.Yes)
        {
            return;
        }

        _transactionDataList.Remove(selectedItem);
        if (_transactionDataList.Count == 0)
        {
            detanggal.Enabled = true;
        }

        e.Handled = true;
    }

    private void gridView1_ValidatingEditor(
        object sender,
        DevExpress.XtraEditors.Controls.BaseContainerValidateEditorEventArgs e)
    {
        if (gridView1.FocusedColumn?.FieldName != nameof(TransactionStockOpname.QtyFisik))
        {
            return;
        }

        if (!decimal.TryParse(e.Value?.ToString(), out decimal quantity))
        {
            e.Valid = false;
            e.ErrorText = "Qty Fisik harus berupa angka.";
            return;
        }

        if (quantity < 0)
        {
            e.Valid = false;
            e.ErrorText = "Qty Fisik tidak boleh negatif.";
        }
    }

    private void blbibatal_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
    {
        if (_transactionDataList.Count > 0
            && XtraMessageBox.Show(
                "Batalkan transaksi Stock Opname yang sedang diinput?",
                "Konfirmasi Batal",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) != DialogResult.Yes)
        {
            return;
        }

        NewTransaction();
    }

    private void ucPenjualan_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.F5)
        {
            blbisimpan.PerformClick();
            e.Handled = true;
        }
    }

    private void txtqty_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            SendKeys.Send("{TAB}");
        }
    }

    private void txtharga_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            SendKeys.Send("{TAB}");
        }
    }

    private void blbisimpan_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
    {
        if (_isSaving)
        {
            return;
        }

        gridView1.CloseEditor();
        if (!gridView1.UpdateCurrentRow())
        {
            ShowWarning("Perbaiki nilai Qty Fisik yang belum valid.");
            return;
        }

        if (!TryGetTransactionDate(out DateTime transactionDate))
        {
            ShowWarning("Tanggal Stock Opname tidak valid.");
            return;
        }

        List<TransactionStockOpname> items = _transactionDataList.ToList();
        if (!StockOpnameValidator.TryValidate(transactionDate, items, out string validationError))
        {
            ShowWarning(validationError);
            return;
        }

        decimal totalDifference = items.Sum(item => item.Total);
        DialogResult confirmation = XtraMessageBox.Show(
            $"Simpan Stock Opname?\n\nJumlah item: {items.Count:N0}\n"
            + $"Nilai selisih: {totalDifference:N0}",
            "Konfirmasi Simpan",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirmation != DialogResult.Yes)
        {
            return;
        }

        SetSavingState(true);
        try
        {
            string transactionNumber = _controller.SaveStockOpname(transactionDate, items);
            XtraMessageBox.Show(
                $"Stock Opname berhasil disimpan.\nNomor transaksi: {transactionNumber}",
                "Simpan Berhasil",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            NewTransaction();
        }
        catch (Exception ex)
        {
            ShowError($"Stock Opname gagal disimpan. Data input tetap dipertahankan.\n{ex.Message}");
        }
        finally
        {
            SetSavingState(false);
        }
    }

    private void SetSavingState(bool isSaving)
    {
        _isSaving = isSaving;
        blbisimpan.Enabled = !isSaving;
        blbibatal.Enabled = !isSaving;
        barcodeTextBox.Enabled = !isSaving;
        gridControl1.Enabled = !isSaving;
        UpdateBulkActionAvailability();
        Cursor = isSaving ? Cursors.WaitCursor : Cursors.Default;
    }

    private void detanggal_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            barcodeTextBox.Focus();
        }
    }

    private void lookUpEditSupplier_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            SendKeys.Send("{TAB}");
        }
    }

    private void txtpotongan_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            SendKeys.Send("{TAB}");
        }
    }

    private void txtqtyfisik_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Enter)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(txtItemBarang.Text) || _productId == 0)
        {
            ShowWarning("Pilih barang yang valid sebelum mengisi Qty Fisik.");
            barcodeTextBox.Focus();
            return;
        }

        if (!decimal.TryParse(txtqtysystem.Text, out decimal systemQuantity)
            || !decimal.TryParse(txtqtyfisik.Text, out decimal physicalQuantity)
            || !decimal.TryParse(txthpp.Text, out decimal hpp))
        {
            ShowWarning("Qty System, Qty Fisik, atau HPP tidak valid.");
            return;
        }

        if (physicalQuantity < 0)
        {
            ShowWarning("Qty Fisik tidak boleh negatif.");
            txtqtyfisik.SelectAll();
            txtqtyfisik.Focus();
            return;
        }

        TransactionStockOpname item = new()
        {
            Nomor_SO = txtnotransaksi.Text,
            Tanggal = detanggal.DateTime.Date,
            ProductId = _productId,
            Barcode = barcodeTextBox.Text.Trim(),
            Kode_Item = txtItemBarang.Text.Trim(),
            ProductName = txtnamabarang.Text.Trim(),
            Satuan = txtsatuan.Text.Trim(),
            QtySystem = systemQuantity,
            QtyFisik = physicalQuantity,
            Hpp = hpp
        };

        UpsertItem(item);
        ResetItemFields();
        e.Handled = true;
    }

    private void UpsertItem(TransactionStockOpname incomingItem)
    {
        TransactionStockOpname? existingItem = _transactionDataList.FirstOrDefault(
            item => string.Equals(
                item.Kode_Item,
                incomingItem.Kode_Item,
                StringComparison.OrdinalIgnoreCase));

        if (existingItem is null)
        {
            _transactionDataList.Add(incomingItem);
            existingItem = incomingItem;
        }
        else
        {
            existingItem.ProductId = incomingItem.ProductId;
            existingItem.Barcode = incomingItem.Barcode;
            existingItem.ProductName = incomingItem.ProductName;
            existingItem.Satuan = incomingItem.Satuan;
            existingItem.QtySystem = incomingItem.QtySystem;
            existingItem.Hpp = incomingItem.Hpp;
            existingItem.QtyFisik = incomingItem.QtyFisik;
        }

        detanggal.Enabled = false;
        int rowHandle = gridView1.LocateByValue(
            nameof(TransactionStockOpname.Kode_Item),
            existingItem.Kode_Item);
        if (rowHandle >= 0)
        {
            gridView1.FocusedRowHandle = rowHandle;
        }
    }

    private void ResetItemFields(bool clearBarcode = true)
    {
        _productId = 0;
        if (clearBarcode)
        {
            barcodeTextBox.Text = string.Empty;
        }

        txtItemBarang.Text = string.Empty;
        txtnamabarang.Text = string.Empty;
        txtsatuan.Text = string.Empty;
        txtqtysystem.Text = "0";
        txtqtyfisik.Text = "0";
        txthpp.Text = "0";
        barcodeTextBox.Focus();
    }

    private void FocusLastPhysicalQuantity()
    {
        if (gridView1.RowCount == 0)
        {
            return;
        }

        gridView1.Focus();
        gridView1.FocusedRowHandle = gridView1.RowCount - 1;
        gridView1.FocusedColumn = gridView1.Columns[nameof(TransactionStockOpname.QtyFisik)];
        gridView1.ShowEditor();
    }

    private void ucStockOpname_Load(object sender, EventArgs e)
    {
        ConfigureGrid();
        NewTransaction();
    }

    private void ConfigureGrid()
    {
        string[] hiddenColumns =
        [
            nameof(TransactionStockOpname.Nomor_SO),
            nameof(TransactionStockOpname.Tanggal),
            nameof(TransactionStockOpname.ProductId),
            nameof(TransactionStockOpname.Barcode)
        ];

        foreach (string columnName in hiddenColumns)
        {
            gridView1.Columns[columnName].Visible = false;
        }

        gridView1.Columns[nameof(TransactionStockOpname.No)].Width = 45;
        gridView1.Columns[nameof(TransactionStockOpname.ProductName)].Width = 300;
        gridView1.Columns[nameof(TransactionStockOpname.Hpp)].DisplayFormat.FormatType =
            DevExpress.Utils.FormatType.Numeric;
        gridView1.Columns[nameof(TransactionStockOpname.Hpp)].DisplayFormat.FormatString = "n0";
        gridView1.Columns[nameof(TransactionStockOpname.Total)].DisplayFormat.FormatType =
            DevExpress.Utils.FormatType.Numeric;
        gridView1.Columns[nameof(TransactionStockOpname.Total)].DisplayFormat.FormatString = "n0";

        foreach (GridColumn column in gridView1.Columns)
        {
            column.OptionsColumn.AllowSort = DevExpress.Utils.DefaultBoolean.False;
            column.OptionsColumn.ReadOnly =
                column.FieldName != nameof(TransactionStockOpname.QtyFisik);
        }

        gridView1.OptionsView.ShowFooter = true;
        gridView1.Columns[nameof(TransactionStockOpname.Kode_Item)].SummaryItem.SummaryType =
            SummaryItemType.Count;
        gridView1.Columns[nameof(TransactionStockOpname.Kode_Item)].SummaryItem.DisplayFormat =
            "Item: {0:N0}";
        gridView1.Columns[nameof(TransactionStockOpname.Total)].SummaryItem.SummaryType =
            SummaryItemType.Sum;
        gridView1.Columns[nameof(TransactionStockOpname.Total)].SummaryItem.DisplayFormat =
            "Selisih: {0:N0}";

        barLargeButtonItem1.Caption = "Impor Stok Minus";
        barLargeButtonItem1.Hint =
            "Khusus 31 Desember: tambahkan seluruh barang dengan stok sistem minus.";
        barLargeButtonItem4.Caption = "Set Nol Barang Belum Diopname";
        barLargeButtonItem4.Hint =
            "Khusus 31 Desember: tambahkan barang yang belum pernah diopname dengan Qty Fisik 0.";
    }

    private void detanggal_EditValueChanging(
        object sender,
        DevExpress.XtraEditors.Controls.ChangingEventArgs e)
    {
        if (!_isInitializing && _transactionDataList.Count > 0)
        {
            e.Cancel = true;
            ShowWarning("Tanggal tidak dapat diubah setelah daftar barang terisi.");
        }
    }

    private void detanggal_EditValueChanged(object sender, EventArgs e)
    {
        if (_isInitializing || !TryGetTransactionDate(out DateTime selectedDate))
        {
            return;
        }

        UpdateTemporaryTransactionNumber(selectedDate);
        _minusStockImported = false;
        _uncountedStockImported = false;
        UpdateBulkActionAvailability();
    }

    private void barLargeButtonItem1_ItemClick(
        object sender,
        DevExpress.XtraBars.ItemClickEventArgs e)
    {
        ImportStock(
            item => item.STOCK_AKHIR < 0,
            "stok sistem minus",
            ref _minusStockImported);
    }

    private void barLargeButtonItem4_ItemClick(
        object sender,
        DevExpress.XtraBars.ItemClickEventArgs e)
    {
        ImportStock(
            item => item.STOCK_OPNAME_COUNT == 0 && item.STOCK_AKHIR != 0,
            "barang yang belum pernah diopname",
            ref _uncountedStockImported);
    }

    private void ImportStock(
        Func<DTOStockData, bool> predicate,
        string description,
        ref bool importCompleted)
    {
        if (importCompleted)
        {
            XtraMessageBox.Show(
                $"Import {description} sudah diproses.",
                "Informasi",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        if (!TryGetTransactionDate(out DateTime transactionDate)
            || transactionDate.Month != 12
            || transactionDate.Day != 31)
        {
            ShowWarning("Import massal hanya tersedia untuk Stock Opname tanggal 31 Desember.");
            return;
        }

        SplashScreenManager.ShowForm(typeof(WaitForm1), true, true);
        try
        {
            DateTime startDate = new(transactionDate.Year, 1, 1);
            List<DTOStockData> candidates = _controller
                .GetProductStockInfo(startDate, transactionDate)
                .Where(predicate)
                .GroupBy(item => item.KODE_ITEM, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToList();

            SplashScreenManager.CloseForm(false);

            if (candidates.Count == 0)
            {
                XtraMessageBox.Show(
                    $"Tidak ditemukan {description}.",
                    "Informasi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                importCompleted = true;
                UpdateBulkActionAvailability();
                return;
            }

            DialogResult confirmation = XtraMessageBox.Show(
                $"Tambahkan {candidates.Count:N0} {description} dengan Qty Fisik 0?",
                "Konfirmasi Import",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirmation != DialogResult.Yes)
            {
                return;
            }

            foreach (DTOStockData stock in candidates)
            {
                UpsertItem(new TransactionStockOpname
                {
                    Nomor_SO = txtnotransaksi.Text,
                    Tanggal = transactionDate,
                    ProductId = 0,
                    Barcode = string.Empty,
                    Kode_Item = stock.KODE_ITEM,
                    ProductName = stock.PRODUCTNAME,
                    Satuan = stock.SATUAN,
                    QtySystem = stock.STOCK_AKHIR,
                    QtyFisik = 0,
                    Hpp = stock.TOTAL_COST_AVG
                });
            }

            importCompleted = true;
            UpdateBulkActionAvailability();
        }
        catch (Exception ex)
        {
            ShowError($"Import {description} gagal.\n{ex.Message}");
        }
        finally
        {
            if (SplashScreenManager.Default?.IsSplashFormVisible == true)
            {
                SplashScreenManager.CloseForm(false);
            }
        }
    }

    private void UpdateBulkActionAvailability()
    {
        bool isYearEnd = TryGetTransactionDate(out DateTime date)
            && date.Month == 12
            && date.Day == 31;

        barLargeButtonItem1.Enabled = !_isSaving && isYearEnd && !_minusStockImported;
        barLargeButtonItem4.Enabled = !_isSaving && isYearEnd && !_uncountedStockImported;
    }

    private bool TryGetTransactionDate(out DateTime date)
    {
        if (detanggal.EditValue is DateTime editDate)
        {
            date = editDate.Date;
            return true;
        }

        return DateTime.TryParse(detanggal.Text, out date);
    }

    private static void ShowWarning(string message)
    {
        XtraMessageBox.Show(
            message,
            "Stock Opname",
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning);
    }

    private static void ShowError(string message)
    {
        XtraMessageBox.Show(
            message,
            "Stock Opname",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }
}
