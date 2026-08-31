namespace AuthNexus.Application.Persistence;

public interface IAuthNexusUnitOfWork
{
    Task<int> CommitAsync(CancellationToken cancellationToken = default);

    Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default);
}
