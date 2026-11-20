using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Schuelerfirma.Api.Application.Auth;
using Schuelerfirma.Api.Application.Users;
using Schuelerfirma.Api.Domain;
using Schuelerfirma.Api.Infrastructure.Auth;
using Schuelerfirma.Api.Infrastructure.InMemory;
using Schuelerfirma.Api.Tests.Auth;

namespace Schuelerfirma.Api.Tests.Users;

public class UserAdministrationServiceTests
{
    private const int AdminRole = 1;
    private const int UserRole = 2;

    // An id that is never a real user: actions "by somebody else", so the self-protection rules do not apply.
    private const int SomebodyElse = 999;

    private readonly FakeTimeProvider time = new(new DateTimeOffset(2026, 11, 1, 8, 0, 0, TimeSpan.Zero));
    private readonly InMemoryStore store = new();
    private readonly InMemoryUserRepository users;
    private readonly InMemoryUserSessionRepository sessions;
    private readonly PasswordHasher<User> hasher = new(Options.Create(new PasswordHasherOptions { IterationCount = 1_000 }));
    private readonly ListLoggerProvider logs = new();
    private readonly UserAdministrationService service;

    public UserAdministrationServiceTests()
    {
        users = new InMemoryUserRepository(store);
        sessions = new InMemoryUserSessionRepository(store);
        var policy = new PasswordPolicy(Options.Create(new PasswordPolicyOptions()), new EmbeddedCommonPasswordList());
        var permissions = new RolePermissionOptions();
        permissions.RolePermissions["Admin"] = [Permissions.Wildcard];
        permissions.RolePermissions["User"] = [Permissions.EventsCreate];
        var loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(logs));

