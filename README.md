# Justine & Fabien

A bilingual wedding website with a public experience, private household RSVP management, and an organizer dashboard.

## Overview

This monorepo includes:

- Public wedding landing page in English and French
- Household registration and login flow
- RSVP management for adults and children
- Private organizer dashboard for households and RSVP health
- Azure-ready backend and infrastructure scaffolding following the project plan

## Product constraints

- Public registration is allowed
- One account per household
- No invitation code, no email verification, no password recovery
- Auth is scoped to household users and administrators only
- All private actions must be protected by server-side authorization
- Public editorial content stays bilingual
- Destructive admin actions require confirmation and audit logging

## Local setup

1. Open the repository root in VS Code.
2. Install dependencies with npm:
   npm install
3. Start the public web app:
   npm run dev:web
4. Start the admin app:
   npm run dev:admin

## Project structure

- apps/web: public wedding website
- apps/admin: private organizer area
- api: Azure Functions backend and shared contracts
- infrastructure: Bicep and deployment artifacts
- docs: architecture and operational guidance

## Notes on auth and RSVP

The household flow keeps account credentials in browser storage for local-first prototyping and stores only a password hash plus salt. The RSVP experience lets a household manage event attendance, guest profiles, and dietary notes without exposing private content publicly.

## Persistent backend and secrets

The API now uses Azure Table Storage for household records and audit entries through the `TableStorage__ConnectionString` environment variable. For local development, point it to the Azurite or Azure Storage emulator and define a strong admin key in `WeddingAdminKey`.

Example values:

- `TableStorage__ConnectionString=UseDevelopmentStorage=true`
- `WeddingTableName=Households`
- `WeddingAdminKey=change-me-admin-key`

Private household routes expect an authenticated household identifier in the `x-household-id` header or an `Authorization: Bearer <householdId>` header. Admin endpoints require the `x-admin-key` header or `Authorization: Bearer <adminKey>`.

## Next phases

- Connect the web app to the persistent backend/API layer
- Extend the admin confirmation flow for destructive actions
- Extend the gallery and media workflow
- Deploy infrastructure to Azure
