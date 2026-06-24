
using BackOffice.Controller;
using BackOffice.Model;
using DevExpress.XtraEditors;

namespace BackOffice.UC
{
    public partial class ucSupplier : UserControl
    {
        private string _selectedKode;
        private readonly SupplierController controller = new();

        private static ucSupplier _instance;
        public static ucSupplier Instance
        {
            get
            {
                _instance ??= new ucSupplier();
                return _instance;
            }
        }

        public ucSupplier()
        {
            InitializeComponent();
        }

        private void ucSupplier_Load(object sender, EventArgs e)
        {
            LoadSupplier();
            ResetForm();
        }

        private void LoadSupplier()
        {
            try
            {
                var suppliers = controller.GetAllSuppliers();
                gridControl1.DataSource = suppliers;
                gridView1.BestFitColumns();
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show("Gagal memuat data supplier: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ResetForm()
        {
            txtkode.Text = string.Empty;
            txtnama.Text = string.Empty;
            _selectedKode = null;

            txtkode.Enabled = true;
            barLargeButtonItem1.Enabled = true;
            barLargeButtonItem2.Enabled = false;
            barLargeButtonItem3.Enabled = false;
        }

        // Save button
        private void barLargeButtonItem1_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtkode.Text) || string.IsNullOrWhiteSpace(txtnama.Text))
            {
                XtraMessageBox.Show("Kode dan Nama supplier harus diisi.", "Validasi",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string kode = txtkode.Text.Trim().ToUpper();
            string nama = txtnama.Text.Trim().ToUpper();

            try
            {
                if (controller.IsKodeExists(kode))
                {
                    XtraMessageBox.Show("Kode supplier sudah ada.", "Validasi",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                DTOSupplier supplier = new() { KODE = kode, NAMA = nama, AKTIF = "Y" };
                int result = controller.InsertSupplier(supplier);

                if (result > 0)
                {
                    XtraMessageBox.Show("Supplier berhasil ditambahkan.", "Informasi",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadSupplier();
                    ResetForm();
                }
                else
                {
                    XtraMessageBox.Show("Gagal menambahkan supplier.", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show("Error saat menyimpan supplier: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Grid double click - select row for edit/delete
        private void gridControl1_DoubleClick(object sender, EventArgs e)
        {
            if (gridView1.SelectedRowsCount > 0)
            {
                var row = gridView1.GetFocusedRow() as DTOSupplier;
                if (row != null)
                {
                    _selectedKode = row.KODE;
                    txtkode.Text = row.KODE;
                    txtnama.Text = row.NAMA;

                    txtkode.Enabled = false;
                    barLargeButtonItem1.Enabled = false;
                    barLargeButtonItem2.Enabled = true;
                    barLargeButtonItem3.Enabled = true;
                }
            }
        }

        // Update button
        private void barLargeButtonItem2_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedKode))
            {
                XtraMessageBox.Show("Pilih supplier yang akan diubah.", "Validasi",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtnama.Text))
            {
                XtraMessageBox.Show("Nama supplier harus diisi.", "Validasi",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                DTOSupplier supplier = new() { KODE = _selectedKode, NAMA = txtnama.Text.Trim().ToUpper() };
                int rowsAffected = controller.UpdateSupplier(supplier);

                if (rowsAffected > 0)
                {
                    XtraMessageBox.Show("Supplier berhasil diubah.", "Informasi",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadSupplier();
                    ResetForm();
                }
                else
                {
                    XtraMessageBox.Show("Tidak ada data yang diubah.", "Peringatan",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show("Error saat mengubah supplier: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Delete button (soft delete)
        private void barLargeButtonItem3_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedKode))
            {
                XtraMessageBox.Show("Pilih supplier yang akan dihapus.", "Validasi",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var confirm = XtraMessageBox.Show(
                $"Apakah Anda yakin ingin menonaktifkan supplier '{_selectedKode}'?",
                "Konfirmasi", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            try
            {
                int rowsAffected = controller.DeactivateSupplier(_selectedKode);

                if (rowsAffected > 0)
                {
                    XtraMessageBox.Show("Supplier berhasil dinonaktifkan.", "Informasi",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadSupplier();
                    ResetForm();
                }
                else
                {
                    XtraMessageBox.Show("Tidak ada data yang diubah.", "Peringatan",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show("Error saat menonaktifkan supplier: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
