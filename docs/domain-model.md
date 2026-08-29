# Domain Model Ledger

## Code that exists

The only stable domain-level value in code is `AuthNexusProduct` in
`src/backend/AuthNexus.Contracts/AuthNexusProduct.cs`. The `Domain`, `Application`, and
`Infrastructure` projects contain assembly markers. There are no entities, value objects,
repositories, EF Core mappings, or migrations.

## V0.1 vocabulary

The following names are reserved for later V0.1 work:

| Concept | Reason it belongs in V0.1 | Implemented |
| --- | --- | --- |
| `ApplicationProfile` | Resolve the calling application's redirect and policy context. | No |
| `UserAccount` | Internal identity root independent of login method. | No |
| `AuthenticationTransaction` | One server-owned state machine for an interactive attempt. | No |
| `Session` | Durable record behind an opaque browser cookie. | No |
| `SecurityEvent` | Append-only security-relevant activity. | No |
| `NotificationOutbox` | Commit notification work with the state change that produced it. | No |

Password credentials arrive in V0.2; OTP challenges and delivery records in V0.3; external
identities in V0.4; passkeys in V0.5; TOTP and recovery codes in V0.6. Those models should not be
pre-created in V0.1 without the behavior and tests that define them.

## Constraints carried into implementation

- One interactive attempt maps to one `AuthenticationTransaction`.
- Authentication methods produce evidence; they do not create sessions or link accounts directly.
- Session cookies contain random opaque secrets; durable storage receives only a verifier/hash.
- Plaintext passwords, OTPs, session secrets, provider secrets, and recovery codes never belong in
  transaction or audit records.
- Security notifications are written through an outbox in the same PostgreSQL transaction as the
  state change.
