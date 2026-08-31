using AuthNexus.Application.Persistence;
using AuthNexus.Domain.Identity;
using AuthNexus.Domain.Sessions;
using AuthNexus.Domain.Tenancy;
using AuthNexus.Modules.Sessions;
using Microsoft.EntityFrameworkCore;
using ApplicationId = AuthNexus.Domain.Applications.ApplicationId;

namespace AuthNexus.Infrastructure.Persistence.Sessions;

internal sealed class SessionRepository(AuthNexusDbContext dbContext) : ISessionRepository
{
    public async Task<Persisted<Session>?> GetByIdAsync(
        SessionId sessionId,
        CancellationToken cancellationToken = default)
    {
        if (sessionId.IsEmpty)
        {
            throw new ArgumentException("A session ID is required.", nameof(sessionId));
        }

        var record = await dbContext.Set<SessionRecord>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.SessionId == sessionId.Value,
                cancellationToken);

        return record is null
            ? null
            : new Persisted<Session>(
                ToDomain(record),
                new PersistenceVersion(record.Version));
    }

    public void Add(Session session)
    {
        ArgumentNullException.ThrowIfNull(session);

        dbContext.Set<SessionRecord>()
            .Add(ToRecord(session, Guid.NewGuid()));
    }

    public void Update(Persisted<Session> persisted)
    {
        ArgumentNullException.ThrowIfNull(persisted);

        var record = ToRecord(persisted.Entity, Guid.NewGuid());
        var entry = dbContext.Attach(record);

        entry.Property(candidate => candidate.Version).OriginalValue =
            persisted.Version.Value;
        entry.State = EntityState.Modified;
    }

    private static SessionRecord ToRecord(Session session, Guid version) =>
        new()
        {
            SessionId = session.SessionId.Value,
            SecretHash = session.SecretHash.EncodedValue,
            UserId = session.UserId.Value,
            ApplicationId = session.ApplicationId.Value,
            TenantId = session.TenantId?.Value,
            State = checked((short)session.State),
            AuthenticatedAt = session.AuthenticatedAt,
            CreatedAt = session.CreatedAt,
            LastSeenAt = session.LastSeenAt,
            IdleExpiresAt = session.IdleExpiresAt,
            AbsoluteExpiresAt = session.AbsoluteExpiresAt,
            UpdatedAt = session.UpdatedAt,
            StateChangedAt = session.StateChangedAt,
            SecretRotatedAt = session.SecretRotatedAt,
            RotationCount = session.RotationCount,
            RevokedAt = session.RevokedAt,
            RevocationReason = session.RevocationReason is null
                ? null
                : checked((short)session.RevocationReason.Value),
            ExpiredAt = session.ExpiredAt,
            Version = version,
        };

    private static Session ToDomain(SessionRecord record) =>
        Session.Rehydrate(
            new SessionId(record.SessionId),
            new SessionSecretHash(record.SecretHash),
            new UserId(record.UserId),
            new ApplicationId(record.ApplicationId),
            record.TenantId is null ? null : new TenantId(record.TenantId.Value),
            (SessionState)record.State,
            record.AuthenticatedAt,
            record.CreatedAt,
            record.LastSeenAt,
            record.IdleExpiresAt,
            record.AbsoluteExpiresAt,
            record.UpdatedAt,
            record.StateChangedAt,
            record.SecretRotatedAt,
            record.RotationCount,
            record.RevokedAt,
            record.RevocationReason is null
                ? null
                : (SessionRevocationReason)record.RevocationReason.Value,
            record.ExpiredAt);
}
