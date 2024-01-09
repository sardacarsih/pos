using System.ComponentModel;

namespace BackOffice.UC
{
    public class TransactionBarangRusak : INotifyPropertyChanged
    {
        
        private decimal qtyFisik;
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


        public decimal QtyFisik
        {
            get => qtyFisik;
            set
            {
                if (qtyFisik != value)
                {
                    qtyFisik = value;
                    UpdateTotal();
                    OnPropertyChanged(nameof(QtyFisik));
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

        private void UpdateTotal()
        {
            total = QtyFisik * Hpp;
            OnPropertyChanged(nameof(Total));
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

        public string Keterangan { get; set; }

       

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}
