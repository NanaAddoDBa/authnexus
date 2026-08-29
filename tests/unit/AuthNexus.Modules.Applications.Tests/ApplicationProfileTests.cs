using AuthNexus.Domain.Tenancy;
using AuthNexus.Modules.Applications;
using DomainApplicationId = AuthNexus.Domain.Applications.ApplicationId;

namespace AuthNexus.Modules.Applications.Tests;

public sealed class ApplicationProfileTests
{
    private static readonly DomainApplicationId ApplicationId = new(
        Guid.Parse("f7779f08-c61c-461c-91d9-47a486bcfce8"));

    [Fact]
    public void Create_captures_the_minimum_resolvable_application_context()
    {
        var tenantId = new TenantId(Guid.Parse("c2cc2f89-84c6-46de-9eef-0c10d9d04f48"));
        var redirect = RedirectUri.Create("https://accounts.example.com/auth/callback");

        var profile = CreateProfile(
            tenantId: tenantId,
            applicationName: "  Customer Portal  ",
            defaultLocale: "en-us",
            authenticationPolicyReference: "  consumer-default  ",
            registrationSchemaReference: "  consumer-registration  ",
            allowedRedirectUris: [redirect]);

        Assert.Equal(ApplicationId, profile.ApplicationId);
        Assert.Equal(tenantId, profile.TenantId);
        Assert.Equal(ApplicationType.Web, profile.Type);
        Assert.Equal(ApplicationAudience.Consumer, profile.Audience);
        Assert.Equal(ApplicationMode.SignInOrRegister, profile.Mode);
        Assert.Equal("Customer Portal", profile.ApplicationName);
        Assert.Equal("en-US", profile.DefaultLocale);
        Assert.Equal("consumer-default", profile.AuthenticationPolicyReference);
        Assert.Equal("consumer-registration", profile.RegistrationSchemaReference);
        Assert.Equal([redirect], profile.AllowedRedirectUris);
    }

    [Fact]
    public void Create_accepts_a_profile_without_a_tenant_or_registration_schema()
    {
        var profile = CreateProfile(
            tenantId: null,
            mode: ApplicationMode.SignInOnly,
            registrationSchemaReference: null);

        Assert.Null(profile.TenantId);
        Assert.Null(profile.RegistrationSchemaReference);
    }

    [Fact]
    public void Create_copies_the_redirect_collection()
    {
        var originalRedirect = RedirectUri.Create("https://accounts.example.com/callback");
        var redirects = new List<RedirectUri> { originalRedirect };
        var profile = CreateProfile(allowedRedirectUris: redirects);

        redirects[0] = RedirectUri.Create("https://accounts.example.com/replaced");

        Assert.Equal([originalRedirect], profile.AllowedRedirectUris);
    }

    [Fact]
    public void AllowsRedirectTo_uses_the_registered_canonical_destination()
    {
        var profile = CreateProfile(
            allowedRedirectUris:
            [
                RedirectUri.Create("https://ACCOUNTS.example.com/callback?source=login"),
            ]);

        Assert.True(
            profile.AllowsRedirectTo(
                RedirectUri.Create("https://accounts.example.com/callback?source=login")));
        Assert.False(
            profile.AllowsRedirectTo(
                RedirectUri.Create("https://accounts.example.com/callback?source=register")));
    }

    [Fact]
    public void Create_rejects_an_empty_application_id()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => CreateProfile(applicationId: default(DomainApplicationId)));

        Assert.Equal("applicationId", exception.ParamName);
    }

    [Fact]
    public void Create_rejects_an_empty_optional_tenant_id()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => CreateProfile(tenantId: default(TenantId)));

        Assert.Equal("tenantId", exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_rejects_a_missing_application_name(string? applicationName)
    {
        var exception = Assert.Throws<ArgumentException>(
            () => CreateProfile(applicationName: applicationName!));

        Assert.Equal("applicationName", exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_rejects_a_missing_authentication_policy_reference(
        string? authenticationPolicyReference)
    {
        var exception = Assert.Throws<ArgumentException>(
            () => CreateProfile(authenticationPolicyReference: authenticationPolicyReference!));

        Assert.Equal("authenticationPolicyReference", exception.ParamName);
    }

    [Fact]
    public void Create_rejects_a_blank_registration_schema_reference()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => CreateProfile(registrationSchemaReference: "  "));

        Assert.Equal("registrationSchemaReference", exception.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not a locale")]
    public void Create_rejects_an_invalid_default_locale(string defaultLocale)
    {
        var exception = Assert.Throws<ArgumentException>(
            () => CreateProfile(defaultLocale: defaultLocale));

        Assert.Equal("defaultLocale", exception.ParamName);
    }

    [Fact]
    public void Create_rejects_undefined_classification_values()
    {
        Assert.Equal(
            "type",
            Assert.Throws<ArgumentOutOfRangeException>(
                () => CreateProfile(type: (ApplicationType)0)).ParamName);
        Assert.Equal(
            "audience",
            Assert.Throws<ArgumentOutOfRangeException>(
                () => CreateProfile(audience: (ApplicationAudience)0)).ParamName);
        Assert.Equal(
            "mode",
            Assert.Throws<ArgumentOutOfRangeException>(
                () => CreateProfile(mode: (ApplicationMode)0)).ParamName);
    }

    [Fact]
    public void Create_rejects_a_missing_redirect_allowlist()
    {
        Assert.Equal(
            "allowedRedirectUris",
            Assert.Throws<ArgumentNullException>(
                () => ApplicationProfile.Create(
                    ApplicationId,
                    null,
                    ApplicationType.Web,
                    ApplicationAudience.Consumer,
                    ApplicationMode.SignInOnly,
                    "Customer Portal",
                    "en-US",
                    "consumer-default",
                    null,
                    null!)).ParamName);
        Assert.Equal(
            "allowedRedirectUris",
            Assert.Throws<ArgumentException>(
                () => CreateProfile(allowedRedirectUris: [])).ParamName);
    }

    [Fact]
    public void Create_rejects_duplicate_redirects_after_canonicalization()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => CreateProfile(
                allowedRedirectUris:
                [
                    RedirectUri.Create("https://ACCOUNTS.example.com/callback"),
                    RedirectUri.Create("https://accounts.example.com/callback"),
                ]));

        Assert.Equal("allowedRedirectUris", exception.ParamName);
    }

    [Fact]
    public void Strong_identifiers_reject_empty_values()
    {
        Assert.Equal(
            "value",
            Assert.Throws<ArgumentException>(() => new DomainApplicationId(Guid.Empty)).ParamName);
        Assert.Equal(
            "value",
            Assert.Throws<ArgumentException>(() => new TenantId(Guid.Empty)).ParamName);
    }

    private static ApplicationProfile CreateProfile(
        DomainApplicationId? applicationId = null,
        TenantId? tenantId = null,
        ApplicationType type = ApplicationType.Web,
        ApplicationAudience audience = ApplicationAudience.Consumer,
        ApplicationMode mode = ApplicationMode.SignInOrRegister,
        string applicationName = "Customer Portal",
        string defaultLocale = "en-US",
        string authenticationPolicyReference = "consumer-default",
        string? registrationSchemaReference = "consumer-registration",
        IEnumerable<RedirectUri>? allowedRedirectUris = null)
    {
        return ApplicationProfile.Create(
            applicationId ?? ApplicationId,
            tenantId,
            type,
            audience,
            mode,
            applicationName,
            defaultLocale,
            authenticationPolicyReference,
            registrationSchemaReference,
            allowedRedirectUris ??
            [
                RedirectUri.Create("https://accounts.example.com/auth/callback"),
            ]);
    }
}
