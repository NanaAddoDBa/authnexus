namespace AuthNexus.Modules.Sessions;

public enum SessionRevocationReason
{
    UserLogout = 1,
    UserRevoked = 2,
    LogoutAll = 3,
    CredentialReset = 4,
    SecurityStampChanged = 5,
    AccountProtected = 6,
    AccountSuspended = 7,
    AccountDeletion = 8,
    AdministratorRevoked = 9,
    SecurityIncident = 10,
}
