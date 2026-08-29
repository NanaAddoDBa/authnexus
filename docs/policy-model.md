# Policy Model

## Purpose

AuthNexus uses one backend policy evaluator to determine method eligibility, ordering, assurance,
MFA, step-up, authentication age, recovery, registration permission, and session policy from
application, tenant, role, action, evidence, risk, account, and provider-health context.

## Planned contract

```csharp
Task<AuthenticationRequirement> EvaluateAsync(
    AuthenticationContext context,
    CancellationToken cancellationToken);
```

The result will contain only backend-authoritative decisions such as allowed and ordered methods,
required assurance, step-up requirements, maximum authentication age, registration/recovery
permission, session policy, and machine-readable reason codes.

## Current phase

The evaluator and runtime policy data model are not implemented in V0.1 Phase A. This document
records the future boundary so the frontend scaffold does not assume it owns policy logic.

Policy versioning, dry runs, impact previews, rollback, and the administrative editor belong to
V0.9. Provider outages must never automatically lower the required assurance.
