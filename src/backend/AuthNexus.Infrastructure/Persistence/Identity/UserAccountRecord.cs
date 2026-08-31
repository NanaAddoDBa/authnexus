namespace AuthNexus.Infrastructure.Persistence.Identity;

internal sealed class UserAccountRecord : IConcurrencyTrackedRecord
{
    private UserAccountRecord()
    {
    }

    internal UserAccountRecord(
        Guid userId,
        short state,
        DateTimeOffset createdAt,
        DateTimeOffset stateChangedAt,
        Guid version)
    {
        UserId = userId;
        State = state;
        CreatedAt = createdAt;
        StateChangedAt = stateChangedAt;
        Version = version;
    }

    internal Guid UserId { get; private set; }

    internal short State { get; private set; }

    internal DateTimeOffset CreatedAt { get; private set; }

    internal DateTimeOffset StateChangedAt { get; private set; }

    internal Guid Version { get; private set; }

    Guid IConcurrencyTrackedRecord.Version
    {
        get => Version;
        set => Version = value;
    }
}
