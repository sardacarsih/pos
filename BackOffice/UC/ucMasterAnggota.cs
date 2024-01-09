using BackOffice.Controller;
using BackOffice.DataLayer;
using BackOffice.Model;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Grid;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.IO;

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

            lookUpEdit1.Properties.DataSource = controller2.GetUnitKerja();
            lookUpEdit1.Properties.ValueMember = "KODE";
            lookUpEdit1.Properties.DisplayMember = "NAMA";

            comboBoxEdit_status.Properties.Items.Add("BULANAN");
            comboBoxEdit_status.Properties.Items.Add("KHT");
            textEdit_nik.Text = controller.GenerateNikAnggota(DateTime.Today);

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
                int selectedIndex = gridView1.GetSelectedRows()[0];

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
                DTOAnggota selectedItem = gridView1.GetRow(focusedHandle) as DTOAnggota;

                // Rest of your code remains the same...
                textEdit_nik.Text = selectedItem.NIK;
                textEdit_nama.Text = selectedItem.NAMA_PELANGGAN;
                dateEdit_tma.Text = selectedItem.TMK.ToString();
                lookUpEdit1.EditValue = selectedItem.KODE_UNIT;
                comboBoxEdit_status.Text = selectedItem.STATUS;
                var nonaktif = selectedItem.AKTIF;
                var nonanggota = selectedItem.ANGGOTA;
                if (nonaktif == "Y") { checkEditnonaktif.Checked = false; } else { checkEditnonaktif.Checked = true; }
                if (nonanggota == "Y") { checkEditnonanggota.Checked = false; } else { checkEditnonanggota.Checked = true; }
                if (selectedItem.GAMBAR != null)
                {
                    using MemoryStream memoryStream = new(selectedItem.GAMBAR);
                    try
                    {
                        Image selectedImage = Image.FromStream(memoryStream);
                        pictureEdit1.Image = selectedImage;
                    }
                    catch (Exception ex)
                    {
                        XtraMessageBox.Show($"Error loading image: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    pictureEdit1.Image = null;
                }
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
            string isaktif = "Y";
            string isanggota = "Y";
            if (checkEditnonanggota.Checked) { isanggota = "T"; }
            if (checkEditnonaktif.Checked) { isaktif = "T"; }

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
                LIMIT_HUTANG = decimal.Parse(textEdit_LIMITHUTANG.Text),
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
            textEdit_LIMITHUTANG.Text = "0";
            checkEditnonaktif.Checked = false;
            checkEditnonanggota.Checked = false;
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
                errorMessage += "NIK is required.\n";
            }

            // Validate textEdit_nama
            if (string.IsNullOrWhiteSpace(textEdit_nama.Text))
            {
                errorMessage += "Nama is required.\n";
            }

            // Validate dateEdit_tma
            DateTime tmaDate;
            if (!DateTime.TryParse(dateEdit_tma.Text, out tmaDate))
            {
                errorMessage += "Invalid TANGGAL .\n";
            }

            // Validate lookUpEdit1
            if (lookUpEdit1.EditValue == null)
            {
                errorMessage += "A value must be selected for UNIT KERJA.\n";
            }

            // Validate textEdit_LIMITHUTANG
            decimal limithutang;
            if (!decimal.TryParse(textEdit_LIMITHUTANG.Text, out limithutang))
            {
                errorMessage += "Invalid LIMITHUTANG value.\n";
            }

            // Check if any errors occurred
            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                // Display error message to the user or handle it as needed
                MessageBox.Show(errorMessage, "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    e.Appearance.BackColor = Color.Red; // Change the background color to red
                    //e.Appearance.ForeColor = Color.White; // Change the font color to white
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
