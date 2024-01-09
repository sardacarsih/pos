using BackOffice.Laporan;
using DevExpress.XtraEditors;
using DevExpress.XtraReports.UI;
using System;
using System.Data;
using System.Linq;

namespace BackOffice.View
{
    public partial class rptparameter : DevExpress.XtraEditors.XtraForm
    {
        public DataSet ReportDataSet { get; set; }
        public DataSet dsPinjamandanKredit { get; set; }
        public int remise { get; set; }
        public string report_periode { get; set; }
        public rptparameter()
        {
            InitializeComponent();
        }

        private void sbcetak_Click(object sender, EventArgs e)
        {
            if (radioGroup1.SelectedIndex == 0)
            {
                if (remise == 1)
                {
                    XtraMessageBox.Show("Tagihan ini hanya ada di Remise 2 dan Bulanan");
                    return;
                }
                //dsPinjamandanKredit.WriteXmlSchema("Pinjamandankredit.xsd");
                XtraReport report1 = new rptDaftarTagihanPinjamanTunai
                {
                    // Set the data sources and parameters for each report
                    DataSource = dsPinjamandanKredit,
                    RequestParameters = true
                };
                report1.Parameters["PERIODE"].Value = report_periode;
                report1.Parameters["ADMIN"].Value = "_________________ ";
                report1.Parameters["KETUA"].Value = "_________________ ";
                // Create a ReportPrintTool instance and assign the report to it
                ReportPrintTool printTool = new(report1);

                // Show the print preview dialog
                printTool.ShowPreview();

            }
            else if (radioGroup1.SelectedIndex == 1)
            {
                if (remise == 1)
                {
                    XtraMessageBox.Show("Tagihan ini hanya ada di Remise 2  dan Bulanan");
                    return;
                }
                //dsPinjamandanKredit.WriteXmlSchema("Pinjamandankredit.xsd");
                XtraReport report1 = new rptDaftarTagihanKreditBarang
                {
                    // Set the data sources and parameters for each report
                    DataSource = dsPinjamandanKredit,
                    RequestParameters = true
                };
                report1.Parameters["PERIODE"].Value = report_periode;
                report1.Parameters["ADMIN"].Value = "_________________ ";
                report1.Parameters["KETUA"].Value = "_________________ ";
                // Create a ReportPrintTool instance and assign the report to it
                ReportPrintTool printTool = new(report1);

                // Show the print preview dialog
                printTool.ShowPreview();
            }
        }

        private void simpleButton1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

    }
}