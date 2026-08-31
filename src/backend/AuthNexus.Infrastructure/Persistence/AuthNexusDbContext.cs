using AuthNexus.Application.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AuthNexus.Infrastructure.Persistence;

public sealed class AuthNexusDbContext : DbContext, IAuthNexusUnitOfWork
{
    public const string MigrationHistorySchema = "infrastructure";
    public const string MigrationHistoryTable = "__ef_migrations_history";

    public AuthNexusDbContext(DbContextOptions<AuthNexusDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuthNexusDbContext).Assembly);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        PrepareTrackedRecords();

        try
        {
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            ChangeTracker.Clear();
            throw new PersistenceConflictException(exception);
        }
    }

    public override async Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        PrepareTrackedRecords();

        try
        {
            return await base
                .SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            ChangeTracker.Clear();
            throw new PersistenceConflictException(exception);
        }
    }

    public Task<int> CommitAsync(CancellationToken cancellationToken = default) =>
        SaveChangesAsync(cancellationToken);

    public async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        await using var transaction = await Database
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            await operation(cancellationToken).ConfigureAwait(false);
            await CommitAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            ChangeTracker.Clear();
            throw;
        }
    }

    private void PrepareTrackedRecords()
    {
        foreach (var entry in ChangeTracker.Entries<IAppendOnlyRecord>())
        {
            if (entry.State is EntityState.Modified or EntityState.Deleted)
            {
                throw new InvalidOperationException(
                    "Append-only records cannot be updated or deleted.");
            }
        }

        foreach (var entry in ChangeTracker.Entries<IConcurrencyTrackedRecord>())
        {
            if (entry.State == EntityState.Added && entry.Entity.Version == Guid.Empty)
            {
                entry.Entity.Version = Guid.NewGuid();
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.Version = Guid.NewGuid();
            }
        }
    }
}
