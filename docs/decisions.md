# Engineering decisions

## Phase 1 foundation (2026-10-08)

- Kept the existing project folders and the `EthioFleet.slnx` layout; corrected references to API -> Application/Infrastructure, Infrastructure -> Application/Domain, Application -> Domain, and Domain -> no projects.
- Removed generated starter code and the existing vehicle, driver, and dashboard examples because Phase 1 explicitly excludes business features.
- Used EF Core 10 with Npgsql 10 to match the .NET 10 target. Configured snake_case naming, UTC `timestamptz` audit timestamps, query-filter scaffolding, soft-delete handling, and PostgreSQL `xmin` row-version concurrency tokens.
- Chose a baseline migration with no business tables. Its purpose is to establish EF migration history without introducing out-of-scope domain features.
- Kept the local connection string in ASP.NET Core User Secrets; `.env` supplies only local Docker Compose database variables and is ignored by Git.
- Used the built-in ASP.NET Core exception handler, health checks, rate limiting, CORS, and OpenAPI; Scalar is enabled only in Development.
- Added API versioning setup and an API-version metadata route, not a business endpoint.
- Used Testcontainers PostgreSQL for HTTP integration tests, and direct change-tracker testing for audit stamping to avoid an in-memory database.

## Product-spec availability

The current `docs/product-spec.md` contains three unnumbered sections and does not contain requested sections 4, 5, 25-28, 38, or 39. This phase follows the explicit task scope and the available architecture rules instead.
