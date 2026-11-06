using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Schuelerfirma.Api.Domain;
using Schuelerfirma.Api.Tests.Integration;

namespace Schuelerfirma.Api.Tests.Auth;

public class LoginTests : IDisposable
{
    private const string CookieName = "__Host-sf.session";

    private readonly AuthApiFactory factory = new();

    public void Dispose() => factory.Dispose();

    private static async Task<(HttpStatusCode Status, string? Title, string? Detail)> Summarize(HttpResponseMessage response)
    {
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return (response.StatusCode, json.GetProperty("title").GetString(), json.GetProperty("detail").GetString());
    }

    private async Task<HttpClient> LoggedInClientAsync(string username = AuthApiFactory.AdminUsername, string password = AuthApiFactory.AdminPassword)
    {
        var client = factory.CreateHttpsClient();
        var response = await AuthApiFactory.LoginAsync(client, username, password);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        return client;
    }

    private static async Task<HttpStatusCode> MeStatusAsync(HttpClient client) =>
        (await client.GetAsync("/api/auth/me")).StatusCode;

    // ---- login and cookie ----

    [Fact]
    public async Task Login_WithCorrectCredentials_ReturnsNoContent_AndLetsTheUserIn()
    {
        var client = await LoggedInClientAsync();

        var me = await client.GetFromJsonAsync<JsonElement>("/api/auth/me");

        Assert.Equal(AuthApiFactory.AdminUsername, me.GetProperty("username").GetString());
        Assert.Equal(AuthApiFactory.AdminEmail, me.GetProperty("email").GetString());
        Assert.Equal(DefaultRoles.Admin, me.GetProperty("role").GetString());
        Assert.True(me.GetProperty("userId").GetInt32() > 0);
        Assert.False(me.TryGetProperty("passwordHash", out _));
    }

    [Fact]
    public async Task TheSessionCookie_HasTheRequiredFlags()
    {
        var response = await AuthApiFactory.LoginAsync(factory.CreateHttpsClient(), AuthApiFactory.AdminUsername, AuthApiFactory.AdminPassword);

        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"));
        var lower = cookie.ToLowerInvariant();
        Assert.StartsWith(CookieName + "=", cookie);
        Assert.Contains("httponly", lower);
        Assert.Contains("secure", lower);
        Assert.Contains("samesite=strict", lower);
        Assert.Contains("path=/", lower);
        Assert.DoesNotContain("domain=", lower);
        // A session cookie: it disappears when the browser is closed.
        Assert.DoesNotContain("expires=", lower);
        Assert.DoesNotContain("max-age=", lower);
    }

