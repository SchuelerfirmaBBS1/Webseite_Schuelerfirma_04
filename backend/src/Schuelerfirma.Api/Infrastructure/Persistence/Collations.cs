namespace Schuelerfirma.Api.Infrastructure.Persistence;

/// <summary>Collations used by the database mapping.</summary>
internal static class Collations
{
    /// <summary>Case-insensitive, accent-sensitive comparison (MySQL 8.0+).</summary>
    public const string CaseInsensitive = "utf8mb4_0900_as_ci";
}