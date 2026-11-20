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

    /// <summary>Contains no upper-case letter.</summary>
    MissingUppercase,

    /// <summary>Contains no lower-case letter.</summary>
    MissingLowercase,

    /// <summary>Contains no digit.</summary>
    MissingDigit,

    /// <summary>Contains no special character (neither letter, digit nor white space).</summary>
    MissingSpecialCharacter,
}

/// <summary>A broken rule together with a German message that can be shown to the user.</summary>
public sealed record PasswordViolation(PasswordRule Rule, string Message);

/// <summary>
/// Checks new passwords against static rules, without any external service: a minimum length, no common or trivial
/// passwords, and, unless switched off in <see cref="PasswordPolicyOptions"/>, at least one upper-case letter, one
/// lower-case letter, one digit and one special character.
/// </summary>
/// <remarks>
/// Length and the list of common passwords do most of the protecting (NIST SP 800-63B). The character classes are
/// required anyway because team and customer expect them from a secure system; they are configurable so they can be
/// relaxed without a code change.
/// </remarks>
public sealed class PasswordPolicy(IOptions<PasswordPolicyOptions> options, ICommonPasswordList commonPasswords)
{
    // Long enough for the longest sequence the policy has to recognize, written forwards. Digits and letters wrap
    // around (9 -> 0, z -> a), so "123456789012" counts as a sequence.
    private static readonly string Digits = Repeat("0123456789");
    private static readonly string Letters = Repeat("abcdefghijklmnopqrstuvwxyz");

    private readonly PasswordPolicyOptions settings = options.Value;

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

        if (length < settings.MinLength)
        {
            violations.Add(new(PasswordRule.TooShort, $"Das Passwort muss mindestens {settings.MinLength} Zeichen lang sein."));
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

        var runes = password.EnumerateRunes().ToList();
        if (settings.RequireUppercase && !runes.Exists(Rune.IsUpper))
        {
            violations.Add(new(PasswordRule.MissingUppercase, "Das Passwort muss mindestens einen Großbuchstaben enthalten."));
        }

        if (settings.RequireLowercase && !runes.Exists(Rune.IsLower))
        {
            violations.Add(new(PasswordRule.MissingLowercase, "Das Passwort muss mindestens einen Kleinbuchstaben enthalten."));
        }

        if (settings.RequireDigit && !runes.Exists(Rune.IsDigit))
        {
            violations.Add(new(PasswordRule.MissingDigit, "Das Passwort muss mindestens eine Ziffer enthalten."));
        }

        if (settings.RequireSpecialCharacter && !runes.Exists(IsSpecialCharacter))
        {
            violations.Add(new(
                PasswordRule.MissingSpecialCharacter,
                "Das Passwort muss mindestens ein Sonderzeichen enthalten (z. B. ! ? # - _ .). Ein Leerzeichen zählt nicht."));
        }

        return violations;
    }

    /// <summary>
    /// Whether <paramref name="character"/> counts as a special character: neither a letter, a digit nor white space.
    /// White space does not count, so a plain sentence with a number does not satisfy the rule.
    /// </summary>
    public static bool IsSpecialCharacter(Rune character) =>
        !Rune.IsLetterOrDigit(character) && !Rune.IsWhiteSpace(character);

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
