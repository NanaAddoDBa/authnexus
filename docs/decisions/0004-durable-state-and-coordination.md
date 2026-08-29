# ADR 0004: Use PostgreSQL for Durable State and Redis for Coordination

**Status:** Accepted

## Decision

PostgreSQL will be the durable authority for security-critical data. Redis may provide distributed
rate limiting, short-lived coordination, risk counters, and justified revocation propagation, but
it will not be the sole durable source of security-critical state.

## Consequences

Redis failure behavior must be explicit and observable. Database migrations begin in V0.1 Phase E;
the local service stack begins in Phase B.
