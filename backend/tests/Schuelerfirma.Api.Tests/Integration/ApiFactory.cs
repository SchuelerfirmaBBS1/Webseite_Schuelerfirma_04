using Microsoft.AspNetCore.Hosting;
using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Mvc.Testing.Handlers;
using Microsoft.Extensions.Configuration;
using Schuelerfirma.Api.Infrastructure.Http;

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
    /// Creates a client that uses HTTPS, keeps cookies and does not follow redirects. By default it behaves like the
    /// frontend: before a POST, PUT, PATCH or DELETE it fetches the CSRF token once (<c>GET /api/auth/csrf</c>) and
    /// sends the current value of the <c>XSRF-TOKEN</c> cookie in the <c>X-XSRF-TOKEN</c> header, unless the request
    /// already has that header. An extension method because <c>WithWebHostBuilder</c> returns the base factory type.
    /// </summary>
    public static HttpClient CreateHttpsClient(this WebApplicationFactory<Program> factory, bool sendCsrfToken = true)
    {
        var cookies = new CookieContainer();
        DelegatingHandler[] handlers = sendCsrfToken
            ? [new CsrfTokenHandler(cookies), new CookieContainerHandler(cookies)]
            : [new CookieContainerHandler(cookies)];
        return factory.CreateDefaultClient(new Uri(Host), handlers);
    }

    /// <summary>Copies the CSRF token from the cookie into the header, as the frontend does.</summary>
    private sealed class CsrfTokenHandler(CookieContainer cookies) : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var method = request.Method;
            var safe = method == HttpMethod.Get || method == HttpMethod.Head || method == HttpMethod.Options || method == HttpMethod.Trace;
            if (!safe && !request.Headers.Contains(CsrfProtectionExtensions.HeaderName))
            {
                var token = ReadToken(request.RequestUri!);
                if (token is null)
                {
                    using var fetch = new HttpRequestMessage(HttpMethod.Get, new Uri(request.RequestUri!, "/api/auth/csrf"));
                    (await base.SendAsync(fetch, cancellationToken)).Dispose();
                    token = ReadToken(request.RequestUri!);
                }

                request.Headers.Add(CsrfProtectionExtensions.HeaderName, token);
            }

            return await base.SendAsync(request, cancellationToken);
        }

        private string? ReadToken(Uri uri) =>
            cookies.GetCookies(uri)[CsrfProtectionExtensions.RequestTokenCookieName]?.Value;
    }
}
