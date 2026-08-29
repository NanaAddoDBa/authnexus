# Domain Model Ledger

## Code that exists

D.1 introduces one entity boundary and three supporting value types:

- `ApplicationProfile` belongs to `AuthNexus.Modules.Applications`.
- `ApplicationId` and `TenantId` live in `AuthNexus.Domain` because transactions, sessions, audit
  records, and other later modules will need the same typed identifiers.
- `RedirectUri` belongs to Applications because that module owns the registered destination
  allowlist.

`ApplicationProfile.Create` captures the minimum context needed before a future authentication
request can be resolved: application and optional tenant identity, type, audience, mode, display
name, default locale, authentication-policy reference, optional registration-schema reference,
and allowed web redirects.

## Enforced D.1 invariants

| Input | Construction rule |
| --- | --- |
| `ApplicationId` | A default or empty GUID is rejected. |
| `TenantId?` | May be absent; an explicitly supplied default or empty GUID is rejected. |
| Type, audience, mode | The value must be one of the declared enum members. Numeric fall-through values are rejected. |
| Application name | Required, trimmed, and never stored as whitespace. |
| Default locale | Required, recognized by .NET globalization data, and stored under its canonical culture name, such as `en-US`. |
| Authentication-policy reference | Required and trimmed. D.1 stores a reference; it does not resolve or evaluate a policy. |
| Registration-schema reference | Optional; when supplied it must contain non-whitespace text. |
| Redirect collection | Required, copied on construction, non-empty, null-free, and duplicate-free after URI canonicalization. |
| Each redirect | Absolute HTTPS with a concrete host, or HTTP only for a loopback host. Wildcard hosts, user information, fragments, backslashes, relative paths, and other schemes are rejected. |

`AllowsRedirectTo` performs equality against the canonical registered `RedirectUri`. This is a
domain allowlist query, not an authentication-request validator: there is no endpoint, application
lookup, tenant lookup, or return-navigation handler yet. Native custom schemes and verified app or
universal-link configuration are also deferred.

## V0.1 vocabulary

The V0.1 domain ledger is:

| Concept | Reason it belongs in V0.1 | Implemented |
| --- | --- | --- |
| `ApplicationProfile` | Resolve the calling application's redirect and policy context. | D.1 foundation |
| `UserAccount` | Internal identity root independent of login method. | No |
| `AuthenticationTransaction` | One server-owned state machine for an interactive attempt. | No |
| `Session` | Durable record behind an opaque browser cookie. | No |
| `SecurityEvent` | Append-only security-relevant activity. | No |
| `NotificationOutbox` | Commit notification work with the state change that produced it. | No |

Password credentials arrive in V0.2; OTP challenges and delivery records in V0.3; external
identities in V0.4; passkeys in V0.5; TOTP and recovery codes in V0.6. Those models should not be
pre-created in V0.1 without the behavior and tests that define them.

There are no repositories, EF Core mappings, migrations, profile seeds, administrative commands,
or HTTP representations. Those omissions are deliberate: D.1 defines valid in-memory state only.

## Constraints carried into implementation

- One interactive attempt maps to one `AuthenticationTransaction`.
- Authentication methods produce evidence; they do not create sessions or link accounts directly.
- Session cookies contain random opaque secrets; durable storage receives only a verifier/hash.
- Plaintext passwords, OTPs, session secrets, provider secrets, and recovery codes never belong in
  transaction or audit records.
- Security notifications are written through an outbox in the same PostgreSQL transaction as the
  state change.
