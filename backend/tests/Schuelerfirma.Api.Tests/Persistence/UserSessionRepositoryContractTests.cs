using Schuelerfirma.Api.Domain;

namespace Schuelerfirma.Api.Tests.Persistence;

public abstract class UserSessionRepositoryContractTests : RepositoryContractTests
{
    private static string Hash(char c) => new(c, UserSession.SessionKeyHashLength);

    private static UserSession NewSession(int userId, char key = 'a', DateTime? expiresAt = null) => new()
    {
        SessionKeyHash = Hash(key),
        UserId = userId,
        Ticket = [1, 2, 3],
        CreatedAt = Now,
        ExpiresAt = expiresAt ?? Now.AddMinutes(30),
    };

    [Fact]
    public async Task Add_ThenGet_ReturnsTheSession()
    {
        var repos = CreateRepositories();
        var user = await AddUserAsync(repos);

        await repos.Sessions.AddAsync(NewSession(user.UserId));
        var loaded = await repos.Sessions.GetAsync(Hash('a'));

        Assert.NotNull(loaded);
        Assert.Equal(user.UserId, loaded.UserId);
        Assert.Equal([1, 2, 3], loaded.Ticket);
        Assert.Equal(Now, loaded.CreatedAt);
        Assert.Equal(Now.AddMinutes(30), loaded.ExpiresAt);
    }

    [Fact]
    public async Task Get_Unknown_ReturnsNull()
    {
        Assert.Null(await CreateRepositories().Sessions.GetAsync(Hash('z')));
    }

    [Fact]
    public async Task Get_ReturnsExpiredSessionsToo()
    {
        var repos = CreateRepositories();
        var user = await AddUserAsync(repos);
        await repos.Sessions.AddAsync(NewSession(user.UserId, expiresAt: Now.AddDays(-1)));

        Assert.NotNull(await repos.Sessions.GetAsync(Hash('a')));
    }

    [Fact]
    public async Task ReturnedSessions_AreCopies()
    {
        var repos = CreateRepositories();
        var user = await AddUserAsync(repos);
        await repos.Sessions.AddAsync(NewSession(user.UserId));

        var first = (await repos.Sessions.GetAsync(Hash('a')))!;
        first.Ticket[0] = 99;
        first.ExpiresAt = Now.AddYears(1);
        var second = (await repos.Sessions.GetAsync(Hash('a')))!;

        Assert.Equal([1, 2, 3], second.Ticket);
        Assert.Equal(Now.AddMinutes(30), second.ExpiresAt);
    }

    [Fact]
    public async Task Add_DuplicateKeyHash_Throws()
    {
        var repos = CreateRepositories();
        var user = await AddUserAsync(repos);
        await repos.Sessions.AddAsync(NewSession(user.UserId));

        await Assert.ThrowsAnyAsync<Exception>(() => repos.Sessions.AddAsync(NewSession(user.UserId)));
    }

    [Fact]
    public async Task Add_UnknownUser_Throws()
    {
        var repos = CreateRepositories();

        await Assert.ThrowsAnyAsync<Exception>(() => repos.Sessions.AddAsync(NewSession(9999)));
    }

    [Fact]
    public async Task Update_SavesTicketAndExpiry()
    {
        var repos = CreateRepositories();
        var user = await AddUserAsync(repos);
        await repos.Sessions.AddAsync(NewSession(user.UserId));

        var edit = (await repos.Sessions.GetAsync(Hash('a')))!;
        edit.Ticket = [9, 8];
        edit.ExpiresAt = Now.AddHours(1);
        await repos.Sessions.UpdateAsync(edit);
        var loaded = (await repos.Sessions.GetAsync(Hash('a')))!;

        Assert.Equal([9, 8], loaded.Ticket);
        Assert.Equal(Now.AddHours(1), loaded.ExpiresAt);
    }

    [Fact]
    public async Task Update_OfADeletedSession_DoesNotBringItBack()
    {
        var repos = CreateRepositories();
        var user = await AddUserAsync(repos);
        await repos.Sessions.AddAsync(NewSession(user.UserId));
        var stale = (await repos.Sessions.GetAsync(Hash('a')))!;
        await repos.Sessions.DeleteAsync(Hash('a'));

        await repos.Sessions.UpdateAsync(stale);

        Assert.Null(await repos.Sessions.GetAsync(Hash('a')));
    }

    [Fact]
    public async Task Delete_RemovesTheSession_AndIsIdempotent()
    {
        var repos = CreateRepositories();
        var user = await AddUserAsync(repos);
        await repos.Sessions.AddAsync(NewSession(user.UserId));

        await repos.Sessions.DeleteAsync(Hash('a'));
        await repos.Sessions.DeleteAsync(Hash('a'));

        Assert.Null(await repos.Sessions.GetAsync(Hash('a')));
    }

    [Fact]
    public async Task DeleteAllForUser_RemovesOnlyThatUsersSessions()
    {
        var repos = CreateRepositories();
        var anna = await AddUserAsync(repos, "anna");
        var berta = await AddUserAsync(repos, "berta");
        await repos.Sessions.AddAsync(NewSession(anna.UserId, 'a'));
        await repos.Sessions.AddAsync(NewSession(anna.UserId, 'b'));
        await repos.Sessions.AddAsync(NewSession(berta.UserId, 'c'));

        var deleted = await repos.Sessions.DeleteAllForUserAsync(anna.UserId);

        Assert.Equal(2, deleted);
        Assert.Null(await repos.Sessions.GetAsync(Hash('a')));
        Assert.Null(await repos.Sessions.GetAsync(Hash('b')));
        Assert.NotNull(await repos.Sessions.GetAsync(Hash('c')));
    }

    [Fact]
    public async Task DeleteAllForUser_CanKeepOneSession()
    {
        var repos = CreateRepositories();
        var anna = await AddUserAsync(repos, "anna");
        await repos.Sessions.AddAsync(NewSession(anna.UserId, 'a'));
        await repos.Sessions.AddAsync(NewSession(anna.UserId, 'b'));

        var deleted = await repos.Sessions.DeleteAllForUserAsync(anna.UserId, Hash('b'));

        Assert.Equal(1, deleted);
        Assert.Null(await repos.Sessions.GetAsync(Hash('a')));
        Assert.NotNull(await repos.Sessions.GetAsync(Hash('b')));
    }

    [Fact]
    public async Task DeleteExpired_RemovesSessionsExpiringAtOrBeforeNow()
    {
        var repos = CreateRepositories();
        var user = await AddUserAsync(repos);
        await repos.Sessions.AddAsync(NewSession(user.UserId, 'a', Now.AddSeconds(-1)));
        await repos.Sessions.AddAsync(NewSession(user.UserId, 'b', Now));
        await repos.Sessions.AddAsync(NewSession(user.UserId, 'c', Now.AddSeconds(1)));

        var deleted = await repos.Sessions.DeleteExpiredAsync(Now);

        Assert.Equal(2, deleted);
        Assert.Null(await repos.Sessions.GetAsync(Hash('a')));
        Assert.Null(await repos.Sessions.GetAsync(Hash('b')));
        Assert.NotNull(await repos.Sessions.GetAsync(Hash('c')));
    }
}
