using AuthNexus.Domain.Identity;
using AuthNexus.Modules.Identity;

namespace AuthNexus.Application.Persistence;

public interface IUserAccountRepository
{
    Task<Persisted<UserAccount>?> GetByIdAsync(
        UserId userId,
        CancellationToken cancellationToken = default);

    void Add(UserAccount entity);

    void Update(Persisted<UserAccount> persisted);
}
