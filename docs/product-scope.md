# Product Scope

## Purpose

AuthNexus is a reusable Universal Authentication Platform intended to support multiple consumer,
SaaS, workforce, administrator, community, and regulated applications through configuration
rather than duplicated authentication implementations.

## Product boundary

AuthNexus owns universal identity and authentication behavior, the reusable Experience Engine,
application profiles, policy evaluation, sessions, recovery, audit, notifications, and provider
adapters. A consuming application supplies its registration, branding, redirect, policy, and
provider configuration.

AuthNexus is not a product-specific login page, a provider-button demonstration, a custom OAuth
authorization server, a browser-token product, an authorization engine driven by frontend state,
or an LLM-driven security decision system.

## Current implementation boundary

V0.1 Phase A establishes the standalone repository and project skeleton. No runtime identity,
credential, session, database, provider, or policy behavior is complete at this phase.

## V1.0 product target

The first production-capable baseline must cumulatively include local email/password identity,
email/SMS/WhatsApp OTP, Google/Apple/Telegram federation, passkeys, TOTP, recovery codes, safe
linking, sessions, application and assurance policies, observability, operations, testing,
deployment support, and security documentation.
