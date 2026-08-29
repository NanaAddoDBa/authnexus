namespace AuthNexus.Domain.Identity;

public readonly record struct UserId
{
    public UserId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("A user ID cannot be empty.", nameof(value));
        }

        Value = value;
    }

    public Guid Value { get; }

    public bool IsEmpty => Value == Guid.Empty;

    public override string ToString() => Value.ToString("D");
}
