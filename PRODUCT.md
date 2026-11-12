# Product

<!-- impeccable:product-schema 1 -->

## Platform
web

## Users
Fleet managers and operations leads at Ethiopian transport businesses are the primary users. Company administrators, dispatchers, drivers, accountants, maintenance managers, and read-only analysts are additional users with distinct responsibilities.

## Product Purpose
EthioFleet OS is a multi-tenant fleet management platform for organizing vehicle, driver, trip, fuel, maintenance, finance, and reporting workflows. Success means operators can manage fleet work from accurate, tenant-scoped records and make operational decisions without relying on disconnected spreadsheets.

## Positioning
The product is initially designed for the Ethiopian market and uses ETB by default, with room to support other currencies and markets. No distinct product differentiator beyond this Ethiopia-first fleet focus has been confirmed; do not invent one.

## Operating Context
Businesses may operate multiple vehicles and users across fleets and roles. Fleet managers and operations leads need a clear view of fleet readiness, current work, and upcoming maintenance. The current documented product language is English. Ethiopia is the initial market; East Africa and broader markets are future expansion goals.

## Capabilities and Constraints
- The existing backend has tenant-scoped vehicle, driver, assignment, trip, fuel, maintenance, inspection, vehicle-document, incident, notification, dashboard, and fleet-report APIs.
- The frontend is Angular 22; the backend is ASP.NET Core/.NET 10 with PostgreSQL.
- Email confirmation is required for new account sign-in, but no email provider is wired. Email-dependent registration and recovery operations currently return an explicit service-unavailable response.
- Role names are seeded, but the complete role-to-operation permission matrix is not implemented.
- Live GPS, AI providers, SMS/email delivery, and production deployment integrations are not connected. Never present simulated data or unconfigured integrations as live.
- ETB is the default currency; configurable currency support is a product requirement.
- The interface should support responsive layouts, accessible contrast, and both light and dark modes.

## Brand Commitments
The product name is EthioFleet OS. The project brief cites Linear, Stripe Dashboard, Microsoft Dynamics 365, and Samsara as design inspiration, not as copying instructions. No logo, established palette, or required typeface has been provided.

## Evidence on Hand
Repository code and tests are the available evidence for existing functionality. No customer testimonials, fleet benchmarks, external provider credentials, logo assets, or live GPS integration have been provided. Do not fabricate these.

## Product Principles
- Keep tenant boundaries explicit and enforce them in both authorization and data access.
- Show operational status and data provenance honestly; distinguish estimates and simulations from live information.
- Optimize for fleet managers' and operations leads' everyday decisions, not decorative dashboards.
- Preserve ETB as the default while keeping currency handling explicit.
- Build role-appropriate workflows without granting unreviewed cross-tenant access.

## Accessibility & Inclusion
Use responsive layouts, keyboard-operable controls, semantic labels, visible focus, and sufficient color contrast. Do not rely on color alone to communicate status.
