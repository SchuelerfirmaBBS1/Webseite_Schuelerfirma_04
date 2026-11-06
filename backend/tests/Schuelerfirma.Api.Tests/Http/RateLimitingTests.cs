using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Schuelerfirma.Api.Infrastructure.Http;
using Schuelerfirma.Api.Tests.Auth;
using Schuelerfirma.Api.Tests.Integration;

namespace Schuelerfirma.Api.Tests.Http;

public class RateLimitingTests : IDisposable
{
    private readonly RateLimitApiFactory factory = new();

    public void Dispose() => factory.Dispose();

    private async Task<HttpResponseMessage> LoginFromAsync(HttpClient client, string remoteIp, string password = "wrong password!!", string? forwardedFor = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(new { username = AuthApiFactory.AdminUsername, password }),
        };
        request.Headers.Add(TestRemoteIpStartupFilter.Header, remoteIp);
        if (forwardedFor is not null)
        {
            request.Headers.Add("X-Forwarded-For", forwardedFor);
        }

        return await client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> ContactFromAsync(HttpClient client, string remoteIp)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/test/security/contact");
        request.Headers.Add(TestRemoteIpStartupFilter.Header, remoteIp);
        return await client.SendAsync(request);
    }

    // ---- login ----

    [Fact]
    public async Task TheSixthLoginWithinAMinute_IsAnswered429_WithRetryAfter_EvenWithTheCorrectPassword()
    {
        var client = factory.CreateHttpsClient();
        for (var i = 0; i < 5; i++)
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await LoginFromAsync(client, "203.0.113.10")).StatusCode);
        }

        var response = await LoginFromAsync(client, "203.0.113.10", AuthApiFactory.AdminPassword);

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.Equal(TimeSpan.FromMinutes(1), response.Headers.RetryAfter?.Delta);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(429, problem.GetProperty("status").GetInt32());
        Assert.Equal("Zu viele Anfragen", problem.GetProperty("title").GetString());
        Assert.False(response.Headers.Contains("Set-Cookie"));
    }

    [Fact]
    public async Task ARejectedLogin_IsLogged_WithPolicyAndClient()
    {
        var client = factory.CreateHttpsClient();
        for (var i = 0; i < 6; i++)
        {
            await LoginFromAsync(client, "203.0.113.11");
        }

        Assert.Contains(factory.Logs.Entries, e => e.Level == LogLevel.Warning && e.Message == "Rate limit 'auth' exceeded by 203.0.113.11");
    }

    [Fact]
    public async Task OtherClients_AreNotAffected()
    {
        var client = factory.CreateHttpsClient();
        for (var i = 0; i < 6; i++)
        {
            await LoginFromAsync(client, "203.0.113.20");
        }

        // 401, not 429: the request reached the login (the account is locked by now, that is the lockout).
        var other = await LoginFromAsync(client, "203.0.113.21");

        Assert.Equal(HttpStatusCode.Unauthorized, other.StatusCode);
    }

    [Fact]
    public async Task BehindTheTrustedProxy_TheForwardedClientIpCounts()
    {
        // Loopback is the trusted proxy by default; every visitor behind it has an own limit.
        var client = factory.CreateHttpsClient();
        for (var i = 0; i < 6; i++)
        {
            await LoginFromAsync(client, "127.0.0.1", forwardedFor: "198.51.100.1");
        }

        var exhausted = await LoginFromAsync(client, "127.0.0.1", forwardedFor: "198.51.100.1");
        var otherVisitor = await LoginFromAsync(client, "127.0.0.1", forwardedFor: "198.51.100.2");

        Assert.Equal(HttpStatusCode.TooManyRequests, exhausted.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, otherVisitor.StatusCode);
    }

    [Fact]
    public async Task AClientCannotEscapeTheLimit_BySendingItsOwnForwardedHeader()
    {
        var client = factory.CreateHttpsClient();
        for (var i = 0; i < 5; i++)
        {
            await LoginFromAsync(client, "203.0.113.30", forwardedFor: $"198.51.100.{i + 10}");
        }

        var response = await LoginFromAsync(client, "203.0.113.30", forwardedFor: "198.51.100.99");

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
    }

    [Fact]
    public async Task OneIPv6Network_SharesOneLimit()
    {
        var client = factory.CreateHttpsClient();
        for (var i = 0; i < 5; i++)
        {
            await LoginFromAsync(client, $"2001:db8:1:2::{i + 1}");
        }

        var sameNetwork = await LoginFromAsync(client, "2001:db8:1:2:ffff::1");
        var otherNetwork = await LoginFromAsync(client, "2001:db8:1:3::1");

        Assert.Equal(HttpStatusCode.TooManyRequests, sameNetwork.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, otherNetwork.StatusCode);
    }

    [Fact]
    public async Task OtherAuthEndpoints_AreNotLimited()
    {
        var client = factory.CreateHttpsClient();
        for (var i = 0; i < 6; i++)
        {
            await LoginFromAsync(client, "203.0.113.40");
        }

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/csrf");
        request.Headers.Add(TestRemoteIpStartupFilter.Header, "203.0.113.40");

        Assert.Equal(HttpStatusCode.NoContent, (await client.SendAsync(request)).StatusCode);
    }

    // ---- contact ----

    [Fact]
    public async Task TheContactPolicy_UsesItsOwnConfiguredLimit()
    {
        var client = factory.CreateHttpsClient();
        for (var i = 0; i < RateLimitApiFactory.ContactLimit; i++)
        {
            Assert.Equal(HttpStatusCode.OK, (await ContactFromAsync(client, "203.0.113.50")).StatusCode);
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, (await ContactFromAsync(client, "203.0.113.50")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await ContactFromAsync(client, "203.0.113.51")).StatusCode);
        // The login limit is a separate counter.
        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginFromAsync(client, "203.0.113.50")).StatusCode);
    }

    // ---- configuration ----

    [Fact]
    public void TheDefaults_Are5PerMinuteForLogin_And10Per10MinutesForTheContactForm()
    {
        using var plain = new ApiFactory();
        plain.CreateHttpsClient();
        var options = plain.Services.GetRequiredService<IOptionsMonitor<RateLimitPolicyOptions>>();

        Assert.Equal(5, options.Get(RateLimitPolicies.Auth).PermitLimit);
        Assert.Equal(TimeSpan.FromMinutes(1), options.Get(RateLimitPolicies.Auth).Window);
        Assert.Equal(10, options.Get(RateLimitPolicies.Contact).PermitLimit);
        Assert.Equal(TimeSpan.FromMinutes(10), options.Get(RateLimitPolicies.Contact).Window);
    }

    [Theory]
    [InlineData("RateLimiting:Auth:PermitLimit", "0")]
    [InlineData("RateLimiting:Auth:Window", "00:00:00")]
    [InlineData("RateLimiting:Contact:Window", "2.00:00:00")]
    public void InvalidLimits_PreventTheStart(string key, string value)
    {
        using var invalid = factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration(
            (_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?> { [key] = value })));

        Assert.ThrowsAny<Exception>(() => invalid.CreateHttpsClient());
    }
}
