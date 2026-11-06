using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Schuelerfirma.Api.Application.Abstractions;
using Schuelerfirma.Api.Application.Auth;
using Schuelerfirma.Api.Domain;
using Schuelerfirma.Api.Infrastructure.Auth;
using Schuelerfirma.Api.Infrastructure.InMemory;

namespace Schuelerfirma.Api.Tests.Auth;

public class AdminBootstrapperTests
{
    private const string StrongPassword = "Sehr-langes-Startpasswort-2026";

    private readonly FakeTimeProvider time = new(new DateTimeOffset(2026, 11, 1, 8, 0, 0, TimeSpan.Zero));
    private readonly ListLoggerProvider logs = new();
    private readonly ServiceProvider services;

    public AdminBootstrapperTests()
    {
        services = new ServiceCollection()
            .AddSingleton<IConfiguration>(new ConfigurationBuilder().Build())
            .AddLogging(logging => logging.AddProvider(logs))
            .AddInMemoryPersistence()
            .AddPasswordSecurity()
            .BuildServiceProvider();
    }

    private IUserRepository Users => services.GetRequiredService<IUserRepository>();

    private AdminBootstrapper CreateBootstrapper(string? username = "admin", string? email = "admin@example.test", string? password = StrongPassword) =>
        new(
            services.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new BootstrapOptions { AdminUsername = username, AdminEmail = email, AdminPassword = password }),
            time,
            services.GetRequiredService<ILogger<AdminBootstrapper>>());

    [Fact]
    public async Task CreatesAnActiveAdmin_WhoMustChangeThePassword()
    {
        await CreateBootstrapper().StartAsync(CancellationToken.None);

        var admin = await Users.GetByUsernameAsync("admin");
        Assert.NotNull(admin);
        Assert.Equal(DefaultRoles.Admin, admin.Role?.Name);
        Assert.Equal("admin@example.test", admin.Email);
        Assert.True(admin.IsActive);
        Assert.True(admin.MustChangePassword);
        Assert.Equal(time.GetUtcNow().UtcDateTime, admin.CreatedAt);
        var hasher = services.GetRequiredService<IPasswordHasher<User>>();
        Assert.NotEqual(StrongPassword, admin.PasswordHash);
        Assert.Equal(PasswordVerificationResult.Success, hasher.VerifyHashedPassword(admin, admin.PasswordHash, StrongPassword));
    }

    [Fact]
    public async Task NeverLogsThePassword()
    {
        await CreateBootstrapper().StartAsync(CancellationToken.None);

        Assert.DoesNotContain(StrongPassword, logs.AllText);
    }

    [Fact]
    public async Task RunningTwice_CreatesOnlyOneAdmin()
    {
        await CreateBootstrapper().StartAsync(CancellationToken.None);
        await CreateBootstrapper().StartAsync(CancellationToken.None);

        Assert.Single(await Users.GetAllAsync());
    }

    [Fact]
    public async Task WhenAnActiveAdminExists_NothingIsCreated()
    {
        await CreateBootstrapper("first", "first@example.test").StartAsync(CancellationToken.None);

        await CreateBootstrapper("second", "second@example.test").StartAsync(CancellationToken.None);

        Assert.Equal(["first"], (await Users.GetAllAsync()).Select(u => u.Username));
    }

    [Fact]
    public async Task WithoutConfiguration_NothingIsCreated_AndAWarningIsLogged()
    {
        await CreateBootstrapper(null, null, null).StartAsync(CancellationToken.None);

        Assert.Empty(await Users.GetAllAsync());
        Assert.Contains(logs.Entries, e => e.Level == LogLevel.Warning && e.Message.Contains("Bootstrap:AdminUsername"));
    }

    [Theory]
    [InlineData("Admin User", "admin@example.test")]
    [InlineData("ab", "admin@example.test")]
    [InlineData("admin", "not-an-email")]
    [InlineData("admin", "Name <admin@example.test>")]
    public async Task InvalidUsernameOrEmail_PreventsStartup(string username, string email)
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateBootstrapper(username, email).StartAsync(CancellationToken.None));
        Assert.Empty(await Users.GetAllAsync());
    }

    [Theory]
    [InlineData("short")]
    [InlineData("admin-admin-admin")]
    [InlineData("987654321098")]
    public async Task AWeakPassword_PreventsStartup_WithoutRevealingIt(string password)
    {
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateBootstrapper(password: password).StartAsync(CancellationToken.None));

        Assert.DoesNotContain(password, exception.Message);
        Assert.Empty(await Users.GetAllAsync());
    }

    [Fact]
    public async Task WhenTheNameIsTakenByADeactivatedUser_NoAdminIsCreated_AndAnErrorIsLogged()
    {
        await Users.AddAsync(new User { Username = "admin", Email = "old@example.test", RoleId = 2, IsActive = false });

        await CreateBootstrapper().StartAsync(CancellationToken.None);

        Assert.Single(await Users.GetAllAsync());
        Assert.Contains(logs.Entries, e => e.Level == LogLevel.Error);
    }
}
