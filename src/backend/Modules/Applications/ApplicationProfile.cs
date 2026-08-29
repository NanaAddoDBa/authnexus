using System.Collections.ObjectModel;
using System.Globalization;
using AuthNexus.Domain.Tenancy;
using DomainApplicationId = AuthNexus.Domain.Applications.ApplicationId;

namespace AuthNexus.Modules.Applications;

public sealed class ApplicationProfile
{
    private readonly ReadOnlyCollection<RedirectUri> _allowedRedirectUris;

    private ApplicationProfile(
        DomainApplicationId applicationId,
        TenantId? tenantId,
        ApplicationType type,
        ApplicationAudience audience,
        ApplicationMode mode,
        string applicationName,
        string defaultLocale,
        string authenticationPolicyReference,
        string? registrationSchemaReference,
        ReadOnlyCollection<RedirectUri> allowedRedirectUris)
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
        _allowedRedirectUris = allowedRedirectUris;
    }

    public DomainApplicationId ApplicationId { get; }

    public TenantId? TenantId { get; }

    public ApplicationType Type { get; }

    public ApplicationAudience Audience { get; }

    public ApplicationMode Mode { get; }

    public string ApplicationName { get; }

    public string DefaultLocale { get; }

    public string AuthenticationPolicyReference { get; }

    public string? RegistrationSchemaReference { get; }

    public IReadOnlyList<RedirectUri> AllowedRedirectUris => _allowedRedirectUris;

    public static ApplicationProfile Create(
        DomainApplicationId applicationId,
        TenantId? tenantId,
        ApplicationType type,
        ApplicationAudience audience,
        ApplicationMode mode,
        string applicationName,
        string defaultLocale,
        string authenticationPolicyReference,
        string? registrationSchemaReference,
        IEnumerable<RedirectUri> allowedRedirectUris)
    {
        if (applicationId.IsEmpty)
        {
            throw new ArgumentException("An application ID is required.", nameof(applicationId));
        }

        if (tenantId is { IsEmpty: true })
        {
            throw new ArgumentException("A tenant ID cannot be empty when supplied.", nameof(tenantId));
        }

        if (!Enum.IsDefined(type))
        {
            throw new ArgumentOutOfRangeException(nameof(type), type, "The application type is not defined.");
        }

        if (!Enum.IsDefined(audience))
        {
            throw new ArgumentOutOfRangeException(
                nameof(audience),
                audience,
                "The application audience is not defined.");
        }

        if (!Enum.IsDefined(mode))
        {
            throw new ArgumentOutOfRangeException(nameof(mode), mode, "The application mode is not defined.");
        }

        var normalizedName = RequireText(applicationName, nameof(applicationName));
        var normalizedLocale = NormalizeLocale(defaultLocale, nameof(defaultLocale));
        var normalizedPolicyReference = RequireText(
            authenticationPolicyReference,
            nameof(authenticationPolicyReference));
        var normalizedSchemaReference = NormalizeOptionalText(
            registrationSchemaReference,
            nameof(registrationSchemaReference));
        var redirectUris = CopyAndValidateRedirectUris(allowedRedirectUris);

        return new ApplicationProfile(
            applicationId,
            tenantId,
            type,
            audience,
            mode,
            normalizedName,
            normalizedLocale,
            normalizedPolicyReference,
            normalizedSchemaReference,
            redirectUris);
    }

    public bool AllowsRedirectTo(RedirectUri destination)
    {
        ArgumentNullException.ThrowIfNull(destination);

        return _allowedRedirectUris.Contains(destination);
    }

    private static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A non-empty value is required.", parameterName);
        }

        return value.Trim();
    }

    private static string? NormalizeOptionalText(string? value, string parameterName)
    {
        if (value is null)
        {
            return null;
        }

        return RequireText(value, parameterName);
    }

    private static string NormalizeLocale(string value, string parameterName)
    {
        var candidate = RequireText(value, parameterName);

        try
        {
            var culture = CultureInfo.GetCultureInfo(candidate);

            if (string.IsNullOrEmpty(culture.Name))
            {
                throw new ArgumentException("The invariant culture cannot be used as a locale.", parameterName);
            }

            return culture.Name;
        }
        catch (CultureNotFoundException exception)
        {
            throw new ArgumentException("The locale is not recognized.", parameterName, exception);
        }
    }

    private static ReadOnlyCollection<RedirectUri> CopyAndValidateRedirectUris(
        IEnumerable<RedirectUri> allowedRedirectUris)
    {
        ArgumentNullException.ThrowIfNull(allowedRedirectUris);

        var redirects = allowedRedirectUris.ToArray();

        if (redirects.Length == 0)
        {
            throw new ArgumentException(
                "At least one allowed redirect URI is required.",
                nameof(allowedRedirectUris));
        }

        if (redirects.Any(redirect => redirect is null))
        {
            throw new ArgumentException(
                "Allowed redirect URIs cannot contain null values.",
                nameof(allowedRedirectUris));
        }

        if (redirects.Distinct().Count() != redirects.Length)
        {
            throw new ArgumentException(
                "Allowed redirect URIs cannot contain duplicates.",
                nameof(allowedRedirectUris));
        }

        return Array.AsReadOnly(redirects);
    }
}
