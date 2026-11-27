using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Schuelerfirma.Api.Application.Auth;
using Schuelerfirma.Api.Domain;
using Schuelerfirma.Api.Tests.Auth;
using Schuelerfirma.Api.Tests.Integration;

namespace Schuelerfirma.Api.Tests.Users;

public class UsersApiTests : IDisposable
{
    private const int AdminRole = 1;
    private const int UserRole = 2;
    private const string NewPassword = "Ein-eigenes-neues-Passwort-9";

    private readonly AuthApiFactory factory = new();

    public void Dispose() => factory.Dispose();

    private async Task<HttpClient> LoginAsync(string username, string password)
    {
        var client = factory.CreateHttpsClient();
        Assert.Equal(HttpStatusCode.NoContent, (await AuthApiFactory.LoginAsync(client, username, password)).StatusCode);
        return client;
    }

    private Task<HttpClient> AdminAsync() => LoginAsync(AuthApiFactory.AdminUsername, AuthApiFactory.AdminPassword);

    private async Task<int> AdminIdAsync() => (await factory.Users.GetByUsernameAsync(AuthApiFactory.AdminUsername))!.UserId;

    private static Task<HttpResponseMessage> CreateAsync(HttpClient client, string username, string email, int roleId = UserRole) =>
        client.PostAsJsonAsync("/api/admin/users", new { username, email, roleId });

    private static Task<HttpResponseMessage> UpdateAsync(HttpClient client, int id, string email, int roleId, bool isActive) =>
        client.PutAsJsonAsync($"/api/admin/users/{id}", new { email, roleId, isActive });

    private static async Task<JsonElement> JsonAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>();

    // ---- access ----

