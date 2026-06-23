using BackOffice.Controller;
using BackOffice.DataLayer;
using BackOffice.Model;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Grid;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.IO;
using System.Windows.Forms;
using BackOffice.UI;
using DevExpress.XtraEditors.Controls;

namespace BackOffice.UC
{
    public partial class ucMasterAnggota : UserControl
    {
        AnggotaController controller = new();
        MasterDataController controller2 = new();
        List<DTOAnggota> anggota;

        //Using singleton pattern to create an instance to ucModule3
        private static ucMasterAnggota _instance;
        public static ucMasterAnggota Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ucMasterAnggota();
                return _instance;
            }
        }
        public ucMasterAnggota()
        {
            InitializeComponent();
            BuildModernLayout();
            ConfigureModernControls();
        }

        private void BuildModernLayout()
        {
            SuspendLayout();
            sidePanel1.SuspendLayout();

            sidePanel1.Controls.Clear();
            sidePanel1.Height = 324;
            sidePanel1.MinimumSize = new Size(0, 300);
            sidePanel1.Padding = new Padding(18, 16, 18, 14);
            sidePanel1.Appearance.BackColor = BackOfficeTheme.Canvas;
            sidePanel1.Appearance.Options.UseBackColor = true;

            var pageHeader = new LabelControl
            {
                Text = "Data Anggota & Pelanggan",
                Dock = DockStyle.Top,
                AutoSizeMode = LabelAutoSizeMode.None,
                Height = 34
            };
            pageHeader.Appearance.Font = new Font("Segoe UI Semibold", 16F);
            pageHeader.Appearance.ForeColor = BackOfficeTheme.TextPrimary;
            pageHeader.Appearance.Options.UseFont = true;
            pageHeader.Appearance.Options.UseForeColor = true;

            var content = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                BackColor = BackOfficeTheme.Canvas,
                Padding = new Padding(0, 8, 0, 0)
            };
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 57F));
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18F));

            PanelControl formCard = CreateCard();
            PanelControl filterCard = CreateCard();
            PanelControl photoCard = CreateCard();
            photoCard.Margin = Padding.Empty;
            content.Controls.Add(formCard, 0, 0);
            content.Controls.Add(filterCard, 1, 0);
            content.Controls.Add(photoCard, 2, 0);

            BuildFormCard(formCard);
            BuildFilterCard(filterCard);
            BuildPhotoCard(photoCard);

            sidePanel1.Controls.Add(content);
            sidePanel1.Controls.Add(pageHeader);
            sidePanel1.ResumeLayout(true);
            ResumeLayout(true);
        }

        private static PanelControl CreateCard()
        {
            return new PanelControl
            {
                Tag = "ui-card",
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 12, 0),
                Padding = new Padding(16, 14, 16, 14),
                BorderStyle = BorderStyles.Simple,
                Appearance =
                {
                    BackColor = Color.White,
                    Options = { UseBackColor = true }
                }
            };
        }

        private void BuildFormCard(PanelControl card)
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 5,
                Padding = Padding.Empty,
                AutoScroll = true
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 105));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 105));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            AddField(layout, labelControl1, textEdit_nik, 0, 0, "NIK");
            AddField(layout, labelControl2, textEdit_nama, 2, 0, "Nama");
            AddField(layout, labelControl4, dateEdit_tma, 0, 1, "Tanggal Masuk");
            AddField(layout, labelControl5, comboBoxEdit_status, 2, 1, "Status Kerja");
            AddField(layout, labelControl3, lookUpEdit1, 0, 2, "Unit Kerja");
            AddField(layout, labelControl6, txtlimithutang, 2, 2, "Limit Hutang");

            checkEditnonaktif.Properties.Caption = "Aktif";
            checkEditnonanggota.Properties.Caption = "Anggota";
            checkEditnonaktif.Checked = true;
            checkEditnonanggota.Checked = true;
            checkEditnonaktif.Dock = DockStyle.Fill;
            checkEditnonanggota.Dock = DockStyle.Fill;
            layout.Controls.Add(checkEditnonaktif, 1, 3);
            layout.Controls.Add(checkEditnonanggota, 3, 3);

            var actions = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(0, 8, 0, 0)
            };
            sbaddorupdate.Width = 120;
            sbbatal.Width = 100;
            actions.Controls.Add(sbbatal);
            actions.Controls.Add(sbaddorupdate);
            layout.Controls.Add(actions, 0, 4);
            layout.SetColumnSpan(actions, 4);
            card.Controls.Add(layout);
        }

        private void BuildFilterCard(PanelControl card)
        {
            var title = new LabelControl
            {
                Text = "Tampilkan Data",
                Dock = DockStyle.Top,
                Height = 28
            };
            title.Appearance.Font = new Font("Segoe UI Semibold", 10F);
            title.Appearance.ForeColor = BackOfficeTheme.BrandBlueDark;
            title.Appearance.Options.UseFont = true;
            title.Appearance.Options.UseForeColor = true;

            radioGroup1.Properties.Items.Clear();
            radioGroup1.Properties.Items.Add(new RadioGroupItem(null, "Anggota"));
            radioGroup1.Properties.Items.Add(new RadioGroupItem(null, "Pelanggan"));
            radioGroup1.Properties.Items.Add(new RadioGroupItem(null, "Semua Aktif"));
            radioGroup1.Properties.Columns = 1;
            radioGroup1.Dock = DockStyle.Fill;
            radioGroup1.SelectedIndex = 0;
            card.Controls.Add(radioGroup1);
            card.Controls.Add(title);
        }

        private void BuildPhotoCard(PanelControl card)
        {
            var hint = new LabelControl
            {
                Text = "Klik foto untuk mengganti",
                Dock = DockStyle.Bottom,
                AutoSizeMode = LabelAutoSizeMode.None,
                Height = 28
            };
            hint.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            hint.Appearance.ForeColor = BackOfficeTheme.TextMuted;
            hint.Appearance.Options.UseForeColor = true;

            pictureEdit1.Dock = DockStyle.Fill;
            pictureEdit1.Properties.SizeMode = PictureSizeMode.Squeeze;
            pictureEdit1.Properties.ShowMenu = false;
            pictureEdit1.Cursor = Cursors.Hand;
            card.Controls.Add(pictureEdit1);
            card.Controls.Add(hint);
        }

        private static void AddField(
            TableLayoutPanel layout,
            LabelControl label,
            BaseEdit editor,
            int column,
            int row,
            string caption)
        {
            label.Text = caption;
            label.Dock = DockStyle.Fill;
            label.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
            editor.Dock = DockStyle.Fill;
            editor.Margin = new Padding(0, 4, 12, 4);
            layout.Controls.Add(label, column, row);
            layout.Controls.Add(editor, column + 1, row);
        }

        private void ConfigureModernControls()
        {
            BackOfficeTheme.StylePrimaryButton(sbaddorupdate);
            BackOfficeTheme.StyleSecondaryButton(sbbatal);
            BackOfficeTheme.StyleGrid(gridView1);

            sbaddorupdate.Text = "Simpan";
            sbbatal.Text = "Batal";
            gridView1.OptionsFind.FindNullPrompt = "Cari NIK, nama, status, atau unit kerja...";
            gridView1.OptionsFind.AlwaysVisible = true;
            gridView1.OptionsFind.FindDelay = 350;
            gridView1.OptionsView.ShowFilterPanelMode = DevExpress.XtraGrid.Views.Base.ShowFilterPanelMode.Never;
            gridView1.OptionsView.ShowGroupPanel = false;
            gridView1.OptionsBehavior.Editable = false;
            gridView1.OptionsSelection.EnableAppearanceFocusedCell = false;

            textEdit_nama.Properties.NullValuePrompt = "Nama lengkap";
            lookUpEdit1.Properties.NullValuePrompt = "Pilih unit kerja";
            comboBoxEdit_status.Properties.NullValuePrompt = "Pilih status kerja";
            comboBoxEdit_status.Properties.TextEditStyle = TextEditStyles.DisableTextEditor;
            txtlimithutang.Properties.MaskSettings.Set("MaskManagerType", typeof(DevExpress.Data.Mask.NumericMaskManager));
            txtlimithutang.Properties.MaskSettings.Set("mask", "n0");
            txtlimithutang.Properties.UseMaskAsDisplayFormat = true;
            txtlimithutang.EditValue = 0m;
        }

        private void dateEdit1_EditValueChanged(object sender, EventArgs e)
        {

        }

        private void labelControl3_Click(object sender, EventArgs e)
        {

        }

        private void ucMasterAnggota_Load(object sender, EventArgs e)
        {
            anggota= controller.GetAnggotaData();
            gridControl1.DataSource = anggota;
            gridView1.Columns["UNIT_KERJA"].GroupIndex=1;
            gridView1.Columns["KODE_UNIT"].Visible = false;
            ConfigureGridColumns();

            lookUpEdit1.Properties.DataSource = controller2.GetUnitKerja();
            lookUpEdit1.Properties.ValueMember = "KODE";
            lookUpEdit1.Properties.DisplayMember = "NAMA";

            comboBoxEdit_status.Properties.Items.Add("BULANAN");
            comboBoxEdit_status.Properties.Items.Add("KHT");
            textEdit_nik.Text = controller.GenerateNikAnggota(DateTime.Today);

        }

        private void ConfigureGridColumns()
        {
            SetColumn("NIK", "NIK", 110);
            SetColumn("NAMA_PELANGGAN", "Nama", 230);
            SetColumn("STATUS", "Status Kerja", 110);
            SetColumn("TMK", "Tanggal Masuk", 120);
            SetColumn("UNIT_KERJA", "Unit Kerja", 220);
            SetColumn("LIMIT_HUTANG", "Limit Hutang", 140);
            SetColumn("AKTIF", "Aktif", 80);
            SetColumn("ANGGOTA", "Anggota", 90);
            SetColumn("GAMBAR", "Foto", 80);

            if (gridView1.Columns["TMK"] != null)
            {
                gridView1.Columns["TMK"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
                gridView1.Columns["TMK"].DisplayFormat.FormatString = "dd MMM yyyy";
            }
            if (gridView1.Columns["LIMIT_HUTANG"] != null)
            {
                gridView1.Columns["LIMIT_HUTANG"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
                gridView1.Columns["LIMIT_HUTANG"].DisplayFormat.FormatString = "n0";
            }
        }

        private void SetColumn(string fieldName, string caption, int width)
        {
            if (gridView1.Columns[fieldName] is { } column)
            {
                column.Caption = caption;
                column.Width = width;
            }
        }
        private void gridView1_DoubleClick(object sender, EventArgs e)
        {
            Get_Product_Item();
            sbaddorupdate.Text = "Update";
        }
    

        private void Get_Product_Item()
        {
            if (gridView1.SelectedRowsCount > 0)
            {
               // int selectedIndex = gridView1.GetSelectedRows()[0];

                // Get the focused row handle, which may be different from the selected row in grouped view
                int focusedHandle = gridView1.FocusedRowHandle;

                // If the focused row handle is not a data row (e.g., a group row), find the first visible data row
                if (!gridView1.IsDataRow(focusedHandle))
                {
                    for (int i = 0; i < gridView1.RowCount; i++)
                    {
                        if (gridView1.IsDataRow(i))
                        {
                            focusedHandle = i;
                            break;
                        }
                    }
                }

                // Now, retrieve the DTOAnggota instance for the focused row
                DTOAnggota? selectedItem = gridView1.GetRow(focusedHandle) as DTOAnggota;

                // Rest of your code remains the same...
                if (selectedItem == null)
                {
                    return;
                }

                textEdit_nik.Text = selectedItem.NIK;
                textEdit_nama.Text = selectedItem.NAMA_PELANGGAN;
                dateEdit_tma.Text = selectedItem.TMK.ToString();
                lookUpEdit1.EditValue = selectedItem.KODE_UNIT;
                comboBoxEdit_status.Text = selectedItem.STATUS;
                txtlimithutang.Text= selectedItem.LIMIT_HUTANG.ToString();
                var nonaktif = selectedItem.AKTIF;
                var nonanggota = selectedItem.ANGGOTA;
                checkEditnonaktif.Checked = nonaktif == "Y";
                checkEditnonanggota.Checked = nonanggota == "Y";
                // Handle image loading
                if (selectedItem.GAMBAR != null)
                {
                    using (MemoryStream memoryStream = new MemoryStream(selectedItem.GAMBAR))
                    {
                        try
                        {
                            Image selectedImage = Image.FromStream(memoryStream);
                            pictureEdit1.Image = selectedImage;
                        }
                        catch (Exception ex)
                        {
                            XtraMessageBox.Show($"Error loading image: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            pictureEdit1.Image = null;
                        }
                    }
                }
                else
                {
                    pictureEdit1.Image = null;
                }
            }
            else
            {
                XtraMessageBox.Show("No row selected.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }





        private void pictureEdit1_Click(object sender, EventArgs e)
        {
            using OpenFileDialog openFileDialog = new();
            openFileDialog.Filter = "Image files (*.jpg, *.jpeg, *.png, *.gif) | *.jpg; *.jpeg; *.png; *.gif";
            openFileDialog.Title = "Select an image";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                // Get the selected image file path
                string selectedImagePath = openFileDialog.FileName;

                try
                {
                    // Load the image from the file
                    Image selectedImage = Image.FromFile(selectedImagePath);

                    // Assign the loaded image to the PictureEdit control
                    pictureEdit1.Image = selectedImage;
                }
                catch (Exception ex)
                {
                    // Handle any potential exceptions that may occur during loading the image
                    XtraMessageBox.Show($"Error loading image: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void simpleButton1_Click(object sender, EventArgs e)
        {

        }

        private void sbaddorupdate_Click(object sender, EventArgs e)
        {
            bool isok = ValidateInputs();
            if (!isok)
            {
                return;
            }
            string isaktif = checkEditnonaktif.Checked ? "Y" : "T";
            string isanggota = checkEditnonanggota.Checked ? "Y" : "T";

            DTOAnggota anggotaToAddorUpdate = new()
            {
                // Modify the properties of the DTOAnggota object as needed
                NIK = textEdit_nik.Text,
                NAMA_PELANGGAN = textEdit_nama.Text,
                TMK = Convert.ToDateTime(dateEdit_tma.Text),
                STATUS = comboBoxEdit_status.Text,
                KODE_UNIT = lookUpEdit1.EditValue.ToString(),
                AKTIF = isaktif,
                ANGGOTA = isanggota,
                LIMIT_HUTANG = decimal.Parse(txtlimithutang.Text),
                GAMBAR = GetNewImageBytes() // Replace this with your own logic to retrieve the new image bytes
            };

            if (sbaddorupdate.Text == "Simpan")
            {
                controller.InsertAnggota(anggotaToAddorUpdate);
                controller.SimpanNomorAnggota(textEdit_nik.Text);
                cleardata();
            }
            else
            {
                controller.UpdateAnggota(anggotaToAddorUpdate);
                sbaddorupdate.Text = "Simpan";
            }
            anggota = controller.GetAnggotaData();
            gridControl1.DataSource = anggota;
            textEdit_nik.Text = controller.GenerateNikAnggota(DateTime.Today);
        }

        private void cleardata()
        {
            textEdit_nik.Text = "";
            textEdit_nama.Text = "";
            dateEdit_tma.Text = DateTime.Today.ToString();
            lookUpEdit1.EditValue = null;
            txtlimithutang.Text = "0";
            checkEditnonaktif.Checked = true;
            checkEditnonanggota.Checked = true;
            pictureEdit1.Image = null;
            sbaddorupdate.Text = "Simpan";
            textEdit_nik.Text = controller.GenerateNikAnggota(DateTime.Today);
        }

        private bool ValidateInputs()
        {
            string errorMessage = "";

            // Validate textEdit_nik
            if (string.IsNullOrWhiteSpace(textEdit_nik.Text))
            {
                errorMessage += "NIK wajib diisi.\n";
            }

            // Validate textEdit_nama
            if (string.IsNullOrWhiteSpace(textEdit_nama.Text))
            {
                errorMessage += "Nama wajib diisi.\n";
            }

            // Validate dateEdit_tma
            DateTime tmaDate;
            if (!DateTime.TryParse(dateEdit_tma.Text, out tmaDate))
            {
                errorMessage += "Tanggal masuk tidak valid.\n";
            }

            // Validate lookUpEdit1
            if (lookUpEdit1.EditValue == null)
            {
                errorMessage += "Unit kerja wajib dipilih.\n";
            }

            // Validate textEdit_LIMITHUTANG
            decimal limithutang;
            if (!decimal.TryParse(txtlimithutang.Text, out limithutang))
            {
                errorMessage += "Limit hutang harus berupa angka.\n";
            }

            // Check if any errors occurred
            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                // Display error message to the user or handle it as needed
                XtraMessageBox.Show(errorMessage, "Periksa Data", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            // Validation passed
            return true;
        }

        private byte[] GetNewImageBytes()
        {
            Image newImage = pictureEdit1.Image;
            if (newImage != null)
            {
                using MemoryStream memoryStream = new();

                // Create a new Bitmap with the desired dimensions
                Bitmap resizedImage = new Bitmap(newImage, 719, 749);

                // Save the resized image to the MemoryStream using PNG format
                resizedImage.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);

                // Convert the MemoryStream to a byte array and return it
                return memoryStream.ToArray();
            }

            return null;
        }

        private void sbbatal_Click(object sender, EventArgs e)
        {
            cleardata();

        }

        private void gridView1_RowCellStyle(object sender, DevExpress.XtraGrid.Views.Grid.RowCellStyleEventArgs e)
        {
            if (e.RowHandle >= 0) // Check if it's a data row
            {
                var gridView = sender as GridView;
                var cellValue = gridView.GetRowCellValue(e.RowHandle, "AKTIF"); // Replace "ColumnName" with the actual column name

                // Apply custom formatting based on the cell value
                if (cellValue != null && cellValue.ToString() == "T")
                {
                    e.Appearance.ForeColor = BackOfficeTheme.Danger;
                    e.Appearance.Font = new Font(e.Appearance.Font, FontStyle.Italic);
                }
            }
        }

        private void radioGroup1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (radioGroup1.SelectedIndex == 2)
            {
                anggota = controller.GetAnggotaData();
                gridControl1.DataSource = anggota;
                gridView1.Columns["UNIT_KERJA"].GroupIndex = 0;
                gridView1.Columns["KODE_UNIT"].Visible = false;
            }
            else
            {
                string isAnggota = string.Empty;


                if (radioGroup1.SelectedIndex == 0)
                {
                    isAnggota = "Y";
                }
                else if (radioGroup1.SelectedIndex == 1)
                {
                    isAnggota = "T";
                }

                IEnumerable<DTOAnggota> members = controller.GetMembersByStatusAndType(isAnggota);

                // Assuming dataGridView1 is the DataGridView control on your form
                gridControl1.DataSource = members.ToList();
                gridView1.Columns["UNIT_KERJA"].GroupIndex = 0;
                gridView1.Columns["KODE_UNIT"].Visible = false;
            }

        }
    }
}
