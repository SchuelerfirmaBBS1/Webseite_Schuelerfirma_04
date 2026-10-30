namespace Schuelerfirma.Api.Domain;

/// <summary>
/// A person who can log in to the admin area. All timestamps are UTC.
/// </summary>
public class User
{
    /// <summary>Maximum length of <see cref="Username"/>.</summary>
    public const int UsernameMaxLength = 32;

    /// <summary>Maximum length of <see cref="Email"/>.</summary>
    public const int EmailMaxLength = 254;

    /// <summary>Maximum length of <see cref="PasswordHash"/>.</summary>
    public const int PasswordHashMaxLength = 256;

    /// <summary>Primary key.</summary>
    public int UserId { get; set; }

    /// <summary>Foreign key to <see cref="Role"/>; every user has exactly one role.</summary>
    public int RoleId { get; set; }

    /// <summary>The user's role. Always loaded by the repositories.</summary>
    public Role? Role { get; set; }

    /// <summary>Unique login name (compared case-insensitively). Never changes after creation.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>Unique e-mail address (compared case-insensitively).</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Password hash in the format of <c>PasswordHasher&lt;User&gt;</c>; never the password itself.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Inactive users cannot log in. Users are deactivated instead of deleted.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Failed login attempts since the last success or lockout. Changed only through the atomic repository methods.</summary>
    public int FailedLoginCount { get; set; }

    /// <summary>The account is locked until this moment, or not locked when <c>null</c> or in the past.</summary>
    public DateTime? LockoutEnd { get; set; }

    /// <summary>The user must choose a new password before doing anything else (temporary password).</summary>
    public bool MustChangePassword { get; set; }

    /// <summary>When the account was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>When the user last logged in successfully, or <c>null</c> if never.</summary>
    public DateTime? LastLoginAt { get; set; }
}
