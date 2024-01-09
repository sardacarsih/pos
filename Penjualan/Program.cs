using DevExpress.XtraEditors;

namespace Penjualan
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            WindowsFormsSettings.LoadApplicationSettings();
            ApplicationConfiguration.Initialize();
            Application.Run(new Frmlogin());
        }
    }
}