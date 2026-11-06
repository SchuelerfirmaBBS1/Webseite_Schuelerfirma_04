using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;

namespace Schuelerfirma.Api.Infrastructure.Http;

/// <summary>
/// Protection against cross-site request forgery: every POST, PUT, PATCH and DELETE must carry a token that only
/// JavaScript running on our own site can read.
/// </summary>
/// <remarks>
/// Two cookies work together. <see cref="CookieName"/> (HttpOnly) holds the secret half; <see cref="RequestTokenCookieName"/>
/// is readable by JavaScript, and the frontend copies its value into the <see cref="HeaderName"/> header. A foreign site
/// can make the browser send cookies, but it cannot read them, so it cannot fill in the header.
/// The token is bound to the logged-in user, so login and logout issue a new one.
/// </remarks>
public static class CsrfProtectionExtensions
{
    /// <summary>Header the frontend sends the token in (the name axios uses by default).</summary>
    public const string HeaderName = "X-XSRF-TOKEN";

    /// <summary>HttpOnly cookie with the secret half of the token pair.</summary>
    public const string CookieName = "__Host-sf.csrf";

    /// <summary>Cookie the frontend reads the token from (the name axios uses by default).</summary>
    public const string RequestTokenCookieName = "XSRF-TOKEN";

    /// <summary>Registers the antiforgery services and the global check for all controllers.</summary>
    public static IServiceCollection AddCsrfProtection(this IServiceCollection services)
    {
        services.AddAntiforgery(options =>
        {
            options.HeaderName = HeaderName;
            options.Cookie.Name = CookieName;
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.Path = "/";
            options.Cookie.IsEssential = true;
            // Framing is already forbidden by the security headers.
            options.SuppressXFrameOptionsHeader = true;
        });

        services.Configure<MvcOptions>(mvc => mvc.Filters.Add<CsrfValidationFilter>());
        return services;
    }

    /// <summary>
    /// Issues a token for <paramref name="user"/> (default: the user of the request) and puts it into the
    /// <see cref="RequestTokenCookieName"/> cookie. Pass the new user after login or logout, because the token is
    /// bound to the identity and <c>HttpContext.User</c> still describes the old one at that point.
    /// </summary>
    public static void IssueCsrfToken(this IAntiforgery antiforgery, HttpContext context, ClaimsPrincipal? user = null)
    {
        if (user is not null)
        {
            context.User = user;
        }

        var tokens = antiforgery.GetAndStoreTokens(context);
        context.Response.Cookies.Append(RequestTokenCookieName, tokens.RequestToken!, new CookieOptions
        {
            HttpOnly = false,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/",
            IsEssential = true,
        });
    }
}
