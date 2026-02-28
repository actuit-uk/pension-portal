# pension-portal

ASP.NET Core MVC proof-of-concept — validates modernised pension calculation architecture on IONOS Windows hosting.

## Purpose

This is a smoke test for migrating the GMPEQ Classic ASP + T-SQL stored procedure architecture to ASP.NET Core MVC + C# calculation library. The domain is deliberately simple (people, dates of birth, age in complete months) to isolate infrastructure risk from domain complexity.

## What It Proves

- ASP.NET Core MVC runs on IONOS shared Windows hosting
- C# calculation library (CalcLib) executes server-side
- SQL Server connectivity via parameterised ADO.NET (no Entity Framework)
- Cookie-based authentication
- SQL injection protection via `SqlCommand` with `CommandType.StoredProcedure`
- CSV upload replaces `BULK INSERT` from filesystem
- CSV export for results download
- Markdown knowledge base replaces database-driven document catalogue
- DocFX replaces Sandcastle for API documentation
- GMP Equalisation UI: CalcLib integrated into web views with year-by-year cash flow and compensation grids

## Solution Structure

```
pension-portal/
├── PensionPortal.sln
├── src/
│   ├── PensionPortal.Web/              ← ASP.NET Core MVC app
│   ├── PensionPortal.CalcLib/          ← Calculation library
│   ├── PensionPortal.CalcLib.Export/   ← Excel export (ClosedXML)
│   └── PensionPortal.CalcLib.Tests/    ← CalcLib unit tests
├── sql/                                ← Database scripts
├── docs/                               ← DocFX documentation
└── data/                               ← Sample CSV files
```

## Prerequisites

- .NET 8.0 SDK
- SQL Server (local instance or IONOS)
- ActuarialData database (S148, PIP, discount rate factors for GMP equalisation)

## Getting Started

```bash
# Build
dotnet build

# Create database (requires sqlcmd)
sqlcmd -S localhost -i sql/01-CreateDB.sql
sqlcmd -S localhost -d PensionPortal -i sql/02-CreateObjects.sql

# Run
dotnet run --project src/PensionPortal.Web
```

## Technology

- **Framework:** ASP.NET Core 8.0 MVC
- **Database:** SQL Server via Microsoft.Data.SqlClient (raw ADO.NET)
- **UI:** Bootstrap 5 (CDN)
- **Documentation:** DocFX
- **Target hosting:** IONOS Windows shared hosting
