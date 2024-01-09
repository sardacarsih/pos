using BackOffice.DataLayer;
using BackOffice.Model;
using Dapper;
using DevExpress.Data.ODataLinq.Helpers;
using DevExpress.Mvvm.Native;
using DevExpress.Utils.DragDrop;
using DevExpress.Utils.Menu;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Base;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraGrid.Views.Grid.ViewInfo;
using DevExpress.XtraSplashScreen;
using OfficeOpenXml;
using Oracle.ManagedDataAccess.Client;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Media;

namespace BackOffice.View
{
    public partial class FrmJurnal : XtraForm
    {
        bool editjurnal, filter = false;
        string periodetujuan, p_iddata, p_nomorHID, P_IDDATA, P_NOMOR, P_PERIODE;
        int pbulan, ptahun;
        double selisihD, selisihK, nilai, nilai2, new_jurnalid, old_JurnalID;
        private readonly SoundPlayer Player = new();
        DataTable dtJurnalKasir, ListCoaAktif, dtAISHeader, dtAISDetail, dtJurnalInventory = new();

        IQueryable<JurnalHeaderDTO>? JurnalHeader = null;
        IQueryable<JurnalDetailDTO>? JurnalDetail = null;
        IEnumerable<JurnalDetailDTO> PencarianJurnal;
        IEnumerable<JurnalDetailDTO> ExportPencarian;
        IEnumerable<JurnalDetailDTO> PencarianJurnal_Bulan;
        IEnumerable<JurnalDetailDTO> ExportPencarian_Bulan;
        List<JurnalDetailReffID> ReffID = new();
        List<JurnalHeaderDTO> JurnalHeader_Filtered = new();

        string[] Bulan = { "Januari", "Februari", "Maret", "April", "Mei", "Juni", "Juli", "Agustus", "September", "Oktober", "Nopember", "Desember" };
        public FrmJurnal()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterScreen;
            this.WindowState = FormWindowState.Normal;
            this.TopMost = false;
        }


