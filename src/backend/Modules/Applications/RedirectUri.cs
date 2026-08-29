namespace AuthNexus.Modules.Applications;

public sealed record RedirectUri
{
    private RedirectUri(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static RedirectUri Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A redirect URI is required.", nameof(value));
        }

        var candidate = value.Trim();

        if (candidate.Contains('\\'))
        {
            throw new ArgumentException("A redirect URI cannot contain backslashes.", nameof(value));
        }

        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri) ||
            string.IsNullOrWhiteSpace(uri.Host))
        {
            throw new ArgumentException("A redirect URI must be an absolute URI with a host.", nameof(value));
        }

        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            throw new ArgumentException("A redirect URI cannot contain user information.", nameof(value));
        }

        if (!string.IsNullOrEmpty(uri.Fragment))
        {
            throw new ArgumentException("A redirect URI cannot contain a fragment.", nameof(value));
        }

        var usesHttps = string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
        var usesLoopbackHttp =
            string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
            uri.IsLoopback;

        if (!usesHttps && !usesLoopbackHttp)
        {
            throw new ArgumentException(
                "A redirect URI must use HTTPS, except for HTTP loopback callbacks.",
                nameof(value));
        }

        return new RedirectUri(uri.AbsoluteUri);
    }

    public override string ToString() => Value;
}
