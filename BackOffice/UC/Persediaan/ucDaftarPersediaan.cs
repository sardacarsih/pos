using BackOffice.Controller;
using BackOffice.Laporan;
using BackOffice.Model;
using DevExpress.Export;
using DevExpress.Utils.Menu;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraPrinting;
using DevExpress.XtraReports.UI;
using System.Diagnostics;
using System.IO;

namespace BackOffice.UC.Persediaan
{
    public partial class ucDaftarPersediaan : UserControl
    {
        StokOpnameController controller = new();
        PersediaanController persediaanController = new();
        List<DTOStockData> persediaan;
        public ucDaftarPersediaan()
        {
            InitializeComponent();
        }

        private void ucDaftarPersediaan_Load(object sender, EventArgs e)
        {
            dateEdit1.Text = DateTime.Today.ToString();

            Load_Persediaan();
        }

        private void dateEdit1_EditValueChanged(object sender, EventArgs e)
        {
            Load_Persediaan();
            if (radioGroup1.SelectedIndex == 1)
            {
                gridControl1.DataSource = persediaan.Where(saldo => saldo.STOCK_AKHIR < 0).ToList();
            }
        }

        private void radioGroup1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (radioGroup1.SelectedIndex == 0)
            {
                gridControl1.DataSource = persediaan;
                gridView1.ExpandAllGroups();
            }
            else
            {
                gridControl1.DataSource = persediaan.Where(saldo => saldo.STOCK_AKHIR < 0).ToList();
                gridView1.ExpandAllGroups();
            }
        }

        private void Load_Persediaan()
        {
            if (DateTime.TryParse(dateEdit1.Text, out DateTime enddate))
            {
                var startdate = new DateTime(enddate.Year, 1, 1);
                persediaan = controller.GetProductStockInfo(startdate, enddate);
                gridControl1.DataSource = persediaan;

                gridView1.Columns["IDX"].GroupIndex = 0;
                gridView1.ExpandAllGroups();
                gridView1.OptionsBehavior.Editable = false;
                gridView1.Columns["STOCKAWAL_HPP"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
                gridView1.Columns["STOCKAWAL_HPP"].DisplayFormat.FormatString = "N0";
                gridView1.Columns["BELI_HARGA_AVG"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
                gridView1.Columns["BELI_HARGA_AVG"].DisplayFormat.FormatString = "N0";
                gridView1.Columns["TOTAL_COST_AVG"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
                gridView1.Columns["TOTAL_COST_AVG"].DisplayFormat.FormatString = "N2";
                gridView1.Columns["PERSEDIAAN"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
                gridView1.Columns["PERSEDIAAN"].DisplayFormat.FormatString = "N2";
                gridView1.OptionsFind.AlwaysVisible = true;
                gridView1.OptionsView.ShowAutoFilterRow = true;

                gridView1.OptionsView.ShowFooter = true;
                gridView1.OptionsFind.ShowFindButton = true;
                gridView1.BestFitColumns();
                GridFormatRule gridFormatRule = new();
                FormatConditionRuleExpression formatConditionRuleExpression = new FormatConditionRuleExpression();


                gridFormatRule.Column = gridView1.Columns["STOCK_AKHIR"]; // Assuming "Stock" is the correct column name
                gridFormatRule.ApplyToRow = true;

                formatConditionRuleExpression.PredefinedName = "Red Fill, Red Text";
                formatConditionRuleExpression.Expression = "[STOCK_AKHIR] < 0";  // Update the condition to check if "Stock" is less than 0

                gridFormatRule.Rule = formatConditionRuleExpression;
                gridView1.FormatRules.Add(gridFormatRule);

                // Add summary for "TOTAL" column
                GridColumn totalColumn = gridView1.Columns["PERSEDIAAN"];
                totalColumn.SummaryItem.SummaryType = DevExpress.Data.SummaryItemType.Sum;
                totalColumn.SummaryItem.DisplayFormat = "Total: {0:N0}";
            }
        }

        private void sbexport_Click(object sender, EventArgs e)
        {
            GridView view = gridView1; // Replace with your actual GridView instance

            // Generate a temporary file path
            string tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.xlsx");

            view.ExportToXlsx(tempFilePath, new XlsxExportOptionsEx { ExportType = ExportType.WYSIWYG });


            // Open the temporary file with the default associated Excel program
            ProcessStartInfo psi = new(tempFilePath)
            {
                UseShellExecute = true
            };
            Process.Start(psi);
        }

        private void gridView1_PopupMenuShowing(object sender, PopupMenuShowingEventArgs e)
        {
            try
            {
                GridView view = sender as GridView;
                if (e.MenuType == DevExpress.XtraGrid.Views.Grid.GridMenuType.Row)
                {
                    int rowHandle = e.HitInfo.RowHandle;
                    //hapus menu jika ada
                    e.Menu.Items.Clear();
                    DXMenuItem kartustock = CreateMenuItemkartustock(view, rowHandle);


                    kartustock.BeginGroup = true;
                    e.Menu.Items.Add(kartustock);
                }
            }
            catch (SystemException ex)
            {
                XtraMessageBox.Show(ex.Message, "Error on Popup Menu", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private DXMenuItem CreateMenuItemkartustock(GridView? view, int rowHandle)
        {
            DXMenuItem checkItem = new("Kartu Stock", new EventHandler(OnKartustockClick));
            checkItem.ImageOptions.Image = imageCollection1.Images[0];
            return checkItem;
        }

        private void OnKartustockClick(object? sender, EventArgs e)
        {
            var rowhandle = gridView1.FocusedRowHandle;
            var kode = gridView1.GetRowCellValue(rowhandle, "KODE_ITEM").ToString();
            var NAMA = gridView1.GetRowCellValue(rowhandle, "PRODUCTNAME").ToString();
            var startdate = new  DateTime(Convert.ToDateTime(dateEdit1.Text).Year,1,1);
            var enddate = Convert.ToDateTime(dateEdit1.Text);
            XtraReport report1 = new rptkartustock
            {
                DataSource = persediaanController.KartuStokBarang(kode, startdate, enddate),
                RequestParameters = true
            };
            report1.Parameters["NAMA"].Value = NAMA +" (Periode : "+startdate.ToString("dd-MMM-yyyy")+" - "+ enddate.ToString("dd-MMM-yyyy")+" )";
            report1.ShowPreview();
        }
    }
}
