namespace AuthNexus.Modules.Sessions;

public sealed class InvalidSessionStateTransitionException : InvalidOperationException
{
    public InvalidSessionStateTransitionException(
        SessionState currentState,
        SessionState requestedState)
        : base($"A session cannot transition from {currentState} to {requestedState}.")
    {
        CurrentState = currentState;
        RequestedState = requestedState;
    }

    public SessionState CurrentState { get; }

    public SessionState RequestedState { get; }
}
