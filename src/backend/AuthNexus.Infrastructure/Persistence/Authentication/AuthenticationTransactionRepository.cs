using AuthNexus.Application.Persistence;
using AuthNexus.Domain;
using AuthNexus.Domain.Authentication;
using AuthNexus.Domain.Identity;
using AuthNexus.Domain.Tenancy;
using AuthNexus.Modules.Authentication;
using Microsoft.EntityFrameworkCore;
using ApplicationId = AuthNexus.Domain.Applications.ApplicationId;

namespace AuthNexus.Infrastructure.Persistence.Authentication;

internal sealed class AuthenticationTransactionRepository(AuthNexusDbContext dbContext)
    : IAuthenticationTransactionRepository
{
    public async Task<Persisted<AuthenticationTransaction>?> GetByIdAsync(
        AuthenticationTransactionId transactionId,
        CancellationToken cancellationToken = default)
    {
        if (transactionId.IsEmpty)
        {
            throw new ArgumentException(
                "An authentication transaction ID is required.",
                nameof(transactionId));
        }

        var record = await dbContext.Set<AuthenticationTransactionRecord>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.TransactionId == transactionId.Value,
                cancellationToken);

        return record is null
            ? null
            : new Persisted<AuthenticationTransaction>(
                ToDomain(record),
                new PersistenceVersion(record.Version));
    }

    public void Add(AuthenticationTransaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        dbContext.Set<AuthenticationTransactionRecord>()
            .Add(ToRecord(transaction, Guid.NewGuid()));
    }

    public void Update(Persisted<AuthenticationTransaction> persisted)
    {
        ArgumentNullException.ThrowIfNull(persisted);

        var record = ToRecord(persisted.Entity, Guid.NewGuid());
        var entry = dbContext.Attach(record);

        entry.Property(candidate => candidate.Version).OriginalValue =
            persisted.Version.Value;
        entry.State = EntityState.Modified;
    }

    private static AuthenticationTransactionRecord ToRecord(
        AuthenticationTransaction transaction,
        Guid version) =>
        new()
        {
            TransactionId = transaction.TransactionId.Value,
            ApplicationId = transaction.ApplicationId.Value,
            TenantId = transaction.TenantId?.Value,
            UserId = transaction.UserId?.Value,
            Purpose = checked((short)transaction.Purpose),
            CorrelationId = transaction.CorrelationId.Value,
            State = checked((short)transaction.State),
            CreatedAt = transaction.CreatedAt,
            ExpiresAt = transaction.ExpiresAt,
            StateChangedAt = transaction.StateChangedAt,
            CompletedAt = transaction.CompletedAt,
            FailedAt = transaction.FailedAt,
            Version = version,
        };

    private static AuthenticationTransaction ToDomain(
        AuthenticationTransactionRecord record) =>
        AuthenticationTransaction.Rehydrate(
            new AuthenticationTransactionId(record.TransactionId),
            new ApplicationId(record.ApplicationId),
            record.TenantId is null ? null : new TenantId(record.TenantId.Value),
            record.UserId is null ? null : new UserId(record.UserId.Value),
            (AuthenticationTransactionPurpose)record.Purpose,
            new CorrelationId(record.CorrelationId),
            (AuthenticationTransactionState)record.State,
            record.CreatedAt,
            record.ExpiresAt,
            record.StateChangedAt,
            record.CompletedAt,
            record.FailedAt);
}
