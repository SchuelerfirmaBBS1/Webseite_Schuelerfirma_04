using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Schuelerfirma.Api.Application.Auth;

namespace Schuelerfirma.Api.Tests.Integration;

public class PasswordOptionsStartupTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public void TheDefaultMinLength_IsTwelve()
    {
        var policy = factory.Services.GetRequiredService<PasswordPolicy>();

        Assert.Contains(PasswordRule.TooShort, policy.Validate("elevenchars", "anna").Select(v => v.Rule));
        Assert.Empty(policy.Validate("twelve chars", "anna"));
    }

    [Fact]
    public void AConfiguredMinLength_IsApplied()
    {
        using var configured = factory.WithWebHostBuilder(b => b.UseSetting("Auth:Password:MinLength", "20"));

        var policy = configured.Services.GetRequiredService<PasswordPolicy>();

        Assert.Contains(PasswordRule.TooShort, policy.Validate("only sixteen chars", "anna").Select(v => v.Rule));
    }

    [Theory]
    [InlineData("11")]
    [InlineData("0")]
    [InlineData("129")]
    public void AMinLengthOutsideTheAllowedRange_PreventsStartup(string value)
    {
        using var invalid = factory.WithWebHostBuilder(b => b.UseSetting("Auth:Password:MinLength", value));

        var exception = Assert.ThrowsAny<Exception>(() => invalid.CreateHttpsClient());

        Assert.Contains("MinLength", exception.ToString());
    }
}
