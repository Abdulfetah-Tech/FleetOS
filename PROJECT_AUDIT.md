# EthioFleet OS Project Audit

**Reviewed:** 2026-11-12
**Scope:** Tracked solution, backend, Angular frontend, Docker Compose, configuration, CI, existing documentation, and tests.

## Existing project structure

```text
EthioFleet.slnx
backend/
  EthioFleet.Api/
  EthioFleet.Application/
  EthioFleet.Domain/
  EthioFleet.Infrastructure/
  EthioFleet.Tests/
frontend/ethiofleet/
docs/
.github/workflows/ci.yml
docker-compose.yml
```

The solution references the projects under `backend/`. Similar project folders at the repository root are ignored legacy/local copies; they are not the active solution and should not be edited as part of normal work.

## Technologies detected

- **Backend:** ASP.NET Core Web API on .NET 10, C#, API versioning, JWT bearer authentication, ASP.NET Core Identity, Serilog, and Scalar/OpenAPI.
- **Application/domain:** Separate Application and Domain projects following inward Clean Architecture dependencies.
- **Persistence:** EF Core with Npgsql/PostgreSQL, snake-case naming, checked-in migrations, and PostgreSQL Testcontainers for integration tests.
- **Frontend:** Angular 22, TypeScript, RxJS, standalone components, and SCSS.
- **Local infrastructure:** Docker Compose starts PostgreSQL 16 only. The API and frontend are run separately.
- **CI:** GitHub Actions restores/builds/tests .NET and performs a production Angular build.

## Existing functionality

- Public tenant registration with email-confirmation enforcement, login, JWT access tokens, hashed refresh tokens, refresh rotation, logout, password reset, role seeding, and account lockout.
- Organization-scoped persistence and API policies; tenant filters, tenant stamping/checks, soft deletion, audit timestamps, and optimistic concurrency.
- Vehicle and driver records, vehicle-driver assignments, trip lifecycle, fuel records, maintenance records/schedules, inspections, vehicle documents, incidents, and in-app notifications.
- Fleet dashboard summary, report query/CSV export, saved report filters, and a reminder background service.
- PostgreSQL migrations and unit/integration tests covering domain rules, authentication, API foundation, and tenant isolation.
- Health endpoints, problem handling, CORS allow-list, rate limiting, security headers, and development-only API docs.

## Missing or incomplete product scope

- Angular currently contains only a landing/status shell; there are no login, fleet, dispatch, finance, or administration screens and no API client/auth flow.
- Organizations are the current tenant boundary. Branches, permissions management, a super-administrator workflow, and the full requested role grants are not implemented. Requested role names are seeded, but most have no operation permissions yet.
- No email provider is configured; email-dependent account workflows return an explicit service-unavailable response until an adapter is configured. SMS/email notification providers beyond the account email boundary, immutable audit-log records, and production session/device management are absent.
- Revenue, general expenses, allowances, spare parts, route/geofence/GPS records, and live device/provider integrations are absent.
- There is no AI provider abstraction or assistant. No AI behavior should be implied until implemented and evaluated.
- PDF/Excel export, production analytics, end-to-end workflows, and production deployment/monitoring/backup configuration are not present.
- Compose has no API/frontend services or migration runner; local configuration currently describes PostgreSQL and manual API setup.
- Development synthetic data/seeding beyond identity roles is not in place.

## Architecture assessment

The existing backend boundaries and tenant-aware persistence provide a sound foundation for incremental SaaS development. It is preferable to extend the active solution rather than replace it. Vehicle, driver, trip, maintenance, fuel, document, inspection, notification, incident, and reporting slices already exist and should be preserved and tested as each is expanded.

The current architecture is a development foundation, not a production-complete SaaS. Tenant isolation is enforced in application/data access but has no separately configured PostgreSQL row-level-security policy. The frontend and operational deployment layers are substantially behind the backend.

## Critical technical issues and risks

1. **Frontend readiness check:** It previously called `/api/v1/health` and expected a `service` response property. The API exposes `/health/ready` and returns `status` plus `checks`. The check has been corrected to match the actual API contract.
2. **Local-only API address:** The Angular shell uses the launch profile's `http://localhost:5062`; deployment needs an environment-specific API origin and secure hosting/CORS configuration.
3. **Account email delivery:** Registration now requires email confirmation and password-reset workflows exist, but no email provider is configured. New accounts cannot sign in until a provider is wired and confirmation mail is delivered.
4. **Incomplete authorization model:** Current policies cover the roles used by implemented operations, not the full product role/permission matrix.
5. **Storage durability:** Vehicle documents use local private filesystem storage. Production needs durable, access-controlled object storage and operational backup/retention policies.
6. **Financial correctness:** Dashboard costs group and return totals by currency, but comprehensive expense/revenue accounting and cross-currency conversion are not implemented.
7. **Production integrations:** No live GPS, AI provider, SMS/email, Redis, or SignalR integration exists; these require explicit providers, secrets, data-protection, and reliability decisions.
8. **Database rollout:** Migrations exist, but automatic/controlled production migration execution and rollback procedures are not documented or configured.

## Baseline verification

- `dotnet build EthioFleet.slnx --no-restore --configuration Release` — passed, zero warnings/errors.
- `dotnet test backend/EthioFleet.Tests/EthioFleet.Tests.csproj --no-restore --configuration Release` — passed, 72 tests.
- `npm run build --prefix frontend/ethiofleet` — passed.
- Docker Compose and production deployment were inspected but not started or validated in this audit.

## Complexity estimate

**Overall: Very high.** This is a multi-domain SaaS product, not a single feature. Existing backend capabilities reduce the foundation work, but the complete frontend, missing business domains/integrations, production controls, and end-to-end validation require multiple small, independently tested delivery phases. No calendar estimate is asserted without an agreed team capacity and release scope.

See [IMPLEMENTATION_ROADMAP.md](./IMPLEMENTATION_ROADMAP.md) for proposed sequencing.
