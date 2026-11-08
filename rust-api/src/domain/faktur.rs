use rust_decimal::Decimal;

/// Satu baris faktur setelah harga/diskon dihitung ulang oleh server.
#[derive(Debug, Clone, PartialEq)]
pub struct Baris {
    pub qty: Decimal,
    pub harga: Decimal,
    pub potongan: Decimal,
}

impl Baris {
    pub fn bruto(&self) -> Decimal {
        self.qty * self.harga
    }
    /// Sama dengan TransactionData.UpdateTotal: qty*harga - potongan.
    pub fn total(&self) -> Decimal {
        self.bruto() - self.potongan
    }
}

#[derive(Debug, Clone, PartialEq)]
pub struct Total {
    pub bruto: Decimal,
    pub potongan: Decimal,
    pub total: Decimal,
}

pub fn total_faktur(baris: &[Baris]) -> Total {
    Total {
        bruto: baris.iter().map(Baris::bruto).sum(),
        potongan: baris.iter().map(|b| b.potongan).sum(),
        total: baris.iter().map(Baris::total).sum(),
    }
}

/// Validasi qty seperti TransactionData: > 0 dan <= 1000.
pub fn qty_valid(qty: Decimal) -> bool {
    qty > Decimal::ZERO && qty <= Decimal::from(1000)
}

/// Aturan jual desktop: HPP harus > 0 dan harga jual harus > HPP.
pub fn harga_boleh_dijual(harga: Decimal, hpp: Decimal) -> Result<(), &'static str> {
    if hpp.is_zero() {
        Err("Hpp belum ditentukan")
    } else if harga <= hpp {
        Err("Harga jual <= harga beli")
    } else {
        Ok(())
    }
}

/// Header ANGSURAN desktop = floor(TOTAL / TENOR).
pub fn angsuran_header(total: Decimal, tenor: i32) -> Decimal {
    (total / Decimal::from(tenor.max(1))).floor()
}

/// Remise 1 untuk tanggal 1-15, selain itu remise 2.
pub fn remise_untuk_hari(hari: u32) -> u8 {
    if hari <= 15 { 1 } else { 2 }
}

#[cfg(test)]
mod tests {
    use super::*;
    use rust_decimal::dec;

    #[test]
    fn totals() {
        let b = [
            Baris {
                qty: dec!(5),
                harga: dec!(1000),
                potongan: dec!(500),
            },
            Baris {
                qty: dec!(2),
                harga: dec!(2500),
                potongan: dec!(0),
            },
        ];
        let t = total_faktur(&b);
        assert_eq!(
            (t.bruto, t.potongan, t.total),
            (dec!(10000), dec!(500), dec!(9500))
        );
    }

    #[test]
    fn sale_rules() {
        assert!(harga_boleh_dijual(dec!(11), dec!(10)).is_ok());
        assert!(harga_boleh_dijual(dec!(10), dec!(10)).is_err());
        assert!(harga_boleh_dijual(dec!(10), dec!(0)).is_err());
        assert!(qty_valid(dec!(1)) && !qty_valid(dec!(0)) && !qty_valid(dec!(1001)));
        assert_eq!(angsuran_header(dec!(100000), 3), dec!(33333));
        assert_eq!((remise_untuk_hari(15), remise_untuk_hari(16)), (1, 2));
    }
}
