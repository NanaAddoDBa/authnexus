using AuthNexus.Modules.Notifications;

namespace AuthNexus.Application.Persistence;

public interface INotificationOutboxRepository
{
    Task<Persisted<NotificationOutboxMessage>?> GetByIdAsync(
        NotificationOutboxMessageId messageId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Persisted<NotificationOutboxMessage>>> GetDueAsync(
        DateTimeOffset observedAt,
        int maximumCount,
        CancellationToken cancellationToken = default);

    void Add(NotificationOutboxMessage message);

    void Update(Persisted<NotificationOutboxMessage> message);
}
