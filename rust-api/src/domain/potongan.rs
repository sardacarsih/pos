use rust_decimal::Decimal;

/// Diskon qty desktop (HitungNilaiPotongan): bila MINQTY > 0 dan qty >= MINQTY,
/// potongan = qty * POTONGAN (per unit). Selain itu 0.
pub fn diskon_qty(qty: Decimal, min_qty: i64, potongan_per_unit: Decimal) -> Decimal {
    if min_qty > 0 && qty >= Decimal::from(min_qty) {
        qty * potongan_per_unit
    } else {
        Decimal::ZERO
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use rust_decimal::dec;

    #[test]
    fn rules() {
        assert_eq!(diskon_qty(dec!(5), 5, dec!(100)), dec!(500));
        assert_eq!(diskon_qty(dec!(4), 5, dec!(100)), Decimal::ZERO);
        assert_eq!(diskon_qty(dec!(9), 0, dec!(100)), Decimal::ZERO);
    }
}
