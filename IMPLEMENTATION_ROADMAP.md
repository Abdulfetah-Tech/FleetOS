# EthioFleet OS Implementation Roadmap

This roadmap extends the active codebase. Existing tested functionality should be maintained rather than recreated. Phase 1 audit and baseline work is recorded in [DEVELOPMENT_PROGRESS.md](./DEVELOPMENT_PROGRESS.md).

## Phase 1 — Audit and foundation

**Status: Audit/documentation complete; baseline build and test commands pass.**

- Record verified architecture, endpoint inventory, database model, gaps, and ordered roadmap.
- Correct the Angular shell's health check against the actual readiness endpoint.
- Preserve the active `backend/` solution and existing local user edits.
- Before calling this phase fully operationally complete, exercise Compose startup and verify migration setup on a clean local database.

## Phase 2 — Backend and tenant foundation

**Status: In progress. Account confirmation/reset APIs and role-name compatibility are implemented; branch hierarchy, immutable audit events, full role grants, and production migration procedure remain.**

- Agree organization onboarding and account lifecycle, including email verification/reset delivery and administrative recovery.
- Define the full role/permission matrix and cover it with authorization tests.
- Add branch hierarchy, immutable audit events, and tenant-scoped documents/relationships where approved.
- Add migration deployment and rollback procedures; test migrations against a clean PostgreSQL instance.
- **Acceptance:** documented identity lifecycle; role/tenant boundary tests; reproducible clean-database migration.

## Phase 3 — Frontend foundation

**Status: Frontend foundation implemented; real-API browser acceptance remains pending.**

- Establish configurable API origin, typed API client, auth/session behavior, route guards, and error/loading states.
- Build responsive application navigation and login/registration flows against existing API contracts.
- Add accessible design tokens and component-level tests.
- **Acceptance:** verify browser login and authenticated navigation against a configured real API; no mock-backed success path.

The current dashboard foundation has unit coverage and responsive browser verification, but account activation and live API login remain unverified. The default email sender is intentionally unconfigured and returns HTTP 503 for registration and other email-dependent operations.

## Phase 4 — Core fleet operations

**Status: In progress; vehicle and driver management workflows are connected.**

- Connect existing vehicle, driver, assignment, trip, fuel, and maintenance APIs to functional UI workflows. Vehicle and driver directory operations are the first connected slices.
- Complete dispatch, trip delivery evidence, document/insurance views, and pagination/filter needs.
- Add missing validation and authorization cases based on agreed operations.
- **Acceptance:** administrator-to-trip workflow uses persisted data and is covered by integration/e2e tests.

## Phase 5 — Notifications, finance, and analytics

- Add expense/revenue and allowance domains, currency policy, profitability calculations, and branch-aware filters.
- Extend analytics and export formats only with clearly specified calculations.
- Add email/SMS provider adapters and notification delivery state/retry handling.
- **Acceptance:** reconciled ETB totals and tenant-scoped report tests; external delivery status is visible.

## Phase 6 — GPS and external integrations

- Define provider/device contracts, consent/retention, location precision, event handling, and operational limits.
- Keep simulated GPS explicitly labeled and confined to development/test.
- Add geofences, trip trace, and alerts only after a real provider or controlled simulator is wired and verified.
- **Acceptance:** provider status and source/timestamp are visible; no simulated location is presented as live.

## Phase 7 — AI integration

- Add a provider-independent adapter and secret configuration for selected providers.
- Expose a small allow-listed set of read-only application queries; never permit unrestricted SQL.
- Keep predictions labeled as estimates, show source/coverage, and require authorized confirmation for any write action.
- Add prompt-injection, tenant-isolation, privacy, failure, and cost-limit tests.
- **Acceptance:** AI can answer only from authorized tools and does not invent unavailable metrics or silently mutate records.

## Phase 8 — Quality and production deployment

- Expand unit, API authorization, tenant-isolation, database migration, frontend, and e2e coverage.
- Add production Docker images/services, secure environment/secrets handling, controlled migrations, monitoring, backup/restore, and incident runbooks.
- Validate performance under representative tenant data and conduct focused security review.
- **Acceptance:** repeatable CI/CD deployment and restore drill; documented limits and verified health/alerts.

## Delivery practices

- Keep each slice buildable and testable; record work in [DEVELOPMENT_PROGRESS.md](./DEVELOPMENT_PROGRESS.md).
- Before each push, run the smallest relevant test set and then the project-level CI checks when the change crosses boundaries.
- Push only reviewed task changes; do not stage local secrets, generated output, or unrelated worktree edits.
