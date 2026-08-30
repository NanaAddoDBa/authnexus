using AuthNexus.Domain;
using AuthNexus.Domain.Authentication;
using AuthNexus.Domain.Identity;
using AuthNexus.Domain.Tenancy;
using ApplicationId = AuthNexus.Domain.Applications.ApplicationId;

namespace AuthNexus.Modules.Authentication;

public sealed class AuthenticationTransaction
{
    private AuthenticationTransaction(
        AuthenticationTransactionId transactionId,
        ApplicationId applicationId,
        TenantId? tenantId,
        UserId? userId,
        AuthenticationTransactionPurpose purpose,
        CorrelationId correlationId,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt)
    {
        TransactionId = transactionId;
        ApplicationId = applicationId;
        TenantId = tenantId;
        UserId = userId;
        Purpose = purpose;
        CorrelationId = correlationId;
        State = AuthenticationTransactionState.Initiated;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
        StateChangedAt = createdAt;
    }

    public AuthenticationTransactionId TransactionId { get; }

    public ApplicationId ApplicationId { get; }

    public TenantId? TenantId { get; }

    public UserId? UserId { get; }

    public AuthenticationTransactionPurpose Purpose { get; }

    public CorrelationId CorrelationId { get; }

    public AuthenticationTransactionState State { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset ExpiresAt { get; }

    public DateTimeOffset StateChangedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public DateTimeOffset? FailedAt { get; private set; }

    public static AuthenticationTransaction Create(
        AuthenticationTransactionId transactionId,
        ApplicationId applicationId,
        TenantId? tenantId,
        UserId? userId,
        AuthenticationTransactionPurpose purpose,
        CorrelationId correlationId,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt)
    {
        if (transactionId.IsEmpty)
        {
            throw new ArgumentException(
                "An authentication transaction ID is required.",
                nameof(transactionId));
        }

        if (applicationId.IsEmpty)
        {
            throw new ArgumentException("An application ID is required.", nameof(applicationId));
        }

        if (tenantId is { IsEmpty: true })
        {
            throw new ArgumentException(
                "A supplied tenant ID cannot be empty.",
                nameof(tenantId));
        }

        if (userId is { IsEmpty: true })
        {
            throw new ArgumentException(
                "A supplied user ID cannot be empty.",
                nameof(userId));
        }

        if (!Enum.IsDefined(purpose))
        {
            throw new ArgumentOutOfRangeException(
                nameof(purpose),
                purpose,
                "The authentication transaction purpose is not defined.");
        }

        if (correlationId.IsEmpty)
        {
            throw new ArgumentException("A correlation ID is required.", nameof(correlationId));
        }

        var normalizedCreatedAt = NormalizeTimestamp(createdAt, nameof(createdAt));
        var normalizedExpiresAt = NormalizeTimestamp(expiresAt, nameof(expiresAt));

        if (normalizedExpiresAt <= normalizedCreatedAt)
        {
            throw new ArgumentOutOfRangeException(
                nameof(expiresAt),
                expiresAt,
                "The expiry must be later than the creation time.");
        }

        return new AuthenticationTransaction(
            transactionId,
            applicationId,
            tenantId,
            userId,
            purpose,
            correlationId,
            normalizedCreatedAt,
            normalizedExpiresAt);
    }

    public void IssueChallenge(DateTimeOffset occurredAt) =>
        Transition(
            AuthenticationTransactionState.ChallengeIssued,
            occurredAt,
            AuthenticationTransactionState.Initiated);

    public void MarkPrimaryVerified(DateTimeOffset occurredAt) =>
        Transition(
            AuthenticationTransactionState.PrimaryVerified,
            occurredAt,
            AuthenticationTransactionState.Initiated,
            AuthenticationTransactionState.ChallengeIssued);

    public void RequireStepUp(DateTimeOffset occurredAt) =>
        Transition(
            AuthenticationTransactionState.StepUpRequired,
            occurredAt,
            AuthenticationTransactionState.PrimaryVerified);

    public void Complete(DateTimeOffset occurredAt) =>
        Transition(
            AuthenticationTransactionState.Completed,
            occurredAt,
            AuthenticationTransactionState.PrimaryVerified,
            AuthenticationTransactionState.StepUpRequired);

    public void Fail(DateTimeOffset occurredAt) =>
        Transition(
            AuthenticationTransactionState.Failed,
            occurredAt,
            AuthenticationTransactionState.Initiated,
            AuthenticationTransactionState.ChallengeIssued,
            AuthenticationTransactionState.PrimaryVerified,
            AuthenticationTransactionState.StepUpRequired);

    public void Cancel(DateTimeOffset occurredAt) =>
        Transition(
            AuthenticationTransactionState.Cancelled,
            occurredAt,
            AuthenticationTransactionState.Initiated,
            AuthenticationTransactionState.ChallengeIssued,
            AuthenticationTransactionState.PrimaryVerified,
            AuthenticationTransactionState.StepUpRequired);

    public void Expire(DateTimeOffset occurredAt)
    {
        EnsureStateCanTransitionTo(
            AuthenticationTransactionState.Expired,
            AuthenticationTransactionState.Initiated,
            AuthenticationTransactionState.ChallengeIssued,
            AuthenticationTransactionState.PrimaryVerified,
            AuthenticationTransactionState.StepUpRequired);

        var normalizedTimestamp = NormalizeTransitionTimestamp(occurredAt);

        if (normalizedTimestamp < ExpiresAt)
        {
            throw new ArgumentOutOfRangeException(
                nameof(occurredAt),
                occurredAt,
                "The transaction cannot expire before its configured expiry.");
        }

        ApplyState(AuthenticationTransactionState.Expired, normalizedTimestamp);
    }

    private void Transition(
        AuthenticationTransactionState requestedState,
        DateTimeOffset occurredAt,
        params AuthenticationTransactionState[] allowedCurrentStates)
    {
        EnsureStateCanTransitionTo(requestedState, allowedCurrentStates);

        var normalizedTimestamp = NormalizeTransitionTimestamp(occurredAt);

        if (normalizedTimestamp >= ExpiresAt)
        {
            throw new AuthenticationTransactionExpiredException(
                ExpiresAt,
                normalizedTimestamp);
        }

        ApplyState(requestedState, normalizedTimestamp);
    }

    private void EnsureStateCanTransitionTo(
        AuthenticationTransactionState requestedState,
        params AuthenticationTransactionState[] allowedCurrentStates)
    {
        if (!allowedCurrentStates.Contains(State))
        {
            throw new InvalidAuthenticationTransactionStateTransitionException(
                State,
                requestedState);
        }
    }

    private DateTimeOffset NormalizeTransitionTimestamp(DateTimeOffset occurredAt)
    {
        var normalizedTimestamp = NormalizeTimestamp(occurredAt, nameof(occurredAt));

        if (normalizedTimestamp < StateChangedAt)
        {
            throw new ArgumentOutOfRangeException(
                nameof(occurredAt),
                occurredAt,
                "A transaction transition cannot occur before the previous state change.");
        }

        return normalizedTimestamp;
    }

    private void ApplyState(
        AuthenticationTransactionState requestedState,
        DateTimeOffset occurredAt)
    {
        State = requestedState;
        StateChangedAt = occurredAt;

        if (requestedState == AuthenticationTransactionState.Completed)
        {
            CompletedAt = occurredAt;
        }
        else if (requestedState == AuthenticationTransactionState.Failed)
        {
            FailedAt = occurredAt;
        }
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
