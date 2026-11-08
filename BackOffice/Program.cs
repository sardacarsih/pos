using DevExpress.XtraEditors;
using Microsoft.Extensions.Configuration;
using Serilog;
using BackOffice.UI;
using System.IO;
using System.Text;

namespace BackOffice
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
                Log.Information("BackOffice application starting up");

                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
                BackOfficeTheme.Initialize();

                Application.ThreadException += (sender, e) =>
                {
                    LogFatal(e.Exception, "Unhandled UI thread exception");
                    XtraMessageBox.Show(
                        $"Terjadi kesalahan aplikasi.\n{e.Exception.Message}",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                };

                AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
                {
                    Exception exception = e.ExceptionObject as Exception
                        ?? new InvalidOperationException(e.ExceptionObject?.ToString() ?? "Unknown fatal exception");
                    LogFatal(exception, "Unhandled non-UI exception");
                };

                WindowsFormsSettings.LoadApplicationSettings();
                ApplicationConfiguration.Initialize();
                Application.Run(new Frmlogin());
            }
            catch (Exception ex)
            {
                LogFatal(ex, "Application terminated unexpectedly");
                XtraMessageBox.Show(
                    $"Aplikasi berhenti karena error.\n{ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                Log.Information("BackOffice application shutting down");
                Log.CloseAndFlush();
            }
        }
        private static void LogFatal(Exception exception, string message)
        {
            Log.Fatal(exception, message);
            WriteFallbackCrashLog(exception, message);
            Log.CloseAndFlush();
        }

        private static void WriteFallbackCrashLog(Exception exception, string message)
        {
            try
            {
                string logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                Directory.CreateDirectory(logDirectory);
                string logPath = Path.Combine(logDirectory, "BackOffice-crash.log");
                var builder = new StringBuilder()
                    .AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}")
                    .AppendLine(exception.ToString())
                    .AppendLine();
                File.AppendAllText(logPath, builder.ToString());
            }
            catch
            {
                // Last-resort crash logging must never trigger another crash.
            }
        }
    }
}
