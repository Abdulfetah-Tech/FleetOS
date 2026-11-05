# Architecture

## Repository layout

```text
EthioFleet/
├── backend/
│   ├── EthioFleet.Api/
│   ├── EthioFleet.Application/
│   ├── EthioFleet.Domain/
│   ├── EthioFleet.Infrastructure/
│   └── EthioFleet.Tests/
├── frontend/ethiofleet/
├── docs/
├── .github/workflows/
├── EthioFleet.slnx
└── docker-compose.yml
```

The solution stays at the root as the single .NET entry point. Backend projects are grouped together, the Angular client is separate, and `backend/Directory.Build.props` centralizes common compiler settings.

## Dependency direction

```text
API --> Application --> Domain
 |                       ^
 +--> Infrastructure -----+
```

- **Domain** contains business entities, value objects, enums, and rules; it has no project dependencies.
- **Application** contains use-case contracts, interfaces, validation, and result types; it depends on Domain.
- **Infrastructure** implements persistence and external concerns; it depends on Application and Domain.
- **API** composes Application and Infrastructure and owns HTTP, authentication, middleware, and endpoints.
- **Tests** contains unit tests and PostgreSQL-backed integration tests.
- **Frontend** is the Angular application at `frontend/ethiofleet/`.

## Persistence and tenancy

`AppDbContext` applies tenant and soft-delete filters to entities implementing the respective interfaces. SaveChanges stamps audit metadata, assigns tenant IDs from authenticated tenant context, prevents cross-organization writes, and converts soft deletes to updates. PostgreSQL `xmin` provides optimistic concurrency tokens.

Foreign keys and tenant-scoped unique indexes reinforce tenant boundaries. Authorization policies require an organization claim; EF query filters are a data-access safeguard, not a replacement for API authorization.

## API and operations

The API provides consistent problem responses, structured request logging, request IDs, health checks, rate limiting, CORS, security headers, API versioning, and JWT role policies. Readiness checks PostgreSQL; liveness does not query dependencies. OpenAPI and Scalar are development-only.

## Validation

```powershell
dotnet build .\EthioFleet.slnx
dotnet test .\backend\EthioFleet.Tests\EthioFleet.Tests.csproj
npm ci --prefix .\frontend\ethiofleet
npm run build --prefix .\frontend\ethiofleet
```
