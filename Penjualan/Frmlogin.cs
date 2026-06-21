using System.IO;
using System.Media;
using DevExpress.XtraEditors;
using DevExpress.XtraSplashScreen;
using Pos.Shared.Auth;
using PosLoginUi;

namespace Penjualan;

public partial class Frmlogin : XtraForm
{
    private readonly AuthRepository auth = new(Global.connectionString);
    private readonly SoundPlayer Player = new();
    private static int kesempatan = 3;
    private Image? loginBackgroundSource;
    private Bitmap? loginBackgroundFrame;

    public Frmlogin()
    {
        InitializeComponent();
        LoadLoginBackground();
    }

    protected override CreateParams CreateParams
    {
        get
        {
            const int WsClipChildren = 0x02000000;
            CreateParams parameters = base.CreateParams;
            parameters.Style |= WsClipChildren;
            return parameters;
        }
    }

    private void Frmlogin_Load(object sender, EventArgs e)
    {
        lblversi.Text = "Version " + Application.ProductVersion.Split('+')[0];
        ApplyWorkingAreaBounds();
        LayoutLoginCard();
        txtuserid.Focus();
    }

    private void Frmlogin_Shown(object sender, EventArgs e)
    {
        ApplyWorkingAreaBounds();
        LayoutLoginCard();
        txtuserid.Focus();
    }

    private void ApplyWorkingAreaBounds()
    {
        Bounds = Screen.FromPoint(Cursor.Position).WorkingArea;
    }

    private void Frmlogin_Resize(object sender, EventArgs e)
    {
        RebuildBackground();
        LayoutLoginCard();
    }

    private void LayoutLoginCard()
    {
        if (ClientSize.Width <= 0 || ClientSize.Height <= 0)
        {
            return;
        }

        int viewportWidth = ClientSize.Width;
        int viewportHeight = ClientSize.Height;
        int cardWidth = Math.Min(560, Math.Max(440, (int)(viewportWidth * 0.34)));
        cardWidth = Math.Min(cardWidth, viewportWidth - 32);
        int cardHeight = Math.Min(850, Math.Max(620, (int)(viewportHeight * 0.90)));
        cardHeight = Math.Min(cardHeight, viewportHeight - 24);
        int rightMargin = Math.Clamp((int)(viewportWidth * 0.09), 38, 190);
        int cardX = Math.Max(16, viewportWidth - rightMargin - cardWidth);
        int cardY = Math.Max(12, (viewportHeight - cardHeight) / 2);

        cardPanel.SetBounds(cardX, cardY, cardWidth, cardHeight);

        int contentLeft = 62;
        int contentWidth = cardWidth - 124;
        int centerX = cardWidth / 2;
        int ScaleY(int baseY)
        {
            const int topInset = 14;
            return topInset + ((baseY - topInset) * (cardHeight - 28) / (790 - 28));
        }

        avatarControl.Location = new Point(centerX - avatarControl.Width / 2, ScaleY(58));
        lblApplication.SetBounds(contentLeft, ScaleY(174), contentWidth, 44);
        lblSubtitle.SetBounds(contentLeft, ScaleY(222), contentWidth, 28);
        accentLine.Location = new Point(centerX - accentLine.Width / 2, ScaleY(263));
        lblUserId.SetBounds(contentLeft, ScaleY(298), contentWidth, 25);
        userInputPanel.SetBounds(contentLeft, ScaleY(328), contentWidth, 56);
        lblPassword.SetBounds(contentLeft, ScaleY(409), contentWidth, 25);
        passwordInputPanel.SetBounds(contentLeft, ScaleY(439), contentWidth, 56);
        Login.SetBounds(contentLeft, ScaleY(523), contentWidth, 60);
        footerLine.SetBounds(contentLeft, ScaleY(628), contentWidth, 1);
        securityShield.Location = new Point(contentLeft + 2, ScaleY(655));
        lblSecurityTitle.SetBounds(contentLeft + 56, ScaleY(654), contentWidth - 56, 23);
        lblSecurityText.SetBounds(contentLeft + 56, ScaleY(679), contentWidth - 56, 24);
        lblversi.SetBounds(contentLeft, cardHeight - 46, contentWidth, 18);
        simpleButton1.SetBounds(viewportWidth - 64, 22, 42, 42);

        cardPanel.BringToFront();
        passwordEyeButton.BringToFront();
        simpleButton1.BringToFront();
    }

