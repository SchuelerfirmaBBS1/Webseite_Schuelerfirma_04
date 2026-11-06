using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Schuelerfirma.Api.Infrastructure.Auth;

namespace Schuelerfirma.Api.Tests.Http;

/// <summary>
/// The rules for the three API areas (#34), checked against endpoint metadata:
/// <list type="bullet">
/// <item><c>/api/public</c>: anonymous; every endpoint that is not a GET has a rate-limiting policy.</item>
/// <item><c>/api/admin</c>: never anonymous, always <c>[HasPermission]</c>.</item>
/// <item><c>/api/auth</c>: login area, no further rules.</item>
/// <item>No endpoint outside these three areas.</item>
/// </list>
/// </summary>
public static class ApiAreaRules
{
    public const string PublicArea = "/api/public";
    public const string AdminArea = "/api/admin";
    public const string AuthArea = "/api/auth";

    /// <summary>Returns one message per violation; empty if all endpoints follow the rules.</summary>
    public static IReadOnlyList<string> Check(IEnumerable<Endpoint> endpoints)
    {
        var violations = new List<string>();
        foreach (var endpoint in endpoints.OfType<RouteEndpoint>())
        {
            var path = "/" + (endpoint.RoutePattern.RawText ?? string.Empty).TrimStart('/');
            var name = $"{endpoint.DisplayName} ({path})";
            var anonymous = endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null;

            if (IsIn(path, PublicArea))
            {
                if (!anonymous)
                {
                    violations.Add($"{name}: public endpoints must be [AllowAnonymous]");
                }

                // No method metadata means the endpoint answers every method, writing ones included.
                var methods = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods ?? [];
                var writes = methods.Count == 0 || methods.Any(method => !HttpMethods.IsGet(method) && !HttpMethods.IsHead(method));
                if (writes && endpoint.Metadata.GetMetadata<EnableRateLimitingAttribute>() is null)
                {
                    violations.Add($"{name}: public endpoints that are not GET need [EnableRateLimiting]");
                }
            }
            else if (IsIn(path, AdminArea))
            {
                if (anonymous)
                {
                    violations.Add($"{name}: admin endpoints must not be [AllowAnonymous]");
                }

                if (endpoint.Metadata.GetMetadata<HasPermissionAttribute>() is null)
                {
                    violations.Add($"{name}: admin endpoints need [HasPermission]");
                }
            }
            else if (!IsIn(path, AuthArea))
            {
                violations.Add($"{name}: endpoints belong under {PublicArea}, {AdminArea} or {AuthArea}");
            }
        }

        return violations;
    }

    private static bool IsIn(string path, string area) =>
        path.Equals(area, StringComparison.OrdinalIgnoreCase) || path.StartsWith(area + "/", StringComparison.OrdinalIgnoreCase);
}
