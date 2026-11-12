# EthioFleet API Documentation

## Base URL and conventions

- Versioned routes use `/api/v1`.
- Development launch profile: `http://localhost:5062`.
- Protected routes accept `Authorization: Bearer <access-token>`.
- JSON request/response bodies use ASP.NET Core controller conventions. Validation/domain failures are returned as client errors; unhandled errors use the configured problem-details exception handler.
- Enum properties retain the API's numeric JSON representation for compatibility.
- OpenAPI and Scalar are available only in the Development environment.

## Operational endpoints

| Method | Path | Access | Purpose |
|---|---|---|---|
| GET | `/health/live` | Public | Process liveness |
| GET | `/health/ready` | Public | Readiness including PostgreSQL; response contains `status` and check statuses |
| GET | `/api/v1` | Public | API version metadata |
| GET | `/openapi/v1.json` | Development only | OpenAPI document |
| GET | `/scalar/v1` | Development only | Scalar API reference |

## Authentication

| Method | Path | Access | Purpose |
|---|---|---|---|
| POST | `/api/v1/auth/register` | Anonymous; auth rate limit | Register an organization and send an email-confirmation token; returns a generic `202 Accepted` without a session |
| POST | `/api/v1/auth/login` | Anonymous; auth rate limit | Authenticate and create a session |
| POST | `/api/v1/auth/refresh` | Anonymous; auth rate limit | Rotate refresh token and issue a new session |
| POST | `/api/v1/auth/logout` | Authenticated | Revoke the supplied refresh token |
| POST | `/api/v1/auth/confirm-email` | Anonymous; auth rate limit | Confirm the email using `{ "email": "...", "token": "..." }`; returns `204` on success |
| POST | `/api/v1/auth/resend-confirmation` | Anonymous; auth rate limit | Request another confirmation message; returns a generic `202` response |
| POST | `/api/v1/auth/password-reset/request` | Anonymous; auth rate limit | Request password-reset instructions; returns a generic `202` response |
| POST | `/api/v1/auth/password-reset/confirm` | Anonymous; auth rate limit | Reset password using `{ "email": "...", "token": "...", "newPassword": "..." }`; returns `204` on success |

Registration body: `email`, `userName`, `password`, `organizationName`. Registration does not return tokens; login is denied until email confirmation succeeds. Login body: `email`, `password`. Refresh/logout body: `refreshToken`. Successful login/refresh returns access/refresh tokens, expirations, and user/organization information. Refresh tokens are returned to clients but stored hashed server-side. Password reset revokes the account's active refresh tokens.

Registration returns the same acceptance response when the email is already registered, so the endpoint does not confirm account existence. An existing account can use the confirmation-resend endpoint.

The API currently registers a provider-neutral email sender boundary but no provider implementation. Email-dependent registration/resend/reset-request operations return `503 Service Unavailable` until one is configured; registration does not persist an account when delivery is unavailable. Email verification and password-reset tokens use ASP.NET Core Identity's data-protection token provider with a two-hour lifetime. Do not expose tokens in logs or client-visible error details.

Production deployment must persist and share ASP.NET Core Data Protection keys across API instances and restarts or outstanding email tokens will become invalid.

## Current organization

| Method | Path | Access | Purpose |
|---|---|---|---|
| GET | `/api/v1/organizations/current` | Tenant member | Current organization context |
| GET | `/api/v1/organizations/current/fleet` | Fleet manager | Current organization fleet summary |

## Fleet operations

| Method | Path | Access | Purpose |
|---|---|---|---|
| GET | `/api/v1/vehicles` | Fleet manager | List/search vehicles |
| GET | `/api/v1/vehicles/{id}` | Fleet manager | Read vehicle |
| POST | `/api/v1/vehicles` | Company owner | Create vehicle |
| PUT | `/api/v1/vehicles/{id}` | Company owner | Update vehicle |
| PATCH | `/api/v1/vehicles/{id}/status` | Fleet manager | Change vehicle status |
| DELETE | `/api/v1/vehicles/{id}` | Company owner | Delete vehicle (soft delete where supported) |
| GET | `/api/v1/vehicles/{vehicleId}/assignment` | Fleet manager | Read active assignment |
| POST | `/api/v1/vehicles/{vehicleId}/assignment` | Company owner | Create/replace assignment |
| DELETE | `/api/v1/vehicles/{vehicleId}/assignment` | Company owner | End assignment |
| GET | `/api/v1/drivers[/{id}]` | Fleet manager | List/search drivers or read a driver |
| POST/PUT/PATCH/DELETE | `/api/v1/drivers[/{id}]` | Company owner | Create/update profile, change status, or soft-delete a driver |
| GET/POST/DELETE | `/api/v1/fuel-records[/{id}]` | Fleet manager for reads; company owner for create/delete | Fuel transactions |
| GET/POST | `/api/v1/maintenance/records` | Maintenance manager for reads; company owner for create | Maintenance history |
| GET/POST | `/api/v1/maintenance/schedules` | Maintenance manager for reads; company owner for create | Preventive schedules |
| POST | `/api/v1/maintenance/schedules/{id}/complete` | Company owner | Complete a scheduled item |
| GET/POST | `/api/v1/inspections[/{id}]` | Inspection submitter; reads require inspection viewer | Vehicle inspection workflow |
| POST | `/api/v1/inspections/{id}/complete` | Inspection submitter | Complete inspection |
| GET/POST | `/api/v1/vehicle-documents` | Tenant member for reads; company owner for create | Vehicle documents metadata/upload |
| GET | `/api/v1/vehicle-documents/{id}` | Tenant member | Read document metadata |
| GET | `/api/v1/vehicle-documents/{id}/content` | Tenant member | Download private document content |
| DELETE | `/api/v1/vehicle-documents/{id}` | Company owner | Delete document |
| GET/POST | `/api/v1/trips[/{id}]` | Fleet manager for reads; company owner for create | List/read/create trips |
| POST | `/api/v1/trips/{id}/start` | Fleet manager | Start planned trip |
| POST | `/api/v1/trips/{id}/complete` | Fleet manager | Complete in-progress trip |
| POST | `/api/v1/trips/{id}/cancel` | Company owner | Cancel trip |
| GET/POST/PATCH | `/api/v1/incidents[/{id}]` | Fleet manager for reads; company owner for create/status changes | Incident management |

