# ADR 0005: Centralize Authentication Transactions, Policy, OTP, and Providers

**Status:** Accepted

## Decision

Every registration, authentication, linking, recovery, and step-up workflow will use a central
`AuthenticationTransaction`. Methods produce structured evidence rather than creating sessions.
The backend policy evaluator determines eligibility and assurance. OTP generation/verification is
shared across delivery channels, and provider-specific integrations are adapters.

## Consequences

No provider controls an independent session system. The frontend renders policy output but cannot
make authorization or assurance decisions.
