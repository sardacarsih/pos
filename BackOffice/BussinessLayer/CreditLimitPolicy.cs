namespace BackOffice.BussinessLayer
{
    // Mirrors Penjualan.BusinessLayer.CreditLimitPolicy — the per-period credit-limit verdict.
    public static class CreditLimitPolicy
    {
        /// <summary>
        /// Returns true when posting <paramref name="invoiceAmount"/> would push the member's
        /// credit spend in the current period beyond <paramref name="limit"/>.
        /// A <paramref name="limit"/> of 0 means "unlimited". Boundary is inclusive.
        /// </summary>
        public static bool IsExceeded(decimal periodSpend, decimal invoiceAmount, decimal limit)
        {
            if (limit == 0)
                return false;

            return periodSpend + invoiceAmount > limit;
        }
    }
}
