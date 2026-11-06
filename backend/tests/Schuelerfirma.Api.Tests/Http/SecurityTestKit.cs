using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Schuelerfirma.Api.Infrastructure.Http;
using Schuelerfirma.Api.Tests.Auth;

namespace Schuelerfirma.Api.Tests.Http;

/// <summary>Endpoints for the CSRF and rate-limiting tests.</summary>
[ApiController]
[Route("test/security")]
public class SecurityProbeController : ControllerBase
{
    [AcceptVerbs("POST", "PUT", "PATCH", "DELETE")]
    [Route("protected")]
    public IActionResult Protected() => Ok();

    [AllowAnonymous]
    [HttpPost("open")]
    public IActionResult Open() => Ok();

    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    [HttpPost("no-csrf")]
    public IActionResult NoCsrf() => Ok();

    /// <summary>Stands in for the contact form (#15), which will use the same policy.</summary>
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Contact)]
    [HttpPost("contact")]
    public IActionResult Contact() => Ok();
}

/// <summary>
/// Lets a test choose the IP address a request comes from (TestServer has none) with the header
/// <see cref="Header"/>. Runs before the application's own middleware, i.e. before <c>UseForwardedHeaders</c>,
/// just like the address of the TCP connection would.
/// </summary>
public sealed class TestRemoteIpStartupFilter : IStartupFilter
{
    public const string Header = "X-Test-Remote-Ip";

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.Use(async (context, nextMiddleware) =>
        {
            if (context.Request.Headers.TryGetValue(Header, out var value))
            {
                context.Connection.RemoteIpAddress = IPAddress.Parse(value.ToString());
            }

            await nextMiddleware(context);
        });
        next(app);
    };
}

/// <summary>Test host with the real login limit (5 per minute), a contact limit of 3 and selectable client IPs.</summary>
public class RateLimitApiFactory : AuthApiFactory
{
    public const int ContactLimit = 3;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["RateLimiting:Auth:PermitLimit"] = "5",
                ["RateLimiting:Contact:PermitLimit"] = ContactLimit.ToString(),
            }));
        builder.ConfigureServices(services => services.AddSingleton<IStartupFilter, TestRemoteIpStartupFilter>());
    }
}
