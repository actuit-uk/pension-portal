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
- **`GmpResult`** — calculation output with pre/post-88 GMP splits for both sexes, plus `TaxYearDetails` audit trail
- **`TaxYearDetail`** — per-tax-year intermediate values (earnings, divisor, accrual rate, S148 factor, revalued earnings, raw GMP)
- **`CashFlowEntry`** — year-by-year pension projection tracking GMP components, excess pension, and total pension for both sexes with GMP status transitions (Exit → Deferred → InPayment)
- **`CompensationEntry`** — per-year equalisation compensation comparing actual vs opposite-sex total pension cashflows, with discount rate and factor for present-value reporting
- **`EqualisationResult`** — complete pipeline output: GMP result, cash flow projection, compensation entries, total compensation, interest on arrears, and total with interest
- **`SchemeConfig`** — scheme-level configuration: NRA ages, accrual rate, PIP method, GMP revaluation method, future assumptions, anti-franking flag
- Internal helpers: `WorkingLife`, `TaxYearGmp`, `Revaluation`, `CashFlowBuilder`, `CompensationCalculator`, `ExcessPensionCalculator`, `AntiFrankingCalculator`, `InterestCalculator`, `BarberWindow` — mirror the principles from the legacy GMPEQ stored procedures but as clean, tested C#

### Full Calculation Pipeline

`GmpCalculator.Calculate()` runs the complete equalisation pipeline in one call:

1. **GMP Calculation** — per-tax-year GMP from earnings, revalued to GMP payable age
2. **Cash Flow Projection** — year-by-year pension from leaving to projection end, with GMP and excess pension tracked separately. Post-88 GMP increases at LPI3 (statutory), excess increases at scheme PIP rate (LPI3 or LPI5)
3. **Anti-Franking** (optional) — applies Ring-Fence 90-97 floor to prevent excess erosion from GMP revaluation increases. No-op under separate increase method; material under overall increase method
4. **Compensation Calculation** — compares actual vs opposite-sex total pension (GMP + excess) from second PIP year onwards. Barber window proportions restrict comparison to equalisation-relevant service
5. **Interest on Arrears** (optional) — simple interest at BoE base rate + 1% on positive past compensation from each year to settlement date

The compensation uses the "separate increase" method: pre-88 GMP stays flat, post-88 GMP increases at CPI capped at 3% (the statutory GMP Increase Order rate), excess pension increases at scheme PIP rate. Compensation accrues from the second PIP year for each sex (the first PIP year establishes the base before any increases apply). Signed differences (C2-style) allow years where the actual sex is better to offset years where the opposite sex is better.

#### Excess Pension

Excess pension (total scheme pension minus GMP) uses a three-tier fallback:

1. **Direct `PensionAtLeaving`** — if the scheme provides total pension at leaving, excess = total minus GMP
2. **Salary-based estimate** — if `FinalPensionableSalary` is provided, total pension = salary × service ÷ accrual denominator
3. **GMP-only** — when neither is available, excess = 0 (valid for GMP-only schemes)

### Methodology Status

The engine covers the full C2 methodology: GMP calculation with Section 148 revaluation, excess pension, Barber window isolation, anti-franking, interest on arrears, and LPI3/LPI5 increase methods. Remaining gaps: FixedRate/LimitedRate revaluation (Issue #5), overall increase method (Issue #5). PASA reference PDFs are in `wwwroot/docs/`.

### Excel Export (CalcLib.Export)

The `PensionPortal.CalcLib.Export` project uses ClosedXML to produce multi-sheet Excel workbooks from `EqualisationResult`. Follows the appcore pattern of formatted tables with frozen panes and type-aware number formats. Sheets: Summary (with compensation totals and Barber window), Tax Year Detail (audit trail), GMP at Leaving, GMP Revalued, Cash Flow (year-by-year projection with GMP/excess/total for both sexes), Compensation (actual vs opposite-sex with discount factors, interest, and grand total).

## Deployment

The application is published with `dotnet publish -c Release` and deployed to IONOS via FTP. The CalcLib DLL is bundled in the published output — it runs on the web server, not as a separate service.