    [Fact]
    public async Task TheCookie_DoesNotContainReadableUserData()
    {
        var response = await AuthApiFactory.LoginAsync(factory.CreateHttpsClient(), AuthApiFactory.AdminUsername, AuthApiFactory.AdminPassword);

        var value = Assert.Single(response.Headers.GetValues("Set-Cookie"));

        Assert.DoesNotContain(AuthApiFactory.AdminUsername, value, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(AuthApiFactory.AdminEmail, value, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LoginResponses_AreNotCacheable()
    {
        var response = await AuthApiFactory.LoginAsync(factory.CreateHttpsClient(), AuthApiFactory.AdminUsername, "wrong");

        Assert.Contains("no-store", response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task Login_WithMissingOrOversizedFields_ReturnsBadRequest()
    {
        var client = factory.CreateHttpsClient();

        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/auth/login", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/auth/login", new { username = "admin" })).StatusCode);
        Assert.Equal(
            HttpStatusCode.BadRequest,
            (await AuthApiFactory.LoginAsync(client, new string('x', 300), "irrelevant-password")).StatusCode);
    }

    // ---- identical failures ----

    [Fact]
    public async Task UnknownUser_WrongPassword_LockedAndDeactivated_AreAnsweredIdentically()
    {
        await factory.AddUserAsync("inactive", active: false);
        await factory.AddUserAsync("locked");
        for (var i = 0; i < 5; i++)
        {
            await AuthApiFactory.LoginAsync(factory.CreateHttpsClient(), "locked", "wrong password!!");
        }

        var responses = new[]
        {
            await AuthApiFactory.LoginAsync(factory.CreateHttpsClient(), "nobody", "wrong password!!"),
            await AuthApiFactory.LoginAsync(factory.CreateHttpsClient(), AuthApiFactory.AdminUsername, "wrong password!!"),
            await AuthApiFactory.LoginAsync(factory.CreateHttpsClient(), "locked", AuthApiFactory.MemberPassword),
            await AuthApiFactory.LoginAsync(factory.CreateHttpsClient(), "inactive", AuthApiFactory.MemberPassword),
        };

        var summaries = new List<(HttpStatusCode, string?, string?)>();
        foreach (var response in responses)
        {
            summaries.Add(await Summarize(response));
            Assert.False(response.Headers.Contains("Set-Cookie"));
        }

        Assert.All(summaries, s => Assert.Equal(HttpStatusCode.Unauthorized, s.Item1));
        Assert.Single(summaries.Distinct());
    }

    // ---- lockout ----

    [Fact]
    public async Task After5WrongPasswords_EvenTheCorrectPasswordIsRefused_Until15MinutesHavePassed()
    {
        for (var i = 0; i < 5; i++)
        {
            await AuthApiFactory.LoginAsync(factory.CreateHttpsClient(), AuthApiFactory.AdminUsername, "wrong password!!");
        }

        var locked = await AuthApiFactory.LoginAsync(factory.CreateHttpsClient(), AuthApiFactory.AdminUsername, AuthApiFactory.AdminPassword);
        factory.Time.Advance(TimeSpan.FromMinutes(15) - TimeSpan.FromSeconds(1));
        var stillLocked = await AuthApiFactory.LoginAsync(factory.CreateHttpsClient(), AuthApiFactory.AdminUsername, AuthApiFactory.AdminPassword);
        factory.Time.Advance(TimeSpan.FromSeconds(1));
        var open = await AuthApiFactory.LoginAsync(factory.CreateHttpsClient(), AuthApiFactory.AdminUsername, AuthApiFactory.AdminPassword);

        Assert.Equal(HttpStatusCode.Unauthorized, locked.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, stillLocked.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, open.StatusCode);
    }

    [Fact]
    public async Task FourWrongPasswords_DoNotLock()
    {
        for (var i = 0; i < 4; i++)
        {
            await AuthApiFactory.LoginAsync(factory.CreateHttpsClient(), AuthApiFactory.AdminUsername, "wrong password!!");
        }

        var response = await AuthApiFactory.LoginAsync(factory.CreateHttpsClient(), AuthApiFactory.AdminUsername, AuthApiFactory.AdminPassword);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    // ---- not logged in ----

    [Fact]
    public async Task Me_WithoutLogin_Returns401_NotARedirect()
    {
        var response = await factory.CreateHttpsClient().GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Null(response.Headers.Location);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task EndpointsWithoutAttributes_RequireLogin_UnlessExplicitlyAnonymous()
    {
        var anonymous = factory.CreateHttpsClient();
        var loggedIn = await LoggedInClientAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/test/probe/protected")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await anonymous.GetAsync("/test/probe/open")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await loggedIn.GetAsync("/test/probe/protected")).StatusCode);
    }

    [Fact]
    public async Task UnknownEndpoints_AreHiddenFromAnonymousClients_ButNotFoundForUsers()
    {
        var loggedIn = await LoggedInClientAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, (await factory.CreateHttpsClient().GetAsync("/api/nothing-here")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await loggedIn.GetAsync("/api/nothing-here")).StatusCode);
    }

    // ---- logout ----

    [Fact]
    public async Task Logout_EndsTheSession_EvenForACopyOfTheOldCookie()
    {
        var client = factory.CreateHttpsClient();
        var cookie = ExtractCookieHeader(await AuthApiFactory.LoginAsync(client, AuthApiFactory.AdminUsername, AuthApiFactory.AdminPassword));
        var replayBeforeLogout = await MeWithCookieAsync(cookie);

        var logout = await client.PostAsync("/api/auth/logout", null);
        var replayAfterLogout = await MeWithCookieAsync(cookie);

        Assert.Equal(HttpStatusCode.OK, replayBeforeLogout);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        // Deleting the cookie in the browser is not enough: the copied value is dead on the server, too.
        Assert.Equal(HttpStatusCode.Unauthorized, replayAfterLogout);
        Assert.Equal(HttpStatusCode.Unauthorized, await MeStatusAsync(client));
    }

    [Fact]
    public async Task Logout_OnlyEndsTheOwnSession()
    {
        var first = await LoggedInClientAsync();
        var second = await LoggedInClientAsync();

        await first.PostAsync("/api/auth/logout", null);

        Assert.Equal(HttpStatusCode.Unauthorized, await MeStatusAsync(first));
        Assert.Equal(HttpStatusCode.OK, await MeStatusAsync(second));
    }

    [Fact]
    public async Task LoggingInAgain_StartsANewSession_AndEndsTheOldOne()
    {
        var client = factory.CreateHttpsClient();
        var firstCookie = ExtractCookieHeader(await AuthApiFactory.LoginAsync(client, AuthApiFactory.AdminUsername, AuthApiFactory.AdminPassword));

        // The same browser logs in again; the session key must change (no session fixation).
        var again = await AuthApiFactory.LoginAsync(client, AuthApiFactory.AdminUsername, AuthApiFactory.AdminPassword);
        var secondCookie = ExtractCookieHeader(again);

        Assert.Equal(HttpStatusCode.NoContent, again.StatusCode);
        Assert.NotEqual(firstCookie, secondCookie);
        Assert.Equal(HttpStatusCode.Unauthorized, await MeWithCookieAsync(firstCookie));
        Assert.Equal(HttpStatusCode.OK, await MeWithCookieAsync(secondCookie));
    }

    [Fact]
    public async Task ANewUserLoggingInOnTheSameBrowser_NeverInheritsThePreviousSession()
    {
        await factory.AddUserAsync("member");
        var browser = factory.CreateHttpsClient();
        var adminCookie = ExtractCookieHeader(await AuthApiFactory.LoginAsync(browser, AuthApiFactory.AdminUsername, AuthApiFactory.AdminPassword));

        var memberLogin = await AuthApiFactory.LoginAsync(browser, "member", AuthApiFactory.MemberPassword);
        var memberCookie = ExtractCookieHeader(memberLogin);

        // The admin's copied cookie must not turn into the member's session (or stay an admin session).
        Assert.Equal(HttpStatusCode.Unauthorized, await MeWithCookieAsync(adminCookie));
        Assert.Equal(HttpStatusCode.OK, await MeWithCookieAsync(memberCookie));
        Assert.Equal("member", (await browser.GetFromJsonAsync<JsonElement>("/api/auth/me")).GetProperty("username").GetString());
    }

    [Fact]
    public async Task ALoginWithAForgedCookie_StillWorks()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(new { username = AuthApiFactory.AdminUsername, password = AuthApiFactory.AdminPassword }),
        };
        request.Headers.Add("Cookie", $"{CookieName}=forged-value; other=1");

        var response = await factory.CreateDefaultClient(new Uri(WebApplicationFactoryExtensions.Host)).SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, await MeWithCookieAsync(ExtractCookieHeader(response)));
    }

