# Architecture

## Request Flow

```
Browser → ASP.NET Core MVC → Controller → DatabaseService → SQL Server
                                    ↓
                               CalcLib (C#)
                                    ↓
                            DatabaseService → SQL Server (save results)
```

## Key Design Decisions

### Thin Stored Procedures

Stored procedures handle CRUD operations only. All business logic lives in C# (CalcLib). This means:

- Calculations are testable with unit tests
- Business logic is version-controlled as C# code
- SQL Server handles what it's good at: storage and retrieval

### DatabaseService Pattern

A single `DatabaseService` class wraps all database access. Every call uses `SqlCommand` with `CommandType.StoredProcedure` and `SqlParameter` objects. String concatenation of SQL is never used.

### Cookie Authentication

ASP.NET Core's built-in cookie authentication middleware replaces the legacy cookie-based system. The `[Authorize]` attribute on controllers provides declarative access control.

### Factor Table Lookup

The factor table (`tblAgeFactor`) maps age in months to a factor value. The C# code reads this table into memory, performs the calculation, and looks up the factor — proving the pattern needed for the real GMP system where actuarial tables drive calculations.

### GMP Calculation Engine (CalcLib)

The GMP Equalisation calculation is implemented as pure, static C# methods in CalcLib with no database dependency. Factor data (S148 earnings orders, fixed revaluation rates, PIP increases, discount rates) is injected via the `IFactorProvider` interface, keeping the calculation logic fully testable.

Key types:

- **`GmpCalculator`** — public entry point for GMP and equalisation calculations
- **`MemberData`** — immutable record of member inputs (sex, DOB, earnings history)
- **`IFactorProvider`** / **`DictionaryFactorProvider`** — factor abstraction with in-memory implementation
- **`GmpResult`** — calculation output with pre/post-88 GMP splits for both sexes
- Internal helpers: `WorkingLife`, `TaxYearGmp`, `Revaluation` — mirror the principles from the legacy GMPEQ stored procedures but as clean, tested C#

## Deployment

The application is published with `dotnet publish -c Release` and deployed to IONOS via FTP. The CalcLib DLL is bundled in the published output — it runs on the web server, not as a separate service.
