# Threat Model

## Status

This is the V0.1 Phase A threat-model baseline. It identifies the main security properties that
later implementation phases must protect. It does not claim that runtime controls already exist.

| Threat | Asset | Planned preventive control | Planned detection and recovery |
| --- | --- | --- | --- |
| Credential stuffing and brute force | Accounts and credentials | Distributed rate limits, generic failures, maintained password hashing | Audit events, metrics, protection state |
| Account enumeration | Identifier privacy | Generic public responses and comparable expensive paths | Enumeration-focused negative tests |
| OTP guessing, flooding, and SMS pumping | Challenges and delivery spend | Shared challenge lifecycle, limits, expiry, resend controls, risk policy | Delivery/audit metrics and alerts |
| Session fixation or theft | Authenticated sessions | Server-managed opaque cookies, rotation, revocation, expiry | Session audit and active-session management |
| OAuth callback injection or replay | External identities | State, nonce, PKCE, callback validation, atomic replay protection | Provider failure events and security tests |
| Unsafe account linking | Account ownership | Explicit linking and no silent email matching | Conflict handling, audit, security notification |
| Recovery abuse | Account control | Assurance-aware recovery, fresh authentication, restricted flows | Recovery audit, alerts, revocation |
| Policy misconfiguration | Access assurance | Versioning, dry runs, guardrails, privileged admin step-up | Before/after audit and rollback |
| Secret or log leakage | Credentials and user data | Secret manager, centralized redaction, secure examples | Redaction tests and incident response |
| Provider or dependency outage | Required assurance | Explicit provider health and failure behavior | Health, alerts, policy-controlled fallback |

## Phase A controls

Phase A establishes secret-ignore rules, placeholder-only configuration, repository security
reporting guidance, no exposed authentication endpoints, and documentation of later control
ownership. It must not be presented as a complete runtime security baseline.

This model is updated whenever new authentication methods, recovery paths, policies, provider
adapters, tenancy, or deployment architecture add attack paths.
