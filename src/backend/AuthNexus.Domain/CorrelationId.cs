namespace AuthNexus.Domain;

public readonly record struct CorrelationId
{
    public CorrelationId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("A correlation ID cannot be empty.", nameof(value));
        }

        Value = value;
    }

    public Guid Value { get; }

    public bool IsEmpty => Value == Guid.Empty;

    public override string ToString() => Value.ToString("D");
}
