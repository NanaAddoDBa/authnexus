# Testing Contract

## Accepted command surface

| Boundary | Command | Current accepted result |
| --- | --- | --- |
| Web types | `pnpm --dir apps/web typecheck` | Passed. |
| Web lint | `pnpm --dir apps/web lint` | Passed. |
| Web production build | `pnpm --dir apps/web build` | Passed. |
| Backend build | `dotnet build AuthNexus.sln --configuration Release --no-restore` | All 26 solution projects compiled with zero warnings and zero errors. |
| Full backend suite | `dotnet test AuthNexus.sln --configuration Release --no-build` | 571 of 571 cases passed across ten test projects. |
| Architecture | `dotnet test tests/architecture/AuthNexus.Architecture.Tests --configuration Release` | Both compiled-module and direct-reference rules passed. |
| Persistence integration | `dotnet test tests/integration/AuthNexus.Persistence.Integration.Tests --configuration Release` | 20 of 20 cases passed against disposable PostgreSQL databases. |
| Persistence security | `dotnet test tests/security/AuthNexus.Persistence.Security.Tests --configuration Release` | 98 of 98 cases passed against disposable PostgreSQL databases. |
| EF model drift | `dotnet ef migrations has-pending-model-changes --project src/backend/AuthNexus.Infrastructure/AuthNexus.Infrastructure.csproj --startup-project src/backend/AuthNexus.Infrastructure/AuthNexus.Infrastructure.csproj --context AuthNexusDbContext --configuration Release --no-build` | No pending model changes. |
| Compose model | `docker compose config --quiet` | Passed. |
| Local dependencies | `infra/docker/verify-local-stack.ps1` | PostgreSQL query, authenticated Redis ping, and Mailpit readiness passed. |

The 571 backend cases are accounted for as follows:

| Test project | Cases |
| --- | ---: |
| Product contract | 1 |
| `ApplicationProfile` unit tests | 34 |
| `UserAccount` unit tests | 46 |
| `AuthenticationTransaction` unit tests | 116 |
| `Session` unit tests | 65 |
| `SecurityEvent` unit tests | 107 |
| `NotificationOutboxMessage` unit tests | 82 |
| Architecture tests | 2 |
| Phase E persistence integration tests | 20 |
| Phase E persistence security tests | 98 |
| **Total** | **571** |

## Disposable PostgreSQL contract

The two Phase E projects link `tests/shared/PostgreSqlTestDatabase.cs`. They never run migrations
against the durable `authnexus` database. For each fixture invocation, the helper connects through
the `postgres` administration database, creates a name in the exact form
`authnexus_test_<32 hexadecimal characters>`, runs the requested migration or test, clears Npgsql
pools, and drops that database with `DROP DATABASE ... WITH (FORCE)`.

Deletion is guarded in code. A name is rejected unless it has the `authnexus_test_` prefix, the
exact expected length, and a hexadecimal-only random suffix. The database connection used by a
test also has pooling disabled. These checks prevent the cleanup path from accepting `authnexus`,
`postgres`, an empty name, or a broad database target.

When `AUTHNEXUS_TEST_POSTGRES_CONNECTION` is absent, the local-only default is:

```text
Host=127.0.0.1;Port=5432;Database=postgres;Username=authnexus;Password=authnexus-local-postgres;Include Error Detail=true
```

Use an override only for a disposable local or CI PostgreSQL instance. The account must be able to
create and drop databases. Never point this variable at production, staging, a shared development
server, or a PostgreSQL cluster whose databases are not disposable.

Run the focused suites from the repository root:

```powershell
docker compose up --detach postgres --wait --wait-timeout 120

$env:AUTHNEXUS_TEST_POSTGRES_CONNECTION = "Host=127.0.0.1;Port=5432;Database=postgres;Username=authnexus;Password=authnexus-local-postgres;Include Error Detail=true"

dotnet test tests/integration/AuthNexus.Persistence.Integration.Tests `
  --configuration Release
dotnet test tests/security/AuthNexus.Persistence.Security.Tests `
  --configuration Release

Remove-Item Env:AUTHNEXUS_TEST_POSTGRES_CONNECTION -ErrorAction SilentlyContinue
```

The accepted Phase E run finished with zero `authnexus_test_*` databases left behind. The durable
`authnexus` database and the Compose `postgres-data` volume were not migrated, reset, truncated,
or deleted.

The model-drift command uses the design-time variable, not the test-administration variable. It is
read-only but the factory requires a non-empty connection string:

```powershell
$env:AUTHNEXUS_POSTGRES_CONNECTION = "Host=127.0.0.1;Port=5432;Database=authnexus;Username=authnexus;Password=authnexus-local-postgres"

dotnet ef migrations has-pending-model-changes `
  --project src/backend/AuthNexus.Infrastructure/AuthNexus.Infrastructure.csproj `
  --startup-project src/backend/AuthNexus.Infrastructure/AuthNexus.Infrastructure.csproj `
  --context AuthNexusDbContext `
  --configuration Release `
  --no-build

Remove-Item Env:AUTHNEXUS_POSTGRES_CONNECTION -ErrorAction SilentlyContinue
```

## Phase E persistence coverage

The 20 integration cases apply, downgrade, and reapply the real migration; inspect migration
history and model drift; round-trip all six roots; exercise all four optimistic-concurrency paths;
commit and roll back multi-record work; reuse a context after rollback; and verify due-message
filtering, exact-time inclusion, deterministic ordering, and batch validation.

The 98 security cases execute every declared check constraint and its nullable lifecycle
regressions; exercise both orders of the profile/first-redirect transaction; preserve the
at-least-one-redirect rule across replacement, move, delete, cascade, and truncate operations;
reject tracked and direct-SQL audit mutation; prove no plaintext destination column exists; and
exercise ciphertext tampering, unknown and wrong keys, cross-row copying, and key rotation.

Those tests found two schema defects before acceptance:

1. The redirect-cardinality triggers were deferred, but the child foreign key was immediate. A
   child-first transaction therefore failed before its parent could be inserted. The migration now
   makes `fk_application_redirect_uris_application_profiles` deferrable and initially deferred.
2. PostgreSQL check constraints accept an expression whose result is `UNKNOWN`. Equality against a
   nullable lifecycle timestamp could therefore pass without the timestamp being present. The
   authentication-transaction, session, and outbox lifecycle predicates now require each
   state-required nullable column with an explicit `IS NOT NULL` clause.

Both fixes are present in the EF model, migration, designer, and snapshot, and their regression
cases pass in the 98-case security suite.

## Phase boundary

These suites close Phase E because they accept the persistence layer introduced in Phase E. They
do not start Phase M. Phase M remains the later whole-product acceptance phase for executable HTTP
authentication journeys, runtime authorization, abuse handling, log behavior, and end-to-end
browser flows after Phases F through L supply those capabilities.

Every later authentication capability must add tests for its success path, ordinary rejection,
expiry, replay, enumeration behavior, concurrency, audit output, session effect, and log
redaction beside the implementation that makes those paths real.
