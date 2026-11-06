using System.Globalization;
using System.Security.Claims;
using Schuelerfirma.Api.Domain;

namespace Schuelerfirma.Api.Application.Auth;

/// <summary>
/// Builds the identity of a logged-in user and reads it back.
/// </summary>
public static class UserPrincipal
{
    /// <summary>Authentication type of the identity; a non-empty value is what makes it count as authenticated.</summary>
    public const string AuthenticationType = "sf.session";

    /// <summary>
    /// Creates the identity with the user's id, name and role. The role comes from <see cref="User.Role"/>,
    /// which the repositories always load.
    /// </summary>
    public static ClaimsPrincipal Create(User user)
    {
        var role = user.Role?.Name ?? throw new InvalidOperationException("The user's role is not loaded.");
        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString(CultureInfo.InvariantCulture)),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, role),
            ],
            AuthenticationType);
        return new ClaimsPrincipal(identity);
    }

    /// <summary>Returns the id of the logged-in user, or <c>null</c> if the principal carries none.</summary>
    public static int? GetUserId(this ClaimsPrincipal? principal) =>
        int.TryParse(principal?.FindFirstValue(ClaimTypes.NameIdentifier), CultureInfo.InvariantCulture, out var id) ? id : null;
}