        private void Behavior_DragOver(object sender, DragOverEventArgs e)
        {

            DragOverGridEventArgs args = DragOverGridEventArgs.GetDragOverGridEventArgs(e);
            e.InsertType = args.InsertType;
            e.InsertIndicatorLocation = args.InsertIndicatorLocation;
            e.Action = args.Action;
            Cursor.Current = args.Cursor;
            args.Handled = true;
        }
        private void PilihanPeriodeAkuntansi()
        {
            try
            {
                using var handle = SplashScreenManager.ShowOverlayForm(this);
                handle.QueueFocus(IntPtr.Zero);

                var Periode_daftar_Jurnal = cmballperiode.Text;
                if (!string.IsNullOrEmpty(defiltertanggal.Text) || Int32.Parse(txtfilterjumlah.Text) > 0 || !string.IsNullOrEmpty(txtfilterkode.Text) || !string.IsNullOrEmpty(txtfilternojurnal.Text) || !string.IsNullOrEmpty(txtfilterketerangan.Text))
                {
                    JurnalDetail = GetJurnalDetails_Dapper("KOPKAR", Periode_daftar_Jurnal);
                    CariJurnal_Bulan();
                    GCHeader.Focus();
                }
                else
                {
                    //Stopwatch watch = new Stopwatch();
                    //watch.Start();                  

                    JurnalHeader = GetJurnalHeader_Dapper("KOPKAR", Periode_daftar_Jurnal);
                    JurnalDetail = GetJurnalDetails_Dapper("KOPKAR", Periode_daftar_Jurnal);

                    GCHeader.DataSource = JurnalHeader;
                    GCDetails.DataSource = null;
                    //watch.Stop();
                    //XtraMessageBox.Show(watch.ElapsedMilliseconds.ToString());

                    GVHeader.Columns["JURNALID"].Visible = false;
                    GVHeader.Columns["HID"].Visible = false;
                    //GVHeader.Columns["Tanggal"].OptionsColumn.FixedWidth = true;
                    //GVHeader.Columns["Tanggal"].Width = 90;
                    GVHeader.Columns["Tanggal"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
                    GVHeader.Columns["Tanggal"].DisplayFormat.FormatString = "dd-MMM-yyyy";
                    GVHeader.BestFitColumns();
                }

                GVHeader.Focus();
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show(ex.Message, "Error Pilihan Periode", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private DXMenuItem CreateMenuExportSelected(GridView view, int rowHandle)
        {
            DXMenuItem checkItem = new("Export Terpilih", new EventHandler(OnExportClick));
            checkItem.ImageOptions.Image = imageCollection1.Images[1];
            return checkItem;
        }

        private void OnExportClick(object? sender, EventArgs e)
        {
            List<double> selectedValues = new();

            // Iterate over the selected rows
            for (int i = 0; i < GVHeader.SelectedRowsCount; i++)
            {
                // Get the selected row handle
                int rowHandle = GVHeader.GetSelectedRows()[i];

                // Get the value from a specific column (replace "ColumnName" with the actual column name)
                double value = Convert.ToDouble(GVHeader.GetRowCellValue(rowHandle, "JURNALID").ToString());

                // Add the value to the list
                selectedValues.Add(value);
            }
            // Create a formatted string containing the selected values
            // string message = "Selected Values:\n\n" + string.Join("\n", selectedValues);

            if (selectedValues.Any())
            {
                ExportJurnalDipilih(selectedValues);
            }
            // Display the MessageBox with the selected values
            // XtraMessageBox.Show(message, "Selected Values", MessageBoxButtons.OK, MessageBoxIcon.Information);


        }

        private void ExportJurnalDipilih(List<double> selectedValues)
        {
            // If you use EPPlus in a noncommercial context
            // according to the Polyform Noncommercial license:
            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
            string filename = string.Empty;
            using (ExcelPackage package = new())
            {
                List<JurnalDetailDTO> selectedJurnalItems = JurnalDetail.Where(j => selectedValues.Contains(j.REFFID)).ToList();
                var wsDt = package.Workbook.Worksheets.Add("Jurnal Entries");

                //Load the datatable and set the number formats...
                wsDt.Cells["A1"].LoadFromCollection(selectedJurnalItems, true);

                //wsDt.Cells["A1"].LoadFromCollection(mydata);
                wsDt.DeleteColumn(1, 2);

                //Add the headers
                wsDt.Cells[1, 1].Value = "NoJurnal";
                wsDt.Cells[1, 2].Value = "Tanggal";
                wsDt.Cells[1, 3].Value = "RowNo";
                wsDt.Cells[1, 4].Value = "Kode";
                wsDt.Cells[1, 5].Value = "Rekening";
                wsDt.Cells[1, 6].Value = "Debet";
                wsDt.Cells[1, 7].Value = "Kredit";
                wsDt.Cells[1, 8].Value = "Keterangan";
                wsDt.Cells[1, 9].Value = "Posted";
                wsDt.Cells[1, 10].Value = "Periode";

                wsDt.Cells[2, 2, selectedJurnalItems.Count + 1, 2].Style.Numberformat.Format = "dd-MMM-yyyy";
                //=IF(A2<>A1,1,C1+1)
                wsDt.Cells[2, 3, selectedJurnalItems.Count + 1, 3].Formula = string.Format("IF(A2<>A1,1,C1+1)", new ExcelAddress(2, 3, selectedJurnalItems.Count + 1, 3).Address);

                // number formats
                string positiveFormat = "#,##0.00_)";
                string negativeFormat = "(#,##0.00)";
                string zeroFormat = "-_)";
                string numberFormat = positiveFormat + ";" + negativeFormat;
                string fullNumberFormat = positiveFormat + ";" + negativeFormat + ";" + zeroFormat;

                wsDt.Cells[2, 6, selectedJurnalItems.Count + 1, 7].Style.Numberformat.Format = fullNumberFormat;
                //wsDt.Cells[2, 7, dt.Rows.Count + 1, 7].Style.Numberformat.Format = "#,##0.00";

                // 
                wsDt.Cells[wsDt.Dimension.Address].AutoFitColumns();
                //package.Save();
                // package.Dispose();

                // Obtain the Excel file data as a byte array
                byte[] excelData = package.GetAsByteArray();

                // Generate a temporary file path
                string tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.xlsx");

                // Write the byte array to the temporary file
                File.WriteAllBytes(tempFilePath, excelData);

                // Open the temporary file with the default associated Excel program
                ProcessStartInfo psi = new(tempFilePath)
                {
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
        }


        private void cmbperiode_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                SendKeys.Send("{TAB}");
            }
        }


        private void sbbatal_Click(object sender, EventArgs e)
        {
            if (XtraMessageBox.Show("Batalkan Transaksi Jurnal ? ", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

        }



        private void JDgridView_CellValueChanged(object sender, DevExpress.XtraGrid.Views.Base.CellValueChangedEventArgs e)
        {
            GridView view = sender as GridView;
            if (view == null) return;
            if (e.Column.Caption != "DEBET") return;
            string cellValue = e.Value + "0" + view.GetRowCellValue(e.RowHandle, view.Columns["KREDIT"]);
            view.SetRowCellValue(e.RowHandle, view.Columns["DEBET"], cellValue);

        }


        private void JDgridView_CustomColumnDisplayText(object sender, CustomColumnDisplayTextEventArgs e)
        {
            if (e.Column.FieldName == "Debet")
                if (Convert.ToDecimal(e.Value) == 0) e.DisplayText = "0";
            if (e.Column.FieldName == "Kredit")
                if (Convert.ToDecimal(e.Value) == 0) e.DisplayText = "0";
        }
        private void simpleButton4_Click(object sender, EventArgs e)
        {
            try
            {

                if (cmballperiode.SelectedIndex != -1)
                {

                    cmballperiode.SelectedIndex--;
                    GCHeader.Focus();
                }
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        private void simpleButton3_Click(object sender, EventArgs e)
        {
            try
            {


                if (cmballperiode.SelectedIndex != -1)
                {
                    cmballperiode.SelectedIndex++;
                    GCHeader.Focus();
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("SelectedIndex"))
                {
                    XtraMessageBox.Show("Periode terakhir", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    XtraMessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

            }

        }


        private void GCHeader_Click(object sender, EventArgs e)
        {
            if (this.GVHeader.GetFocusedRowCellValue("NoJurnal") == null)
                return;
            FilterNomorJurnal();
        }

        private void FilterNomorJurnal()
        {

            try
            {
                var filter = GVHeader.GetRowCellValue(GVHeader.FocusedRowHandle, "NoJurnal").ToString();
                var filtered = JurnalDetail.Where(x => x.NoJurnal == filter).ToList();
                GCDetails.DataSource = filtered;
                GVDetail.Columns[0].Visible = false;
                GVDetail.Columns[1].Visible = false;
                GVDetail.Columns[2].Visible = false;
                GVDetail.Columns[3].OptionsColumn.FixedWidth = true;
                GVDetail.Columns[3].Width = 40;
                GVDetail.Columns[4].OptionsColumn.FixedWidth = true;
                GVDetail.Columns[4].Width = 120;
                GVDetail.Columns[5].OptionsColumn.FixedWidth = true;
                GVDetail.Columns[5].Width = 300;
                GVDetail.Columns[6].OptionsColumn.FixedWidth = true;
                GVDetail.Columns[6].Width = 170;
                GVDetail.Columns[7].OptionsColumn.FixedWidth = true;
                GVDetail.Columns[7].Width = 170;
                GVDetail.Columns[6].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
                GVDetail.Columns[6].DisplayFormat.FormatString = "n2";
                GVDetail.Columns[7].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
                GVDetail.Columns[7].DisplayFormat.FormatString = "n2";
                GVDetail.Columns[6].Summary.Clear();
                GVDetail.Columns[7].Summary.Clear();
                GVDetail.Columns[6].Summary.Add(DevExpress.Data.SummaryItemType.Sum, "Debet", "{0:N2}");
                GVDetail.Columns[7].Summary.Add(DevExpress.Data.SummaryItemType.Sum, "Kredit", "{0:N2}");
                GVDetail.Columns[9].Visible = false;
                GVDetail.Columns[10].Visible = false;
                GVDetail.BestFitColumns();
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show(ex.Message, "Error on filter no jurnal", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void simpleButton1_Click(object sender, EventArgs e)
        {
            this.Close();
        }



        private void GCHeader_KeyUp(object sender, KeyEventArgs e)
        {
            if (this.GVHeader.GetFocusedRowCellValue("NoJurnal") == null)
                return;
            FilterNomorJurnal();
        }






        private void GVHeader_GotFocus(object sender, EventArgs e)
        {
            if (this.GVHeader.GetFocusedRowCellValue("NoJurnal") == null)
                return;
            FilterNomorJurnal();
        }

        private void txtfilterjurnal_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                SendKeys.Send("{TAB}");
            }
            else if (e.KeyCode == Keys.Down)
            {
                GCHeader.Focus();
            }
        }



        private void txtfilterket_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                SendKeys.Send("{TAB}");
            }
            else if (e.KeyCode == Keys.Down)
            {
                GCHeader.Focus();
            }
        }

        GridHitInfo downHitInfo = null;
        private void JDgridView_MouseDown(object sender, MouseEventArgs e)
        {
            GridView view = sender as GridView;
            downHitInfo = null;

            GridHitInfo hitInfo = view.CalcHitInfo(new Point(e.X, e.Y));
            if (Control.ModifierKeys != Keys.None)
                return;
            if (e.Button == MouseButtons.Left && hitInfo.InRow && hitInfo.RowHandle != GridControl.NewItemRowHandle)
                downHitInfo = hitInfo;
        }

        private void JDgridView_MouseMove(object sender, MouseEventArgs e)
        {
            GridView view = sender as GridView;
            if (e.Button == MouseButtons.Left && downHitInfo != null)
            {
                Size dragSize = SystemInformation.DragSize;
                Rectangle dragRect = new Rectangle(new Point(downHitInfo.HitPoint.X - dragSize.Width / 2,
                    downHitInfo.HitPoint.Y - dragSize.Height / 2), dragSize);

                if (!dragRect.Contains(new Point(e.X, e.Y)))
                {
                    view.GridControl.DoDragDrop(downHitInfo, DragDropEffects.All);
                    downHitInfo = null;
                }
            }
        }

        private void GCJurnal_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(GridHitInfo)))
            {
                GridHitInfo downHitInfo = e.Data.GetData(typeof(GridHitInfo)) as GridHitInfo;
                if (downHitInfo == null)
                    return;

                GridControl grid = sender as GridControl;
                GridView view = grid.MainView as GridView;
                GridHitInfo hitInfo = view.CalcHitInfo(grid.PointToClient(new Point(e.X, e.Y)));
                if (hitInfo.InRow && hitInfo.RowHandle != downHitInfo.RowHandle && hitInfo.RowHandle != GridControl.NewItemRowHandle)
                    e.Effect = DragDropEffects.Move;
                else
                    e.Effect = DragDropEffects.None;
            }
        }

        private void allperiode()
        {
            var prd = PeriodeListAll(p_iddata);
            cmballperiode.DataSource = prd;
            cmballperiode.ValueMember = "PERIODE";
            cmballperiode.DisplayMember = "PERIODE";
        }


        private void CariJurnal_Bulan()
        {
            try
            {
                using var handle = SplashScreenManager.ShowOverlayForm(this);
                var dperiode = cmballperiode.Text;


                if (!string.IsNullOrEmpty(defiltertanggal.Text) || decimal.Parse(txtfilterjumlah.Text) > 0 || !string.IsNullOrEmpty(txtfilterkode.Text) || !string.IsNullOrEmpty(txtfilternojurnal.Text) || !string.IsNullOrEmpty(txtfilterketerangan.Text))
                {
                    filter = true;
                    lblrecordbulan.Visible = true;
                    PencarianJurnal_Bulan = SearchJurnal_Bulan("KOPKAR", dperiode, txtfilternojurnal.Text.ToLower(), defiltertanggal.Text, txtfilterkode.Text.ToLower(), txtfilterketerangan.Text.ToLower(), decimal.Parse(txtfilterjumlah.Text));
                    if (PencarianJurnal_Bulan.Count() > 0)
                    {
                        JurnalHeader_Filtered = PencarianJurnal_Bulan.GroupBy(g => new { g.REFFID, g.HIDREFF, g.NoJurnal, g.Tanggal })
                                                .Select(x => new JurnalHeaderDTO { JURNALID = x.Key.REFFID, HID = x.Key.HIDREFF, NoJurnal = x.Key.NoJurnal, Tanggal = x.Key.Tanggal }).ToList();


                        GCHeader.DataSource = JurnalHeader_Filtered;
                        GVHeader.Columns["JURNALID"].Visible = false;
                        //GVHeader.Columns["Tanggal"].OptionsColumn.FixedWidth = true;
                        //GVHeader.Columns["Tanggal"].Width = 90;
                        GVHeader.Columns["Tanggal"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
                        GVHeader.Columns["Tanggal"].DisplayFormat.FormatString = "dd-MMM-yyyy";
                        GVHeader.BestFitColumns();
                        lblrecordbulan.Text = "Filter Record : " + PencarianJurnal_Bulan.Count().ToString();
                        GCHeader.Focus();
                    }
                    else
                    {
                        GCHeader.DataSource = null;
                        GCDetails.DataSource = null;
                        lblrecordbulan.Text = "Filter Record : 0";

                        PencarianJurnal_Bulan = Enumerable.Empty<JurnalDetailDTO>();
                        // XtraMessageBox.Show("Data tidak ditemukan.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }

                }
                else
                {
                    filter = true;
                    lblrecordbulan.Visible = false;
                    lblrecordbulan.Text = "Filter Record : 0";
                    GCHeader.DataSource = JurnalHeader;
                    PencarianJurnal_Bulan = Enumerable.Empty<JurnalDetailDTO>();
                }
                GCHeader.Focus();
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private IEnumerable<JurnalDetailDTO> GetJurnalLengkap(List<JurnalDetailReffID> ReffID)
        {

            IEnumerable<JurnalDetailDTO> SearchJurnal;
            using (var contol = new OracleConnection(global.connectionString))
            {
                string sql = "SELECT REFFID,NOJURNAL,TANGGAL,BARIS,KODE,REKENING,DEBET,KREDIT,KETERANGAN,Posted,Periode FROM ACCT_JURNAL_DTL  WHERE REFFID IN :p_reffid order by periode,nojurnal,baris";

                if (contol.State == ConnectionState.Closed)
                    contol.Open();
                try
                {
                    SearchJurnal = contol.Query<JurnalDetailDTO>(sql, param: new { p_reffid = ReffID.Select(d => d.REFFID) });
                }
                catch (Exception)
                {
                    throw;
                }
                finally
                {
                    if (contol.State == ConnectionState.Open)
                        contol.Close();
                }
            }
            return SearchJurnal;
        }


        private void txtfilternojurnal_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                CariJurnal_Bulan();
            }
        }

        private void defiltertanggal_EditValueChanged(object sender, EventArgs e)
        {
            if (defiltertanggal.EditValue != null)
            {
                CariJurnal_Bulan();
            }
        }

        private void txtfilterkode_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                CariJurnal_Bulan();
            }
        }

        private void Sbfilterexport_Click(object sender, EventArgs e)
        {
            using var handle = SplashScreenManager.ShowOverlayForm(this);
            try
            {
                if (filter)
                {
                    if (cefilterlengkap.Checked == true)
                    {
                        ExportPencarian_Bulan = from detail in JurnalDetail
                                                join header in JurnalHeader_Filtered on detail.REFFID equals header.JURNALID
                                                select detail;
                    }
                    else
                    {
                        ExportPencarian_Bulan = PencarianJurnal_Bulan;
                    }
                    // If you use EPPlus in a noncommercial context
                    // according to the Polyform Noncommercial license:
                    ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                    string filename = string.Empty;
                    using (ExcelPackage package = new())
                    {

                        if (ExportPencarian_Bulan.Any())
                        {
                            var wsDt = package.Workbook.Worksheets.Add("Jurnal Entries");

                            //Load the datatable and set the number formats...
                            wsDt.Cells["A1"].LoadFromCollection(ExportPencarian_Bulan, true);

                            //wsDt.Cells["A1"].LoadFromCollection(mydata);
                            wsDt.DeleteColumn(1, 2);

                            //Add the headers
                            wsDt.Cells[1, 1].Value = "NoJurnal";
                            wsDt.Cells[1, 2].Value = "Tanggal";
                            wsDt.Cells[1, 3].Value = "RowNo";
                            wsDt.Cells[1, 4].Value = "Kode";
                            wsDt.Cells[1, 5].Value = "Rekening";
                            wsDt.Cells[1, 6].Value = "Debet";
                            wsDt.Cells[1, 7].Value = "Kredit";
                            wsDt.Cells[1, 8].Value = "Keterangan";
                            wsDt.Cells[1, 9].Value = "Posted";
                            wsDt.Cells[1, 10].Value = "Periode";

                            wsDt.Cells[2, 2, ExportPencarian_Bulan.Count() + 1, 2].Style.Numberformat.Format = "dd-MMM-yyyy";
                            //=IF(A2<>A1,1,C1+1)
                            wsDt.Cells[2, 3, ExportPencarian_Bulan.Count() + 1, 3].Formula = string.Format("IF(A2<>A1,1,C1+1)", new ExcelAddress(2, 3, ExportPencarian_Bulan.Count() + 1, 3).Address);

                            // number formats
                            string positiveFormat = "#,##0.00_)";
                            string negativeFormat = "(#,##0.00)";
                            string zeroFormat = "-_)";
                            string numberFormat = positiveFormat + ";" + negativeFormat;
                            string fullNumberFormat = positiveFormat + ";" + negativeFormat + ";" + zeroFormat;

                            wsDt.Cells[2, 6, ExportPencarian_Bulan.Count() + 1, 7].Style.Numberformat.Format = fullNumberFormat;
                            //wsDt.Cells[2, 7, dt.Rows.Count + 1, 7].Style.Numberformat.Format = "#,##0.00";

                            // 
                            wsDt.Cells[wsDt.Dimension.Address].AutoFitColumns();
                            //package.Save();
                            // package.Dispose();

                            // Obtain the Excel file data as a byte array
                            byte[] excelData = package.GetAsByteArray();

                            // Generate a temporary file path
                            string tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.xlsx");

                            // Write the byte array to the temporary file
                            File.WriteAllBytes(tempFilePath, excelData);

                            // Open the temporary file with the default associated Excel program
                            ProcessStartInfo psi = new(tempFilePath)
                            {
                                UseShellExecute = true
                            };
                            Process.Start(psi);

                        }
                    }
                }
                else
                {
                    ExportJurnal_Periode();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

            }

        }

        private void ExportJurnaldtExcel(DataTable dt)
        {
            try
            {
                // If you use EPPlus in a noncommercial context
                // according to the Polyform Noncommercial license:
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                string filename = string.Empty;
                using (ExcelPackage package = new())
                {

                    if (dt.Rows.Count > 0)
                    {
                        var wsDt = package.Workbook.Worksheets.Add("Jurnal Entries");

                        //Load the datatable and set the number formats...
                        wsDt.Cells["A1"].LoadFromDataTable(dt, true);

                        //wsDt.Cells["A1"].LoadFromCollection(mydata);
                        wsDt.DeleteColumn(11, 14);

                        //Add the headers
                        wsDt.Cells[1, 1].Value = "NoJurnal";
                        wsDt.Cells[1, 2].Value = "Tanggal";
                        wsDt.Cells[1, 3].Value = "RowNo";
                        wsDt.Cells[1, 4].Value = "Kode";
                        wsDt.Cells[1, 5].Value = "Rekening";
                        wsDt.Cells[1, 6].Value = "Debet";
                        wsDt.Cells[1, 7].Value = "Kredit";
                        wsDt.Cells[1, 8].Value = "Keterangan";
                        wsDt.Cells[1, 9].Value = "Posted";
                        wsDt.Cells[1, 10].Value = "Periode";

                        wsDt.Cells[2, 2, dt.Rows.Count + 1, 2].Style.Numberformat.Format = "dd-MMM-yyyy";
                        //=IF(A2<>A1,1,C1+1)
                        wsDt.Cells[2, 3, dt.Rows.Count + 1, 3].Formula = string.Format("IF(A2<>A1,1,C1+1)", new ExcelAddress(2, 3, dt.Rows.Count + 1, 3).Address);

                        // number formats
                        string positiveFormat = "#,##0.00_)";
                        string negativeFormat = "(#,##0.00)";
                        string zeroFormat = "-_)";
                        string numberFormat = positiveFormat + ";" + negativeFormat;
                        string fullNumberFormat = positiveFormat + ";" + negativeFormat + ";" + zeroFormat;

                        wsDt.Cells[2, 6, dt.Rows.Count + 1, 7].Style.Numberformat.Format = fullNumberFormat;
                        //wsDt.Cells[2, 7, dt.Rows.Count + 1, 7].Style.Numberformat.Format = "#,##0.00";

                        // 
                        wsDt.Cells[wsDt.Dimension.Address].AutoFitColumns();
                        // Obtain the Excel file data as a byte array
                        byte[] excelData = package.GetAsByteArray();

                        // Generate a temporary file path
                        string tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.xlsx");

                        // Write the byte array to the temporary file
                        File.WriteAllBytes(tempFilePath, excelData);

                        // Open the temporary file with the default associated Excel program
                        ProcessStartInfo psi = new(tempFilePath)
                        {
                            UseShellExecute = true
                        };
                        Process.Start(psi);
                    }
                    else
                    {
                        MessageBox.Show("Data tidak ditemukan", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

            }
        }

        private void ExportJurnal_Periode()
        {

            // If you use EPPlus in a noncommercial context
            // according to the Polyform Noncommercial license:
            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
            string filename = string.Empty;
            using (ExcelPackage package = new())
            {

                if (JurnalDetail.Any())
                {
                    var wsDt = package.Workbook.Worksheets.Add("Jurnal Entries");

                    //Load the datatable and set the number formats...
                    wsDt.Cells["A1"].LoadFromCollection(JurnalDetail, true);

                    //wsDt.Cells["A1"].LoadFromCollection(mydata);
                    wsDt.DeleteColumn(1, 2);

                    //Add the headers
                    wsDt.Cells[1, 1].Value = "NoJurnal";
                    wsDt.Cells[1, 2].Value = "Tanggal";
                    wsDt.Cells[1, 3].Value = "RowNo";
                    wsDt.Cells[1, 4].Value = "Kode";
                    wsDt.Cells[1, 5].Value = "Rekening";
                    wsDt.Cells[1, 6].Value = "Debet";
                    wsDt.Cells[1, 7].Value = "Kredit";
                    wsDt.Cells[1, 8].Value = "Keterangan";
                    wsDt.Cells[1, 9].Value = "Posted";
                    wsDt.Cells[1, 10].Value = "Periode";

                    wsDt.Cells[2, 2, JurnalDetail.Count() + 1, 2].Style.Numberformat.Format = "dd-MMM-yyyy";
                    //=IF(A2<>A1,1,C1+1)
                    wsDt.Cells[2, 3, JurnalDetail.Count() + 1, 3].Formula = string.Format("IF(A2<>A1,1,C1+1)", new ExcelAddress(2, 3, JurnalDetail.Count() + 1, 3).Address);

                    // number formats
                    string positiveFormat = "#,##0.00_)";
                    string negativeFormat = "(#,##0.00)";
                    string zeroFormat = "-_)";
                    string numberFormat = positiveFormat + ";" + negativeFormat;
                    string fullNumberFormat = positiveFormat + ";" + negativeFormat + ";" + zeroFormat;

                    wsDt.Cells[2, 6, JurnalDetail.Count() + 1, 7].Style.Numberformat.Format = fullNumberFormat;
                    //wsDt.Cells[2, 7, dt.Rows.Count + 1, 7].Style.Numberformat.Format = "#,##0.00";

                    // 
                    wsDt.Cells[wsDt.Dimension.Address].AutoFitColumns();
                    // Obtain the Excel file data as a byte array
                    byte[] excelData = package.GetAsByteArray();

                    // Generate a temporary file path
                    string tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.xlsx");

                    // Write the byte array to the temporary file
                    File.WriteAllBytes(tempFilePath, excelData);

                    // Open the temporary file with the default associated Excel program
                    ProcessStartInfo psi = new(tempFilePath)
                    {
                        UseShellExecute = true
                    };
                    Process.Start(psi);

                }
            }

        }

        private void cmballperiode_SelectedIndexChanged(object sender, EventArgs e)
        {
            PilihanPeriodeAkuntansi();
        }

        private void repositoryItemLookUpEditkode_QueryCloseUp(object sender, CancelEventArgs e)
        {
            SendKeys.Send("{TAB}");
        }

        private void deJurnal_Enter(object sender, EventArgs e)
        {
            DateEdit edit = sender as DateEdit;
            BeginInvoke(new MethodInvoker(() =>
            {
                edit.SelectionStart = 0;

                edit.SelectionLength = 2;
            }));
        }

        private void GVDetail_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.C)
            {
                GridView view = sender as GridView;
                object value = view.GetFocusedValue();
                Clipboard.SetText(value.ToString());
            }
        }




        private bool IsJurnalTrueRevised(GridView gridViewAISheader, int rowHandle)
        {
            try
            {
                string val = Convert.ToString(gridViewAISheader.GetRowCellValue(rowHandle, "JURNAL"));
                var nojurnal = Convert.ToString(gridViewAISheader.GetRowCellValue(rowHandle, "NOJURNAL"));
                return (val == "T" && !string.IsNullOrEmpty(nojurnal));
            }
            catch
            {
                return false;
            }
        }

        private bool IsJurnalTrue(GridView gridViewAISheader, int rowHandle)
        {
            try
            {
                string val = Convert.ToString(gridViewAISheader.GetRowCellValue(rowHandle, "JURNAL"));
                return (val == "Y");
            }
            catch
            {
                return false;
            }
        }


        private void gckasir_Detail_Click_1(object sender, EventArgs e)
        {

        }




        private void Sbfilterclear_Click(object sender, EventArgs e)
        {
            try
            {
                txtfilternojurnal.Text = "";
                defiltertanggal.Text = "";
                txtfilterkode.Text = "";
                txtfilterjumlah.Text = "0";
                txtfilterketerangan.Text = "";
                lblrecordbulan.Visible = false;
                filter = false;
                PencarianJurnal_Bulan = Enumerable.Empty<JurnalDetailDTO>();
                ExportPencarian_Bulan = Enumerable.Empty<JurnalDetailDTO>();
                // GCHeader.DataSource = JurnalHeader;
                // GCDetails.DataSource = null;
                PilihanPeriodeAkuntansi();
                GCHeader.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

            }
        }

        private void txtfilterjumlah_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                CariJurnal_Bulan();
            }
        }

        private void txtfilterketerangan_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                CariJurnal_Bulan();
            }
        }




        private void cmbbulan_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                SendKeys.Send("{TAB}");
            }
        }



        private DataTable PeriodeListAll(string piddata)
        {
            DataTable dataTable = new();

            using (OracleConnection connection = new(global.connectionString))
            {
                connection.Open();

                using OracleCommand command = new("SELECT DISTINCT PERIODE FROM acct_jurnal_hdr", connection);
                using (OracleDataAdapter adapter = new OracleDataAdapter(command))
                {
                    adapter.Fill(dataTable);
                }
            }

            return dataTable;
        }

        private void FrmJurnal_Load(object sender, EventArgs e)
        {
            allperiode();
        }
        public IQueryable<JurnalDetailDTO> GetJurnalDetails_Dapper(string p_iddata, string p_periode)
        {

            var parameters = new DynamicParameters();
            parameters.Add("p_iddata", p_iddata, DbType.String);
            parameters.Add("p_periode", p_periode, DbType.String);
            var sql1 = string.Empty;
            IEnumerable<JurnalDetailDTO> JurnalDetail;
            using (var contol = new OracleConnection(global.connectionString))
            {
                sql1 = "SELECT REFFID,HIDREFF,NOJURNAL,TANGGAL,BARIS,KODE,REKENING,DEBET,KREDIT,KETERANGAN,Posted,Periode FROM ACCT_JURNAL_DTL WHERE IDDATA=:p_iddata and PERIODE=:p_periode order by nojurnal,baris";

                if (contol.State == ConnectionState.Closed)
                    contol.Open();
                try
                {
                    JurnalDetail = contol.Query<JurnalDetailDTO>(sql1, parameters, commandType: CommandType.Text);
                }
                catch (Exception)
                {
                    throw;
                }

            }
            return JurnalDetail.AsQueryable();
        }

        public IQueryable<JurnalHeaderDTO> GetJurnalHeader_Dapper(string p_iddata, string p_periode)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_iddata", p_iddata, DbType.String);
            parameters.Add("p_periode", p_periode, DbType.String);
            var sql1 = string.Empty;
            IEnumerable<JurnalHeaderDTO> JurnalHeader;
            using (var contol = new OracleConnection(global.connectionString))
            {
                sql1 = "SELECT JURNALID,HID,NOJURNAL,TANGGAL FROM ACCT_JURNAL_HDR WHERE IDDATA=:p_iddata and PERIODE=:p_periode order by nojurnal";

                if (contol.State == ConnectionState.Closed)
                    contol.Open();
                try
                {
                    //KodePerkiraanSaldo = contol.Query<COADaftarPerkiraanSaldoDTO>(sql1, parameters, commandType: CommandType.Text);
                    JurnalHeader = contol.Query<JurnalHeaderDTO>(sql1, parameters, commandType: CommandType.Text);
                }
                catch (Exception)
                {
                    throw;
                }
                //finally
                //{
                //    if (contol.State == ConnectionState.Open)
                //        contol.Close();
                //}
            }
            return JurnalHeader.AsQueryable();
        }
        public IEnumerable<JurnalDetailDTO> SearchJurnal_Bulan(string p_iddata, string p_periode, string p_nojurnal, string p_tanggal, string p_kode, string p_keterangan, decimal p_jumlah)
        {
            IEnumerable<JurnalDetailDTO> SearchJurnal_Bulan;
            using (var contol = new OracleConnection(global.connectionString))
            {
                var dynamicParams = new DynamicParameters();

                string sql = "SELECT REFFID,HIDREFF,NOJURNAL,TANGGAL,BARIS ,KODE,REKENING,DEBET,KREDIT,KETERANGAN,Posted,Periode FROM ACCT_JURNAL_DTL  WHERE 1=1";

                if (p_iddata != null && !string.IsNullOrEmpty(p_periode))
                {
                    sql += " AND IDDATA =:p_iddata";
                    dynamicParams.Add("p_iddata", p_iddata, DbType.String);
                }
                if (!string.IsNullOrEmpty(p_periode) && p_iddata != null)
                {
                    sql += " AND PERIODE =:p_periode";
                    dynamicParams.Add("p_periode", p_periode, DbType.String);
                }
                if (!string.IsNullOrEmpty(p_nojurnal))
                {
                    sql += " AND LOWER(NOJURNAL) LIKE '%' || :p_nojurnal || '%'";
                    dynamicParams.Add("p_nojurnal", p_nojurnal, DbType.String);
                }
                if (!string.IsNullOrEmpty(p_tanggal))
                {
                    sql += " AND TANGGAL=:p_tanggal";
                    dynamicParams.Add("p_tanggal", Convert.ToDateTime(p_tanggal), DbType.Date);
                }

                if (!string.IsNullOrEmpty(p_kode))
                {
                    sql += " AND KODE LIKE :p_kode || '%'";
                    dynamicParams.Add("p_kode", p_kode, DbType.String);
                }
                if (!string.IsNullOrEmpty(p_keterangan))
                {
                    sql += " AND LOWER(KETERANGAN) LIKE '%' || :p_keterangan || '%'";
                    dynamicParams.Add("p_keterangan", p_keterangan, DbType.String);
                }
                if (p_jumlah > 0)
                {
                    sql += " AND (DEBET = :p_jumlah OR  KREDIT = :p_jumlah)";
                    dynamicParams.Add("p_jumlah", p_jumlah, DbType.Decimal);
                }
                sql += " order by nojurnal,baris";

                if (contol.State == ConnectionState.Closed)
                    contol.Open();
                try
                {
                    SearchJurnal_Bulan = contol.Query<JurnalDetailDTO>(sql, dynamicParams);
                }
                catch (Exception)
                {
                    throw;
                }
            }
            return SearchJurnal_Bulan;
        }

        private void FrmJurnal_FormClosed(object sender, FormClosedEventArgs e)
        {
            //this.Close();
        }
    }
}