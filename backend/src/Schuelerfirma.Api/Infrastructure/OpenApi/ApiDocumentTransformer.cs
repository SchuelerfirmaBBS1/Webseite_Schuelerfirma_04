using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Schuelerfirma.Api.Infrastructure.OpenApi;

/// <summary>
/// Sets title and description of the OpenAPI document (including how to log in through the test UI) and describes
/// the three API areas that the operations are grouped by.
/// </summary>
public sealed class ApiDocumentTransformer : IOpenApiDocumentTransformer
{
    private const string Description = """
        Testoberfläche für die Entwicklung, **nur in Development** verfügbar.

        **Ablauf**
        1. `GET /api/auth/csrf` ausführen. Das setzt die CSRF-Cookies; die Oberfläche schickt das Token danach bei jedem
           POST, PUT, PATCH und DELETE automatisch im Header `X-XSRF-TOKEN` mit.
        2. `POST /api/auth/login` mit Benutzername und Passwort. Das setzt das Session-Cookie, der Browser schickt es bei
           allen weiteren Anfragen mit.
        3. Endpunkte ausprobieren. `GET /api/auth/me` zeigt den angemeldeten Benutzer und seine Berechtigungen.
        4. `POST /api/auth/logout` beendet die Session.

        Die Cookies sind `Secure`, deshalb funktioniert die Oberfläche nur über HTTPS (`https://localhost:7205/swagger`,
        Entwicklungszertifikat einmalig mit `dotnet dev-certs https --trust` vertrauen).
        Antwortet ein schreibender Endpunkt mit 400 „Sicherheitstoken fehlt oder ist ungültig“, Schritt 1 wiederholen.
        """;

    /// <inheritdoc />
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Info = new OpenApiInfo
        {
            Title = "Schülerfirma API",
            Version = ApiDocumentationExtensions.DocumentName,
            Description = Description,
        };

        // Replaces the default tags (one per controller): the UI groups by area instead.
        document.Tags = new HashSet<OpenApiTag>(ApiAreas.All.Select(area => new OpenApiTag
        {
            Name = area.Tag,
            Description = area.Description,
        }));

        return Task.CompletedTask;
    }
}
