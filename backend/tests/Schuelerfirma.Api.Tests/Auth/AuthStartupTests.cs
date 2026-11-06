using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Schuelerfirma.Api.Infrastructure.InMemory;
using Schuelerfirma.Api.Tests.Integration;

namespace Schuelerfirma.Api.Tests.Auth;

public class AuthStartupTests : IDisposable
{
    private readonly AuthApiFactory factory = new();

    public void Dispose() => factory.Dispose();

    [Fact]
    public async Task TheEncryptionKeys_ArePersistedInTheConfiguredDirectory()
    {
        await AuthApiFactory.LoginAsync(factory.CreateHttpsClient(), AuthApiFactory.AdminUsername, AuthApiFactory.AdminPassword);

        Assert.NotEmpty(Directory.GetFiles(factory.KeysPath, "key-*.xml"));
    }

    [Fact]
    public void WithoutAKeysPath_ProductionRefusesToStart()
    {
        using var production = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?> { ["DataProtection:KeysPath"] = "" }));
            builder.ConfigureServices(services => services.AddInMemoryPersistence());
        });

        var exception = Assert.ThrowsAny<Exception>(() => production.CreateHttpsClient());

        Assert.Contains("DataProtection:KeysPath", exception.ToString());
    }

    [Theory]
    [InlineData("Auth:Lockout:MaxFailedAttempts", "0", "MaxFailedAttempts")]
    [InlineData("Auth:Lockout:Duration", "00:00:00", "Duration")]
    [InlineData("Auth:Session:IdleTimeout", "00:00:10", "IdleTimeout")]
    [InlineData("Auth:Session:MaxLifetime", "00:10:00", "MaxLifetime")]
    [InlineData("Auth:Session:MaxLifetime", "30.00:00:00", "MaxLifetime")]
    [InlineData("Auth:Session:CleanupInterval", "00:00:01", "CleanupInterval")]
    public void InvalidLockoutOrSessionSettings_PreventStartup(string key, string value, string expectedInMessage)
    {
        using var invalid = factory.WithWebHostBuilder(b => b.UseSetting(key, value));

        var exception = Assert.ThrowsAny<Exception>(() => invalid.CreateHttpsClient());

        Assert.Contains(expectedInMessage, exception.ToString());
    }

    [Fact]
    public async Task ConfiguredLimits_AreApplied()
    {
        using var strict = new AuthApiFactory().WithWebHostBuilder(b => b.UseSetting("Auth:Lockout:MaxFailedAttempts", "2"));
        var client = strict.CreateHttpsClient();

        await AuthApiFactory.LoginAsync(client, AuthApiFactory.AdminUsername, "wrong password!!");
        await AuthApiFactory.LoginAsync(client, AuthApiFactory.AdminUsername, "wrong password!!");
        var response = await AuthApiFactory.LoginAsync(client, AuthApiFactory.AdminUsername, AuthApiFactory.AdminPassword);

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
