# Domain Model

## Current phase

V0.1 Phase A contains assembly boundaries only. It creates no database schema, migrations, or
runtime domain entities. The product identity contract is the only stable code-level contract at
this stage.

## Planned durable concepts

The V0.1-to-V1.0 model evolves through explicit migrations around these concepts:

- `ApplicationProfile`
- `UserAccount`
- `LoginIdentifier`
- `PasswordCredential`
- `ExternalIdentity`
- `PasskeyCredential`
- `TotpCredential`
- `RecoveryCode`
- `AuthenticationTransaction`
- `AuthenticationChallenge`
- `Session`
- `ConsentRecord`
- `RoleAssignment`
- `AuthenticationPolicyVersion`
- `SecurityEvent`
- `NotificationOutbox`
- `RegistrationSchema`
- `ProviderConfiguration`

The initial V0.1 durable model is limited to application profiles, accounts, authentication
transactions, sessions, security events, and outbox state. Credentials and provider-specific
models are added only in their assigned capability releases.

## Invariants

Every interactive registration, authentication, linking, recovery, and step-up workflow will use
one central `AuthenticationTransaction`. Sessions will be server-managed opaque secrets whose
hashes are stored durably. Sensitive state transitions will generate audit events. No plaintext
passwords, OTPs, provider secrets, reset secrets, session secrets, or recovery codes belong in
durable logs or transaction records.