    [Theory]
    [InlineData("GET", "/api/admin/users")]
    [InlineData("GET", "/api/admin/users/1")]
    [InlineData("POST", "/api/admin/users")]
    [InlineData("PUT", "/api/admin/users/1")]
    [InlineData("POST", "/api/admin/users/1/reset-password")]
    [InlineData("POST", "/api/admin/users/1/revoke-sessions")]
    [InlineData("GET", "/api/admin/roles")]
    public async Task TheRoleUser_GetsA403_AndAnonymousClientsA401(string method, string path)
    {
        await factory.AddUserAsync("member");
        var member = await LoginAsync("member", AuthApiFactory.MemberPassword);

        var asMember = await member.SendAsync(new HttpRequestMessage(new HttpMethod(method), path) { Content = JsonContent.Create(new { }) });
        var anonymous = await factory.CreateHttpsClient().SendAsync(new HttpRequestMessage(new HttpMethod(method), path) { Content = JsonContent.Create(new { }) });

        Assert.Equal(HttpStatusCode.Forbidden, asMember.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
    }

    // ---- read ----

    [Fact]
    public async Task TheList_ContainsTheUsers_WithoutPasswordHashes()
    {
        await factory.AddUserAsync("member");

        var response = await (await AdminAsync()).GetAsync("/api/admin/users");
        var body = await response.Content.ReadAsStringAsync();
        var list = JsonDocument.Parse(body).RootElement;

        Assert.Equal(["admin", "member"], list.EnumerateArray().Select(u => u.GetProperty("username").GetString()));
        Assert.DoesNotContain("passwordHash", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("AQAAAA", body); // the prefix of every hash of PasswordHasher
        Assert.Contains("no-store", response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task OneUser_ShowsRoleAndLockout_AndUnknownIdsAre404()
    {
        var member = await factory.AddUserAsync("member");
        await factory.Users.LockOutAsync(member.UserId, factory.Time.GetUtcNow().UtcDateTime.AddMinutes(10));
        var admin = await AdminAsync();

        var json = await admin.GetFromJsonAsync<JsonElement>($"/api/admin/users/{member.UserId}");

        Assert.Equal(DefaultRoles.User, json.GetProperty("role").GetString());
        Assert.Equal(JsonValueKind.String, json.GetProperty("lockedUntil").ValueKind);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync("/api/admin/users/4711")).StatusCode);
    }

    [Fact]
    public async Task TheRoles_AreListedWithTheirPermissions()
    {
        var roles = await (await AdminAsync()).GetFromJsonAsync<JsonElement>("/api/admin/roles");

        var user = roles.EnumerateArray().Single(r => r.GetProperty("name").GetString() == DefaultRoles.User);
        var admin = roles.EnumerateArray().Single(r => r.GetProperty("name").GetString() == DefaultRoles.Admin);
        Assert.Contains(Permissions.EventsCreate, user.GetProperty("permissions").EnumerateArray().Select(p => p.GetString()));
        Assert.Equal(Permissions.All.Count, admin.GetProperty("permissions").GetArrayLength());
    }

    // ---- create ----

    [Fact]
    public async Task ANewUser_LogsInWithTheTemporaryPassword_MustChangeIt_AndThenWorksWithTheRole()
    {
        var created = await CreateAsync(await AdminAsync(), "neu.mitglied", "neu@example.test");
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal("no-store", created.Headers.CacheControl?.ToString().Split(',')[0].Trim());
        var body = await JsonAsync(created);
        var temporaryPassword = body.GetProperty("temporaryPassword").GetString()!;
        Assert.True(body.GetProperty("user").GetProperty("mustChangePassword").GetBoolean());
        Assert.EndsWith($"/api/admin/users/{body.GetProperty("user").GetProperty("userId").GetInt32()}", created.Headers.Location?.ToString());

        var member = await LoginAsync("neu.mitglied", temporaryPassword);
        Assert.Equal(HttpStatusCode.Forbidden, (await member.GetAsync("/api/admin/permission-probe/events")).StatusCode);
        Assert.Equal(
            HttpStatusCode.NoContent,
            (await member.PostAsJsonAsync("/api/auth/change-password", new { currentPassword = temporaryPassword, newPassword = NewPassword })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await AuthApiFactory.LoginAsync(member, "neu.mitglied", NewPassword)).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await member.GetAsync("/api/admin/permission-probe/events")).StatusCode);
    }

    [Fact]
    public async Task InvalidInput_Is400_WithTheField()
    {
        var admin = await AdminAsync();

        var badName = await CreateAsync(admin, "Mit Grossbuchstaben", "x@example.test");
        var noRole = await admin.PostAsJsonAsync("/api/admin/users", new { username = "anna", email = "anna@example.test" });

        Assert.Equal(HttpStatusCode.BadRequest, badName.StatusCode);
        Assert.True((await JsonAsync(badName)).GetProperty("errors").TryGetProperty("Username", out _));
        Assert.Equal(HttpStatusCode.BadRequest, noRole.StatusCode);
    }

    [Fact]
    public async Task ADuplicateUsername_Is409()
    {
        var admin = await AdminAsync();
        await CreateAsync(admin, "anna", "anna@example.test");

        var duplicate = await CreateAsync(admin, "anna", "andere@example.test");

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal("Dieser Benutzername ist bereits vergeben.", (await JsonAsync(duplicate)).GetProperty("detail").GetString());
    }

    // ---- update ----

    [Fact]
    public async Task ADeactivatedUser_LosesTheRunningSessionImmediately()
    {
        var user = await factory.AddUserAsync("member");
        var member = await LoginAsync("member", AuthApiFactory.MemberPassword);
        Assert.Equal(HttpStatusCode.OK, (await member.GetAsync("/api/auth/me")).StatusCode);

        var response = await UpdateAsync(await AdminAsync(), user.UserId, user.Email, UserRole, false);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False((await JsonAsync(response)).GetProperty("isActive").GetBoolean());
        Assert.Equal(HttpStatusCode.Unauthorized, (await member.GetAsync("/api/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await AuthApiFactory.LoginAsync(factory.CreateHttpsClient(), "member", AuthApiFactory.MemberPassword)).StatusCode);
    }

    [Fact]
    public async Task ARoleChange_EndsTheSessionsOfTheUser()
    {
        var user = await factory.AddUserAsync("member");
        var member = await LoginAsync("member", AuthApiFactory.MemberPassword);

        await UpdateAsync(await AdminAsync(), user.UserId, user.Email, AdminRole, true);

        Assert.Equal(HttpStatusCode.Unauthorized, (await member.GetAsync("/api/auth/me")).StatusCode);
    }

    [Fact]
    public async Task TheOwnAccount_CannotBeDeactivatedOrDemoted()
    {
        var admin = await AdminAsync();
        var id = await AdminIdAsync();

        var deactivate = await UpdateAsync(admin, id, AuthApiFactory.AdminEmail, AdminRole, false);
        var demote = await UpdateAsync(admin, id, AuthApiFactory.AdminEmail, UserRole, true);

        Assert.Equal(HttpStatusCode.Conflict, deactivate.StatusCode);
        Assert.Equal("Das eigene Konto kann nicht deaktiviert werden.", (await JsonAsync(deactivate)).GetProperty("detail").GetString());
        Assert.Equal(HttpStatusCode.Conflict, demote.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/auth/me")).StatusCode);
    }

    [Fact]
    public async Task UpdatingAnUnknownUser_Is404()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await UpdateAsync(await AdminAsync(), 4711, "x@example.test", UserRole, true)).StatusCode);
    }

    // ---- reset password and sessions ----

    [Fact]
    public async Task ResetPassword_ReplacesThePassword_AndTheUserMustChangeItAgain()
    {
        var user = await factory.AddUserAsync("member");
        var member = await LoginAsync("member", AuthApiFactory.MemberPassword);

        var response = await (await AdminAsync()).PostAsync($"/api/admin/users/{user.UserId}/reset-password", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await JsonAsync(response);
        Assert.Equal(1, body.GetProperty("endedSessions").GetInt32());
        Assert.Equal(HttpStatusCode.Unauthorized, (await member.GetAsync("/api/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await AuthApiFactory.LoginAsync(factory.CreateHttpsClient(), "member", AuthApiFactory.MemberPassword)).StatusCode);
        var again = await LoginAsync("member", body.GetProperty("temporaryPassword").GetString()!);
        Assert.True((await again.GetFromJsonAsync<JsonElement>("/api/auth/me")).GetProperty("mustChangePassword").GetBoolean());
    }

    [Fact]
    public async Task TheOwnPassword_CannotBeResetHere()
    {
        var response = await (await AdminAsync()).PostAsync($"/api/admin/users/{await AdminIdAsync()}/reset-password", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await AuthApiFactory.LoginAsync(factory.CreateHttpsClient(), AuthApiFactory.AdminUsername, AuthApiFactory.AdminPassword)).StatusCode);
    }

    [Fact]
    public async Task RevokeSessions_LogsTheUserOutEverywhere()
    {
        var user = await factory.AddUserAsync("member");
        var laptop = await LoginAsync("member", AuthApiFactory.MemberPassword);
        var phone = await LoginAsync("member", AuthApiFactory.MemberPassword);

        var response = await (await AdminAsync()).PostAsync($"/api/admin/users/{user.UserId}/revoke-sessions", null);

        Assert.Equal(2, (await JsonAsync(response)).GetProperty("endedSessions").GetInt32());
        Assert.Equal(HttpStatusCode.Unauthorized, (await laptop.GetAsync("/api/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await phone.GetAsync("/api/auth/me")).StatusCode);
        // The account itself stays usable. Like the frontend, the browser fetches a fresh CSRF token before the login:
        // the old one belonged to the ended session.
        await laptop.GetAsync("/api/auth/csrf");
        Assert.Equal(HttpStatusCode.NoContent, (await AuthApiFactory.LoginAsync(laptop, "member", AuthApiFactory.MemberPassword)).StatusCode);
    }
}
