using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.Options;
using Schuelerfirma.Api.Application.Auth;

namespace Schuelerfirma.Api.Infrastructure.Auth;

/// <summary>
/// Settings for where the keys that encrypt the session cookie are stored.
/// </summary>
public class DataProtectionKeyOptions
{
    /// <summary>Configuration section these options are bound from.</summary>
    public const string SectionName = "DataProtection";

    /// <summary>
    /// Directory for the encryption keys. Required outside Development; without persistent keys every restart would
    /// log everybody out. The directory must be writable for the application and unreadable for everybody else,
    /// because the keys are stored unencrypted.
    /// </summary>
    public string? KeysPath { get; set; }
}

/// <summary>
/// Registers login with server-side sessions, the lockout, the first-admin bootstrapper and the rule that every
/// endpoint needs a logged-in user unless it is explicitly anonymous.
/// </summary>
public static class AuthenticationExtensions
{
    /// <summary>Name of the session cookie. The <c>__Host-</c> prefix makes browsers insist on Secure and Path=/.</summary>
    public const string CookieName = "__Host-sf.session";

    /// <summary>Registers everything needed for login; the middleware is added with <c>UseAuthentication</c> and <c>UseAuthorization</c>.</summary>
    public static IServiceCollection AddSessionAuthentication(this IServiceCollection services)
    {
        AddOptions(services);
        AddDataProtectionKeys(services);

        services.AddSingleton<SessionTicketStore>();
        services.AddScoped<SessionCookieEvents>();
        services.AddSingleton<DummyPasswordHash>();
        services.AddScoped<AuthService>();
        services.AddHostedService<AdminBootstrapper>();
        services.AddHostedService<SessionCleanupService>();

        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = CookieName;
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.Cookie.Path = "/";
                options.Cookie.IsEssential = true;
                options.SlidingExpiration = true;
                options.EventsType = typeof(SessionCookieEvents);
            });

        // Values that come from DI are applied lazily, so the final configuration (and a fake clock in tests) is used.
        services.AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
            .Configure<SessionTicketStore, TimeProvider, IOptions<LoginSessionOptions>>((cookie, store, time, session) =>
            {
                cookie.SessionStore = store;
                cookie.TimeProvider = time;
                cookie.ExpireTimeSpan = session.Value.IdleTimeout;
            });

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        return services;
    }

    private static void AddOptions(IServiceCollection services)
    {
        services.AddOptions<LoginSessionOptions>().BindConfiguration(LoginSessionOptions.SectionName)
            .ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<LoginLockoutOptions>().BindConfiguration(LoginLockoutOptions.SectionName)
            .ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<BootstrapOptions>().BindConfiguration(BootstrapOptions.SectionName);
    }

    private static void AddDataProtectionKeys(IServiceCollection services)
    {
        services.AddOptions<DataProtectionKeyOptions>().BindConfiguration(DataProtectionKeyOptions.SectionName)
            .Validate<IHostEnvironment>(
                (options, environment) => environment.IsDevelopment() || !string.IsNullOrWhiteSpace(options.KeysPath),
                "DataProtection:KeysPath must be set outside Development, otherwise every restart logs everybody out.")
            .ValidateOnStart();

        services.AddDataProtection().SetApplicationName("Schuelerfirma.Api");

        services.AddOptions<KeyManagementOptions>()
            .Configure<IOptions<DataProtectionKeyOptions>, ILoggerFactory>((management, keyOptions, loggerFactory) =>
            {
                var path = keyOptions.Value.KeysPath;
                if (!string.IsNullOrWhiteSpace(path))
                {
                    management.XmlRepository = new FileSystemXmlRepository(Directory.CreateDirectory(path), loggerFactory);
                }
            });
    }
}
