# Authentication Flows

## Current phase

No authentication workflow is executable in V0.1 Phase A. The web page is a repository-status
screen and the API intentionally exposes no authentication endpoint.

## Shared future orchestration

Every interactive workflow will use this backend-authoritative sequence:

```text
Resolve application context
        ↓
Create AuthenticationTransaction
        ↓
Evaluate initial policy
        ↓
Render allowed methods
        ↓
Begin and complete selected method
        ↓
Produce AuthenticationEvidence
        ↓
Resolve identity, registration, or linking
        ↓
Evaluate assurance and step-up requirement
        ↓
Create or upgrade a server-managed session
        ↓
Audit and return to an allowlisted destination
```

Methods return authentication evidence. They do not independently create sessions, decide policy,
authorize actions, merge accounts, or grant registration permission.

Detailed password, OTP, provider, passkey, TOTP, recovery, linking, and step-up workflows are
implemented only in their assigned cumulative releases.
