using System.ComponentModel.DataAnnotations;

namespace Schuelerfirma.Api.Application.Auth;

/// <summary>
/// Lifetime of login sessions, bound from <c>Auth:Session</c>. Invalid values stop the application at startup.
/// </summary>
public class LoginSessionOptions : IValidatableObject
{
    /// <summary>Configuration section these options are bound from.</summary>
    public const string SectionName = "Auth:Session";

    /// <summary>A session ends after this long without activity (sliding). Default 30 minutes.</summary>
    public TimeSpan IdleTimeout { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>A session ends this long after login, no matter how active it is. Default 8 hours.</summary>
    public TimeSpan MaxLifetime { get; set; } = TimeSpan.FromHours(8);

    /// <summary>How often expired sessions are removed from storage. Default 10 minutes.</summary>
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromMinutes(10);

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (IdleTimeout < TimeSpan.FromMinutes(1) || IdleTimeout > TimeSpan.FromDays(1))
        {
            yield return new ValidationResult("Auth:Session:IdleTimeout must be between 1 minute and 1 day.", [nameof(IdleTimeout)]);
        }

        if (MaxLifetime < IdleTimeout || MaxLifetime > TimeSpan.FromDays(7))
        {
            yield return new ValidationResult(
                "Auth:Session:MaxLifetime must not be shorter than IdleTimeout and at most 7 days.", [nameof(MaxLifetime)]);
        }

        if (CleanupInterval < TimeSpan.FromSeconds(10) || CleanupInterval > TimeSpan.FromDays(1))
        {
            yield return new ValidationResult("Auth:Session:CleanupInterval must be between 10 seconds and 1 day.", [nameof(CleanupInterval)]);
        }
    }
}
