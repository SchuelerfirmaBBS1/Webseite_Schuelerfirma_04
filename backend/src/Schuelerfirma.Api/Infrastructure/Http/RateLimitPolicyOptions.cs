using System.ComponentModel.DataAnnotations;

namespace Schuelerfirma.Api.Infrastructure.Http;

/// <summary>
/// How many requests one client may send to the endpoints of a rate-limiting policy within a time window.
/// Bound per policy from <c>RateLimiting:&lt;Policy&gt;</c>, e.g. <c>RateLimiting:Auth</c>.
/// </summary>
public class RateLimitPolicyOptions : IValidatableObject
{
    /// <summary>Configuration section that holds one subsection per policy.</summary>
    public const string SectionName = "RateLimiting";

    /// <summary>Requests allowed per client and window.</summary>
    [Range(1, 100_000)]
    public int PermitLimit { get; set; }

    /// <summary>Length of the window; the counter starts again after it.</summary>
    public TimeSpan Window { get; set; }

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Window < TimeSpan.FromSeconds(1) || Window > TimeSpan.FromDays(1))
        {
            yield return new ValidationResult("Window must be between one second and one day.", [nameof(Window)]);
        }
    }
}
