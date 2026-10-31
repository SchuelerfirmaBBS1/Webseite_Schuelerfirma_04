using Microsoft.Extensions.Options;
using Schuelerfirma.Api.Application.Auth;
using Schuelerfirma.Api.Infrastructure.Auth;

namespace Schuelerfirma.Api.Tests.Auth;

public class TemporaryPasswordGeneratorTests
{
    private static readonly PasswordPolicy Policy = new(
        Options.Create(new PasswordPolicyOptions()), new EmbeddedCommonPasswordList());

    private readonly TemporaryPasswordGenerator generator = new(Policy);

    [Fact]
    public void GeneratedPasswords_HaveSixteenCharacters_WithoutConfusableOnes()
    {
        for (var i = 0; i < 200; i++)
        {
            var password = generator.Generate("anna");

            Assert.Equal(16, password.Length);
            Assert.DoesNotContain(password, c => "0O1lI".Contains(c));
            Assert.All(password, c => Assert.Contains(c, TemporaryPasswordGenerator.Alphabet));
        }
    }

    [Fact]
    public void GeneratedPasswords_SatisfyThePolicy()
    {
        for (var i = 0; i < 500; i++)
        {
            var password = generator.Generate("anna");

            Assert.Empty(Policy.Validate(password, "anna"));
        }
    }

    [Fact]
    public void GeneratedPasswords_DoNotRepeat()
    {
        var passwords = Enumerable.Range(0, 200).Select(_ => generator.Generate("anna")).ToList();

        Assert.Equal(passwords.Count, passwords.Distinct().Count());
    }

    [Fact]
    public void TheUsername_IsNeverContained()
    {
        // A one-letter user name makes a chance hit likely, so the generator has to draw again.
        for (var i = 0; i < 200; i++)
        {
            Assert.DoesNotContain("a", generator.Generate("a"), StringComparison.OrdinalIgnoreCase);
        }
    }
}
