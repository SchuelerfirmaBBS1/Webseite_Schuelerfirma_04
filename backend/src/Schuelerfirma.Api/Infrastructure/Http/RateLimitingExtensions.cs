using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace Schuelerfirma.Api.Infrastructure.Http;

/// <summary>
/// Names of the rate-limiting policies, for <c>[EnableRateLimiting(...)]</c>.
/// </summary>
public static class RateLimitPolicies
{
    /// <summary>Login and other password checks: few requests per client, against password guessing.</summary>
    public const string Auth = "auth";

    /// <summary>
    /// Public forms such as the contact form. Generous on purpose: at the trade fair many visitors share one IP.
    /// </summary>
    public const string Contact = "contact";
}

/// <summary>
/// Limits how many requests one client (identified by its IP address) may send to selected endpoints.
/// </summary>
public static class RateLimitingExtensions
{
    // Defaults; each value can be overridden under RateLimiting:<Policy>.
    private static readonly Dictionary<string, (string Section, int PermitLimit, TimeSpan Window)> Policies = new()
    {
        [RateLimitPolicies.Auth] = ("Auth", 5, TimeSpan.FromMinutes(1)),
        [RateLimitPolicies.Contact] = ("Contact", 10, TimeSpan.FromMinutes(10)),
    };

    /// <summary>
    /// Registers the policies from <see cref="RateLimitPolicies"/>. Rejected requests get a 429 ProblemDetails with
    /// <c>Retry-After</c>. The middleware is added with <c>UseRateLimiter</c>, after <c>UseForwardedHeaders</c> so
    /// the real client IP behind the reverse proxy is used.
    /// </summary>
    public static IServiceCollection AddClientRateLimiting(this IServiceCollection services)
    {
        foreach (var (name, (section, permitLimit, window)) in Policies)
        {
            services.AddOptions<RateLimitPolicyOptions>(name)
                .Configure(options =>
                {
                    options.PermitLimit = permitLimit;
                    options.Window = window;
                })
                .BindConfiguration($"{RateLimitPolicyOptions.SectionName}:{section}")
                .ValidateDataAnnotations()
                .ValidateOnStart();
        }

        services.AddRateLimiter(options =>
        {
            foreach (var name in Policies.Keys)
            {
                options.AddPolicy(name, context =>
                {
                    var settings = context.RequestServices.GetRequiredService<IOptionsMonitor<RateLimitPolicyOptions>>().Get(name);
                    return RateLimitPartition.GetFixedWindowLimiter(
                        ClientPartitionKey(context.Connection.RemoteIpAddress),
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = settings.PermitLimit,
                            Window = settings.Window,
                            QueueLimit = 0,
                        });
                });
            }

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = OnRejectedAsync;
        });

        return services;
    }

    /// <summary>
    /// The key requests are counted under. IPv4 addresses count individually. IPv6 addresses count per /64 network,
    /// because a single connection usually owns a whole /64 and could otherwise switch addresses to escape the limit.
    /// </summary>
    public static string ClientPartitionKey(IPAddress? address)
    {
        if (address is null)
        {
            return "unknown";
        }

        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (address.AddressFamily != AddressFamily.InterNetworkV6)
        {
            return address.ToString();
        }

        var bytes = address.GetAddressBytes();
        Array.Clear(bytes, 8, 8);
        return new IPAddress(bytes) + "/64";
    }

    private static async ValueTask OnRejectedAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var http = context.HttpContext;
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            http.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
        }

        // Only the policy name and the client: the path may contain anything the client sent.
        var policy = http.GetEndpoint()?.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName;
        http.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(RateLimitingExtensions).FullName!)
            .LogWarning("Rate limit '{Policy}' exceeded by {Client}", policy, ClientPartitionKey(http.Connection.RemoteIpAddress));

        await http.RequestServices.GetRequiredService<IProblemDetailsService>().WriteAsync(new ProblemDetailsContext
        {
            HttpContext = http,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Zu viele Anfragen",
                Detail = "Bitte einen Moment warten und es dann erneut versuchen.",
            },
        });
    }
}
