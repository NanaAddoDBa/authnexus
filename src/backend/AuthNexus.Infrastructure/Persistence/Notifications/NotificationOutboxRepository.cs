using AuthNexus.Application.Persistence;
using AuthNexus.Modules.Notifications;
using Microsoft.EntityFrameworkCore;

namespace AuthNexus.Infrastructure.Persistence.Notifications;

internal sealed class NotificationOutboxRepository : INotificationOutboxRepository
{
    public const int MaximumDueBatchSize = 500;

    private readonly AuthNexusDbContext _context;
    private readonly INotificationDestinationProtector _destinationProtector;

    internal NotificationOutboxRepository(
        AuthNexusDbContext context,
        INotificationDestinationProtector destinationProtector)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _destinationProtector = destinationProtector ??
            throw new ArgumentNullException(nameof(destinationProtector));
    }

    public async Task<Persisted<NotificationOutboxMessage>?> GetByIdAsync(
        NotificationOutboxMessageId messageId,
        CancellationToken cancellationToken = default)
    {
        if (messageId.IsEmpty)
        {
            throw new ArgumentException("A notification message ID is required.", nameof(messageId));
        }

        var record = await _context.Set<NotificationOutboxMessageRecord>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.MessageId == messageId.Value,
                cancellationToken)
            .ConfigureAwait(false);

        return record?.ToDomain(_destinationProtector);
    }

    public async Task<IReadOnlyList<Persisted<NotificationOutboxMessage>>> GetDueAsync(
        DateTimeOffset observedAt,
        int maximumCount,
        CancellationToken cancellationToken = default)
    {
        if (observedAt == default)
        {
            throw new ArgumentOutOfRangeException(
                nameof(observedAt),
                observedAt,
                "A non-default observation time is required.");
        }

        if (maximumCount is <= 0 or > MaximumDueBatchSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumCount),
                maximumCount,
                $"The due-message batch size must be between 1 and {MaximumDueBatchSize}.");
        }

        var normalizedObservedAt = observedAt.ToUniversalTime();
        var dueStates = new[]
        {
            (int)NotificationOutboxState.Pending,
            (int)NotificationOutboxState.RetryScheduled,
        };

        var records = await _context.Set<NotificationOutboxMessageRecord>()
            .AsNoTracking()
            .Where(record =>
                dueStates.Contains(record.State) &&
                record.NextAttemptAt != null &&
                record.NextAttemptAt <= normalizedObservedAt)
            .OrderBy(record => record.NextAttemptAt)
            .ThenBy(record => record.MessageId)
            .Take(maximumCount)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return records
            .Select(record => record.ToDomain(_destinationProtector))
            .ToArray();
    }

    public void Add(NotificationOutboxMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        _context.Set<NotificationOutboxMessageRecord>().Add(
            NotificationOutboxMessageRecord.FromDomain(message, _destinationProtector));
    }

    public void Update(Persisted<NotificationOutboxMessage> message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var record = NotificationOutboxMessageRecord.FromDomain(
            message.Entity,
            _destinationProtector,
            message.Version);
        var entry = _context.Attach(record);

        entry.Property(candidate => candidate.Version).OriginalValue = message.Version.Value;
        entry.State = EntityState.Modified;
    }
}
