namespace Schuelerfirma.Api.Infrastructure.Http;

/// <summary>
/// Adds baseline security headers to every response of the API.
/// </summary>
public static class SecurityHeadersExtensions
{
    /// <summary>
    /// Sets <c>X-Content-Type-Options</c>, <c>Content-Security-Policy</c> (<c>frame-ancestors 'none'</c>)
    /// and <c>Referrer-Policy</c> on every response, including error responses.
    /// Register this as the first middleware.
    /// </summary>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            // OnStarting instead of setting the headers up front: the exception handler
            // clears the response headers before it writes the error response.
            context.Response.OnStarting(() =>
            {
                var headers = context.Response.Headers;
                headers["X-Content-Type-Options"] = "nosniff";
                headers["Content-Security-Policy"] = "frame-ancestors 'none'";
                headers["Referrer-Policy"] = "no-referrer";
                return Task.CompletedTask;
            });

            await next();
        });
    }
}
