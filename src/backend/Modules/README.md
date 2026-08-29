# Backend Modules

Each child directory is a .NET class-library project named `AuthNexus.Modules.<Module>`. The
assembly is the compile-time ownership boundary. Modules cannot reference one another;
cross-module workflows will be coordinated from `AuthNexus.Application`.

All eleven assemblies retain a `ModuleAssemblyMarker` for catalog verification. Applications is
now the first module with domain code. It references the dependency-free `AuthNexus.Domain`
assembly for shared application and tenant identifiers. That narrow inward dependency does not
allow Applications to reach another product module, infrastructure, or the API.

## Current module map

| Assembly | Owned boundary | Code present now |
| --- | --- | --- |
| `AuthNexus.Modules.Applications` | Registered applications, redirect configuration, branding references, and application settings. | `ApplicationProfile`, type/audience/mode enums, and safe web `RedirectUri`. No resolver or storage. |
| `AuthNexus.Modules.Identity` | Accounts, identifiers, credentials, external identities, linking, and account lifecycle. | Marker only. |
| `AuthNexus.Modules.Authentication` | Authentication transactions, challenges, method coordination, and evidence verification. | Marker only. |
| `AuthNexus.Modules.Registration` | Pending registration, schema-driven fields, terms acceptance, and completion. | Marker only. |
| `AuthNexus.Modules.Sessions` | Session issue, rotation, expiry, revocation, logout, and authentication evidence. | Marker only. |
| `AuthNexus.Modules.Recovery` | Password reset, factor recovery or replacement, recovery codes, and session consequences. | Marker only. |
| `AuthNexus.Modules.Policies` | Method eligibility and ordering, assurance, step-up, session rules, and policy versions. | Marker only. |
| `AuthNexus.Modules.Risk` | Deterministic security signals, throttling inputs, provider health, and explainable risk results. | Marker only. |
| `AuthNexus.Modules.Notifications` | Transactional email, SMS, WhatsApp, outbox delivery, retry, and delivery status. | Marker only. |
| `AuthNexus.Modules.Audit` | Security and administrative events, correlation, actor/target relationships, and redaction. | Marker only. |
| `AuthNexus.Modules.Administration` | Application, provider, policy, schema, branding, security-event, and rollout management. | Marker only. |

The ownership column reserves a destination for later work. Only the code named in the final
column exists.

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

AuthNexus.Modules.Applications -> AuthNexus.Domain
other ten modules              -> no project references
AuthNexus.Contracts            -> no project references
AuthNexus.Domain               -> no project references
```

`tests/architecture/AuthNexus.Architecture.Tests` compiles against every module marker and reads
the production project files. Its tests fail when a required module is missing, a marker namespace
does not match its assembly, a new production project is not declared, or any direct project
reference differs from this graph. Changes to the graph therefore require an explicit test update
in the same review.

D.1 adds one in-memory entity boundary. It adds no repositories, database packages, provider
adapters, dependency-injection registration, profile lookup, or HTTP endpoints.
