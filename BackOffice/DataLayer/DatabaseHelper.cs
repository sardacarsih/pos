using Microsoft.Extensions.Configuration;

namespace BackOffice.DataLayer
{
    public static class global
    {
        public static readonly string connectionString;
        public static readonly string DefaultCustomerNIK;

        static global()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build();

            connectionString = configuration.GetConnectionString("OracleConnection")
                ?? throw new InvalidOperationException(
                    "Connection string 'OracleConnection' not found in appsettings.json");

            DefaultCustomerNIK = configuration["AppSettings:DefaultCustomerNIK"] ?? "00.00004";
        }
    }
}
