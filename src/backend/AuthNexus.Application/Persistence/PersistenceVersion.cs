namespace AuthNexus.Application.Persistence;

public readonly record struct PersistenceVersion
{
    public PersistenceVersion(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "A persistence version cannot be empty.",
                nameof(value));
        }

        Value = value;
    }

    public Guid Value { get; }

    public override string ToString() => "[persistence-version]";
}
