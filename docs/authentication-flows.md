# Authentication Flow Boundary

There is no authentication flow in the repository yet. D.3 provides an in-memory
`AuthenticationTransaction` with purpose, expiry, and legal state transitions, but no host creates
or persists one. D.4 provides an in-memory Session lifetime and revocation record, but it does not
generate a secret, set a cookie, look up a record, or authenticate a request.
The D.5 `SecurityEvent` is also not wired to these lifecycle methods; no successful or rejected
operation emits or persists an event yet.
The D.6 `NotificationOutboxMessage` can record delivery outcomes in memory, but no workflow writes
one, no database commits one beside a state change, and no worker sends it.
`apps/api/Program.cs` builds and runs an empty ASP.NET Core host;
`apps/web/src/app/page.tsx` renders project status. No endpoint accepts an identifier, password,
OTP, provider callback, transaction ID, or session cookie.

When flow work begins, every interactive method must enter the same server-owned sequence:

```text
resolve ApplicationProfile
create AuthenticationTransaction
evaluate allowed methods and required assurance
collect one method's evidence
resolve registration, identity, or explicit linking
re-evaluate assurance
issue or upgrade an opaque server-managed session
write SecurityEvent and NotificationOutbox records
redirect only to an allowlisted destination
```

A password verifier, Google callback, passkey assertion, or OTP check may produce evidence. It may
not independently issue a session, merge accounts, change application policy, or choose the return
URL. This rule keeps provider-specific code out of the orchestration layer.

The D.3 aggregate protects lifecycle order only. It does not prove that a challenge or primary or
step-up factor succeeded; the later orchestrator must validate structured evidence before invoking
those transitions. Detailed request/response paths will be added with executable orchestration and
host tests. Until then, this file defines ownership, not an API contract.
