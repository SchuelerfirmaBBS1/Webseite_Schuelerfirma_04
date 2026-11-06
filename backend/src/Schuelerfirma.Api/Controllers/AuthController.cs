using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Schuelerfirma.Api.Application.Abstractions;
using Schuelerfirma.Api.Application.Auth;
using Schuelerfirma.Api.Contracts.Auth;

namespace Schuelerfirma.Api.Controllers;

/// <summary>
/// Login, logout and the current user.
/// </summary>
[ApiController]
[Route("api/auth")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class AuthController(AuthService authService, IUserRepository users) : ControllerBase
{
    /// <summary>
    /// Logs in and sets the session cookie. Every failure (unknown user, wrong password, locked or deactivated
    /// account) is answered with exactly the same 401, so nothing can be learned about which accounts exist.
    /// </summary>
    /// <response code="204">Logged in; the session cookie is set.</response>
    /// <response code="401">The credentials were not accepted.</response>
    [HttpPost("login")]
    [AllowAnonymous]
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

        // LoginStartsNewSessionMiddleware has removed any session that came with this request, so this always
        // creates a session with a new key.
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, UserPrincipal.Create(result.User));
        return NoContent();
    }

    /// <summary>Ends the current session on the server and removes the cookie.</summary>
    /// <response code="204">Logged out.</response>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    /// <summary>Returns the logged-in user.</summary>
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

        return new MeResponse(user.UserId, user.Username, user.Email, user.Role.Name);
    }
}
