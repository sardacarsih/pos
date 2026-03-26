using System;
using System.Data;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraSplashScreen;
using Oracle.ManagedDataAccess.Client;
using System.Media;
using BackOffice.DataLayer;

namespace BackOffice
{
    public  partial class Frmlogin : DevExpress.XtraEditors.XtraForm
    {
        OracleConnection con = new(global.connectionString);
        private SoundPlayer Player = new();
        static int kesempatan = 3;
        public Frmlogin()
        {
            InitializeComponent();
        }
        DataTable dt;
        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        private void Frmlogin_Load(object sender, EventArgs e)
        {
            var screen = Screen.FromControl(this);
            int w = Math.Max(820, Math.Min(1100, (int)(screen.WorkingArea.Width * 0.47)));
            int h = Math.Max(520, Math.Min(700, (int)(screen.WorkingArea.Height * 0.54)));
            this.ClientSize = new Size(w, h);
            this.CenterToScreen();

            lblversi.Text = "Version " + Application.ProductVersion;
            txtuserid.Focus();
        }

        private void txtuserid_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                SendKeys.Send("{TAB}");
            }
        }


        private void txtpwd_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                SendKeys.Send("{TAB}");
                Login.PerformClick();
            }
        }

        private void panelBranding_Paint(object sender, PaintEventArgs e)
        {
            var panel = (PanelControl)sender;
            if (panel.ClientRectangle.Width == 0 || panel.ClientRectangle.Height == 0)
                return;
            using var brush = new LinearGradientBrush(
                panel.ClientRectangle,
                Color.FromArgb(15, 23, 42),    // #0f172a slate-900
                Color.FromArgb(30, 58, 95),    // #1e3a5f dark blue
                LinearGradientMode.Vertical);
            e.Graphics.FillRectangle(brush, panel.ClientRectangle);

            // Subtle decorative circle
            using var circleBrush = new SolidBrush(Color.FromArgb(8, 255, 255, 255));
            int circleSize = (int)(panel.Width * 1.2);
            e.Graphics.FillEllipse(circleBrush,
                -panel.Width / 3, (int)(panel.Height * 0.55),
                circleSize, circleSize);
        }

        private void panelBranding_Resize(object sender, EventArgs e)
        {
            var panel = panelBranding;
            int centerX = panel.Width / 2;

            int logoH = pictureBoxLogo.Height;
            int brandH = lblBrandName.Height;
            int tagH = lblTagline.Height;
            int gap1 = 16;
            int gap2 = 8;
            int totalHeight = logoH + gap1 + brandH + gap2 + tagH;
            int startY = (panel.Height - totalHeight) / 2 - 20;

            pictureBoxLogo.Location = new Point(centerX - pictureBoxLogo.Width / 2, startY);

            lblBrandName.Location = new Point(0, startY + logoH + gap1);
            lblBrandName.Size = new Size(panel.Width, brandH);

            lblTagline.Location = new Point(0, lblBrandName.Bottom + gap2);
            lblTagline.Size = new Size(panel.Width, tagH);

            // Version at bottom
            lblversi.Location = new Point(0, panel.Height - 30);
            lblversi.Size = new Size(panel.Width, 20);
        }

        private void Login_Click(object sender, EventArgs e)
        {
            Login.Enabled = false;
            simpleButton1.Enabled = false;
            try
            {
                using var handle = SplashScreenManager.ShowOverlayForm(this);
                if (string.IsNullOrEmpty(txtuserid.Text) | (string.IsNullOrEmpty(txtpwd.Text)))
                {
                    //this.Player.SoundLocation = Environment.CurrentDirectory + "\\wav\\userid.wav";
                    //this.Player.Play();
                    MessageBox.Show("UserID atau Password belum diisi", "Konfirmasi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (kesempatan == 0)
                {
                    //this.Player.SoundLocation = Environment.CurrentDirectory + "\\wav\\hub_manager.wav";
                    //this.Player.Play();
                    XtraMessageBox.Show("3 Kesempatan Gagal digunakan - Hubungi Manager Anda", "Info",MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                string pwd = txtpwd.Text;
                using OracleCommand cmd = new("SELECT * FROM VLOGIN where userid=:USERID  AND APPID='GL' ", con);

                if (con.State != ConnectionState.Open)
                {
                    con.Open();
                }
                cmd.Parameters.Add(":USERID", OracleDbType.Varchar2, 25).Value = txtuserid.Text.ToLower();

                OracleDataReader dr;
                dr = cmd.ExecuteReader();
                if (dr.HasRows)
                {
                    dt = new DataTable();
                    dt.Load(dr);
                    string hashPass = dt.Rows[0]["PASSWORD"].ToString();

                    var p = new PasswordCryptographyPbkdf2();

                    bool masuk = p.IsValidPassword(pwd, hashPass);
                    if (masuk)
                    {
                        LoginInfo.role = dt.Rows[0]["AKSESROLE"].ToString();
                        LoginInfo.userID = dt.Rows[0]["USERID"].ToString();

                        Hide();
                        new BackOffice().Show();
                    }
                    else
                    {
                        --kesempatan;
                        XtraMessageBox.Show("Password Salah. " +
                           "\nAnda Memiliki " + Convert.ToString(kesempatan) + "X Kesempatan untuk Mencoba", "Perhatian", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }

                }
                else
                {
                    XtraMessageBox.Show("UserID tidak terdaftar. " +
                        "\nAnda Memiliki " + Convert.ToString(kesempatan) + "X Kesempatan untuk Mencoba", "Perhatian", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    --kesempatan;
                    txtuserid.Focus();
                }
                dr.Close();
                con.Close();
            }
            catch (SystemException ex)
            {
                XtraMessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                --kesempatan;
            }
            finally
            {
                Login.Enabled = true;
                simpleButton1.Enabled = true;
            }

        }
        private void SimpleButton1_Click(object sender, EventArgs e)
        {
            //con.Close();
            this.Close();
        }

    }
}
