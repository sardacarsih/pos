//! Port `ucPinjaman.CalculateAngsuranPinjaman` (BackOffice): anuitas, bunga = saldo x rate per bulan,
//! Pmt dihitung ulang dengan sisa tenor, jatuh tempo akhir bulan mulai bulan pinjaman. Hitungan double
//! seperti C# (Financial.Pmt), dibulatkan 2 desimal per baris.
use chrono::{Datelike, Months, NaiveDate};
use rust_decimal::{Decimal, prelude::{FromPrimitive, ToPrimitive}};

/// JSON sebagai angka (bukan string) agar klien web/mobile langsung bisa menghitung.
pub fn angka<S: serde::Serializer>(d: &Decimal, s: S) -> Result<S::Ok, S::Error> {
    s.serialize_f64(d.to_f64().unwrap_or_default())
}

#[derive(Debug, Clone, PartialEq, serde::Serialize)]
pub struct Baris {
    pub periode: i32,
    pub jatuh_tempo: NaiveDate,
    pub ke: i32,
    #[serde(serialize_with = "angka")]
    pub saldo_awal: Decimal,
    #[serde(serialize_with = "angka")]
    pub pokok: Decimal,
    #[serde(serialize_with = "angka")]
    pub bunga: Decimal,
    #[serde(serialize_with = "angka")]
    pub angsuran: Decimal,
    #[serde(serialize_with = "angka")]
    pub saldo_akhir: Decimal,
}

/// `Financial.Pmt(rate, n, -pv, 0, EndOfPeriod)`.
pub fn pmt(rate: f64, n: i32, pv: f64) -> f64 {
    if rate == 0.0 {
        pv / n as f64
    } else {
        pv * rate / (1.0 - (1.0 + rate).powi(-n))
    }
}

fn d2(v: f64) -> Decimal {
    Decimal::from_f64(v).unwrap_or_default().round_dp(2)
}

fn akhir_bulan(d: NaiveDate) -> NaiveDate {
    let awal = NaiveDate::from_ymd_opt(d.year(), d.month(), 1).expect("tanggal valid");
    (awal + Months::new(1)).pred_opt().expect("tanggal valid")
}

/// `bunga_persen` per bulan (mis. 1.6). Error bila tenor < 1 atau jumlah <= 0.
pub fn jadwal(tanggal: NaiveDate, jumlah: i64, tenor: i32, bunga_persen: f64) -> Result<Vec<Baris>, String> {
    if tenor < 1 || jumlah <= 0 || !(0.0..100.0).contains(&bunga_persen) {
        return Err("jumlah, tenor, atau bunga tidak valid".into());
    }
    let r = bunga_persen / 100.0;
    let mut saldo = jumlah as f64;
    let mut out = Vec::with_capacity(tenor as usize);
    for i in 0..tenor {
        let jt = akhir_bulan(tanggal.checked_add_months(Months::new(i as u32)).ok_or("tanggal di luar jangkauan")?);
        let bunga = d2(saldo * r);
        let angsuran = pmt(r, tenor - i, saldo);
        let pokok = (d2(angsuran) - bunga).round_dp(2);
        let akhir = saldo - f64::try_from(pokok).unwrap_or_default();
        out.push(Baris {
            periode: jt.year() * 100 + jt.month() as i32,
            jatuh_tempo: jt,
            ke: i + 1,
            saldo_awal: d2(saldo),
            pokok,
            bunga,
            angsuran: d2(angsuran),
            saldo_akhir: d2(akhir),
        });
        saldo = akhir;
    }
    Ok(out)
}

#[cfg(test)]
mod tests {
    use super::*;
    use rust_decimal::dec;

    fn d(y: i32, m: u32, dd: u32) -> NaiveDate {
        NaiveDate::from_ymd_opt(y, m, dd).unwrap()
    }

    #[test]
    fn pmt_sama_dengan_excel() {
        // =PMT(1.6%;12;-10000000) = 922.520,56 (dibulatkan)
        assert_eq!(d2(pmt(0.016, 12, 10_000_000.0)), dec!(922520.56));
        assert_eq!(pmt(0.0, 4, 1000.0), 250.0);
    }

    #[test]
    fn jadwal_anuitas_lunas_dan_tetap() {
        let rows = jadwal(d(2026, 11, 18), 10_000_000, 12, 1.6).unwrap();
        assert_eq!(rows.len(), 12);
        assert_eq!(rows[0].bunga, dec!(160000.00));
        assert_eq!(rows[0].angsuran, dec!(922520.56));
        assert_eq!(rows[0].pokok, dec!(762520.56));
        assert_eq!(rows[0].jatuh_tempo, d(2026, 11, 30));
        assert_eq!(rows[1].periode, 202612);
        assert_eq!(rows[2].jatuh_tempo, d(2027, 1, 31));
        assert!(rows.iter().all(|r| (r.angsuran - dec!(922520.56)).abs() <= dec!(0.02)));
        assert!(rows.last().unwrap().saldo_akhir.abs() <= dec!(0.05));
        for w in rows.windows(2) {
            assert_eq!(w[0].saldo_akhir, w[1].saldo_awal);
        }
    }

    #[test]
    fn input_tidak_valid() {
        assert!(jadwal(d(2026, 1, 1), 0, 12, 1.6).is_err());
        assert!(jadwal(d(2026, 1, 1), 1000, 0, 1.6).is_err());
    }
}
