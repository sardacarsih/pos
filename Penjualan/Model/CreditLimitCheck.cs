namespace Penjualan.Model
{
    public class CreditLimitCheck
    {
        public string NIK { get; set; }
        // Member billing status (e.g. "BULANAN") — selects the accounting-period window.
        public string STATUS { get; set; }
        // Faktur date — used to resolve the current period window from POS_PERIODE.
        public DateTime TransactionDate { get; set; }
        public decimal InvoiceAmount { get; set; }
    }

    public class CreditLimitExceededException : Exception
    {
        public decimal CurrentDebt { get; }
        public decimal InvoiceAmount { get; }
        public decimal Limit { get; }

        public CreditLimitExceededException(decimal currentDebt, decimal invoiceAmount, decimal limit)
            : base($"Credit limit exceeded. Debt: {currentDebt:N0}, Invoice: {invoiceAmount:N0}, Limit: {limit:N0}")
        {
            CurrentDebt = currentDebt;
            InvoiceAmount = invoiceAmount;
            Limit = limit;
        }
    }
}
