using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Schuelerfirma.Api.Application.Auth;
using Schuelerfirma.Api.Domain;
using Schuelerfirma.Api.Infrastructure.InMemory;

namespace Schuelerfirma.Api.Tests.Auth;

public class AuthServiceTests
{
    private const string Password = "correct horse battery staple";

    private readonly FakeTimeProvider time = new(new DateTimeOffset(2026, 11, 1, 8, 0, 0, TimeSpan.Zero));
    private readonly InMemoryStore store = new();
    private readonly InMemoryUserRepository users;
    private readonly PasswordHasher<User> hasher = new(Options.Create(new PasswordHasherOptions { IterationCount = 5_000 }));
    private readonly RecordingHasher recorder;

    public AuthServiceTests()
    {
        users = new InMemoryUserRepository(store);
        recorder = new RecordingHasher(hasher);
    }

    private DateTime Now => time.GetUtcNow().UtcDateTime;

    private AuthService CreateService(int maxAttempts = 5) => new(
        users,
        recorder,
        new DummyPasswordHash(recorder),
        time,
        Options.Create(new LoginLockoutOptions { MaxFailedAttempts = maxAttempts, Duration = TimeSpan.FromMinutes(15) }),
        NullLogger<AuthService>.Instance);

    private async Task<User> AddUserAsync(string username = "anna", bool active = true, string? hash = null)
    {
        var user = new User { Username = username, Email = $"{username}@example.test", RoleId = 2, IsActive = active, CreatedAt = Now };
        user.PasswordHash = hash ?? hasher.HashPassword(user, Password);
        await users.AddAsync(user);
        return user;
    }

    private async Task FailTimesAsync(AuthService service, int times)
    {
        for (var i = 0; i < times; i++)
        {
            await service.LoginAsync("anna", "wrong password!!", null);
        }
    }

    [Fact]
    public async Task CorrectCredentials_Succeed_AndRecordTheLogin()
    {
        var user = await AddUserAsync();
        await users.IncrementFailedLoginCountAsync(user.UserId);

        var result = await CreateService().LoginAsync("anna", Password, "10.0.0.1");

        Assert.Equal(LoginOutcome.Success, result.Outcome);
        Assert.Equal(user.UserId, result.User?.UserId);
        var stored = (await users.GetByIdAsync(user.UserId))!;
        Assert.Equal(0, stored.FailedLoginCount);
        Assert.Equal(Now, stored.LastLoginAt);
    }

    [Fact]
    public async Task TheUsername_IsMatchedIgnoringCase()
    {
        await AddUserAsync("anna");

        Assert.True((await CreateService().LoginAsync("ANNA", Password, null)).Succeeded);
    }

    [Fact]
    public async Task AnUnknownUser_Fails()
    {
        var result = await CreateService().LoginAsync("nobody", Password, null);

        Assert.Equal(LoginOutcome.UnknownUser, result.Outcome);
        Assert.Null(result.User);
    }

    [Fact]
    public async Task AWrongPassword_Fails_AndCountsTheAttempt()
    {
        var user = await AddUserAsync();

        var result = await CreateService().LoginAsync("anna", "wrong password!!", null);

        Assert.Equal(LoginOutcome.WrongPassword, result.Outcome);
        Assert.Equal(1, (await users.GetByIdAsync(user.UserId))!.FailedLoginCount);
    }

    [Fact]
    public async Task TheFifthWrongPassword_LocksTheAccountFor15Minutes()
    {
        var user = await AddUserAsync();
        var service = CreateService();

        await FailTimesAsync(service, 4);
        Assert.Null((await users.GetByIdAsync(user.UserId))!.LockoutEnd);
        await FailTimesAsync(service, 1);

        var stored = (await users.GetByIdAsync(user.UserId))!;
        Assert.Equal(Now.AddMinutes(15), stored.LockoutEnd);
        Assert.Equal(0, stored.FailedLoginCount);
    }

    [Fact]
    public async Task TheLimit_IsConfigurable()
    {
        var user = await AddUserAsync();
        var service = CreateService(maxAttempts: 3);

        await FailTimesAsync(service, 3);

        Assert.NotNull((await users.GetByIdAsync(user.UserId))!.LockoutEnd);
    }

