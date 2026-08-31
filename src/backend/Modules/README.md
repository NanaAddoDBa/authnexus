# Backend Modules

Each child directory is a .NET class-library project named `AuthNexus.Modules.<Module>`. The
assembly is the compile-time ownership boundary. Modules cannot reference one another;
cross-module workflows will be coordinated from `AuthNexus.Application`.

All eleven assemblies retain a `ModuleAssemblyMarker` for catalog verification. Applications,
Identity, Authentication, Sessions, Audit, and Notifications now contain Phase D domain code.
Each references the dependency-free `AuthNexus.Domain` assembly for identifiers that later modules
must share. Those narrow inward dependencies do not allow any module to reach another product
module, infrastructure, or the API.

Phase E does not place EF Core inside those modules. `AuthNexus.Application` owns repository ports;
`AuthNexus.Infrastructure` owns the row types, mappings, repositories, transaction boundary, and
migration.

## Current module map

| Assembly | Owned boundary | Code present now |
| --- | --- | --- |
| `AuthNexus.Modules.Applications` | Registered applications, redirect configuration, branding references, and application settings. | `ApplicationProfile`, type/audience/mode enums, and safe web `RedirectUri`. Infrastructure supplies add/get persistence for profiles and redirect children; the module remains EF-free. No runtime resolver. |
| `AuthNexus.Modules.Identity` | Accounts, identifiers, credentials, external identities, linking, and account lifecycle. | `UserAccount`, six explicit states, seven legal transitions, and transition-specific rejection. Infrastructure supplies add/get/update persistence with optimistic versioning. No login identifiers, credentials, or runtime resolver. |
| `AuthNexus.Modules.Authentication` | Authentication transactions, challenges, method coordination, and evidence verification. | `AuthenticationTransaction`, 14 purposes, eight states, seven named operations, lifetime enforcement, and transition-specific rejection. Infrastructure supplies add/get/update persistence with lifecycle constraints and optimistic versioning. No challenge/evidence verifier or orchestrator. |
| `AuthNexus.Modules.Registration` | Pending registration, schema-driven fields, terms acceptance, and completion. | Marker only. |
| `AuthNexus.Modules.Sessions` | Session issue, rotation, expiry, revocation, logout, and authentication evidence. | `Session`, a redacted stored-verifier value, three states, ten revocation reasons, activity/rotation/revocation/expiry rules, and half-open lifetime checks. Infrastructure supplies add/get/update persistence, a unique verifier index, and optimistic versioning. No cookie, secret generator, middleware, or runtime lookup. |
| `AuthNexus.Modules.Recovery` | Password reset, factor recovery or replacement, recovery codes, and session consequences. | Marker only. |
| `AuthNexus.Modules.Policies` | Method eligibility and ordering, assurance, step-up, session rules, and policy versions. | Marker only. |
| `AuthNexus.Modules.Risk` | Deterministic security signals, throttling inputs, provider health, and explainable risk results. | Marker only. |
| `AuthNexus.Modules.Notifications` | Transactional email, SMS, WhatsApp, outbox delivery, retry, and delivery status. | `NotificationOutboxMessage`, protected payload bytes, explicitly revealed/redacted destination, four states, three channels, and due/retry/terminal-result rules. Infrastructure supplies add/get/due/update persistence and AES-GCM destination protection. No claim lease, worker, provider, or retry policy. |
| `AuthNexus.Modules.Audit` | Security and administrative events, correlation, actor/target relationships, and redaction. | Immutable `SecurityEvent`, 37 fixed machine event codes, six results, optional actor/target/application/tenant/session context, and bounded defensive metadata. Infrastructure supplies append/get persistence and append-only guards. No trusted emitter, query service, retention policy, or endpoint. |
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
├── AuthNexus.Domain
├── AuthNexus.Modules.Applications
├── AuthNexus.Modules.Audit
├── AuthNexus.Modules.Authentication
├── AuthNexus.Modules.Identity
├── AuthNexus.Modules.Notifications
└── AuthNexus.Modules.Sessions

AuthNexus.Application
├── AuthNexus.Contracts
├── AuthNexus.Domain
└── AuthNexus.Modules.* (all eleven module assemblies)

AuthNexus.Modules.Applications -> AuthNexus.Domain
AuthNexus.Modules.Authentication -> AuthNexus.Domain
AuthNexus.Modules.Identity     -> AuthNexus.Domain
AuthNexus.Modules.Sessions     -> AuthNexus.Domain
AuthNexus.Modules.Audit        -> AuthNexus.Domain
AuthNexus.Modules.Notifications -> AuthNexus.Domain
other five modules             -> no project references
AuthNexus.Contracts            -> no project references
AuthNexus.Domain               -> no project references
```

`tests/architecture/AuthNexus.Architecture.Tests` compiles against every module marker and reads
the production project files. It is configured to fail when a required module is missing, a marker
namespace does not match its assembly, a new production project is undeclared, or a direct project
reference differs from this graph. The expected graph was updated for Phase E, and both architecture
cases passed during Phase E acceptance.

Phase E adds repository contracts in Application and EF Core/Npgsql adapters in Infrastructure.
Product modules remain free of database packages. The change does not add runtime lookup,
authentication orchestration, provider adapters, workers, or HTTP endpoints.
