using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Schuelerfirma.Api.Application.Abstractions;
using Schuelerfirma.Api.Application.Auth;
using Schuelerfirma.Api.Domain;
using Schuelerfirma.Api.Infrastructure.Auth;
using Schuelerfirma.Api.Infrastructure.InMemory;

namespace Schuelerfirma.Api.Tests.Auth;

public class SessionCleanupServiceTests
{
    private readonly FakeTimeProvider time = new(new DateTimeOffset(2026, 11, 1, 8, 0, 0, TimeSpan.Zero));
    private readonly ServiceProvider services;

    public SessionCleanupServiceTests()
    {
        services = new ServiceCollection().AddInMemoryPersistence().BuildServiceProvider();
    }

    private IUserSessionRepository Sessions => services.GetRequiredService<IUserSessionRepository>();

    private SessionCleanupService CreateService() => new(
        services.GetRequiredService<IServiceScopeFactory>(),
        time,
        Options.Create(new LoginSessionOptions { CleanupInterval = TimeSpan.FromMinutes(10) }),
        NullLogger<SessionCleanupService>.Instance);

    private async Task AddSessionsAsync()
    {
        var user = new User { Username = "anna", Email = "anna@example.test", RoleId = 2 };
        await services.GetRequiredService<IUserRepository>().AddAsync(user);
        var now = time.GetUtcNow().UtcDateTime;
        await Sessions.AddAsync(new UserSession { SessionKeyHash = new string('a', 64), UserId = user.UserId, CreatedAt = now, ExpiresAt = now.AddMinutes(5) });
        await Sessions.AddAsync(new UserSession { SessionKeyHash = new string('b', 64), UserId = user.UserId, CreatedAt = now, ExpiresAt = now.AddHours(24) });
    }

    [Fact]
    public async Task Cleanup_RemovesOnlyExpiredSessions()
    {
        await AddSessionsAsync();
        time.Advance(TimeSpan.FromMinutes(10));

        var deleted = await CreateService().CleanupAsync();

        Assert.Equal(1, deleted);
        Assert.Null(await Sessions.GetAsync(new string('a', 64)));
        Assert.NotNull(await Sessions.GetAsync(new string('b', 64)));
    }

    [Fact]
    public async Task TheService_CleansUpOnItsOwn_EveryInterval()
    {
        await AddSessionsAsync();
        using var service = CreateService();
        await service.StartAsync(CancellationToken.None);
        try
        {
            // The timer is created shortly after StartAsync returns; keep advancing the clock until it has fired.
            for (var i = 0; i < 100 && await Sessions.GetAsync(new string('a', 64)) is not null; i++)
            {
                time.Advance(TimeSpan.FromMinutes(10));
                await Task.Delay(20);
            }

            Assert.Null(await Sessions.GetAsync(new string('a', 64)));
            Assert.NotNull(await Sessions.GetAsync(new string('b', 64)));
        }
        finally
        {
            await service.StopAsync(CancellationToken.None);
        }
    }
}
