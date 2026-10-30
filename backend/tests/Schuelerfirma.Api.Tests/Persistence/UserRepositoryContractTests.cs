using Schuelerfirma.Api.Application.Abstractions;
using Schuelerfirma.Api.Domain;

namespace Schuelerfirma.Api.Tests.Persistence;

public abstract class UserRepositoryContractTests : RepositoryContractTests
{
    [Fact]
    public async Task Add_AssignsId_AndGetByIdLoadsTheRole()
    {
        var repos = CreateRepositories();
        var user = NewUser("anna", roleId: AdminRoleId);

        await repos.Users.AddAsync(user);
        var loaded = await repos.Users.GetByIdAsync(user.UserId);

        Assert.True(user.UserId > 0);
        Assert.NotNull(loaded);
        Assert.Equal("anna", loaded.Username);
        Assert.Equal(DefaultRoles.Admin, loaded.Role?.Name);
        Assert.Equal(Now, loaded.CreatedAt);
    }

    [Fact]
    public async Task GetById_Unknown_ReturnsNull()
    {
        Assert.Null(await CreateRepositories().Users.GetByIdAsync(9999));
    }

    [Fact]
    public async Task ReturnedUsers_AreCopies_ChangesNeedUpdate()
    {
        var repos = CreateRepositories();
        var user = await AddUserAsync(repos);

        var first = await repos.Users.GetByIdAsync(user.UserId);
        first!.Email = "changed@example.test";
        var second = await repos.Users.GetByIdAsync(user.UserId);

        Assert.Equal("anna@example.test", second!.Email);
    }

    [Fact]
    public async Task GetByUsername_IgnoresCase()
    {
        var repos = CreateRepositories();
        var user = await AddUserAsync(repos, "Anna");

        var loaded = await repos.Users.GetByUsernameAsync("aNNa");

        Assert.Equal(user.UserId, loaded?.UserId);
        Assert.Null(await repos.Users.GetByUsernameAsync("berta"));
    }

    [Fact]
    public async Task Add_DuplicateUsername_IgnoringCase_Throws()
    {
        var repos = CreateRepositories();
        await AddUserAsync(repos, "anna");

        var exception = await Assert.ThrowsAsync<DuplicateEntryException>(
            () => repos.Users.AddAsync(NewUser("ANNA", "other@example.test")));

        Assert.Equal(nameof(User.Username), exception.Field);
    }

    [Fact]
    public async Task Add_DuplicateEmail_IgnoringCase_Throws()
    {
        var repos = CreateRepositories();
        await AddUserAsync(repos, "anna");

        var exception = await Assert.ThrowsAsync<DuplicateEntryException>(
            () => repos.Users.AddAsync(NewUser("berta", "ANNA@example.test")));

        Assert.Equal(nameof(User.Email), exception.Field);
    }

    [Fact]
    public async Task Add_UnknownRole_Throws()
    {
        var repos = CreateRepositories();

        await Assert.ThrowsAnyAsync<Exception>(() => repos.Users.AddAsync(NewUser(roleId: 9999)));
    }

    [Fact]
    public async Task ExistsByUsername_AndEmail_IgnoreCase()
    {
        var repos = CreateRepositories();
        await AddUserAsync(repos, "anna");

        Assert.True(await repos.Users.ExistsByUsernameAsync("ANNA"));
        Assert.True(await repos.Users.ExistsByEmailAsync("Anna@Example.Test"));
        Assert.False(await repos.Users.ExistsByUsernameAsync("berta"));
        Assert.False(await repos.Users.ExistsByEmailAsync("berta@example.test"));
    }

    [Fact]
    public async Task GetAll_OrdersByUsernameIgnoringCase()
    {
        var repos = CreateRepositories();
        await AddUserAsync(repos, "carla");
        await AddUserAsync(repos, "Anna");
        await AddUserAsync(repos, "berta");

        var users = await repos.Users.GetAllAsync();

        Assert.Equal(["Anna", "berta", "carla"], users.Select(u => u.Username));
        Assert.All(users, u => Assert.NotNull(u.Role));
    }

