# About Pension Portal

Pension Portal is a proof-of-concept ASP.NET Core MVC application that validates the modernised architecture for pension calculation systems.

## Architecture

The application demonstrates a clean separation between:

- **Web layer** (ASP.NET Core MVC) — handles HTTP requests, renders views
- **Calculation library** (CalcLib) — pure C# business logic, no database dependency
- **Database** (SQL Server) — data storage only, thin CRUD stored procedures

This replaces the legacy pattern where business logic lived in T-SQL stored procedures.

## Key Design Decisions

### No Entity Framework

The application uses raw ADO.NET (`SqlCommand` with `CommandType.StoredProcedure`) rather than Entity Framework. This is deliberate — the architecture is sproc-driven and metadata-first, which is the inverse of EF's assumptions.

### Parameterised Queries

All database calls use `SqlParameter` objects. User input is never concatenated into SQL strings. This eliminates SQL injection by design.

### Cookie Authentication

ASP.NET Core's built-in cookie authentication middleware replaces the legacy cookie-based username/PIN system, with proper session management and `[Authorize]` attributes.

### Markdown Knowledge Base

Documentation is served from `.md` files rendered as HTML via Markdig, replacing the database-driven document catalogue (`tblDocuments`).

## Technology Stack

| Component | Technology |
|-----------|-----------|
| Framework | ASP.NET Core 8.0 MVC |
| Database | SQL Server (Microsoft.Data.SqlClient) |
| UI | Bootstrap 5 (CDN) |
| Documentation | DocFX + Markdig |
| Hosting target | IONOS Windows shared hosting |
