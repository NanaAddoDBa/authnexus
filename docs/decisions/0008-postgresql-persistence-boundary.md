# ADR 0008: Keep PostgreSQL Persistence in Infrastructure

**Status:** Accepted

## Context

The six Phase D roots need durable storage without making product modules depend on EF Core or
turning persistence records into domain objects. Authentication workflows will also need one
transaction that can include a state change, a security event, and an outbox message.

## Decision

`AuthNexus.Application` owns repository and unit-of-work ports. `AuthNexus.Infrastructure` owns
Npgsql, EF Core, private row records, relational mappings, repository adapters, destination
protection, and migrations. Product modules remain EF-free.

One `AuthNexusDbContext` spans the six persisted roots. Tables are separated into `applications`,
`identity`, `authentication`, `sessions`, `audit`, and `notifications` schemas; EF migration
history uses the `infrastructure` schema. The only relational ownership foreign key in this model
is from application redirect rows to their application profile. Identifiers shared across module
records are not cross-module foreign keys.

Repositories stage changes and never call `SaveChanges` independently. A caller commits through
`IAuthNexusUnitOfWork`, optionally inside its explicit PostgreSQL transaction boundary. Mutable
`UserAccount`, `AuthenticationTransaction`, `Session`, and notification-outbox rows carry caller-
held GUID versions configured as optimistic concurrency tokens. `SecurityEvent` is insert-only in
the repository, guarded against tracked mutation in the context, and protected from SQL update or
delete by the initial migration.

Notification destinations are encrypted before persistence with an AES-256-GCM envelope. Each
row records its key ID and envelope version, and authenticated associated data binds the envelope
to its outbox message ID. Key material is supplied by runtime composition and does not belong in
the repository. Notification payloads remain caller-protected and retain their separate key ID
and format version.

Migration execution is an explicit operator action. The API does not call `Database.Migrate`,
`EnsureCreated`, or `AddAuthNexusPersistence` during Phase E.

## Consequences

Infrastructure directly references the six persisted module assemblies so it can translate
between private row records and domain objects. Those references point inward; no product module
references Infrastructure or a database package.

The initial migration declares the complete Phase E relational model, including deferred redirect
ownership and append-only audit triggers. Phase E acceptance applied, downgraded, and reapplied the
migration in disposable PostgreSQL databases; round-tripped all six roots; exercised every
declared check constraint and all four optimistic-conflict paths; rejected audit mutation and
destination-envelope tampering; and proved transaction commit and rollback. This verifies the
persistence boundary itself. It does not make the boundary runtime behavior of the API, which
still does not register or invoke it.
