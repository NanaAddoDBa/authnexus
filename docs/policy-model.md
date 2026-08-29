# Policy Boundary

No policy evaluator or policy table exists yet. The current web page therefore must not infer which
authentication methods are allowed, whether registration is open, or whether step-up is required.

The future evaluator belongs in the backend application layer and will receive resolved context
such as application, tenant, requested action, account, existing evidence, provider health, and
risk signals. Its result must be data the web can render: ordered methods, required assurance,
maximum authentication age, registration/recovery permission, session rules, and reason codes.

Two constraints already apply:

1. An unavailable provider cannot silently lower the required assurance.
2. A browser request cannot override server-resolved application or policy configuration.

Policy persistence, versioning, dry runs, impact previews, and administrative editing belong to
V0.9. Adding an interface before V0.1 has application and transaction context would create a
contract based on guesses, so Phase B does not add one.
