# AuthNexus

AuthNexus is a standalone identity and authentication platform. This repository is still in V0.1.
Its six Phase D domain roots now have an EF Core/PostgreSQL persistence layer: application-owned
repository contracts, infrastructure mappings and repositories, a shared unit of work, protected
notification destinations, and an accepted initial migration. The API does not register or invoke
this layer, and there is still no login endpoint or runtime authentication flow. Phase E is
complete; Phase F is the next planned phase and has not started.

## What works today

| Area | Current implementation |
| --- | --- |
| Web | Next.js 16 app in `apps/web`; `/` renders a static project-status page. |
| API | ASP.NET Core 10 host in `apps/api`; it starts without exposing product routes. |
| Backend | `AuthNexus.Application` owns repository and unit-of-work contracts. `AuthNexus.Infrastructure` maps the six persisted roots to PostgreSQL and supplies Npgsql repository adapters. No host composition or authentication orchestration is wired. |
| Local services | PostgreSQL 16.10, Redis 7.4.5, and Mailpit 1.27.4 in `compose.yaml`. |
| Tests | All 26 solution projects build cleanly; 571 backend cases across ten test projects cover the product contract, six Phase D roots, two architecture rules, and Phase E PostgreSQL integration and security behavior. |
| CI | The source workflow runs frontend, backend, and local Compose jobs separately. The backend job supplies a disposable PostgreSQL service for the Phase E suites. |

No application service currently loads these records. Login identifiers, registration workflows,
credentials, OTP challenges, social providers, passkeys, cookie handling, policy evaluation,
authentication orchestration, notification delivery, and production deployment are not
implemented. The documents under `docs/` distinguish checked-in implementation from tested or
running behavior.

## Start the local dependencies

Docker Desktop must be running. From the repository root:

```powershell
docker compose up --detach --wait
powershell -NoProfile -ExecutionPolicy Bypass -File infra/docker/verify-local-stack.ps1
```

The default endpoints are deliberately bound to the local machine:

| Service | Address | Local purpose |
| --- | --- | --- |
| PostgreSQL | `localhost:5432` | Durable local dependency; the API is not connected |
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

docker compose config --quiet
docker compose up --detach --wait --wait-timeout 120
powershell -NoProfile -ExecutionPolicy Bypass -File infra/docker/verify-local-stack.ps1

$env:AUTHNEXUS_TEST_POSTGRES_CONNECTION = "Host=127.0.0.1;Port=5432;Database=postgres;Username=authnexus;Password=authnexus-local-postgres;Include Error Detail=true"
dotnet test AuthNexus.sln --configuration Release --no-build
Remove-Item Env:AUTHNEXUS_TEST_POSTGRES_CONNECTION -ErrorAction SilentlyContinue
```

## Repository map

```text
apps/api/                       ASP.NET Core process
apps/web/                       Next.js process
src/backend/AuthNexus.*         shared technical-layer assemblies
src/backend/Modules/*/          eleven product-module class libraries
src/backend/AuthNexus.Application/Persistence/
                                repository and unit-of-work ports
src/backend/AuthNexus.Infrastructure/Persistence/
                                EF Core model, repositories, protection, and migrations
tests/architecture/             compiled-module and project-graph checks
tests/unit/                     product and Phase D domain-boundary tests
tests/integration/              Phase E migration, repository, transaction, and concurrency tests
tests/security/                 Phase E constraints, immutability, and destination-protection tests
tests/e2e/                      reserved for later runnable product journeys
tests/shared/                   guarded disposable-PostgreSQL test database support
infra/docker/                   local-stack verification
docs/decisions/                 accepted architecture decisions
docs/implementation-notes/      phase records with explicit implementation/evidence status
.config/dotnet-tools.json       repository-local dotnet-ef version
compose.yaml                    PostgreSQL, Redis, and Mailpit
```

## Release progress

V0.1 is delivered as small, reviewable phases. Phases A through E are complete. The Phase E gate
passed 20 integration cases, 98 persistence-security cases, both architecture rules, and the full
571-case backend suite. Phase F is next but not started. No `v0.1.0` tag exists because Phases F
through N remain open.

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
