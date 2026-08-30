# Testing Contract

## Checks that run now

| Boundary | Command | What it proves |
| --- | --- | --- |
| Web types | `pnpm --dir apps/web typecheck` | TypeScript compiles without generated route-type assumptions. |
| Web lint | `pnpm --dir apps/web lint` | Current source passes the configured Next.js ESLint rules. |
| Web build | `pnpm --dir apps/web build` | Next.js can produce the current static routes. |
| Backend build | `dotnet build AuthNexus.sln --configuration Release` | All 24 solution projects compile with warnings treated as errors. |
| Contract test | `dotnet test tests/unit/AuthNexus.Contracts.Tests --configuration Release` | The canonical product-name contract holds. |
| ApplicationProfile tests | `dotnet test tests/unit/AuthNexus.Modules.Applications.Tests --configuration Release` | 34 cases cover profile construction, strong identifiers, enum guards, locale normalization, redirect safety, canonical matching, collection copying, and duplicate rejection. |
| UserAccount tests | `dotnet test tests/unit/AuthNexus.Modules.Identity.Tests --configuration Release` | 46 cases cover account creation, all seven legal transitions, all 35 forbidden state/action pairs, UTC chronology, terminal deletion, and rejection without mutation. |
| AuthenticationTransaction tests | `dotnet test tests/unit/AuthNexus.Modules.Authentication.Tests --configuration Release` | 116 cases protect the 14-purpose vocabulary, all 18 legal and 38 forbidden state/action pairs, application/tenant/user/correlation context, UTC chronology, exact and late expiry, terminal timestamps, replay rejection, and non-mutation. |
| Session tests | `dotnet test tests/unit/AuthNexus.Modules.Sessions.Tests --configuration Release` | 65 cases protect identity/context, canonical fixed hash representation including pad bits, three states, ten revocation reasons, lifetime construction, half-open usability, activity, rotation, revocation, idle/absolute expiry, all 12 state/action pairs, UTC chronology, and rejection snapshots. |
| SecurityEvent tests | `dotnet test tests/unit/AuthNexus.Modules.Audit.Tests --configuration Release` | 107 cases protect the 37 exact type codes, six results, immutable context, UTC time, bounded summaries and metadata, separator-aware secret-key guardrails, Unicode display safety, defensive copying, and non-leaking validation. |
| NotificationOutbox tests | `dotnet test tests/unit/AuthNexus.Modules.Notifications.Tests --configuration Release` | 82 cases protect message context, destination/payload disclosure boundaries, four states, three channels, all six legal and six forbidden state/action pairs, exact due times, retries, terminal outcomes, UTC chronology, and rejection snapshots. |
| Architecture tests | `dotnet test tests/architecture/AuthNexus.Architecture.Tests --configuration Release` | Every required module has a compiled marker and direct production project references match the approved graph. |
| Compose model | `docker compose config --quiet` | Interpolation and Compose structure are valid. |
| Local dependencies | `infra/docker/verify-local-stack.ps1` | Containers are healthy; PostgreSQL accepts a query, Redis accepts an authenticated command, and Mailpit is ready. |

The source workflow runs frontend, backend, and local-runtime jobs separately. Its backend job
builds the solution and runs all eight test projects. The downstream mirror request waits for all
three jobs.

## Empty test roots

`tests/integration`, `tests/security`, and `tests/e2e` contain scope notes rather than executable
tests. There is no behavior to exercise there yet. Testcontainers should arrive with database and
Redis integration code; API-host tests with endpoints; browser tests with the first real user
journey. Adding placeholder behavior tests would inflate the count without increasing confidence.

The architecture suite is not a placeholder. It protects a concrete production graph and fails
when a module disappears, a new backend project is undeclared, or a direct reference crosses a
boundary. Any intentional graph change must update the expected graph in the same review.

On the current Windows development machine, Application Control can block an already-built test or
copied module DLL even though the same solution build succeeds. The control is not disabled. The
hosted Linux backend job is therefore the whole-suite acceptance result when that occurs. During
D.3, all 116 transaction and both architecture cases passed in focused local runs. Subsequent
whole-suite attempts were blocked while loading first the unchanged Identity assembly and then
copied Authentication assemblies (`0x800711C7`).

During D.4, the initial 62 Session cases passed locally. A review then added two canonical
base64url pad-bit rejection cases and one absolute-deadline case. A later full Phase D run executed
the final 65-case Session matrix successfully. Earlier attempts were blocked while loading its
copied module assembly; the security control was not disabled.

During D.5, the complete 107-case Audit suite passed locally in Release. The Audit module's only
direct production dependency is Domain. Architecture execution remains subject to the same
Windows Application Control limitation and is accepted through hosted Linux CI.

During D.6, the complete 82-case Notifications suite passed locally in Release. The suite includes
an ordinary JSON-serialization check for destination disclosure, scalar-aware Unicode formatting
rejection, and malformed-surrogate coverage. The Notifications module's only direct production
dependency is Domain. Hosted Linux CI remains the decisive whole-solution and architecture gate.

The final local Phase D pass restored and compiled all 24 solution projects with zero warnings and
zero errors. Of the 453 declared backend cases, 417 executed and passed. Windows Application
Control blocked the 34 ApplicationProfile cases and both architecture cases before their
assertions loaded (`0x800711C7`). Hosted Linux must therefore execute and pass all 453 cases before
the source commit is accepted for mirroring.

Each authentication capability must later cover its success path, ordinary rejection, expiry,
replay, enumeration behavior, concurrency, audit output, session effect, and log redaction. Those
cases belong beside the implementation that makes them meaningful.
