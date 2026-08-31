using AuthNexus.Application.Persistence;
using AuthNexus.Domain.Identity;
using AuthNexus.Modules.Identity;
using Microsoft.EntityFrameworkCore;

namespace AuthNexus.Infrastructure.Persistence.Identity;

internal sealed class UserAccountRepository(AuthNexusDbContext dbContext)
    : IUserAccountRepository
{
    public async Task<Persisted<UserAccount>?> GetByIdAsync(
        UserId userId,
        CancellationToken cancellationToken = default)
    {
        if (userId.IsEmpty)
        {
            throw new ArgumentException("A user ID is required.", nameof(userId));
        }

        var record = await dbContext.Set<UserAccountRecord>()
            .AsNoTracking()
            .SingleOrDefaultAsync(account => account.UserId == userId.Value, cancellationToken);

        return record is null ? null : ToPersisted(record);
    }

    public void Add(UserAccount entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        dbContext.Set<UserAccountRecord>().Add(ToRecord(entity, Guid.NewGuid()));
    }

    public void Update(Persisted<UserAccount> persisted)
    {
        ArgumentNullException.ThrowIfNull(persisted);

        var record = ToRecord(persisted.Entity, Guid.NewGuid());
        var entry = dbContext.Attach(record);

        entry.Property(account => account.Version).OriginalValue = persisted.Version.Value;
        entry.State = EntityState.Modified;
    }

    private static UserAccountRecord ToRecord(UserAccount entity, Guid version) =>
        new(
            entity.UserId.Value,
            checked((short)entity.State),
            entity.CreatedAt,
            entity.StateChangedAt,
            version);

    private static Persisted<UserAccount> ToPersisted(UserAccountRecord record)
    {
        var entity = UserAccount.Restore(
            new UserId(record.UserId),
            (UserAccountState)record.State,
            record.CreatedAt,
            record.StateChangedAt);

        return new Persisted<UserAccount>(entity, new PersistenceVersion(record.Version));
    }
}