    [Fact]
    public async Task ASuccess_ResetsTheCounter_SoTheLimitStartsAgain()
    {
        var user = await AddUserAsync();
        var service = CreateService();

        await FailTimesAsync(service, 4);
        await service.LoginAsync("anna", Password, null);
        await FailTimesAsync(service, 4);

        Assert.Null((await users.GetByIdAsync(user.UserId))!.LockoutEnd);
    }

    [Fact]
    public async Task ALockedAccount_RejectsEvenTheCorrectPassword_WithoutCheckingIt()
    {
        var user = await AddUserAsync();
        var service = CreateService();
        await FailTimesAsync(service, 5);
        recorder.VerifiedHashes.Clear();
        var lockoutEnd = (await users.GetByIdAsync(user.UserId))!.LockoutEnd;

        time.Advance(TimeSpan.FromMinutes(5));
        var correct = await service.LoginAsync("anna", Password, null);
        var wrong = await service.LoginAsync("anna", "wrong password!!", null);

        Assert.Equal(LoginOutcome.LockedOut, correct.Outcome);
        Assert.Equal(LoginOutcome.LockedOut, wrong.Outcome);
        Assert.DoesNotContain(user.PasswordHash, recorder.VerifiedHashes);
        var stored = (await users.GetByIdAsync(user.UserId))!;
        Assert.Equal(lockoutEnd, stored.LockoutEnd);
        Assert.Equal(0, stored.FailedLoginCount);
        Assert.Null(stored.LastLoginAt);
    }

    [Fact]
    public async Task TheLockout_EndsExactlyAfterItsDuration()
    {
        await AddUserAsync();
        var service = CreateService();
        await FailTimesAsync(service, 5);

        time.Advance(TimeSpan.FromMinutes(15) - TimeSpan.FromSeconds(1));
        Assert.Equal(LoginOutcome.LockedOut, (await service.LoginAsync("anna", Password, null)).Outcome);

        time.Advance(TimeSpan.FromSeconds(1));
        Assert.True((await service.LoginAsync("anna", Password, null)).Succeeded);
    }

    [Fact]
    public async Task ADeactivatedAccount_Fails_EvenWithTheCorrectPassword()
    {
        await AddUserAsync(active: false);

        var result = await CreateService().LoginAsync("anna", Password, null);

        Assert.Equal(LoginOutcome.Inactive, result.Outcome);
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("inactive")]
    [InlineData("locked")]
    public async Task UnknownInactiveAndLockedAccounts_CostOnePasswordCheckAgainstADummyHash(string kind)
    {
        var service = CreateService();
        var real = await AddUserAsync("anna", active: kind != "inactive");
        if (kind == "locked")
        {
            await users.LockOutAsync(real.UserId, Now.AddMinutes(10));
        }

        recorder.VerifiedHashes.Clear();
        await service.LoginAsync(kind == "unknown" ? "nobody" : "anna", Password, null);

        // Exactly one expensive check happens, so the response time is the same as for a wrong password,
        // and it never uses the account's real hash.
        var verified = Assert.Single(recorder.VerifiedHashes);
        Assert.NotEqual(real.PasswordHash, verified);
    }

    [Fact]
    public async Task AWrongPassword_CostsOnePasswordCheckAgainstTheRealHash()
    {
        var real = await AddUserAsync();
        recorder.VerifiedHashes.Clear();

        await CreateService().LoginAsync("anna", "wrong password!!", null);

        Assert.Equal([real.PasswordHash], recorder.VerifiedHashes);
    }

    [Fact]
    public async Task AnOutdatedHash_IsReplacedAtLogin()
    {
        var weakHasher = new PasswordHasher<User>(Options.Create(new PasswordHasherOptions { IterationCount = 1_000 }));
        var user = new User { Username = "anna" };
        var oldHash = weakHasher.HashPassword(user, Password);
        await AddUserAsync(hash: oldHash);

        var result = await CreateService().LoginAsync("anna", Password, null);

        Assert.True(result.Succeeded);
        var stored = (await users.GetByUsernameAsync("anna"))!;
        Assert.NotEqual(oldHash, stored.PasswordHash);
        Assert.Equal(PasswordVerificationResult.Success, hasher.VerifyHashedPassword(stored, stored.PasswordHash, Password));
    }
}
