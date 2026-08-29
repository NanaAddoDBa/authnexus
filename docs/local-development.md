# Local Development

## Required tools

The repository currently pins or tests against:

- Node.js 22 and pnpm 10.18.3 for `apps/web`.
- .NET SDK 10.0.400 through `global.json`.
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
health route and is not wired to the Compose services; both facts are intentional at the Phase B
boundary.

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

Phase B provides development dependencies and their health contract only. It does not add EF Core,
database migrations, Redis consumers, SMTP delivery code, fake SMS/WhatsApp adapters, or an OIDC
simulator. Those pieces require contracts owned by later V0.1 phases.
