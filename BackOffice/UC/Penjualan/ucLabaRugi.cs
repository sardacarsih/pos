using BackOffice.Controller;
using BackOffice.Laporan;
using BackOffice.Model;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraReports.UI;

namespace BackOffice.UC
{
    public partial class ucLabaRugi : UserControl
    {
        PenjualanController controller = new();
        List<DTOLabaRugi> LabaRugiList;
        public ucLabaRugi()
        {
            InitializeComponent();
        }

        private void ucLabaRugi_Load(object sender, EventArgs e)
        {
            spinEdit1.Value = DateTime.Now.Year;

            Load_LabaRugi();
        }
        private void Load_LabaRugi()
        {
            var tahun = (int)spinEdit1.Value;
            LabaRugiList = controller.GetLabaRugi(tahun);
            gridControl1.DataSource = LabaRugiList;

            gridView1.Columns["BULAN"].Visible =false;
            gridView1.Columns["BULANINT"].GroupIndex = 0;
            gridView1.Columns["PENJUALAN"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            gridView1.Columns["PENJUALAN"].DisplayFormat.FormatString = "N0";
            gridView1.Columns["HPP"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            gridView1.Columns["HPP"].DisplayFormat.FormatString = "N0";
            gridView1.Columns["LABA"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            gridView1.Columns["LABA"].DisplayFormat.FormatString = "N0";
            //gridView1.ExpandAllGroups();           

            // Add subtotals for "PENJUALAN," "HPP," and "LABA" columns in the footer
            // Handle the CustomSummaryCalculate event to calculate subtotals in the footer
            //gridView1.CustomSummaryCalculate += GridView1_CustomSummaryCalculate;

            // Enable the footer to show summaries
            gridView1.OptionsView.ShowFooter = true;

            gridView1.GroupSummary.Clear();
            gridView1.GroupSummary.Add(new GridGroupSummaryItem(DevExpress.Data.SummaryItemType.Sum, "PENJUALAN", null, "(PENJUALAN = {0:N0})"));
            gridView1.GroupSummary.Add(new GridGroupSummaryItem(DevExpress.Data.SummaryItemType.Sum, "HPP", null, "(HPP = {0:N0})"));
            gridView1.GroupSummary.Add(new GridGroupSummaryItem(DevExpress.Data.SummaryItemType.Sum, "LABA", null, "(LABA = {0:N0})"));

            // Add subtotals for "PENJUALAN," "HPP," and "LABA" columns within each group
            gridView1.GroupSummary.Add(DevExpress.Data.SummaryItemType.Sum, "PENJUALAN", gridView1.Columns["PENJUALAN"], "Subtotal Penjualan: {0:N0}");
            gridView1.GroupSummary.Add(DevExpress.Data.SummaryItemType.Sum, "HPP", gridView1.Columns["HPP"], "Subtotal HPP: {0:N0}");
            gridView1.GroupSummary.Add(DevExpress.Data.SummaryItemType.Sum, "LABA", gridView1.Columns["LABA"], "Subtotal Laba: {0:N0}");

            // Add summary for "PENJUALAN" column
            GridColumn penjualanColumn = gridView1.Columns["PENJUALAN"];
            penjualanColumn.SummaryItem.SummaryType = DevExpress.Data.SummaryItemType.Sum;
            penjualanColumn.SummaryItem.DisplayFormat = "Total Penjualan: {0:N0}";

            // Add summary for "HPP" column
            GridColumn hppColumn = gridView1.Columns["HPP"];
            hppColumn.SummaryItem.SummaryType = DevExpress.Data.SummaryItemType.Sum;
            hppColumn.SummaryItem.DisplayFormat = "Total HPP: {0:N0}";

            // Add summary for "LABA" column
            GridColumn labaColumn = gridView1.Columns["LABA"];
            labaColumn.SummaryItem.SummaryType = DevExpress.Data.SummaryItemType.Sum;
            labaColumn.SummaryItem.DisplayFormat = "Total LABA: {0:N0}";
        }
        private void sbcetak_Click(object sender, EventArgs e)
        {
            rptStockOpname report = new()
            {
                DataSource = LabaRugiList,
                ShowPrintMarginsWarning = false
            };
            report.Parameters["Tahun"].Value = "TAHUN " + spinEdit1.Value;
            ReportPrintTool tool = new(report);
            tool.ShowPreview();

        }

        private void spinEdit1_EditValueChanged(object sender, EventArgs e)
        {
            Load_LabaRugi();
        }
    }
}
