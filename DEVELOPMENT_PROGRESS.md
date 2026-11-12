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
