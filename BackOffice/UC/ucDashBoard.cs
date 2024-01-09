using BackOffice.Model;
using DevExpress.DashboardCommon;
using DevExpress.DashboardCommon.Native;
using DevExpress.DashboardWin;
using DevExpress.DataAccess.EntityFramework;
using DevExpress.ExpressApp;

namespace BackOffice.UC
{
    public partial class ucDashBoard : DevExpress.XtraEditors.XtraUserControl
    {
        private DevExpress.DashboardWin.DashboardViewer dashboardViewer;
        private DevExpress.DashboardCommon.Dashboard dashboard;

        // Using singleton pattern to create an instance to ucModule3
        private static ucDashBoard _instance;

        public static ucDashBoard Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ucDashBoard();
                return _instance;
            }
        }

        public ucDashBoard()
        {
            InitializeComponent();
            InitializeDashboardViewer();
        }

        private void InitializeDashboardViewer()
        {
            dashboardViewer = new DevExpress.DashboardWin.DashboardViewer();
            dashboardViewer.Dock = System.Windows.Forms.DockStyle.Fill;
            dashboardViewer.CustomizeDashboardTitle += DashboardViewer1_CustomizeDashboardTitle;
            Controls.Add(dashboardViewer);
        }

        private void ucDashBoard_Load(object sender, EventArgs e)
        {
            try
            {
                // Create an instance of DTODashboard
                DTODashboard dashboardData = new DTODashboard();

                // Call the GetTop20SalesSummary method to get the list of SalesSummary objects
                List<SalesSummary> salesList = dashboardData.GetTop20SalesSummary(DateTime.Today.Year);

                // Create a Dashboard
                dashboard = new Dashboard();
                DashboardObjectDataSource objectDataSource = new DashboardObjectDataSource();

                // Set the data source and data member
                objectDataSource.DataSource = salesList;
                objectDataSource.Fill();

                // Add the data source to the dashboard
                dashboard.DataSources.Add(objectDataSource);

                if (dashboard.DataSources.Count > 0)
                {
                    // Configure Pie Chart
                    PieDashboardItem pies = new PieDashboardItem();
                    pies.DataSource = dashboard.DataSources[0];
                    Dimension salesPersonArgument = new Dimension("NAMA_BARANG");
                    Measure quantity = new Measure("QTY");
                    quantity.NumericFormat.FormatType = DataItemNumericFormatType.Number;
                    quantity.NumericFormat.Precision = 2; // Set precision to 0 for N0 format
                    pies.Arguments.Add(salesPersonArgument);
                    pies.Values.Add(quantity);
                    // Set title for the Pie Chart
                    //pies.Text = "Top 20 Sales This Year"; // Add this line
                    // Configure Grid
                    GridDashboardItem grid = new GridDashboardItem();
                    grid.DataSource = dashboard.DataSources[0];
                    grid.Columns.Add(new GridDimensionColumn(new Dimension("NAMA_BARANG")));
                    grid.Columns.Add(new GridMeasureColumn(new Measure("QTY") { NumericFormat = { FormatType = DataItemNumericFormatType.Number } }));
                    grid.Columns.Add(new GridMeasureColumn(new Measure("JUMLAH") { NumericFormat = { FormatType = DataItemNumericFormatType.Number} }));


                    // Add items to the dashboard
                    dashboard.Items.AddRange(pies, grid);

                    // Load the dashboard into the DashboardViewer
                    dashboardViewer.Dashboard = dashboard;
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private ChartDashboardItem CreateChart(DashboardObjectDataSource dataSource)
        {
            // Creates a chart dashboard item and specifies its data source.
            ChartDashboardItem chart = new ChartDashboardItem();
            chart.DataSource = dataSource;

            // Specifies the dimension used to provide data for arguments on a chart.
            chart.Arguments.Add(new Dimension("Sales Person", DateTimeGroupInterval.Year));

            // Specifies the dimension that provides data for chart series.
            chart.SeriesDimensions.Add(new Dimension("OrderDate"));

            // Adds a new chart pane to the chart's Panes collection.
            chart.Panes.Add(new ChartPane());

            // Creates a new series of the Bar type.
            DevExpress.DashboardCommon.SimpleSeries salesAmountSeries = new DevExpress.DashboardCommon.SimpleSeries(DevExpress.DashboardCommon.SimpleSeriesType.Bar);

            // Specifies the measure that provides data used to calculate
            // the Y-coordinate of data points.
            salesAmountSeries.Value = new Measure("Extended Price");

            // Adds created series to the pane's Series collection to display within this pane.
            chart.Panes[0].Series.Add(salesAmountSeries);

            chart.Panes.Add(new ChartPane());

            // Creates a new series of the StackedBar type.
            DevExpress.DashboardCommon.SimpleSeries taxesAmountSeries = new DevExpress.DashboardCommon.SimpleSeries(DevExpress.DashboardCommon.SimpleSeriesType.StackedBar);

            taxesAmountSeries.Value = new Measure("Quantity");
            chart.Panes[1].Series.Add(taxesAmountSeries);

            return chart;
        }


        private void DashboardViewer1_CustomizeDashboardTitle(object sender, CustomizeDashboardTitleEventArgs e)
        {
            // Create a custom toolbar item named "Reload Data"
            DashboardToolbarItem loadDataButton = new DashboardToolbarItem(
                "LoadData",
                new Action<DashboardToolbarItemClickEventArgs>(args =>
                {
                    // Reload data when the button is clicked
                    dashboardViewer.ReloadData();
                })
            );

            // Set the caption for the button
            loadDataButton.Caption = "Reload Data";

            // Add the custom toolbar item to the title
            e.Items.Add(loadDataButton);
        }
    }
}
