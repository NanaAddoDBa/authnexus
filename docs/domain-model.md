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

The plans require every security-sensitive account transition to emit an audit event. No runtime
workflow invokes these transitions. Phase E can stage and persist an account update through the
shared unit of work, but no handler currently couples that update to a `SecurityEvent`. That atomic
workflow remains mandatory before an endpoint or administrative operation can change account
state.

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
network fields remain deferred until their evidence, policy, and privacy contracts exist. Phase E
maps and rehydrates the current record and applies an optimistic version check to updates. Secret
generation/hash derivation, constant-time verification, cookies, middleware, request lookup,
logout orchestration, and endpoints remain Phase G work.

## D.5 SecurityEvent

`SecurityEvent` belongs to `AuthNexus.Modules.Audit` and is immutable after construction. It stores
its Audit-owned event ID, UTC timestamp, one of 37 fixed machine event types, one of six declared
results, optional actor/target/application/tenant/session context, required correlation identity,
optional bounded network and user-agent summaries, and immutable bounded metadata.

Tenant context is an explicit AuthNexus extension to the plan's abbreviated event list. It carries
the scope already established by the application profile, transaction, and session; it neither
resolves nor authorizes a tenant.

The type codes reproduce Plan 1's catalogue from `registration_requested` through
`provider_unavailable`. The plan names a `Result` field but no vocabulary, so D.5 explicitly adopts
`Succeeded`, `Failed`, `Denied`, `Throttled`, `Cancelled`, and `Informational`. It deliberately
defines no type/result compatibility matrix before the producers exist.

Metadata is copied, read-only, limited to 32 canonical keys and 512 characters per value, and
rejects control/Unicode separator/format characters, duplicates, and separator-aware sensitive
keys. This is not the Phase I redaction pipeline: safe-looking keys can still carry unsafe values,
so future trusted builders and serialized-output tests remain mandatory.

“Append-only” still begins with the event's lack of a mutation surface. Phase E adds an append/get
repository, rejects tracked update/delete operations, and declares database update/delete/truncate
rejection triggers in the migration. No workflow emits an event, and query services, retention,
authorization, exports, transactional coupling, and database-level acceptance tests remain
deferred.

## D.6 NotificationOutboxMessage

`NotificationOutboxMessage` belongs to `AuthNexus.Modules.Notifications`. It records notification
work accepted for later delivery, not a provider request already sent. The record carries its own
message identity, required correlation identity, optional target user/application/tenant context,
a machine notification type, one of three channels, a redacted destination value, an
already-protected payload, and UTC creation/availability/lifecycle times.

Creation starts in `Pending` with zero attempts and `NextAttemptAt = AvailableAt`. An attempt is
due at or after that timestamp. `RecordDelivered`, `ScheduleRetry`, and `FailPermanently` are each
legal from `Pending` and `RetryScheduled`: six accepted state/action pairs. The same actions are
forbidden from `Delivered` and `PermanentlyFailed`: six rejected pairs. A retry must be scheduled
strictly after its failed attempt; accepted outcomes increment the attempt count exactly once and
keep terminal timestamps mutually exclusive.

The clear destination has no public `Value` getter. A future provider adapter must call the
explicit `RevealForDelivery()` method; display and ordinary JSON serialization remain redacted.
The payload makes defensive byte copies and exposes only explicit copy-for-delivery access.

Phase E encrypts the revealed destination with AES-GCM before writing it, and stores the key ID and
envelope format version beside the ciphertext. Authenticated associated data binds the envelope to
its outbox message ID, so moving a valid ciphertext to another row fails authentication. The
payload remains caller-protected and is stored with its own key ID and format version. The
repository supports due-message lookup and optimistic updates. Key provisioning, managed key
storage, a rotation procedure, claim/lease behavior, retry policy, templates, providers, receipts,
and operational replay remain outside Phase E.

## V0.1 vocabulary

The V0.1 domain ledger is:

| Concept | Reason it belongs in V0.1 | Domain foundation | Phase E persistence |
| --- | --- | --- | --- |
| `ApplicationProfile` | Resolve the calling application's redirect and policy context. | D.1 complete | add/get mapping plus redirect child table |
| `UserAccount` | Internal identity root independent of login method. | D.2 complete | add/get/update with version token |
| `AuthenticationTransaction` | One server-owned state machine for an interactive attempt. | D.3 complete | add/get/update with version token |
| `Session` | Durable record behind an opaque browser cookie. | D.4 complete | add/get/update with version token |
| `SecurityEvent` | Append-only security-relevant activity. | D.5 complete | append/get mapping and append-only guards |
| `NotificationOutboxMessage` | Record notification work that must later commit with its originating state change. | D.6 complete | add/get/due/update, version token, protected destination |

Password credentials arrive in V0.2; OTP challenges and delivery records in V0.3; external
identities in V0.4; passkeys in V0.5; TOTP and recovery codes in V0.6. Those models should not be
pre-created in V0.1 without the behavior and tests that define them.

Phase E supplies repository contracts, EF Core mappings, and an initial migration. There are still
no seeds, administrative commands, HTTP representations, or API persistence composition. Twenty
integration cases and 98 persistence-security cases now accept the migration, all six repository
round trips, four stale-writer paths, transaction rollback, every declared check constraint,
append-only audit enforcement, and notification-destination protection against disposable
PostgreSQL databases.

## Constraints carried into implementation

- One interactive attempt maps to one `AuthenticationTransaction`.
- Authentication methods produce evidence; they do not create sessions or link accounts directly.
- Session cookies contain random opaque secrets; durable storage receives only a verifier/hash.
- Plaintext passwords, OTPs, session secrets, provider secrets, and recovery codes never belong in
  transaction or audit records.
- The shared unit of work is designed to commit a domain change and outbox insert in the same
  PostgreSQL transaction. Database tests prove multi-record commit, rollback after a real unique
  index failure, tracker clearing, and same-context reuse. No orchestrator stages the eventual
  domain-change/outbox pair yet.
