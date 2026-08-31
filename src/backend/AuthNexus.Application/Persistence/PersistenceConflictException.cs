namespace AuthNexus.Application.Persistence;

public sealed class PersistenceConflictException : Exception
{
    public PersistenceConflictException(Exception innerException)
        : base(
            "The stored record changed after it was loaded. Reload it before retrying the operation.",
            innerException)
    {
    }
}
