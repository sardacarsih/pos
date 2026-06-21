using Microsoft.Extensions.Configuration;
using Migrator;
using Oracle.ManagedDataAccess.Client;

if (HasFlag(args, "--help") || HasFlag(args, "-h"))
{
    PrintUsage();
    return 0;
}

Dictionary<string, string?> opts = ParseArgs(args);

string? connectionString = ResolveConnection(opts);
if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine(
        "ERROR: connection string tidak ditemukan.\n" +
        "Gunakan --connection \"<connstr>\", env POS_CONNECTION, atau appsettings.json di folder kerja.");
    return 2;
}

try
{
    Console.WriteLine("== POS Migrator ==");

    // Cek koneksi lebih dulu agar pesan error jelas.
    using (var test = new OracleConnection(connectionString))
    {
        test.Open();
        Console.WriteLine($"Terhubung ke: {test.DataSource}");
    }

    Console.WriteLine("\nMenerapkan migrasi:");
    var runner = new MigrationRunner(connectionString);
    int newCount = runner.Apply(Migrations.All);
    Console.WriteLine($"-> {newCount} migrasi baru diterapkan ({Migrations.All.Length} total terdefinisi).");

    if (!HasFlag(args, "--skip-admin"))
    {
        string adminUser = opts.GetValueOrDefault("--admin-user") ?? "admin";
        bool defaultPwd = !opts.TryGetValue("--admin-password", out string? adminPwd) || string.IsNullOrEmpty(adminPwd);
        adminPwd ??= "Admin#2026";

        Console.WriteLine("\nSeeding admin default:");
        AdminSeeder.Seed(connectionString, adminUser, adminPwd);
        if (defaultPwd)
        {
            Console.WriteLine($"  [WARN ] memakai password default \"{adminPwd}\". SEGERA ganti via menu Manajemen User!");
        }
    }

    Console.WriteLine("\nSelesai.");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine("\nGAGAL: " + ex.Message);
    return 1;
}

// ----------------------------------------------------------------- helpers

static Dictionary<string, string?> ParseArgs(string[] args)
{
    var dict = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
    for (int i = 0; i < args.Length; i++)
    {
        if (!args[i].StartsWith("--", StringComparison.Ordinal))
        {
            continue;
        }
        // Flag tanpa nilai bila argumen berikutnya juga opsi (atau habis).
        if (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
        {
            dict[args[i]] = args[++i];
        }
        else
        {
            dict[args[i]] = null;
        }
    }
    return dict;
}

static bool HasFlag(string[] args, string name) =>
    Array.Exists(args, a => string.Equals(a, name, StringComparison.OrdinalIgnoreCase));

static string? ResolveConnection(Dictionary<string, string?> opts)
{
    if (opts.TryGetValue("--connection", out string? c) && !string.IsNullOrWhiteSpace(c))
    {
        return c;
    }

    string? env = Environment.GetEnvironmentVariable("POS_CONNECTION");
    if (!string.IsNullOrWhiteSpace(env))
    {
        return env;
    }

    IConfigurationRoot config = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
        .Build();
    return config.GetConnectionString("OracleConnection");
}

static void PrintUsage()
{
    Console.WriteLine(
        "POS Migrator - menerapkan migrasi skema & membuat admin default.\n\n" +
        "Pemakaian:\n" +
        "  PosMigrator [opsi]\n\n" +
        "Opsi:\n" +
        "  --connection \"<connstr>\"   Connection string Oracle (atau env POS_CONNECTION / appsettings.json)\n" +
        "  --admin-user <nama>         Username admin (default: admin)\n" +
        "  --admin-password <pwd>      Password admin (default: Admin#2026 - segera ganti)\n" +
        "  --skip-admin                Hanya migrasi skema, tanpa membuat admin\n" +
        "  --help                      Tampilkan bantuan ini\n\n" +
        "Contoh:\n" +
        "  PosMigrator --connection \"Data Source=...;User Id=kopkar;Password=...;\" --admin-password \"Rahasia#1\"\n");
}
