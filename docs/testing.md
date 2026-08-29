# Testing Contract

## Checks that run now

| Boundary | Command | What it proves |
| --- | --- | --- |
| Web types | `pnpm --dir apps/web typecheck` | TypeScript compiles without generated route-type assumptions. |
| Web lint | `pnpm --dir apps/web lint` | Current source passes the configured Next.js ESLint rules. |
| Web build | `pnpm --dir apps/web build` | Next.js can produce the current static routes. |
| Backend build | `dotnet build AuthNexus.sln --configuration Release` | All 20 solution projects compile with warnings treated as errors. |
| Contract test | `dotnet test tests/unit/AuthNexus.Contracts.Tests --configuration Release` | The canonical product-name contract holds. |
| ApplicationProfile tests | `dotnet test tests/unit/AuthNexus.Modules.Applications.Tests --configuration Release` | 34 cases cover profile construction, strong identifiers, enum guards, locale normalization, redirect safety, canonical matching, collection copying, and duplicate rejection. |
| UserAccount tests | `dotnet test tests/unit/AuthNexus.Modules.Identity.Tests --configuration Release` | 46 cases cover account creation, all seven legal transitions, all 35 forbidden state/action pairs, UTC chronology, terminal deletion, and rejection without mutation. |
| Architecture tests | `dotnet test tests/architecture/AuthNexus.Architecture.Tests --configuration Release` | Every required module has a compiled marker and direct production project references match the approved graph. |
| Compose model | `docker compose config --quiet` | Interpolation and Compose structure are valid. |
| Local dependencies | `infra/docker/verify-local-stack.ps1` | Containers are healthy; PostgreSQL accepts a query, Redis accepts an authenticated command, and Mailpit is ready. |

The source workflow runs frontend, backend, and local-runtime jobs separately. Its backend job
builds the solution and runs all four test projects. The downstream mirror request waits for all
three jobs.

## Empty test roots

`tests/integration`, `tests/security`, and `tests/e2e` contain scope notes rather than executable
tests. There is no behavior to exercise there yet. Testcontainers should arrive with database and
Redis integration code; API-host tests with endpoints; browser tests with the first real user
journey. Adding placeholder behavior tests would inflate the count without increasing confidence.

The architecture suite is not a placeholder. It protects a concrete production graph and fails
when a module disappears, a new backend project is undeclared, or a direct reference crosses a
boundary. Any intentional graph change must update the expected graph in the same review.

On the current Windows development machine, Application Control can block a copied module DLL in
the architecture test output even though the same solution build succeeds. The control is not
disabled. The hosted Linux backend job is therefore the acceptance result for that suite; the 34
ApplicationProfile and 46 UserAccount tests run locally without that exception.

Each authentication capability must later cover its success path, ordinary rejection, expiry,
replay, enumeration behavior, concurrency, audit output, session effect, and log redaction. Those
cases belong beside the implementation that makes them meaningful.
