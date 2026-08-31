namespace AuthNexus.Infrastructure.Persistence.Applications;

internal sealed class ApplicationRedirectUriRecord
{
    private ApplicationRedirectUriRecord()
    {
    }

    internal ApplicationRedirectUriRecord(Guid applicationId, int sortOrder, string redirectUri)
    {
        ApplicationId = applicationId;
        SortOrder = sortOrder;
        RedirectUri = redirectUri;
    }

    internal Guid ApplicationId { get; private set; }

    internal int SortOrder { get; private set; }

    internal string RedirectUri { get; private set; } = string.Empty;
}
