# Local Stack Operations

The Compose file lives at the repository root so standard `docker compose` commands work without a
file argument. Keep image versions, port bindings, volumes, and health checks in `compose.yaml`;
keep the behavioral acceptance check in `verify-local-stack.ps1`.

The stack is for developer machines and CI. It deliberately uses readable local credentials and
loopback-only host ports. It is not a production deployment template.
