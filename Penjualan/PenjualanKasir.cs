using DevExpress.Utils.CodedUISupport;
using DevExpress.XtraBars;
using DevExpress.XtraBars.FluentDesignSystem;
using DevExpress.XtraBars.Navigation;
using DevExpress.XtraEditors;
using Penjualan.BusinessLayer;
using Penjualan.UC;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Penjualan
{
    public partial class PenjualanKasir : DevExpress.XtraBars.FluentDesignSystem.FluentDesignForm
    {
        
        public PenjualanKasir()
        {
            InitializeComponent();
        }


        private void PenjualanKasir_Load(object sender, EventArgs e)
        {
            LoginInfo.Penjualan_Control_Qty = POS_Services.GetSettingKontrol_qty_Saldo();

            //Add module1 to panel control
            if (!fluentDesignFormContainer.Controls.Contains(ucPenjualan.Instance))
            {
                fluentDesignFormContainer.Controls.Add(ucPenjualan.Instance);
                ucPenjualan.Instance.Dock = DockStyle.Fill;
                ucPenjualan.Instance.BringToFront();
            }
            else
                ucPenjualan.Instance.BringToFront();
        }

        private void accordionControlElementPenjualan_Click(object sender, EventArgs e)
        {
            //Add module1 to panel control
            if (!fluentDesignFormContainer.Controls.Contains(ucPenjualan.Instance))
            {
                fluentDesignFormContainer.Controls.Add(ucPenjualan.Instance);
                ucPenjualan.Instance.Dock = DockStyle.Fill;
                ucPenjualan.Instance.BringToFront();
            }
            else
                ucPenjualan.Instance.BringToFront();
           
        }

        private void accordionControlElementDaftarPenjualan_Click(object sender, EventArgs e)
        {
            ////Add module1 to panel control
            //if (!fluentDesignFormContainer.Controls.Contains(ucDaftarPenjualan.Instance))
            //{
            //    fluentDesignFormContainer.Controls.Add(ucDaftarPenjualan.Instance);
            //    ucDaftarPenjualan.Instance.Dock = DockStyle.Fill;
            //    ucDaftarPenjualan.Instance.BringToFront();
            //}
            //else
            //    ucDaftarPenjualan.Instance.BringToFront();

            // Change the form's text
            this.Text = "Daftar Penjualan";

            // Create a new instance of ucDaftarPenjualan
            ucDaftarPenjualan newInstance = new ucDaftarPenjualan();

            // Check if the fluentDesignFormContainer already contains a control
            if (fluentDesignFormContainer.Controls.Count > 0)
            {
                // Remove the existing control from the container
                fluentDesignFormContainer.Controls.RemoveAt(0);
            }

            // Add the new instance to the panel control
            fluentDesignFormContainer.Controls.Add(newInstance);
            newInstance.Dock = DockStyle.Fill;
            newInstance.BringToFront();

        }


        private void accordionControlElementReturPenjualan_Click(object sender, EventArgs e)
        {
            //Add module1 to panel control
            if (!fluentDesignFormContainer.Controls.Contains(ucReturPenjualan.Instance))
            {
                fluentDesignFormContainer.Controls.Add(ucReturPenjualan.Instance);
                ucReturPenjualan.Instance.Dock = DockStyle.Fill;
                ucReturPenjualan.Instance.BringToFront();
            }
            else
                ucReturPenjualan.Instance.BringToFront();
        }

        private void accordionControlElementLaporan_Click(object sender, EventArgs e)
        {
            //Add module1 to panel control
            if (!fluentDesignFormContainer.Controls.Contains(ucLaporan.Instance))
            {
                fluentDesignFormContainer.Controls.Add(ucLaporan.Instance);
                ucLaporan.Instance.Dock = DockStyle.Fill;
                ucLaporan.Instance.BringToFront();
            }
            else
                ucLaporan.Instance.BringToFront();
        }

        private void accordionControlElementKeluar_Click(object sender, EventArgs e)
        {
            if (XtraMessageBox.Show("Keluar dari Aplikasi Penjualan Kasir ? ",
         "Konfirmasi", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == System.Windows.Forms.DialogResult.No)
            { return; }
            else
            {
                this.Close();
                Application.Exit();
            }

        }

        private void accordionControlElementDashBoard_Click(object sender, EventArgs e)
        {
            //Add module1 to panel control
            if (!fluentDesignFormContainer.Controls.Contains(ucDashBoard.Instance))
            {
                fluentDesignFormContainer.Controls.Add(ucDashBoard.Instance);
                ucDashBoard.Instance.Dock = DockStyle.Fill;
                ucDashBoard.Instance.BringToFront();
            }
            else
                ucDashBoard.Instance.BringToFront();
        }

        private void accordionControl1_SizeChanged(object sender, EventArgs e)
        {
            if (accordionControl1.Size.Width == 0)
            {
                // Set the position and size of the fluentDesignFormContainer when the accordion is collapsed
                fluentDesignFormContainer.Dock = DockStyle.Fill;
            }
            else
            {
                // Set the position and size of the fluentDesignFormContainer when the accordion is expanded
                fluentDesignFormContainer.Dock = DockStyle.None;
                fluentDesignFormContainer.Left = accordionControl1.Width + 10;
                fluentDesignFormContainer.Width = this.ClientSize.Width - accordionControl1.Width - 10;
                fluentDesignFormContainer.Height = this.ClientSize.Height;
            }
        }

        private void accordionControlElement1_Click(object sender, EventArgs e)
        {
            //Add module1 to panel control
            if (!fluentDesignFormContainer.Controls.Contains(ucPenjualanAngsuran.Instance))
            {
                fluentDesignFormContainer.Controls.Add(ucPenjualanAngsuran.Instance);
                ucPenjualanAngsuran.Instance.Dock = DockStyle.Fill;
                ucPenjualanAngsuran.Instance.BringToFront();
            }
            else
                ucPenjualanAngsuran.Instance.BringToFront();

        }

        private void PenjualanKasir_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Check if the user clicked the close button (X) or used Alt+F4
            if (e.CloseReason == CloseReason.UserClosing)
            {
                DialogResult result = XtraMessageBox.Show("Anda akan keluar dari Program ?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.No)
                {
                    e.Cancel = true; // Cancel the form closing
                }
                else if (result == DialogResult.Yes)
                {
                    Application.Exit(); // Exit the application
                }
            }
        }
    }
}
