# AuthNexus Web Host

`apps/web` is a Next.js 16 App Router project. The only route implemented is `/`, which states the
current foundation status. It does not call the API or render an authentication method.

```powershell
pnpm --dir apps/web install --frozen-lockfile
pnpm --dir apps/web dev
```

Development runs at `http://localhost:3000`. The authoritative checks are `typecheck`, `lint`, and
`build`; all three run in source CI.
