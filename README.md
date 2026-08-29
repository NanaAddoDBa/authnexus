# AuthNexus — Universal Authentication Platform

AuthNexus is a reusable, application-neutral identity and authentication platform for multiple
consuming applications. It is being engineered as production-capable security infrastructure, not
as a login-page demo or a collection of provider buttons.

## Current status

**Current version:** V0.1 — Universal application/session foundation
**Current phase:** Phase A — Repository foundation complete

The repository currently contains the initial Next.js web scaffold, ASP.NET Core 10 API and
backend solution boundaries, a small contract test, documentation foundations, and CI/mirror
automation definitions. It does **not** yet implement registration, password authentication,
OTP, federation, passkeys, TOTP, recovery, sessions, database migrations, Redis, Docker Compose,
or production-provider integrations.

## Product model

AuthNexus separates the reusable identity core from application-specific configuration:

```text
Consuming application
        ↓
Application profile, branding, redirects, registration schema, and policy
        ↓
AuthNexus Experience Engine
        ↓
AuthNexus authentication, identity, session, policy, audit, and notification core
```

The authentication engine is not rewritten for each consuming application.

## Architecture

AuthNexus is a modular monolith with:

- Next.js, React, TypeScript, and the App Router for the Experience Engine.
- ASP.NET Core 10 and C# for the authoritative API and platform core.
- PostgreSQL as the future durable store and Redis as non-authoritative coordination.
- Server-managed opaque sessions, centralized authentication transactions, policy-driven method
  eligibility, provider adapters, a shared OTP engine, audit events, and an outbox as planned
  platform foundations.

Read [architecture.md](docs/architecture.md) for the current design boundary and
[product-scope.md](docs/product-scope.md) for the product scope.

## Version roadmap

| Version | Primary capability |
| --- | --- |
| V0.1 | Universal application/session foundation |
| V0.2 | Email/password identity lifecycle |
| V0.3 | Shared Email/SMS/WhatsApp OTP |
| V0.4 | Google/Apple/Telegram federation |
| V0.5 | Passkeys |
| V0.6 | TOTP and recovery codes |
| V0.7 | Unified identity and account linking |
| V0.8 | Recovery and Security Center |
| V0.9 | Policy and assurance engine |
| V1.0 | Security operations and production assurance |

Every version preserves prior accepted capabilities and adds one complete coherent capability
slice. A release tag is created only after the complete version acceptance gate passes.

## Repository layout

```text
apps/
├── api/                         ASP.NET Core API host
└── web/                         Next.js Experience Engine

src/backend/
├── AuthNexus.Contracts/
├── AuthNexus.Domain/
├── AuthNexus.Application/
├── AuthNexus.Infrastructure/
└── Modules/                     Product-boundary map

tests/
├── unit/
├── integration/
├── security/
└── e2e/

docs/                            Product, architecture, security, release, and implementation notes
infra/                           Future local/deployment/observability ownership boundary
```

## Local prerequisites

- Node.js 22 or later.
- pnpm 10.18.3.
- .NET SDK 10.0.400 or a compatible later 10.0 feature band.

Validate the current foundation:

```powershell
pnpm --dir apps/web install --frozen-lockfile
pnpm --dir apps/web typecheck
pnpm --dir apps/web lint
pnpm --dir apps/web build

dotnet restore AuthNexus.sln
dotnet build AuthNexus.sln --configuration Release --no-restore
dotnet test AuthNexus.sln --configuration Release --no-build
```

Docker Compose, PostgreSQL, Redis, Mailpit, and local delivery fakes are added in V0.1 Phase B;
they are not available yet.

## Documentation

- [Product scope](docs/product-scope.md)
- [Architecture](docs/architecture.md)
- [Domain model](docs/domain-model.md)
- [Threat model](docs/threat-model.md)
- [Authentication flows](docs/authentication-flows.md)
- [Policy model](docs/policy-model.md)
- [Security decisions](docs/security-decisions.md)
- [Local development](docs/local-development.md)
- [Testing](docs/testing.md)
- [V0.1 release record](docs/releases/v0.1.md)

## Downstream monorepo mirror

The standalone `NanaAddoDBa/authnexus` repository is the AuthNexus source of truth. The ordinary
files under `NanaAddoDBa/nana-monorepo/apps/authnexus` are a one-way downstream mirror. Develop
AuthNexus only in this repository. The mirror design and its credential boundary are documented
in [docs/architecture.md](docs/architecture.md) and in the monorepo mirror guide once the target
integration is merged.

## Security

AuthNexus is security-sensitive software. Read [SECURITY.md](SECURITY.md) before reporting a
vulnerability or contributing security-sensitive changes.
