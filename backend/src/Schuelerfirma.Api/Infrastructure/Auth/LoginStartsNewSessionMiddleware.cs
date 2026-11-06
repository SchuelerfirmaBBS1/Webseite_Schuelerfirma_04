using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.Options;
using Schuelerfirma.Api.Application.Auth;

namespace Schuelerfirma.Api.Infrastructure.Auth;

/// <summary>
/// Makes every login create a brand-new session.
/// </summary>
/// <remarks>
/// When a request already carries a session cookie, the cookie handler does not create a new session on sign-in but
/// <i>renews the existing one</i> with the new identity. On a shared computer (the trade fair!) that would hand the
/// previous user's session key to the next user, and anyone holding a copy of the old cookie would suddenly act
/// as the new user. This middleware therefore ends the session of a login request and hides its cookie from the
/// authentication handler, which then has no choice but to start a new session with a new key.
/// Must run before <c>UseAuthentication</c>.
/// </remarks>
public sealed class LoginStartsNewSessionMiddleware(
    RequestDelegate next,
    IOptionsMonitor<CookieAuthenticationOptions> cookieOptions,
    SessionTicketStore sessions,
    ILogger<LoginStartsNewSessionMiddleware> logger)
{
    /// <summary>The path of the login endpoint (<c>AuthController</c>).</summary>
    public const string LoginPath = "/api/auth/login";

    // The claim the cookie handler puts into the cookie to point at the server-side session.
    private const string SessionIdClaim = "Microsoft.AspNetCore.Authentication.Cookies-SessionId";

    /// <summary>Processes a request.</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        if (HttpMethods.IsPost(context.Request.Method)
            && context.Request.Path.Equals(LoginPath, StringComparison.OrdinalIgnoreCase)
            && context.Request.Cookies.TryGetValue(AuthenticationExtensions.CookieName, out var cookieValue))
        {
            await EndSessionAsync(cookieValue);
            RemoveSessionCookie(context);
        }

        await next(context);
    }

    private async Task EndSessionAsync(string cookieValue)
    {
        try
        {
            var options = cookieOptions.Get(CookieAuthenticationDefaults.AuthenticationScheme);
            var key = options.TicketDataFormat.Unprotect(cookieValue)?.Principal.FindFirst(SessionIdClaim)?.Value;
            if (key is not null)
            {
                await sessions.RemoveAsync(key);
            }
        }
        catch (Exception exception)
        {
            // A forged or outdated cookie value: nothing to end. The login goes on and replaces the cookie.
            logger.LogDebug(exception, "Could not read the session cookie of a login request");
        }
    }

    private static void RemoveSessionCookie(HttpContext context)
    {
        var remaining = context.Request.Cookies
            .Where(cookie => cookie.Key != AuthenticationExtensions.CookieName)
            .Select(cookie => $"{cookie.Key}={cookie.Value}");
        context.Request.Headers.Cookie = new StringValues(string.Join("; ", remaining));
    }
}
