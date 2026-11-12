# EthioFleet

EthioFleet is a multi-tenant fleet-management platform for businesses. This monorepo uses a Clean Architecture .NET backend and an Angular frontend.

## Repository structure

```text
EthioFleet/
├── backend/
│   ├── EthioFleet.Api/
│   ├── EthioFleet.Application/
│   ├── EthioFleet.Domain/
│   ├── EthioFleet.Infrastructure/
│   └── EthioFleet.Tests/
├── frontend/
│   └── ethiofleet/
├── docs/
├── .github/workflows/
├── EthioFleet.slnx
└── docker-compose.yml
```

The solution remains at the repository root. References point inward: API -> Application/Infrastructure, Infrastructure -> Application/Domain, Application -> Domain, Domain -> no project dependencies.

## Prerequisites

- .NET 10 SDK
- Node.js 24 and npm compatible with the frontend lockfile
- Docker Desktop with Compose for PostgreSQL and container-backed integration tests

## Local setup

1. Copy `.env.example` to `.env` and set a local PostgreSQL password. Compose reads these values when starting PostgreSQL; ASP.NET Core does not automatically load `.env` files.
2. Start PostgreSQL and check its health:

   ```powershell
   docker compose up -d postgres
   docker compose ps
   ```

3. Add the database connection string and JWT signing key to User Secrets:

   ```powershell
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=ethiofleet;Username=ethiofleet;Password=<your-local-password>" --project .\backend\EthioFleet.Api\EthioFleet.Api.csproj
   dotnet user-secrets set "Authentication:Jwt:SigningKey" "<unique-secret-of-at-least-32-UTF-8-bytes>" --project .\backend\EthioFleet.Api\EthioFleet.Api.csproj
   ```

   Account registration, confirmation resend, and password-reset requests require an email sender. No provider is configured by default, so those email-dependent endpoints return `503 Service Unavailable` until an `IEmailSender` implementation is registered.

4. Restore, build, and test the backend:

   ```powershell
   dotnet tool restore
   dotnet restore .\EthioFleet.slnx
   dotnet build .\EthioFleet.slnx
   dotnet test .\backend\EthioFleet.Tests\EthioFleet.Tests.csproj
   ```

   PostgreSQL integration tests use Testcontainers and require Docker.

5. Apply migrations and run the API:

   ```powershell
   dotnet tool run dotnet-ef database update --project .\backend\EthioFleet.Infrastructure\EthioFleet.Infrastructure.csproj --startup-project .\backend\EthioFleet.Api\EthioFleet.Api.csproj
   dotnet run --project .\backend\EthioFleet.Api\EthioFleet.Api.csproj
   ```

6. Install and run Angular:

   ```powershell
   npm ci --prefix .\frontend\ethiofleet
   npm start --prefix .\frontend\ethiofleet
   ```

## API endpoints

- `GET /health/live` — process liveness.
- `GET /health/ready` — readiness, including PostgreSQL connectivity.
- `/api/v1` — API version metadata.
- `/api/v1/*` — authenticated fleet, driver, trip, fuel, maintenance, inspection, incident, notification, organization, and report endpoints.
- `/scalar/v1` and `/openapi/v1.json` — development-only API documentation.

See [API_DOCUMENTATION.md](./API_DOCUMENTATION.md) for the current endpoint inventory. See [PROJECT_AUDIT.md](./PROJECT_AUDIT.md), [SYSTEM_ARCHITECTURE.md](./SYSTEM_ARCHITECTURE.md), [DATABASE_DESIGN.md](./DATABASE_DESIGN.md), [IMPLEMENTATION_ROADMAP.md](./IMPLEMENTATION_ROADMAP.md), and [DEVELOPMENT_PROGRESS.md](./DEVELOPMENT_PROGRESS.md) for the verified baseline and delivery plan. Existing design records remain in `docs/`.
