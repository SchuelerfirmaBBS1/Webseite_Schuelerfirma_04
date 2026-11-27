using Schuelerfirma.Api.Application.Abstractions;

namespace Schuelerfirma.Api.Infrastructure;

/// <summary>
/// Makes sure the application never runs without real storage.
/// </summary>
public static class PersistenceStartupCheck
{
    /// <summary>
    /// Verifies that every repository interface has an implementation. Outside Development the
    /// repositories are only registered when a database connection string is configured, so without one
    /// the application refuses to start instead of silently running without a database.
    /// </summary>
    /// <remarks>
    /// Only checks the registrations and creates no instances: the EF Core repositories are scoped and
    /// must not be resolved from the root provider.
    /// </remarks>
    /// <exception cref="InvalidOperationException">A repository is not registered.</exception>
    public static void EnsurePersistenceRegistered(this IServiceProvider services)
    {
        Type[] required =
        [
            typeof(IUserRepository), typeof(IRoleRepository), typeof(IUserSessionRepository),
            typeof(ICategoryRepository), typeof(IProductRepository),
        ];

        var registrations = services.GetRequiredService<IServiceProviderIsService>();
        var missing = required.Where(type => !registrations.IsService(type)).Select(type => type.Name).ToList();
        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                $"No persistence is registered (missing: {string.Join(", ", missing)}). " +
                "The in-memory repositories are only available in Development.");
        }
    }
}
