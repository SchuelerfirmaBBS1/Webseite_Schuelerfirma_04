using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Schuelerfirma.Api.Infrastructure.InMemory;

namespace Schuelerfirma.Api.Tests.Integration;

public class SkeletonTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task UnknownEndpoint_ReturnsProblemDetails404()
    {
        var response = await factory.CreateHttpsClient().GetAsync("/api/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(404, problem.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task Responses_CarrySecurityHeaders()
    {
        var response = await factory.CreateHttpsClient().GetAsync("/api/does-not-exist");

        AssertSecurityHeaders(response);
    }

    [Fact]
    public async Task Responses_DoNotAllowCrossOriginRequests()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/does-not-exist");
        request.Headers.Add("Origin", "https://evil.example");

        var response = await factory.CreateHttpsClient().SendAsync(request);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task UnhandledException_InProduction_ReturnsProblemDetailsWithoutStackTrace()
    {
        using var production = CreateProductionFactory();

        var response = await production.CreateHttpsClient().GetAsync("/test/throw");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(ThrowingController.SecretDetail, body);
        Assert.DoesNotContain("StackTrace", body, StringComparison.OrdinalIgnoreCase);
        AssertSecurityHeaders(response);
    }

    [Fact]
    public async Task Production_SendsHstsHeader()
    {
        using var production = CreateProductionFactory();

        var response = await production.CreateHttpsClient().GetAsync("/api/does-not-exist");

        Assert.True(response.Headers.Contains("Strict-Transport-Security"));
    }

    [Fact]
    public void TimeProvider_IsRegisteredAsSystemClock()
    {
        Assert.Same(TimeProvider.System, factory.Services.GetRequiredService<TimeProvider>());
    }

    [Fact]
    public void ForwardedHeaders_DefaultTrustsOnlyLoopback()
    {
        var options = factory.Services.GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;

        Assert.Equal([IPAddress.IPv6Loopback], options.KnownProxies);
        Assert.Contains(options.KnownIPNetworks, n => n.Contains(IPAddress.Loopback));
        Assert.Equal(ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto, options.ForwardedHeaders);
    }

    [Fact]
    public void ForwardedHeaders_ConfiguredProxiesReplaceTheDefaults()
    {
        using var configured = factory.WithWebHostBuilder(b => b.UseSetting("ForwardedHeaders:KnownProxies:0", "10.0.0.5"));

        var options = configured.Services.GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;

        Assert.Equal([IPAddress.Parse("10.0.0.5")], options.KnownProxies);
        Assert.Empty(options.KnownIPNetworks);
    }

    [Fact]
    public void ForwardedHeaders_InvalidProxyAddress_FailsAtStartup()
    {
        using var invalid = factory.WithWebHostBuilder(b => b.UseSetting("ForwardedHeaders:KnownProxies:0", "not-an-ip"));

        var exception = Assert.ThrowsAny<Exception>(() => invalid.CreateHttpsClient());

        Assert.Contains("not-an-ip", exception.ToString());
    }

    private WebApplicationFactory<Program> CreateProductionFactory() => factory.WithWebHostBuilder(builder =>
    {
        builder.UseEnvironment("Production");
        builder.ConfigureServices(services =>
        {
            // Production has no storage yet; these tests are about the HTTP pipeline, not about data.
            services.AddInMemoryPersistence();
            services.AddControllers().AddApplicationPart(typeof(ThrowingController).Assembly);
        });
    });

    private static void AssertSecurityHeaders(HttpResponseMessage response)
    {
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("frame-ancestors 'none'", response.Headers.GetValues("Content-Security-Policy").Single());
        Assert.Equal("no-referrer", response.Headers.GetValues("Referrer-Policy").Single());
    }
}
