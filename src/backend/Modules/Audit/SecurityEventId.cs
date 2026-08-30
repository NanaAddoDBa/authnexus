namespace AuthNexus.Modules.Audit;

public readonly record struct SecurityEventId
{
    public SecurityEventId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("A security event ID cannot be empty.", nameof(value));
        }

        Value = value;
    }

    public Guid Value { get; }

    public bool IsEmpty => Value == Guid.Empty;

    public override string ToString() => Value.ToString("D");
}
