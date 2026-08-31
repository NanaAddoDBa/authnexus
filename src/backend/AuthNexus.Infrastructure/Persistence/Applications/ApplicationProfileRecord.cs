namespace AuthNexus.Infrastructure.Persistence.Applications;

internal sealed class ApplicationProfileRecord
{
    private ApplicationProfileRecord()
    {
    }

    internal ApplicationProfileRecord(
        Guid applicationId,
        Guid? tenantId,
        short type,
        short audience,
        short mode,
        string applicationName,
        string defaultLocale,
        string authenticationPolicyReference,
        string? registrationSchemaReference,
        Guid version,
        IEnumerable<ApplicationRedirectUriRecord> allowedRedirectUris)
    {
        ApplicationId = applicationId;
        TenantId = tenantId;
        Type = type;
        Audience = audience;
        Mode = mode;
        ApplicationName = applicationName;
        DefaultLocale = defaultLocale;
        AuthenticationPolicyReference = authenticationPolicyReference;
        RegistrationSchemaReference = registrationSchemaReference;
        Version = version;
        AllowedRedirectUris = allowedRedirectUris.ToList();
    }

    internal Guid ApplicationId { get; private set; }

    internal Guid? TenantId { get; private set; }

    internal short Type { get; private set; }

    internal short Audience { get; private set; }

    internal short Mode { get; private set; }

    internal string ApplicationName { get; private set; } = string.Empty;

    internal string DefaultLocale { get; private set; } = string.Empty;

    internal string AuthenticationPolicyReference { get; private set; } = string.Empty;

    internal string? RegistrationSchemaReference { get; private set; }

    internal Guid Version { get; private set; }

    internal List<ApplicationRedirectUriRecord> AllowedRedirectUris { get; private set; } = [];
}
