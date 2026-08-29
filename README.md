# AuthNexus

AuthNexus is a standalone identity and authentication platform. This repository is still in V0.1:
it has a buildable web/API skeleton and a working local dependency stack, but it does not have a
login endpoint or a user table yet.

## What works today

| Area | Current implementation |
| --- | --- |
| Web | Next.js 16 app in `apps/web`; `/` renders a static project-status page. |
| API | ASP.NET Core 10 host in `apps/api`; it starts without exposing product routes. |
| Backend | Eleven compiled module assemblies behind the existing technical layers; markers only, with no domain behavior or persistence. |
| Local services | PostgreSQL 16.10, Redis 7.4.5, and Mailpit 1.27.4 in `compose.yaml`. |
| Tests | One product-contract test, two executable architecture rules, and web type-check, lint, and build checks. |
| CI | Frontend, backend, and local Compose runtime are validated independently. |

Registration, passwords, OTPs, social providers, passkeys, sessions, migrations, policy evaluation,
and production deployment are not implemented. The documents under `docs/` record how those
pieces will be added; they are not evidence that the code exists.

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
tests/unit/                     product contract tests
tests/integration|security|e2e  reserved test roots with scope notes
infra/docker/                   local-stack verification
docs/decisions/                 accepted architecture decisions
docs/implementation-notes/      completed task records and test evidence
compose.yaml                    PostgreSQL, Redis, and Mailpit
```

## Release progress

V0.1 is delivered as small, reviewable phases. Phase A established the repository and downstream
mirror, Phase B established the local dependency stack, and Phase C established compile-time
module boundaries. Phase D introduces the first domain model and invariants; persistence remains a
later phase. No `v0.1.0` tag exists because the rest of V0.1 has not passed acceptance.

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
