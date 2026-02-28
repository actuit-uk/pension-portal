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

Two pension increase methods are supported (configured via `SchemeConfig.IncreaseMethod`):

- **Separate** (default): each component increases independently — pre-88 GMP stays flat, post-88 GMP increases at LPI3 (statutory), excess increases at scheme PIP rate. Anti-franking is a no-op because excess is never reduced by GMP increases.
- **Overall**: the scheme applies one rate to the total pension, then tests the GMP floor (pre-88 flat + post-88 at LPI3). Excess is the residual and can erode to zero if the scheme rate is lower than the effective GMP increase rate. Anti-franking becomes material under this method.

Compensation accrues from the second PIP year for each sex (the first PIP year establishes the base before any increases apply). Signed differences (C2-style) allow years where the actual sex is better to offset years where the opposite sex is better.

#### Excess Pension

Excess pension (total scheme pension minus GMP) uses a three-tier fallback:

1. **Direct `PensionAtLeaving`** — if the scheme provides total pension at leaving, excess = total minus GMP
2. **Salary-based estimate** — if `FinalPensionableSalary` is provided, total pension = salary × service ÷ accrual denominator
3. **GMP-only** — when neither is available, excess = 0 (valid for GMP-only schemes)

### EarningsEstimator

`EarningsEstimator.Estimate()` synthesises a complete `MemberData` record from minimal inputs (DOB, sex, join/leave dates, ~1990 salary). Designed for small schemes with patchy records that lack per-year earnings histories.

The estimator inflates/deflates the 1990 salary anchor using the ONS average weekly earnings index, then converts each tax year's estimated salary to the appropriate format:
- Pre-1988: NI contributions (band earnings × `TaxYearGmp.NiDivisor`)
- Post-1988: band earnings directly

An optional `salaryMargin` parameter allows upper-bound sensitivity testing. `FinalPensionableSalary` is set from the inflated salary at leaving, enabling the tier-2 excess pension fallback.

Reference data (LEL, UEL, earnings index) is hardcoded in `Internal/NiThresholds.cs` — fixed historical HMRC/ONS data that never varies between cases.

### Methodology Status

The engine covers the full C2 methodology: GMP calculation with all three revaluation methods (Section 148, FixedRate, LimitedRate), excess pension, Barber window isolation, anti-franking, interest on arrears, separate and overall pension increase methods, and LPI3/LPI5 increase rates. The EarningsEstimator enables minimal-data members to use the full pipeline. PASA reference PDFs are in `wwwroot/docs/`.

### Excel Export (CalcLib.Export)

The `PensionPortal.CalcLib.Export` project uses ClosedXML to produce multi-sheet Excel workbooks from `EqualisationResult`. Follows the appcore pattern of formatted tables with frozen panes and type-aware number formats. Sheets: Summary (with compensation totals and Barber window), Tax Year Detail (audit trail), GMP at Leaving, GMP Revalued, Cash Flow (year-by-year projection with GMP/excess/total for both sexes), Compensation (actual vs opposite-sex with discount factors, interest, and grand total).

## Database Schema

The application uses SQL Server with two schema namespaces:

### `dbo` — pension-standard tables (member data)

Member data uses the [pension-standard](https://github.com/actuit-uk/pension-standard) schema. Key tables consumed by CalcLib:

| Table | CalcLib mapping |
|-------|----------------|
| `person` | `MemberData.Sex` (from `gender`), `MemberData.DateOfBirth` |
| `member` | `MemberData.DateOfLeaving`, section FK for benefit rules |
| `gmp` | `MemberData.DateCOStart/DateCOEnd`, `GmpRevaluationMethod` (from `revaluation_basis`), `MemberData.HasTransferredInGmp` (from `gmp_source = 'TRANSFER_IN'`) |
| `section` | `SchemeConfig` — NRA, accrual rate, increase method, PIP method, anti-franking (pension-standard [#1](https://github.com/actuit-uk/pension-standard/issues/1)) |
| `financial` | `MemberData.Earnings`, salary data for `EarningsEstimator` (pension-standard [#2](https://github.com/actuit-uk/pension-standard/issues/2)) |
| `benefit_slice` | `MemberData.PensionAtLeaving` (from `slice_type = 'SCHEME'`) |

The `section` table links loosely to FinancialEntities' `ArrangementSection` via `source_system` / `source_record_id`.

### `calc` — GMP equalisation engine (results + factors)

Engine-specific tables for factor data and calculation outputs:

| Table | Purpose |
|-------|---------|
| `calc.factor` | Actuarial reference data: S148 orders, LPI rates, discount rates, BoE base rates. Keyed by `(factor_type, tax_year)`. Will dovetail with ActuarialData library when finalised. |
| `calc.run` | Calculation audit trail: inputs snapshot, summary outputs, run metadata |
| `calc.run_cashflow` | Year-by-year cash flow projection (maps to `CashFlowEntry`) |
| `calc.run_compensation` | Year-by-year compensation (maps to `CompensationEntry`) |

All input data lives in pension-standard (`dbo`). The `calc` schema holds only factor reference data and calculation outputs.

### Data flow

```
dbo.person + dbo.member + dbo.gmp     → MemberData
dbo.section                            → SchemeConfig
dbo.financial (earnings/salary)        → MemberData.Earnings (or via EarningsEstimator)
calc.factor                            → IFactorProvider
                    ↓
            GmpCalculator.Calculate()
                    ↓
calc.run + calc.run_cashflow + calc.run_compensation
                    ↓
dbo.gmp (update equalisation_status, equalisation_uplift)
```

## Web UI Integration

### GmpEqualisationController

The `GmpEqualisationController` bridges pension-standard data to CalcLib, providing an interactive GMP equalisation workflow directly in the web UI.

**Index** — lists members who have GMP records, filtered by the active role. Queries `PensionDataService.GetMembersWithGmp()` which joins `member`, `person`, `scheme`, and `gmp` tables to show key member info and contracted-out date ranges.

**Run** — loads a member's detail, GMP records, and section rules, then bridges DB rows into CalcLib inputs:

- `MemberData` — via `EarningsEstimator.Estimate()` with a ~£15k 1990 salary anchor, using CO dates from the GMP record and date of leaving from the membership
- `SchemeConfig` — constructed from section rules (NRA, accrual denominator, increase method, PIP method, anti-franking, revaluation basis)
- `IFactorProvider` — `DictionaryFactorProvider` with database-loaded factors (or empty for estimation)

The result view presents `EqualisationResult` in three layers:

1. **Summary cards** — compensation total, interest, key inputs, GMP breakdown (at leaving + revalued)
2. **Cash Flow tab** — year-by-year grid with side-by-side male/female columns: GMP status, pre-88 GMP, post-88 GMP, excess, total pension, and increase factors
3. **Compensation tab** — year-by-year actual vs opposite-sex cash flows, compensation difference, discount rate and factor, with a totals footer

The tabbed layout keeps detailed grids navigable without clutter. PIP transition years are highlighted for quick identification.

### Navigation

The Calcs dropdown in the main nav provides an extensible entry point for calculation types. GMP Equalisation is the first item; future calculation types slot in as additional dropdown items. Members with GMP records also see a "Run Equalisation" button on their detail page.

## Deployment

The application is published with `dotnet publish -c Release` and deployed to IONOS via FTP. The CalcLib DLL is bundled in the published output — it runs on the web server, not as a separate service.
