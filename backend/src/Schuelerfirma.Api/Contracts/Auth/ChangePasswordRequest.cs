using System.ComponentModel.DataAnnotations;

namespace Schuelerfirma.Api.Contracts.Auth;

/// <summary>Body of <c>POST /api/auth/change-password</c>.</summary>
/// <param name="CurrentPassword">The password the user has now.</param>
/// <param name="NewPassword">The new password; the password policy decides the exact limits.</param>
public sealed record ChangePasswordRequest(
    [Required, StringLength(1024)] string CurrentPassword,
    [Required, StringLength(1024)] string NewPassword);