Vehicle listing accepts `page` (values below 1 normalize to 1), `pageSize` (normalized to 1–100), and optional `search` (maximum 100 characters; plate, make, or model). The response contains `items`, normalized `page` and `pageSize`, and `totalCount`. Missing vehicles return 404 Problem Details; duplicate plate/VIN conflicts return 409 Problem Details. Domain validation errors use the global 400 Problem Details response.

Driver listing accepts `page`, `pageSize`, and optional `search` (maximum 100 characters; full name, email, phone, or license number). Pagination uses the same normalization and response shape. Driver status mutations, like profile writes and deletion, require the CompanyOwner policy; reads require FleetManager. Missing drivers return 404 Problem Details, duplicate license conflicts return 409 Problem Details, and invalid input returns 400.

Vehicle assignment reads require FleetManager; assignment creation and ending require CompanyOwner. A missing active assignment returns 404. Creating an assignment requires a same-organization active vehicle and an active driver with a non-expired license; these checks are enforced by the API even when a client filters its candidate list. The vehicle-scoped assignment response includes the driver and vehicle identifiers, display names, assignment timestamps, active state, and optional notes.

Trip listing accepts `page`, `pageSize`, and optional `vehicleId`, `driverId`, and `status` filters. Creating a trip requires CompanyOwner and an existing active vehicle assignment for the selected vehicle and driver; the API also checks the driver license and that the starting odometer is not below the vehicle's current reading. FleetManager can start and complete trips; starting uses the API clock, and completion records an end time and odometer reading. CompanyOwner can cancel planned or in-progress trips. The domain enforces valid state transitions and non-decreasing trip mileage.

## Dashboard, reports, and notifications

| Method | Path | Access | Purpose |
|---|---|---|---|
| GET | `/api/v1/dashboard/fleet` | Fleet manager | Fleet counts, costs, due maintenance/documents, open incidents |
| GET | `/api/v1/reports/fleet?from=YYYY-MM-DD&to=YYYY-MM-DD` | Finance manager | Fleet report for a date range |
| GET | `/api/v1/reports/fleet/export.csv?from=...&to=...` | Finance manager | CSV report export |
| GET/POST | `/api/v1/reports/fleet/saved-filters` | Finance manager | List/create saved report filters |
| GET/PUT/DELETE | `/api/v1/reports/fleet/saved-filters/{id}` | Finance manager | Read/update/delete saved filter |
| GET | `/api/v1/notifications` | Tenant member | List notifications |
| POST | `/api/v1/notifications/{id}/read` | Tenant member | Mark notification read |

## Authorization policies

- `TenantMember`: authenticated principal with an `organization_id` claim.
- `CompanyOwner`: tenant member with the CompanyOwner role.
- `FleetManager`: CompanyOwner or FleetManager.
- `MaintenanceManager`: CompanyOwner or MaintenanceManager.
- `InspectionViewer`: CompanyOwner, FleetManager, or MaintenanceManager.
- `InspectionSubmitter`: CompanyOwner, FleetManager, MaintenanceManager, or Driver.
- `FinanceManager`: CompanyOwner or FinanceManager.

New organization registrations receive the `CompanyAdministrator` identity role. Its access token includes the legacy `CompanyOwner` role claim so existing policies and clients continue to work. `CompanyOwner` remains seeded for existing users. Other requested roles are seeded as catalog entries, but their operation-level permissions are not yet granted; `SuperAdministrator` has no cross-tenant API access.

Data access is also tenant-filtered. The policies and database filters are defense-in-depth, not a replacement for each other.

## Limitations

This document inventories current routes, not the entire requested future API. There are no current endpoints for branch management, general expenses/revenue, GPS tracking, AI, provider delivery configuration, or user/permission administration. Request DTOs and response types are defined in the corresponding Application contracts and controllers; inspect Development OpenAPI for the currently generated schema.
