# Pension Portal Documentation

Welcome to the Pension Portal documentation. This site covers both the project overview and the API reference for the calculation library.

## Quick Links

- [Project Overview](articles/overview.md) — architecture, design decisions, and deployment
- [API Reference](api/PensionPortal.CalcLib.AgeCalculator.yml) — auto-generated from XML doc comments on CalcLib

## What is Pension Portal?

Pension Portal is a proof-of-concept ASP.NET Core MVC application that validates a modernised architecture for pension calculation systems. It demonstrates:

- **C# calculation library** executing server-side (replacing T-SQL business logic)
- **Parameterised database access** via thin stored procedures (no SQL injection)
- **Cookie authentication** via ASP.NET Core middleware
- **Markdown knowledge base** replacing HTML-in-database patterns
- **CSV import/export** for bulk data operations
- **Factor table lookup** proving the reference-data-to-C#-calc pattern

## Technology Stack

| Layer | Technology |
|---|---|
| Web framework | ASP.NET Core 8.0 MVC |
| Calculation engine | PensionPortal.CalcLib (.NET class library) |
| Database | SQL Server with thin CRUD stored procedures |
| Authentication | ASP.NET Core cookie authentication |
| UI | Bootstrap 5, Razor views |
| Documentation | DocFX (this site), Markdig (in-app knowledge base) |
