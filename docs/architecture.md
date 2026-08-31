# Architecture

## Current process and dependency map

```text
apps/web (Next.js, :3000)       apps/api (ASP.NET Core, :5220)
           no API calls yet          no product routes yet
                                        |
                                        | not wired yet
                                        v
             PostgreSQL :5432   Redis :6379   Mailpit SMTP :1025 / UI :8025
```

`compose.yaml` owns only the three local dependencies. The web and API run as host processes so
their normal development tools and reload behavior remain available. All dependency ports publish
to `127.0.0.1`.

The API references `AuthNexus.Application`, `AuthNexus.Contracts`, and
`AuthNexus.Infrastructure`. The production project graph is now:

```text
AuthNexus.Api
├── AuthNexus.Application
├── AuthNexus.Contracts
└── AuthNexus.Infrastructure

AuthNexus.Infrastructure
├── AuthNexus.Application
├── AuthNexus.Contracts
├── AuthNexus.Domain
├── AuthNexus.Modules.Applications
├── AuthNexus.Modules.Audit
├── AuthNexus.Modules.Authentication
├── AuthNexus.Modules.Identity
├── AuthNexus.Modules.Notifications
└── AuthNexus.Modules.Sessions

AuthNexus.Application
├── AuthNexus.Contracts
├── AuthNexus.Domain
└── AuthNexus.Modules.* (eleven product modules)

AuthNexus.Modules.Applications -> AuthNexus.Domain
AuthNexus.Modules.Authentication -> AuthNexus.Domain
AuthNexus.Modules.Identity     -> AuthNexus.Domain
AuthNexus.Modules.Sessions     -> AuthNexus.Domain
AuthNexus.Modules.Audit        -> AuthNexus.Domain
AuthNexus.Modules.Notifications -> AuthNexus.Domain
other five modules             -> no project references
AuthNexus.Contracts            -> no project references
AuthNexus.Domain               -> no project references
```

Each product module is a separate class-library assembly under `src/backend/Modules`. The
application assembly remains the cross-module orchestration boundary; modules do not reference one
another, Infrastructure, or the API. The six concrete modules depend inward on Domain for shared
identifiers. Infrastructure depends on those same six module assemblies only to translate between
domain objects and its private relational records. Product modules remain free of EF Core and
PostgreSQL packages. The architecture suite contains the expected graph above, and its Phase E
update passed both architecture cases. The complete solution contains 26 projects: the
production projects, the established unit and architecture projects, and the two Phase E
persistence test projects.

Applications owns `ApplicationProfile`; Identity owns `UserAccount`; Authentication owns the
expiring `AuthenticationTransaction`; Sessions owns the server-side session record; Audit owns
immutable `SecurityEvent`; and Notifications owns the protected outbox message and delivery-state
record. Application exposes a repository port for each boundary and one shared unit-of-work port.
Infrastructure implements them with `AuthNexusDbContext`, Npgsql, and private row types. The other
five module assemblies contain markers only. The API still has no persistence registration,
runtime resolver, worker, or product route.

## Current persistence boundary

One EF Core context owns the following PostgreSQL model:

| Schema | Table | Domain boundary | Repository operations |
| --- | --- | --- | --- |
| `applications` | `application_profiles` | `ApplicationProfile` | add, get |
| `applications` | `application_redirect_uris` | Redirect allowlist children | loaded with the profile |
| `identity` | `user_accounts` | `UserAccount` | add, get, update |
| `authentication` | `authentication_transactions` | `AuthenticationTransaction` | add, get, update |
| `sessions` | `sessions` | `Session` | add, get, update |
| `audit` | `security_events` | `SecurityEvent` | append, get |
| `notifications` | `outbox_messages` | `NotificationOutboxMessage` | add, get, get due, update |
| `infrastructure` | `__ef_migrations_history` | EF migration history | EF-managed |

Repository writes only stage changes. `CommitAsync` saves the tracked unit of work;
`ExecuteInTransactionAsync` invokes one operation and commit inside a PostgreSQL transaction.
`UserAccount`, `AuthenticationTransaction`, `Session`, and outbox updates carry a GUID persistence
version so EF can reject stale writes. `SecurityEvent` is insert-only in the repository and in the
change-tracker guard. The migration declares the relational constraints and the database-side
append-only rule. The accepted integration and security suites exercised the migration, all six
repositories, four stale-writer conflicts, transaction rollback, each declared check constraint,
and the audit mutation triggers against disposable PostgreSQL databases.

Application-profile redirects are the only current relational child relationship. Identifiers
that point across module roots are stored as typed UUID values without cross-module foreign keys.
The redirect foreign key and minimum-cardinality triggers are deferrable and initially deferred,
so either row can be staged first while commit still rejects a profile with no redirect. No seed
data is defined.

The test databases use random guarded names beginning with `authnexus_test_`. Migration
apply/downgrade/reapply never targeted the durable local `authnexus` database or its Compose
volume, and the accepted run left no disposable databases behind. The EF model and migration
snapshot report no pending changes.

## Chosen end-state shape

AuthNexus is being built as a modular monolith. Authentication often updates an account,
transaction, challenge, session, audit event, and outbox entry together. Keeping those writes in
one process and one PostgreSQL transaction is simpler to reason about than distributing the first
version across services.

PostgreSQL is the only mapped durable store. Redis still has no application consumer and remains
reserved for cache entries, rate limits, and short-lived coordination; losing it must not erase an
identity, session record, challenge result, or audit event. Mailpit is a local email sink only.

The planned request path is:

```text
consumer redirect -> Next.js experience -> ASP.NET Core API
                  -> application/policy resolution
                  -> one authentication transaction
                  -> method adapter
                  -> identity/session decision
                  -> audit + outbox in PostgreSQL
```

Profile construction, redirect allowlist membership, account/transaction/session lifecycle,
immutable event construction, outbox delivery-state rules, and persistence adapters now exist. The
request path is not executable: no application service resolves a profile, changes an account or
transaction, issues a session, emits a security event, stages an outbox message, or accepts an
authentication request.

## Repository and mirror boundary

`NanaAddoDBa/authnexus` owns development, CI, tags, and releases. After source CI passes, the
monorepo imports the exact source commit into `apps/authnexus` and records it in
`.source-revision`. Source `.github/` files are excluded because workflows apply only at a
repository root. There is no reverse sync or submodule.

The current monorepo detector supports its established Node.js and Go deployment contracts.
AuthNexus is not registered there while its mixed Next.js/.NET deployment contract is undefined;
standalone CI remains the build authority.
