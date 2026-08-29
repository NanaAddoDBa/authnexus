# Testing Contract

## Checks that run now

| Boundary | Command | What it proves |
| --- | --- | --- |
| Web types | `pnpm --dir apps/web typecheck` | TypeScript compiles without generated route-type assumptions. |
| Web lint | `pnpm --dir apps/web lint` | Current source passes the configured Next.js ESLint rules. |
| Web build | `pnpm --dir apps/web build` | Next.js can produce the current static routes. |
| Backend build | `dotnet build AuthNexus.sln --configuration Release` | All project references compile with warnings treated as errors. |
| Unit test | `dotnet test AuthNexus.sln --configuration Release` | The canonical product-name contract holds. |
| Compose model | `docker compose config --quiet` | Interpolation and Compose structure are valid. |
| Local dependencies | `infra/docker/verify-local-stack.ps1` | Containers are healthy; PostgreSQL accepts a query, Redis accepts an authenticated command, and Mailpit is ready. |

The source workflow runs frontend, backend, and local-runtime jobs separately. The downstream
mirror request waits for all three.

## Empty test roots

`tests/integration`, `tests/security`, and `tests/e2e` contain scope notes rather than executable
tests. There is no behavior to exercise there yet. Testcontainers should arrive with database and
Redis integration code; API-host tests with endpoints; browser tests with the first real user
journey. Adding placeholder passing tests would inflate the count without increasing confidence.

Each authentication capability must later cover its success path, ordinary rejection, expiry,
replay, enumeration behavior, concurrency, audit output, session effect, and log redaction. Those
cases belong beside the implementation that makes them meaningful.
