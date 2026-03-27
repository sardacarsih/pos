using DevExpress.XtraEditors;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Penjualan
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Environment.CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;

            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();

            try
            {
                Log.Information("Penjualan application starting up");

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

                Application.ThreadException += (sender, e) =>
                {
                    Log.Fatal(e.Exception, "Unhandled UI thread exception");
                    MessageBox.Show(
                        $"Terjadi kesalahan: {e.Exception.Message}",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                };

                AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
                {
                    Log.Fatal((Exception)e.ExceptionObject, "Unhandled non-UI exception");
                    Log.CloseAndFlush();
                };

                WindowsFormsSettings.LoadApplicationSettings();
                ApplicationConfiguration.Initialize();
                Application.Run(new Frmlogin());
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application terminated unexpectedly");
                throw;
            }
            finally
            {
                Log.Information("Penjualan application shutting down");
                Log.CloseAndFlush();
            }
        }
    }
}
