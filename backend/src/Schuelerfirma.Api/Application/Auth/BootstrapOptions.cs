namespace Schuelerfirma.Api.Application.Auth;

/// <summary>
/// Credentials of the first administrator, bound from <c>Bootstrap</c>. They must come from
/// <c>dotnet user-secrets</c> or environment variables, never from a file in the repository.
/// </summary>
public class BootstrapOptions
{
    /// <summary>Configuration section these options are bound from.</summary>
    public const string SectionName = "Bootstrap";

    /// <summary>User name of the first administrator.</summary>
    public string? AdminUsername { get; set; }

    /// <summary>E-mail address of the first administrator.</summary>
    public string? AdminEmail { get; set; }

    /// <summary>Initial password of the first administrator; it has to be changed at the first login.</summary>
    public string? AdminPassword { get; set; }
}
