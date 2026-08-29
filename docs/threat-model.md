# Threat Model

## Assets present at the Phase B boundary

There are no accounts or credentials yet. The assets that do exist are the repository, CI history,
local PostgreSQL/Redis/Mailpit data, development configuration, and the one-way mirror credentials
that are still unconfigured.

| Current threat | Current control | Known gap |
| --- | --- | --- |
| Local services exposed to the LAN | Every published Compose port binds to `127.0.0.1`. | Local malware and same-user processes can still connect. |
| Reusing development credentials | Values are named `authnexus-local-*` and documented as disposable. | Compose cannot prevent someone copying them elsewhere. |
| Accidental data loss | Named volumes survive ordinary `docker compose down`. | `down --volumes` is destructive; there is no backup. |
| Treating Redis as durable identity storage | Architecture and ADR 0004 reserve PostgreSQL as authoritative. | No application code enforces this boundary yet. |
| Source/mirror drift | `.source-revision` identifies the imported green source commit. | Automatic sync is disabled until dedicated credentials exist. |
| Secret committed to Git | `.env` and common key formats are ignored; examples contain local values only. | Ignore rules are not secret scanning. |
| Misreading design docs as shipped controls | Documents now state current code evidence and missing pieces. | Review discipline remains necessary. |

## Threats introduced by later authentication work

Credential stuffing, enumeration, OTP pumping, session theft, OAuth replay, unsafe account linking,
recovery abuse, and policy misconfiguration become active threats only when their entry points and
assets exist. The relevant implementation phase must extend this file with concrete source/sink
paths, prevention, detection, recovery, and tests. A roadmap table by itself is not a control.

## Phase B operating rule

The Compose stack is a developer dependency stack, not a deployment template. It must not be run on
a publicly reachable host with the checked-in defaults. Production network policy, TLS, secret
management, backups, restore exercises, and monitoring remain unimplemented.
