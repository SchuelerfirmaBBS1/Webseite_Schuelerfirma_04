using System.Net.Mail;
using System.Text.RegularExpressions;
using Schuelerfirma.Api.Domain;

namespace Schuelerfirma.Api.Application.Auth;

/// <summary>
/// Format rules for user names and e-mail addresses.
/// </summary>
public static partial class AccountRules
{
    /// <summary>Shortest allowed user name.</summary>
    public const int UsernameMinLength = 3;

    /// <summary>User names consist of lower-case letters, digits, dot, underscore and hyphen.</summary>
    public static bool IsValidUsername(string? username) =>
        username is not null
        && username.Length is >= UsernameMinLength and <= User.UsernameMaxLength
        && UsernamePattern().IsMatch(username);

    /// <summary>A plain address like <c>name@example.org</c>; no display names or comments.</summary>
    public static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email) || email.Length > User.EmailMaxLength)
        {
            return false;
        }

        return MailAddress.TryCreate(email, out var address) && address.Address == email && address.Host.Contains('.');
    }

    [GeneratedRegex("^[a-z0-9._-]+$")]
    private static partial Regex UsernamePattern();
}
