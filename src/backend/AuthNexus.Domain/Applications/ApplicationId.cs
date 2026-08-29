namespace AuthNexus.Domain.Applications;

public readonly record struct ApplicationId
{
    public ApplicationId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("An application ID cannot be empty.", nameof(value));
        }

        Value = value;
    }

    public Guid Value { get; }

    public bool IsEmpty => Value == Guid.Empty;

    public override string ToString() => Value.ToString("D");
}
