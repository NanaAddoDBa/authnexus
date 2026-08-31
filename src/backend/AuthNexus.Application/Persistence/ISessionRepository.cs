using AuthNexus.Domain.Sessions;
using AuthNexus.Modules.Sessions;

namespace AuthNexus.Application.Persistence;

public interface ISessionRepository
{
    Task<Persisted<Session>?> GetByIdAsync(
        SessionId sessionId,
        CancellationToken cancellationToken = default);

    void Add(Session session);

    void Update(Persisted<Session> persisted);
}
