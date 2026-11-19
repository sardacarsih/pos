use rust_decimal::Decimal;

/// Port `CreditLimitPolicy.IsExceeded`. Limit 0 = tidak dibatasi; batas inklusif (sama dengan limit masih boleh).
pub fn is_exceeded(period_spend: Decimal, invoice_amount: Decimal, limit: Decimal) -> bool {
    if limit.is_zero() {
        return false;
    }
    period_spend + invoice_amount > limit
}

#[cfg(test)]
mod tests {
    use super::*;
    use rust_decimal::dec;

    #[test]
    fn per_period_cap() {
        // (periodSpend, invoice, limit, expected) - identik dengan CreditLimitPolicyTests.cs
        let cases = [
            (100, 50, 200, false),
            (0, 200, 200, false),
            (0, 199, 200, false),
            (150, 50, 200, false),
            (150, 51, 200, true),
            (0, 201, 200, true),
            (200, 1, 200, true),
            (250, 0, 200, true),
        ];
        for (s, i, l, e) in cases {
            assert_eq!(is_exceeded(s.into(), i.into(), l.into()), e, "{s} {i} {l}");
        }
    }

    #[test]
    fn limit_zero_means_unlimited() {
        for inv in [0, 5_000_000, 999_999_999] {
            assert!(!is_exceeded(dec!(1_000_000_000), inv.into(), Decimal::ZERO));
        }
    }

    #[test]
    fn handles_decimal_fractions() {
        assert!(!is_exceeded(dec!(100.50), dec!(99.49), dec!(200.00)));
        assert!(is_exceeded(dec!(100.50), dec!(99.51), dec!(200.00)));
    }
}
