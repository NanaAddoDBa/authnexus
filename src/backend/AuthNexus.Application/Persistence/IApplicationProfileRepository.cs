using AuthNexus.Modules.Applications;
using DomainApplicationId = AuthNexus.Domain.Applications.ApplicationId;

namespace AuthNexus.Application.Persistence;

public interface IApplicationProfileRepository
{
    Task<Persisted<ApplicationProfile>?> GetByIdAsync(
        DomainApplicationId applicationId,
        CancellationToken cancellationToken = default);

    void Add(ApplicationProfile entity);
}
