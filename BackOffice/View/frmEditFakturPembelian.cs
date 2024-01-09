using BackOffice.UC;
using System;
using System.Linq;

namespace BackOffice.View
{
    public partial class frmEditFakturPembelian : DevExpress.XtraEditors.XtraForm
    {

        
        public frmEditFakturPembelian()
        {
            InitializeComponent();
            // Get the screen's working area (excluding taskbars and other docked items)
            var screenBounds = Screen.GetWorkingArea(this);

            // You can adjust the form's size based on a percentage of the screen size or any other logic.
            // Here, we set the form's size to 80% of the screen width and height.
            int desiredWidth = (int)(screenBounds.Width * 0.8);
            int desiredHeight = (int)(screenBounds.Height * 0.8);

            // Set the form's size
            this.Size = new System.Drawing.Size(desiredWidth, desiredHeight);

            // Center the form on the screen
            this.StartPosition = FormStartPosition.CenterScreen;
        }
        public ucPembelianEdit ucPembelianEditInstance; // Declare a class-level variable to hold the instance
        private void frmEditFakturPembelian_Load(object sender, EventArgs e)
        {
            
            // Check if the instance is null or disposed before creating a new one
            if (ucPembelianEditInstance == null || ucPembelianEditInstance.IsDisposed)
            {
                ucPembelianEditInstance = new ucPembelianEdit();
            }

            // Add the instance to the panel and set its properties
            ucPembelianEditInstance.Dock = DockStyle.Fill;

            // Check if the instance is not already part of the panel's controls
            if (!panelControl.Controls.Contains(ucPembelianEditInstance))
            {
                panelControl.Controls.Add(ucPembelianEditInstance);
            }

            // Bring the control to the front
            ucPembelianEditInstance.BringToFront();

           


        }

        private void frmEditFakturPenjualan_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.DialogResult = DialogResult.OK;
        }

        private void frmEditFakturPenjualan_FormClosed(object sender, FormClosedEventArgs e)
        {

        }
    }
}