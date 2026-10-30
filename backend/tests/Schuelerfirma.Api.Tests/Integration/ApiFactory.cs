using Microsoft.AspNetCore.Mvc.Testing;

namespace Schuelerfirma.Api.Tests.Integration;

/// <summary>
/// Test host for the API.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
}

/// <summary>
/// Helpers for clients that talk to the API like a browser would in production.
/// </summary>
public static class WebApplicationFactoryExtensions
{
    /// <summary>Host name the test clients talk to; not <c>localhost</c> because HSTS skips localhost.</summary>
    public const string Host = "https://sf.example.test";

    /// <summary>
    /// Creates a client that uses HTTPS and does not follow redirects. An extension method because
    /// <c>WithWebHostBuilder</c> returns the base factory type, not <see cref="ApiFactory"/>.
    /// </summary>
    public static HttpClient CreateHttpsClient(this WebApplicationFactory<Program> factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri(Host),
            AllowAutoRedirect = false,
        });
}
