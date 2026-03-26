namespace Penjualan.Model
{
    public class CreditLimitCheck
    {
        public string NIK { get; set; }
        public string STATUS { get; set; }
        public decimal Limit { get; set; }
        public decimal InvoiceAmount { get; set; }
        public DateTime PeriodFrom { get; set; }
        public DateTime PeriodTo { get; set; }
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
