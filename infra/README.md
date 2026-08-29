# Infrastructure in V0.1

The only executable infrastructure today is the local dependency stack in the repository-root
`compose.yaml`:

- PostgreSQL stores data in the Compose-managed `postgres-data` volume.
- Redis runs with authentication and append-only persistence in `redis-data`.
- Mailpit stores captured messages in `mailpit-data` and exposes SMTP plus its browser UI on the
  loopback interface.

`infra/docker/verify-local-stack.ps1` is the acceptance check used locally and in CI. The
`infra/database` and `infra/observability` directories are ownership markers only; they contain no
migrations, dashboards, collectors, or deployment configuration yet.

Production containers, secret management, cloud resources, backups, restore tests, TLS, and
monitoring are not part of the Phase B stack.
