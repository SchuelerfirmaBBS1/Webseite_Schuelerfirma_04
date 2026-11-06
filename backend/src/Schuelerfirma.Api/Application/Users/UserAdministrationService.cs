using Microsoft.AspNetCore.Identity;
using Schuelerfirma.Api.Application.Abstractions;
using Schuelerfirma.Api.Application.Auth;
using Schuelerfirma.Api.Domain;

namespace Schuelerfirma.Api.Application.Users;

/// <summary>
/// What admins do with user accounts: create, change, reset passwords, end sessions. Users are never deleted, only
/// deactivated, so it stays traceable who did what.
/// </summary>
/// <remarks>
/// Protection rules: nobody can deactivate their own account, change their own role or reset their own password here
/// (that would bypass the current password; the own password is changed with <c>/api/auth/change-password</c>), and
/// the last active user who may manage users is kept.
/// </remarks>
public sealed class UserAdministrationService(
    IUserRepository users,
    IRoleRepository roles,
    IUserSessionRepository sessions,
    IRolePermissionProvider permissions,
    ITemporaryPasswordGenerator temporaryPasswords,
    IPasswordHasher<User> hasher,
    TimeProvider timeProvider,
    ILogger<UserAdministrationService> logger)
{
    /// <summary>Returns all users ordered by user name.</summary>
    public Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default) =>
        users.GetAllAsync(cancellationToken);

    /// <summary>Returns the user, or <c>null</c>.</summary>
    public Task<User?> GetAsync(int userId, CancellationToken cancellationToken = default) =>
        users.GetByIdAsync(userId, cancellationToken);

    /// <summary>
    /// Creates an active user with a temporary password that has to be changed at the first login.
    /// </summary>
    /// <param name="actingUserId">The admin who does this (for the log).</param>
    /// <param name="username">3–32 characters out of <c>a-z 0-9 . _ -</c>; unique.</param>
    /// <param name="email">A valid, unique e-mail address.</param>
    /// <param name="roleId">An existing role.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<UserAdministrationResult> CreateAsync(
        int actingUserId, string username, string email, int roleId, CancellationToken cancellationToken = default)
    {
        if (!AccountRules.IsValidUsername(username))
        {
            return UserAdministrationResult.Invalid(
                "Username",
                $"Der Benutzername muss {AccountRules.UsernameMinLength} bis {User.UsernameMaxLength} Zeichen lang sein "
                + "und darf nur Kleinbuchstaben, Ziffern, Punkt, Unterstrich und Bindestrich enthalten.");
        }

        if (Validate(email, await roles.GetByIdAsync(roleId, cancellationToken)) is { } invalid)
        {
            return invalid;
        }

        if (await users.ExistsByUsernameAsync(username, cancellationToken))
        {
            return Duplicate(nameof(User.Username));
        }

        if (await users.ExistsByEmailAsync(email, cancellationToken))
        {
            return Duplicate(nameof(User.Email));
        }

        var user = new User
        {
            Username = username,
            Email = email,
            RoleId = roleId,
            IsActive = true,
            MustChangePassword = true,
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
        };
        var temporaryPassword = temporaryPasswords.Generate(username);
        user.PasswordHash = hasher.HashPassword(user, temporaryPassword);

        try
        {
            await users.AddAsync(user, cancellationToken);
        }
        catch (DuplicateEntryException exception)
        {
            // Another request took the name or address between the check and the insert.
            return Duplicate(exception.Field);
        }

        var created = (await users.GetByIdAsync(user.UserId, cancellationToken))!;
        logger.LogInformation(
            "User {ActorId} created user {Username} (id {UserId}) with role {Role}",
            actingUserId, LogText.Safe(created.Username), created.UserId, created.Role?.Name);
        return new UserAdministrationResult(UserAdministrationError.None, created, temporaryPassword);
    }

    /// <summary>
    /// Changes e-mail address, role and active state. A role change or a deactivation ends all sessions of the user.
    /// </summary>
    public async Task<UserAdministrationResult> UpdateAsync(
        int actingUserId, int userId, string email, int roleId, bool isActive, CancellationToken cancellationToken = default)
    {
        var user = await users.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return UserAdministrationResult.NotFound;
        }

        var role = await roles.GetByIdAsync(roleId, cancellationToken);
        if (Validate(email, role) is { } invalid)
        {
            return invalid;
        }

        var roleChanged = user.RoleId != roleId;
        var deactivated = user.IsActive && !isActive;

        if (userId == actingUserId && deactivated)
        {
            return UserAdministrationResult.Conflict("Das eigene Konto kann nicht deaktiviert werden.", "IsActive");
        }

        if (userId == actingUserId && roleChanged)
        {
            return UserAdministrationResult.Conflict("Die eigene Rolle kann nicht geändert werden.", "RoleId");
        }

        if ((roleChanged || deactivated)
            && CanManageUsers(user.Role!) && user.IsActive
            && !(isActive && CanManageUsers(role!))
            && await CountActiveUserManagersAsync(cancellationToken) <= 1)
        {
            return UserAdministrationResult.Conflict(
                "Das ist der letzte aktive Benutzer, der Benutzer verwalten darf. Er muss erhalten bleiben.");
        }

        if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase)
            && await users.ExistsByEmailAsync(email, cancellationToken))
        {
            return Duplicate(nameof(User.Email));
        }

        var previous = (user.Email, Role: user.Role?.Name, user.IsActive);
        user.Email = email;
        user.RoleId = roleId;
        user.IsActive = isActive;

        try
        {
            await users.UpdateAsync(user, cancellationToken);
        }
        catch (DuplicateEntryException exception)
        {
            return Duplicate(exception.Field);
        }

        var ended = roleChanged || deactivated
            ? await sessions.DeleteAllForUserAsync(userId, cancellationToken: cancellationToken)
            : 0;

        logger.LogInformation(
            "User {ActorId} changed user {Username} (id {UserId}): e-mail {EmailChanged}, role {OldRole} -> {NewRole}, "
            + "active {OldActive} -> {NewActive}; {Sessions} session(s) ended",
            actingUserId, LogText.Safe(user.Username), userId,
            previous.Email == email ? "unchanged" : "changed",
            previous.Role, role!.Name, previous.IsActive, isActive, ended);

        return new UserAdministrationResult(
            UserAdministrationError.None, await users.GetByIdAsync(userId, cancellationToken), EndedSessions: ended);
    }

    /// <summary>
    /// Gives the user a new temporary password, lifts a lockout and ends all sessions. Not for the own account.
    /// </summary>
    public async Task<UserAdministrationResult> ResetPasswordAsync(
        int actingUserId, int userId, CancellationToken cancellationToken = default)
    {
        var user = await users.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return UserAdministrationResult.NotFound;
        }

        if (userId == actingUserId)
        {
            return UserAdministrationResult.Conflict(
                "Das eigene Passwort wird über „Passwort ändern“ geändert, nicht über das Zurücksetzen.");
        }

        var temporaryPassword = temporaryPasswords.Generate(user.Username);
        user.PasswordHash = hasher.HashPassword(user, temporaryPassword);
        user.MustChangePassword = true;
        await users.UpdateAsync(user, cancellationToken);
        await users.ClearLockoutAsync(userId, cancellationToken);
        var ended = await sessions.DeleteAllForUserAsync(userId, cancellationToken: cancellationToken);

        logger.LogInformation(
            "User {ActorId} reset the password of {Username} (id {UserId}); {Sessions} session(s) ended",
            actingUserId, LogText.Safe(user.Username), userId, ended);
        return new UserAdministrationResult(
            UserAdministrationError.None, await users.GetByIdAsync(userId, cancellationToken), temporaryPassword, ended);
    }

    /// <summary>Ends all sessions of the user, e.g. when someone stayed logged in on the trade-fair laptop.</summary>
    public async Task<UserAdministrationResult> RevokeSessionsAsync(
        int actingUserId, int userId, CancellationToken cancellationToken = default)
    {
        var user = await users.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return UserAdministrationResult.NotFound;
        }

        var ended = await sessions.DeleteAllForUserAsync(userId, cancellationToken: cancellationToken);
        logger.LogInformation(
            "User {ActorId} ended {Sessions} session(s) of {Username} (id {UserId})",
            actingUserId, ended, LogText.Safe(user.Username), userId);
        return new UserAdministrationResult(UserAdministrationError.None, user, EndedSessions: ended);
    }

    private static UserAdministrationResult? Validate(string email, Role? role)
    {
        if (!AccountRules.IsValidEmail(email))
        {
            return UserAdministrationResult.Invalid("Email", "Die E-Mail-Adresse ist ungültig.");
        }

        return role is null ? UserAdministrationResult.Invalid("RoleId", "Diese Rolle gibt es nicht.") : null;
    }

    private static UserAdministrationResult Duplicate(string field) => UserAdministrationResult.Conflict(
        field == nameof(User.Username)
            ? "Dieser Benutzername ist bereits vergeben."
            : "Diese E-Mail-Adresse wird bereits von einem anderen Konto verwendet.",
        field);

    private bool CanManageUsers(Role role) => permissions.HasPermission(role.Name, Permissions.UsersManage);

    /// <remarks>
    /// Check and change are two steps: two admins deactivating each other at the same moment could both pass the
    /// check. With EF Core (#22) the check and the update should run in one transaction.
    /// </remarks>
    private async Task<int> CountActiveUserManagersAsync(CancellationToken cancellationToken)
    {
        var managerRoles = (await roles.GetAllAsync(cancellationToken)).Where(CanManageUsers).Select(r => r.RoleId).ToList();
        return managerRoles.Count == 0 ? 0 : await users.CountActiveInRolesAsync(managerRoles, cancellationToken);
    }
}
