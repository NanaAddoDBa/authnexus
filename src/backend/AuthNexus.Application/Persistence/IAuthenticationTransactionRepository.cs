using AuthNexus.Domain.Authentication;
using AuthNexus.Modules.Authentication;

namespace AuthNexus.Application.Persistence;

public interface IAuthenticationTransactionRepository
{
    Task<Persisted<AuthenticationTransaction>?> GetByIdAsync(
        AuthenticationTransactionId transactionId,
        CancellationToken cancellationToken = default);

    void Add(AuthenticationTransaction transaction);

    void Update(Persisted<AuthenticationTransaction> persisted);
}
