using Microsoft.AspNetCore.Mvc;
using Schuelerfirma.Api.Application.Auth;
using Schuelerfirma.Api.Application.Users;
using Schuelerfirma.Api.Contracts.Users;
using Schuelerfirma.Api.Infrastructure.Auth;

namespace Schuelerfirma.Api.Controllers;

/// <summary>
/// User management for admins (basis for the admin panel, #11). Users are deactivated, never deleted. Answers with
/// temporary passwords are not cached (see <see cref="AdminApiController"/>).
/// </summary>
[Route("api/admin/users")]
[HasPermission(Permissions.UsersManage)]
public class UsersController(UserAdministrationService administration, TimeProvider timeProvider) : AdminApiController
{
    private DateTime Now => timeProvider.GetUtcNow().UtcDateTime;

    // The fallback policy and [HasPermission] guarantee a logged-in user here.
    private int ActingUserId => User.GetUserId()!.Value;

    /// <summary>Returns all users ordered by user name.</summary>
    /// <response code="200">The users.</response>
    [HttpGet]
    public async Task<IReadOnlyList<UserResponse>> GetAll(CancellationToken cancellationToken) =>
        [.. (await administration.GetAllAsync(cancellationToken)).Select(user => UserResponse.From(user, Now))];

    /// <summary>Returns one user.</summary>
    /// <response code="200">The user.</response>
    /// <response code="404">There is no such user.</response>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserResponse>> Get(int id, CancellationToken cancellationToken) =>
        await administration.GetAsync(id, cancellationToken) is { } user ? UserResponse.From(user, Now) : NotFound();

    /// <summary>Creates a user with a temporary password that has to be changed at the first login.</summary>
    /// <response code="201">Created; the answer contains the temporary password, which is shown only this once.</response>
    /// <response code="400">An input is invalid.</response>
    /// <response code="409">User name or e-mail address is already taken.</response>
    [HttpPost]
    public async Task<IActionResult> Create(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var result = await administration.CreateAsync(
            ActingUserId, request.Username, request.Email, request.RoleId!.Value, cancellationToken);
        if (result.Error != UserAdministrationError.None)
        {
            return Failure(result);
        }

        var user = UserResponse.From(result.User!, Now);
        return CreatedAtAction(nameof(Get), new { id = user.UserId }, new CreatedUserResponse(user, result.TemporaryPassword!));
    }

    /// <summary>
    /// Changes e-mail address, role and active state. A role change or a deactivation ends the user's sessions.
    /// </summary>
    /// <response code="200">The changed user.</response>
    /// <response code="400">An input is invalid.</response>
    /// <response code="404">There is no such user.</response>
    /// <response code="409">E-mail address taken, own account or role, or the last user who may manage users.</response>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var result = await administration.UpdateAsync(
            ActingUserId, id, request.Email, request.RoleId!.Value, request.IsActive!.Value, cancellationToken);
        return result.Error == UserAdministrationError.None ? Ok(UserResponse.From(result.User!, Now)) : Failure(result);
    }

    /// <summary>Gives the user a new temporary password, lifts a lockout and ends all sessions of the user.</summary>
    /// <response code="200">The temporary password, shown only this once.</response>
    /// <response code="404">There is no such user.</response>
    /// <response code="409">The own password is changed with <c>POST /api/auth/change-password</c>.</response>
    [HttpPost("{id:int}/reset-password")]
    public async Task<IActionResult> ResetPassword(int id, CancellationToken cancellationToken)
    {
        var result = await administration.ResetPasswordAsync(ActingUserId, id, cancellationToken);
        return result.Error == UserAdministrationError.None
            ? Ok(new PasswordResetResponse(result.TemporaryPassword!, result.EndedSessions))
            : Failure(result);
    }

    /// <summary>Ends all sessions of the user, e.g. after somebody stayed logged in on the trade-fair laptop.</summary>
    /// <response code="200">The number of ended sessions.</response>
    /// <response code="404">There is no such user.</response>
    [HttpPost("{id:int}/revoke-sessions")]
    public async Task<IActionResult> RevokeSessions(int id, CancellationToken cancellationToken)
    {
        var result = await administration.RevokeSessionsAsync(ActingUserId, id, cancellationToken);
        return result.Error == UserAdministrationError.None ? Ok(new RevokedSessionsResponse(result.EndedSessions)) : Failure(result);
    }

    private IActionResult Failure(UserAdministrationResult result)
    {
        switch (result.Error)
        {
            case UserAdministrationError.NotFound:
                return NotFound();

            case UserAdministrationError.Invalid:
                ModelState.AddModelError(result.Field!, result.Message!);
                return ValidationProblem();

            default:
                return Problem(statusCode: StatusCodes.Status409Conflict, title: "Nicht möglich", detail: result.Message);
        }
    }
}
