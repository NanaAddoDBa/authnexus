using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AuthNexus.Infrastructure.Persistence;

public sealed class AuthNexusDbContextFactory : IDesignTimeDbContextFactory<AuthNexusDbContext>
{
    public const string ConnectionEnvironmentVariable = "AUTHNEXUS_POSTGRES_CONNECTION";

    public AuthNexusDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionEnvironmentVariable);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Set {ConnectionEnvironmentVariable} before using the EF Core design-time tools.");
        }

        var options = new DbContextOptionsBuilder<AuthNexusDbContext>()
            .UseNpgsql(
                connectionString,
                postgres => postgres.MigrationsHistoryTable(
                    AuthNexusDbContext.MigrationHistoryTable,
                    AuthNexusDbContext.MigrationHistorySchema))
            .Options;

        return new AuthNexusDbContext(options);
    }
}
