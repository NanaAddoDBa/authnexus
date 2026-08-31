using AuthNexus.Infrastructure.Persistence;
using AuthNexus.Persistence.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AuthNexus.Persistence.Integration.Tests;

public sealed class MigrationTests
{
    private const string InitialMigration = "20260830190919_PhaseEInitialPersistence";

    private static readonly string[] ProductTables =
    [
        "applications.application_profiles",
        "applications.application_redirect_uris",
        "identity.user_accounts",
        "authentication.authentication_transactions",
        "sessions.sessions",
        "audit.security_events",
        "notifications.outbox_messages",
    ];

    [Fact]
    public async Task Initial_migration_applies_to_an_empty_database_and_records_its_history()
    {
        await using var database = await PostgreSqlTestDatabase.CreateEmptyAsync();
        await using var context = database.CreateDbContext();

        Assert.Empty(await context.Database.GetAppliedMigrationsAsync());
        Assert.Equal([InitialMigration], await context.Database.GetPendingMigrationsAsync());

        await context.Database.MigrateAsync();

        Assert.Equal([InitialMigration], await context.Database.GetAppliedMigrationsAsync());
        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
        Assert.False(context.Database.HasPendingModelChanges());

        await using var connection = await database.OpenConnectionAsync();
        await using var history = connection.CreateCommand();
        history.CommandText =
            "SELECT \"MigrationId\" " +
            "FROM infrastructure.__ef_migrations_history " +
            "ORDER BY \"MigrationId\";";

        var recordedMigrations = new List<string>();
        await using (var reader = await history.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                recordedMigrations.Add(reader.GetString(0));
            }
        }

        Assert.Equal([InitialMigration], recordedMigrations);

        foreach (var productTable in ProductTables)
        {
            Assert.True(
                await RelationExistsAsync(connection, productTable),
                $"Expected {productTable} to exist after applying {InitialMigration}.");
        }
    }

    [Fact]
    public async Task Initial_migration_can_downgrade_to_zero_and_reapply()
    {
        await using var database = await PostgreSqlTestDatabase.CreateEmptyAsync();
        await database.MigrateAsync();

        await using (var context = database.CreateDbContext())
        {
            var migrator = context.GetService<IMigrator>();
            await migrator.MigrateAsync(Migration.InitialDatabase);
        }

        await using (var connection = await database.OpenConnectionAsync())
        {
            foreach (var productTable in ProductTables)
            {
                Assert.False(
                    await RelationExistsAsync(connection, productTable),
                    $"Expected {productTable} to be removed by the downgrade.");
            }
        }

        await database.MigrateAsync();

        await using var verificationContext = database.CreateDbContext();
        Assert.Equal(
            [InitialMigration],
            await verificationContext.Database.GetAppliedMigrationsAsync());
        Assert.Empty(await verificationContext.Database.GetPendingMigrationsAsync());
    }

    private static async Task<bool> RelationExistsAsync(
        Npgsql.NpgsqlConnection connection,
        string qualifiedRelationName)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT to_regclass(@relation_name) IS NOT NULL;";
        command.Parameters.AddWithValue("relation_name", qualifiedRelationName);

        return (bool)(await command.ExecuteScalarAsync())!;
    }
}
