using System.ComponentModel.DataAnnotations;

namespace Schuelerfirma.Api.Application.Auth;

/// <summary>
/// Account lockout after failed logins, bound from <c>Auth:Lockout</c>. Invalid values stop the application at startup.
/// </summary>
public class LoginLockoutOptions : IValidatableObject
{
    /// <summary>Configuration section these options are bound from.</summary>
    public const string SectionName = "Auth:Lockout";

    /// <summary>Number of failed attempts after which the account is locked. Default 5.</summary>
    [Range(1, 100, ErrorMessage = "Auth:Lockout:MaxFailedAttempts must be between 1 and 100.")]
    public int MaxFailedAttempts { get; set; } = 5;

    /// <summary>How long the account stays locked. Default 15 minutes.</summary>
    public TimeSpan Duration { get; set; } = TimeSpan.FromMinutes(15);

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Duration < TimeSpan.FromSeconds(1) || Duration > TimeSpan.FromDays(1))
        {
            yield return new ValidationResult("Auth:Lockout:Duration must be between 1 second and 1 day.", [nameof(Duration)]);
        }
    }
}
