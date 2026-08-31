using AuthNexus.Application.Persistence;
using AuthNexus.Modules.Audit;
using Microsoft.EntityFrameworkCore;

namespace AuthNexus.Infrastructure.Persistence.Audit;

internal sealed class SecurityEventRepository : ISecurityEventRepository
{
    private readonly AuthNexusDbContext _context;

    public SecurityEventRepository(AuthNexusDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<SecurityEvent?> GetByIdAsync(
        SecurityEventId eventId,
        CancellationToken cancellationToken = default)
    {
        if (eventId.IsEmpty)
        {
            throw new ArgumentException("A security event ID is required.", nameof(eventId));
        }

        var record = await _context.Set<SecurityEventRecord>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.EventId == eventId.Value,
                cancellationToken)
            .ConfigureAwait(false);

        return record?.ToDomain();
    }

    public void Append(SecurityEvent securityEvent)
    {
        ArgumentNullException.ThrowIfNull(securityEvent);
        _context.Set<SecurityEventRecord>().Add(SecurityEventRecord.FromDomain(securityEvent));
    }
}
