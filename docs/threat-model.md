# Threat Model

## Assets and controls at the current V0.1 boundary

There is still no login-identifier or credential model. Phase E now declares durable rows for the
six Phase D boundaries, so migration integrity, stale-write handling, audit immutability, recipient
protection, and transaction atomicity are current tested concerns even though the API is not
connected. Other assets include the repository, CI history, local PostgreSQL/Redis/Mailpit data,
development configuration, and the one-way mirror credentials that are still unconfigured.

| Current threat | Current control | Known gap |
| --- | --- | --- |
| Local services exposed to the LAN | Every published Compose port binds to `127.0.0.1`. | Local malware and same-user processes can still connect. |
| Reusing development credentials | Values are named `authnexus-local-*` and documented as disposable. | Compose cannot prevent someone copying them elsewhere. |
| Accidental data loss | Named volumes survive ordinary `docker compose down`. | `down --volumes` is destructive; there is no backup. |
| Treating Redis as durable identity storage | Phase E maps every durable root only through the PostgreSQL provider; there is no Redis application consumer. | Redis outage and fallback behavior remain untested. |
| Source/mirror drift | `.source-revision` identifies the imported green source commit. | Automatic sync is disabled until dedicated credentials exist. |
| Secret committed to Git | `.env` and common key formats are ignored; examples contain local values only. | Ignore rules are not secret scanning. |
| Misreading design docs as shipped controls | Documents now state current code evidence and missing pieces. | Review discipline remains necessary. |
| Stale mutable-record write | GUID concurrency versions and `PersistenceConflictException` are exercised with two independent writers for accounts, authentication transactions, sessions, and outbox messages; each test preserves the first writer and clears the failed context. | Runtime handlers still need an explicit reload/retry policy. |
| Audit-event alteration | The context rejects tracked update/delete operations, and PostgreSQL rejects direct update, delete, and truncate operations in passing security cases. | Database-role design, retention, correction events, and authorized query/export paths are absent. |
| Notification-recipient disclosure | The stored row has no plaintext destination column; tests cover authenticated encryption, tampering, wrong and unknown keys, cross-row ciphertext substitution, and old-key reads during rotation. | There is no managed key source, operational rotation procedure, or production key-access audit. |
| Partial multi-record commit | Successful multi-repository commits and a real unique-index failure prove transaction commit, rollback, tracker clearing, and same-context reuse. | No authentication orchestration path yet stages account, audit, session, and outbox effects together. |
| Nullable lifecycle check bypass | Authentication, session, and outbox constraints require state-specific nullable timestamps with explicit `IS NOT NULL` predicates; null-bypass regressions pass. | New nullable lifecycle columns require the same review because PostgreSQL accepts a check result of `UNKNOWN`. |
| Redirect cardinality bypass | The child foreign key and the minimum-one redirect triggers are deferrable; tests cover both insert orders, replacement, move, delete, cascade, and truncate. | A later profile-update workflow must continue to use a transaction and surface constraint failures safely. |
| EF model and migration drift | Clean apply/downgrade/reapply tests pass, migration history is inspected, and EF reports no pending model changes. | Production rollout, backup, restore, and forward-only incident procedures are not defined. |
| Test cleanup damages durable data | Test databases must match `authnexus_test_<32 hex characters>` before forced deletion, and test connections disable pooling. The accepted run left none behind and did not modify `authnexus` or its volume. | A privileged test connection must still be restricted to a disposable local or CI PostgreSQL cluster. |

## Threats introduced by later authentication work

Credential stuffing, enumeration, OTP pumping, session theft, OAuth replay, unsafe account linking,
recovery abuse, and policy misconfiguration become active threats only when their entry points and
assets exist. The relevant implementation phase must extend this file with concrete source/sink
paths, prevention, detection, recovery, and tests. A roadmap table by itself is not a control.

## Phase B operating rule

The Compose stack is a developer dependency stack, not a deployment template. It must not be run on
a publicly reachable host with the checked-in defaults. Production network policy, TLS, secret
management, backups, restore exercises, and monitoring remain unimplemented.
