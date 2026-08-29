# Backend Modules

Each child directory is a .NET class-library project named `AuthNexus.Modules.<Module>`. The
assembly is the compile-time ownership boundary; adding a direct reference from one module to
another is not allowed. Cross-module workflows will be coordinated from `AuthNexus.Application`
instead of turning one module into an informal composition root.

The only source in each module today is `ModuleAssemblyMarker`. Those markers prove that all
required assemblies build and give architecture tests a stable type to identify. They are not
service locators, runtime plug-ins, domain models, or implemented product behavior.

## Ownership reserved for later phases

| Assembly | Future ownership; not implemented in Phase C |
| --- | --- |
| `AuthNexus.Modules.Applications` | Registered applications, redirect configuration, branding references, and application settings. |
| `AuthNexus.Modules.Identity` | Accounts, identifiers, credentials, external identities, linking, and account lifecycle. |
| `AuthNexus.Modules.Authentication` | Authentication transactions, challenges, method coordination, and evidence verification. |
| `AuthNexus.Modules.Registration` | Pending registration, schema-driven fields, terms acceptance, and completion. |
| `AuthNexus.Modules.Sessions` | Session issue, rotation, expiry, revocation, logout, and authentication evidence. |
| `AuthNexus.Modules.Recovery` | Password reset, factor recovery or replacement, recovery codes, and session consequences. |
| `AuthNexus.Modules.Policies` | Method eligibility and ordering, assurance, step-up, session rules, and policy versions. |
| `AuthNexus.Modules.Risk` | Deterministic security signals, throttling inputs, provider health, and explainable risk results. |
| `AuthNexus.Modules.Notifications` | Transactional email, SMS, WhatsApp, outbox delivery, retry, and delivery status. |
| `AuthNexus.Modules.Audit` | Security and administrative events, correlation, actor/target relationships, and redaction. |
| `AuthNexus.Modules.Administration` | Application, provider, policy, schema, branding, security-event, and rollout management. |

The table assigns ownership so later work has a destination. It does not mean any listed
capability exists yet.

## Enforced dependency graph

```text
AuthNexus.Api
├── AuthNexus.Application
├── AuthNexus.Contracts
└── AuthNexus.Infrastructure

AuthNexus.Infrastructure
├── AuthNexus.Application
├── AuthNexus.Contracts
└── AuthNexus.Domain

AuthNexus.Application
├── AuthNexus.Contracts
├── AuthNexus.Domain
└── AuthNexus.Modules.* (all eleven module assemblies)

AuthNexus.Modules.*  -> no project references
AuthNexus.Contracts  -> no project references
AuthNexus.Domain     -> no project references
```

`tests/architecture/AuthNexus.Architecture.Tests` compiles against every module marker and reads
the production project files. Its tests fail when a required module is missing, a marker namespace
does not match its assembly, a new production project is not declared, or any direct project
reference differs from this graph. Changes to the graph therefore require an explicit test update
in the same review.

Phase C intentionally contains no entities, repositories, database packages, provider adapters,
dependency-injection registration, or HTTP endpoints.
