using System.ComponentModel.DataAnnotations;

namespace Schuelerfirma.Api.Application.Auth;

/// <summary>
/// Settings of the password policy, bound from <c>Auth:Password</c>. Invalid values stop the application at startup.
/// </summary>
public class PasswordPolicyOptions
{
    /// <summary>Configuration section these options are bound from.</summary>
    public const string SectionName = "Auth:Password";

    /// <summary>The configured minimum length can never be lower than this.</summary>
    public const int AbsoluteMinLength = 12;

    /// <summary>Maximum length of a password; keeps hashing and checking cheap for oversized input.</summary>
    public const int MaxLength = 128;

    /// <summary>Minimum length of a password, between <see cref="AbsoluteMinLength"/> and <see cref="MaxLength"/>.</summary>
    [Range(AbsoluteMinLength, MaxLength, ErrorMessage = "Auth:Password:MinLength must be between 12 and 128.")]
    public int MinLength { get; set; } = AbsoluteMinLength;
}
