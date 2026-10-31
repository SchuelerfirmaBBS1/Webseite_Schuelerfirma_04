using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Schuelerfirma.Api.Application.Abstractions;
using Schuelerfirma.Api.Infrastructure.InMemory;

namespace Schuelerfirma.Api.Tests.Integration;

public class PersistenceStartupTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public void Development_UsesInMemoryRepositories()
    {
        Assert.IsType<InMemoryUserRepository>(factory.Services.GetRequiredService<IUserRepository>());
        Assert.IsType<InMemoryRoleRepository>(factory.Services.GetRequiredService<IRoleRepository>());
        Assert.IsType<InMemoryUserSessionRepository>(factory.Services.GetRequiredService<IUserSessionRepository>());
    }

    [Fact]
    public void Development_RepositoriesShareOneStore()
    {
        var store = factory.Services.GetRequiredService<InMemoryStore>();

        Assert.Same(store, factory.Services.GetRequiredService<InMemoryStore>());
        Assert.Single(factory.Services.GetServices<IUserRepository>());
    }

    [Fact]
    public void Production_WithoutRegisteredRepositories_RefusesToStart()
    {
        using var production = factory.WithWebHostBuilder(b => b.UseEnvironment("Production"));

        var exception = Assert.ThrowsAny<Exception>(() => production.CreateHttpsClient());

        Assert.Contains("No persistence is registered", exception.ToString());
        Assert.Contains(nameof(IUserRepository), exception.ToString());
    }
}
