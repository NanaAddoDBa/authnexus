namespace AuthNexus.Modules.Authentication;

public sealed class InvalidAuthenticationTransactionStateTransitionException
    : InvalidOperationException
{
    public InvalidAuthenticationTransactionStateTransitionException(
        AuthenticationTransactionState currentState,
        AuthenticationTransactionState requestedState)
        : base(
            "An authentication transaction cannot transition " +
            $"from {currentState} to {requestedState}.")
    {
        CurrentState = currentState;
        RequestedState = requestedState;
    }

    public AuthenticationTransactionState CurrentState { get; }

    public AuthenticationTransactionState RequestedState { get; }
}
