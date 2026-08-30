namespace AuthNexus.Modules.Notifications;

public sealed class InvalidNotificationOutboxStateTransitionException : InvalidOperationException
{
    public InvalidNotificationOutboxStateTransitionException(
        NotificationOutboxState currentState,
        NotificationOutboxState requestedState)
        : base(
            $"A notification outbox message cannot transition from {currentState} to {requestedState}.")
    {
        CurrentState = currentState;
        RequestedState = requestedState;
    }

    public NotificationOutboxState CurrentState { get; }

    public NotificationOutboxState RequestedState { get; }
}
