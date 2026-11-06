using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Schuelerfirma.Api.Application.Auth;
using Schuelerfirma.Api.Tests.Http;
using Schuelerfirma.Api.Tests.Integration;

namespace Schuelerfirma.Api.Tests.Auth;

public class ChangePasswordTests : IDisposable
{
    private const string NewPassword = "Ein-ganz-neues-Passwort-2026";

    private readonly AuthApiFactory factory = new();

    public void Dispose() => factory.Dispose();

    private async Task<HttpClient> LoginAsync(string username, string password)
    {
        var client = factory.CreateHttpsClient();
        Assert.Equal(HttpStatusCode.NoContent, (await AuthApiFactory.LoginAsync(client, username, password)).StatusCode);
        return client;
    }

    private async Task<HttpClient> MemberAsync(bool mustChangePassword = false)
    {
        await factory.AddUserAsync("member", mustChangePassword: mustChangePassword);
        return await LoginAsync("member", AuthApiFactory.MemberPassword);
    }

    private static Task<HttpResponseMessage> ChangeAsync(HttpClient client, string currentPassword, string newPassword) =>
        client.PostAsJsonAsync("/api/auth/change-password", new { currentPassword, newPassword });

    private static async Task<string[]> ErrorsAsync(HttpResponseMessage response, string field)
    {
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        return [.. problem.GetProperty("errors").GetProperty(field).EnumerateArray().Select(e => e.GetString()!)];
    }

    private static async Task<HttpStatusCode> MeStatusAsync(HttpClient client) => (await client.GetAsync("/api/auth/me")).StatusCode;

    // ---- success ----

