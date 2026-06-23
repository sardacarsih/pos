namespace Penjualan.BusinessLayer
{
    /// <summary>
    /// Pure decision rule for the per-period credit limit, kept free of any DB/UI so it
    /// can be unit tested in isolation. The DB-side <c>ValidateCreditLimit</c> resolves the
    /// period spend and member limit, then defers the verdict to this policy.
    /// </summary>
    public static class CreditLimitPolicy
    {
        /// <summary>
        /// Returns true when posting <paramref name="invoiceAmount"/> would push the member's
        /// credit spend in the current period beyond <paramref name="limit"/>.
        /// A <paramref name="limit"/> of 0 means "unlimited" and is never exceeded.
        /// The boundary is inclusive: spend exactly equal to the limit is allowed.
        /// </summary>
        public static bool IsExceeded(decimal periodSpend, decimal invoiceAmount, decimal limit)
        {
            if (limit == 0)
                return false;

            return periodSpend + invoiceAmount > limit;
        }
    }
}
