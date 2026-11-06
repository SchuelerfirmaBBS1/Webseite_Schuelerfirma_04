using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Schuelerfirma.Api.Application.Abstractions;
using Schuelerfirma.Api.Application.Auth;
using Schuelerfirma.Api.Contracts.Auth;
using Schuelerfirma.Api.Infrastructure.Auth;
using Schuelerfirma.Api.Infrastructure.Http;

namespace Schuelerfirma.Api.Controllers;

/// <summary>
/// Login, logout, the current user and the CSRF token.
/// </summary>
[ApiController]
[Route("api/auth")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class AuthController(
    AuthService authService,
    IUserRepository users,
    IAntiforgery antiforgery,
    SessionTicketStore sessions,
    IRolePermissionProvider permissions) : ControllerBase
{
    /// <summary>
    /// Sets the CSRF cookies for the current visitor (logged in or not). The frontend calls this when it starts and
    /// before every login, then sends the value of the <c>XSRF-TOKEN</c> cookie in the <c>X-XSRF-TOKEN</c> header
    /// with every POST, PUT, PATCH and DELETE.
    /// </summary>
    /// <response code="204">The cookies are set.</response>
    [HttpGet("csrf")]
    [AllowAnonymous]
    public IActionResult Csrf()
    {
        antiforgery.IssueCsrfToken(HttpContext);
        return NoContent();
    }

    /// <summary>
    /// Logs in and sets the session cookie. Every failure (unknown user, wrong password, locked or deactivated
    /// account) is answered with exactly the same 401, so nothing can be learned about which accounts exist.
    /// </summary>
    /// <response code="204">Logged in; the session cookie is set.</response>
    /// <response code="401">The credentials were not accepted.</response>
    /// <response code="429">Too many attempts from this client; see <c>Retry-After</c>.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(
            request.Username, request.Password, HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);

        if (result.User is null)
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Anmeldung fehlgeschlagen",
                detail: "Benutzername oder Passwort ist falsch.");
        }

        // LoginStartsNewSessionMiddleware has hidden the session cookie of this request, so SignInAsync always creates
        // a session with a new key. The session the browser had until now ends here, and only after a successful login.
        if (HttpContext.Features.Get<ReplacedSession>() is { } replaced)
        {
            await sessions.RemoveAsync(replaced.Key);
        }

        var principal = UserPrincipal.Create(result.User);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        antiforgery.IssueCsrfToken(HttpContext, principal);
        return NoContent();
    }

    /// <summary>Ends the current session on the server, removes the cookie and issues a token for an anonymous visitor.</summary>
    /// <response code="204">Logged out.</response>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        antiforgery.IssueCsrfToken(HttpContext, new ClaimsPrincipal(new ClaimsIdentity()));
        return NoContent();
    }

    /// <summary>Returns the logged-in user with the permissions of their role.</summary>
    /// <response code="200">The user.</response>
    /// <response code="401">Not logged in.</response>
    [HttpGet("me")]
    public async Task<ActionResult<MeResponse>> Me(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var user = userId is null ? null : await users.GetByIdAsync(userId.Value, cancellationToken);
        if (user?.Role is null)
        {
            return Unauthorized();
        }

        IReadOnlyList<string> usable = user.MustChangePassword
            ? []
            : [.. permissions.GetPermissions(user.Role.Name).Order(StringComparer.Ordinal)];
        return new MeResponse(user.UserId, user.Username, user.Email, user.Role.Name, usable, user.MustChangePassword);
    }

    /// <summary>
    /// Changes the password of the logged-in user. On success <b>all</b> sessions of the user end, including this
    /// one: the user logs in again with the new password. As long as a temporary password has not been changed,
    /// this, <c>me</c> and <c>logout</c> are the only endpoints that work for the user.
    /// </summary>
    /// <response code="204">Changed; the user is logged out everywhere and gets a new CSRF token for the next login.</response>
    /// <response code="400">Wrong current password, the account is locked, or the new password is not accepted.</response>
    /// <response code="401">Not logged in, or too many wrong attempts: the account is locked and all sessions have ended.</response>
    /// <response code="429">Too many attempts from this client; see <c>Retry-After</c>.</response>
    [HttpPost("change-password")]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await authService.ChangePasswordAsync(
            userId.Value, request.CurrentPassword, request.NewPassword,
            HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);

        switch (result.Outcome)
        {
            case ChangePasswordOutcome.WrongCurrentPassword:
                ModelState.AddModelError(nameof(request.CurrentPassword), "Das aktuelle Passwort ist falsch.");
                return ValidationProblem();

            case ChangePasswordOutcome.LockedOut:
                ModelState.AddModelError(
                    nameof(request.CurrentPassword),
                    "Nach zu vielen Fehlversuchen ist das Konto vorübergehend gesperrt. Bitte später erneut versuchen.");
                return ValidationProblem();

            case ChangePasswordOutcome.InvalidNewPassword:
                foreach (var violation in result.Violations)
                {
                    ModelState.AddModelError(nameof(request.NewPassword), violation);
                }

                return ValidationProblem();

            case ChangePasswordOutcome.LockedNow:
                await EndThisSessionAsync();
                return Problem(
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "Konto gesperrt",
                    detail: "Zu viele Fehlversuche. Das Konto ist vorübergehend gesperrt und alle Anmeldungen wurden beendet.");

            case ChangePasswordOutcome.UnknownUser:
                await EndThisSessionAsync();
                return Unauthorized();

            default:
                await EndThisSessionAsync();
                return NoContent();
        }
    }

    // The sessions are already deleted on the server; this removes the cookie and hands out an anonymous CSRF token.
    private async Task EndThisSessionAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        antiforgery.IssueCsrfToken(HttpContext, new ClaimsPrincipal(new ClaimsIdentity()));
    }
}
