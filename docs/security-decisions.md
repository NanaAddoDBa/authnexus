# Security Decisions

The active Architecture Decision Records live in [decisions](decisions/README.md). The following
security boundaries are already accepted for AuthNexus, although most runtime implementation is
scheduled for later V0.1 phases:

- Server-managed opaque browser sessions instead of long-lived browser tokens.
- PostgreSQL as durable identity state and Redis only as non-authoritative coordination.
- Central authentication transactions and evidence rather than provider-specific session flows.
- Shared OTP security lifecycle separated from delivery adapters.
- Backend-authoritative policy and assurance decisions.
- Application profiles and schema-driven registration without arbitrary executable configuration.
- Transactional outbox delivery for security notifications and challenge delivery.
- One-way standalone-to-monorepo synchronization with no submodules or reverse writes.

The Phase A repository baseline also prevents direct secret commits through ignore rules and keeps
the example environment file limited to placeholders. Secret management, Data Protection key
persistence, HTTP hardening, rate limits, audit persistence, and redaction implementation are
not complete until their assigned phases and must not be assumed present.
