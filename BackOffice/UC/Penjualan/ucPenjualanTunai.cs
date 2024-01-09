using BackOffice.BussinessLayer;
using BackOffice.Laporan;
using DevExpress.XtraBars;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Repository;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraReports.UI;
using System.Data;

namespace BackOffice.UC
{
    public partial class ucPenjualanTunai : UserControl
    {
        DataSet _ds;
        string JenisLaporan;
        //Using singleton pattern to create an instance to ucModule3
        private static ucPenjualanTunai _instance;
        public static ucPenjualanTunai Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ucPenjualanTunai();
                return _instance;
            }
        }
        public ucPenjualanTunai()
        {
            InitializeComponent();
        }


        private void ucPenjualanTunai_Load(object sender, EventArgs e)
        {
            Load_Bulan();
            Load_Remise();
        }

        private void Load_Remise()
        {
            Dictionary<int, string> Remises = new()
            {
                { 1, "1" },
                { 2, "2" }
            };
            repositoryItemLookUpEdit_Remise.DataSource = Remises;
        }
        string[] bulan = { "Januari", "Februari", "Maret", "April", "Mei", "Juni", "Juli", "Agustus", "September", "Oktober", "November", "Desember" };
        string[] periode = { "Remise I", "Remise II & Bulanan" };
        private void Load_Bulan()
        {
            
            int currentMonthIndex = DateTime.Now.Month - 1;
            ((RepositoryItemComboBox)bei_bulan.Edit).Items.AddRange(bulan);
            bei_bulan.EditValue = bulan[currentMonthIndex];

         
            bei_tahun.EditValue = DateTime.Now.Year;

           

            int currentremiseIndex = 0;
            if(DateTime.Today.Day > 15)
            {
                currentremiseIndex = 1;
            }
            ((RepositoryItemComboBox)bei_remise.Edit).Items.AddRange(periode);
            bei_remise.EditValue = periode[currentremiseIndex];




        }

        private void bei_tahun_EditValueChanged(object? sender, EventArgs e)
        {
            Load_Tagihan_Perioe();
        }

        private void bei_bulan_EditValueChanged(object sender, EventArgs e)
        {
            Load_Tagihan_Perioe();
        }      

        private void bei_remise_EditValueChanged(object sender, EventArgs e)
        {
            Load_Tagihan_Perioe();
        }
        DateTime p_daritanggal, p_daritanggalr2, p_sampaitanggal, p_tglAngsuran;

        private void repositoryItemRadioGroup1_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            RadioGroup radioGroup = sender as RadioGroup;
            JenisLaporan = radioGroup.EditValue.ToString();
            //XtraMessageBox.Show(JenisLaporan);
        }

        private void bei_bulan_ItemClick(object sender, ItemClickEventArgs e)
        {

        }

        private void bei_remise_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {

        }

        private void Load_Tagihan_Perioe()
        {
           
            if( bei_bulan.EditValue!= null && bei_tahun.EditValue!=null && bei_remise.EditValue != null)
            {
               
                int p_bulan =  Array.IndexOf(bulan, bei_bulan.EditValue.ToString())+1;
                int p_remise = Array.IndexOf(periode, bei_remise.EditValue.ToString())+1;
                var p_tahun = Convert.ToInt16(bei_tahun.EditValue.ToString());
                var p_periode = Convert.ToInt32(p_tahun.ToString() + p_bulan.ToString("00"));
                

                _ds = POS_Services.Penjualan_Tunai(p_periode,p_remise);
                //gridControl1.DataSource = _ds.Tables["rekap"];

                // Sets up a master-detail relationship between data tables.
                DataColumn keyColumn = _ds.Tables["rekap_NIK"].Columns["NIK"];
                DataColumn foreignKeyColumn = _ds.Tables["tagihan_waserda"].Columns["NIK"];
                _ds.Relations.Add("rekap_tagihan_waserda", keyColumn, foreignKeyColumn);

                //Bind the grid control to the data source
                gridControl1.DataSource = _ds.Tables["rekap_NIK"];
                gridControl1.ForceInitialize();

                //Hide the CategoryID column for the master View
                gridView1.Columns["UNIT_KERJA"].GroupIndex = 0;
                gridView1.Columns["SIMPANAN"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
                gridView1.Columns["SIMPANAN"].DisplayFormat.FormatString = "N0";
                gridView1.Columns["KREDIT"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
                gridView1.Columns["KREDIT"].DisplayFormat.FormatString = "N0";
                gridView1.Columns["PINJAMAN"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
                gridView1.Columns["PINJAMAN"].DisplayFormat.FormatString = "N0";
                gridView1.Columns["WASERDA"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
                gridView1.Columns["WASERDA"].DisplayFormat.FormatString = "N0";
                gridView1.Columns["TOTAL"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
                gridView1.Columns["TOTAL"].DisplayFormat.FormatString = "N0";




                // Creates a pattern view (CardView) to display detail data.
                GridView gridview_waserda = new (gridControl1);
                gridControl1.LevelTree.Nodes.Add("rekap_tagihan_waserda", gridview_waserda);
                // Specifies the detail view's caption (the caption of detail tabs).
                gridview_waserda.ViewCaption = "Detail Barang";

                //Create columns for the detail pattern View
                gridview_waserda.PopulateColumns(_ds.Tables["tagihan_waserda"]);
                //Hide the CategoryID column for the detail View
                gridview_waserda.OptionsView.ShowGroupPanel = false;
                gridview_waserda.Columns["NIK"].VisibleIndex = -1;
                gridview_waserda.Columns["NAMA_PELANGGAN"].VisibleIndex = -1;
                gridview_waserda.Columns["STATUS"].VisibleIndex = -1;
                gridview_waserda.Columns["UNIT_KERJA"].VisibleIndex = -1;
                //Format UnitPrice column values as currency
                gridview_waserda.Columns["HARGA_BARANG"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
                gridview_waserda.Columns["HARGA_BARANG"].DisplayFormat.FormatString = "N0";
                gridview_waserda.Columns["BRUTO"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
                gridview_waserda.Columns["BRUTO"].DisplayFormat.FormatString = "N0";
                gridview_waserda.Columns["POTONGAN"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
                gridview_waserda.Columns["POTONGAN"].DisplayFormat.FormatString = "N0";
                gridview_waserda.Columns["TOTAL_HARGA"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
                gridview_waserda.Columns["TOTAL_HARGA"].DisplayFormat.FormatString = "N0";
                
                
                // Sort the grid view by the TANGGAL column in ascending order
                gridview_waserda.Columns["TANGGAL"].SortOrder = DevExpress.Data.ColumnSortOrder.Ascending;

                GridGroupSummaryItem WASERDA = new()
                {
                    FieldName = "WASERDA",
                    ShowInGroupColumnFooter = gridView1.Columns["WASERDA"],
                    SummaryType = DevExpress.Data.SummaryItemType.Sum,
                    DisplayFormat = "{0:N0}"
                };
                gridView1.GroupSummary.Add(WASERDA);

                GridGroupSummaryItem TOTAL = new()
                {
                    FieldName = "TOTAL",
                    ShowInGroupColumnFooter = gridView1.Columns["TOTAL"],
                    SummaryType = DevExpress.Data.SummaryItemType.Sum,
                    DisplayFormat = "{0:N0}"
                };
                gridView1.GroupSummary.Add(TOTAL);

                //// Enable grouping in the GridView
                //gridView1.OptionsView.GroupFooterShowMode = GroupFooterShowMode.VisibleAlways;



                //// Add summary for "SIMPANAN" column
                //GridColumn simpananColumn = gridView1.Columns["SIMPANAN"];
                //simpananColumn.SummaryItem.SummaryType = DevExpress.Data.SummaryItemType.Sum;
                //simpananColumn.SummaryItem.DisplayFormat = "Total: {0:N0}";

                //// Add summary for "KREDIT" column
                //GridColumn kreditColumn = gridView1.Columns["KREDIT"];
                //kreditColumn.SummaryItem.SummaryType = DevExpress.Data.SummaryItemType.Sum;
                //kreditColumn.SummaryItem.DisplayFormat = "Total: {0:N0}";

                //// Add summary for "PINJAMAN" column
                //GridColumn pinjamanColumn = gridView1.Columns["PINJAMAN"];
                //pinjamanColumn.SummaryItem.SummaryType = DevExpress.Data.SummaryItemType.Sum;
                //pinjamanColumn.SummaryItem.DisplayFormat = "Total: {0:N0}";

                //// Add summary for "WASERDA" column
                //GridColumn waserdaColumn = gridView1.Columns["WASERDA"];
                //waserdaColumn.SummaryItem.SummaryType = DevExpress.Data.SummaryItemType.Sum;
                //waserdaColumn.SummaryItem.DisplayFormat = "Total: {0:N0}";

                // Enable footer display
                gridView1.OptionsView.ShowFooter = true;
                gridView1.OptionsFind.ShowFindButton = true;


                // Add summary for "TOTAL" column
                GridColumn totalColumn = gridView1.Columns["TOTAL"];
                totalColumn.SummaryItem.SummaryType = DevExpress.Data.SummaryItemType.Sum;
                totalColumn.SummaryItem.DisplayFormat = "Total: {0:N0}";
               


            }

        }

        private void BLBICETAK_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
           var periode= "PERIODE : "+bei_bulan.EditValue.ToString()+" " + bei_tahun.EditValue.ToString() + " ( "+bei_remise.EditValue.ToString() + " )";

            //_ds.WriteXmlSchema("Tagihan_Penjualan.xsd");

            if (JenisLaporan == "Rekap")
            {
                // Create instances of the reports you want to merge
                XtraReport report1 = new rptRekapbyUnitKerja();
                XtraReport report2 = new rptTagihan_Penjualan();

                // Set the data sources and parameters for each report
                report1.DataSource = _ds;
                report1.RequestParameters = true;
                report1.Parameters["PERIODE"].Value = periode;
                report1.CreateDocument();

                // Set other properties for report1 as needed

                report2.DataSource = _ds;
                report2.RequestParameters = true;
                report2.Parameters["PERIODE"].Value = periode;
                report2.CreateDocument();
                // Set other properties for report2 as needed

                report1.Pages.AddRange(report2.Pages);
                report1.PrintingSystem.ContinuousPageNumbering = true;

                ReportPrintTool tool = new ReportPrintTool(report1);
                tool.ShowPreview();
            }
            else
            {
                // Create instances of the reports you want to merge
                XtraReport report1 = new rptTagihan_PenjualanDetails
                {
                    // Set the data sources and parameters for each report
                    DataSource = _ds,
                    RequestParameters = true
                };
                report1.Parameters["PERIODE"].Value = periode;
                // Create a ReportPrintTool instance and assign the report to it
                ReportPrintTool printTool = new(report1);

                // Preview the report
                printTool.ShowPreviewDialog();
            }

        }
    }
}
