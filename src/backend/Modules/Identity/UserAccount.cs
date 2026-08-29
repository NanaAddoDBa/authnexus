using AuthNexus.Domain.Identity;

namespace AuthNexus.Modules.Identity;

public sealed class UserAccount
{
    private UserAccount(UserId userId, DateTimeOffset createdAt)
    {
        UserId = userId;
        State = UserAccountState.PendingVerification;
        CreatedAt = createdAt;
        StateChangedAt = createdAt;
    }

    public UserId UserId { get; }

    public UserAccountState State { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset StateChangedAt { get; private set; }

    public static UserAccount Create(UserId userId, DateTimeOffset createdAt)
    {
        if (userId.IsEmpty)
        {
            throw new ArgumentException("A user ID is required.", nameof(userId));
        }

        return new UserAccount(userId, NormalizeTimestamp(createdAt, nameof(createdAt)));
    }

    public void Activate(DateTimeOffset occurredAt) =>
        Transition(
            UserAccountState.PendingVerification,
            UserAccountState.Active,
            occurredAt);

    public void ProtectTemporarily(DateTimeOffset occurredAt) =>
        Transition(
            UserAccountState.Active,
            UserAccountState.TemporarilyProtected,
            occurredAt);

    public void RestoreAfterProtection(DateTimeOffset occurredAt) =>
        Transition(
            UserAccountState.TemporarilyProtected,
            UserAccountState.Active,
            occurredAt);

    public void Suspend(DateTimeOffset occurredAt) =>
        Transition(
            UserAccountState.Active,
            UserAccountState.Suspended,
            occurredAt);

    public void Reactivate(DateTimeOffset occurredAt) =>
        Transition(
            UserAccountState.Suspended,
            UserAccountState.Active,
            occurredAt);

    public void RequestDeletion(DateTimeOffset occurredAt) =>
        Transition(
            UserAccountState.Active,
            UserAccountState.DeletionPending,
            occurredAt);

    public void CompleteDeletion(DateTimeOffset occurredAt) =>
        Transition(
            UserAccountState.DeletionPending,
            UserAccountState.Deleted,
            occurredAt);

    private void Transition(
        UserAccountState requiredState,
        UserAccountState nextState,
        DateTimeOffset occurredAt)
    {
        if (State != requiredState)
        {
            throw new InvalidUserAccountStateTransitionException(State, nextState);
        }

        var normalizedTimestamp = NormalizeTimestamp(occurredAt, nameof(occurredAt));

        if (normalizedTimestamp < StateChangedAt)
        {
            throw new ArgumentOutOfRangeException(
                nameof(occurredAt),
                occurredAt,
                "A state transition cannot occur before the previous state change.");
        }

        State = nextState;
        StateChangedAt = normalizedTimestamp;
    }

    private static DateTimeOffset NormalizeTimestamp(DateTimeOffset timestamp, string parameterName)
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
