# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

WinForms POS for a cooperative store (Kopkar Kusuma Lestari): .NET 8, Oracle (`Oracle.ManagedDataAccess.Core` + Dapper), DevExpress WinForms 24.1. UI text, comments and identifiers are largely Indonesian. See `README.md` for the table overview and sales flow.

`POS.sln` contains four projects:
- `Penjualan/` — cashier terminal (also hosts the credit-limit logic).
- `BackOffice/` — purchasing, inventory, finance, reports, master data.
- `Migrator/` — console tool that applies forward-only Oracle migrations (login/RBAC schema).
- `Penjualan.Tests/` — xUnit tests (despite the name, only covers Penjualan).

## Commands

```bash
dotnet build
dotnet run --project Penjualan
dotnet run --project BackOffice
dotnet run --project Migrator -- --admin-password "..."   # or --skip-admin, --help
dotnet test Penjualan.Tests/Penjualan.Tests.csproj
dotnet test Penjualan.Tests/Penjualan.Tests.csproj --filter "FullyQualifiedName~CreditLimitPolicyTests"   # single class/test
```

- Releases: pushing a `v*` tag triggers `.github/workflows/release.yml` (publishes both apps, zips, creates a GitHub release). No CI build/test on PRs.
- DB-backed integration tests (`Penjualan.Tests/Integration`) are skipped unless `POS_TEST_ORACLE` is set; point it at a disposable schema (the fixture creates and drops `FIN_ANGGOTA`, `POS_PERIODE`, `POS_PENJUALAN` and refuses to run if they exist).
- `appsettings.json` (DB credentials, gitignored) must be created from each project's `appsettings.example.json` — Penjualan, BackOffice, Migrator.

## Architecture

Each app uses the same layering: `UC/` UserControls (screens) → `BusinessLayer/` (`POS_Services`, `Tools_Services` facades) → `DataLayer/` repositories (Dapper/`OracleCommand`) behind `Interface/`, with DTOs in `Model/`. BackOffice additionally has `Controller/` classes per module and `BussinessLayer/` (sic, double-s) services. Reports are DevExpress XtraReports under `Laporan/`.

Cross-project sharing is by **linked source files, not a library**: `Shared/Auth/*.cs` and `Shared/LoginVisuals.cs` are compiled into Penjualan (and BackOffice/Migrator) via `<Compile Include="..\Shared\..." Link=...>`. Edit them in `Shared/`, never in a copy. Each app's `Frmlogin` authenticates against `POS_USER`/`POS_ROLE`/`POS_USER_ACCESS` with per-app roles (`AppIds`), PBKDF2-SHA256 hashes; there is no hardcoded account.

Schema changes go in `Migrator/Migrations.cs` as new forward-only entries with numeric-prefix ids (`0001_...`). DDL is wrapped by `Ddl(...)` to swallow "already exists" errors, so migrations must stay idempotent. `db/login_schema.sql` is the raw reference.

### Credit limit (limit hutang) — locked decisions

`FIN_ANGGOTA.LIMIT_HUTANG` is a **per-period spending cap**, not a cumulative outstanding balance. Enforced in `Penjualan/DataLayer/FakturPenjualan.cs` `ValidateCreditLimit`, which locks the member row (`FOR UPDATE`) and sums `POS_PENJUALAN.TOTAL` within the current `POS_PERIODE` window (BULANAN → `BDARI..BSAMPAI`, else `R1DARI..R2SAMPAI`). Pure verdict logic lives in `BusinessLayer/CreditLimitPolicy.cs`. Do not change these without re-confirming with the user:
- Full faktur TOTAL counts (down payment is not netted); tenor=1 credit sales count.
- Resets per month window, even for remise members.
- `LIMIT_HUTANG = 0` means unlimited, not "no credit".
- The period is found from the **date range** (`BDARI..BSAMPAI` containing the transaction date), never from the calendar month. Same lookup drives open/closed status and the limit window (`Shared/PeriodeGuard.cs` linked into Penjualan and BackOffice, rust-api `periode_untuk_tanggal`; keep them in sync). A date outside every range = "periode belum diatur" and blocks the sale.
- Do not revert to a `POS_KREDIT_ANGSURAN` outstanding-balance model. Installment schedules come from `BusinessLayer/AngsuranCalculator.cs`.

### BackOffice grid theming

`BackOffice/UI/BackOfficeTheme.cs` styles every grid view globally. Columns stretch to fill width by default; a grid opts out (fixed widths + horizontal scrollbar) by setting `gridControl.Tag = "ui-fixed-columns"`.
