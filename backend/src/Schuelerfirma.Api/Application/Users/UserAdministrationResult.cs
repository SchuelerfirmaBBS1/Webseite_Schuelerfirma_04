using Schuelerfirma.Api.Domain;

namespace Schuelerfirma.Api.Application.Users;

/// <summary>Why an action of <see cref="UserAdministrationService"/> did not succeed.</summary>
public enum UserAdministrationError
{
    /// <summary>The action succeeded.</summary>
    None,

    /// <summary>The user does not exist.</summary>
    NotFound,

    /// <summary>An input is invalid; see <see cref="UserAdministrationResult.Field"/> and the message.</summary>
    Invalid,

    /// <summary>The action breaks a rule (duplicate, own account, last administrator); see the message.</summary>
    Conflict,
}

/// <summary>Result of an action of <see cref="UserAdministrationService"/>.</summary>
/// <param name="Error">Whether and why the action failed.</param>
/// <param name="User">The user after the action, if it succeeded.</param>
/// <param name="TemporaryPassword">The new one-time password; only after creating a user or resetting a password.</param>
/// <param name="EndedSessions">Number of sessions that were ended.</param>
/// <param name="Field">The input the message refers to (property name of the request), if any.</param>
/// <param name="Message">German message that can be shown in the admin panel.</param>
public sealed record UserAdministrationResult(
    UserAdministrationError Error,
    User? User = null,
    string? TemporaryPassword = null,
    int EndedSessions = 0,
    string? Field = null,
    string? Message = null)
{
    /// <summary>The user does not exist.</summary>
    public static UserAdministrationResult NotFound { get; } = new(UserAdministrationError.NotFound);

    /// <summary>An input is invalid.</summary>
    public static UserAdministrationResult Invalid(string field, string message) =>
        new(UserAdministrationError.Invalid, Field: field, Message: message);

    /// <summary>A rule forbids the action.</summary>
    public static UserAdministrationResult Conflict(string message, string? field = null) =>
        new(UserAdministrationError.Conflict, Field: field, Message: message);
}
