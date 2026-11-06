using System.Buffers.Binary;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Schuelerfirma.Api.Domain;
using Schuelerfirma.Api.Infrastructure.Auth;
using Schuelerfirma.Api.Tests.Integration;

namespace Schuelerfirma.Api.Tests.Auth;

public class PasswordHashingTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string Password = "correct horse battery staple";

    private readonly User user = new() { Username = "anna" };

    private IPasswordHasher<User> Hasher => factory.Services.GetRequiredService<IPasswordHasher<User>>();

    [Fact]
    public void ACorrectPassword_Verifies_WithoutNeedingARehash()
    {
        var hash = Hasher.HashPassword(user, Password);

        Assert.Equal(PasswordVerificationResult.Success, Hasher.VerifyHashedPassword(user, hash, Password));
    }

    [Fact]
    public void AWrongPassword_Fails()
    {
        var hash = Hasher.HashPassword(user, Password);

        Assert.Equal(PasswordVerificationResult.Failed, Hasher.VerifyHashedPassword(user, hash, Password + "x"));
    }

    [Fact]
    public void TheSamePassword_ProducesDifferentHashes()
    {
        Assert.NotEqual(Hasher.HashPassword(user, Password), Hasher.HashPassword(user, Password));
    }

    [Fact]
    public void NewHashes_UseHmacSha512_With210000Iterations()
    {
        var bytes = Convert.FromBase64String(Hasher.HashPassword(user, Password));

        // Format of PasswordHasher<TUser> version 3: marker 0x01, PRF, iteration count, salt size (big-endian uint32 each).
        Assert.Equal(0x01, bytes[0]);
        Assert.Equal(2u, BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(1))); // 2 = HMAC-SHA512
        Assert.Equal(210_000u, BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(5)));
        Assert.Equal(210_000, PasswordSecurityExtensions.HashIterations);
    }

    [Fact]
    public void AHashWith100000Iterations_VerifiesButAsksForARehash()
    {
        var oldHasher = new PasswordHasher<User>(Options.Create(new PasswordHasherOptions { IterationCount = 100_000 }));
        var oldHash = oldHasher.HashPassword(user, Password);

        Assert.Equal(
            PasswordVerificationResult.SuccessRehashNeeded,
            Hasher.VerifyHashedPassword(user, oldHash, Password));
        Assert.Equal(
            PasswordVerificationResult.Failed,
            Hasher.VerifyHashedPassword(user, oldHash, "wrong password"));
    }
}
