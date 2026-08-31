using System.Text.Json;
using AuthNexus.Domain;
using AuthNexus.Domain.Applications;
using AuthNexus.Domain.Identity;
using AuthNexus.Domain.Sessions;
using AuthNexus.Domain.Tenancy;
using AuthNexus.Modules.Audit;

namespace AuthNexus.Infrastructure.Persistence.Audit;

internal sealed class SecurityEventRecord : IAppendOnlyRecord
{
    private static readonly IReadOnlyDictionary<string, SecurityEventType> EventTypesByCode =
        Enum.GetValues<SecurityEventType>()
            .ToDictionary(eventType => eventType.ToCode(), StringComparer.Ordinal);

    public Guid EventId { get; set; }

    public DateTimeOffset Timestamp { get; set; }

    public string EventType { get; set; } = string.Empty;

    public int Result { get; set; }

    public Guid? ActorUserId { get; set; }

    public Guid? TargetUserId { get; set; }

    public Guid? ApplicationId { get; set; }

    public Guid? TenantId { get; set; }

    public Guid? SessionId { get; set; }

    public Guid CorrelationId { get; set; }

    public string? NetworkSummary { get; set; }

    public string? UserAgentSummary { get; set; }

    public string Metadata { get; set; } = "{}";

    public static SecurityEventRecord FromDomain(SecurityEvent securityEvent)
    {
        ArgumentNullException.ThrowIfNull(securityEvent);

        var orderedMetadata = securityEvent.Metadata.Values
            .OrderBy(entry => entry.Key, StringComparer.Ordinal)
            .ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal);

        return new SecurityEventRecord
        {
            EventId = securityEvent.EventId.Value,
            Timestamp = securityEvent.Timestamp,
            EventType = securityEvent.EventTypeCode,
            Result = (int)securityEvent.Result,
            ActorUserId = securityEvent.ActorUserId?.Value,
            TargetUserId = securityEvent.TargetUserId?.Value,
            ApplicationId = securityEvent.ApplicationId?.Value,
            TenantId = securityEvent.TenantId?.Value,
            SessionId = securityEvent.SessionId?.Value,
            CorrelationId = securityEvent.CorrelationId.Value,
            NetworkSummary = securityEvent.NetworkSummary,
            UserAgentSummary = securityEvent.UserAgentSummary,
            Metadata = JsonSerializer.Serialize(orderedMetadata),
        };
    }

    public SecurityEvent ToDomain()
    {
        if (!EventTypesByCode.TryGetValue(EventType, out var eventType))
        {
            throw new InvalidOperationException("The stored security event type is not recognized.");
        }

        var values = JsonSerializer.Deserialize<Dictionary<string, string>>(Metadata)
            ?? throw new InvalidOperationException("The stored security event metadata is not an object.");

        return SecurityEvent.Create(
            new SecurityEventId(EventId),
            Timestamp,
            eventType,
            (SecurityEventResult)Result,
            ActorUserId is { } actorUserId ? new UserId(actorUserId) : null,
            TargetUserId is { } targetUserId ? new UserId(targetUserId) : null,
            ApplicationId is { } applicationId
                ? new AuthNexus.Domain.Applications.ApplicationId(applicationId)
                : null,
            TenantId is { } tenantId ? new TenantId(tenantId) : null,
            SessionId is { } sessionId ? new SessionId(sessionId) : null,
            new CorrelationId(CorrelationId),
            NetworkSummary,
            UserAgentSummary,
            SecurityEventMetadata.Create(values));
    }
}
