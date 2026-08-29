namespace AuthNexus.Modules.Identity;

public enum UserAccountState
{
    PendingVerification = 1,
    Active = 2,
    TemporarilyProtected = 3,
    Suspended = 4,
    DeletionPending = 5,
    Deleted = 6,
}
