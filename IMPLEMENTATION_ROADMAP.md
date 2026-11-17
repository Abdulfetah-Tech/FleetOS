# Implementation Roadmap

## Phase A – Stabilisation (completed)
- Refactor fuel‑management component tests.
- Resolve duplicate imports and Jasmine‑spy issues.
- Verify core unit tests (vehicles, drivers, trips, auth, fuel) all pass.
- Basic CI workflow runs .NET and Angular tests.

## Phase B – Backend Foundations (next)
| Milestone | Description | Owner | Target |
|-----------|-------------|-------|--------|
| **Auth hardening** | Email confirmation, password‑reset flows, rotating refresh tokens, role‑based policies for all roles (Super‑Admin, Company‑Admin, Fleet‑Manager, etc.) | Backend lead | 2 weeks |
| **Multi‑tenant isolation** | Global EF Core query filter, TenantId column on every table, row‑level‑security (future) | DBA | 1 week |
| **Domain expansion** | Add entities: `MaintenanceRecord`, `MaintenanceSchedule`, `GpsLocation`, `Expense`, `Revenue`, `Notification`, `AiConversation` | Backend lead | 3 weeks |
| **Background services** | Reminder service (maintenance, license expiry), fuel‑anomaly detector, notification dispatcher (email/SMS) | Ops | 2 weeks |
| **Redis & SignalR** | Introduce Redis cache for vehicle look‑ups, enable SignalR hub for real‑time trip updates | Ops | 1 week |
| **Automated migrations** | Add Flyway/EF migration scripts, CI step to run migrations on staging | DBA | 1 week |

## Phase C – Front‑end Foundations (parallel)
| Milestone | Description | Owner | Target |
|-----------|-------------|-------|--------|
| **Shared layout** | Sidebar, top‑bar, theme (Angular Material), responsive breakpoints, role‑based navigation | Front‑end lead | 1 week |
| **Feature scaffolding** | Generate routes & skeleton components for Vehicles, Drivers, Trips, Maintenance, GPS, Finance, AI Assistant | Front‑end lead | 2 weeks |
| **State management** | Introduce ComponentStore or NgRx for global state (current user, selected company, notifications) | Front‑end lead | 1 week |
| **API client layer** | Typed services wrapping HttpClient, auto‑inject JWT, retry & error handling | Front‑end lead | 1 week |
| **UI polish** | Material tables with pagination, chart components (Chart.js/ECharts), loading & error states | Front‑end lead | 2 weeks |

## Phase D – Advanced Features (post‑MVP)
- **GPS integration** – abstraction for real‑time location, mock provider for dev.
- **AI Assistant** – `IAiProvider` interface, prompt handling, security guardrails.
- **Finance module** – revenue, expense tracking, reporting, PDF/Excel export.
- **Analytics dashboard** – KPIs, custom date/vehicle filters, export.
- **CI/CD enhancements** – build Docker images, push to registry, deploy to AWS/EKS.
- **Observability** – OpenTelemetry, Grafana dashboards, alerting.

## Ongoing Activities
- **Testing** – unit, integration, and end‑to‑end Cypress tests for every UI flow.
- **Documentation** – keep `PROJECT_AUDIT.md`, `SYSTEM_ARCHITECTURE.md`, `DATABASE_DESIGN.md`, `API_DOCUMENTATION.md` up‑to‑date.
- **Security reviews** – regular OWASP checks, secret management, dependency scanning.

---
*The roadmap is a living document; each sprint will refine dates and expand scope based on stakeholder feedback.*