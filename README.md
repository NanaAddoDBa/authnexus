# AuthNexus

AuthNexus is a standalone identity and authentication platform. This repository is still in V0.1:
it has a buildable web/API skeleton, a working local dependency stack, and all six Phase D
in-memory domain foundations. It still has no login endpoint, database tables, or runtime
authentication orchestration.

## What works today

| Area | Current implementation |
| --- | --- |
| Web | Next.js 16 app in `apps/web`; `/` renders a static project-status page. |
| API | ASP.NET Core 10 host in `apps/api`; it starts without exposing product routes. |
| Backend | Eleven compiled module assemblies. Applications, Identity, Authentication, Sessions, Audit, and Notifications own the six Phase D records and invariants; the other five modules still contain markers only. There is no persistence or runtime orchestration. |
| Local services | PostgreSQL 16.10, Redis 7.4.5, and Mailpit 1.27.4 in `compose.yaml`. |
| Tests | 453 backend cases cover the product contract, all six Phase D boundaries, and two executable architecture rules; the web has type-check, lint, and build checks. |
| CI | Frontend, backend, and local Compose runtime are validated independently. |

Application/account/transaction loading, login identifiers, registration, passwords, OTPs,
social providers, passkeys, database mappings, cookie handling, policy evaluation, transaction
orchestration, notification delivery, and production deployment are not implemented. The
documents under `docs/` distinguish current code from later work.

## Start the local dependencies

Docker Desktop must be running. From the repository root:

```powershell
docker compose up --detach --wait
powershell -NoProfile -ExecutionPolicy Bypass -File infra/docker/verify-local-stack.ps1
```

The default endpoints are deliberately bound to the local machine:

| Service | Address | Local purpose |
| --- | --- | --- |
| PostgreSQL | `localhost:5432` | Future durable identity data |
| Redis | `localhost:6379` | Future non-authoritative coordination |
| Mailpit SMTP | `localhost:1025` | Capture development email |
| Mailpit UI | `http://localhost:8025` | Inspect captured email |

The checked-in passwords are disposable local defaults. Copy `.env.example` to `.env` only when
you need different ports or credentials. Never reuse these values outside local development.

Stop the containers while retaining their volumes:

```powershell
docker compose down
```

Delete the local databases and captured mail as well:

```powershell
docker compose down --volumes
```

See [Local development](docs/local-development.md) for web/API commands, verification output, and
port-conflict recovery.

## Validate the source tree

```powershell
pnpm --dir apps/web install --frozen-lockfile
pnpm --dir apps/web typecheck
pnpm --dir apps/web lint
pnpm --dir apps/web build

dotnet restore AuthNexus.sln
dotnet build AuthNexus.sln --configuration Release --no-restore
dotnet test AuthNexus.sln --configuration Release --no-build

docker compose config --quiet
docker compose up --detach --wait --wait-timeout 120
powershell -NoProfile -ExecutionPolicy Bypass -File infra/docker/verify-local-stack.ps1
```

## Repository map

```text
apps/api/                       ASP.NET Core process
apps/web/                       Next.js process
src/backend/AuthNexus.*         shared technical-layer assemblies
src/backend/Modules/*/          eleven product-module class libraries
tests/architecture/             compiled-module and project-graph checks
tests/unit/                     product and Phase D domain-boundary tests
tests/integration|security|e2e  reserved test roots with scope notes
infra/docker/                   local-stack verification
docs/decisions/                 accepted architecture decisions
docs/implementation-notes/      completed task records and test evidence
compose.yaml                    PostgreSQL, Redis, and Mailpit
```

## Release progress

V0.1 is delivered as small, reviewable phases. Phases A through C established the repository,
local dependencies, and compile-time module boundaries. Phase D now defines `ApplicationProfile`,
`UserAccount`, `AuthenticationTransaction`, `Session`, immutable `SecurityEvent`, and the protected
`NotificationOutboxMessage` delivery-state record. These are in-memory contracts, not running
authentication capability. Persistence remains Phase E work and has not started. No `v0.1.0` tag
exists because the rest of V0.1 has not passed acceptance.

The detailed phase ledger is in [docs/releases/v0.1.md](docs/releases/v0.1.md).

## Source ownership

[`NanaAddoDBa/authnexus`](https://github.com/NanaAddoDBa/authnexus) is the only development and
release repository. `NanaAddoDBa/nana-monorepo/apps/authnexus` is an ordinary-files mirror of a
specific green source commit. Changes made in the mirror are not synchronized back.

The source `.github/` directory is excluded from the mirror. The monorepo owns its sync workflow
and records the imported source SHA in `apps/authnexus/.source-revision`.

## Working documents

- [Product boundary](docs/product-scope.md)
- [Current and planned architecture](docs/architecture.md)
- [Local development](docs/local-development.md)
- [Testing contract](docs/testing.md)
- [Threat model](docs/threat-model.md)
- [Security decisions](docs/security-decisions.md)
- [V0.1 phase ledger](docs/releases/v0.1.md)
- [Security reporting](SECURITY.md)