        service = new UserAdministrationService(
            users,
            new InMemoryRoleRepository(store),
            sessions,
            new ConfiguredRolePermissionProvider(new StaticOptionsMonitor(permissions), loggerFactory.CreateLogger<ConfiguredRolePermissionProvider>()),
            new TemporaryPasswordGenerator(policy),
            hasher,
            time,
            loggerFactory.CreateLogger<UserAdministrationService>());
    }

    private sealed class StaticOptionsMonitor(RolePermissionOptions value) : IOptionsMonitor<RolePermissionOptions>
    {
        public RolePermissionOptions CurrentValue => value;

        public RolePermissionOptions Get(string? name) => value;

        public IDisposable? OnChange(Action<RolePermissionOptions, string?> listener) => null;
    }

    private DateTime Now => time.GetUtcNow().UtcDateTime;

    private async Task<User> AddAsync(string username, int roleId = UserRole, bool active = true)
    {
        var user = new User { Username = username, Email = $"{username}@example.test", RoleId = roleId, IsActive = active, CreatedAt = Now };
        user.PasswordHash = hasher.HashPassword(user, "Ein-altes-langes-Passwort-1");
        await users.AddAsync(user);
        return user;
    }

    private async Task AddSessionsAsync(int userId, int count)
    {
        for (var i = 0; i < count; i++)
        {
            await sessions.AddAsync(new UserSession
            {
                SessionKeyHash = $"{userId:D3}{i:D3}".PadRight(UserSession.SessionKeyHashLength, 'f'),
                UserId = userId,
                CreatedAt = Now,
                ExpiresAt = Now.AddMinutes(30),
            });
        }
    }

    private Task<int> RemainingSessionsAsync(int userId) => sessions.DeleteAllForUserAsync(userId);

    // ---- create ----

    [Fact]
    public async Task Create_MakesAnActiveUser_WithATemporaryPasswordThatMustBeChanged()
    {
        var result = await service.CreateAsync(SomebodyElse, "neu.mitglied", "neu@example.test", UserRole);

        Assert.Equal(UserAdministrationError.None, result.Error);
        var user = result.User!;
        Assert.True(user.IsActive);
        Assert.True(user.MustChangePassword);
        Assert.Equal("User", user.Role?.Name);
        Assert.Equal(Now, user.CreatedAt);
        Assert.Equal(PasswordVerificationResult.Success, hasher.VerifyHashedPassword(user, user.PasswordHash, result.TemporaryPassword!));
    }

    [Theory]
    [InlineData("Gross", "a@example.test", UserRole, "Username")]
    [InlineData("ab", "a@example.test", UserRole, "Username")]
    [InlineData("mit leerzeichen", "a@example.test", UserRole, "Username")]
    [InlineData("anna", "keine-adresse", UserRole, "Email")]
    [InlineData("anna", "a@example.test", 42, "RoleId")]
    public async Task Create_RejectsInvalidInput_NamingTheField(string username, string email, int roleId, string field)
    {
        var result = await service.CreateAsync(SomebodyElse, username, email, roleId);

        Assert.Equal(UserAdministrationError.Invalid, result.Error);
        Assert.Equal(field, result.Field);
        Assert.Empty(await users.GetAllAsync());
    }

    [Theory]
    [InlineData("anna", "andere@example.test", "Username")]
    [InlineData("berta", "ANNA@example.test", "Email")]
    public async Task Create_RejectsDuplicates_CaseInsensitively(string username, string email, string field)
    {
        await AddAsync("anna");

        var result = await service.CreateAsync(SomebodyElse, username, email, UserRole);

        Assert.Equal(UserAdministrationError.Conflict, result.Error);
        Assert.Equal(field, result.Field);
    }

    // ---- update ----

    [Fact]
    public async Task Update_ARoleChange_EndsTheSessionsOfTheUser()
    {
        var anna = await AddAsync("anna");
        await AddSessionsAsync(anna.UserId, 2);

        var result = await service.UpdateAsync(SomebodyElse, anna.UserId, anna.Email, AdminRole, true);

        Assert.Equal(UserAdministrationError.None, result.Error);
        Assert.Equal("Admin", result.User!.Role?.Name);
        Assert.Equal(2, result.EndedSessions);
        Assert.Equal(0, await RemainingSessionsAsync(anna.UserId));
    }

    [Fact]
    public async Task Update_ADeactivation_EndsTheSessionsOfTheUser()
    {
        var anna = await AddAsync("anna");
        await AddSessionsAsync(anna.UserId, 1);

        var result = await service.UpdateAsync(SomebodyElse, anna.UserId, anna.Email, UserRole, false);

        Assert.False(result.User!.IsActive);
        Assert.Equal(0, await RemainingSessionsAsync(anna.UserId));
    }

    [Fact]
    public async Task Update_OnlyTheEmail_KeepsTheSessions()
    {
        var anna = await AddAsync("anna");
        await AddSessionsAsync(anna.UserId, 1);

        var result = await service.UpdateAsync(SomebodyElse, anna.UserId, "anna.neu@example.test", UserRole, true);

        Assert.Equal("anna.neu@example.test", result.User!.Email);
        Assert.Equal(0, result.EndedSessions);
        Assert.Equal(1, await RemainingSessionsAsync(anna.UserId));
    }

    [Fact]
    public async Task Update_NobodyCanDeactivateThemselves_OrChangeTheirOwnRole_ButTheirEmail()
    {
        var admin = await AddAsync("chefin", AdminRole);
        await AddAsync("zweite.admin", AdminRole);

        var deactivate = await service.UpdateAsync(admin.UserId, admin.UserId, admin.Email, AdminRole, false);
        var demote = await service.UpdateAsync(admin.UserId, admin.UserId, admin.Email, UserRole, true);
        var email = await service.UpdateAsync(admin.UserId, admin.UserId, "chefin@schule.test", AdminRole, true);

        Assert.Equal(UserAdministrationError.Conflict, deactivate.Error);
        Assert.Equal(UserAdministrationError.Conflict, demote.Error);
        Assert.Equal(UserAdministrationError.None, email.Error);
    }

    [Fact]
    public async Task Update_TheLastActiveUserManager_CannotBeDeactivatedOrDemoted()
    {
        var admin = await AddAsync("chefin", AdminRole);

        var deactivate = await service.UpdateAsync(SomebodyElse, admin.UserId, admin.Email, AdminRole, false);
        var demote = await service.UpdateAsync(SomebodyElse, admin.UserId, admin.Email, UserRole, true);

        Assert.Equal(UserAdministrationError.Conflict, deactivate.Error);
        Assert.Contains("letzte", deactivate.Message);
        Assert.Equal(UserAdministrationError.Conflict, demote.Error);
        Assert.True((await users.GetByIdAsync(admin.UserId))!.IsActive);
    }

    [Fact]
    public async Task Update_AUserManager_CanBeDeactivated_WhenAnotherOneIsLeft()
    {
        var admin = await AddAsync("chefin", AdminRole);
        await AddAsync("zweite.admin", AdminRole);
        await AddAsync("inaktive.admin", AdminRole, active: false);

        var result = await service.UpdateAsync(SomebodyElse, admin.UserId, admin.Email, AdminRole, false);

        Assert.Equal(UserAdministrationError.None, result.Error);
    }

    [Fact]
    public async Task Update_ChangesThatDoNotTouchUserManagement_AreNotBlockedByTheLastManagerRule()
    {
        var admin = await AddAsync("chefin", AdminRole);
        var anna = await AddAsync("anna");

        Assert.Equal(UserAdministrationError.None, (await service.UpdateAsync(SomebodyElse, anna.UserId, anna.Email, UserRole, false)).Error);
        Assert.Equal(UserAdministrationError.None, (await service.UpdateAsync(SomebodyElse, admin.UserId, "neu@example.test", AdminRole, true)).Error);
    }

    [Fact]
    public async Task Update_RejectsAnEmailOfAnotherAccount_UnknownUsersAndRoles()
    {
        await AddAsync("anna");
        var berta = await AddAsync("berta");

        Assert.Equal(UserAdministrationError.Conflict, (await service.UpdateAsync(SomebodyElse, berta.UserId, "Anna@example.test", UserRole, true)).Error);
        Assert.Equal(UserAdministrationError.NotFound, (await service.UpdateAsync(SomebodyElse, 4711, "x@example.test", UserRole, true)).Error);
        Assert.Equal("RoleId", (await service.UpdateAsync(SomebodyElse, berta.UserId, berta.Email, 42, true)).Field);
    }

    // ---- reset password ----

    [Fact]
    public async Task ResetPassword_SetsATemporaryPassword_LiftsTheLockout_AndEndsTheSessions()
    {
        var anna = await AddAsync("anna");
        await users.LockOutAsync(anna.UserId, Now.AddMinutes(10));
        await AddSessionsAsync(anna.UserId, 3);

        var result = await service.ResetPasswordAsync(SomebodyElse, anna.UserId);

        var stored = (await users.GetByIdAsync(anna.UserId))!;
        Assert.Equal(3, result.EndedSessions);
        Assert.True(stored.MustChangePassword);
        Assert.Null(stored.LockoutEnd);
        Assert.Equal(PasswordVerificationResult.Success, hasher.VerifyHashedPassword(stored, stored.PasswordHash, result.TemporaryPassword!));
        Assert.Equal(0, await RemainingSessionsAsync(anna.UserId));
    }

    [Fact]
    public async Task ResetPassword_IsNotPossibleForTheOwnAccount()
    {
        var admin = await AddAsync("chefin", AdminRole);
        var hashBefore = admin.PasswordHash;

        var result = await service.ResetPasswordAsync(admin.UserId, admin.UserId);

        Assert.Equal(UserAdministrationError.Conflict, result.Error);
        Assert.Equal(hashBefore, (await users.GetByIdAsync(admin.UserId))!.PasswordHash);
    }

    // ---- sessions and log ----

    [Fact]
    public async Task RevokeSessions_EndsAllSessions_AndCountsThem()
    {
        var anna = await AddAsync("anna");
        await AddSessionsAsync(anna.UserId, 2);

        var result = await service.RevokeSessionsAsync(SomebodyElse, anna.UserId);

        Assert.Equal(2, result.EndedSessions);
        Assert.Equal(0, await RemainingSessionsAsync(anna.UserId));
        Assert.Equal(UserAdministrationError.NotFound, (await service.RevokeSessionsAsync(SomebodyElse, 4711)).Error);
    }

    [Fact]
    public async Task EveryAction_IsLogged_WithTheActor_ButNeverWithATemporaryPassword()
    {
        var created = await service.CreateAsync(7, "anna", "anna@example.test", UserRole);
        var reset = await service.ResetPasswordAsync(7, created.User!.UserId);
        await service.UpdateAsync(7, created.User.UserId, "anna@example.test", UserRole, false);
        await service.RevokeSessionsAsync(7, created.User.UserId);

        var text = logs.AllText;
        Assert.DoesNotContain(created.TemporaryPassword!, text);
        Assert.DoesNotContain(reset.TemporaryPassword!, text);
        Assert.Contains("User 7 created user anna", text);
        Assert.Contains("User 7 reset the password of anna", text);
        Assert.Contains("User 7 changed user anna", text);
        Assert.Contains("User 7 ended 0 session(s) of anna", text);
    }
}
