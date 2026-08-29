# Architecture Decision Records

This directory records choices that later code must respect. An `Accepted` status means the design
was chosen; it does not mean the runtime behavior exists. `docs/security-decisions.md` tracks the
current implementation evidence for each security-relevant decision.

| ADR | Decision | Status |
| --- | --- | --- |
| [0001](0001-modular-monolith.md) | Use a modular monolith | Accepted |
| [0002](0002-identity-platform-foundation.md) | Use ASP.NET Core Identity as an infrastructure foundation | Accepted |
| [0003](0003-server-managed-sessions.md) | Use server-managed opaque sessions | Accepted |
| [0004](0004-durable-state-and-coordination.md) | Use PostgreSQL for durable state and Redis for coordination | Accepted |
| [0005](0005-central-authentication-orchestration.md) | Centralize transactions, policy, OTP, and providers | Accepted |
| [0006](0006-application-profiles-and-registration.md) | Use application profiles and schema-driven registration | Accepted |
| [0007](0007-one-way-monorepo-mirror.md) | Mirror AuthNexus one way into nana-monorepo | Accepted |
