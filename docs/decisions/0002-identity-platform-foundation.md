# ADR 0002: Use ASP.NET Core Identity as an Infrastructure Foundation

**Status:** Accepted

## Decision

AuthNexus will use ASP.NET Core Identity as maintained local-credential infrastructure while
explicitly modeling platform concepts such as accounts, identifiers, credentials, transactions,
sessions, evidence, policies, audit, and outbox state.

## Consequences

Framework support does not replace the domain model. Provider-specific behavior, account linking,
policy, and session ownership remain AuthNexus responsibilities.

Phase E persists only AuthNexus's `UserAccount` domain record. It does not introduce ASP.NET Core
Identity stores, identifier tables, credential tables, password hashes, or framework sign-in
behavior.
