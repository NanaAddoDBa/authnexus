# Authentication Flow Boundary

There is no authentication flow in the repository yet. `apps/api/Program.cs` builds and runs an
empty ASP.NET Core host; `apps/web/src/app/page.tsx` renders project status. No endpoint accepts an
identifier, password, OTP, provider callback, or session cookie.

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

Detailed success and failure paths will be added alongside executable code and tests. Until then,
this file defines ownership, not an API contract.
