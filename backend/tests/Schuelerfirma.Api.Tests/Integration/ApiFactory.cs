using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Schuelerfirma.Api.Tests.Integration;

/// <summary>
/// Test host for the API.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string keysPath = Path.Combine(Path.GetTempPath(), "sf-test-keys-" + Guid.NewGuid().ToString("N"));

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // The session cookie is encrypted with persistent keys; keep the tests' keys out of the source tree.
        // Added last on purpose: values in the developer's user-secrets must not influence the tests.
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["DataProtection:KeysPath"] = keysPath,
                ["Bootstrap:AdminUsername"] = null,
                ["Bootstrap:AdminEmail"] = null,
                ["Bootstrap:AdminPassword"] = null,
            }));
    }

    /// <summary>Directory the encryption keys of this host are written to.</summary>
    public string KeysPath => keysPath;

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && Directory.Exists(keysPath))
        {
            Directory.Delete(keysPath, recursive: true);
        }
    }
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
