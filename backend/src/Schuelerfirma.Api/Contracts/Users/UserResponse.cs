using Schuelerfirma.Api.Domain;

namespace Schuelerfirma.Api.Contracts.Users;

/// <summary>A user as the admin area sees it; never contains the password hash.</summary>
/// <param name="UserId">Id of the user.</param>
/// <param name="Username">Login name.</param>
/// <param name="Email">E-mail address.</param>
/// <param name="RoleId">Id of the role.</param>
/// <param name="Role">Name of the role.</param>
/// <param name="IsActive">Whether the user can log in.</param>
/// <param name="MustChangePassword">The user still has a temporary password.</param>
/// <param name="LockedUntil">End of a lockout after failed logins (UTC), or <c>null</c> if not locked.</param>
/// <param name="CreatedAt">When the account was created (UTC).</param>
/// <param name="LastLoginAt">Last successful login (UTC), or <c>null</c>.</param>
public sealed record UserResponse(
    int UserId,
    string Username,
    string Email,
    int RoleId,
    string Role,
    bool IsActive,
    bool MustChangePassword,
    DateTime? LockedUntil,
    DateTime CreatedAt,
    DateTime? LastLoginAt)
{
    /// <summary>Maps a user; <paramref name="now"/> decides whether a lockout is still running.</summary>
    public static UserResponse From(User user, DateTime now) => new(
        user.UserId,
        user.Username,
        user.Email,
        user.RoleId,
        user.Role?.Name ?? string.Empty,
        user.IsActive,
        user.MustChangePassword,
        user.LockoutEnd > now ? user.LockoutEnd : null,
        user.CreatedAt,
        user.LastLoginAt);
}

/// <summary>Answer to creating a user: the user and the one-time password to hand over.</summary>
/// <param name="User">The new user.</param>
/// <param name="TemporaryPassword">Shown only now; it has to be changed at the first login.</param>
public sealed record CreatedUserResponse(UserResponse User, string TemporaryPassword);

/// <summary>Answer to a password reset.</summary>
/// <param name="TemporaryPassword">Shown only now; it has to be changed at the next login.</param>
/// <param name="EndedSessions">Number of sessions that were ended.</param>
public sealed record PasswordResetResponse(string TemporaryPassword, int EndedSessions);

/// <summary>Answer to ending the sessions of a user.</summary>
/// <param name="EndedSessions">Number of sessions that were ended.</param>
public sealed record RevokedSessionsResponse(int EndedSessions);

/// <summary>A role with the permissions it has according to the configuration.</summary>
/// <param name="RoleId">Id of the role.</param>
/// <param name="Name">Name of the role.</param>
/// <param name="Permissions">Permissions in alphabetical order, wildcard expanded.</param>
public sealed record RoleResponse(int RoleId, string Name, IReadOnlyList<string> Permissions);
