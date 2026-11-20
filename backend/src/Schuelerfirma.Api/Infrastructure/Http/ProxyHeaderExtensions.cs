using System.Net;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;

namespace Schuelerfirma.Api.Infrastructure.Http;

/// <summary>
/// Configures which reverse proxies may set <c>X-Forwarded-*</c> headers.
/// </summary>
public static class ProxyHeaderExtensions
{
    /// <summary>Configuration key holding the list of trusted proxy IP addresses.</summary>
    public const string KnownProxiesKey = "ForwardedHeaders:KnownProxies";

    /// <summary>
    /// Trusts <c>X-Forwarded-For</c> and <c>X-Forwarded-Proto</c> only from the proxies listed under
    /// <c>ForwardedHeaders:KnownProxies</c>. Without that setting only loopback is trusted, so a client
    /// cannot spoof its IP address (which the rate limiting relies on) by sending the header itself.
    /// </summary>
    /// <exception cref="InvalidOperationException">An entry in the list is not a valid IP address (thrown at startup).</exception>
    public static IServiceCollection AddProxyHeaderHandling(this IServiceCollection services)
    {
        services.AddOptions<ForwardedHeadersOptions>()
            .Configure<IConfiguration>((options, configuration) =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

                var proxies = configuration.GetSection(KnownProxiesKey).Get<string[]>() ?? [];
                if (proxies.Length == 0)
                {
                    return;
                }

                // Replace the loopback-only defaults with the configured proxies.
                options.KnownProxies.Clear();
                options.KnownIPNetworks.Clear();
                foreach (var proxy in proxies)
                {
                    if (!IPAddress.TryParse(proxy, out var address))
                    {
                        throw new InvalidOperationException(
                            $"'{proxy}' in configuration '{KnownProxiesKey}' is not a valid IP address.");
                    }

                    options.KnownProxies.Add(address);
                }
            });

        return services;
    }
}
