namespace AuthNexus.Infrastructure.Persistence.Authentication;

internal sealed class AuthenticationTransactionRecord : IConcurrencyTrackedRecord
{
    public Guid TransactionId { get; set; }

    public Guid ApplicationId { get; set; }

    public Guid? TenantId { get; set; }

    public Guid? UserId { get; set; }

    public short Purpose { get; set; }

    public Guid CorrelationId { get; set; }

    public short State { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset StateChangedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public DateTimeOffset? FailedAt { get; set; }

    public Guid Version { get; set; }
}
