using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Schuelerfirma.Api.Application.Abstractions;
using Schuelerfirma.Api.Domain;

namespace Schuelerfirma.Api.Application.Auth;

/// <summary>
/// Checks credentials and enforces the account lockout. It does not create the session; the caller does that
/// once <see cref="LoginAsync"/> succeeds.
/// </summary>
public sealed class AuthService(
    IUserRepository users,
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
            var failedAttempts = await users.IncrementFailedLoginCountAsync(user.UserId, cancellationToken);
            if (failedAttempts >= lockout.MaxFailedAttempts)
            {
                await users.LockOutAsync(user.UserId, now + lockout.Duration, cancellationToken);
                logger.LogWarning(
                    "User {Username} locked for {Duration} after {Attempts} failed logins (client {ClientIp})",
                    LogText.Safe(username), lockout.Duration, failedAttempts, LogText.Safe(clientIp));
            }

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

    private LoginResult Fail(LoginOutcome outcome, string username, string? clientIp, string password)
    {
        dummyHash.Verify(password);
        logger.LogWarning(
            "Login failed for {Username}: {Reason} (client {ClientIp})",
            LogText.Safe(username), outcome, LogText.Safe(clientIp));
        return new LoginResult(outcome);
    }
}