    [Fact]
    public async Task ASuccessfulChange_EndsAllSessions_IncludingTheCurrentOne()
    {
        var client = await MemberAsync();
        var otherDevice = await LoginAsync("member", AuthApiFactory.MemberPassword);

        var response = await ChangeAsync(client, AuthApiFactory.MemberPassword, NewPassword);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, await MeStatusAsync(client));
        Assert.Equal(HttpStatusCode.Unauthorized, await MeStatusAsync(otherDevice));
    }

    [Fact]
    public async Task AfterTheChange_OnlyTheNewPasswordWorks()
    {
        var client = await MemberAsync();
        await ChangeAsync(client, AuthApiFactory.MemberPassword, NewPassword);

        var withOld = await AuthApiFactory.LoginAsync(factory.CreateHttpsClient(), "member", AuthApiFactory.MemberPassword);
        var withNew = await AuthApiFactory.LoginAsync(client, "member", NewPassword);

        Assert.Equal(HttpStatusCode.Unauthorized, withOld.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, withNew.StatusCode);
        Assert.Equal(HttpStatusCode.OK, await MeStatusAsync(client));
    }

    [Fact]
    public async Task ASuccessfulChange_RemovesTheCookie_AndIssuesAnAnonymousCsrfToken()
    {
        var response = await ChangeAsync(await MemberAsync(), AuthApiFactory.MemberPassword, NewPassword);

        var cookies = response.Headers.GetValues("Set-Cookie").ToList();
        Assert.Contains(cookies, c => c.StartsWith("__Host-sf.session=;"));
        Assert.Contains(cookies, c => c.StartsWith("XSRF-TOKEN="));
        Assert.Contains("no-store", response.Headers.CacheControl?.ToString());
    }

    // ---- refused changes ----

    [Fact]
    public async Task AWrongCurrentPassword_Is400_OnTheField_AndKeepsTheSession()
    {
        var client = await MemberAsync();

        var response = await ChangeAsync(client, "wrong password!!", NewPassword);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(["Das aktuelle Passwort ist falsch."], await ErrorsAsync(response, "CurrentPassword"));
        Assert.Equal(HttpStatusCode.OK, await MeStatusAsync(client));
        Assert.Equal(1, (await factory.Users.GetByUsernameAsync("member"))!.FailedLoginCount);
    }

    [Fact]
    public async Task AWeakNewPassword_Is400_WithTheBrokenRule()
    {
        var response = await ChangeAsync(await MemberAsync(), AuthApiFactory.MemberPassword, "kurz");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Das Passwort muss mindestens 12 Zeichen lang sein.", await ErrorsAsync(response, "NewPassword"));
    }

    [Fact]
    public async Task TheCurrentPasswordAsNewPassword_IsRejected()
    {
        var response = await ChangeAsync(await MemberAsync(), AuthApiFactory.MemberPassword, AuthApiFactory.MemberPassword);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Das neue Passwort muss sich vom bisherigen unterscheiden.", await ErrorsAsync(response, "NewPassword"));
    }

    [Fact]
    public async Task MissingFields_Are400()
    {
        var client = await MemberAsync();

        var response = await client.PostAsJsonAsync("/api/auth/change-password", new { currentPassword = AuthApiFactory.MemberPassword });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task WithoutLogin_TheAnswerIs401()
    {
        var response = await ChangeAsync(factory.CreateHttpsClient(), AuthApiFactory.MemberPassword, NewPassword);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task FiveWrongCurrentPasswords_LockTheAccount_AndEndEverySession()
    {
        var client = await MemberAsync();
        var otherDevice = await LoginAsync("member", AuthApiFactory.MemberPassword);
        for (var i = 0; i < 4; i++)
        {
            Assert.Equal(HttpStatusCode.BadRequest, (await ChangeAsync(client, "wrong password!!", NewPassword)).StatusCode);
        }

        var fifth = await ChangeAsync(client, "wrong password!!", NewPassword);

        Assert.Equal(HttpStatusCode.Unauthorized, fifth.StatusCode);
        var problem = await fifth.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Konto gesperrt", problem.GetProperty("title").GetString());
        Assert.Equal(HttpStatusCode.Unauthorized, await MeStatusAsync(client));
        Assert.Equal(HttpStatusCode.Unauthorized, await MeStatusAsync(otherDevice));
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await AuthApiFactory.LoginAsync(factory.CreateHttpsClient(), "member", AuthApiFactory.MemberPassword)).StatusCode);
    }

    [Fact]
    public async Task OnAnAccountLockedFromTheLoginPage_TheRunningSessionCannotChangeThePassword()
    {
        var client = await MemberAsync();
        for (var i = 0; i < 5; i++)
        {
            await AuthApiFactory.LoginAsync(factory.CreateHttpsClient(), "member", "wrong password!!");
        }

        var response = await ChangeAsync(client, AuthApiFactory.MemberPassword, NewPassword);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("gesperrt", Assert.Single(await ErrorsAsync(response, "CurrentPassword")));
    }

    [Fact]
    public async Task Logs_NeverContainAPassword()
    {
        var client = await MemberAsync();
        await ChangeAsync(client, "Falsches-Passwort-im-Log-1", NewPassword);
        await ChangeAsync(client, AuthApiFactory.MemberPassword, NewPassword);

        var text = factory.Logs.AllText;
        Assert.DoesNotContain("Falsches-Passwort-im-Log-1", text);
        Assert.DoesNotContain(NewPassword, text);
        Assert.DoesNotContain(AuthApiFactory.MemberPassword, text);
        Assert.Contains("User member changed the password", text);
    }

    // ---- temporary password ----

    [Fact]
    public async Task WithATemporaryPassword_OnlyMeChangePasswordAndLogoutWork()
    {
        var client = await MemberAsync(mustChangePassword: true);

        // The role User has events.create, but not before the password is changed.
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/admin/permission-probe/events")).StatusCode);
        var me = await client.GetFromJsonAsync<JsonElement>("/api/auth/me");
        Assert.True(me.GetProperty("mustChangePassword").GetBoolean());
        Assert.Empty(me.GetProperty("permissions").EnumerateArray());
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync("/api/auth/logout", null)).StatusCode);
    }

    [Fact]
    public async Task AfterChangingTheTemporaryPassword_TheRolesPermissionsApply()
    {
        var client = await MemberAsync(mustChangePassword: true);

        Assert.Equal(HttpStatusCode.NoContent, (await ChangeAsync(client, AuthApiFactory.MemberPassword, NewPassword)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await AuthApiFactory.LoginAsync(client, "member", NewPassword)).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/admin/permission-probe/events")).StatusCode);
        var me = await client.GetFromJsonAsync<JsonElement>("/api/auth/me");
        Assert.False(me.GetProperty("mustChangePassword").GetBoolean());
        Assert.Contains(Permissions.EventsCreate, me.GetProperty("permissions").EnumerateArray().Select(p => p.GetString()));
    }

    [Fact]
    public async Task SettingTheFlagOnALoggedInUser_AppliesImmediately()
    {
        var client = await MemberAsync();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/admin/permission-probe/events")).StatusCode);

        var user = (await factory.Users.GetByUsernameAsync("member"))!;
        user.MustChangePassword = true;
        await factory.Users.UpdateAsync(user);

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/admin/permission-probe/events")).StatusCode);
    }

    [Fact]
    public async Task TheFirstAdministrator_MustChangeThePasswordFirst()
    {
        using var production = new AuthApiFactory { KeepBootstrapPasswordFlag = true };
        var client = production.CreateHttpsClient();
        await AuthApiFactory.LoginAsync(client, AuthApiFactory.AdminUsername, AuthApiFactory.AdminPassword);

        var me = await client.GetFromJsonAsync<JsonElement>("/api/auth/me");

        Assert.True(me.GetProperty("mustChangePassword").GetBoolean());
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/admin/permission-probe/users")).StatusCode);
    }

    // ---- protections from #33 ----

    [Fact]
    public async Task WithoutLoginOrWithAWrongCsrfToken_TheChangeIsRefused()
    {
        var client = factory.CreateHttpsClient(sendCsrfToken: false);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/change-password")
        {
            Content = JsonContent.Create(new { currentPassword = AuthApiFactory.AdminPassword, newPassword = NewPassword }),
        };

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.SendAsync(request)).StatusCode);
        var loggedIn = await LoginAsync(AuthApiFactory.AdminUsername, AuthApiFactory.AdminPassword);
        var withoutToken = new HttpRequestMessage(HttpMethod.Post, "/api/auth/change-password")
        {
            Content = JsonContent.Create(new { currentPassword = AuthApiFactory.AdminPassword, newPassword = NewPassword }),
        };
        withoutToken.Headers.Add("X-XSRF-TOKEN", "not-a-token");

        Assert.Equal(HttpStatusCode.BadRequest, (await loggedIn.SendAsync(withoutToken)).StatusCode);
    }
}

public class ChangePasswordRateLimitTests : IDisposable
{
    private readonly RateLimitApiFactory factory = new();

    public void Dispose() => factory.Dispose();

    [Fact]
    public async Task ChangePassword_SharesTheAuthLimit()
    {
        var client = factory.CreateHttpsClient();
        Assert.Equal(HttpStatusCode.NoContent, (await AuthApiFactory.LoginAsync(client, AuthApiFactory.AdminUsername, AuthApiFactory.AdminPassword)).StatusCode);

        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 6; i++)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/change-password")
            {
                Content = JsonContent.Create(new { currentPassword = "kurz", newPassword = "kurz" }),
            };
            request.Headers.Add(TestRemoteIpStartupFilter.Header, "203.0.113.90");
            statuses.Add((await client.SendAsync(request)).StatusCode);
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, statuses[5]);
    }
}
