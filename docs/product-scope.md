# Product Boundary

AuthNexus exists so consuming applications do not each invent registration, credentials, external
provider handling, recovery, and session security. A consumer should eventually supply an
application profile—allowed redirects, branding, registration fields, enabled methods, and policy—
while AuthNexus owns the security-sensitive workflow.

## In this repository

- The reusable browser experience in `apps/web`.
- The public HTTP boundary in `apps/api`.
- Identity, authentication, session, recovery, policy, audit, notification, and provider code under
  `src/backend` as those modules are added.
- Local dependency and future deployment definitions under `infra` and `compose.yaml`.
- Product decisions and release evidence under `docs`.

## Outside this repository

- A consuming product's business authorization model and application pages.
- A custom OAuth authorization server or general-purpose identity-provider product.
- Frontend-owned method eligibility, session issuance, account linking, or assurance decisions.
- Real SMS, WhatsApp, email, or federation credentials in source control.
- Development in the downstream `nana-monorepo/apps/authnexus` mirror.

## Current evidence

Through D.4, the code can build the web and API processes, start PostgreSQL, Redis, and Mailpit
locally, construct an `ApplicationProfile` that rejects unsafe or incomplete configuration,
exercise the legal `UserAccount` state transitions, and construct an expiring
`AuthenticationTransaction` whose 18 legal and 38 forbidden state/action pairs are executable in
memory. It can also exercise a Session record's idle/absolute lifetime, stored-verifier rotation,
revocation, and expiry rules without accepting a raw cookie secret. No running process loads or
stores these models, creates a transaction, or issues or validates a session. There is still
no login identifier, credential, challenge verification, cookie, authentication endpoint, or
provider callback. Later-flow documents remain design inputs until corresponding code and tests
exist.
