using BackOffice.Model;
using BackOffice.UC;
using DevExpress.XtraBars.FluentDesignSystem;
using DevExpress.XtraEditors;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BackOffice.View
{
    public partial class frmEditFakturPenjualan : DevExpress.XtraEditors.XtraForm
    {

        
        public frmEditFakturPenjualan()
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
        public ucPenjualanEdit ucPenjualanEditInstance; // Declare a class-level variable to hold the instance
        private void frmEditFakturPenjualan_Load(object sender, EventArgs e)
        {
            
            // Check if the instance is null or disposed before creating a new one
            if (ucPenjualanEditInstance == null || ucPenjualanEditInstance.IsDisposed)
            {
                ucPenjualanEditInstance = new ucPenjualanEdit();
            }

            // Add the instance to the panel and set its properties
            ucPenjualanEditInstance.Dock = DockStyle.Fill;

            // Check if the instance is not already part of the panel's controls
            if (!panelControl.Controls.Contains(ucPenjualanEditInstance))
            {
                panelControl.Controls.Add(ucPenjualanEditInstance);
            }

            // Bring the control to the front
            ucPenjualanEditInstance.BringToFront();

           


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