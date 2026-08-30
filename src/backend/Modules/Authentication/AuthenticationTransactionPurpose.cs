namespace AuthNexus.Modules.Authentication;

public enum AuthenticationTransactionPurpose
{
    SignIn = 1,
    Register = 2,
    VerifyIdentifier = 3,
    PasswordReset = 4,
    LinkExternalIdentity = 5,
    UnlinkExternalIdentity = 6,
    EnrollPasskey = 7,
    EnrollTotp = 8,
    ChangeEmail = 9,
    ChangePhone = 10,
    ReplaceRecoveryMethod = 11,
    SensitiveActionStepUp = 12,
    AdminPolicyChange = 13,
    AccountDeletion = 14,
}
