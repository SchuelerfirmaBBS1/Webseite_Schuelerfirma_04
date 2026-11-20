namespace Schuelerfirma.Api.Application.Auth;

/// <summary>How an attempt to change the password ended.</summary>
public enum ChangePasswordOutcome
{
    /// <summary>The password was changed; all sessions of the user have ended.</summary>
    Changed,

    /// <summary>The user no longer exists or is deactivated.</summary>
    UnknownUser,

    /// <summary>The account was already locked; nothing was checked.</summary>
    LockedOut,

    /// <summary>The current password was wrong; the attempt counts towards the lockout.</summary>
    WrongCurrentPassword,

    /// <summary>The wrong current password triggered the lockout; all sessions of the user have ended.</summary>
    LockedNow,

    /// <summary>The new password breaks the policy or equals the current one; see <see cref="ChangePasswordResult.Violations"/>.</summary>
    InvalidNewPassword,
}

/// <summary>Result of <see cref="AuthService.ChangePasswordAsync"/>.</summary>
/// <param name="Outcome">How the attempt ended.</param>
/// <param name="Violations">German messages for the new password; only for <see cref="ChangePasswordOutcome.InvalidNewPassword"/>.</param>
public sealed record ChangePasswordResult(ChangePasswordOutcome Outcome, IReadOnlyList<string> Violations)
{
    /// <summary>Creates a result without messages.</summary>
    public ChangePasswordResult(ChangePasswordOutcome outcome)
        : this(outcome, [])
    {
    }
}
