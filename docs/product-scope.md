# Product Boundary

AuthNexus exists so consuming applications do not each invent registration, credentials, external
provider handling, recovery, and session security. A consumer should eventually supply an
application profile—allowed redirects, branding, registration fields, enabled methods, and policy—
while AuthNexus owns the security-sensitive workflow.

## In this repository

- The reusable browser experience in `apps/web`.
- The public HTTP boundary in `apps/api`.
- Domain code in the Applications, Identity, Authentication, Sessions, Audit, and Notifications
  modules; the other five module assemblies currently retain markers only.
- Persistence ports in `AuthNexus.Application` and PostgreSQL mappings, repositories, migration
  tooling, and notification-destination protection in `AuthNexus.Infrastructure`.
- Local dependency and future deployment definitions under `infra` and `compose.yaml`.
- Product decisions and release evidence under `docs`.

## Outside this repository

- A consuming product's business authorization model and application pages.
- A custom OAuth authorization server or general-purpose identity-provider product.
- Frontend-owned method eligibility, session issuance, account linking, or assurance decisions.
- Real SMS, WhatsApp, email, or federation credentials in source control.
- Development in the downstream `nana-monorepo/apps/authnexus` mirror.

## Current evidence

Phase D supplies the six executable domain records and their in-memory invariants:
`ApplicationProfile`, `UserAccount`, `AuthenticationTransaction`, `Session`, `SecurityEvent`, and
`NotificationOutboxMessage`.

Phase E adds repository ports, Npgsql/EF Core mappings, relational constraints, optimistic version
tokens for mutable records, append-only audit guards, AES-GCM destination protection for outbox
recipients, a due-message query, and one shared transaction boundary. The migration declares seven
tables across six module schemas plus a separate migration-history schema. Phase E acceptance
applied, downgraded, and reapplied it only in guarded disposable databases; 20 integration and 98
security cases passed, and the durable local `authnexus` database and volume were left unchanged.

No running process registers or calls these adapters. There is still no login identifier,
credential, challenge verifier, session-cookie subsystem, authentication endpoint, provider
adapter, notification worker, or callback. Later-flow documents remain design inputs until their
corresponding runtime code and tests exist.
