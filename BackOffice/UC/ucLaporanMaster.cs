using BackOffice.BussinessLayer;
using BackOffice.Controller;
using BackOffice.Laporan;
using DevExpress.XtraPrinting;
using DevExpress.XtraReports.UI;
using DevExpress.XtraSplashScreen;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;

namespace BackOffice.UC
{
    public partial class ucLaporanMaster : UserControl
    {

        //Using singleton pattern to create an instance to ucModule3
        private static ucLaporanMaster? _instance;
        // Create an instance of PersediaanController
        AnggotaController controller = new ();
        MasterDataController controller2 = new();
        public static ucLaporanMaster Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ucLaporanMaster();
                return _instance;
            }
        }
        public ucLaporanMaster()
        {
            InitializeComponent();
        }

        private void ucLaporanMaster_Load(object sender, EventArgs e)
        {
            DateTime date = DateTime.Today;
            var firstDayOfMonth = new DateTime(date.Year, date.Month, 1);
            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddSeconds(-1);

            searchLookUpEdit1.Properties.DataSource = controller2.GetUnitKerja();
            searchLookUpEdit1.Properties.ValueMember = "KODE";
            searchLookUpEdit1.Properties.DisplayMember = "NAMA";
            


        }


        private void btncetak_Click(object sender, EventArgs e)
        {
            using var handle = SplashScreenManager.ShowOverlayForm(this);
            //try
            //{
            if (searchLookUpEdit1.EditValue == null) { return; }
               
                    XtraReport report = null;

                    switch (radioGroup1.SelectedIndex)
                    {
                        case 0:
                            
                            var kta = controller.GetAnggotaData();
                            var filterunitkerja = kta.Where(x => x.KODE_UNIT == searchLookUpEdit1.EditValue.ToString()).ToList();
                            report = new rptKTA
                            {
                                DataSource = filterunitkerja,
                                RequestParameters = true
                            };
                            report.ShowPreviewDialog();

                            break;

                        case 1:
                    var ktaexport = controller.GetAnggotaData();
                    var exportktatoimage = ktaexport.Where(x => x.KODE_UNIT == searchLookUpEdit1.EditValue.ToString()).ToList();
                    report = new rptktafile
                    {
                        DataSource = exportktatoimage,
                        RequestParameters = true
                    };
                    report.CreateDocument();

                    // Prompt the user to choose the export folder using a SaveFileDialog.
                    var saveFileDialog = new SaveFileDialog();
                    saveFileDialog.Filter = "PNG Image|*.png";
                    saveFileDialog.Title = "Export Report to Image";
                    saveFileDialog.FileName = report.Name + ".png";

                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        string imageExportFile = saveFileDialog.FileName;
                        report.PrintingSystem.ExportOptions.Image.ExportMode = ImageExportMode.DifferentFiles;
                        report.PrintingSystem.ExportToImage(imageExportFile);

                        // Open the folder containing the exported file using various methods.
                        string exportFolderPath = Path.GetDirectoryName(imageExportFile);
                        try
                        {
                            // Method 1: Using the default file explorer
                            Process.Start(exportFolderPath);
                        }
                        catch (Win32Exception)
                        {
                            try
                            {
                                // Method 2: Using explorer.exe
                                Process.Start("explorer.exe", exportFolderPath);
                            }
                            catch (Exception ex)
                            {
                                try
                                {
                                    // Method 3: Using cmd.exe
                                    Process.Start("cmd.exe", $"/c start {exportFolderPath}");
                                }
                                catch (Exception cmdEx)
                                {
                                    // Handle and log any exceptions encountered while attempting to open the folder.
                                    MessageBox.Show($"Failed to open the folder: {exportFolderPath}\n\n{cmdEx.Message}", "Error");
                                }
                            }
                        }
                    }



                    break;
                        case 2:
                           
                            break;
                        default:
                            // Handle the case where no option is selected
                            break;
                    }
                
         
               
            //}
            //catch (Exception ex)
            //{
            //    // Handle any other exceptions that may occur during report generation
            //    MessageBox.Show("An error occurred: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //}
        }

        private void searchLookUpEdit1_EditValueChanged(object sender, EventArgs e)
        {

        }
    }
}
