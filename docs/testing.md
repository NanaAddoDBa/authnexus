# Testing Strategy

AuthNexus uses cumulative testing aligned with each capability release:

```text
Unit → Integration → Provider/contract → End-to-end → Security → Load/abuse → Operational failure
```

Every authentication method eventually requires success, failure, expiry, replay, abuse,
enumeration, concurrency, session, audit, and log-redaction coverage.

## Current Phase A evidence

The repository contains one focused xUnit contract test that verifies the canonical AuthNexus
product identity. It is not presented as authentication behavior coverage. The web scaffold is
validated through TypeScript, lint, and production build checks; the .NET solution is restored,
built, and tested.

Integration, security, and browser test roots exist but contain no behavior-dependent tests until
the platform has behavior to exercise. Testcontainers and Playwright are introduced with the
corresponding infrastructure and experience phases.
