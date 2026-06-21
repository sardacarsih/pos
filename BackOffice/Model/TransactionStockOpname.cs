using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BackOffice.UC;

public class TransactionStockOpname : INotifyPropertyChanged
{
    private decimal _qtySystem;
    private decimal _qtyFisik;
    private decimal _selisih;
    private decimal _hpp;
    private decimal _total;

    public string Nomor_SO { get; set; } = string.Empty;
    public DateTime Tanggal { get; set; }
    public int No { get; set; }
    public int ProductId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string Kode_Item { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Satuan { get; set; } = string.Empty;

    public decimal QtySystem
    {
        get => _qtySystem;
        set
        {
            if (_qtySystem == value)
            {
                return;
            }

            _qtySystem = value;
            OnPropertyChanged();
            Recalculate();
        }
    }

    public decimal QtyFisik
    {
        get => _qtyFisik;
        set
        {
            if (_qtyFisik == value)
            {
                return;
            }

            _qtyFisik = value;
            OnPropertyChanged();
            Recalculate();
        }
    }

    public decimal Selisih => _selisih;

    public decimal Hpp
    {
        get => _hpp;
        set
        {
            if (_hpp == value)
            {
                return;
            }

            _hpp = value;
            OnPropertyChanged();
            Recalculate();
        }
    }

    public decimal Total => _total;

    private void Recalculate()
    {
        decimal newSelisih = _qtyFisik - _qtySystem;
        decimal newTotal = newSelisih * _hpp;

        if (_selisih != newSelisih)
        {
            _selisih = newSelisih;
            OnPropertyChanged(nameof(Selisih));
        }

        if (_total != newTotal)
        {
            _total = newTotal;
            OnPropertyChanged(nameof(Total));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
