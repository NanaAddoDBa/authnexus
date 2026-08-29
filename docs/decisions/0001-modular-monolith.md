# ADR 0001: Use a Modular Monolith

**Status:** Accepted

## Context

Identity operations require consistent state across accounts, credentials, authentication
transactions, sessions, recovery, policy, audit, and notifications.

## Decision

AuthNexus will be a modular monolith for V1.0. Modules remain explicit in the codebase, but the
platform does not introduce networked microservices without a later evidenced need.

## Consequences

The platform can use durable transactions and test module boundaries locally. Future extraction is
possible only when a concrete operational need justifies it.
