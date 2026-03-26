using Oracle.ManagedDataAccess.Client;
using System.ComponentModel;
using System.Data;
using System.Text;
using BackOffice.DataLayer;
using DevExpress.XtraGrid.Views.Base;

namespace BackOffice.UC
{
    public partial class ucPeriode : UserControl
    {
        private BindingList<PeriodeDTO> periodeList;
        private PeriodeDAL periodeDAL;
        private int currentTahun;
        //Using singleton pattern to create an instance to ucModule3
        private static ucPeriode _instance;
        public static ucPeriode Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ucPeriode();
                return _instance;
            }
        }
        public ucPeriode()
        {
            InitializeComponent();
            gridView1.CellValueChanged += GridView1_CellValueChanged;
            gridView1.ValidateRow += GridView1_ValidateRow;
            setahun.EditValueChanged += Setahun_EditValueChanged;
        }

        private void Setahun_EditValueChanged(object? sender, EventArgs e)
        {
            LoadList_Periode();
        }

        private void GridView1_CellValueChanged(object sender, CellValueChangedEventArgs e)
        {
            try
            {
                var view = sender as ColumnView;
                var rowHandle = e.RowHandle;

                if (rowHandle < 0 || rowHandle >= periodeList.Count)
                    return;

                var periode = periodeList[rowHandle];
                var columnName = e.Column.FieldName;
                var newValue = e.Value;

                // Update the DTO based on the column changed
                UpdatePeriodeDTO(periode, columnName, newValue);

                // Validate the changes
                var validationErrors = periode.Validate();
                if (validationErrors.Any())
                {
                    MessageBox.Show(string.Join("\n", validationErrors), "Validation Error",
                                  MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    view.RefreshData();
                    return;
                }

                // Update database
                using (var dal = new PeriodeDAL())
                {
                    if (dal.UpdatePeriode(periode))
                    {
                        // Success - refresh the specific row
                        view.RefreshRow(rowHandle);
                    }
                    else
                    {
                        MessageBox.Show("Tidak ada data yang diupdate.", "Info",
                                      MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating data: {ex.Message}", "Update Error",
                               MessageBoxButtons.OK, MessageBoxIcon.Error);

                // Refresh data to show original values
                LoadList_Periode();
            }
        }

        private void UpdatePeriodeDTO(PeriodeDTO periode, string columnName, object newValue)
        {
            switch (columnName.ToUpper())
            {
                case "REMISE1":
                    periode.Remise1 = newValue?.ToString() ?? "";
                    break;
                case "R1DARI":
                    periode.R1Dari = ConvertToNullableDateTime(newValue);
                    break;
                case "R1SAMPAI":
                    periode.R1Sampai = ConvertToNullableDateTime(newValue);
                    break;
                case "REMISE2":
                    periode.Remise2 = newValue?.ToString() ?? "";
                    break;
                case "R2DARI":
                    periode.R2Dari = ConvertToNullableDateTime(newValue);
                    break;
                case "R2SAMPAI":
                    periode.R2Sampai = ConvertToNullableDateTime(newValue);
                    break;
                case "BULANAN":
                    periode.Bulanan = newValue?.ToString() ?? "";
                    break;
                case "BDARI":
                    periode.BDari = ConvertToNullableDateTime(newValue);
                    break;
                case "BSAMPAI":
                    periode.BSampai = ConvertToNullableDateTime(newValue);
                    break;
            }
        }

        private DateTime? ConvertToNullableDateTime(object value)
        {
            if (value == null || value == DBNull.Value)
                return null;

            if (value is DateTime dateTime)
                return dateTime;

            if (DateTime.TryParse(value.ToString(), out DateTime parsedDate))
                return parsedDate;

            return null;
        }

        private void GridView1_ValidateRow(object sender, ValidateRowEventArgs e)
        {
            try
            {
                var view = sender as ColumnView;
                var rowHandle = e.RowHandle;

                if (rowHandle < 0 || rowHandle >= periodeList.Count)
                    return;

                var periode = periodeList[rowHandle];

                // Get current values from the grid (in case they haven't been committed yet)
                var tempPeriode = new PeriodeDTO
                {
                    R1Dari = ConvertToNullableDateTime(view.GetRowCellValue(rowHandle, "R1Dari")),
                    R1Sampai = ConvertToNullableDateTime(view.GetRowCellValue(rowHandle, "R1Sampai")),
                    R2Dari = ConvertToNullableDateTime(view.GetRowCellValue(rowHandle, "R2Dari")),
                    R2Sampai = ConvertToNullableDateTime(view.GetRowCellValue(rowHandle, "R2Sampai")),
                    BDari = ConvertToNullableDateTime(view.GetRowCellValue(rowHandle, "BDari")),
                    BSampai = ConvertToNullableDateTime(view.GetRowCellValue(rowHandle, "BSampai"))
                };

                var validationErrors = tempPeriode.Validate();
                if (validationErrors.Any())
                {
                    e.ErrorText = string.Join("; ", validationErrors);
                    e.Valid = false;
                }
            }
            catch (Exception ex)
            {
                e.ErrorText = $"Error validating data: {ex.Message}";
                e.Valid = false;
            }
        }
        private void ucPeriode_Load(object sender, EventArgs e)
        {
            setahun.Value = DateTime.Now.Year;
            LoadList_Periode();

        }

        private void LoadList_Periode()
        {
            try
            {
                currentTahun = Convert.ToInt32(setahun.Value.ToString());

                using (var dal = new PeriodeDAL())
                {
                    var periodeData = dal.GetPeriodeByTahun(currentTahun);
                    periodeList = new BindingList<PeriodeDTO>(periodeData);

                    gridControl1.DataSource = periodeList;
                   

                    // Configure date edit repositories for date columns
                    ConfigureDateColumns();
                    MakeDateColumnsEditable();
                    gridView1.BestFitColumns();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading periode data: {ex.Message}", "Load Error",
                               MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ConfigureDateColumns()
        {
            // Create DateEdit repository
            var dateEditRepo = new DevExpress.XtraEditors.Repository.RepositoryItemDateEdit();
            dateEditRepo.CalendarTimeProperties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
        new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)
    });
            dateEditRepo.DisplayFormat.FormatString = "dd-MMM-yyyy";
            dateEditRepo.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            dateEditRepo.EditFormat.FormatString = "dd-MMM-yyyy";
            dateEditRepo.EditFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            dateEditRepo.Mask.EditMask = "dd-MMM-yyyy";
            dateEditRepo.AllowNullInput = DevExpress.Utils.DefaultBoolean.True;

            // Create CheckEdit repository for toggle columns (Y/T)
            var toggleSwitchRepo = new DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit();
            toggleSwitchRepo.AutoHeight = false;
            toggleSwitchRepo.ValueChecked = "Y";   // Checked value
            toggleSwitchRepo.ValueUnchecked = "T"; // Unchecked value
            toggleSwitchRepo.NullStyle = DevExpress.XtraEditors.Controls.StyleIndeterminate.Unchecked;
            toggleSwitchRepo.CheckStyle = DevExpress.XtraEditors.Controls.CheckStyles.Style1;
            toggleSwitchRepo.Caption = ""; // No caption next to checkbox

            if (gridControl1.MainView is DevExpress.XtraGrid.Views.Grid.GridView gridView)
            {
                // Configure date columns
                var dateColumnConfigs = new[]
                {
            new { Field = "R1Dari", Caption = "R1 Dari" },
            new { Field = "R1Sampai", Caption = "R1 Sampai" },
            new { Field = "R2Dari", Caption = "R2 Dari" },
            new { Field = "R2Sampai", Caption = "R2 Sampai" },
            new { Field = "BDari", Caption = "B Dari" },
            new { Field = "BSampai", Caption = "B Sampai" }
        };

                foreach (var config in dateColumnConfigs)
                {
                    if (gridView.Columns[config.Field] != null)
                    {
                        var column = gridView.Columns[config.Field];
                        column.ColumnEdit = dateEditRepo;
                        column.Caption = config.Caption;
                        column.DisplayFormat.FormatString = "dd-MMM-yyyy";
                        column.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
                        column.Width = 120;
                        column.MinWidth = 100;
                    }
                }

                // Configure toggle columns
                var toggleColumns = new[] { "Remise1", "Remise2", "Bulanan" };
                foreach (var field in toggleColumns)
                {
                    if (gridView.Columns[field] != null)
                    {
                        var column = gridView.Columns[field];
                        column.ColumnEdit = toggleSwitchRepo;
                        column.Width = 70;
                        column.MinWidth = 60;
                        column.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
                    }
                }

                // Hide display helper columns
                var displayColumns = new[] { "R1DariDisplay", "R1SampaiDisplay", "R2DariDisplay",
                                     "R2SampaiDisplay", "BDariDisplay", "BSampaiDisplay" };
                foreach (var colName in displayColumns)
                {
                    if (gridView.Columns[colName] != null)
                        gridView.Columns[colName].Visible = false;
                }
            }
        }


        private void MakeDateColumnsEditable()
        {
            if (gridControl1.MainView is DevExpress.XtraGrid.Views.Grid.GridView gridView)
            {
                gridView.OptionsBehavior.Editable = true;

                // Make date columns editable
                string[] dateColumns = { "R1Dari", "R1Sampai", "R2Dari", "R2Sampai", "BDari", "BSampai" };
                foreach (var colName in dateColumns)
                {
                    if (gridView.Columns[colName] != null)
                    {
                        gridView.Columns[colName].OptionsColumn.AllowEdit = true;
                        gridView.Columns[colName].OptionsColumn.ReadOnly = false;
                    }
                }

                // Make text columns editable
                string[] textColumns = { "Remise1", "Remise2", "Bulanan" };
                foreach (var colName in textColumns)
                {
                    if (gridView.Columns[colName] != null)
                    {
                        gridView.Columns[colName].OptionsColumn.AllowEdit = true;
                        gridView.Columns[colName].OptionsColumn.ReadOnly = false;
                    }
                }

                // Make read-only columns
                string[] readOnlyColumns = { "Periode", "Bulan" };
                foreach (var colName in readOnlyColumns)
                {
                    if (gridView.Columns[colName] != null)
                    {
                        gridView.Columns[colName].OptionsColumn.AllowEdit = false;
                        gridView.Columns[colName].OptionsColumn.ReadOnly = true;
                    }
                }
            }
        }

    }

    public class PeriodeDTO
    {
        public int Periode { get; set; }
        public string Bulan { get; set; }
        public string Remise1 { get; set; }
        public DateTime? R1Dari { get; set; }
        public DateTime? R1Sampai { get; set; }
        public string Remise2 { get; set; }
        public DateTime? R2Dari { get; set; }
        public DateTime? R2Sampai { get; set; }
        public string Bulanan { get; set; }
        public DateTime? BDari { get; set; }
        public DateTime? BSampai { get; set; }

        // Helper properties for grid binding
        public string R1DariDisplay => R1Dari?.ToString("dd-MMM-yyyy") ?? "";
        public string R1SampaiDisplay => R1Sampai?.ToString("dd-MMM-yyyy") ?? "";
        public string R2DariDisplay => R2Dari?.ToString("dd-MMM-yyyy") ?? "";
        public string R2SampaiDisplay => R2Sampai?.ToString("dd-MMM-yyyy") ?? "";
        public string BDariDisplay => BDari?.ToString("dd-MMM-yyyy") ?? "";
        public string BSampaiDisplay => BSampai?.ToString("dd-MMM-yyyy") ?? "";

        // Constructor
        public PeriodeDTO() { }

        public PeriodeDTO(int periode, string bulan)
        {
            Periode = periode;
            Bulan = bulan;
        }

        // Validation method
        public List<string> Validate()
        {
            var errors = new List<string>();

            if (R1Dari.HasValue && R1Sampai.HasValue && R1Dari > R1Sampai)
                errors.Add("R1 Dari tidak boleh lebih besar dari R1 Sampai");

            if (R2Dari.HasValue && R2Sampai.HasValue && R2Dari > R2Sampai)
                errors.Add("R2 Dari tidak boleh lebih besar dari R2 Sampai");

            if (BDari.HasValue && BSampai.HasValue && BDari > BSampai)
                errors.Add("B Dari tidak boleh lebih besar dari B Sampai");

            return errors;
        }
    }
    // Data Access Layer for Periode
    public class PeriodeDAL : IDisposable
    {
        private OracleConnection connection;
        private bool disposed = false;

        public PeriodeDAL()
        {
            connection = new OracleConnection(global.connectionString);
            try
            {
                connection.Open();

                // Set Oracle session parameters for consistent date handling
                using (var formatCmd = new OracleCommand("ALTER SESSION SET NLS_DATE_FORMAT = 'DD-MON-YYYY'", connection))
                    formatCmd.ExecuteNonQuery();

                using var langCmd = new OracleCommand("ALTER SESSION SET NLS_DATE_LANGUAGE = 'ENGLISH'", connection);
                langCmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                connection?.Dispose();
                System.Diagnostics.Debug.WriteLine($"Warning: Could not initialize PeriodeDAL: {ex.Message}");
                throw;
            }
        }

        public List<PeriodeDTO> GetPeriodeByTahun(int tahun)
        {
            var periodeList = new List<PeriodeDTO>();

            // Ensure all 12 months exist
            EnsurePeriodesExist(tahun);

            string query = @"
            SELECT 
                PERIODE,
                BULAN,
                REMISE1,
                TO_DATE(R1DARI, 'DD-MON-YYYY', 'NLS_DATE_LANGUAGE = ENGLISH') AS R1DARI,
                TO_DATE(R1SAMPAI, 'DD-MON-YYYY', 'NLS_DATE_LANGUAGE = ENGLISH') AS R1SAMPAI,
                REMISE2,
                TO_DATE(R2DARI, 'DD-MON-YYYY', 'NLS_DATE_LANGUAGE = ENGLISH') AS R2DARI,
                TO_DATE(R2SAMPAI, 'DD-MON-YYYY', 'NLS_DATE_LANGUAGE = ENGLISH') AS R2SAMPAI,
                BULANAN,
                TO_DATE(BDARI, 'DD-MON-YYYY', 'NLS_DATE_LANGUAGE = ENGLISH') AS BDARI,
                TO_DATE(BSAMPAI, 'DD-MON-YYYY', 'NLS_DATE_LANGUAGE = ENGLISH') AS BSAMPAI
            FROM POS_PERIODE 
            WHERE TAHUN = :tahun 
            ORDER BY PERIODE";


            using (var cmd = new OracleCommand(query, connection))
            {
                cmd.Parameters.Add(":tahun", OracleDbType.Int32).Value = tahun;

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var periode = new PeriodeDTO
                        {
                            Periode = reader.GetInt32("PERIODE"),
                            Bulan = reader.IsDBNull("BULAN") ? "" : reader.GetString("BULAN"),
                            Remise1 = reader.IsDBNull("REMISE1") ? "" : reader.GetString("REMISE1"),
                            R1Dari = reader.IsDBNull("R1DARI") ? null : reader.GetDateTime("R1DARI"),
                            R1Sampai = reader.IsDBNull("R1SAMPAI") ? null : reader.GetDateTime("R1SAMPAI"),
                            Remise2 = reader.IsDBNull("REMISE2") ? "" : reader.GetString("REMISE2"),
                            R2Dari = reader.IsDBNull("R2DARI") ? null : reader.GetDateTime("R2DARI"),
                            R2Sampai = reader.IsDBNull("R2SAMPAI") ? null : reader.GetDateTime("R2SAMPAI"),
                            Bulanan = reader.IsDBNull("BULANAN") ? "" : reader.GetString("BULANAN"),
                            BDari = reader.IsDBNull("BDARI") ? null : reader.GetDateTime("BDARI"),
                            BSampai = reader.IsDBNull("BSAMPAI") ? null : reader.GetDateTime("BSAMPAI")
                        };
                        periodeList.Add(periode);
                    }
                }
            }

            return periodeList;
        }

        public bool UpdatePeriode(PeriodeDTO periode)
        {
            // Validate before update
            var validationErrors = periode.Validate();
            if (validationErrors.Any())
                throw new ValidationException(string.Join("; ", validationErrors));

            string updateQuery = @"
                UPDATE POS_PERIODE SET 
                    REMISE1 = :remise1,
                    R1DARI = :r1dari,
                    R1SAMPAI = :r1sampai,
                    REMISE2 = :remise2,
                    R2DARI = :r2dari,
                    R2SAMPAI = :r2sampai,
                    BULANAN = :bulanan,
                    BDARI = :bdari,
                    BSAMPAI = :bsampai
                WHERE PERIODE = :periode";

            using (var cmd = new OracleCommand(updateQuery, connection))
            {
                cmd.Parameters.Add(":remise1", OracleDbType.Varchar2).Value = string.IsNullOrEmpty(periode.Remise1) ? DBNull.Value : periode.Remise1;
                cmd.Parameters.Add(":r1dari", OracleDbType.Date).Value = periode.R1Dari ?? (object)DBNull.Value;
                cmd.Parameters.Add(":r1sampai", OracleDbType.Date).Value = periode.R1Sampai ?? (object)DBNull.Value;
                cmd.Parameters.Add(":remise2", OracleDbType.Varchar2).Value = string.IsNullOrEmpty(periode.Remise2) ? DBNull.Value : periode.Remise2;
                cmd.Parameters.Add(":r2dari", OracleDbType.Date).Value = periode.R2Dari ?? (object)DBNull.Value;
                cmd.Parameters.Add(":r2sampai", OracleDbType.Date).Value = periode.R2Sampai ?? (object)DBNull.Value;
                cmd.Parameters.Add(":bulanan", OracleDbType.Varchar2).Value = string.IsNullOrEmpty(periode.Bulanan) ? DBNull.Value : periode.Bulanan;
                cmd.Parameters.Add(":bdari", OracleDbType.Date).Value = periode.BDari ?? (object)DBNull.Value;
                cmd.Parameters.Add(":bsampai", OracleDbType.Date).Value = periode.BSampai ?? (object)DBNull.Value;
                cmd.Parameters.Add(":periode", OracleDbType.Int32).Value = periode.Periode;

                return cmd.ExecuteNonQuery() > 0;
            }
        }

        private void EnsurePeriodesExist(int tahun)
        {
            string[] bulanIndonesia = {
                "Januari", "Februari", "Maret", "April", "Mei", "Juni",
                "Juli", "Agustus", "September", "Oktober", "November", "Desember"
            };

            // Check existing periods
            var existingPeriods = new HashSet<int>();
            using (var checkCmd = new OracleCommand("SELECT PERIODE FROM POS_PERIODE WHERE TAHUN = :tahun", connection))
            {
                checkCmd.Parameters.Add(":tahun", OracleDbType.Int32).Value = tahun;
                using var reader = checkCmd.ExecuteReader();
                while (reader.Read())
                    existingPeriods.Add(reader.GetInt32(0));
            }

            // Insert missing periods
            string insertQuery = @"
                INSERT INTO POS_PERIODE (
                    TAHUN, PERIODE, BULAN,
                    REMISE1, R1DARI, R1SAMPAI,
                    REMISE2, R2DARI, R2SAMPAI,
                    BULANAN, BDARI, BSAMPAI
                ) VALUES (
                    :tahun, :periode, :bulan,
                    NULL, NULL, NULL,
                    NULL, NULL, NULL,
                    NULL, NULL, NULL
                )";

            using (var insertCmd = new OracleCommand(insertQuery, connection))
            {
                for (int bulan = 1; bulan <= 12; bulan++)
                {
                    int periode = (tahun * 100) + bulan;
                    if (existingPeriods.Contains(periode))
                        continue;

                    insertCmd.Parameters.Clear();
                    insertCmd.Parameters.Add(":tahun", OracleDbType.Int32).Value = tahun;
                    insertCmd.Parameters.Add(":periode", OracleDbType.Int32).Value = periode;
                    insertCmd.Parameters.Add(":bulan", OracleDbType.Varchar2).Value = bulanIndonesia[bulan - 1];

                    insertCmd.ExecuteNonQuery();
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed && disposing)
            {
                connection?.Close();
                connection?.Dispose();
                disposed = true;
            }
        }
    }
    public class ValidationException : Exception
    {
        public ValidationException(string message) : base(message) { }
    }
}
