# Contributing

Work from `NanaAddoDBa/authnexus`, not the copy under `nana-monorepo/apps/authnexus`. The mirror is
updated only after source CI accepts an exact commit.

## Before changing code

1. Check the current phase in `docs/releases/v0.1.md`.
2. Keep the change to one deliverable that can be reviewed and reverted on its own.
3. Read the applicable ADRs in `docs/decisions`.
4. Name the behavior that is deliberately left for the next task.

Do not add a future provider, entity, endpoint, or UI state simply because a later roadmap item
mentions it. When behavior changes, add the smallest test that would fail without the change and
update one implementation note with the commands actually run.

Never commit `.env`, real provider credentials, keys, passwords, OTPs, session/recovery secrets,
or personal test data. Values ending in `-local-*` in `.env.example` are disposable Compose
defaults, not production configuration.

## Current validation

```powershell
pnpm --dir apps/web typecheck
pnpm --dir apps/web lint
pnpm --dir apps/web build

dotnet build AuthNexus.sln --configuration Release
dotnet test AuthNexus.sln --configuration Release

docker compose config --quiet
docker compose up --detach --wait --wait-timeout 120
powershell -NoProfile -ExecutionPolicy Bypass -File infra/docker/verify-local-stack.ps1
docker compose down --volumes --remove-orphans
```

Integration, security, and browser suites stay empty until a corresponding executable boundary
exists. Placeholder passing tests are not useful evidence.

## Task record

For a completed task, record only information that changed the implementation:

- version, phase, and exact deliverable;
- files and behavior added or removed;
- security and data-lifecycle consequences;
- commands run and their observed results;
- known gaps and the next boundary.

Store that record under `docs/implementation-notes/`. Use an ADR only when the change alters an
architectural decision rather than restating the task.
