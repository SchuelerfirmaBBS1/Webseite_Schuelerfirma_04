using Schuelerfirma.Api.Infrastructure.Http;

namespace Schuelerfirma.Api.Infrastructure.OpenApi;

/// <summary>
/// The API test UI for development (#51): an OpenAPI description of all endpoints under <c>/openapi/v1.json</c> and
/// Swagger UI under <c>/swagger</c>. Only for Development; <c>Program</c> registers nothing of this elsewhere, so in
/// every other environment both paths are answered like any unknown path.
/// </summary>
public static class ApiDocumentationExtensions
{
    /// <summary>Name of the OpenAPI document.</summary>
    public const string DocumentName = "v1";

    /// <summary>Path of the OpenAPI document.</summary>
    public const string DocumentPath = DevelopmentToolEndpoint.PathPrefix + "/" + DocumentName + ".json";

    /// <summary>Path prefix of Swagger UI (without leading slash, as Swagger UI expects it).</summary>
    public const string UiRoutePrefix = "swagger";

    // Does what the frontend does: copies the CSRF token from the readable cookie into the header of every writing
    // request. Swagger UI calls the API from the same address, so the browser sends the cookies on its own.
    private const string CsrfRequestInterceptor = $$"""
        (request) => {
          const method = (request.method || 'GET').toUpperCase();
          if (!['GET', 'HEAD', 'OPTIONS', 'TRACE'].includes(method)) {
            const match = document.cookie.match(/(?:^|;\s*){{CsrfProtectionExtensions.RequestTokenCookieName}}=([^;]*)/);
            if (match) {
              request.headers['{{CsrfProtectionExtensions.HeaderName}}'] = decodeURIComponent(match[1]);
            }
          }
          return request;
        }
        """;

    /// <summary>Registers the OpenAPI document with the descriptions of this project.</summary>
    public static IServiceCollection AddApiDocumentation(this IServiceCollection services) =>
        services.AddOpenApi(DocumentName, options =>
        {
            options.AddDocumentTransformer<ApiDocumentTransformer>();
            options.AddOperationTransformer<ApiAreaOperationTransformer>();
        });

    /// <summary>
    /// Serves Swagger UI under <c>/swagger</c>. The files come from the package, nothing is loaded from other servers.
    /// Register this before <c>UseAuthentication</c>: the fallback policy also applies to requests without an
    /// endpoint, so after it the UI itself would require a login.
    /// </summary>
    public static IApplicationBuilder UseApiTestUi(this IApplicationBuilder app) =>
        app.UseSwaggerUI(options =>
        {
            options.RoutePrefix = UiRoutePrefix;
            options.DocumentTitle = "Schülerfirma API – Testoberfläche";
            options.SwaggerEndpoint(DocumentPath, "Schülerfirma API");
            options.EnableTryItOutByDefault();
            options.DisplayRequestDuration();
            options.UseRequestInterceptor(CsrfRequestInterceptor);
        });

    /// <summary>
    /// Maps the OpenAPI document under <c>/openapi/v1.json</c>, readable without login and marked as
    /// <see cref="DevelopmentToolEndpoint"/>.
    /// </summary>
    public static IEndpointConventionBuilder MapApiDocumentation(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapOpenApi(DevelopmentToolEndpoint.PathPrefix + "/{documentName}.json")
            .AllowAnonymous()
            .WithMetadata(DevelopmentToolEndpoint.Instance);
}
