using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.Options;

namespace Schuelerfirma.Api.Infrastructure.Auth;

/// <summary>
/// The session a login request came with. <c>AuthController</c> ends it once the login has succeeded.
/// </summary>
/// <param name="Key">Key of the session in the <see cref="SessionTicketStore"/>.</param>
public sealed record ReplacedSession(string Key);

/// <summary>
/// Makes every login create a brand-new session.
/// </summary>
/// <remarks>
/// When a request already carries a session cookie, the cookie handler does not create a new session on sign-in but
/// <i>renews the existing one</i> with the new identity. On a shared computer (the trade fair!) that would hand the
/// previous user's session key to the next user, and anyone holding a copy of the old cookie would suddenly act
/// as the new user. This middleware therefore hides the session cookie of a login request from the authentication
/// handler, which then has no choice but to start a new session with a new key.
/// The old session is not ended here but only after a successful login (see <see cref="ReplacedSession"/>): a login
/// that is refused (CSRF check, wrong password) must not log out whoever is logged in.
/// Must run before <c>UseAuthentication</c>.
/// </remarks>
public sealed class LoginStartsNewSessionMiddleware(
    RequestDelegate next,
    IOptionsMonitor<CookieAuthenticationOptions> cookieOptions,
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
            if (ReadSessionKey(cookieValue) is { } key)
            {
                context.Features.Set(new ReplacedSession(key));
            }

            RemoveSessionCookie(context);
        }

        await next(context);
    }

    private string? ReadSessionKey(string cookieValue)
    {
        try
        {
            var options = cookieOptions.Get(CookieAuthenticationDefaults.AuthenticationScheme);
            return options.TicketDataFormat.Unprotect(cookieValue)?.Principal.FindFirst(SessionIdClaim)?.Value;
        }
        catch (Exception exception)
        {
            // A forged or outdated cookie value: nothing to end. The login goes on and replaces the cookie.
            logger.LogDebug(exception, "Could not read the session cookie of a login request");
            return null;
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
