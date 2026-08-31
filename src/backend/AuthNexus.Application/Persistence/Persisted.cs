namespace AuthNexus.Application.Persistence;

public sealed record Persisted<T>(T Entity, PersistenceVersion Version)
    where T : class
{
    public T Entity { get; } = Entity ?? throw new ArgumentNullException(nameof(Entity));
}
