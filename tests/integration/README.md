# Integration Tests

`AuthNexus.Persistence.Integration.Tests` contains the 20 database-backed cases that close the
Phase E repository and transaction gate. They cover migration history, apply/downgrade/reapply,
all six persisted roots, four optimistic-concurrency conflicts, shared commit, explicit
transaction commit, rollback with context reuse, and notification due-query behavior.

Run the project from the repository root:

```powershell
dotnet test tests/integration/AuthNexus.Persistence.Integration.Tests `
  --configuration Release
```

The project links `tests/shared/PostgreSqlTestDatabase.cs`. It creates only guarded random
`authnexus_test_<32 hex characters>` databases and drops them after use. See
`docs/local-development.md` before overriding `AUTHNEXUS_TEST_POSTGRES_CONNECTION`; the configured
role can create and forcibly drop databases and must be limited to a disposable local or CI
cluster.

These persistence tests belong to Phase E. They do not start the later Phase M whole-product
integration and end-to-end gate.
