using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Schuelerfirma.Api.Tests.Auth;
using Schuelerfirma.Api.Tests.Integration;

namespace Schuelerfirma.Api.Tests.Http;

public class CsrfTests : IDisposable
{
    private const string HeaderName = "X-XSRF-TOKEN";
    private const string TokenCookie = "XSRF-TOKEN";

    private readonly AuthApiFactory factory = new();

    public void Dispose() => factory.Dispose();

    // A client that never adds the header by itself; the tests decide which token is sent.
    private HttpClient ManualClient() => factory.CreateHttpsClient(sendCsrfToken: false);

    private static string? TokenFrom(HttpResponseMessage response)
    {
        var setCookie = response.Headers.TryGetValues("Set-Cookie", out var values)
            ? values.LastOrDefault(c => c.StartsWith(TokenCookie + "="))
            : null;
        return setCookie?[(TokenCookie.Length + 1)..setCookie.IndexOf(';')];
    }

    private static async Task<string> FetchTokenAsync(HttpClient client) =>
        TokenFrom(await client.GetAsync("/api/auth/csrf"))!;

    private static Task<HttpResponseMessage> SendAsync(HttpClient client, HttpMethod method, string path, string? token, object? body = null)
    {
        var request = new HttpRequestMessage(method, path) { Content = body is null ? null : JsonContent.Create(body) };
        if (token is not null)
        {
            request.Headers.Add(HeaderName, token);
        }

        return client.SendAsync(request);
    }

    private static Task<HttpResponseMessage> LoginAsync(HttpClient client, string? token) =>
        SendAsync(client, HttpMethod.Post, "/api/auth/login", token,
            new { username = AuthApiFactory.AdminUsername, password = AuthApiFactory.AdminPassword });

    /// <summary>Logs in with a client that does not add tokens itself; returns the token issued with the login.</summary>
    private async Task<(HttpClient Client, string Token)> LoggedInManualClientAsync()
    {
        var client = ManualClient();
        var login = await LoginAsync(client, await FetchTokenAsync(client));
        Assert.Equal(HttpStatusCode.NoContent, login.StatusCode);
        return (client, TokenFrom(login)!);
    }

    // ---- token endpoint ----

    [Fact]
    public async Task TheTokenEndpoint_IsAnonymous_AndSetsBothCookiesWithTheRequiredFlags()
    {
        var response = await ManualClient().GetAsync("/api/auth/csrf");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var cookies = response.Headers.GetValues("Set-Cookie").Select(c => c.ToLowerInvariant()).ToList();

        var secret = Assert.Single(cookies, c => c.StartsWith("__host-sf.csrf="));
        Assert.Contains("httponly", secret);
        Assert.Contains("secure", secret);
        Assert.Contains("samesite=strict", secret);
        Assert.Contains("path=/", secret);

        // Readable for JavaScript, otherwise the frontend could not copy it into the header.
        var readable = Assert.Single(cookies, c => c.StartsWith("xsrf-token="));
        Assert.DoesNotContain("httponly", readable);
        Assert.Contains("secure", readable);
        Assert.Contains("samesite=strict", readable);
        Assert.Contains("path=/", readable);
        Assert.DoesNotContain("domain=", readable);
    }

    [Fact]
    public async Task TheTokenResponse_IsNotCacheable()
    {
        var response = await ManualClient().GetAsync("/api/auth/csrf");

        Assert.Contains("no-store", response.Headers.CacheControl?.ToString());
    }

