namespace AuthNexus.Infrastructure.Persistence.Sessions;

internal sealed class SessionRecord : IConcurrencyTrackedRecord
{
    public Guid SessionId { get; set; }

    public string SecretHash { get; set; } = string.Empty;

    public Guid UserId { get; set; }

    public Guid ApplicationId { get; set; }

    public Guid? TenantId { get; set; }

    public short State { get; set; }

    public DateTimeOffset AuthenticatedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset LastSeenAt { get; set; }

    public DateTimeOffset IdleExpiresAt { get; set; }

    public DateTimeOffset AbsoluteExpiresAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public DateTimeOffset StateChangedAt { get; set; }

    public DateTimeOffset SecretRotatedAt { get; set; }

    public int RotationCount { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }

    public short? RevocationReason { get; set; }

    public DateTimeOffset? ExpiredAt { get; set; }

    public Guid Version { get; set; }
}
