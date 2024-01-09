using BackOffice.BussinessLayer;
using BackOffice.DataLayer;
using BackOffice.Model;
using Dapper;
using DevExpress.XtraEditors;
using DevExpress.XtraSplashScreen;
using DevExpress.XtraWaitForm;
using Newtonsoft.Json;
using OfficeOpenXml;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static BackOffice.Model.DTOBackup;

namespace BackOffice
{
    public partial class frmfixedform : DevExpress.XtraEditors.XtraForm
    {
        public frmfixedform()
        {
            InitializeComponent();
        }

        private void SBFixed_Click(object sender, EventArgs e)
        {
            // Open dialog box to select an Excel file
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*";
            openFileDialog.Title = "Select Excel File";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                // Get the selected file path
                string filePath = openFileDialog.FileName;

                // Display the WaitForm
                SplashScreenManager.ShowForm(this, typeof(WaitForm1));

                //try
                //{
                    // Call the method to import the Excel file and process the data
                    List<DTOFixed> PenjualanFixed = ImportExcelToList(filePath);

                    // Perform further operations with the imported data as needed

                    UpdatePenjualanFromList(PenjualanFixed);

                    // Example: Simulate a time-consuming operation
                    System.Threading.Thread.Sleep(3000); // Sleep for 3 seconds

                    // After the busy process is complete, hide the splash screen
                    SplashScreenManager.CloseForm();

                    XtraMessageBox.Show("Update Penjualan Selesai", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                //}
                //catch (Exception ex)
                //{
                //    // Close the splash screen and display an error message
                //    SplashScreenManager.CloseForm();
                //    DevExpress.XtraEditors.XtraMessageBox.Show("An error occurred: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                //}
            }
        }

        private void UpdatePenjualanFromList(List<DTOFixed> penjualanFixed)
        {
            using OracleConnection connection = new(global.connectionString);
            connection.Open();

            foreach (var faktur in penjualanFixed)
            {
                string updateQuery = @"UPDATE POS_PENJUALAN 
                       SET ID_PELANGGAN = :idpelanggan,
                           NIK=:nik,
                           NAMA_PELANGGAN=:nama,
                           STATUS=:status,
                           UNIT_KERJA=:uker
                           WHERE NO_TRANSAKSI = :faktur";

                connection.Execute(updateQuery, new
                {
                    idpelanggan = faktur.ID_PELANGGAN,
                    nik = faktur.NIK,
                    nama = faktur.NAMA_PELANGGAN,
                    status = faktur.STATUS,
                    uker = faktur.UNIT_KERJA,
                    faktur = faktur.NO_TRANSAKSI
                });
            }
        }

        private List<DTOFixed> ImportExcelToList(string filePath)
        {
            List<DTOFixed> PenjualanList = new();
            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
            using var package = new ExcelPackage(new FileInfo(filePath));
            ExcelWorksheet worksheet = package.Workbook.Worksheets[0]; // Assuming the first worksheet

            int rowCount = worksheet.Dimension.Rows;
            int colCount = worksheet.Dimension.Columns;

            for (int row = 2; row <= rowCount; row++) // Assuming the header is in the first row
            {
                DTOFixed faktur = new DTOFixed
                {
                    NO_TRANSAKSI = worksheet.Cells[row, 1].Value?.ToString(),
                    ID_PELANGGAN = int.Parse(worksheet.Cells[row, 2].Value?.ToString()),
                    NIK = worksheet.Cells[row, 3].Value?.ToString(),
                    NAMA_PELANGGAN = worksheet.Cells[row, 4].Value?.ToString(),
                    STATUS = worksheet.Cells[row, 5].Value?.ToString(),
                    UNIT_KERJA = worksheet.Cells[row, 6].Value?.ToString()
                };

                PenjualanList.Add(faktur);
            }
            return PenjualanList;
        }
   

        private void btnImport_Click(object sender, EventArgs e)
        {
            try
            {
                var Restoredata = new RestoreData();
                // Create an instance of OpenFileDialog
                using OpenFileDialog openFileDialog = new();
                // Set the title and filter for the dialog box
                openFileDialog.Title = "Select JSON File";
                openFileDialog.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";

                // Show the dialog box and check if the user clicked OK
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    // Display the loading form
                    SplashScreenManager.ShowDefaultWaitForm("Please wait", "Restore up data...");
                    // Get the selected file path
                    string filePath = openFileDialog.FileName;

                    // Read the JSON data from the file
                    string jsonData = File.ReadAllText(filePath);

                    // Deserialize the JSON data into an object of MergedData type
                    var mergedData = JsonConvert.DeserializeObject<MergedData>(jsonData);

                    // Access the lists

                    List<FinAnggota> finAnggotaList = mergedData.AnggotaList;
                    List<PosProduct> barangList = mergedData.BarangList;
                    List<PEMBELIANMASTER> pembelianList = mergedData.PembelianList;
                    List<PEMBELIANDETAIL> pembelianDetailList = mergedData.PembelianDetailList;
                    List<PENJUALANMASTER> penjualanList = mergedData.PenjualanList;
                    List<PosPenjualanDetail> penjualanDetailList = mergedData.PenjualanDetailList;

                    //Restoredata.RestoreAnggota(finAnggotaList);
                    //Restoredata.RestoreBarang(barangList);
                    Restoredata.DeleteAndInsertMasterDetailData_Penjualan(penjualanList, penjualanDetailList);

                    // Continue with the rest of your code...
                    gridControl1.DataSource = penjualanList;
                    MessageBox.Show("Restore completed successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions or log errors
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Close the loading form
                SplashScreenManager.CloseDefaultWaitForm();
            }
        }      

        private void sbbackup_Click(object sender, EventArgs e)
        {
            try
            {
                // Display the loading form
                SplashScreenManager.ShowDefaultWaitForm("Please wait", "Backing up data...");

                var backupdata = new PosBackup();
                List<FinAnggota> anggotaList = backupdata.LoadAnggota();
                List<PosProduct> barangList = backupdata.LoadBarang();
                List<PEMBELIANMASTER> pembelianList = backupdata.LoadPembelian();
                List<PEMBELIANDETAIL> pembelianDetailList = backupdata.LoadPembelianDetail();
                List<PENJUALANMASTER> penjualanList = backupdata.LoadPosPenjualanData();
                List<PosPenjualanDetail> penjualanDetailList = backupdata.LoadPosPenjualanDetails();

                // Merge the lists into a single object
                var mergedData = new MergedData
                {
                    AnggotaList = anggotaList,
                    BarangList = barangList,
                    PembelianList = pembelianList,
                    PembelianDetailList = pembelianDetailList,
                    PenjualanList = penjualanList,
                    PenjualanDetailList = penjualanDetailList
                };

                // Convert the merged data to JSON
                string jsonData = JsonConvert.SerializeObject(mergedData, Formatting.Indented);

                // Create a SaveFileDialog
                using SaveFileDialog saveFileDialog = new();
                saveFileDialog.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
                saveFileDialog.FilterIndex = 1;
                saveFileDialog.RestoreDirectory = true;

                // Show the save dialog box
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    // Get the selected file name
                    string filePath = saveFileDialog.FileName;

                    // Write the JSON data to the selected file
                    File.WriteAllText(filePath, jsonData);
                    MessageBox.Show("Backup completed successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions or log errors
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Close the loading form
                SplashScreenManager.CloseDefaultWaitForm();
            }
        }
    }
}