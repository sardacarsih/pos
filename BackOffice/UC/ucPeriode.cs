using Oracle.ManagedDataAccess.Client;
using Serilog;
using System.ComponentModel;
using System.Data;
using System.Text;
using BackOffice.DataLayer;
using BackOffice.UI;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Grid;
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
            ConfigurePageLayout();
            gridView1.CellValueChanged += GridView1_CellValueChanged;
            gridView1.ValidateRow += GridView1_ValidateRow;
            gridView1.ShowingEditor += GridView1_ShowingEditor;
            setahun.EditValueChanged += Setahun_EditValueChanged;
        }

        private void ConfigurePageLayout()
        {
            BackColor = BackOfficeTheme.Canvas;
            Padding = new Padding(0);

            sidePanel1.Height = 92;
            sidePanel1.Padding = new Padding(24, 12, 24, 12);
            sidePanel1.Appearance.BackColor = BackOfficeTheme.Surface;
            sidePanel1.Appearance.Options.UseBackColor = true;

            var title = new LabelControl
            {
                Text = "Pengaturan Periode",
                Tag = "ui-title",
                AutoSizeMode = LabelAutoSizeMode.None,
                Location = new Point(24, 13),
                Size = new Size(360, 28)
            };
            title.Appearance.Font = new Font("Segoe UI Semibold", 15F);
            title.Appearance.ForeColor = BackOfficeTheme.TextPrimary;
            title.Appearance.Options.UseFont = true;
            title.Appearance.Options.UseForeColor = true;

            var description = new LabelControl
            {
                Text = "Atur jadwal remise dan periode bulanan untuk setiap bulan.",
                AutoSizeMode = LabelAutoSizeMode.None,
                Location = new Point(25, 45),
                Size = new Size(520, 22)
            };
            description.Appearance.Font = new Font("Segoe UI", 9F);
            description.Appearance.ForeColor = BackOfficeTheme.TextMuted;
            description.Appearance.Options.UseFont = true;
            description.Appearance.Options.UseForeColor = true;

            var yearLabel = new LabelControl
            {
                Text = "Tahun",
                AutoSizeMode = LabelAutoSizeMode.None,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Size = new Size(50, 24)
            };
            yearLabel.Appearance.Font = new Font("Segoe UI Semibold", 9F);
            yearLabel.Appearance.ForeColor = BackOfficeTheme.TextMuted;
            yearLabel.Appearance.Options.UseFont = true;
            yearLabel.Appearance.Options.UseForeColor = true;

            setahun.Parent = sidePanel1;
            setahun.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            setahun.Size = new Size(120, 32);
            setahun.Properties.MinValue = 2000;
            setahun.Properties.MaxValue = 2100;
            setahun.Properties.IsFloatValue = false;
            setahun.Properties.MaskSettings.Set("mask", "d");
            setahun.Properties.Appearance.Font = new Font("Segoe UI Semibold", 10F);
            setahun.Properties.Appearance.Options.UseFont = true;

            sidePanel1.Controls.Add(title);
            sidePanel1.Controls.Add(description);
            sidePanel1.Controls.Add(yearLabel);

            void AlignYearFilter()
            {
                setahun.Location = new Point(
                    Math.Max(24, sidePanel1.ClientSize.Width - setahun.Width - 24),
                    34);
                yearLabel.Location = new Point(setahun.Left, 12);
            }

            sidePanel1.Resize += (_, _) => AlignYearFilter();
            AlignYearFilter();

            sidePanel2.Padding = new Padding(16);
            sidePanel2.Appearance.BackColor = BackOfficeTheme.Canvas;
            sidePanel2.Appearance.Options.UseBackColor = true;
            gridControl1.Dock = DockStyle.Fill;

            BackOfficeTheme.StyleGrid(gridView1);
            gridView1.OptionsView.ColumnAutoWidth = true;
            gridView1.OptionsView.ShowAutoFilterRow = false;
            gridView1.OptionsView.ShowFooter = false;
            gridView1.OptionsBehavior.EditorShowMode =
                DevExpress.Utils.EditorShowMode.MouseDownFocused;
            gridView1.OptionsSelection.EnableAppearanceFocusedCell = true;
            gridView1.FocusRectStyle = DrawFocusRectStyle.RowFocus;
            gridView1.RowHeight = 38;
            gridView1.ColumnPanelRowHeight = 42;
        }

        private void Setahun_EditValueChanged(object? sender, EventArgs e)
        {
            LoadList_Periode();
        }

        private void GridView1_CellValueChanged(object sender, CellValueChangedEventArgs e)
        {
            try
            {
                if (sender is not GridView view || view.GetRow(e.RowHandle) is not PeriodeDTO periode)
                {
                    return;
                }
                string kolom = e.Column.FieldName.ToUpperInvariant();
                if (!BisaUbahTanggal(periode, kolom))
                {
                    LoadList_Periode();
                    return;
                }

                DateTime? tanggal = ConvertToNullableDateTime(e.Value);
                UpdatePeriodeDTO(periode, kolom, tanggal);
                var validationErrors = periode.Validate();
                if (validationErrors.Count > 0)
                {
                    MessageBox.Show(string.Join("\n", validationErrors), "Tanggal periode tidak valid",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    LoadList_Periode();
                    return;
                }

                using var dal = new PeriodeDAL();
                DateTime bulan = new(periode.Periode / 100, periode.Periode % 100, 1);
                PeriodeDTO? Tetangga(int offset)
                {
                    DateTime target = bulan.AddMonths(offset);
                    return dal.GetPeriode(target.Year * 100 + target.Month);
                }

                var peringatan = PeriodeDTO.PeriksaKesinambungan(periode, Tetangga(-1), Tetangga(1));
                if (peringatan.Count > 0 &&
                    MessageBox.Show(string.Join("\n", peringatan) + "\n\nTetap simpan?",
                        "Jadwal periode tidak tersambung", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                {
                    LoadList_Periode();
                    return;
                }

                if (!dal.UpdateTanggal(periode.Periode, kolom, tanggal))
                {
                    MessageBox.Show("Tanggal tidak disimpan. Periksa apakah periode sudah ditutup atau rentang tanggal berubah.",
                        "Setting Periode", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    LoadList_Periode();
                    return;
                }

                Log.Information("Tanggal {Kolom} periode {Periode} diubah menjadi {Tanggal} oleh {User}",
                    kolom, periode.Periode, tanggal, LoginInfo.userID);
                view.RefreshRow(e.RowHandle);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating data: {ex.Message}", "Update Error",
                               MessageBoxButtons.OK, MessageBoxIcon.Error);

                // Refresh data to show original values
                LoadList_Periode();
            }
        }

        private void GridView1_ShowingEditor(object? sender, CancelEventArgs e)
        {
            if (sender is GridView view && view.FocusedColumn != null && view.GetFocusedRow() is PeriodeDTO periode)
            {
                e.Cancel = !BisaUbahTanggal(periode, view.FocusedColumn.FieldName.ToUpperInvariant());
            }
        }

        private static bool BisaUbahTanggal(PeriodeDTO periode, string kolom)
        {
            string? status = kolom switch
            {
                "R1DARI" or "R1SAMPAI" => periode.Remise1 ?? "",
                "R2DARI" or "R2SAMPAI" => periode.Remise2 ?? "",
                "BDARI" or "BSAMPAI" => periode.Bulanan ?? "",
                _ => null
            };
            return status != null && !string.Equals(status.Trim(), "Y", StringComparison.OrdinalIgnoreCase);
        }

        private static void UpdatePeriodeDTO(PeriodeDTO periode, string kolom, DateTime? tanggal)
        {
            switch (kolom)
            {
                case "R1DARI":
                    periode.R1Dari = tanggal;
                    break;
                case "R1SAMPAI":
                    periode.R1Sampai = tanggal;
                    break;
                case "R2DARI":
                    periode.R2Dari = tanggal;
                    break;
                case "R2SAMPAI":
                    periode.R2Sampai = tanggal;
                    break;
                case "BDARI":
                    periode.BDari = tanggal;
                    break;
                case "BSAMPAI":
                    periode.BSampai = tanggal;
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
                    ConfigureGridColumns();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading periode data: {ex.Message}", "Load Error",
                               MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ConfigureGridColumns()
        {
            gridView1.OptionsView.ColumnAutoWidth = true;

            ConfigureColumn("Periode", "Periode", 90, 0);
            ConfigureColumn("Bulan", "Bulan", 120, 1);
            ConfigureColumn("Remise1", "Remise 1", 82, 2, true);
            ConfigureColumn("R1Dari", "Mulai R1", 135, 3);
            ConfigureColumn("R1Sampai", "Selesai R1", 135, 4);
            ConfigureColumn("Remise2", "Remise 2", 82, 5, true);
            ConfigureColumn("R2Dari", "Mulai R2", 135, 6);
            ConfigureColumn("R2Sampai", "Selesai R2", 135, 7);
            ConfigureColumn("Bulanan", "Bulanan", 82, 8, true);
            ConfigureColumn("BDari", "Mulai Bulanan", 145, 9);
            ConfigureColumn("BSampai", "Selesai Bulanan", 145, 10);
        }

        private void ConfigureColumn(
            string fieldName,
            string caption,
            int width,
            int visibleIndex,
            bool center = false)
        {
            var column = gridView1.Columns[fieldName];
            if (column is null)
            {
                return;
            }

            column.Caption = caption;
            column.Width = width;
            column.MinWidth = Math.Min(width, 72);
            column.VisibleIndex = visibleIndex;
            column.AppearanceHeader.TextOptions.HAlignment =
                center ? DevExpress.Utils.HorzAlignment.Center : DevExpress.Utils.HorzAlignment.Near;
            column.AppearanceCell.TextOptions.HAlignment =
                center ? DevExpress.Utils.HorzAlignment.Center : DevExpress.Utils.HorzAlignment.Near;
        }

        // Glyph tegas: kotak hijau + centang putih (aktif) vs kotak abu kosong (nonaktif).
        private static System.Windows.Forms.ImageList BuildCheckImages()
        {
            var list = new System.Windows.Forms.ImageList { ImageSize = new System.Drawing.Size(20, 20), ColorDepth = System.Windows.Forms.ColorDepth.Depth32Bit };
            for (int i = 0; i < 2; i++)
            {
                var bmp = new System.Drawing.Bitmap(20, 20);
                using (var g = System.Drawing.Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    var box = new System.Drawing.Rectangle(1, 1, 17, 17);
                    if (i == 1)
                    {
                        using var fill = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(34, 139, 34));
                        g.FillRectangle(fill, box);
                        using var tick = new System.Drawing.Pen(System.Drawing.Color.White, 2.6f)
                        { StartCap = System.Drawing.Drawing2D.LineCap.Round, EndCap = System.Drawing.Drawing2D.LineCap.Round, LineJoin = System.Drawing.Drawing2D.LineJoin.Round };
                        g.DrawLines(tick, new[] { new System.Drawing.Point(5, 10), new System.Drawing.Point(8, 13), new System.Drawing.Point(14, 6) });
                    }
                    else
                    {
                        using var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(150, 150, 150), 1.5f);
                        g.FillRectangle(System.Drawing.Brushes.White, box);
                        g.DrawRectangle(pen, box);
                    }
                }
                list.Images.Add(bmp);
            }
            return list;
        }

        private void ConfigureDateColumns()
        {
            // Create DateEdit repository
            var dateEditRepo = new DevExpress.XtraEditors.Repository.RepositoryItemDateEdit();
            dateEditRepo.CalendarTimeProperties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
        new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)
    });
            dateEditRepo.DisplayFormat.FormatString = "dd MMM yyyy";
            dateEditRepo.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            dateEditRepo.EditFormat.FormatString = "dd MMM yyyy";
            dateEditRepo.EditFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            dateEditRepo.Mask.EditMask = "dd MMM yyyy";
            dateEditRepo.AllowNullInput = DevExpress.Utils.DefaultBoolean.True;

            // Create CheckEdit repository for toggle columns (Y/T)
            var toggleSwitchRepo = new DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit();
            toggleSwitchRepo.AutoHeight = false;
            toggleSwitchRepo.ValueChecked = "Y";   // Checked value
            toggleSwitchRepo.ValueUnchecked = "T"; // Unchecked value
            toggleSwitchRepo.NullStyle = DevExpress.XtraEditors.Controls.StyleIndeterminate.Unchecked;
            toggleSwitchRepo.CheckStyle = DevExpress.XtraEditors.Controls.CheckStyles.UserDefined;
            toggleSwitchRepo.GlyphAlignment = DevExpress.Utils.HorzAlignment.Center;
            toggleSwitchRepo.Images = BuildCheckImages();
            toggleSwitchRepo.ImageIndexUnchecked = 0;
            toggleSwitchRepo.ImageIndexChecked = 1;
            toggleSwitchRepo.ImageIndexGrayed = 0;
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
                        column.DisplayFormat.FormatString = "dd MMM yyyy";
                        column.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
                        column.Width = 135;
                        column.MinWidth = 110;
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

        public static List<string> PeriksaKesinambungan(PeriodeDTO cur, PeriodeDTO? prev, PeriodeDTO? next)
        {
            var errors = new List<string>();

            if (cur.R1Sampai.HasValue && cur.R2Dari.HasValue && cur.R2Dari.Value.Date != cur.R1Sampai.Value.Date.AddDays(1))
                errors.Add($"Periode {cur.Periode}: R2 Dari seharusnya {cur.R1Sampai.Value.AddDays(1):dd MMM yyyy} (sehari setelah R1 Sampai)");

            void Sambung(DateTime? akhirSebelum, DateTime? awalSesudah, string label, int pSebelum, int pSesudah)
            {
                if (akhirSebelum.HasValue && awalSesudah.HasValue && awalSesudah.Value.Date != akhirSebelum.Value.Date.AddDays(1))
                    errors.Add($"{label}: periode {pSesudah} seharusnya mulai {akhirSebelum.Value.AddDays(1):dd MMM yyyy} " +
                               $"(sehari setelah akhir periode {pSebelum})");
            }

            if (prev != null)
            {
                Sambung(prev.R2Sampai, cur.R1Dari, "Remise", prev.Periode, cur.Periode);
                Sambung(prev.BSampai, cur.BDari, "Bulanan", prev.Periode, cur.Periode);
            }
            if (next != null)
            {
                Sambung(cur.R2Sampai, next.R1Dari, "Remise", cur.Periode, next.Periode);
                Sambung(cur.BSampai, next.BDari, "Bulanan", cur.Periode, next.Periode);
            }
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
                Log.Warning(ex, "Could not initialize PeriodeDAL");
                throw;
            }
        }

        private const string SelectPeriodeSql = @"
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
            FROM POS_PERIODE ";

        private static PeriodeDTO MapPeriode(OracleDataReader reader) => new PeriodeDTO
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

        public List<PeriodeDTO> GetPeriodeByTahun(int tahun)
        {
            var periodeList = new List<PeriodeDTO>();

            // Ensure all 12 months exist
            EnsurePeriodesExist(tahun);

            using (var cmd = new OracleCommand(SelectPeriodeSql + "WHERE TAHUN = :tahun ORDER BY PERIODE", connection))
            {
                cmd.Parameters.Add(":tahun", OracleDbType.Int32).Value = tahun;

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                    periodeList.Add(MapPeriode(reader));
            }

            return periodeList;
        }

        /// <summary>Single period row, or null when it does not exist (no auto-create).</summary>
        public PeriodeDTO? GetPeriode(int periode)
        {
            using var cmd = new OracleCommand(SelectPeriodeSql + "WHERE PERIODE = :periode", connection);
            cmd.Parameters.Add(":periode", OracleDbType.Int32).Value = periode;
            using var reader = cmd.ExecuteReader();
            return reader.Read() ? MapPeriode(reader) : null;
        }

        public bool UpdateTanggal(int periode, string kolom, DateTime? tanggal)
        {
            var (flag, pasangan, pembanding) = kolom switch
            {
                "R1DARI" => ("REMISE1", "R1SAMPAI", "<="),
                "R1SAMPAI" => ("REMISE1", "R1DARI", ">="),
                "R2DARI" => ("REMISE2", "R2SAMPAI", "<="),
                "R2SAMPAI" => ("REMISE2", "R2DARI", ">="),
                "BDARI" => ("BULANAN", "BSAMPAI", "<="),
                "BSAMPAI" => ("BULANAN", "BDARI", ">="),
                _ => throw new ArgumentOutOfRangeException(nameof(kolom))
            };

            // Penutupan berjalan di aplikasi lain. Periksa flag dan tanggal pasangannya di
            // database saat UPDATE agar tampilan yang sudah lama terbuka tidak menimpa periode.
            string sql = $"""
                UPDATE POS_PERIODE
                SET {kolom} = TO_CHAR(:tanggal, 'DD-MON-YYYY', 'NLS_DATE_LANGUAGE=ENGLISH')
                WHERE PERIODE = :periode AND NVL(UPPER(TRIM({flag})), 'T') <> 'Y'
                  AND (:tanggal IS NULL OR {pasangan} IS NULL OR :tanggal {pembanding}
                       TO_DATE({pasangan}, 'DD-MON-YYYY', 'NLS_DATE_LANGUAGE=ENGLISH'))
                """;
            using var cmd = new OracleCommand(sql, connection) { BindByName = true };
            cmd.Parameters.Add("tanggal", OracleDbType.Date).Value = (object?)tanggal?.Date ?? DBNull.Value;
            cmd.Parameters.Add("periode", OracleDbType.Int32).Value = periode;
            return cmd.ExecuteNonQuery() == 1;
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
