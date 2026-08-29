using AuthNexus.Modules.Applications;

namespace AuthNexus.Modules.Applications.Tests;

public sealed class RedirectUriTests
{
    [Theory]
    [InlineData("https://accounts.example.com/callback")]
    [InlineData("https://accounts.example.com:8443/callback?source=auth")]
    [InlineData("http://localhost:3000/callback")]
    [InlineData("http://127.0.0.1:49152/callback")]
    [InlineData("http://[::1]:49152/callback")]
    public void Create_accepts_https_and_http_loopback_destinations(string value)
    {
        var redirect = RedirectUri.Create(value);

        Assert.Equal(new Uri(value).AbsoluteUri, redirect.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("/callback")]
    [InlineData("https:///callback")]
    [InlineData("ftp://accounts.example.com/callback")]
    [InlineData("http://accounts.example.com/callback")]
    [InlineData("https://user:secret@accounts.example.com/callback")]
    [InlineData("https://accounts.example.com/callback#complete")]
    [InlineData("https://accounts.example.com\\@attacker.example/callback")]
    public void Create_rejects_destinations_that_are_not_safe_web_redirects(string? value)
    {
        Assert.Throws<ArgumentException>(() => RedirectUri.Create(value!));
    }
}
