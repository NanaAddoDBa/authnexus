# ADR 0003: Use Server-Managed Opaque Sessions

**Status:** Accepted

## Decision

Browser authentication will use secure `HttpOnly` cookies carrying opaque random session secrets.
Only hashes are stored server side. Sessions support rotation, revocation, idle expiry, absolute
expiry, authentication evidence, and step-up state.

## Consequences

Long-lived browser tokens in `localStorage` are not an AuthNexus session mechanism. Concrete
session implementation begins in V0.1 Phase G.
