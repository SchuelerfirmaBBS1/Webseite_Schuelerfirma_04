using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Schuelerfirma.Api.Application.Abstractions;
using Schuelerfirma.Api.Application.Auth;
using Schuelerfirma.Api.Domain;
using Schuelerfirma.Api.Infrastructure.Auth;
using Schuelerfirma.Api.Infrastructure.InMemory;

namespace Schuelerfirma.Api.Tests.Auth;

public class SessionTicketStoreTests
{
    private readonly FakeTimeProvider time = new(new DateTimeOffset(2026, 11, 1, 8, 0, 0, TimeSpan.Zero));
    private readonly ServiceProvider services;
    private readonly SessionTicketStore store;
    private readonly User user;

    public SessionTicketStoreTests()
    {
        services = new ServiceCollection().AddInMemoryPersistence().BuildServiceProvider();
        store = new SessionTicketStore(
            services.GetRequiredService<IServiceScopeFactory>(), time, Options.Create(new LoginSessionOptions()));

        user = new User { Username = "anna", Email = "anna@example.test", RoleId = 2, CreatedAt = time.GetUtcNow().UtcDateTime };
        services.GetRequiredService<IUserRepository>().AddAsync(user).GetAwaiter().GetResult();
        user.Role = new Role { RoleId = 2, Name = DefaultRoles.User };
    }

    private IUserSessionRepository Sessions => services.GetRequiredService<IUserSessionRepository>();

    private AuthenticationTicket NewTicket() =>
        new(UserPrincipal.Create(user), new AuthenticationProperties(), CookieAuthenticationDefaults.AuthenticationScheme);

    [Fact]
    public async Task TheKey_IsRandom_AndOnlyItsHashIsStored()
    {
        var first = await store.StoreAsync(NewTicket());
        var second = await store.StoreAsync(NewTicket());

        Assert.NotEqual(first, second);
        Assert.Equal(43, first.Length); // 32 random bytes as base64url
        var stored = await Sessions.GetAsync(SessionTicketStore.Hash(first));
        Assert.NotNull(stored);
        Assert.Equal(64, stored.SessionKeyHash.Length);
        Assert.NotEqual(first, stored.SessionKeyHash);
        Assert.Null(await Sessions.GetAsync(first));
    }

    [Fact]
    public async Task AStoredSession_CanBeRetrieved_WithItsUser()
    {
        var key = await store.StoreAsync(NewTicket());

        var ticket = await store.RetrieveAsync(key);

        Assert.Equal(user.UserId, ticket?.Principal.GetUserId());
    }

    [Fact]
    public async Task AnUnknownKey_RetrievesNothing()
    {
        Assert.Null(await store.RetrieveAsync("not-a-session"));
    }

    [Fact]
    public async Task ASession_ExpiresAfterTheIdleTimeout_AndIsDeleted()
    {
        var key = await store.StoreAsync(NewTicket());

        time.Advance(TimeSpan.FromMinutes(30));

        Assert.Null(await store.RetrieveAsync(key));
        Assert.Null(await Sessions.GetAsync(SessionTicketStore.Hash(key)));
    }

    [Fact]
    public async Task Renewing_ExtendsTheIdleTimeout()
    {
        var key = await store.StoreAsync(NewTicket());

        time.Advance(TimeSpan.FromMinutes(20));
        await store.RenewAsync(key, (await store.RetrieveAsync(key))!);
        time.Advance(TimeSpan.FromMinutes(20));

        Assert.NotNull(await store.RetrieveAsync(key));
    }

    [Fact]
    public async Task Renewing_NeverExtendsBeyondTheMaximumLifetime()
    {
        var key = await store.StoreAsync(NewTicket());

        // Stay active every 10 minutes until 7 h 50 min have passed, renewing the session each time.
        for (var i = 0; i < 47; i++)
        {
            time.Advance(TimeSpan.FromMinutes(10));
            await store.RenewAsync(key, (await store.RetrieveAsync(key))!);
        }

        var stored = (await Sessions.GetAsync(SessionTicketStore.Hash(key)))!;
        Assert.Equal(stored.CreatedAt.AddHours(8), stored.ExpiresAt);
        time.Advance(TimeSpan.FromMinutes(10));
        Assert.Null(await store.RetrieveAsync(key));
    }

    [Fact]
    public async Task Removing_DeletesTheSession()
    {
        var key = await store.StoreAsync(NewTicket());

        await store.RemoveAsync(key);

        Assert.Null(await store.RetrieveAsync(key));
    }

    [Fact]
    public async Task Renewing_ASessionThatWasRemoved_DoesNotBringItBack()
    {
        var key = await store.StoreAsync(NewTicket());
        var ticket = (await store.RetrieveAsync(key))!;
        await store.RemoveAsync(key);

        await store.RenewAsync(key, ticket);

        Assert.Null(await store.RetrieveAsync(key));
    }
}
