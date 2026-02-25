# Project Overview

Pension Portal is a proof-of-concept application built to validate the full modernised architecture on IONOS Windows shared hosting before migrating the real GMP Equalisation calculation system.

## Why This POC Exists

The existing GMPEQ system uses Classic ASP with business logic in T-SQL stored procedures. The modernisation replaces this with:

- ASP.NET Core MVC for the web layer
- A C# class library (CalcLib) for calculation logic
- Thin CRUD-only stored procedures for data access
- Markdown files for documentation (replacing HTML-in-database)

## Domain

The POC uses a deliberately simple domain to isolate infrastructure risk from domain complexity:

- **People** with dates of birth
- **Age in complete months** as the calculation
- **Factor table lookup** mapping age to a factor value

This proves every architectural layer without requiring actuarial domain expertise to validate.

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
├── sql/                            Database creation scripts
├── docs/                           DocFX documentation (this site)
└── data/                           Sample CSV files
```
