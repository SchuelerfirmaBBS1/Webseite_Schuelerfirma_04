using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Schuelerfirma.Api.Application.Abstractions;
using Schuelerfirma.Api.Application.Auth;

namespace Schuelerfirma.Api.Infrastructure.Auth;

/// <summary>
/// Cookie behavior for an API: answers 401/403 instead of redirecting to a login page, and re-checks the user
/// on every request.
/// </summary>
public sealed class SessionCookieEvents(IUserRepository users) : CookieAuthenticationEvents
{
    /// <inheritdoc />
    public override Task RedirectToLogin(RedirectContext<CookieAuthenticationOptions> context)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override Task RedirectToAccessDenied(RedirectContext<CookieAuthenticationOptions> context)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    }

    /// <summary>
    /// A session is only as good as the user behind it: if the user was deactivated or removed the session ends
    /// right now, and role changes apply immediately because the identity is rebuilt from the database.
    /// </summary>
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var userId = context.Principal.GetUserId();
        var user = userId is null ? null : await users.GetByIdAsync(userId.Value, context.HttpContext.RequestAborted);

        if (user is null || !user.IsActive)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(context.Scheme.Name);
            return;
        }

        context.ReplacePrincipal(UserPrincipal.Create(user));
    }
}
