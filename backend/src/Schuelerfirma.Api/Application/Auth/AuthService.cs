using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Schuelerfirma.Api.Application.Abstractions;
using Schuelerfirma.Api.Domain;

namespace Schuelerfirma.Api.Application.Auth;

/// <summary>
/// Checks credentials, enforces the account lockout and changes passwords. It does not create or delete the session
/// cookie; the caller does that once <see cref="LoginAsync"/> or <see cref="ChangePasswordAsync"/> succeeds.
/// </summary>
public sealed class AuthService(
    IUserRepository users,
    IUserSessionRepository sessions,
    PasswordPolicy passwordPolicy,
    IPasswordHasher<User> hasher,
    DummyPasswordHash dummyHash,
    TimeProvider timeProvider,
    IOptions<LoginLockoutOptions> lockoutOptions,
    ILogger<AuthService> logger)
{
    private readonly LoginLockoutOptions lockout = lockoutOptions.Value;

    /// <summary>
    /// Tries to log a user in.
    /// </summary>
    /// <remarks>
    /// An unknown, deactivated or locked account is not checked against the real password (so a locked account
    /// cannot be guessed at while it is locked), but a dummy hash is computed instead so all failures take
    /// equally long. After <see cref="LoginLockoutOptions.MaxFailedAttempts"/> wrong passwords the account is locked.
    /// A hash created with fewer iterations than the current setting is replaced.
    /// </remarks>
    /// <param name="username">The login name (case-insensitive).</param>
    /// <param name="password">The password to check.</param>
    /// <param name="clientIp">The client's address, only for the log.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<LoginResult> LoginAsync(
        string username, string password, string? clientIp, CancellationToken cancellationToken = default)
    {
        var user = await users.GetByUsernameAsync(username, cancellationToken);
        var now = timeProvider.GetUtcNow().UtcDateTime;

        if (user is null)
        {
            return Fail(LoginOutcome.UnknownUser, username, clientIp, password);
        }

        if (!user.IsActive)
        {
            return Fail(LoginOutcome.Inactive, username, clientIp, password);
        }

        if (user.LockoutEnd > now)
        {
            return Fail(LoginOutcome.LockedOut, username, clientIp, password);
        }

        var verification = hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (verification == PasswordVerificationResult.Failed)
        {
            await RecordFailedAttemptAsync(user, now, clientIp, cancellationToken);
            logger.LogWarning(
                "Login failed for {Username}: {Reason} (client {ClientIp})",
                LogText.Safe(username), LoginOutcome.WrongPassword, LogText.Safe(clientIp));
            return new LoginResult(LoginOutcome.WrongPassword);
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = hasher.HashPassword(user, password);
            await users.UpdateAsync(user, cancellationToken);
        }

        await users.RecordSuccessfulLoginAsync(user.UserId, now, cancellationToken);
        logger.LogInformation(
            "User {Username} logged in (client {ClientIp})", LogText.Safe(username), LogText.Safe(clientIp));
        return new LoginResult(LoginOutcome.Success, user);
    }

    /// <summary>
    /// Changes the password of a logged-in user.
    /// </summary>
    /// <remarks>
    /// The current password is checked like at the login: a wrong one counts towards the lockout. If it triggers the
    /// lockout, all sessions of the user end, because whoever guesses passwords inside a running session probably
    /// holds a stolen one. On success the new password is stored, <see cref="User.MustChangePassword"/> and the
    /// failed-login count are reset and <b>all</b> sessions of the user end, including the current one: the user logs
    /// in again with the new password and gets a new session key.
    /// </remarks>
    /// <param name="userId">The logged-in user.</param>
    /// <param name="currentPassword">The password the user has now.</param>
    /// <param name="newPassword">The new password; checked against <see cref="PasswordPolicy"/>.</param>
    /// <param name="clientIp">The client's address, only for the log.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<ChangePasswordResult> ChangePasswordAsync(
        int userId, string currentPassword, string newPassword, string? clientIp, CancellationToken cancellationToken = default)
    {
        var user = await users.GetByIdAsync(userId, cancellationToken);
        var now = timeProvider.GetUtcNow().UtcDateTime;

        if (user is null || !user.IsActive)
        {
            return new ChangePasswordResult(ChangePasswordOutcome.UnknownUser);
        }

        if (user.LockoutEnd > now)
        {
            return new ChangePasswordResult(ChangePasswordOutcome.LockedOut);
        }

        if (hasher.VerifyHashedPassword(user, user.PasswordHash, currentPassword) == PasswordVerificationResult.Failed)
        {
            logger.LogWarning(
                "Password change failed for {Username}: wrong current password (client {ClientIp})",
                LogText.Safe(user.Username), LogText.Safe(clientIp));

            if (await RecordFailedAttemptAsync(user, now, clientIp, cancellationToken))
            {
                await sessions.DeleteAllForUserAsync(user.UserId, cancellationToken: cancellationToken);
                return new ChangePasswordResult(ChangePasswordOutcome.LockedNow);
            }

            return new ChangePasswordResult(ChangePasswordOutcome.WrongCurrentPassword);
        }

        List<string> violations = [.. passwordPolicy.Validate(newPassword, user.Username).Select(v => v.Message)];
        if (string.Equals(newPassword, currentPassword, StringComparison.Ordinal))
        {
            violations.Add("Das neue Passwort muss sich vom bisherigen unterscheiden.");
        }

        if (violations.Count > 0)
        {
            return new ChangePasswordResult(ChangePasswordOutcome.InvalidNewPassword, violations);
        }

        user.PasswordHash = hasher.HashPassword(user, newPassword);
        user.MustChangePassword = false;
        await users.UpdateAsync(user, cancellationToken);
        await users.ClearLockoutAsync(user.UserId, cancellationToken);
        var ended = await sessions.DeleteAllForUserAsync(user.UserId, cancellationToken: cancellationToken);

        logger.LogInformation(
            "User {Username} changed the password; {Sessions} session(s) ended (client {ClientIp})",
            LogText.Safe(user.Username), ended, LogText.Safe(clientIp));
        return new ChangePasswordResult(ChangePasswordOutcome.Changed);
    }

    /// <summary>Counts a wrong password and locks the account at the limit.</summary>
    /// <returns>Whether this attempt locked the account.</returns>
    private async Task<bool> RecordFailedAttemptAsync(User user, DateTime now, string? clientIp, CancellationToken cancellationToken)
    {
        var failedAttempts = await users.IncrementFailedLoginCountAsync(user.UserId, cancellationToken);
        if (failedAttempts < lockout.MaxFailedAttempts)
        {
            return false;
        }

        await users.LockOutAsync(user.UserId, now + lockout.Duration, cancellationToken);
        logger.LogWarning(
            "User {Username} locked for {Duration} after {Attempts} failed attempts (client {ClientIp})",
            LogText.Safe(user.Username), lockout.Duration, failedAttempts, LogText.Safe(clientIp));
        return true;
    }

    private LoginResult Fail(LoginOutcome outcome, string username, string? clientIp, string password)
    {
        dummyHash.Verify(password);
        logger.LogWarning(
            "Login failed for {Username}: {Reason} (client {ClientIp})",
            LogText.Safe(username), outcome, LogText.Safe(clientIp));
        return new LoginResult(outcome);
    }
}
