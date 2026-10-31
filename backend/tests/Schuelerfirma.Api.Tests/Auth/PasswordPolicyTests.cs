using Microsoft.Extensions.Options;
using Schuelerfirma.Api.Application.Auth;

namespace Schuelerfirma.Api.Tests.Auth;

public class PasswordPolicyTests
{
    private sealed class FakeCommonPasswords(params string[] passwords) : ICommonPasswordList
    {
        public bool Contains(string password) => passwords.Contains(password, StringComparer.OrdinalIgnoreCase);
    }

    private static PasswordPolicy CreatePolicy(int minLength = 12, params string[] common) =>
        new(Options.Create(new PasswordPolicyOptions { MinLength = minLength }), new FakeCommonPasswords(common));

    private static IEnumerable<PasswordRule> Rules(PasswordPolicy policy, string? password, string? username = "anna") =>
        policy.Validate(password, username).Select(v => v.Rule);

    [Fact]
    public void AGoodPassphrase_IsAccepted()
    {
        Assert.Empty(CreatePolicy().Validate("correct horse battery staple", "anna"));
    }

    [Theory]
    [InlineData(11, false)]
    [InlineData(12, true)]
    [InlineData(128, true)]
    [InlineData(129, false)]
    public void Length_IsCheckedAtTheBoundaries(int length, bool accepted)
    {
        var password = string.Concat(Enumerable.Range(0, length).Select(i => (char)('a' + ((i * 7) % 23))));

        Assert.Equal(accepted, !Rules(CreatePolicy(), password).Any());
    }

    [Fact]
    public void TooShort_ReportsTheRuleAndAGermanMessage()
    {
        var violation = Assert.Single(CreatePolicy().Validate("kurz", "anna"));

        Assert.Equal(PasswordRule.TooShort, violation.Rule);
        Assert.Equal("Das Passwort muss mindestens 12 Zeichen lang sein.", violation.Message);
    }

    [Fact]
    public void TooLong_ReportsOnlyThatRule()
    {
        var password = new string('x', 129) + "anna";

        var violation = Assert.Single(CreatePolicy().Validate(password, "anna"));

        Assert.Equal(PasswordRule.TooLong, violation.Rule);
    }

    [Fact]
    public void ConfiguredMinLength_IsUsed()
    {
        var policy = CreatePolicy(minLength: 16);

        Assert.Contains(PasswordRule.TooShort, Rules(policy, "fifteen chars!!"));
        Assert.DoesNotContain(PasswordRule.TooShort, Rules(policy, "sixteen chars!!!"));
    }

    [Fact]
    public void Length_CountsCharactersNotUtf16Units()
    {
        // Six emoji are twelve UTF-16 units but only six characters.
        Assert.Contains(PasswordRule.TooShort, Rules(CreatePolicy(), "😀😃😄😁😆😅"));
    }

    [Theory]
    [InlineData("my-ANNA-password-1", "anna")]
    [InlineData("annasecretpassword", "Anna")]
    public void ContainingTheUsername_IsRejected_IgnoringCase(string password, string username)
    {
        Assert.Contains(PasswordRule.ContainsUsername, Rules(CreatePolicy(), password, username));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void WithoutAUsername_TheUsernameRuleIsSkipped(string? username)
    {
        Assert.Empty(CreatePolicy().Validate("correct horse battery staple", username));
    }

    [Theory]
    [InlineData("aaaaaaaaaaaa")]
    [InlineData("000000000000")]
    [InlineData("😀😀😀😀😀😀😀😀😀😀😀😀")]
    public void OneRepeatedCharacter_IsRejected(string password)
    {
        Assert.Contains(PasswordRule.RepeatedCharacter, Rules(CreatePolicy(), password));
    }

    [Theory]
    [InlineData("123456789012")]
    [InlineData("987654321098")]
    [InlineData("abcdefghijkl")]
    [InlineData("zyxwvutsrqpo")]
    [InlineData("ABCDEFGHIJKL")]
    [InlineData("wxyzabcdefgh")]
    public void AscendingOrDescendingSequences_AreRejected(string password)
    {
        Assert.Contains(PasswordRule.Sequence, Rules(CreatePolicy(), password));
    }

    [Theory]
    [InlineData("abcdefghijkx")]
    [InlineData("123456789013")]
    [InlineData("acegikmoqsuw")]
    public void AlmostSequences_AreAccepted(string password)
    {
        Assert.DoesNotContain(PasswordRule.Sequence, Rules(CreatePolicy(), password));
    }

    [Fact]
    public void CommonPasswords_AreRejected_IgnoringCase()
    {
        var policy = CreatePolicy(12, "thisismypassword");

        Assert.Contains(PasswordRule.CommonPassword, Rules(policy, "ThisIsMyPassword"));
        Assert.DoesNotContain(PasswordRule.CommonPassword, Rules(policy, "ThisIsNotMyPassword"));
    }

    [Fact]
    public void NoCharacterClassesAreRequired()
    {
        // Only lower-case letters, no digits and no special characters: fine, because it is long.
        Assert.Empty(CreatePolicy().Validate("lowercaseletterspassphrase", "anna"));
    }

    [Fact]
    public void SeveralBrokenRules_AreAllReported()
    {
        var rules = Rules(CreatePolicy(), "aaaa", "aaaa").ToList();

        Assert.Contains(PasswordRule.TooShort, rules);
        Assert.Contains(PasswordRule.ContainsUsername, rules);
        Assert.Contains(PasswordRule.RepeatedCharacter, rules);
    }

    [Fact]
    public void ANullPassword_IsTreatedAsEmpty()
    {
        Assert.Contains(PasswordRule.TooShort, Rules(CreatePolicy(), null));
    }
}
