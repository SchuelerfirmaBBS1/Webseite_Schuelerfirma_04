using System.Text;
using Microsoft.Extensions.Options;

namespace Schuelerfirma.Api.Application.Auth;

/// <summary>The rules a password can violate.</summary>
public enum PasswordRule
{
    /// <summary>Shorter than the configured minimum length.</summary>
    TooShort,

    /// <summary>Longer than <see cref="PasswordPolicyOptions.MaxLength"/>.</summary>
    TooLong,

    /// <summary>Contains the user name.</summary>
    ContainsUsername,

    /// <summary>Consists of one single character, repeated.</summary>
    RepeatedCharacter,

    /// <summary>Is an ascending or descending sequence of letters or digits.</summary>
    Sequence,

    /// <summary>Is on the list of common passwords.</summary>
    CommonPassword,
}

/// <summary>A broken rule together with a German message that can be shown to the user.</summary>
public sealed record PasswordViolation(PasswordRule Rule, string Message);

/// <summary>
/// Checks new passwords against static rules, without any external service. Following NIST SP 800-63B the policy
/// asks for length instead of character classes: there is no requirement for capital letters, digits or special characters.
/// </summary>
public sealed class PasswordPolicy(IOptions<PasswordPolicyOptions> options, ICommonPasswordList commonPasswords)
{
    // Long enough for the longest sequence the policy has to recognize, written forwards. Digits and letters wrap
    // around (9 -> 0, z -> a), so "123456789012" counts as a sequence.
    private static readonly string Digits = Repeat("0123456789");
    private static readonly string Letters = Repeat("abcdefghijklmnopqrstuvwxyz");

    private readonly int minLength = options.Value.MinLength;

    /// <summary>
    /// Checks a password. Nothing is trimmed or normalized, spaces are allowed.
    /// </summary>
    /// <param name="password">The new password.</param>
    /// <param name="username">The user's login name; the password must not contain it.</param>
    /// <returns>All broken rules; empty if the password is acceptable.</returns>
    public IReadOnlyList<PasswordViolation> Validate(string? password, string? username)
    {
        password ??= string.Empty;

        // Length counts characters as users see them (code points), not UTF-16 units: an emoji is one character.
        var length = password.EnumerateRunes().Count();
        if (length > PasswordPolicyOptions.MaxLength)
        {
            // Nothing else is checked: there is no point in analysing oversized input.
            return [new(PasswordRule.TooLong, $"Das Passwort darf höchstens {PasswordPolicyOptions.MaxLength} Zeichen lang sein.")];
        }

        List<PasswordViolation> violations = [];

        if (length < minLength)
        {
            violations.Add(new(PasswordRule.TooShort, $"Das Passwort muss mindestens {minLength} Zeichen lang sein."));
        }

        if (!string.IsNullOrEmpty(username) && password.Contains(username, StringComparison.OrdinalIgnoreCase))
        {
            violations.Add(new(PasswordRule.ContainsUsername, "Das Passwort darf den Benutzernamen nicht enthalten."));
        }

        if (length > 1 && password.EnumerateRunes().Distinct().Count() == 1)
        {
            violations.Add(new(PasswordRule.RepeatedCharacter, "Das Passwort darf nicht nur aus demselben Zeichen bestehen."));
        }

        if (IsSequence(password))
        {
            violations.Add(new(PasswordRule.Sequence, "Das Passwort darf keine einfache Zeichenfolge wie „123456789012“ oder „abcdefghijkl“ sein."));
        }

        if (commonPasswords.Contains(password))
        {
            violations.Add(new(PasswordRule.CommonPassword, "Dieses Passwort wird sehr häufig verwendet und ist daher unsicher."));
        }

        return violations;
    }

    /// <summary>Whether the whole password is an ascending or descending run of letters or digits.</summary>
    private static bool IsSequence(string password)
    {
        if (password.Length < 2)
        {
            return false;
        }

        var lower = password.ToLowerInvariant();
        var reversed = string.Create(lower.Length, lower, static (span, source) =>
        {
            source.AsSpan().CopyTo(span);
            span.Reverse();
        });

        return Digits.Contains(lower, StringComparison.Ordinal)
            || Digits.Contains(reversed, StringComparison.Ordinal)
            || Letters.Contains(lower, StringComparison.Ordinal)
            || Letters.Contains(reversed, StringComparison.Ordinal);
    }

    private static string Repeat(string alphabet)
    {
        var builder = new StringBuilder();
        for (var i = 0; i < (PasswordPolicyOptions.MaxLength / alphabet.Length) + 2; i++)
        {
            builder.Append(alphabet);
        }

        return builder.ToString();
    }
}
