# Local Development

## Required tools

The repository currently pins or tests against:

- Node.js 22 and pnpm 10.18.3 for `apps/web`.
- .NET SDK 10.0.400 through `global.json`.
- The repository-local `dotnet-ef` 10.0.11 tool declared in `.config/dotnet-tools.json`.
- Docker Engine with Compose v2-compatible commands for `compose.yaml`.

On the Windows development machine used for Phase B, Docker Engine 29.7.2 and Compose 5.4.0 were
used for acceptance.

## Dependency stack

`compose.yaml` starts three dependencies. It does not start the web or API processes.

| Compose service | Image | Host binding | Persistent volume |
| --- | --- | --- | --- |
| `postgres` | `postgres:16.10-alpine` | `127.0.0.1:5432` | `postgres-data` |
| `redis` | `redis:7.4.5-alpine` | `127.0.0.1:6379` | `redis-data` |
| `mailpit` | `axllent/mailpit:v1.27.4` | `127.0.0.1:1025`, `127.0.0.1:8025` | `mailpit-data` |

Start and verify them:

```powershell
docker compose config --quiet
docker compose up --detach --wait --wait-timeout 120
powershell -NoProfile -ExecutionPolicy Bypass -File infra/docker/verify-local-stack.ps1
docker compose ps
```

The verification script does more than inspect container state. It connects to the configured
PostgreSQL database, expects an authenticated Redis `PONG`, and runs Mailpit's built-in readiness
check. It exits non-zero on the first failed dependency.

Open `http://localhost:8025` to inspect email captured through `localhost:1025`.

## Configuration overrides

Compose supplies runnable defaults, so `.env` is optional. To change a port or local credential:

```powershell
Copy-Item .env.example .env
```

Then edit `.env`. The connection-string examples must be updated when their matching port,
username, or password changes. `.env` is ignored by Git.

The default values are intentionally obvious local credentials. They are unsuitable for a shared
server, CI secret, preview environment, or production deployment. All published ports bind to
`127.0.0.1`, not every network interface.

## Inspect the Phase E migration

The design-time factory reads `AUTHNEXUS_POSTGRES_CONNECTION` directly from the process
environment. A `.env` file is not loaded automatically. From the repository root:

```powershell
dotnet tool restore
$env:AUTHNEXUS_POSTGRES_CONNECTION = "Host=127.0.0.1;Port=5432;Database=authnexus;Username=authnexus;Password=authnexus-local-postgres"

dotnet ef migrations list `
  --project src/backend/AuthNexus.Infrastructure/AuthNexus.Infrastructure.csproj `
  --startup-project src/backend/AuthNexus.Infrastructure/AuthNexus.Infrastructure.csproj `
  --context AuthNexusDbContext

dotnet ef migrations script `
  --project src/backend/AuthNexus.Infrastructure/AuthNexus.Infrastructure.csproj `
  --startup-project src/backend/AuthNexus.Infrastructure/AuthNexus.Infrastructure.csproj `
  --context AuthNexusDbContext

dotnet ef migrations has-pending-model-changes `
  --project src/backend/AuthNexus.Infrastructure/AuthNexus.Infrastructure.csproj `
  --startup-project src/backend/AuthNexus.Infrastructure/AuthNexus.Infrastructure.csproj `
  --context AuthNexusDbContext `
  --configuration Release
```

EF stores its history in `infrastructure.__ef_migrations_history`. Listing or scripting a
migration does not change PostgreSQL. `dotnet ef database update` does change the database in the
named `postgres-data` volume and should be run only when that is intended. The API does not call
`Database.Migrate`, `EnsureCreated`, or `AddAuthNexusPersistence` during startup.

Notification-destination keys are runtime composition input to `AddAuthNexusPersistence`. There
is no runtime or production key in product configuration, secret-manager integration, or
production key-rotation procedure. Fixed values under `tests/shared` are test fixtures only.

## Run the Phase E database tests safely

The integration and security projects require permission to create and drop databases. They use
`AUTHNEXUS_TEST_POSTGRES_CONNECTION`, which is separate from the design-time
`AUTHNEXUS_POSTGRES_CONNECTION` above. If the test variable is absent, its exact local default is:

```text
Host=127.0.0.1;Port=5432;Database=postgres;Username=authnexus;Password=authnexus-local-postgres;Include Error Detail=true
```

The fixture uses the connection only as an administration connection. It forces the administration
database to `postgres`, disables pooling for each generated database connection, and creates a
fresh `authnexus_test_<32 hexadecimal characters>` database. Cleanup refuses any name that does
not match that exact prefix, length, and suffix format before issuing a forced database drop.

Start PostgreSQL and run the suites from the repository root:

```powershell
docker compose up --detach postgres --wait --wait-timeout 120

$env:AUTHNEXUS_TEST_POSTGRES_CONNECTION = "Host=127.0.0.1;Port=5432;Database=postgres;Username=authnexus;Password=authnexus-local-postgres;Include Error Detail=true"

dotnet test tests/integration/AuthNexus.Persistence.Integration.Tests `
  --configuration Release
dotnet test tests/security/AuthNexus.Persistence.Security.Tests `
  --configuration Release

Remove-Item Env:AUTHNEXUS_TEST_POSTGRES_CONNECTION -ErrorAction SilentlyContinue
```

Use that variable only with a disposable local or CI cluster. Its role must have `CREATE DATABASE`
and `DROP DATABASE` permission. Do not point it at production, staging, a shared development
server, or any cluster containing databases that the test process must not remove.

The accepted Phase E run left zero `authnexus_test_*` databases. It did not migrate, truncate,
reset, or delete the durable `authnexus` database or the `postgres-data` volume. Fixed encryption
keys in `tests/shared` are test fixtures only; product code contains no runtime or production key.

## Run the application processes

Install and start the web process:

```powershell
pnpm --dir apps/web install --frozen-lockfile
pnpm --dir apps/web dev
```

The web status page is available at `http://localhost:3000`.

Start the API in a second terminal:

```powershell
dotnet run --project apps/api/AuthNexus.Api.csproj --launch-profile http
```

The development profile listens at `http://localhost:5220`. The API currently has no product or
health route and does not register the Phase E persistence services. `AddAuthNexusPersistence` is
available to a later composition step, but Phase E does not connect the host.

## Stop, retain, or reset data

`docker compose down` removes the containers and network but retains the three named volumes.
Starting the stack again reuses the same PostgreSQL data, Redis append-only file, and Mailpit
database.

Use the destructive form only when you want a clean local state:

```powershell
docker compose down --volumes --remove-orphans
```

That command permanently removes the local AuthNexus database, Redis data, and captured messages.

## Port conflicts

If another local service owns 5432, 6379, 1025, or 8025, copy `.env.example` and change the
corresponding `AUTHNEXUS_*_PORT`. Confirm the resolved mapping with:

```powershell
docker compose config
```

Do not stop or delete an unrelated database just to free a default port.

## Current boundary

The local stack supplies PostgreSQL, Redis, and Mailpit. Phase E's PostgreSQL migration and
repositories are accepted, but they remain a library boundary. Redis consumers, SMTP delivery,
fake SMS/WhatsApp adapters, API startup wiring, automatic migration, authentication endpoints, and
an OIDC simulator remain unimplemented. Phase F is next and has not started.
