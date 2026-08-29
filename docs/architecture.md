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
└── AuthNexus.Domain

AuthNexus.Application
├── AuthNexus.Contracts
├── AuthNexus.Domain
└── AuthNexus.Modules.* (eleven product modules)

AuthNexus.Modules.*  -> no project references
AuthNexus.Contracts  -> no project references
AuthNexus.Domain     -> no project references
```

Each product module is a separate class-library assembly under `src/backend/Modules`. The
application assembly is the cross-module orchestration boundary; modules do not reference one
another, infrastructure, or the API. Architecture tests compare the checked-in project files with
this exact graph.

The module assemblies contain markers only. There are still no domain entities, services,
repositories, dependency-injection registrations, or API routes.

## Chosen end-state shape

AuthNexus is being built as a modular monolith. Authentication often updates an account,
transaction, challenge, session, audit event, and outbox entry together. Keeping those writes in
one process and one PostgreSQL transaction is simpler to reason about than distributing the first
version across services.

PostgreSQL will be the durable record. Redis may coordinate cache entries, rate limits, and short
lived signals, but losing Redis must not erase an identity, session record, challenge result, or
audit event. Mailpit is a local email sink only.

The planned request path is:

```text
consumer redirect -> Next.js experience -> ASP.NET Core API
                  -> application/policy resolution
                  -> one authentication transaction
                  -> method adapter
                  -> identity/session decision
                  -> audit + outbox in PostgreSQL
```

None of that request path is executable yet.

## Repository and mirror boundary

`NanaAddoDBa/authnexus` owns development, CI, tags, and releases. After source CI passes, the
monorepo imports the exact source commit into `apps/authnexus` and records it in
`.source-revision`. Source `.github/` files are excluded because workflows apply only at a
repository root. There is no reverse sync or submodule.

The current monorepo detector supports its established Node.js and Go deployment contracts.
AuthNexus is not registered there while its mixed Next.js/.NET deployment contract is undefined;
standalone CI remains the build authority.
