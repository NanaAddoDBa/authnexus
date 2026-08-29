# Contributing to AuthNexus

AuthNexus is developed as a cumulative, versioned identity platform. The current accepted version
and phase determine what may be implemented next.

## Development rules

- Work on one atomic task at a time.
- Do not implement future-version capability behavior early.
- Preserve accepted module boundaries, APIs, migrations, and security behavior.
- Add tests and documentation with behavior changes.
- Never commit secrets, `.env` files, provider credentials, private keys, tokens, passwords, OTPs,
  recovery codes, or session secrets.
- Document architectural changes through an ADR before or with implementation.
- Treat the standalone `NanaAddoDBa/authnexus` repository as the source of truth. Do not develop
  AuthNexus from its downstream `nana-monorepo/apps/authnexus` mirror.

## Atomic task format

Every implementation task records:

```text
Version:
Phase:
Capability:
Task:

Goal:
Dependencies:
Files affected:
Domain changes:
Database changes:
Backend changes:
API changes:
UI changes:
Accessibility considerations:
Security considerations:
Audit / observability:
Tests:
Documentation:
Acceptance criteria:
Commit scope:
```

After the task, add or update a concise note under `docs/implementation-notes/` explaining the
change, its security implications, test evidence, known limitations, and the remaining work in
the current phase.

## Validation

Run the checks appropriate to the changed boundary. The current Phase A baseline is:

```powershell
pnpm --dir apps/web typecheck
pnpm --dir apps/web lint
pnpm --dir apps/web build

dotnet build AuthNexus.sln --configuration Release
dotnet test AuthNexus.sln --configuration Release
```

Integration, security, and end-to-end test suites are added when the corresponding runtime
behavior exists.
