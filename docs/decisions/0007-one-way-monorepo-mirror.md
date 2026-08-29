# ADR 0007: Mirror AuthNexus One Way into nana-monorepo

**Status:** Accepted

## Decision

`NanaAddoDBa/authnexus` is the authoritative source repository. Its accepted `main` revisions are
mirrored one way as ordinary files into `NanaAddoDBa/nana-monorepo/apps/authnexus`. The initial
import uses Git subtree semantics; later updates use exact-green-SHA snapshots and
`.source-revision` traceability.

## Consequences

There is no bidirectional synchronization, submodule, nested Git repository, reverse workflow, or
AuthNexus release process owned by the monorepo. Standalone `.github` configuration is excluded
from the mirror; monorepo synchronization automation remains monorepo-owned.
