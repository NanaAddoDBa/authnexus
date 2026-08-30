using AuthNexus.Domain;
using AuthNexus.Domain.Identity;
using AuthNexus.Domain.Sessions;
using AuthNexus.Domain.Tenancy;
using System.Globalization;
using ApplicationId = AuthNexus.Domain.Applications.ApplicationId;

namespace AuthNexus.Modules.Audit;

public sealed class SecurityEvent
{
    public const int MaximumNetworkSummaryLength = 256;
    public const int MaximumUserAgentSummaryLength = 512;

    private SecurityEvent(
        SecurityEventId eventId,
        DateTimeOffset timestamp,
        SecurityEventType eventType,
        SecurityEventResult result,
        UserId? actorUserId,
        UserId? targetUserId,
        ApplicationId? applicationId,
        TenantId? tenantId,
        SessionId? sessionId,
        CorrelationId correlationId,
        string? networkSummary,
        string? userAgentSummary,
        SecurityEventMetadata metadata)
    {
        EventId = eventId;
        Timestamp = timestamp;
        EventType = eventType;
        Result = result;
        ActorUserId = actorUserId;
        TargetUserId = targetUserId;
        ApplicationId = applicationId;
        TenantId = tenantId;
        SessionId = sessionId;
        CorrelationId = correlationId;
        NetworkSummary = networkSummary;
        UserAgentSummary = userAgentSummary;
        Metadata = metadata;
    }

    public SecurityEventId EventId { get; }

    public DateTimeOffset Timestamp { get; }

    public SecurityEventType EventType { get; }

    public string EventTypeCode => EventType.ToCode();

    public SecurityEventResult Result { get; }

    public UserId? ActorUserId { get; }

    public UserId? TargetUserId { get; }

    public ApplicationId? ApplicationId { get; }

    public TenantId? TenantId { get; }

    public SessionId? SessionId { get; }

    public CorrelationId CorrelationId { get; }

    public string? NetworkSummary { get; }

    public string? UserAgentSummary { get; }

    public SecurityEventMetadata Metadata { get; }

    public static SecurityEvent Create(
        SecurityEventId eventId,
        DateTimeOffset timestamp,
        SecurityEventType eventType,
        SecurityEventResult result,
        UserId? actorUserId,
        UserId? targetUserId,
        ApplicationId? applicationId,
        TenantId? tenantId,
        SessionId? sessionId,
        CorrelationId correlationId,
        string? networkSummary,
        string? userAgentSummary,
        SecurityEventMetadata metadata)
    {
        if (eventId.IsEmpty)
        {
            throw new ArgumentException("A security event ID is required.", nameof(eventId));
        }

        if (!Enum.IsDefined(eventType))
        {
            throw new ArgumentOutOfRangeException(
                nameof(eventType),
                eventType,
                "The security event type is not defined.");
        }

        if (!Enum.IsDefined(result))
        {
            throw new ArgumentOutOfRangeException(
                nameof(result),
                result,
                "The security event result is not defined.");
        }

        ValidateOptionalId(actorUserId, nameof(actorUserId));
        ValidateOptionalId(targetUserId, nameof(targetUserId));
        ValidateOptionalId(applicationId, nameof(applicationId));
        ValidateOptionalId(tenantId, nameof(tenantId));
        ValidateOptionalId(sessionId, nameof(sessionId));

        if (correlationId.IsEmpty)
        {
            throw new ArgumentException("A correlation ID is required.", nameof(correlationId));
        }

        ArgumentNullException.ThrowIfNull(metadata);

        return new SecurityEvent(
            eventId,
            NormalizeTimestamp(timestamp, nameof(timestamp)),
            eventType,
            result,
            actorUserId,
            targetUserId,
            applicationId,
            tenantId,
            sessionId,
            correlationId,
            NormalizeOptionalSummary(
                networkSummary,
                MaximumNetworkSummaryLength,
                nameof(networkSummary)),
            NormalizeOptionalSummary(
                userAgentSummary,
                MaximumUserAgentSummaryLength,
                nameof(userAgentSummary)),
            metadata);
    }

    private static void ValidateOptionalId(UserId? value, string parameterName)
    {
        if (value is { IsEmpty: true })
        {
            throw new ArgumentException(
                "A supplied user ID cannot be empty.",
                parameterName);
        }
    }

    private static void ValidateOptionalId(ApplicationId? value, string parameterName)
    {
        if (value is { IsEmpty: true })
        {
            throw new ArgumentException(
                "A supplied application ID cannot be empty.",
                parameterName);
        }
    }

    private static void ValidateOptionalId(TenantId? value, string parameterName)
    {
        if (value is { IsEmpty: true })
        {
            throw new ArgumentException(
                "A supplied tenant ID cannot be empty.",
                parameterName);
        }
    }

    private static void ValidateOptionalId(SessionId? value, string parameterName)
    {
        if (value is { IsEmpty: true })
        {
            throw new ArgumentException(
                "A supplied session ID cannot be empty.",
                parameterName);
        }
    }

    private static string? NormalizeOptionalSummary(
        string? value,
        int maximumLength,
        string parameterName)
    {
        if (value is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A supplied summary cannot be blank.", parameterName);
        }

        var normalizedValue = value.Trim();

        if (normalizedValue.Length > maximumLength ||
            normalizedValue.Any(IsUnsafeTextCharacter))
        {
            throw new ArgumentException(
                $"A supplied summary must be at most {maximumLength} characters and contain no control characters.",
                parameterName);
        }

        return normalizedValue;
    }

    private static bool IsUnsafeTextCharacter(char character)
    {
        var category = char.GetUnicodeCategory(character);

        return category is UnicodeCategory.Control or
            UnicodeCategory.LineSeparator or
            UnicodeCategory.ParagraphSeparator or
            UnicodeCategory.Format;
    }

    private static DateTimeOffset NormalizeTimestamp(
        DateTimeOffset timestamp,
        string parameterName)
    {
        if (timestamp == default)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                timestamp,
                "A non-default timestamp is required.");
        }

        return timestamp.ToUniversalTime();
    }
}
