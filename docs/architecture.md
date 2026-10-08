# Architecture

The solution follows Clean Architecture with project references pointing inward:

```text
API -> Application -> Domain
API -> Infrastructure -> Application -> Domain
Infrastructure -> Domain
Domain -> no project references
```

The API composes application and infrastructure registrations and contains only HTTP concerns. Application owns use-case contracts, current-user/tenant/clock interfaces, validation setup, and result types. Domain contains framework-independent primitives, auditable and tenant-owned contracts, money/currency value objects, and domain exceptions. Infrastructure owns EF Core/Npgsql persistence and implements the application contracts.

`AppDbContext` applies tenant and soft-delete filters to mapped entities implementing the relevant domain interfaces. SaveChanges stamps audit metadata, assigns tenant IDs from the authenticated tenant context, prevents writes across organization boundaries, and converts deletes of soft-deletable entities to updates. PostgreSQL `xmin` is configured as the EF row-version token for mapped entities.

The API provides RFC 7807 errors, health checks, request-ID logging, baseline rate limiting, configured CORS, security headers, URL API versioning, and development-only OpenAPI/Scalar. The readiness probe checks PostgreSQL connectivity; the liveness probe does not access dependencies.
