using BackOffice.UC;

namespace BackOffice.Model;

internal static class StockOpnameValidator
{
    public static bool TryValidate(
        DateTime transactionDate,
        IReadOnlyCollection<TransactionStockOpname> items,
        out string errorMessage)
    {
        if (transactionDate == default)
        {
            errorMessage = "Tanggal Stock Opname tidak valid.";
            return false;
        }

        if (items.Count == 0)
        {
            errorMessage = "Daftar barang Stock Opname masih kosong.";
            return false;
        }

        HashSet<string> itemCodes = new(StringComparer.OrdinalIgnoreCase);

        foreach (TransactionStockOpname item in items)
        {
            if (string.IsNullOrWhiteSpace(item.Kode_Item))
            {
                errorMessage = "Terdapat baris dengan kode barang kosong.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(item.ProductName))
            {
                errorMessage = $"Nama barang untuk kode {item.Kode_Item} kosong.";
                return false;
            }

            if (item.QtyFisik < 0)
            {
                errorMessage = $"Qty Fisik {item.ProductName} tidak boleh negatif.";
                return false;
            }

            if (item.Hpp < 0)
            {
                errorMessage = $"HPP {item.ProductName} tidak boleh negatif.";
                return false;
            }

            if (!itemCodes.Add(item.Kode_Item.Trim()))
            {
                errorMessage = $"Barang {item.Kode_Item} muncul lebih dari satu kali.";
                return false;
            }
        }

        errorMessage = string.Empty;
        return true;
    }
}
