namespace AuthNexus.Modules.Authentication;

public enum AuthenticationTransactionState
{
    Initiated = 1,
    ChallengeIssued = 2,
    PrimaryVerified = 3,
    StepUpRequired = 4,
    Completed = 5,
    Failed = 6,
    Expired = 7,
    Cancelled = 8,
}