    [Fact]
    public async Task Update_SavesProfileFields()
    {
        var repos = CreateRepositories();
        var user = await AddUserAsync(repos);

        var edit = (await repos.Users.GetByIdAsync(user.UserId))!;
        edit.Email = "new@example.test";
        edit.RoleId = AdminRoleId;
        edit.PasswordHash = "new-hash";
        edit.IsActive = false;
        edit.MustChangePassword = true;
        await repos.Users.UpdateAsync(edit);
        var loaded = (await repos.Users.GetByIdAsync(user.UserId))!;

        Assert.Equal("new@example.test", loaded.Email);
        Assert.Equal(AdminRoleId, loaded.RoleId);
        Assert.Equal(DefaultRoles.Admin, loaded.Role?.Name);
        Assert.Equal("new-hash", loaded.PasswordHash);
        Assert.False(loaded.IsActive);
        Assert.True(loaded.MustChangePassword);
    }

    [Fact]
    public async Task Update_NeverChangesTheUsername()
    {
        var repos = CreateRepositories();
        var user = await AddUserAsync(repos, "anna");

        var edit = (await repos.Users.GetByIdAsync(user.UserId))!;
        edit.Username = "renamed";
        await repos.Users.UpdateAsync(edit);

        Assert.Equal("anna", (await repos.Users.GetByIdAsync(user.UserId))!.Username);
    }

    [Fact]
    public async Task Update_WithStaleCopy_DoesNotOverwriteTheFailedLoginCount()
    {
        var repos = CreateRepositories();
        var user = await AddUserAsync(repos);
        var stale = (await repos.Users.GetByIdAsync(user.UserId))!;

        await repos.Users.IncrementFailedLoginCountAsync(user.UserId);
        await repos.Users.LockOutAsync(user.UserId, Now.AddMinutes(15));
        await repos.Users.IncrementFailedLoginCountAsync(user.UserId);
        stale.IsActive = false;
        await repos.Users.UpdateAsync(stale);
        var loaded = (await repos.Users.GetByIdAsync(user.UserId))!;

        Assert.Equal(1, loaded.FailedLoginCount);
        Assert.Equal(Now.AddMinutes(15), loaded.LockoutEnd);
        Assert.False(loaded.IsActive);
    }

    [Fact]
    public async Task Update_DuplicateEmail_Throws_AndKeepingOwnEmailIsFine()
    {
        var repos = CreateRepositories();
        var anna = await AddUserAsync(repos, "anna");
        await AddUserAsync(repos, "berta");

        var edit = (await repos.Users.GetByIdAsync(anna.UserId))!;
        await repos.Users.UpdateAsync(edit);
        edit.Email = "BERTA@example.test";

        var exception = await Assert.ThrowsAsync<DuplicateEntryException>(() => repos.Users.UpdateAsync(edit));
        Assert.Equal(nameof(User.Email), exception.Field);
    }

    [Fact]
    public async Task Update_UnknownUser_Throws()
    {
        var repos = CreateRepositories();
        var ghost = NewUser();
        ghost.UserId = 9999;

        await Assert.ThrowsAsync<KeyNotFoundException>(() => repos.Users.UpdateAsync(ghost));
    }

    [Fact]
    public async Task CountActiveInRoles_CountsOnlyActiveUsersOfThoseRoles()
    {
        var repos = CreateRepositories();
        await AddUserAsync(repos, "admin1", AdminRoleId);
        var inactiveAdmin = await AddUserAsync(repos, "admin2", AdminRoleId);
        await AddUserAsync(repos, "member", UserRoleId);
        var edit = (await repos.Users.GetByIdAsync(inactiveAdmin.UserId))!;
        edit.IsActive = false;
        await repos.Users.UpdateAsync(edit);

        Assert.Equal(1, await repos.Users.CountActiveInRolesAsync([AdminRoleId]));
        Assert.Equal(2, await repos.Users.CountActiveInRolesAsync([AdminRoleId, UserRoleId]));
        Assert.Equal(0, await repos.Users.CountActiveInRolesAsync([]));
    }

