# EthioFleet API Documentation

## Base URL and conventions

- Versioned routes use `/api/v1`.
- Development launch profile: `http://localhost:5062`.
- Protected routes accept `Authorization: Bearer <access-token>`.
- JSON request/response bodies use ASP.NET Core controller conventions. Validation/domain failures are returned as client errors; unhandled errors use the configured problem-details exception handler.
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
| POST | `/api/v1/auth/register` | Anonymous; auth rate limit | Register organization owner and create a session |
| POST | `/api/v1/auth/login` | Anonymous; auth rate limit | Authenticate and create a session |
| POST | `/api/v1/auth/refresh` | Anonymous; auth rate limit | Rotate refresh token and issue a new session |
| POST | `/api/v1/auth/logout` | Authenticated | Revoke the supplied refresh token |

Registration body: `email`, `userName`, `password`, `organizationName`. Login body: `email`, `password`. Refresh/logout body: `refreshToken`. Successful auth returns access/refresh tokens, expirations, and user/organization information. Refresh tokens are returned to clients but stored hashed server-side.

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
| GET/POST/PUT/PATCH/DELETE | `/api/v1/drivers[/{id}]` | Fleet manager for reads/status; company owner for writes | Driver profile and status operations |
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

Data access is also tenant-filtered. The policies and database filters are defense-in-depth, not a replacement for each other.

## Limitations

This document inventories current routes, not the entire requested future API. There are no current endpoints for branch management, general expenses/revenue, GPS tracking, AI, provider delivery configuration, or user/permission administration. Request DTOs and response types are defined in the corresponding Application contracts and controllers; inspect Development OpenAPI for the currently generated schema.