    // ---- session follows the user ----

    [Fact]
    public async Task ADeactivatedUser_LosesTheRunningSession_Immediately()
    {
        var user = await factory.AddUserAsync("member");
        var client = await LoggedInClientAsync("member", AuthApiFactory.MemberPassword);
        Assert.Equal(HttpStatusCode.OK, await MeStatusAsync(client));

        var edit = (await factory.Users.GetByIdAsync(user.UserId))!;
        edit.IsActive = false;
        await factory.Users.UpdateAsync(edit);

        Assert.Equal(HttpStatusCode.Unauthorized, await MeStatusAsync(client));
        // The session was also removed from storage, not just ignored.
        Assert.Equal(0, await factory.Sessions.DeleteAllForUserAsync(user.UserId));
    }

    [Fact]
    public async Task ARoleChange_AppliesImmediately()
    {
        var user = await factory.AddUserAsync("member");
        var client = await LoggedInClientAsync("member", AuthApiFactory.MemberPassword);
        Assert.Equal(DefaultRoles.User, (await client.GetFromJsonAsync<JsonElement>("/api/auth/me")).GetProperty("role").GetString());

        var edit = (await factory.Users.GetByIdAsync(user.UserId))!;
        edit.RoleId = 1;
        await factory.Users.UpdateAsync(edit);

        Assert.Equal(DefaultRoles.Admin, (await client.GetFromJsonAsync<JsonElement>("/api/auth/me")).GetProperty("role").GetString());
    }

