namespace AuthNexus.Modules.Identity;

public sealed class InvalidUserAccountStateTransitionException : InvalidOperationException
{
    public InvalidUserAccountStateTransitionException(
        UserAccountState currentState,
        UserAccountState requestedState)
        : base($"A user account cannot transition from {currentState} to {requestedState}.")
    {
        CurrentState = currentState;
        RequestedState = requestedState;
    }

    public UserAccountState CurrentState { get; }

    public UserAccountState RequestedState { get; }
}
