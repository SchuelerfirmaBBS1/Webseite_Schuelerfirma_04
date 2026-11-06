using System.Security.Cryptography;
using Schuelerfirma.Api.Application.Auth;

namespace Schuelerfirma.Api.Infrastructure.Auth;

/// <summary>
/// Generates random one-time passwords with a cryptographic random number generator. The alphabet leaves out
/// characters that are easily mixed up when a password is read aloud or copied by hand: <c>0 O 1 l I</c>.
/// </summary>
public sealed class TemporaryPasswordGenerator(PasswordPolicy policy) : ITemporaryPasswordGenerator
{
    /// <summary>Number of characters of a generated password.</summary>
    public const int Length = 16;

    /// <summary>The characters a generated password consists of.</summary>
    public const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";

    // A random password can by chance violate the policy for a particular user name (for example when the user
    // name is very short); in that case another one is drawn.
    private const int MaxAttempts = 100;

    /// <inheritdoc />
    public string Generate(string username)
    {
        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            var password = string.Create(Length, 0, static (span, _) =>
            {
                for (var i = 0; i < span.Length; i++)
                {
                    // GetInt32 avoids the bias of a plain modulo operation.
                    span[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
                }
            });

            if (policy.Validate(password, username).Count == 0)
            {
                return password;
            }
        }

        throw new InvalidOperationException("Could not generate a temporary password that satisfies the password policy.");
    }
}
