# Security Decision Status

The ADRs under `docs/decisions` explain the accepted design. This page records which parts have
corresponding code today.

| Decision | Current evidence | Still missing |
| --- | --- | --- |
| Modular monolith | Eleven module assemblies, one application orchestration boundary, `ApplicationProfile` owned by Applications, `UserAccount` owned by Identity, `AuthenticationTransaction` owned by Authentication, and executable tests for the exact direct-reference graph. | The remaining V0.1 domain roots, persistence transaction boundaries, runtime composition, and cross-module workflows. |
| Explicit account state | Six states, seven named legal transitions, all 35 forbidden state/action pairs tested, and terminal deletion without contradictory booleans. | Atomic audit emission, persistence/concurrency rules, administrative authorization, and runtime state checks. |
| PostgreSQL is durable | Pinned local container, volume, and query-based acceptance check. | EF Core model, migrations, backups, production service. |
| Redis is coordination only | Authenticated local container with append-only local data. | Consumers, outage policy, rate-limit implementation. |
| Server-managed opaque sessions | ADR 0003 only. | Cookie, secret generation, verifier storage, rotation, revocation. |
| Central authentication transaction | Eight explicit states, 14 fixed purposes, 18 legal and 38 forbidden state/action pairs, half-open expiry enforcement, four terminal outcomes, correlation identity, and 116 executable tests. | Challenge/evidence verification, policy and risk input, user binding after creation, persistence/concurrency, orchestration, atomic audit/outbox output, and endpoints. |
| Provider adapters | Architecture decision only. | Interfaces, fakes, and all production adapters. |
| One-way monorepo mirror | Exact source SHA, subtree history, target sync workflow. | Two dedicated repository credentials for automatic sync. |

## Local-stack safety

Phase B publishes PostgreSQL, Redis, Mailpit SMTP, and Mailpit UI only on `127.0.0.1`. Redis and
PostgreSQL require the Compose credentials. Those credentials are checked-in development defaults,
not secrets, and must never be copied into a shared or production environment. The stack uses
`no-new-privileges` and named volumes, but it has no TLS, backup policy, secret manager, or hardened
production network.

The repository ignores `.env`, private-key formats, and common secret files. Ignore rules reduce
accidental commits; they are not a secret-scanning or runtime secret-management system.
