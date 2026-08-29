# Architecture

## Architectural style

AuthNexus is a modular monolith. Identity operations frequently require consistent state across
account creation, credentials, authentication transactions, sessions, recovery, policy, audit,
and notification outbox records. Networked microservices are not part of V1.0.

## Runtime model

```text
Consuming application
        ↓
Experience Engine (Next.js)
        ↓
AuthNexus API and orchestrator (ASP.NET Core)
        ↓
Policy and risk evaluation
        ↓
Identity, authentication, sessions, recovery, audit, and notification core
        ↓
PostgreSQL durable state / Redis non-authoritative coordination / provider adapters
```

The backend is authoritative for authentication-method eligibility, assurance, registration,
linking, recovery, session issuance, and authorization-related decisions. The frontend renders
the backend result; it does not decide security policy.

## Repository structure

The standalone repository owns the complete product lifecycle:

```text
apps/web                         Next.js Experience Engine
apps/api                         ASP.NET Core API host
src/backend/AuthNexus.*          Contracts, domain, application, infrastructure assemblies
src/backend/Modules              Reserved modular-monolith product boundaries
tests                            Unit, integration, security, and end-to-end suites
docs                             Versioned product and engineering documentation
infra                            Local, deployment, and observability ownership boundary
```

The V0.1 Phase A structure is deliberately a foundation. Authentication providers, persistence,
Docker Compose, sessions, audit, and policy runtime behavior are introduced by later V0.1 phases.

## One-way monorepo mirror

`NanaAddoDBa/authnexus` is the only source of truth. `NanaAddoDBa/nana-monorepo/apps/authnexus`
is a downstream ordinary-files mirror that uses initial Git subtree semantics and later one-way
snapshot synchronization.

```text
authnexus/main
        ↓ exact source SHA after successful standalone CI
nana-monorepo sync/authnexus pull request
        ↓ protected monorepo validation and merge
nana-monorepo/master: apps/authnexus
```

The mirror excludes the standalone repository's `.github/` directory because repository-level
automation belongs to the repository where it runs. The monorepo owns only its root
`.github/workflows/sync-authnexus.yml` workflow and its mirror metadata file:
`apps/authnexus/.source-revision`.

The mirror never writes to AuthNexus, does not use submodules, and does not become an AuthNexus
build or release source. Cross-repository synchronization uses a dedicated secret-backed token or
GitHub App with the minimum target-repository permissions. It is not enabled until that credential
is deliberately configured.
