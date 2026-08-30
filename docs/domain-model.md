# Domain Model Ledger

## D.1 ApplicationProfile

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

## D.2 UserAccount

`UserAccount` belongs to `AuthNexus.Modules.Identity`. Its shared `UserId` value lives in
`AuthNexus.Domain.Identity` so later transaction, session, audit, and notification models can refer
to the same account without referencing the Identity module.

The D.2 root contains only:

- `UserId`;
- `State`;
- `CreatedAt`;
- `StateChangedAt`.

An account is always created in `PendingVerification`; callers cannot select an initial state or
set `State` directly. Both timestamps are required, normalized to UTC, and state changes cannot be
recorded before the preceding state change.

### Legal state transitions

| Operation | Required state | Resulting state |
| --- | --- | --- |
| `Activate` | `PendingVerification` | `Active` |
| `ProtectTemporarily` | `Active` | `TemporarilyProtected` |
| `RestoreAfterProtection` | `TemporarilyProtected` | `Active` |
| `Suspend` | `Active` | `Suspended` |
| `Reactivate` | `Suspended` | `Active` |
| `RequestDeletion` | `Active` | `DeletionPending` |
| `CompleteDeletion` | `DeletionPending` | `Deleted` |

Every other state/operation pair throws `InvalidUserAccountStateTransitionException` without
changing either `State` or `StateChangedAt`. `Deleted` is terminal; there is no direct
`Active -> Deleted` path, deletion cancellation, or caller-supplied arbitrary transition. The
temporary-protection restoration and administrative reactivation paths return to `Active` through
separate methods so their intent cannot be confused.

The entity deliberately carries no email, phone, username, credential, display profile,
application ID, tenant ID, role, or consent. D.2 establishes the platform identity root, not a
login method or application membership.

The plans require every security-sensitive account transition to emit an audit event. D.2 does not
claim that behavior: no runtime workflow or persistence path calls these methods yet. The later
SecurityEvent and persistence work must make audit emission atomic with the persisted transition
before an endpoint or administrative operation can change account state.

## D.3 AuthenticationTransaction

`AuthenticationTransaction` belongs to `AuthNexus.Modules.Authentication`. Its strong
`AuthenticationTransactionId` and the shared `CorrelationId` live in `AuthNexus.Domain`, alongside
the existing application, tenant, and user identifiers consumed by this boundary. Authentication
therefore references Domain without referencing Applications or Identity.

The D.3 aggregate stores:

- `TransactionId`;
- `ApplicationId` and optional `TenantId`;
- optional `UserId` for a workflow whose user is already known at creation;
- one of the 14 explicit `Purpose` values;
- `CorrelationId`;
- `State`;
- `CreatedAt`, `ExpiresAt`, and `StateChangedAt`;
- optional `CompletedAt` and `FailedAt` terminal timestamps.

Creation always starts in `Initiated`. IDs must be non-default, a supplied tenant or user ID cannot
be its default value, the purpose must be defined, and creation/expiry times are normalized to UTC.
`ExpiresAt` must be later than `CreatedAt`.

### Legal D.3 transitions

| Operation | Required state | Resulting state |
| --- | --- | --- |
| `IssueChallenge` | `Initiated` | `ChallengeIssued` |
| `MarkPrimaryVerified` | `Initiated` or `ChallengeIssued` | `PrimaryVerified` |
| `RequireStepUp` | `PrimaryVerified` | `StepUpRequired` |
| `Complete` | `PrimaryVerified` or `StepUpRequired` | `Completed` |
| `Fail` | Any live state | `Failed` |
| `Cancel` | Any live state | `Cancelled` |
| `Expire` | Any live state, at or after the deadline | `Expired` |

The four live states are `Initiated`, `ChallengeIssued`, `PrimaryVerified`, and `StepUpRequired`.
The other four are terminal. Across eight states and seven operations, 18 pairs are legal and all
38 other pairs are rejected without changing `State`, `StateChangedAt`, `CompletedAt`, or
`FailedAt`.

The usable interval is `CreatedAt <= occurredAt < ExpiresAt`. A normal transition at or after the
deadline throws `AuthenticationTransactionExpiredException`; explicit expiry is rejected before
the deadline and accepted at or after it. Completion sets only `CompletedAt`, failure sets only
`FailedAt`, and cancellation/expiry set neither. A completed or otherwise terminal transaction is
not later converted to `Expired` merely because its original deadline passes.

