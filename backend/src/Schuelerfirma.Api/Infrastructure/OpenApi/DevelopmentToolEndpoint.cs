namespace Schuelerfirma.Api.Infrastructure.OpenApi;

/// <summary>
/// Endpoint metadata that marks an endpoint of the developer tools (the OpenAPI document under <c>/openapi</c>).
/// These endpoints exist only in Development and are not part of the API, so the rules for the three API areas
/// make an exception for them, but only under <c>/openapi</c>.
/// </summary>
public sealed class DevelopmentToolEndpoint
{
    /// <summary>The path all developer-tool endpoints live under.</summary>
    public const string PathPrefix = "/openapi";

    /// <summary>The single instance to attach as metadata.</summary>
    public static DevelopmentToolEndpoint Instance { get; } = new();

    private DevelopmentToolEndpoint()
    {
    }
}
