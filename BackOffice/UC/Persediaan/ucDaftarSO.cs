using BackOffice.Controller;
using BackOffice.Laporan;
using BackOffice.Model;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraReports.UI;
using OfficeOpenXml;
using System.Diagnostics;
using System.IO;

namespace BackOffice.UC.Persediaan
{
    public partial class ucDaftarSO : UserControl
    {
        StokOpnameController controller = new();
        List<DTOStoctOpnameMaster> StockOpnameList;
        public ucDaftarSO()
        {
            InitializeComponent();
        }

        private void ucDaftarSO_Load(object sender, EventArgs e)
        {
            spinEdit1.Value = DateTime.Now.Year;

            Load_StockOpname();
        }
        private void Load_StockOpname()
        {
            var tahun = (int)spinEdit1.Value;
            StockOpnameList = controller.DaftarStockOpname(tahun);
            gridControl1.DataSource = StockOpnameList;

            gridView1.Columns["BULAN"].GroupIndex = 0;
            gridView1.Columns["TOTAL"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            gridView1.Columns["TOTAL"].DisplayFormat.FormatString = "N0";
            gridView1.ExpandAllGroups();

            // DETAIL
            GridView detailGridView = new(gridControl1);
            gridControl1.LevelTree.Nodes.Add("Details", detailGridView);

            // Assuming StockOpnameDetailList is the property containing detail data in your StockOpnameMaster class
            detailGridView.PopulateColumns(typeof(DTOStoctOpnameDetail));

            detailGridView.OptionsBehavior.Editable = false;
            detailGridView.OptionsView.ShowGroupPanel = false;
            detailGridView.Columns["NOMOR_SO"].VisibleIndex = -1;
            // Add summary for "TOTAL" column
            GridColumn totalColumn = gridView1.Columns["TOTAL"];
            totalColumn.SummaryItem.SummaryType = DevExpress.Data.SummaryItemType.Sum;
            totalColumn.SummaryItem.DisplayFormat = "Total: {0:N0}";
        }
        private void sbcetak_Click(object sender, EventArgs e)
        {
            rptStockOpname report = new()
            {
                DataSource = StockOpnameList,
                ShowPrintMarginsWarning = false
            };
            report.Parameters["Tahun"].Value = "TAHUN " + spinEdit1.Value;
            ReportPrintTool tool = new(report);
            tool.ShowPreview();

        }

        private void spinEdit1_EditValueChanged(object sender, EventArgs e)
        {
            Load_StockOpname();
        }
    }
}
