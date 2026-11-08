use chrono::{Datelike, Months, NaiveDate};
use rust_decimal::Decimal;

#[derive(Debug, Clone, PartialEq)]
pub struct Angsuran {
    pub periode: i32,
    pub no_transaksi: String,
    pub jatuh_tempo: NaiveDate,
    pub ke: i32,
    pub saldo_awal: Decimal,
    pub angsuran: Decimal,
    pub saldo_akhir: Decimal,
}

fn akhir_bulan(d: NaiveDate) -> NaiveDate {
    let (y, m) = if d.month() == 12 {
        (d.year() + 1, 1)
    } else {
        (d.year(), d.month() + 1)
    };
    NaiveDate::from_ymd_opt(y, m, 1)
        .and_then(|n| n.pred_opt())
        .expect("tanggal valid")
}

/// Port `AngsuranCalculator.Calculate`: bagi rata (dibulatkan ke bawah), sisa ke angsuran terakhir,
/// jatuh tempo = akhir bulan ke-(i-1) setelah tanggal belanja, saldo berantai.
pub fn hitung(
    no_transaksi: &str,
    tanggal: NaiveDate,
    jumlah: Decimal,
    tenor: i32,
) -> Result<Vec<Angsuran>, String> {
    if tenor < 1 {
        return Err("tenor harus >= 1".into());
    }
    let mut saldo_awal = jumlah;
    let p = (saldo_awal / Decimal::from(tenor)).floor();
    let mut out = Vec::with_capacity(tenor as usize);
    for i in 1..=tenor {
        let bulan = tanggal
            .checked_add_months(Months::new((i - 1) as u32))
            .ok_or("tanggal di luar jangkauan")?;
        let jt = akhir_bulan(bulan);
        let bayar = if i == tenor { saldo_awal } else { p };
        let saldo_akhir = saldo_awal - bayar;
        out.push(Angsuran {
            periode: jt.year() * 100 + jt.month() as i32,
            no_transaksi: no_transaksi.to_string(),
            jatuh_tempo: jt,
            ke: i,
            saldo_awal: saldo_awal.round_dp(2),
            angsuran: bayar.round_dp(2),
            saldo_akhir: saldo_akhir.round_dp(2),
        });
        saldo_awal = saldo_akhir;
    }
    Ok(out)
}

#[cfg(test)]
mod tests {
    use super::*;
    use rust_decimal::dec;

    const NO: &str = "W-26-000123";
    fn d(y: i32, m: u32, dd: u32) -> NaiveDate {
        NaiveDate::from_ymd_opt(y, m, dd).unwrap()
    }

    #[test]
    fn single_tenor_one_paid_off_row() {
        let rows = hitung(NO, d(2026, 6, 10), dec!(100000), 1).unwrap();
        assert_eq!(rows.len(), 1);
        let r = &rows[0];
        assert_eq!(r.ke, 1);
        assert_eq!(r.periode, 202606);
        assert_eq!(r.jatuh_tempo, d(2026, 6, 30));
        assert_eq!(r.saldo_awal, dec!(100000));
        assert_eq!(r.angsuran, dec!(100000));
        assert_eq!(r.saldo_akhir, Decimal::ZERO);
        assert_eq!(r.no_transaksi, NO);
    }

    #[test]
    fn even_split() {
        let rows = hitung(NO, d(2026, 6, 10), dec!(400000), 4).unwrap();
        assert_eq!(rows.len(), 4);
        assert!(rows.iter().all(|r| r.angsuran == dec!(100000)));
        assert_eq!(
            rows.iter().map(|r| r.angsuran).sum::<Decimal>(),
            dec!(400000)
        );
        assert_eq!(rows.last().unwrap().saldo_akhir, Decimal::ZERO);
    }

    #[test]
    fn non_divisible_last_absorbs_remainder() {
        let rows = hitung(NO, d(2026, 6, 10), dec!(100000), 3).unwrap();
        assert_eq!(rows[0].angsuran, dec!(33333));
        assert_eq!(rows[1].angsuran, dec!(33333));
        assert_eq!(rows[2].angsuran, dec!(33334));
        assert_eq!(
            rows.iter().map(|r| r.angsuran).sum::<Decimal>(),
            dec!(100000)
        );
        assert_eq!(rows[2].saldo_akhir, Decimal::ZERO);
    }

    #[test]
    fn due_dates_roll_over_month_and_year() {
        let rows = hitung(NO, d(2026, 11, 15), dec!(300000), 3).unwrap();
        assert_eq!(rows[0].jatuh_tempo, d(2026, 11, 30));
        assert_eq!(rows[1].jatuh_tempo, d(2026, 12, 31));
        assert_eq!(rows[2].jatuh_tempo, d(2027, 1, 31));
        assert_eq!(
            [rows[0].periode, rows[1].periode, rows[2].periode],
            [202611, 202612, 202701]
        );
    }

    #[test]
    fn due_date_clamps_to_short_month() {
        let rows = hitung(NO, d(2026, 1, 31), dec!(200000), 2).unwrap();
        assert_eq!(rows[0].jatuh_tempo, d(2026, 1, 31));
        assert_eq!(rows[1].jatuh_tempo, d(2026, 2, 28));
    }

    #[test]
    fn saldo_chain_continuous() {
        let rows = hitung(NO, d(2026, 6, 10), dec!(100000), 3).unwrap();
        assert_eq!(rows[0].saldo_awal, dec!(100000));
        for w in rows.windows(2) {
            assert_eq!(w[0].saldo_akhir, w[1].saldo_awal);
        }
        for r in &rows {
            assert_eq!(r.saldo_awal - r.angsuran, r.saldo_akhir);
        }
        assert_eq!(rows.last().unwrap().saldo_akhir, Decimal::ZERO);
    }

    #[test]
    fn always_sum_to_principal() {
        for (jumlah, tenor) in [
            (100_000, 1),
            (100_000, 3),
            (250_000, 6),
            (1_000_000, 12),
            (99_999, 7),
            (123_457, 11),
        ] {
            let rows = hitung(NO, d(2026, 6, 10), Decimal::from(jumlah), tenor).unwrap();
            assert_eq!(rows.len(), tenor as usize);
            assert_eq!(
                rows.iter().map(|r| r.angsuran).sum::<Decimal>(),
                Decimal::from(jumlah)
            );
            assert_eq!(rows.last().unwrap().saldo_akhir, Decimal::ZERO);
            for (i, r) in rows.iter().enumerate() {
                assert_eq!(r.ke, i as i32 + 1);
                assert_eq!(r.no_transaksi, NO);
            }
        }
    }

    #[test]
    fn tenor_invalid_rejected() {
        assert!(hitung(NO, d(2026, 6, 10), dec!(1000), 0).is_err());
    }
}
