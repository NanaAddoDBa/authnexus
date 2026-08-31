# Security Tests

`AuthNexus.Persistence.Security.Tests` contains the 98 database-backed negative and protection
cases that close the Phase E security gate. They execute the complete check-constraint catalogue,
nullable lifecycle regressions, redirect cardinality and deferral, EF and direct-SQL audit
immutability, stored-recipient inspection, destination tampering, wrong and unknown keys,
cross-row ciphertext substitution, key rotation, and due-query boundaries.

Run the project from the repository root:

```powershell
dotnet test tests/security/AuthNexus.Persistence.Security.Tests `
  --configuration Release
```

The project links `tests/shared/PostgreSqlTestDatabase.cs` and uses the same guarded
`authnexus_test_<32 hex characters>` database lifecycle as the integration project. Read
`docs/local-development.md` before setting `AUTHNEXUS_TEST_POSTGRES_CONNECTION`. Never give the
fixture a production, staging, shared-development, or otherwise non-disposable PostgreSQL cluster.

This project accepts the persistence controls introduced in Phase E. Phase M remains not started;
later authentication entry points must add their own abuse, replay, enumeration, authorization,
logging, and end-to-end cases when those entry points exist.
