# Local Development

## Current V0.1 Phase A setup

AuthNexus currently requires Node.js 22+, pnpm 10.18.3, and .NET SDK 10.0.400.

Install and validate the web scaffold:

```powershell
pnpm --dir apps/web install --frozen-lockfile
pnpm --dir apps/web dev
```

In another terminal, validate the backend solution:

```powershell
dotnet restore AuthNexus.sln
dotnet build AuthNexus.sln --configuration Release --no-restore
dotnet test AuthNexus.sln --configuration Release --no-build
```

The API project is intentionally route-free during Phase A. It is a buildable application host,
not a usable authentication server yet.

## Configuration safety

Copy `.env.example` only for local placeholders. Never commit `.env` files, provider credentials,
tokens, keys, production connection strings, or test-account secrets.

## Deferred local runtime

Docker Compose, PostgreSQL, Redis, Mailpit, fake SMS, fake WhatsApp, health checks, and
observability services belong to V0.1 Phase B. Their absence is expected in Phase A.