    [Fact]
    public async Task IncrementFailedLoginCount_ReturnsTheNewValue_AndZeroForUnknownUser()
    {
        var repos = CreateRepositories();
        var user = await AddUserAsync(repos);

        Assert.Equal(1, await repos.Users.IncrementFailedLoginCountAsync(user.UserId));
        Assert.Equal(2, await repos.Users.IncrementFailedLoginCountAsync(user.UserId));
        Assert.Equal(2, (await repos.Users.GetByIdAsync(user.UserId))!.FailedLoginCount);
        Assert.Equal(0, await repos.Users.IncrementFailedLoginCountAsync(9999));
    }

    [Fact]
    public async Task IncrementFailedLoginCount_InParallel_LosesNoCounts()
    {
        const int attempts = 100;
        var repos = CreateRepositories();
        var user = await AddUserAsync(repos);

        var results = await Task.WhenAll(Enumerable.Range(0, attempts)
            .Select(_ => Task.Run(() => repos.Users.IncrementFailedLoginCountAsync(user.UserId))));

        Assert.Equal(Enumerable.Range(1, attempts), results.Order());
        Assert.Equal(attempts, (await repos.Users.GetByIdAsync(user.UserId))!.FailedLoginCount);
    }

    [Fact]
    public async Task LockOut_SetsLockoutEnd_AndResetsTheCounter()
    {
        var repos = CreateRepositories();
        var user = await AddUserAsync(repos);
        await repos.Users.IncrementFailedLoginCountAsync(user.UserId);

        await repos.Users.LockOutAsync(user.UserId, Now.AddMinutes(15));
        var loaded = (await repos.Users.GetByIdAsync(user.UserId))!;

        Assert.Equal(Now.AddMinutes(15), loaded.LockoutEnd);
        Assert.Equal(0, loaded.FailedLoginCount);
    }

    [Fact]
    public async Task RecordSuccessfulLogin_ResetsCounterAndLockout_AndSetsLastLoginAt()
    {
        var repos = CreateRepositories();
        var user = await AddUserAsync(repos);
        await repos.Users.IncrementFailedLoginCountAsync(user.UserId);
        await repos.Users.LockOutAsync(user.UserId, Now.AddMinutes(15));
        await repos.Users.IncrementFailedLoginCountAsync(user.UserId);

        await repos.Users.RecordSuccessfulLoginAsync(user.UserId, Now.AddHours(1));
        var loaded = (await repos.Users.GetByIdAsync(user.UserId))!;

        Assert.Equal(0, loaded.FailedLoginCount);
        Assert.Null(loaded.LockoutEnd);
        Assert.Equal(Now.AddHours(1), loaded.LastLoginAt);
    }

    [Fact]
    public async Task ClearLockout_ResetsCounterAndLockout_ButKeepsLastLoginAt()
    {
        var repos = CreateRepositories();
        var user = await AddUserAsync(repos);
        await repos.Users.RecordSuccessfulLoginAsync(user.UserId, Now);
        await repos.Users.IncrementFailedLoginCountAsync(user.UserId);
        await repos.Users.LockOutAsync(user.UserId, Now.AddMinutes(15));

        await repos.Users.ClearLockoutAsync(user.UserId);
        var loaded = (await repos.Users.GetByIdAsync(user.UserId))!;

        Assert.Equal(0, loaded.FailedLoginCount);
        Assert.Null(loaded.LockoutEnd);
        Assert.Equal(Now, loaded.LastLoginAt);
    }

    [Fact]
    public async Task CounterMethods_WithUnknownUser_DoNothing()
    {
        var repos = CreateRepositories();

        await repos.Users.LockOutAsync(9999, Now);
        await repos.Users.RecordSuccessfulLoginAsync(9999, Now);
        await repos.Users.ClearLockoutAsync(9999);

        Assert.Empty(await repos.Users.GetAllAsync());
    }
}
