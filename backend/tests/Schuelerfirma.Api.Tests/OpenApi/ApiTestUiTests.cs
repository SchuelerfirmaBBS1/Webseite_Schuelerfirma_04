using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Schuelerfirma.Api.Infrastructure.Http;
using Schuelerfirma.Api.Infrastructure.InMemory;
using Schuelerfirma.Api.Infrastructure.OpenApi;
using Schuelerfirma.Api.Tests.Integration;

namespace Schuelerfirma.Api.Tests.OpenApi;

public partial class ApiTestUiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    // ---- Development: the UI is there, without login ----

    [Fact]
    public async Task Development_SwaggerUi_IsServedWithoutLogin()
    {
        using var response = await factory.CreateHttpsClient().GetAsync("/swagger/index.html");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Development_SwaggerUi_LoadsNothingFromOtherServers()
    {
        var html = await factory.CreateHttpsClient().GetStringAsync("/swagger/index.html");

        var references = ExternalReference().Matches(html).Select(match => match.Value).ToList();
        Assert.Empty(references);
        Assert.Contains("swagger-ui-bundle.js", html);
    }

    [Fact]
    public async Task Development_SwaggerUi_CopiesTheCsrfCookieIntoTheHeader()
    {
        // The interceptor is part of the UI's configuration script; JSON-encoded, hence no exact match on the code.
        var script = await factory.CreateHttpsClient().GetStringAsync("/swagger/index.js");

        Assert.Contains("RequestInterceptorFunction", script);
        Assert.Contains(CsrfProtectionExtensions.RequestTokenCookieName + "=", script);
        Assert.Contains(CsrfProtectionExtensions.HeaderName, script);
    }

    [Fact]
    public async Task Development_OpenApiDocument_IsServedWithoutLogin_AndExplainsTheLogin()
    {
        using var response = await factory.CreateHttpsClient().GetAsync(ApiDocumentationExtensions.DocumentPath);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var description = document.RootElement.GetProperty("info").GetProperty("description").GetString();
        Assert.Contains("GET /api/auth/csrf", description);
        Assert.Contains("dotnet dev-certs https --trust", description);
    }

    [Fact]
    public async Task OpenApiDocument_ContainsAllEndpoints_GroupedByArea()
    {
        using var document = await GetDocumentAsync();
        var root = document.RootElement;

        Assert.Equal(["auth", "admin", "public"], root.GetProperty("tags").EnumerateArray().Select(t => t.GetProperty("name").GetString()));
        Assert.Equal(["auth"], Tags(Operation(root, "/api/auth/login", "post")));
        Assert.Equal(["auth"], Tags(Operation(root, "/api/auth/me", "get")));
        Assert.Equal(["admin"], Tags(Operation(root, "/api/admin/users", "get")));
        Assert.Equal(["admin"], Tags(Operation(root, "/api/admin/roles", "get")));
    }

    [Fact]
    public async Task OpenApiDocument_UsesTheXmlComments()
    {
        using var document = await GetDocumentAsync();
        var login = Operation(document.RootElement, "/api/auth/login", "post");

        Assert.StartsWith("Logs in and sets the session cookie.", login.GetProperty("summary").GetString());
        Assert.Equal("The credentials were not accepted.", Response(login, "401").GetProperty("description").GetString());
    }

    [Fact]
    public async Task OpenApiDocument_ListsTheAnswersOfMiddlewareAndFilters()
    {
        using var document = await GetDocumentAsync();
        var root = document.RootElement;

        // Admin: login and permission required, writing requests need the CSRF token.
        var createUser = Operation(root, "/api/admin/users", "post");
        Assert.Superset(new HashSet<string> { "201", "400", "401", "403", "409" }, ResponseCodes(createUser).ToHashSet());
        Assert.Contains("CSRF", Response(createUser, "400").GetProperty("description").GetString());

        // Login: anonymous (no 401 from the fallback policy, its own 401 stays) but rate limited.
        var login = Operation(root, "/api/auth/login", "post");
        Assert.Contains("429", ResponseCodes(login));
        Assert.DoesNotContain("403", ResponseCodes(login));

        // Anonymous GET: neither 401 nor the CSRF 400.
        Assert.Equal(["204"], ResponseCodes(Operation(root, "/api/auth/csrf", "get")));

        // Logged in without [HasPermission]: 401, but no 403.
        var me = Operation(root, "/api/auth/me", "get");
        Assert.Contains("401", ResponseCodes(me));
        Assert.DoesNotContain("403", ResponseCodes(me));
    }

    [Fact]
    public async Task OpenApiDocument_DescribesErrorsAsProblemJson()
    {
        using var document = await GetDocumentAsync();
        var getUser = Operation(document.RootElement, "/api/admin/users/{id}", "get");

        foreach (var code in new[] { "401", "403", "404" })
        {
            var content = Response(getUser, code).GetProperty("content");
            Assert.Equal(["application/problem+json"], content.EnumerateObject().Select(media => media.Name));
            Assert.Equal(
                "#/components/schemas/ProblemDetails",
                content.GetProperty("application/problem+json").GetProperty("schema").GetProperty("$ref").GetString());
        }
    }

    [Fact]
    public void Development_TheOpenApiEndpointIsTheOnlyDevelopmentTool()
    {
        factory.CreateHttpsClient();

        var tool = Assert.Single(DevelopmentTools(factory));
        Assert.StartsWith(DevelopmentToolEndpoint.PathPrefix + "/", "/" + tool.RoutePattern.RawText?.TrimStart('/'));
    }

    // ---- every other environment: nothing of it ----

    [Theory]
    [InlineData("/swagger")]
    [InlineData("/swagger/index.html")]
    [InlineData("/swagger/index.js")]
    [InlineData(ApiDocumentationExtensions.DocumentPath)]
    public async Task Production_TheTestUiIsNotThere(string path)
    {
        using var production = CreateProductionFactory();

        using var response = await production.CreateHttpsClient().GetAsync(path);

        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.MovedPermanently, response.StatusCode);
        Assert.DoesNotContain("swagger", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Production_HasNoDevelopmentTools()
    {
        using var production = CreateProductionFactory();
        production.CreateHttpsClient();

        Assert.Empty(DevelopmentTools(production));
    }

    private async Task<JsonDocument> GetDocumentAsync() =>
        JsonDocument.Parse(await factory.CreateHttpsClient().GetStringAsync(ApiDocumentationExtensions.DocumentPath));

    private static JsonElement Operation(JsonElement root, string path, string method) =>
        root.GetProperty("paths").GetProperty(path).GetProperty(method);

    private static JsonElement Response(JsonElement operation, string code) =>
        operation.GetProperty("responses").GetProperty(code);

    private static List<string> ResponseCodes(JsonElement operation) =>
        [.. operation.GetProperty("responses").EnumerateObject().Select(response => response.Name).Order()];

    private static List<string?> Tags(JsonElement operation) =>
        [.. operation.GetProperty("tags").EnumerateArray().Select(tag => tag.GetString())];

    private static List<RouteEndpoint> DevelopmentTools(WebApplicationFactory<Program> host) =>
    [
        .. host.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Where(endpoint => endpoint.Metadata.GetMetadata<DevelopmentToolEndpoint>() is not null),
    ];

    private WebApplicationFactory<Program> CreateProductionFactory() => factory.WithWebHostBuilder(builder =>
    {
        builder.UseEnvironment("Production");
        // Production has no storage yet; these tests are about the test UI, not about data.
        builder.ConfigureServices(services => services.AddInMemoryPersistence());
    });

    // src or href pointing to another server (absolute or protocol-relative URL).
    [GeneratedRegex("""(?:src|href)\s*=\s*["']?(?:https?:)?//""", RegexOptions.IgnoreCase)]
    private static partial Regex ExternalReference();
}
