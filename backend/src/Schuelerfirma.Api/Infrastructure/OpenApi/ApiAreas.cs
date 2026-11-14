namespace Schuelerfirma.Api.Infrastructure.OpenApi;

/// <summary>An API area as it appears in the OpenAPI document: path prefix, tag and explanation.</summary>
/// <param name="PathPrefix">Path prefix of the area, e.g. <c>/api/auth</c>.</param>
/// <param name="Tag">Tag the test UI groups the area's operations by.</param>
/// <param name="Description">Explanation shown above the group.</param>
public sealed record ApiArea(string PathPrefix, string Tag, string Description);

/// <summary>
/// The three API areas (see <c>PublicApiController</c>, <c>AdminApiController</c> and <c>AuthController</c>), in the
/// order the test UI shows them.
/// </summary>
public static class ApiAreas
{
    /// <summary>Login, logout, current user and CSRF token.</summary>
    public static ApiArea Auth { get; } = new(
        "/api/auth", "auth", "Anmeldung, Abmeldung, angemeldeter Benutzer und CSRF-Token. Hier anfangen.");

    /// <summary>Admin area: requires a login and the matching permission.</summary>
    public static ApiArea Admin { get; } = new(
        "/api/admin", "admin", "Verwaltung: nur mit Login und der passenden Berechtigung der Rolle.");

    /// <summary>Public area: works without login.</summary>
    public static ApiArea Public { get; } = new(
        "/api/public", "public", "Öffentlich: ohne Login erreichbar, für die Besucher der Webseite.");

    /// <summary>All areas in display order.</summary>
    public static IReadOnlyList<ApiArea> All { get; } = [Auth, Admin, Public];

    /// <summary>Returns the area <paramref name="path"/> belongs to, or <c>null</c>.</summary>
    public static ApiArea? Find(string path) => All.FirstOrDefault(area =>
        path.Equals(area.PathPrefix, StringComparison.OrdinalIgnoreCase)
        || path.StartsWith(area.PathPrefix + "/", StringComparison.OrdinalIgnoreCase));
}
