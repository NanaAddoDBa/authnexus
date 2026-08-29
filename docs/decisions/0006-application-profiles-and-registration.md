# ADR 0006: Use Application Profiles and Schema-Driven Registration

**Status:** Accepted

## Decision

Consuming applications configure branding, redirects, locale, registration fields, authentication
methods, policy, sessions, and recovery through application profiles. Registration fields are
schema driven and verified identity is collected before unnecessary profile data where possible.

## Consequences

AuthNexus avoids per-application authentication implementations and does not execute arbitrary
schema code. Concrete profile and schema persistence are introduced in V0.1 and later slices.
