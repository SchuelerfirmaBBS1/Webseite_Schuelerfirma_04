using Schuelerfirma.Api.Infrastructure.Auth;

namespace Schuelerfirma.Api.Tests.Auth;

public class EmbeddedCommonPasswordListTests
{
    private readonly EmbeddedCommonPasswordList list = new();

    [Fact]
    public void TheEmbeddedListIsLoaded()
    {
        Assert.True(list.Count > 1000);
    }

    [Theory]
    [InlineData("qwe123qwe123")]
    [InlineData("ThisIsMyPassword")]
    public void KnownCommonPasswords_AreFound_IgnoringCase(string password)
    {
        Assert.True(list.Contains(password));
    }

    [Fact]
    public void AnUncommonPassphrase_IsNotFound()
    {
        Assert.False(list.Contains("zebra orchid lantern 42 harbor"));
    }
}