    // ---- session lifetime ----

    [Fact]
    public async Task ASession_EndsAfter30MinutesWithoutActivity()
    {
        var client = await LoggedInClientAsync();

        factory.Time.Advance(TimeSpan.FromMinutes(29));
        Assert.Equal(HttpStatusCode.OK, await MeStatusAsync(client));
        factory.Time.Advance(TimeSpan.FromMinutes(31));

        Assert.Equal(HttpStatusCode.Unauthorized, await MeStatusAsync(client));
    }

    [Fact]
    public async Task ASession_IsExtendedByActivity()
    {
        var client = await LoggedInClientAsync();

        // 20 + 20 minutes in total are more than the 30-minute timeout; only activity can explain this.
        factory.Time.Advance(TimeSpan.FromMinutes(20));
        Assert.Equal(HttpStatusCode.OK, await MeStatusAsync(client));
        factory.Time.Advance(TimeSpan.FromMinutes(20));
        Assert.Equal(HttpStatusCode.OK, await MeStatusAsync(client));
    }

    [Fact]
    public async Task ASession_EndsAfter8Hours_NoMatterHowActive()
    {
        var client = await LoggedInClientAsync();

        for (var minutes = 20; minutes < 8 * 60; minutes += 20)
        {
            factory.Time.Advance(TimeSpan.FromMinutes(20));
            Assert.Equal(HttpStatusCode.OK, await MeStatusAsync(client));
        }

        factory.Time.Advance(TimeSpan.FromMinutes(20));
        Assert.Equal(HttpStatusCode.Unauthorized, await MeStatusAsync(client));
    }

    // ---- logging ----

    [Fact]
    public async Task Logs_ContainTheUsernameButNeverAPassword()
    {
        const string secret = "Geheim-Passwort-nicht-loggen-42";
        await AuthApiFactory.LoginAsync(factory.CreateHttpsClient(), "nobody", secret);
        await AuthApiFactory.LoginAsync(factory.CreateHttpsClient(), AuthApiFactory.AdminUsername, secret);
        await AuthApiFactory.LoginAsync(factory.CreateHttpsClient(), AuthApiFactory.AdminUsername, AuthApiFactory.AdminPassword);

        var text = factory.Logs.AllText;

        Assert.DoesNotContain(secret, text);
        Assert.DoesNotContain(AuthApiFactory.AdminPassword, text);
        Assert.Contains("Login failed for nobody: UnknownUser", text);
        Assert.Contains("Login failed for admin: WrongPassword", text);
        Assert.Contains("User admin logged in", text);
    }

    [Fact]
    public async Task ALockout_IsLogged()
    {
        for (var i = 0; i < 5; i++)
        {
            await AuthApiFactory.LoginAsync(factory.CreateHttpsClient(), AuthApiFactory.AdminUsername, "wrong password!!");
        }

        Assert.Contains(factory.Logs.Entries, e => e.Level == LogLevel.Warning && e.Message.Contains("locked for 00:15:00"));
    }

    [Fact]
    public async Task ControlCharactersInAUsername_CannotForgeLogLines()
    {
        await AuthApiFactory.LoginAsync(factory.CreateHttpsClient(), "x\r\nFORGED LINE", "wrong password!!");

        Assert.All(factory.Logs.Entries, e => Assert.DoesNotContain('\n', e.Message));
    }

    // ---- helpers ----

    private static string ExtractCookieHeader(HttpResponseMessage response)
    {
        // When a login replaces a running session the response first deletes the old cookie, then sets the new one.
        var setCookie = response.Headers.GetValues("Set-Cookie").Last();
        return setCookie[..setCookie.IndexOf(';')];
    }

    private async Task<HttpStatusCode> MeWithCookieAsync(string cookieHeader)
    {
        // A client without its own cookie handling, so exactly the copied value is sent.
        var client = factory.CreateDefaultClient(new Uri(WebApplicationFactoryExtensions.Host));
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Add("Cookie", cookieHeader);
        return (await client.SendAsync(request)).StatusCode;
    }
}
