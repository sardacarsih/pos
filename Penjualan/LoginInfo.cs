namespace Penjualan
{
    public static class LoginInfo
    {
        public static bool Penjualan_Control_Qty;
        public static string userID;
        public static string role;

        /// <summary>USER_ID numerik dari POS_USER.</summary>
        public static int UserId { get; set; }

        /// <summary>Nama lengkap user yang login (POS_USER.FULL_NAME).</summary>
        public static string FullName { get; set; }
    }
}
