# Project Overview

Pension Portal is a proof-of-concept application built to validate the full modernised architecture on IONOS Windows shared hosting before migrating the real GMP Equalisation calculation system.

## Why This POC Exists

The existing GMPEQ system uses Classic ASP with business logic in T-SQL stored procedures. The modernisation replaces this with:

- ASP.NET Core MVC for the web layer
- A C# class library (CalcLib) for calculation logic
- Thin CRUD-only stored procedures for data access
- Markdown files for documentation (replacing HTML-in-database)

## Domain

The project has two calculation layers:

- **POC layer** — People with dates of birth, age in complete months, factor table lookup. This proves every architectural layer without requiring actuarial domain expertise to validate.
- **GMP Equalisation layer** — The real calculation engine, implementing GMP calculation from contracted-out earnings, S148/Fixed/Limited rate revaluation, and equalisation compensation. This is being built incrementally in CalcLib with full test coverage against the legacy GMPEQ database results.

## Solution Structure

```
pension-portal/
├── src/
│   ├── PensionPortal.Web/          ASP.NET Core MVC app
│   │   ├── Controllers/            MVC controllers
│   │   ├── Views/                  Razor views
│   │   ├── Services/               DatabaseService
│   │   └── Docs/                   Markdown articles (in-app)
│   └── PensionPortal.CalcLib/      Calculation library
│       └── Internal/               Internal calculator helpers
├── tests/
│   └── PensionPortal.CalcLib.Tests/ xUnit test project
├── sql/                            Database creation scripts
├── docs/                           DocFX documentation (this site)
└── data/                           Sample CSV files
```
