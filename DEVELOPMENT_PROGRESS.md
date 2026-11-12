# EthioFleet OS Development Progress

## Phase 1 — Project audit and foundation

**Status:** Audit and baseline verification complete; operational setup follow-up remains.

### Completed

- Confirmed `EthioFleet.slnx` uses the .NET 10 projects under `backend/`; Angular 22 lives under `frontend/ethiofleet`.
- Inspected API/controllers, domain and EF Core model, migrations, auth/tenant handling, configuration, Compose, CI, and existing product documentation.
- Added the initial audit, architecture, database, API, and roadmap documents.
- Corrected the Angular shell to call `/health/ready` and render the actual readiness response; added a focused frontend test.

### Verification

- Release backend build: passed, zero warnings/errors.
- Backend tests: 72 passed.
- Angular production build after the health-check correction: passed.
- Angular frontend tests after the health-check correction: 3 passed.
- Compose startup and a clean-database migration were not run during this phase.

### Outstanding

- Verify local PostgreSQL Compose startup and clean migration execution.
- Replace the shell's fixed development API origin with deployment configuration when implementing the frontend foundation.
- Define the full role matrix and production storage/migration approach.

### Next

Continue Phase 2 with authorization grants, branch scoping, immutable audit events, and migration/rollback validation. Preserve the existing fleet slices.

## Phase 2 — Backend and tenant foundation

**Status:** In progress; account lifecycle slice implemented.

### Completed in this slice

- Public registration now creates an unconfirmed company administrator and does not issue a session.
- Email confirmation is required before login; resend-confirmation and password-reset endpoints use short-lived ASP.NET Core Identity tokens.
- Password reset revokes active refresh tokens.
- Added provider-neutral `IEmailSender`; the default implementation is explicitly unconfigured and returns HTTP 503 for email-dependent operations. Registration rolls back rather than creating an unusable unverified account.
- Seeded the requested role names. New `CompanyAdministrator` tokens include `CompanyOwner` as a compatibility claim; the full permission mapping is not complete.
- Duplicate registration responses no longer disclose whether an email is already registered.

### Verification

- Targeted registration/confirmation/refresh/logout and password-reset integration tests: 2 passed against PostgreSQL Testcontainers.
- Tests cover login denial before confirmation, role compatibility, refresh-token revocation after password reset, and explicit unconfigured-provider errors plus rollback when delivery fails.
- Full Release backend suite: 73 passed; build completed with zero warnings and errors.

### Outstanding

- Choose and implement an email provider; until then new registration and email-dependent account flows return 503.
- Persist/share ASP.NET Core Data Protection keys in deployment before enabling email tokens across restarts or replicas.
- Define and test actual permissions for Operations Manager, Dispatcher, Accountant, Driver, Read-only Analyst, and Super Administrator.
- Implement branch scoping and immutable audit events with tenant-isolation tests.
- Validate clean-database migration and rollback procedures.

### Next

Complete the role-to-operation matrix and authorization tests, then implement the approved branch and audit-log model.

## Phase 3 — Frontend foundation

**Status:** Implemented; live-API browser acceptance remains pending.

### Completed in this slice

- Added runtime API-origin configuration, typed auth and dashboard clients, login and registration routes, route protection, access-token refresh, logout, and light/dark theme switching.
- Replaced the health-only shell with a fleet-readiness dashboard using the tenant-scoped dashboard API. It shows fleet totals, overdue maintenance, documents expiring within 30 days, currency-grouped monthly costs, active drivers, trips, and open incidents.
- Kept loading, empty, API-error, and unconfigured-email states explicit; no demo records or simulated GPS are presented as live.
- Added responsive navigation and layout rules, an approved Fleet Wayfinding design brief, and the shipped design-system documentation.

### Verification

- Angular tests: 6 passed across 4 test files.
- Angular production build: passed with no warnings.
- Browser flow with intercepted API responses: registration's expected 503 error, sign-in, and dashboard data rendering verified; responsive layout checked at 390px, 768px, and 1440px in light and dark themes with no horizontal overflow.
- A mobile breakpoint specificity issue found in browser review was corrected and rechecked.

### Outstanding

- Verify login and authenticated navigation against a running API and a real, email-confirmed account. The default email sender is unconfigured, so registration cannot complete until a provider is configured.
- Validate the dashboard against tenant data in the integration environment; browser fixture responses were intercepted and are not application seed data.
- Continue with fleet operation workflows after Phase 2 authorization, branch-scoping, and migration requirements are resolved.

### Next

Configure an email provider and test the frontend against the real API with a verified account, then begin the approved vehicle and driver workflows.

## Phase 4 — Core fleet operations

**Status:** In progress; vehicle and driver management are connected operations workflows.

### Completed in this slice

- Extracted dashboard navigation, theme, and account actions into a shared authenticated workspace layout.
- Added tenant-scoped vehicle listing, search, pagination, create, edit, status change, and owner-only removal against the existing API.
- Added overflow-safe pagination normalization across paginated services and tenant-isolation regression coverage for vehicle operations.
- Unified vehicle endpoint errors with RFC Problem Details and removed duplicate EF Core mappings for trips and vehicle documents.
- Added tenant-scoped driver search and pagination, owner-only create/edit/status/remove controls, and a license-expiry-aware driver directory.
- Standardized driver missing-record errors as Problem Details and capped driver search at 100 characters; added regression checks for search validation and owner-only writes.

### Verification

- Shared pagination boundary unit tests pass, including the maximum page value.
- Angular production build passes without component-style budget warnings.
- Vehicle tenant-isolation coverage verifies trusted tenant stamping, list isolation, cross-tenant not-found behavior, and protected deletion. Driver integration coverage verifies tenant separation, owner-only mutations, search limits, and Problem Details responses.
- Backend test suite: 77 passed. Angular tests: 14 passed across 8 files. API build: zero warnings and errors.
- Browser verification with intercepted API responses exercised vehicle and driver create/edit/status/remove and authenticated navigation. Dashboard and vehicle layouts were checked at 1440px, 768px, 390px, and 320px; the driver layout was rechecked at the same widths after its mobile navigation adjustment. Light/dark rendering showed no horizontal overflow.

### Outstanding

- Continue with assignment, trip, fuel, and maintenance workflows after the corresponding operation requirements and authorization boundaries are validated.

### Next

Verify driver workflows, then connect vehicle assignments.
