# Authentication Flow Boundary

There is no authentication flow in the repository yet. Persistence adapters now exist for
`ApplicationProfile`, `UserAccount`, `AuthenticationTransaction`, `Session`, `SecurityEvent`, and
`NotificationOutboxMessage`, but no application service or host invokes them. No request creates
an authentication transaction, verifies evidence, changes account state, or issues a session.

`ISecurityEventRepository` can stage an append, but no lifecycle workflow emits an event.
`INotificationOutboxRepository` can stage and query messages, and the shared unit of work can
commit several staged records in one transaction, but no workflow currently stages a state change
and outbox insert together. There is no outbox claim/lease or delivery worker.

The D.4 session record still does not generate a secret, set a cookie, look up a request's
credential, or authenticate that request.
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
