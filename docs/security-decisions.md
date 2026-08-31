# Security Decision Status

The ADRs under `docs/decisions` explain the accepted design. This page records which parts have
corresponding code today.

Phase E entries below include database acceptance evidence from 20 integration and 98 security
cases. Those cases used guarded disposable databases; they did not alter the durable local
`authnexus` database.

| Decision | Current evidence | Still missing |
| --- | --- | --- |
| Modular monolith | Eleven module assemblies, one application orchestration boundary, six module-owned roots, application-owned persistence ports, infrastructure adapters, a shared unit of work, and both architecture rules passing. | API composition and cross-module runtime handlers. |
| Explicit account state | Six states, seven named legal transitions, all 35 forbidden state/action pairs, relational lifecycle checks, and a passing competing-writer conflict test. | Atomic audit workflow, administrative authorization, and runtime state checks. |
| PostgreSQL is durable | Pinned local container and health query, one schema-separated Npgsql/EF Core model, migration-history placement, clean apply/downgrade/reapply, every declared check constraint exercised, and no pending model changes. | Backups, restore exercises, production service, and production database operations. |
| Redis is coordination only | Authenticated local container with append-only local data. | Consumers, outage policy, rate-limit implementation. |
| Server-managed opaque sessions | Phase D's fixed-size verifier and lifecycle rules plus an accepted mapping, unique verifier index, explicit non-null terminal predicates, round trip, stale-writer conflict, and relational negative cases. | Secret generation and derivation, constant-time verification, cookie transport, middleware, runtime lookup, authorization, cross-node invalidation, and endpoints. |
| Central authentication transaction | Eight states, 14 purposes, the accepted domain transition matrix, lifecycle mapping and indexes, explicit non-null terminal predicates, round trip, stale-writer conflict, and relational negative cases. | Challenge/evidence verification, policy and risk input, user binding after creation, orchestration, atomic audit/outbox output, and endpoints. |
| Append-oriented security events | Immutable domain event, fixed 37-code catalogue, bounded metadata, append/get repository, JSONB mapping, plus passing tracked-EF and direct-SQL update/delete/truncate rejection cases. | Trusted event builders, atomic emission, query services, retention, authorization, and exports. |
| Notification outbox persistence | Protected payload, redacted destination, row-bound AES-GCM envelope, explicit lifecycle predicates, due query, optimistic conflicts, rollback coverage, plaintext-absence inspection, tamper and substitution rejection, and key-rotation tests. | Originating workflow, claim/lease, worker, retry policy, providers, receipts, operational replay, and managed key storage. |
| Provider adapters | Architecture decision only. | Interfaces, fakes, and all production adapters. |
| One-way monorepo mirror | Exact source SHA, subtree history, target sync workflow. | Two dedicated repository credentials for automatic sync. |

The Phase E `identity.user_accounts` table stores the current AuthNexus `UserAccount` lifecycle
record. It is not an ASP.NET Core Identity store and does not introduce identifiers or credentials.

Acceptance corrected two fail-open schema details before Phase E was closed. The profile/redirect
foreign key is now deferrable like the cardinality triggers, and nullable terminal timestamps in
authentication, session, and outbox checks are guarded with explicit `IS NOT NULL` predicates so
PostgreSQL's `UNKNOWN` check result cannot admit an incomplete lifecycle row.

## Local-stack safety

Phase B publishes PostgreSQL, Redis, Mailpit SMTP, and Mailpit UI only on `127.0.0.1`. Redis and
PostgreSQL require the Compose credentials. Those credentials are checked-in development defaults,
not secrets, and must never be copied into a shared or production environment. The stack uses
`no-new-privileges` and named volumes, but it has no TLS, backup policy, secret manager, or hardened
production network.

The repository ignores `.env`, private-key formats, and common secret files. Ignore rules reduce
accidental commits; they are not a secret-scanning or runtime secret-management system.
