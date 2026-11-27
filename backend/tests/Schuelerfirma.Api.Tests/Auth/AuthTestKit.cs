using System.Net.Http.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Schuelerfirma.Api.Application.Abstractions;
using Schuelerfirma.Api.Domain;
using Schuelerfirma.Api.Infrastructure.Auth;
using Schuelerfirma.Api.Infrastructure.Http;
using Schuelerfirma.Api.Infrastructure.InMemory;
using Schuelerfirma.Api.Tests.Integration;

namespace Schuelerfirma.Api.Tests.Auth;

/// <summary>Collects log messages so tests can check what was (and was not) logged.</summary>
public sealed class ListLoggerProvider : ILoggerProvider
{
    private readonly List<(LogLevel Level, string Message)> entries = [];

    /// <summary>All messages logged so far, formatted.</summary>
    public IReadOnlyList<(LogLevel Level, string Message)> Entries
    {
        get
        {
            lock (entries)
            {
                return [.. entries];
            }
        }
    }

    /// <summary>All formatted messages as one string.</summary>
    public string AllText => string.Join('\n', Entries.Select(e => e.Message));

    public ILogger CreateLogger(string categoryName) => new ListLogger(this);

    public void Dispose()
    {
    }

    private sealed class ListLogger(ListLoggerProvider owner) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            lock (owner.entries)
            {
                owner.entries.Add((logLevel, formatter(state, exception)));
            }
        }
    }
}

/// <summary>A controller without any attribute: the fallback policy must protect it.</summary>
[ApiController]
[Route("test/probe")]
public class ProbeController : ControllerBase
{
    [HttpGet("protected")]
    public IActionResult Protected() => Ok();

    [AllowAnonymous]
    [HttpGet("open")]
    public IActionResult Open() => Ok();
}

/// <summary>
/// Test host with a fake clock, a known first administrator and a few probe endpoints, for the login tests.
/// </summary>
public class AuthApiFactory : ApiFactory
{
    public const string AdminUsername = "admin";
    public const string AdminEmail = "admin@example.test";
    public const string AdminPassword = "Sehr-langes-Startpasswort-2026";
    public const string MemberPassword = "Ein-anderes-langes-Passwort-17";

    public FakeTimeProvider Time { get; } = new(new DateTimeOffset(2026, 11, 1, 8, 0, 0, TimeSpan.Zero));

    public ListLoggerProvider Logs { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["Bootstrap:AdminUsername"] = AdminUsername,
                ["Bootstrap:AdminEmail"] = AdminEmail,
                ["Bootstrap:AdminPassword"] = AdminPassword,
                // The login tests log in far more often than the real limit allows; RateLimitingTests check the limit.
                ["RateLimiting:Auth:PermitLimit"] = "10000",
            }));
        builder.ConfigureLogging(logging => logging.AddProvider(Logs));
        builder.ConfigureServices(services =>
        {
            services.AddSingleton<TimeProvider>(Time);
            services.AddControllers().AddApplicationPart(typeof(ProbeController).Assembly);
        });
    }

    /// <summary>Leaves <c>MustChangePassword</c> of the bootstrapped administrator as in production.</summary>
    public bool KeepBootstrapPasswordFlag { get; init; }

    /// <summary>
    /// The bootstrapped administrator has to change the password, like in production. Most tests want a usable
    /// admin, so the flag is removed after startup; tests for the flag create users with <c>mustChangePassword</c>.
    /// </summary>
    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        if (KeepBootstrapPasswordFlag)
        {
            return host;
        }

        var users = host.Services.GetRequiredService<IUserRepository>();
        var admin = users.GetByUsernameAsync(AdminUsername).GetAwaiter().GetResult();
        if (admin is not null)
        {
            admin.MustChangePassword = false;
            users.UpdateAsync(admin).GetAwaiter().GetResult();
        }

        return host;
    }

    public IUserRepository Users => Services.GetRequiredService<IUserRepository>();

    public IUserSessionRepository Sessions => Services.GetRequiredService<IUserSessionRepository>();

    /// <summary>Creates a user directly in the store (the bootstrapped admin exists already).</summary>
    public async Task<User> AddUserAsync(
        string username, string password = MemberPassword, string role = DefaultRoles.User, bool active = true, bool mustChangePassword = false)
    {
        var roleId = (await Services.GetRequiredService<IRoleRepository>().GetByNameAsync(role))!.RoleId;
        var user = new User
        {
            Username = username,
            Email = $"{username}@example.test",
            RoleId = roleId,
            IsActive = active,
            MustChangePassword = mustChangePassword,
            CreatedAt = Time.GetUtcNow().UtcDateTime,
        };
        user.PasswordHash = Services.GetRequiredService<IPasswordHasher<User>>().HashPassword(user, password);
        await Users.AddAsync(user);
        return user;
    }

    /// <summary>
    /// Fetches a CSRF token pair for an anonymous visitor without any cookie handling: the value for the
    /// <c>Cookie</c> header (<c>__Host-sf.csrf=...</c>) and the value for the <c>X-XSRF-TOKEN</c> header.
    /// </summary>
    public async Task<(string Cookie, string Header)> FetchAnonymousCsrfTokensAsync()
    {
        var response = await CreateDefaultClient(new Uri(WebApplicationFactoryExtensions.Host)).GetAsync("/api/auth/csrf");
        var cookies = response.Headers.GetValues("Set-Cookie").Select(c => c[..c.IndexOf(';')]).ToList();
        var cookie = cookies.Single(c => c.StartsWith(CsrfProtectionExtensions.CookieName + "="));
        var header = cookies.Single(c => c.StartsWith(CsrfProtectionExtensions.RequestTokenCookieName + "="));
        return (cookie, header[(header.IndexOf('=') + 1)..]);
    }

    /// <summary>Posts credentials to the login endpoint.</summary>
    public static Task<HttpResponseMessage> LoginAsync(HttpClient client, string username, string password) =>
        client.PostAsJsonAsync("/api/auth/login", new { username, password });
}

/// <summary>Wraps the framework hasher and remembers which hashes it was asked to verify.</summary>
public sealed class RecordingHasher(IPasswordHasher<User> inner) : IPasswordHasher<User>
{
    public List<string> VerifiedHashes { get; } = [];

    public string HashPassword(User user, string password) => inner.HashPassword(user, password);

    public PasswordVerificationResult VerifyHashedPassword(User user, string hashedPassword, string providedPassword)
    {
        VerifiedHashes.Add(hashedPassword);
        return inner.VerifyHashedPassword(user, hashedPassword, providedPassword);
    }
}
