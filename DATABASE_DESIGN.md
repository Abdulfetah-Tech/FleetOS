# EthioFleet OS Database Design

## Current persistence model

PostgreSQL is accessed through EF Core/Npgsql. The active `AppDbContext` includes ASP.NET Core Identity plus these domain sets:

| Area | Current persisted data |
|---|---|
| Tenant and identity | Organizations, Identity users/roles, domain users/roles, refresh tokens |
| Fleet | Vehicles, vehicle assignments, drivers and embedded driver-license data |
| Operations | Trips, fuel records, maintenance records and schedules, vehicle inspections, incidents |
| Supporting | Vehicle documents, notifications, saved fleet-report filters |

The repository contains migrations from the initial foundation through the incidents slice. Migrations use PostgreSQL types and snake_case naming. ETB is the default currency in relevant value objects/entities; currency codes are stored with monetary values.

## Existing safeguards

- Domain entities use GUID primary keys and enforce input/state invariants.
- Tenant-owned entities carry `OrganizationId`. EF Core applies organization filters; save stamping sets the organization on inserts and rejects writes outside the current organization.
- Soft-deletable entities are hidden by query filters and converted to soft deletes by SaveChanges.
- Selected parent-child relationships use composite organization/entity keys to prevent cross-tenant references at the database layer.
- Tenant-scoped uniqueness is used for normalized vehicle plates/VINs, driver email/license data, and selected supporting records.
- Time values are stored as PostgreSQL `timestamp with time zone`; audited entities receive created/updated metadata.
- PostgreSQL `xmin` is configured as an optimistic concurrency token.

These application/relational safeguards are not PostgreSQL row-level security. Any direct SQL, new data path, or background task must preserve tenant scope explicitly.

## Product-domain gaps

The requested design is broader than the implemented schema. Not currently persisted: branches, explicit company hierarchy separate from organization, permission catalog/assignments, audit-event history, routes, trip assignments separate from the current vehicle assignment, fuel stations, expense/revenue/allowance ledgers, spare parts, GPS points/geofences, AI conversations, and external notification delivery state. Some document data and operational dates exist, but they do not represent a complete compliance/retention model.

## Proposed additions (not yet migrated)

1. **Organization structure:** `Branch` with organization-scoped code/name/status; optional `BranchId` on applicable operational entities. Use composite tenant foreign keys.
2. **Authorization/audit:** permission catalog and role-permission mappings only if policy requirements warrant them; append-only `AuditLog` with tenant, actor, action, entity reference, timestamp, and redacted change metadata.
3. **Operations and finance:** explicit route/stop entities; expense categories and expense entries; revenue entries; trip expense/allocation links; allowances; optional spare-part inventory and maintenance usage.
4. **Tracking:** provider/device metadata and time-indexed location events with retention/partitioning and tenant/time indexes; geofence configuration and generated events.
5. **Notifications and AI:** delivery attempts/status with provider-neutral references; AI conversation metadata and redacted content retention only after privacy policy approval.

Design each new table from concrete query/retention patterns. Prefer tenant-scoped unique constraints and composite foreign keys wherever both sides are tenant-owned. Avoid storing large document bytes in relational tables; retain private object keys plus integrity, size, MIME, and lifecycle metadata.

## Migration and operational requirements

- Keep migrations additive and reviewed; never assume startup auto-migration is appropriate for production.
- Test upgrades from the previous schema and a fresh database.
- Define backup/restore, retention, and migration rollback/recovery procedures before production.
- Configure PostgreSQL row-level security only as an additional independently tested defense if operational roles and connection pooling can reliably set/reset tenant context.
- Add representative multi-tenant load fixtures before indexing or partitioning decisions.
