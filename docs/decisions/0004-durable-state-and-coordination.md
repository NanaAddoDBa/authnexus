# ADR 0004: Use PostgreSQL for Durable State and Redis for Coordination

**Status:** Accepted

## Decision

PostgreSQL will be the durable authority for security-critical data. Redis may provide distributed
rate limiting, short-lived coordination, risk counters, and justified revocation propagation, but
it will not be the sole durable source of security-critical state.

## Consequences

Redis failure behavior must be explicit and observable. Phase B introduced the local service
stack. Phase E introduces the first PostgreSQL model and migration; it does not add a Redis
application consumer.
