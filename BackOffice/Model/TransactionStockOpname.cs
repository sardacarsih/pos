using System.ComponentModel;

namespace BackOffice.UC
{
    public class TransactionStockOpname : INotifyPropertyChanged
    {
        private decimal qtySystem;
        private decimal qtyFisik;
        private decimal selisih;
        private decimal hpp;
        private decimal total;

        public string Nomor_SO { get; set; }
        public DateTime Tanggal { get; set; }
        public int No { get; set; }
        public int ProductId { get; set; }
        public string Barcode { get; set; }
        public string Kode_Item { get; set; }
        public string ProductName { get; set; }
        public string Satuan { get; set; }

        public decimal QtySystem
        {
            get => qtySystem;
            set
            {
                if (qtySystem != value)
                {
                    qtySystem = value;
                    UpdateSelisih();
                    OnPropertyChanged(nameof(QtySystem));
                }
            }
        }

        public decimal QtyFisik
        {
            get => qtyFisik;
            set
            {
                if (qtyFisik != value)
                {
                    qtyFisik = value;
                    UpdateSelisih();
                    UpdateTotal();
                    OnPropertyChanged(nameof(QtyFisik));
                }
            }
        }
        
        public decimal Selisih
        {
            get => selisih;
            set
            {
                if (selisih != value)
                {
                    selisih = value;
                    UpdateTotal();
                    OnPropertyChanged(nameof(Selisih));
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
                    UpdateTotal();
                    OnPropertyChanged(nameof(Hpp));
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
                    UpdateTotal();
                    OnPropertyChanged(nameof(Total));
                }
            }
        }
        private void UpdateSelisih()
        {
            selisih = QtyFisik - QtySystem;
            OnPropertyChanged(nameof(Selisih));
        }
        private void UpdateTotal()
        {
            total = selisih * hpp;
            OnPropertyChanged(nameof(Total));
        }



        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}
