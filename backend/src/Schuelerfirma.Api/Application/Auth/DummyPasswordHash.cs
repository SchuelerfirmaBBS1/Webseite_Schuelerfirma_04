using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Schuelerfirma.Api.Domain;

namespace Schuelerfirma.Api.Application.Auth;

/// <summary>
/// Spends the time of one password check without checking anything real. A login for an unknown, locked or
/// deactivated account calls this, so the response time does not reveal which case it was.
/// </summary>
public sealed class DummyPasswordHash(IPasswordHasher<User> hasher)
{
    private readonly Lazy<string> hash = new(() =>
        hasher.HashPassword(new User(), Convert.ToBase64String(RandomNumberGenerator.GetBytes(24))));

    /// <summary>Hashes-and-compares <paramref name="password"/> against a random hash; the result is meaningless.</summary>
    public void Verify(string password) => hasher.VerifyHashedPassword(new User(), hash.Value, password);
}