The direct `Initiated -> PrimaryVerified` path supports methods that do not need a separately
persisted challenge. The entity does not verify evidence: a future Phase F orchestrator must prove
primary or step-up evidence before invoking the corresponding lifecycle method.

The full plan also names requested return destination, selected method, identifier hash, required
and achieved assurance, and risk result. Those remain deferred until their shared validation,
method, policy, evidence, and risk contracts exist; D.3 does not freeze them as weak strings.
Optional `UserId` is creation context only—there is no lookup, binding, or rebinding behavior.

## D.4 Session

`Session` belongs to `AuthNexus.Modules.Sessions`. Its `SessionId` lives in the shared Domain
assembly because the audit boundary needs to reference a session without depending on the
Sessions module. The entity stores only an already-derived `SessionSecretHash`; it never accepts a
raw browser secret.

The D.4 record contains session, user, application, and optional tenant identity; authentication
and creation timestamps; last-seen, idle-expiry, and absolute-expiry timestamps; verifier-rotation
time and count; current state; global/state-change timestamps; and mutually exclusive revocation
or expiry terminal data. Optional tenant context is an explicit AuthNexus extension to the plan's
abbreviated session list so later authorization cannot lose the scope already resolved by the
application profile and transaction.

Creation enforces `AuthenticatedAt <= CreatedAt < IdleExpiresAt <= AbsoluteExpiresAt` and starts in
`Active`. The usable interval is `CreatedAt <= observedAt < min(IdleExpiresAt,
AbsoluteExpiresAt)`. `CanBeUsedAt` applies that rule without waiting for or performing an `Expired`
state mutation.

| Operation | Required state/time | Result |
| --- | --- | --- |
| `RecordActivity` | Active and before effective expiry | Advances last-seen time and a non-shortening idle deadline capped by absolute expiry. |
| `RotateSecretHash` | Active and before effective expiry | Replaces a distinct stored verifier and increments the rotation count in place. |
| `Revoke` | Active | `Revoked`, preserving the first timestamp and machine reason. |
| `Revoke` | Revoked | Idempotent no-op. |
| `Expire` | Active and at/after effective expiry | `Expired`. |

Revocation may record a reason even after an active record's deadlines have elapsed because it can
only remove access; activity and rotation still fail at the deadline. `Revoked` is never later
overwritten by `Expired`. All operation times are UTC-normalized and globally nondecreasing, and
rejection cannot partially alter lifecycle state.

The full plan's assurance, authentication-method, MFA, reauthentication, device, user-agent, and
network fields remain deferred until their evidence, policy, and privacy contracts exist. Secret
generation/hash derivation, cookies, middleware, lookup, logout orchestration, persistence, and
endpoints remain Phase E/G work rather than D.4 behavior.

## V0.1 vocabulary

The V0.1 domain ledger is:

| Concept | Reason it belongs in V0.1 | Implemented |
| --- | --- | --- |
| `ApplicationProfile` | Resolve the calling application's redirect and policy context. | D.1 foundation |
| `UserAccount` | Internal identity root independent of login method. | D.2 foundation |
| `AuthenticationTransaction` | One server-owned state machine for an interactive attempt. | D.3 foundation |
| `Session` | Durable record behind an opaque browser cookie. | D.4 foundation |
| `SecurityEvent` | Append-only security-relevant activity. | No |
| `NotificationOutbox` | Commit notification work with the state change that produced it. | No |

Password credentials arrive in V0.2; OTP challenges and delivery records in V0.3; external
identities in V0.4; passkeys in V0.5; TOTP and recovery codes in V0.6. Those models should not be
pre-created in V0.1 without the behavior and tests that define them.

There are no repositories, EF Core mappings, migrations, seeds, administrative commands, or HTTP
representations. Those omissions are deliberate: D.1 through D.4 define valid in-memory state
only.

## Constraints carried into implementation

- One interactive attempt maps to one `AuthenticationTransaction`.
- Authentication methods produce evidence; they do not create sessions or link accounts directly.
- Session cookies contain random opaque secrets; durable storage receives only a verifier/hash.
- Plaintext passwords, OTPs, session secrets, provider secrets, and recovery codes never belong in
  transaction or audit records.
- Security notifications are written through an outbox in the same PostgreSQL transaction as the
  state change.
