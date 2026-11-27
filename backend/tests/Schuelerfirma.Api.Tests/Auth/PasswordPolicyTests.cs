using Microsoft.Extensions.Options;
using Schuelerfirma.Api.Application.Auth;

namespace Schuelerfirma.Api.Tests.Auth;

public class PasswordPolicyTests
{
    private sealed class FakeCommonPasswords(params string[] passwords) : ICommonPasswordList
    {
        public bool Contains(string password) => passwords.Contains(password, StringComparer.OrdinalIgnoreCase);
    }

    // Satisfies every rule, including all four character classes.
    private const string GoodPassword = "Correct horse battery staple #7";

    private static PasswordPolicy CreatePolicy(int minLength = 12, params string[] common) =>
        new(Options.Create(new PasswordPolicyOptions { MinLength = minLength }), new FakeCommonPasswords(common));

    // For tests about the other rules: without the character classes, a test sees only the rule it is about.
    private static PasswordPolicy CreatePolicyWithoutCharacterClasses() =>
        new(Options.Create(new PasswordPolicyOptions
        {
            RequireUppercase = false,
            RequireLowercase = false,
            RequireDigit = false,
            RequireSpecialCharacter = false,
        }), new FakeCommonPasswords());

    private static IEnumerable<PasswordRule> Rules(PasswordPolicy policy, string? password, string? username = "anna") =>
        policy.Validate(password, username).Select(v => v.Rule);

    [Fact]
    public void AGoodPassword_IsAccepted()
    {
        Assert.Empty(CreatePolicy().Validate(GoodPassword, "anna"));
    }

    [Theory]
    [InlineData(11, false)]
    [InlineData(12, true)]
    [InlineData(128, true)]
    [InlineData(129, false)]
    public void Length_IsCheckedAtTheBoundaries(int length, bool accepted)
    {
        var password = "A1!" + string.Concat(Enumerable.Range(0, length - 3).Select(i => (char)('a' + ((i * 7) % 23))));

        Assert.Equal(accepted, !Rules(CreatePolicy(), password).Any());
    }

    [Fact]
    public void TooShort_ReportsTheRuleAndAGermanMessage()
    {
        var violation = Assert.Single(CreatePolicyWithoutCharacterClasses().Validate("kurz", "anna"));

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
        Assert.Empty(CreatePolicy().Validate(GoodPassword, username));
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

    // ---- character classes ----

    [Theory]
    [InlineData("correct horse battery staple #7", PasswordRule.MissingUppercase)]
    [InlineData("CORRECT HORSE BATTERY STAPLE #7", PasswordRule.MissingLowercase)]
    [InlineData("Correct horse battery staple #x", PasswordRule.MissingDigit)]
    [InlineData("Correcthorsebatterystaple7", PasswordRule.MissingSpecialCharacter)]
    public void AMissingCharacterClass_IsRejected(string password, PasswordRule rule)
    {
        var violation = Assert.Single(CreatePolicy().Validate(password, "anna"));

        Assert.Equal(rule, violation.Rule);
    }

    [Fact]
    public void AllMissingCharacterClasses_AreReportedTogether_WithGermanMessages()
    {
        var violations = CreatePolicy().Validate("            ", "anna");

        Assert.Equal(
            [
                "Das Passwort muss mindestens einen Großbuchstaben enthalten.",
                "Das Passwort muss mindestens einen Kleinbuchstaben enthalten.",
                "Das Passwort muss mindestens eine Ziffer enthalten.",
                "Das Passwort muss mindestens ein Sonderzeichen enthalten (z. B. ! ? # - _ .). Ein Leerzeichen zählt nicht.",
            ],
            violations.Where(v => v.Rule != PasswordRule.RepeatedCharacter).Select(v => v.Message));
    }

    [Fact]
    public void ASpace_IsNoSpecialCharacter()
    {
        Assert.Contains(PasswordRule.MissingSpecialCharacter, Rules(CreatePolicy(), "Ein ganzer Satz mit 2026"));
    }

    [Theory]
    [InlineData("!")]
    [InlineData("?")]
    [InlineData("#")]
    [InlineData("-")]
    [InlineData("_")]
    [InlineData(".")]
    [InlineData("@")]
    [InlineData("€")]
    [InlineData("§")]
    [InlineData("😀")]
    public void EveryCharacterThatIsNeitherLetterDigitNorSpace_IsASpecialCharacter(string special)
    {
        Assert.Empty(CreatePolicy().Validate($"Correct horse battery staple 7{special}", "anna"));
    }

    [Fact]
    public void Umlauts_CountAsLetters()
    {
        // Upper-case and lower-case only from umlauts; ß and ä are no special characters.
        Assert.Empty(CreatePolicy().Validate("ÄÖÜ äöü ß 2026 !", "anna"));
        Assert.Contains(PasswordRule.MissingSpecialCharacter, Rules(CreatePolicy(), "ÄÖÜäöüßxyz2026"));
    }

    [Theory]
    [InlineData(nameof(PasswordPolicyOptions.RequireUppercase), "correct horse battery staple #7")]
    [InlineData(nameof(PasswordPolicyOptions.RequireLowercase), "CORRECT HORSE BATTERY STAPLE #7")]
    [InlineData(nameof(PasswordPolicyOptions.RequireDigit), "Correct horse battery staple #x")]
    [InlineData(nameof(PasswordPolicyOptions.RequireSpecialCharacter), "Correcthorsebatterystaple7")]
    public void EachCharacterClass_CanBeSwitchedOff(string option, string password)
    {
        var options = new PasswordPolicyOptions();
        typeof(PasswordPolicyOptions).GetProperty(option)!.SetValue(options, false);
        var policy = new PasswordPolicy(Options.Create(options), new FakeCommonPasswords());

        Assert.Empty(policy.Validate(password, "anna"));
    }

    [Fact]
    public void WithoutCharacterClasses_ALongLowercasePassphrase_IsAccepted()
    {
        Assert.Empty(CreatePolicyWithoutCharacterClasses().Validate("lowercaseletterspassphrase", "anna"));
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
