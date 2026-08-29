using AuthNexus.Contracts;

namespace AuthNexus.Contracts.Tests;

public sealed class AuthNexusProductTests
{
    [Fact]
    public void FormalName_uses_the_canonical_product_identity()
    {
        Assert.Equal("AuthNexus", AuthNexusProduct.Name);
        Assert.Equal(
            "AuthNexus — Universal Authentication Platform",
            AuthNexusProduct.FormalName);
    }
}
