# Point of Sales (POS) - Kopkar Kusuma Lestari

A WinForms-based Point of Sales application built with .NET 8.0 and Oracle Database for cooperative store (koperasi) management.

## Projects

| Project | Description |
|---------|-------------|
| **Penjualan** | Cashier / POS terminal for daily sales transactions |
| **BackOffice** | Back-office management: purchasing, inventory, finance, reports, and master data |

## Tech Stack

- **.NET 8.0** - Windows Forms (WinForms)
- **Oracle Database** - via Oracle.ManagedDataAccess.Core
- **Dapper** - Lightweight ORM for data access
- **DevExpress WinForms** - UI components (grids, editors, reports)
- **QRCoder** - QR code generation for receipts

## Architecture

```
├── BusinessLayer/      # Service facade (POS_Services, Tools_Services)
├── DataLayer/          # Database repositories (Dapper + OracleCommand)
├── Interface/          # Repository interfaces
├── Model/              # DTOs and data models
├── UC/                 # UserControls (main UI screens)
├── Laporan/            # DevExpress XtraReports (receipts, reports)
└── Program.cs          # Application entry point
```

### Sales Flow (Penjualan)

1. **Barcode Scan** - Scan or search for products
2. **Cart Management** - Add items, adjust quantities, apply quantity-based discounts
3. **Payment** - Select customer, choose payment type (cash/credit), installment options
4. **Invoice Save** - Atomic transaction with credit limit validation
5. **Receipt Print** - Auto-print receipt via DevExpress XtraReports

### BackOffice Modules

- **Penjualan** - View/edit sales history, return processing
- **Pembelian** - Purchase order management, supplier tracking
- **Persediaan** - Inventory, stock opname, damaged goods
- **Finance** - Accounts receivable, installment tracking, loans, period closing
- **Master Data** - Products, members/customers, settings, periods

## Database

Oracle Database with key tables:

- `POS_PRODUCT` - Product master
- `POS_PENJUALAN` / `POS_PENJUALAN_DETAIL` - Sales transactions
- `POS_PEMBELIAN` / `POS_PEMBELIANDETAIL` - Purchase transactions
- `POS_STOCK` - Stock balances
- `POS_PERIODE` - Accounting periods with remise dates
- `FIN_ANGGOTA` - Members/customers with credit limits
- `POS_KREDIT_ANGSURAN` - Credit installment schedules
- `NOMOR_TRANSAKSI` - Atomic transaction number generation

## Getting Started

### Prerequisites

- .NET 8.0 SDK
- Oracle Database
- DevExpress WinForms license (v24.1+)

### Configuration

The real `appsettings.json` files hold DB credentials and are **gitignored**.
Each project ships an `appsettings.example.json` template — copy it and fill in
your Oracle connection:

```bash
cp Penjualan/appsettings.example.json  Penjualan/appsettings.json
cp BackOffice/appsettings.example.json BackOffice/appsettings.json
cp Migrator/appsettings.example.json   Migrator/appsettings.json
```

```json
{
  "ConnectionStrings": {
    "OracleConnection": "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=your_host)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=your_service)));User Id=your_user;Password=your_password;"
  }
}
```

### Database setup (migrations + admin)

Run the migrator once against your Oracle instance. It creates the login/RBAC
tables (`POS_APP`, `POS_ROLE`, `POS_USER`, `POS_USER_ACCESS`), tracks applied
migrations in `POS_SCHEMA_MIGRATION` (idempotent — safe to re-run), and seeds a
default `admin` user with the `ADMIN` role on all apps:

```bash
# uses Migrator/appsettings.json, or pass --connection / set POS_CONNECTION
dotnet run --project Migrator -- --admin-password "ChangeMe#1"

dotnet run --project Migrator -- --skip-admin   # schema only, no admin
dotnet run --project Migrator -- --help
```

> The default admin password is `Admin#2026` if `--admin-password` is omitted —
> change it immediately via **BackOffice → Manajemen User**.

Logins are validated against these tables (PBKDF2-SHA256 hashes); there is no
hardcoded account. Each user is granted a role **per app** (Penjualan,
BackOffice, Pembelian).

### Build & Run

```bash
dotnet build
dotnet run --project Penjualan    # Run the cashier app
dotnet run --project BackOffice   # Run the back-office app
```

## License

Proprietary - Internal use only.
