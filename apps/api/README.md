# AuthNexus API Host

`AuthNexus.Api.csproj` is an ASP.NET Core 10 process with references to the application, contracts,
and infrastructure assemblies. `Program.cs` currently only builds and runs the host.

```powershell
dotnet run --project apps/api/AuthNexus.Api.csproj --launch-profile http
```

The profile listens on `http://localhost:5220`. There is no health, registration, login, session,
provider, or administration route yet, and the host does not open PostgreSQL, Redis, or SMTP
connections. Those omissions remain part of the accepted Phase C boundary.
