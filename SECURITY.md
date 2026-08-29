# Security Policy

AuthNexus is security-sensitive identity and authentication software. Vulnerability reports must
not disclose exploit details, credentials, tokens, personal data, or proof-of-concept attack
material through public issues.

## Reporting a vulnerability

Use a private GitHub Security Advisory for this repository, or contact the repository owner by an
already verified private channel. Do not open a public issue for a suspected vulnerability.

Include enough information to reproduce and assess the problem safely:

- A concise description of the affected component and impact.
- A minimal, non-sensitive reproduction path.
- Relevant version, commit, configuration assumptions, and suggested mitigation if known.

Do not include real passwords, OTPs, reset tokens, session secrets, provider secrets, private
keys, recovery codes, or personal data.

## Supported versions

AuthNexus has no tagged or supported production release. `main` is active V0.1 development code;
the local Compose stack and status page must not be treated as a deployed authentication service.
Supported versions and response targets will be defined before the first production release.

## Security engineering expectations

Security-sensitive changes require explicit negative and abuse-case testing where applicable,
redacted logging, audit consideration, documentation of architecture changes, and no secrets in
source control.
