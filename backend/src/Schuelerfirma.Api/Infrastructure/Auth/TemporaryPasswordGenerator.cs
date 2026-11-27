using System.Security.Cryptography;
using Schuelerfirma.Api.Application.Auth;

namespace Schuelerfirma.Api.Infrastructure.Auth;

/// <summary>
/// Generates random one-time passwords with a cryptographic random number generator. Every password contains at
/// least one upper-case letter, one lower-case letter, one digit and one special character, so it satisfies the
/// character classes of the password policy. The alphabet leaves out characters that are easily mixed up when a
/// password is read aloud or copied by hand (<c>0 O 1 l I</c>) and uses only special characters that are easy to type.
/// </summary>
public sealed class TemporaryPasswordGenerator(PasswordPolicy policy) : ITemporaryPasswordGenerator
{
    /// <summary>Number of characters of a generated password.</summary>
    public const int Length = 16;

    /// <summary>Upper-case letters a generated password may contain.</summary>
    public const string Uppercase = "ABCDEFGHJKLMNPQRSTUVWXYZ";

    /// <summary>Lower-case letters a generated password may contain.</summary>
    public const string Lowercase = "abcdefghijkmnopqrstuvwxyz";

    /// <summary>Digits a generated password may contain.</summary>
    public const string Digits = "23456789";

    /// <summary>Special characters a generated password may contain.</summary>
    public const string Specials = "!#%+-=?";

    /// <summary>All characters a generated password consists of.</summary>
    public const string Alphabet = Uppercase + Lowercase + Digits + Specials;

    // One character of each class is placed first; the shuffle afterwards hides where they are.
    private static readonly string[] RequiredClasses = [Uppercase, Lowercase, Digits, Specials];

    // A random password can by chance violate the policy for a particular user name (for example when the user
    // name is very short); in that case another one is drawn.
    private const int MaxAttempts = 100;

    /// <inheritdoc />
    public string Generate(string username)
    {
        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            var password = Draw();
            if (policy.Validate(password, username).Count == 0)
            {
                return password;
            }
        }

        throw new InvalidOperationException("Could not generate a temporary password that satisfies the password policy.");
    }

    private static string Draw()
    {
        var characters = new char[Length];
        for (var i = 0; i < characters.Length; i++)
        {
            var source = i < RequiredClasses.Length ? RequiredClasses[i] : Alphabet;

            // GetInt32 avoids the bias of a plain modulo operation.
            characters[i] = source[RandomNumberGenerator.GetInt32(source.Length)];
        }

        RandomNumberGenerator.Shuffle(characters.AsSpan());
        return new string(characters);
    }
}
