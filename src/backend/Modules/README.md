# Backend Modules

Each child directory is a .NET class-library project named `AuthNexus.Modules.<Module>`. The
assembly is the compile-time ownership boundary. Modules cannot reference one another;
cross-module workflows will be coordinated from `AuthNexus.Application`.

All eleven assemblies retain a `ModuleAssemblyMarker` for catalog verification. Applications,
Identity, and Authentication now contain domain code. Each references the dependency-free
`AuthNexus.Domain` assembly for identifiers that later modules must share. Those narrow inward
dependencies do not allow any module to reach another product module, infrastructure, or the API.

## Current module map

| Assembly | Owned boundary | Code present now |
| --- | --- | --- |
| `AuthNexus.Modules.Applications` | Registered applications, redirect configuration, branding references, and application settings. | `ApplicationProfile`, type/audience/mode enums, and safe web `RedirectUri`. No resolver or storage. |
| `AuthNexus.Modules.Identity` | Accounts, identifiers, credentials, external identities, linking, and account lifecycle. | `UserAccount`, six explicit states, seven legal transitions, and transition-specific rejection. No login identifiers, credentials, resolver, or storage. |
| `AuthNexus.Modules.Authentication` | Authentication transactions, challenges, method coordination, and evidence verification. | `AuthenticationTransaction`, 14 purposes, eight states, seven named operations, lifetime enforcement, and transition-specific rejection. No challenge/evidence verification, orchestrator, or storage. |
| `AuthNexus.Modules.Registration` | Pending registration, schema-driven fields, terms acceptance, and completion. | Marker only. |
| `AuthNexus.Modules.Sessions` | Session issue, rotation, expiry, revocation, logout, and authentication evidence. | `Session`, a redacted stored-verifier value, three states, ten revocation reasons, activity/rotation/revocation/expiry rules, and half-open lifetime checks. No cookie, secret generator, middleware, lookup, or storage. |
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
AuthNexus.Modules.Authentication -> AuthNexus.Domain
AuthNexus.Modules.Identity     -> AuthNexus.Domain
AuthNexus.Modules.Sessions     -> AuthNexus.Domain
other seven modules            -> no project references
AuthNexus.Contracts            -> no project references
AuthNexus.Domain               -> no project references
```

`tests/architecture/AuthNexus.Architecture.Tests` compiles against every module marker and reads
the production project files. Its tests fail when a required module is missing, a marker namespace
does not match its assembly, a new production project is not declared, or any direct project
reference differs from this graph. Changes to the graph therefore require an explicit test update
in the same review.

D.1 through D.4 add four in-memory entity boundaries. They add no repositories, database
packages, provider adapters, dependency-injection registration, runtime lookup/orchestration, or
HTTP endpoints.
