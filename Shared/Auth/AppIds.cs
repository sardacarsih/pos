namespace Pos.Shared.Auth
{
    /// <summary>
    /// Kode aplikasi (APP_ID) yang dipakai untuk login &amp; gating akses.
    /// Harus cocok dengan baris di tabel POS_APP.
    /// </summary>
    public static class AppIds
    {
        public const string Penjualan = "PENJUALAN";
        public const string BackOffice = "BACKOFFICE";
        public const string Pembelian = "PEMBELIAN";
    }
}
