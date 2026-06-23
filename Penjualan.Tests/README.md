# Penjualan.Tests

Tests for the credit-limit ("limit hutang") feature.

## Unit tests (always run)
Pure logic, no external dependencies:
- `CreditLimitPolicyTests` — the per-period limit verdict (`CreditLimitPolicy.IsExceeded`),
  including the inclusive boundary, `limit == 0` = unlimited, and decimal fractions.
- `AngsuranCalculatorTests` — the installment schedule (`AngsuranCalculator.Calculate`):
  even split, remainder absorbed by the last installment, due-date month/year rollover and
  short-month clamping, continuous saldo chain, and "installments always sum to principal".

```
dotnet test Penjualan.Tests/Penjualan.Tests.csproj
```

## Integration tests (opt-in, DB-backed)
`Integration/CreditLimitValidationIntegrationTests` exercise
`FakturPenjualan.ValidateCreditLimit` end-to-end against a **real Oracle schema** — the
actual SQL (TO_DATE/NLS window resolution from `POS_PERIODE`, `NVL/SUM/BETWEEN` over
`POS_PENJUALAN`, `FIN_ANGGOTA ... FOR UPDATE`) plus the period computation and policy verdict.

They are **skipped** unless `POS_TEST_ORACLE` is set, so the suite stays green without a DB.

### Running them
Point the variable at a **disposable / empty** Oracle schema (the fixture creates the
minimal `FIN_ANGGOTA`, `POS_PERIODE`, `POS_PENJUALAN` tables and drops them afterwards; it
refuses to run if those tables already exist, to avoid clobbering real data):

```powershell
$env:POS_TEST_ORACLE = "User Id=postest;Password=...;Data Source=localhost:1521/XEPDB1"
dotnet test Penjualan.Tests/Penjualan.Tests.csproj
```

Cases covered: under/at/over limit, `limit = 0` unlimited, only in-window spend counts,
BULANAN vs remise window selection, missing-period bypass, and the member-row lock
(`FOR UPDATE`) serializing concurrent validations (ORA-00054).
