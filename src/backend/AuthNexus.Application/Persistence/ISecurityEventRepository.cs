using AuthNexus.Modules.Audit;

namespace AuthNexus.Application.Persistence;

public interface ISecurityEventRepository
{
    Task<SecurityEvent?> GetByIdAsync(
        SecurityEventId eventId,
        CancellationToken cancellationToken = default);

    void Append(SecurityEvent securityEvent);
}
