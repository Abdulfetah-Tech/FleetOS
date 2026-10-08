# Product specification

## Vision

EthioFleet is a multi-tenant fleet management platform for Ethiopian businesses. The platform manages vehicles, drivers, trips, documents, maintenance, fuel, incidents, expenses, reporting, and subscriptions in ETB with an English-language interface.

## Initial delivery scope

The initial scaffold focuses on architecture, secure extensibility, and a minimal API/frontend shell rather than a full operational fleet implementation. It includes:

- API, application, infrastructure, and domain project boundaries
- Tenant-aware contracts and organization-scoped entity design
- A lightweight dashboard summary service
- Health endpoints and a startup-ready API configuration
- Angular frontend shell with app routing and service integration
- PostgreSQL and environment configuration for local development

## Future domains

The next production slices will add driver operations, vehicle management, trip planning, maintenance workflows, financial reporting, notifications, and audit logs under the same tenant-isolation model.
