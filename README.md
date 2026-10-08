# EthioFleet

EthioFleet is a multi-tenant fleet-management platform foundation for Ethiopian businesses. This Phase 1 delivery is foundation-only; it does not implement fleet business features.

## Prerequisites

- .NET 10 SDK
- Docker Desktop with Docker Compose
- .NET EF CLI tool (restored from the repository manifest below)

## Local setup

1. Copy `.env.example` to `.env` and set a unique local `POSTGRES_PASSWORD`.
2. Start PostgreSQL and wait until its health check passes:

   ```powershell
   docker compose up -d
   docker compose ps
   ```

3. Store the local connection string in .NET User Secrets (replace the password with the value from `.env`):

   ```powershell
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=ethiofleet;Username=ethiofleet;Password=<your-local-password>" --project .\EthioFleet.Api\EthioFleet.Api.csproj
   ```

4. Restore and build:

   ```powershell
   dotnet tool restore
   dotnet build
   ```

5. Apply the baseline migration:

   ```powershell
   dotnet tool run dotnet-ef database update --project .\EthioFleet.Infrastructure\EthioFleet.Infrastructure.csproj --startup-project .\EthioFleet.Api\EthioFleet.Api.csproj
   ```

6. Run the API:

   ```powershell
   dotnet run --project .\EthioFleet.Api\EthioFleet.Api.csproj
   ```

## Endpoints

- `GET /health/live` — process liveness.
- `GET /health/ready` — readiness, including PostgreSQL connectivity.
- `/api/v1` — API version metadata.
- `/scalar/v1` — Scalar UI and `/openapi/v1.json` — OpenAPI document, development only.

## Tests

Run `dotnet test`. PostgreSQL-backed API integration tests use Testcontainers and therefore require Docker. Unit tests cover audit stamping without making a database connection.

## Project layout

- `EthioFleet.Domain` — domain primitives, exceptions, and value objects.
- `EthioFleet.Application` — interfaces, result/error types, validation pipeline, and registration.
- `EthioFleet.Infrastructure` — EF Core/Npgsql persistence, tenant filters, and registration.
- `EthioFleet.Api` — HTTP composition, health, diagnostics, versioning, and API middleware.
- `EthioFleet.Tests` — unit and PostgreSQL-backed integration tests.

See [docs/architecture.md](./docs/architecture.md) and [docs/decisions.md](./docs/decisions.md).
