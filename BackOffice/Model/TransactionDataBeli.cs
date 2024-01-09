using System.ComponentModel;

namespace BackOffice.UC
{
    public class TransactionDataBeli : INotifyPropertyChanged, IDataErrorInfo
    {
        private decimal qty;
        private decimal hpp;
        private decimal bruto;
        private decimal potongan;
        private decimal total;

        public int No { get; set; }
        public int ProductId { get; set; }
        public string Barcode { get; set; }
        public string Kode_Item { get; set; }
        public string ProductName { get; set; }
        public string Satuan { get; set; }
        public decimal Price { get; set; }


        public decimal Qty
        {
            get => qty;
            set
            {
                if (qty != value)
                {
                    qty = value;
                    UpdateBruto();
                    UpdateTotal();
                    OnPropertyChanged(nameof(Qty));
                }
            }
        }


        public decimal Hpp
        {
            get => hpp;
            set
            {
                if (hpp != value)
                {
                    hpp = value;
                    UpdateBruto();
                    UpdateTotal();
                    OnPropertyChanged(nameof(Price));
                }
            }
        }
        public decimal Bruto
        {
            get => bruto;
            set
            {
                if (bruto != value)
                {
                    bruto = value;
                    OnPropertyChanged(nameof(Bruto));
                }
            }
        }
        public decimal Potongan
        {
            get => potongan;
            set
            {
                if (potongan != value)
                {
                    potongan = value;
                    UpdateTotal();
                    OnPropertyChanged(nameof(Potongan));
                }
            }
        }

        public decimal Total
        {
            get => total;
            set
            {
                if (total != value)
                {
                    total = value;
                    OnPropertyChanged(nameof(Total));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void UpdateBruto()
        {
            Bruto = Qty * hpp;
        }
        public void UpdateTotal()
        {
            Total = (Qty * hpp) - Potongan;
        }

        // IDataErrorInfo implementation
        public string Error => null;

        public string this[string columnName] => ValidateColumn(columnName);

        private string ValidateColumn(string columnName)
        {
            string error = null;

            switch (columnName)
            {
                case nameof(Qty):
                    if (Qty < 0)
                        error = "QTY TIDAK BOLEH NEGATIVE.";
                    break;

                // Add additional validation for other columns if needed
                case nameof(Total):
                    if (Total < 0)
                        error = "TOTAL TIDAK BOLEH NEGATIVE.";
                    break;

                default:
                    break;
            }

            return error;
        }
    }
}
