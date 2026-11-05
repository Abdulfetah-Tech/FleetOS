# Engineering decisions

## Foundation

- Preserved Clean Architecture dependency direction: API -> Application/Infrastructure, Infrastructure -> Application/Domain, Application -> Domain, Domain -> no project dependencies.
- Configured EF Core/Npgsql for PostgreSQL, snake_case naming, UTC timestamps, tenant filters, soft deletion, and `xmin` concurrency tokens.
- Kept local database credentials in User Secrets and ignored `.env` files for local Compose configuration.
- Used ASP.NET Core exception handling, health checks, rate limiting, CORS, OpenAPI, API versioning, and development-only Scalar.
- Used Testcontainers PostgreSQL for persistence-backed integration tests and unit tests for domain and audit behavior.

## Vehicle foundation

- Added a tenant-owned Vehicle domain entity with identifier normalization, mileage/status rules, and type/fuel/status enums.
- Added organization foreign keys and tenant-scoped unique indexes for normalized plates and optional VINs.
- Added the `AddVehicles` migration and integration tests for vehicle uniqueness and tenant stamping.

## Repository layout (2026-11-05)

- Kept `EthioFleet.slnx` at repository root; grouped .NET projects under `backend/` and Angular under `frontend/ethiofleet/`.
- Added `backend/Directory.Build.props` and GitHub Actions jobs for backend build/tests and frontend production build.
- Retained distinct API, Application, Domain, Infrastructure, and Tests projects to keep dependency boundaries explicit.

## Product-spec availability

`docs/product-spec.md` is a short outline; implementation follows verified code and explicit feature acceptance criteria rather than assuming unimplemented requirements exist.
