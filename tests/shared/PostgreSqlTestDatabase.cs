using AuthNexus.Infrastructure.Persistence;
using AuthNexus.Infrastructure.Persistence.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace AuthNexus.Persistence.Tests.Support;

public sealed class PostgreSqlTestDatabase : IAsyncDisposable
{
    public const string ConnectionEnvironmentVariable =
        "AUTHNEXUS_TEST_POSTGRES_CONNECTION";

    public const string DatabaseNamePrefix = "authnexus_test_";

    private const string LocalAdministratorConnection =
        "Host=127.0.0.1;Port=5432;Database=postgres;Username=authnexus;" +
        "Password=authnexus-local-postgres;Include Error Detail=true";

    private bool _created;

    private PostgreSqlTestDatabase(string databaseName, string administratorConnectionString)
    {
        DatabaseName = databaseName;
        AdministratorConnectionString = administratorConnectionString;

        var databaseConnection = new NpgsqlConnectionStringBuilder(administratorConnectionString)
        {
            Database = databaseName,
            Pooling = false,
        };

        ConnectionString = databaseConnection.ConnectionString;
    }

    public string DatabaseName { get; }

    public string ConnectionString { get; }

    private string AdministratorConnectionString { get; }

    public static async Task<PostgreSqlTestDatabase> CreateMigratedAsync(
        CancellationToken cancellationToken = default)
    {
        var database = CreateUninitialized();

        try
        {
            await database.CreateAsync(cancellationToken);
            await database.MigrateAsync(cancellationToken);
            return database;
        }
        catch
        {
            await database.DisposeAsync();
            throw;
        }
    }

    public static async Task<PostgreSqlTestDatabase> CreateEmptyAsync(
        CancellationToken cancellationToken = default)
    {
        var database = CreateUninitialized();

        try
        {
            await database.CreateAsync(cancellationToken);
            return database;
        }
        catch
        {
            await database.DisposeAsync();
            throw;
        }
    }

    public ServiceProvider CreateServiceProvider(
        NotificationDestinationProtectionOptions? destinationProtection = null)
    {
        var services = new ServiceCollection();
        services.AddAuthNexusPersistence(
            ConnectionString,
            destinationProtection ?? TestDestinationProtectionOptions.CurrentOnly());

        return services.BuildServiceProvider(validateScopes: true);
    }

    public AuthNexusDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthNexusDbContext>()
            .UseNpgsql(
                ConnectionString,
                postgres => postgres.MigrationsHistoryTable(
                    AuthNexusDbContext.MigrationHistoryTable,
                    AuthNexusDbContext.MigrationHistorySchema))
            .Options;

        return new AuthNexusDbContext(options);
    }

    public async Task<NpgsqlConnection> OpenConnectionAsync(
        CancellationToken cancellationToken = default)
    {
        var connection = new NpgsqlConnection(ConnectionString);

        try
        {
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        await using var context = CreateDbContext();
        await context.Database.MigrateAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (!_created)
        {
            return;
        }

        EnsureSafeDatabaseName(DatabaseName);
        NpgsqlConnection.ClearAllPools();

        await using var connection = new NpgsqlConnection(AdministratorConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = $"DROP DATABASE IF EXISTS \"{DatabaseName}\" WITH (FORCE);";
        await command.ExecuteNonQueryAsync();
        _created = false;
    }

    private static PostgreSqlTestDatabase CreateUninitialized()
    {
        var administratorConnection =
            Environment.GetEnvironmentVariable(ConnectionEnvironmentVariable) ??
            LocalAdministratorConnection;
        var builder = new NpgsqlConnectionStringBuilder(administratorConnection)
        {
            Database = "postgres",
            Pooling = false,
        };
        var databaseName = DatabaseNamePrefix + Guid.NewGuid().ToString("N");

        return new PostgreSqlTestDatabase(databaseName, builder.ConnectionString);
    }

    private async Task CreateAsync(CancellationToken cancellationToken)
    {
        EnsureSafeDatabaseName(DatabaseName);

        await using var connection = new NpgsqlConnection(AdministratorConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE \"{DatabaseName}\";";
        await command.ExecuteNonQueryAsync(cancellationToken);
        _created = true;
    }

    private static void EnsureSafeDatabaseName(string databaseName)
    {
        if (!databaseName.StartsWith(DatabaseNamePrefix, StringComparison.Ordinal) ||
            databaseName.Length != DatabaseNamePrefix.Length + 32 ||
            databaseName[DatabaseNamePrefix.Length..].Any(character => !Uri.IsHexDigit(character)))
        {
            throw new InvalidOperationException(
                "The disposable PostgreSQL database name failed its safety check.");
        }
    }
}

public static class TestDestinationProtectionOptions
{
    public const string CurrentKeyId = "test-destination:v2";

    public const string PreviousKeyId = "test-destination:v1";

    public const string CurrentKey =
        "ICEiIyQlJicoKSorLC0uLzAxMjM0NTY3ODk6Ozw9Pj8=";

    public const string PreviousKey =
        "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8=";

    public static NotificationDestinationProtectionOptions CurrentOnly() =>
        new()
        {
            CurrentKeyId = CurrentKeyId,
            Keys = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [CurrentKeyId] = CurrentKey,
            },
        };

    public static NotificationDestinationProtectionOptions Rotating() =>
        new()
        {
            CurrentKeyId = CurrentKeyId,
            Keys = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [PreviousKeyId] = PreviousKey,
                [CurrentKeyId] = CurrentKey,
            },
        };

    public static NotificationDestinationProtectionOptions PreviousOnly() =>
        new()
        {
            CurrentKeyId = PreviousKeyId,
            Keys = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [PreviousKeyId] = PreviousKey,
            },
        };
}

public abstract class PostgreSqlTestFixture : IAsyncLifetime
{
    public PostgreSqlTestDatabase Database { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Database = await PostgreSqlTestDatabase.CreateMigratedAsync();
    }

    public async Task DisposeAsync()
    {
        if (Database is not null)
        {
            await Database.DisposeAsync();
        }
    }
}
