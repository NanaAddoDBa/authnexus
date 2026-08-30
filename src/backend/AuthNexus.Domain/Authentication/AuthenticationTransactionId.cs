namespace AuthNexus.Domain.Authentication;

public readonly record struct AuthenticationTransactionId
{
    public AuthenticationTransactionId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "An authentication transaction ID cannot be empty.",
                nameof(value));
        }

        Value = value;
    }

    public Guid Value { get; }

    public bool IsEmpty => Value == Guid.Empty;

    public override string ToString() => Value.ToString("D");
}