    private void LoadLoginBackground()
    {
        using Stream? stream = typeof(Frmlogin).Assembly
            .GetManifestResourceStream("Penjualan.Assets.LoginKopkar.png");
        if (stream is null)
        {
            throw new InvalidOperationException("Embedded login background was not found.");
        }

        using Image image = Image.FromStream(stream);
        loginBackgroundSource = new Bitmap(image);
        RebuildBackground();
    }

    private void RebuildBackground()
    {
        if (loginBackgroundSource is null || ClientSize.Width <= 0 || ClientSize.Height <= 0)
        {
            return;
        }

        Bitmap newFrame = LoginArtwork.RenderCover(loginBackgroundSource, ClientSize);
        BackgroundImage = newFrame;
        BackgroundImageLayout = ImageLayout.Stretch;
        loginBackgroundFrame?.Dispose();
        loginBackgroundFrame = newFrame;
    }

    private void Frmlogin_FormClosed(object sender, FormClosedEventArgs e)
    {
        BackgroundImage = null;
        loginBackgroundFrame?.Dispose();
        loginBackgroundSource?.Dispose();
    }

    private void PasswordEyeButton_Click(object sender, EventArgs e)
    {
        bool showPassword = txtpwd.Properties.UseSystemPasswordChar;
        txtpwd.Properties.UseSystemPasswordChar = !showPassword;
        passwordEyeButton.PasswordVisible = showPassword;
        passwordEyeButton.Invalidate();
        txtpwd.Focus();
        txtpwd.SelectionStart = txtpwd.Text.Length;
    }

    private void txtuserid_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            txtpwd.Focus();
            e.SuppressKeyPress = true;
        }
    }

    private void txtpwd_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            Login.PerformClick();
            e.SuppressKeyPress = true;
        }
    }

    private void Login_Click(object sender, EventArgs e)
    {
        Login.Enabled = false;
        simpleButton1.Enabled = false;
        try
        {
            using var handle = SplashScreenManager.ShowOverlayForm(this);
            if (string.IsNullOrEmpty(txtuserid.Text) | string.IsNullOrEmpty(txtpwd.Text))
            {
                MessageBox.Show(
                    "UserID atau Password belum diisi",
                    "Konfirmasi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (kesempatan == 0)
            {
                XtraMessageBox.Show(
                    "3 Kesempatan Gagal digunakan - Hubungi Manager Anda",
                    "Info",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            string pwd = txtpwd.Text;
            string username = txtuserid.Text.Trim().ToLower();
            LoginResult? res = auth.Lookup(username, AppIds.Penjualan);

            if (res is null)
            {
                XtraMessageBox.Show(
                    "UserID tidak terdaftar atau tidak punya akses Penjualan. \nAnda Memiliki " +
                    kesempatan + "X Kesempatan untuk Mencoba",
                    "Perhatian",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                --kesempatan;
                txtuserid.Focus();
                return;
            }

            if (!res.IsActive || res.IsLocked)
            {
                XtraMessageBox.Show(
                    "Akun Anda nonaktif atau terkunci - Hubungi Manager Anda",
                    "Info",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            var passwordCryptography = new PasswordCryptographyPbkdf2();
            if (passwordCryptography.IsValidPassword(pwd, res.PasswordHash))
            {
                if (passwordCryptography.NeedsRehash(res.PasswordHash))
                {
                    auth.UpdatePassword(res.UserId, passwordCryptography.GetHashPassword(pwd));
                }
                LoginInfo.role = res.RoleName;
                LoginInfo.userID = res.Username;
                Hide();
                new PenjualanKasir().Show();
            }
            else
            {
                --kesempatan;
                XtraMessageBox.Show(
                    "Password Salah. \nAnda Memiliki " + kesempatan +
                    "X Kesempatan untuk Mencoba",
                    "Perhatian",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
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
        Close();
    }
}