    // ---- the check ----

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public async Task WritingRequests_WithoutToken_AreRejected_WithAProblemDetails(string method)
    {
        var (client, _) = await LoggedInManualClientAsync();

        var response = await SendAsync(client, new HttpMethod(method), "/test/security/protected", token: null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Sicherheitstoken fehlt oder ist ungültig", problem.GetProperty("title").GetString());
    }

    [Fact]
    public async Task AWrongToken_IsRejected_AValidOneIsAccepted()
    {
        var (client, token) = await LoggedInManualClientAsync();

        var wrong = await SendAsync(client, HttpMethod.Post, "/test/security/protected", "not-a-token");
        var valid = await SendAsync(client, HttpMethod.Post, "/test/security/protected", token);

        Assert.Equal(HttpStatusCode.BadRequest, wrong.StatusCode);
        Assert.Equal(HttpStatusCode.OK, valid.StatusCode);
    }

    [Fact]
    public async Task ATokenFromAnotherBrowser_IsRejected()
    {
        // The header alone is not enough: it has to match the HttpOnly cookie of this browser.
        var (client, _) = await LoggedInManualClientAsync();
        var (_, otherToken) = await LoggedInManualClientAsync();

        var response = await SendAsync(client, HttpMethod.Post, "/test/security/protected", otherToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ReadingRequests_NeedNoToken()
    {
        var (client, _) = await LoggedInManualClientAsync();

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/auth/me")).StatusCode);
    }

    [Fact]
    public async Task WithoutLogin_TheAnswerIs401_NotACsrfError()
    {
        var response = await SendAsync(ManualClient(), HttpMethod.Post, "/test/security/protected", token: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AnonymousVisitors_NeedATokenForPublicEndpoints_Too()
    {
        var client = ManualClient();

        var without = await SendAsync(client, HttpMethod.Post, "/test/security/open", token: null);
        var with = await SendAsync(client, HttpMethod.Post, "/test/security/open", await FetchTokenAsync(client));

        Assert.Equal(HttpStatusCode.BadRequest, without.StatusCode);
        Assert.Equal(HttpStatusCode.OK, with.StatusCode);
    }

    [Fact]
    public async Task EndpointsMarkedWithIgnoreAntiforgeryToken_AreNotChecked()
    {
        var response = await SendAsync(ManualClient(), HttpMethod.Post, "/test/security/no-csrf", token: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ---- login and logout ----

    [Fact]
    public async Task Login_WithoutToken_IsRejected_AndStartsNoSession()
    {
        var response = await LoginAsync(ManualClient(), token: null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(response.Headers.Contains("Set-Cookie"));
        var admin = (await factory.Users.GetByUsernameAsync(AuthApiFactory.AdminUsername))!;
        Assert.Equal(0, await factory.Sessions.DeleteAllForUserAsync(admin.UserId));
    }

    [Fact]
    public async Task Login_IssuesANewToken_ThatWorks_WhileTheAnonymousOneNoLongerDoes()
    {
        var client = ManualClient();
        var anonymousToken = await FetchTokenAsync(client);

        var login = await LoginAsync(client, anonymousToken);
        var newToken = TokenFrom(login);

        Assert.NotNull(newToken);
        Assert.NotEqual(anonymousToken, newToken);
        Assert.Equal(HttpStatusCode.BadRequest, (await SendAsync(client, HttpMethod.Post, "/test/security/protected", anonymousToken)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(client, HttpMethod.Post, "/test/security/protected", newToken)).StatusCode);
    }

    [Fact]
    public async Task Logout_IssuesAnAnonymousToken_ForTheNextLogin()
    {
        var (client, token) = await LoggedInManualClientAsync();

        var logout = await SendAsync(client, HttpMethod.Post, "/api/auth/logout", token);
        var anonymousToken = TokenFrom(logout);
        var nextLogin = await LoginAsync(client, anonymousToken);

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.NotNull(anonymousToken);
        Assert.Equal(HttpStatusCode.NoContent, nextLogin.StatusCode);
    }

    [Fact]
    public async Task LoginWhileLoggedIn_WithTheTokenOfTheRunningSession_IsRefused_UntilLoggedOut()
    {
        // A login always runs as an anonymous request (LoginStartsNewSessionMiddleware), but the token of a running
        // session belongs to its user. On the trade-fair laptop the next person therefore has to log out first.
        var (client, token) = await LoggedInManualClientAsync();

        var refused = await LoginAsync(client, token);
        var tokenWhileLoggedIn = await FetchTokenAsync(client);
        var stillRefused = await LoginAsync(client, tokenWhileLoggedIn);

        Assert.Equal(HttpStatusCode.BadRequest, refused.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, stillRefused.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/auth/me")).StatusCode);
    }

    [Fact]
    public async Task AfterTheSessionExpired_ANewlyFetchedToken_AllowsTheNextLogin()
    {
        var (client, _) = await LoggedInManualClientAsync();
        factory.Time.Advance(TimeSpan.FromMinutes(31));

        var token = await FetchTokenAsync(client);
        var login = await LoginAsync(client, token);

        Assert.Equal(HttpStatusCode.NoContent, login.StatusCode);
    }

    [Fact]
    public async Task TheDefaultTestClient_BehavesLikeTheFrontend()
    {
        // Guards the helper all other tests rely on.
        var client = factory.CreateHttpsClient();

        Assert.Equal(HttpStatusCode.NoContent, (await AuthApiFactory.LoginAsync(client, AuthApiFactory.AdminUsername, AuthApiFactory.AdminPassword)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/test/security/protected", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync("/api/auth/logout", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/test/security/open", null)).StatusCode);
    }
}
