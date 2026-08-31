using AuthNexus.Modules.Notifications;

namespace AuthNexus.Infrastructure.Persistence.Notifications;

internal interface INotificationDestinationProtector
{
    ProtectedNotificationDestination Protect(
        NotificationOutboxMessageId messageId,
        NotificationDestination destination);

    NotificationDestination Unprotect(
        NotificationOutboxMessageId messageId,
        ProtectedNotificationDestination protectedDestination);
}
