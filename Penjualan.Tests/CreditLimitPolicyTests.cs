using Penjualan.BusinessLayer;
using Xunit;

namespace Penjualan.Tests
{
    public class CreditLimitPolicyTests
    {
        // periodSpend, invoiceAmount, limit, expectedExceeded
        [Theory]
        // --- under the limit ---
        [InlineData(100, 50, 200, false)]   // room to spare
        [InlineData(0, 200, 200, false)]    // first purchase, lands exactly on the limit
        [InlineData(0, 199, 200, false)]    // first purchase, just under
        // --- boundary is inclusive (equal is allowed) ---
        [InlineData(150, 50, 200, false)]   // spend + invoice == limit
        // --- over the limit ---
        [InlineData(150, 51, 200, true)]    // over by 1
        [InlineData(0, 201, 200, true)]     // first purchase already over
        [InlineData(200, 1, 200, true)]     // no remaining headroom
        // --- existing spend already exceeds, even a zero invoice ---
        [InlineData(250, 0, 200, true)]
        public void IsExceeded_PerPeriodCap(decimal periodSpend, decimal invoiceAmount, decimal limit, bool expected)
        {
            Assert.Equal(expected, CreditLimitPolicy.IsExceeded(periodSpend, invoiceAmount, limit));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(5_000_000)]
        [InlineData(999_999_999)]
        public void IsExceeded_LimitZeroMeansUnlimited(decimal invoiceAmount)
        {
            // A limit of 0 is "unlimited" — never exceeded regardless of spend/invoice.
            Assert.False(CreditLimitPolicy.IsExceeded(1_000_000_000m, invoiceAmount, limit: 0));
        }

        [Fact]
        public void IsExceeded_HandlesDecimalFractions()
        {
            // 100.50 + 99.49 = 199.99 <= 200.00  -> allowed
            Assert.False(CreditLimitPolicy.IsExceeded(100.50m, 99.49m, 200.00m));
            // 100.50 + 99.51 = 200.01 > 200.00   -> exceeded
            Assert.True(CreditLimitPolicy.IsExceeded(100.50m, 99.51m, 200.00m));
        }
    }
}
